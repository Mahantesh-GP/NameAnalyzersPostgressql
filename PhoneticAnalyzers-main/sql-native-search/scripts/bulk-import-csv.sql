-- Bulk import CSV data directly to PostgreSQL
-- Usage: psql -h localhost -U postgres -d phonetic_native -f bulk-import-csv.sql

-- 1. Create temporary staging table (if needed)
CREATE TEMP TABLE person_staging (
    external_id TEXT,
    full_name TEXT,
    county TEXT,
    flag TEXT
);

-- 2. Import CSV data using COPY command
-- Replace '/path/to/your/data.csv' with actual file path
\COPY person_staging(external_id, full_name, county, flag) FROM 'C:/path/to/your/data.csv' WITH (FORMAT CSV, HEADER true, DELIMITER ',', ENCODING 'UTF8');

-- 3. Insert into person table and process phonetics
-- This uses the existing ingest_person function
DO $$
DECLARE
    rec RECORD;
    batch_size INT := 1000;
    batch_count INT := 0;
BEGIN
    FOR rec IN SELECT external_id, full_name, county, flag FROM person_staging
    LOOP
        PERFORM ingest_person(rec.external_id, rec.full_name, rec.county, rec.flag);
        
        batch_count := batch_count + 1;
        
        -- Commit every 1000 records for performance
        IF batch_count % batch_size = 0 THEN
            RAISE NOTICE 'Processed % records', batch_count;
            COMMIT;
        END IF;
    END LOOP;
    
    RAISE NOTICE 'Total processed: % records', batch_count;
END $$;

-- 4. Cleanup staging table
DROP TABLE IF EXISTS person_staging;

-- 5. Verify import
SELECT COUNT(*) as total_persons FROM person;
SELECT COUNT(*) as total_name_tokens FROM person_names;
