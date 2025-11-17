-- Update functions to track nickname variants for display

-- Enhanced version: adds a token row with phonetic columns and nickname tracking
CREATE OR REPLACE FUNCTION add_token_with_phonetics(
  p_person_id BIGINT,
  p_token TEXT,
  p_position INT,
  p_is_nickname BOOLEAN DEFAULT FALSE,
  p_original_token TEXT DEFAULT NULL
) RETURNS VOID LANGUAGE sql AS $$
  INSERT INTO person_names(person_id, name_token, token_position, soundex_code, metaphone_code, double_metaphone_code, is_nickname, original_token)
  VALUES (
    p_person_id,
    p_token,
    p_position,
    soundex(p_token),
    metaphone(p_token, 4),
    dmetaphone(p_token),
    p_is_nickname,
    p_original_token
  );
$$;

-- Enhanced ingest function: tokenize, generate phonetics, expand nicknames with tracking
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

  -- Base tokens (original, not nicknames)
  FOR r IN SELECT * FROM tokenize_name(v_norm) LOOP
    PERFORM add_token_with_phonetics(v_person_id, r.token, r.token_position, FALSE, NULL);

    -- Nickname expansion for this token
    FOR n IN (
      SELECT nickname FROM nickname_maps WHERE canonical_name = r.token
      UNION ALL
      SELECT canonical_name FROM nickname_maps WHERE is_bidirectional AND nickname = r.token
    ) LOOP
      PERFORM add_token_with_phonetics(v_person_id, n.nickname, r.token_position, TRUE, r.token);
    END LOOP;
  END LOOP;

  RETURN v_person_id;
END;$$;
