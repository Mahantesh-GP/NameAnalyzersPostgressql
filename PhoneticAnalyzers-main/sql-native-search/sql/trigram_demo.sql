-- trigram_demo.sql
-- Demonstrates pg_trgm behavior: EXPLAIN ANALYZE before and after creating a trigram index.
-- NOTE: `CREATE INDEX CONCURRENTLY` cannot run inside a transaction block. Run this in psql (or a client that does not wrap in a transaction).

-- 1) Make sure extension exists
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- 2) Create demo table and populate it with a few real names + many filler rows
CREATE TABLE IF NOT EXISTS trigram_demo (
  id serial PRIMARY KEY,
  name text
);

TRUNCATE TABLE trigram_demo;

-- insert some known names we want to match
INSERT INTO trigram_demo (name)
SELECT name FROM (VALUES ('John'), ('Jon'), ('Johnny'), ('Jonathan'), ('Joan'), ('Jean'), ('Johan')) v(name);

-- add many filler rows to simulate a large table (adjust count as needed)
INSERT INTO trigram_demo (name)
SELECT md5(i::text) FROM generate_series(1,200000) s(i);

-- show the trigrams Postgres builds for the literal 'John'
SELECT show_trgm('John') AS trigrams_for_John;

-- 3) Run an EXPLAIN ANALYZE before creating index (planner may choose seq scan)
EXPLAIN ANALYZE
SELECT id, name, similarity(name, 'john') AS sim
FROM trigram_demo
WHERE name % 'john'
ORDER BY sim DESC
LIMIT 10;

-- 4) Create a GIN trigram index (CONCURRENTLY recommended for production)
-- If running from a client that wraps statements in a transaction, run this single statement in psql.
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_trigram_demo_name_trgm
  ON trigram_demo USING gin (name gin_trgm_ops);

-- 5) Run same query again and compare plans/timings
EXPLAIN ANALYZE
SELECT id, name, similarity(name, 'john') AS sim
FROM trigram_demo
WHERE name % 'john'
ORDER BY sim DESC
LIMIT 10;

-- Cleanup suggestion (optional):
-- DROP INDEX IF EXISTS idx_trigram_demo_name_trgm;
-- TRUNCATE TABLE trigram_demo;
-- DROP TABLE IF EXISTS trigram_demo;

-- End of demo
