-- Staging + bulk ingest workflow

-- Staging table for raw CSV loads
CREATE TABLE IF NOT EXISTS staging_persons (
  external_id TEXT,
  full_name   TEXT,
  county      TEXT
);

-- Process staging: upsert into person and generate names + phonetics + nicknames
CREATE OR REPLACE FUNCTION process_staging_persons(batch_size INT DEFAULT 50000)
RETURNS VOID LANGUAGE plpgsql AS $$
DECLARE
  r RECORD;
  processed INT := 0;
BEGIN
  FOR r IN
    SELECT * FROM staging_persons
  LOOP
    PERFORM ingest_person(r.external_id, r.full_name, r.county);
    processed := processed + 1;

    IF processed % batch_size = 0 THEN
      PERFORM pg_sleep(0); -- yield
    END IF;
  END LOOP;

  -- Truncate after successful processing
  TRUNCATE staging_persons;
END;$$;

-- Helper note: Use psql COPY for fastest ingest
-- Example:
-- \copy staging_persons(external_id, full_name, county) FROM 'path\\to\\file.csv' WITH (FORMAT csv, HEADER true)
-- SELECT process_staging_persons();
