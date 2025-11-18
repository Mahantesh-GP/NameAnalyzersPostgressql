-- Enhancements for business name handling (flag='B')
-- Problems with current approach:
-- 1. Nickname expansion applies to business names (incorrect)
-- 2. Phonetic matching on suffixes like "LLC", "INC" creates noise
-- 3. No special handling for common business patterns

-- Add flag-aware columns to person_names
ALTER TABLE person_names ADD COLUMN IF NOT EXISTS is_business_suffix BOOLEAN DEFAULT FALSE;

-- Update business suffix detection
CREATE OR REPLACE FUNCTION mark_business_suffixes()
RETURNS VOID LANGUAGE plpgsql AS $$
BEGIN
  UPDATE person_names pn
  SET is_business_suffix = TRUE
  FROM person p
  WHERE pn.person_id = p.person_id
    AND p.flag = 'B'
    AND pn.name_token IN (
      'LIMITED', 'LTD', 'PRIVATE', 'PVT', 'LLC', 'INC', 'CORP', 'CO',
      'CORPORATION', 'COMPANY', 'SOLUTIONS', 'SERVICES', 'GROUP',
      'ENTERPRISES', 'INDUSTRIES', 'INTERNATIONAL', 'GLOBAL', 'PARTNERS',
      'ASSOCIATES', 'HOLDINGS', 'VENTURES', 'CAPITAL', 'MANAGEMENT',
      'TECHNOLOGIES', 'SYSTEMS', 'CONSULTING', 'FINANCIAL', 'INSURANCE',
      'INVESTMENTS', 'PROPERTIES', 'DEVELOPMENT', 'CONSTRUCTION', 'MARKETING'
    );
END;
$$;

-- Enhanced ingest_person: skip nickname expansion for businesses
CREATE OR REPLACE FUNCTION ingest_person(
  p_external_id TEXT,
  p_full_name   TEXT,
  p_county      TEXT DEFAULT NULL,
  p_flag        CHAR(1) DEFAULT 'I'
) RETURNS BIGINT LANGUAGE plpgsql AS $$
DECLARE
  v_person_id BIGINT;
  v_norm TEXT := normalize_name(p_full_name);
  v_is_business BOOLEAN := (p_flag = 'B');
  r RECORD;
  n RECORD;
BEGIN
  v_person_id := upsert_person(p_external_id, p_full_name, p_county, p_flag);

  -- Remove previous tokens for idempotency on re-ingest of same external_id
  DELETE FROM person_names WHERE person_id = v_person_id;

  -- Base tokens
  FOR r IN SELECT * FROM tokenize_name(v_norm) LOOP
    PERFORM add_token_with_phonetics(v_person_id, r.token, r.token_position, FALSE, NULL);

    -- Nickname expansion ONLY for individuals (flag='I'), NOT businesses
    IF NOT v_is_business THEN
      FOR n IN (
        SELECT nickname FROM nickname_maps WHERE canonical_name = r.token
        UNION ALL
        SELECT canonical_name FROM nickname_maps WHERE is_bidirectional AND nickname = r.token
      ) LOOP
        PERFORM add_token_with_phonetics(v_person_id, n.nickname, r.token_position, TRUE, r.token);
      END LOOP;
    END IF;
  END LOOP;

  RETURN v_person_id;
END;$$;

-- Enhanced normalize_business_name: strip common suffixes for core matching
CREATE OR REPLACE FUNCTION normalize_business_core(p_name TEXT)
RETURNS TEXT LANGUAGE sql IMMUTABLE AS $$
  SELECT regexp_replace(
    normalize_name(p_name),
    '\s+(LIMITED|LTD|PRIVATE|PVT|LLC|INC|CORP|CO|CORPORATION|COMPANY|SOLUTIONS|SERVICES|GROUP|ENTERPRISES|INDUSTRIES|INTERNATIONAL|GLOBAL|PARTNERS|ASSOCIATES|HOLDINGS|VENTURES|CAPITAL|MANAGEMENT|TECHNOLOGIES|SYSTEMS|CONSULTING|FINANCIAL|INSURANCE|INVESTMENTS|PROPERTIES|DEVELOPMENT|CONSTRUCTION|MARKETING)$',
    '',
    'g'
  )::text;
$$;

-- Add business_core_name column for faster exact matching
ALTER TABLE person ADD COLUMN IF NOT EXISTS business_core_name TEXT;

-- Populate business_core_name for existing businesses
UPDATE person
SET business_core_name = normalize_business_core(full_name)
WHERE flag = 'B';

-- Trigger to maintain business_core_name
CREATE OR REPLACE FUNCTION set_business_core_name()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
  IF NEW.flag = 'B' THEN
    NEW.business_core_name := normalize_business_core(NEW.full_name);
  ELSE
    NEW.business_core_name := NULL;
  END IF;
  RETURN NEW;
END;$$;

DROP TRIGGER IF EXISTS trg_person_business_core ON person;
CREATE TRIGGER trg_person_business_core
BEFORE INSERT OR UPDATE ON person
FOR EACH ROW EXECUTE FUNCTION set_business_core_name();

-- Index for business core name exact matching
CREATE INDEX IF NOT EXISTS ix_person_business_core 
ON person(business_core_name) 
WHERE flag = 'B' AND business_core_name IS NOT NULL;

-- Recommendations for search function enhancement:
-- 1. For flag='B' records, prioritize business_core_name exact match over token-level fuzzy
-- 2. Skip phonetic matching on business suffixes (is_business_suffix = TRUE)
-- 3. Increase weight for distinctive business tokens (brand names) vs generic suffixes
-- 4. Consider adding industry/category tags for better business search

COMMENT ON COLUMN person.business_core_name IS 'Normalized business name with common suffixes removed for core matching';
COMMENT ON COLUMN person_names.is_business_suffix IS 'Marks tokens that are common business suffixes (LLC, INC, etc.) to reduce phonetic noise';

-- Apply suffix marking to existing data
SELECT mark_business_suffixes();

