-- Performance indexes

-- Trigram index for fuzzy matching on tokens
CREATE INDEX IF NOT EXISTS ix_person_names_token_trgm
ON person_names USING GIN (name_token gin_trgm_ops);

-- Phonetic code lookup indexes
CREATE INDEX IF NOT EXISTS ix_person_names_soundex
ON person_names (soundex_code);

CREATE INDEX IF NOT EXISTS ix_person_names_metaphone
ON person_names (metaphone_code);

CREATE INDEX IF NOT EXISTS ix_person_names_dmetaphone
ON person_names (double_metaphone_code);

-- Normalized name support
CREATE INDEX IF NOT EXISTS ix_person_normalized_trgm
ON person USING GIN (normalized_name gin_trgm_ops);

-- Optional: county filter patterns
CREATE INDEX IF NOT EXISTS ix_person_county ON person(county);
