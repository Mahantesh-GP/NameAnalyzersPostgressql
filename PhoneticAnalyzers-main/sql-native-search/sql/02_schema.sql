-- Core schema for SQL-native phonetic + nickname search

-- Persons (minimal columns; extend as needed)
CREATE TABLE IF NOT EXISTS person (
    person_id       BIGSERIAL PRIMARY KEY,
    external_id     TEXT UNIQUE,
    full_name       TEXT NOT NULL,
    normalized_name TEXT NOT NULL,
    county          TEXT,
    flag            CHAR(1) DEFAULT 'I',
    created_utc     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_utc     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Tokenized names + phonetic codes (one row per token and per nickname variant)
CREATE TABLE IF NOT EXISTS person_names (
    person_name_id          BIGSERIAL PRIMARY KEY,
    person_id               BIGINT NOT NULL REFERENCES person(person_id) ON DELETE CASCADE,
    name_token              TEXT NOT NULL,
    token_position          INT,
    soundex_code            TEXT,
    metaphone_code          TEXT,
    double_metaphone_code   TEXT,
    created_utc             TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Nickname dictionary (pre-populated offline, no LLM in hot path)
CREATE TABLE IF NOT EXISTS nickname_maps (
    id              BIGSERIAL PRIMARY KEY,
    canonical_name  TEXT NOT NULL,
    nickname        TEXT NOT NULL,
    is_bidirectional BOOLEAN NOT NULL DEFAULT TRUE,
    created_utc     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (canonical_name, nickname)
);

-- Helper: maintain updated_utc
CREATE OR REPLACE FUNCTION set_updated_utc()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
  NEW.updated_utc := NOW();
  RETURN NEW;
END;$$;

DROP TRIGGER IF EXISTS trg_person_updated_utc ON person;
CREATE TRIGGER trg_person_updated_utc
BEFORE UPDATE ON person
FOR EACH ROW EXECUTE FUNCTION set_updated_utc();
