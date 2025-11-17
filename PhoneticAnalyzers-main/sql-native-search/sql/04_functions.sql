-- Utility + ingest functions (normalize, tokenize, phonetics, nickname expansion)

-- Normalizes a name: trim, collapse spaces, upper-case, remove accents
CREATE OR REPLACE FUNCTION normalize_name(p_name TEXT)
RETURNS TEXT LANGUAGE sql IMMUTABLE AS $$
  SELECT regexp_replace(upper(unaccent(coalesce(p_name, ''))), '\s+', ' ', 'g')::text;
$$;

-- Splits a normalized name into tokens preserving positions
CREATE OR REPLACE FUNCTION tokenize_name(p_normalized TEXT)
RETURNS TABLE(token TEXT, token_position INT) LANGUAGE plpgsql IMMUTABLE AS $$
DECLARE
  arr TEXT[] := regexp_split_to_array(coalesce(p_normalized, ''), '\s+');
  i INT := 1;
  tok TEXT;
BEGIN
  FOREACH tok IN ARRAY arr LOOP
    token := tok;
    token_position := i;
    RETURN NEXT;
    i := i + 1;
  END LOOP;
END;$$;

-- Insert a person (idempotent on external_id) and return person_id
CREATE OR REPLACE FUNCTION upsert_person(
  p_external_id TEXT,
  p_full_name   TEXT,
  p_county      TEXT DEFAULT NULL,
  p_flag        CHAR(1) DEFAULT 'I'
) RETURNS BIGINT LANGUAGE plpgsql AS $$
DECLARE
  v_person_id BIGINT;
  v_normalized TEXT := normalize_name(p_full_name);
BEGIN
  INSERT INTO person(external_id, full_name, normalized_name, county, flag)
  VALUES (p_external_id, p_full_name, v_normalized, p_county, p_flag)
  ON CONFLICT (external_id)
  DO UPDATE SET full_name = EXCLUDED.full_name,
                normalized_name = EXCLUDED.normalized_name,
                county = COALESCE(EXCLUDED.county, person.county),
                flag = COALESCE(EXCLUDED.flag, person.flag)
  RETURNING person_id INTO v_person_id;

  RETURN v_person_id;
END;$$;

-- Adds a token row with phonetic columns computed
CREATE OR REPLACE FUNCTION add_token_with_phonetics(
  p_person_id BIGINT,
  p_token TEXT,
  p_position INT
) RETURNS VOID LANGUAGE sql AS $$
  INSERT INTO person_names(person_id, name_token, token_position, soundex_code, metaphone_code, double_metaphone_code)
  VALUES (
    p_person_id,
    p_token,
    p_position,
    soundex(p_token),
    metaphone(p_token, 4),
    dmetaphone(p_token)
  );
$$;

-- Ingest a single person: tokenize, generate phonetics, expand nicknames
CREATE OR REPLACE FUNCTION ingest_person(
  p_external_id TEXT,
  p_full_name   TEXT,
  p_county      TEXT DEFAULT NULL,
  p_flag        CHAR(1) DEFAULT 'I'
) RETURNS BIGINT LANGUAGE plpgsql AS $$
DECLARE
  v_person_id BIGINT;
  v_norm TEXT := normalize_name(p_full_name);
  r RECORD;
  n RECORD;
BEGIN
  v_person_id := upsert_person(p_external_id, p_full_name, p_county, p_flag);

  -- Remove previous tokens for idempotency on re-ingest of same external_id
  DELETE FROM person_names WHERE person_id = v_person_id;

  -- Base tokens
  FOR r IN SELECT * FROM tokenize_name(v_norm) LOOP
    PERFORM add_token_with_phonetics(v_person_id, r.token, r.token_position);

    -- Nickname expansion for this token
    FOR n IN (
      SELECT nickname FROM nickname_maps WHERE canonical_name = r.token
      UNION ALL
      SELECT canonical_name FROM nickname_maps WHERE is_bidirectional AND nickname = r.token
    ) LOOP
      PERFORM add_token_with_phonetics(v_person_id, n.nickname, r.token_position);
    END LOOP;
  END LOOP;

  RETURN v_person_id;
END;$$;
