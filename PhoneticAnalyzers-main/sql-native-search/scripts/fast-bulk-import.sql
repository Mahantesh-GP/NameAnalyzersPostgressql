-- FASTER bulk import - bypasses function calls for initial load
-- Usage: psql -h localhost -U postgres -d phonetic_native -f fast-bulk-import.sql

-- Step 1: Import CSV into person table directly
\COPY person(external_id, full_name, county, flag) FROM 'C:/path/to/your/data.csv' WITH (FORMAT CSV, HEADER true, DELIMITER ',', ENCODING 'UTF8');

-- Step 2: Batch process to add normalized names and phonetic codes
UPDATE person 
SET normalized_name = normalize_name(full_name)
WHERE normalized_name IS NULL;

-- Step 3: Bulk insert into person_names with phonetic codes
INSERT INTO person_names (person_id, name_token, soundex_code, metaphone_code, double_metaphone_code)
SELECT 
    p.person_id,
    t.token,
    soundex(t.token),
    metaphone(t.token, 4),
    dmetaphone(t.token)
FROM person p
CROSS JOIN LATERAL tokenize_name(p.normalized_name) AS t
WHERE NOT EXISTS (
    SELECT 1 FROM person_names pn 
    WHERE pn.person_id = p.person_id AND pn.name_token = t.token
);

-- Step 4: Create indexes for performance (if not already exists)
CREATE INDEX IF NOT EXISTS idx_person_normalized_name ON person(normalized_name);
CREATE INDEX IF NOT EXISTS idx_person_names_token ON person_names(name_token);
CREATE INDEX IF NOT EXISTS idx_person_names_soundex ON person_names(soundex_code);
CREATE INDEX IF NOT EXISTS idx_person_names_metaphone ON person_names(metaphone_code);
CREATE INDEX IF NOT EXISTS idx_person_names_dmetaphone ON person_names(double_metaphone_code);
CREATE INDEX IF NOT EXISTS idx_person_names_person_id ON person_names(person_id);

-- Enable trigram index for fuzzy search
CREATE INDEX IF NOT EXISTS idx_person_names_token_trgm ON person_names USING gin(name_token gin_trgm_ops);

-- Step 5: Verify import
SELECT 
    (SELECT COUNT(*) FROM person) as total_persons,
    (SELECT COUNT(*) FROM person_names) as total_name_tokens,
    (SELECT COUNT(DISTINCT person_id) FROM person_names) as persons_with_tokens;
