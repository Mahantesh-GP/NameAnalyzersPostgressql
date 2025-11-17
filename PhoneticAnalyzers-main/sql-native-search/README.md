# SQL-Native Phonetic + Nickname Search (PostgreSQL)

This project implements a high-performance, PostgreSQL-native approach for name search at very large scale (millions to billions of rows) without LLMs or app-layer phonetic code generation in the hot path.

Core principles:
- Generate and persist phonetic codes in SQL (fuzzystrmatch)
- Use pg_trgm for fuzzy/tolerant matching
- Expand nicknames via a mapping table (no LLM in hot path)
- Perform search and ranking entirely in SQL
- Support bulk ingest via COPY + staging for maximum throughput

## Contents
- `sql/01_extensions.sql` — enables `pg_trgm`, `fuzzystrmatch`, `unaccent`
- `sql/02_schema.sql` — tables: `person`, `person_names`, `nickname_maps`
- `sql/03_indexes.sql` — GIN/BTREE indexes for speed
- `sql/04_functions.sql` — normalization, tokenization, ingest + phonetic gen
- `sql/05_search.sql` — `search_persons()` set-returning function with ranking
- `sql/06_staging.sql` — staging + bulk ingest workflow using COPY
- `sql/seed/seed_nicknames.sql` — starter nickname mappings with upsert
- `scripts/deploy.ps1` — applies all SQL scripts with `psql`
- `scripts/run-all.ps1` — convenience wrapper to run extensions→schema→indexes→functions→search→staging→seed in order

## Prerequisites
- PostgreSQL 14+ (tested with 17)
- `psql` on PATH
- Database role with privileges to create extensions, tables, functions

## Quick Start (Windows PowerShell)

Set connection environment variables or edit the scripts to hardcode a connection string.

```powershell
# Example environment variables
$env:PGHOST = "localhost"
$env:PGPORT = "5432"
$env:PGDATABASE = "phonetic_native"
$env:PGUSER = "postgres"
$env:PGPASSWORD = "postgres"

# Deploy all
./scripts/run-all.ps1
```

## Bulk Ingest Workflow
1. COPY CSV into staging table `staging_persons`
2. Call `process_staging_persons()` to:
   - upsert into `person`
   - tokenize names into `person_names`
   - generate phonetic codes (soundex, metaphone, double metaphone)
   - expand nicknames via `nickname_maps` and generate their phonetics

## Search API (SQL)
Use `SELECT * FROM search_persons(query_name := 'Johnny Davis', max_results := 50, min_similarity := 0.3);`
The function returns `person_id, full_name, match_type, similarity_score, matched_field, matched_value` and is ranked by best match first.

## Performance Notes
- Ensure `pg_trgm` and `fuzzystrmatch` are enabled
- Maintain `GIN` trigram index on `person_names.name_token`
- Keep phonetic columns persisted and indexed for exact code lookups
- Prefer bulk loads via `COPY` + staging and batch transaction sizes (5k–50k rows)
- Consider partitioning `person` by a business key (e.g., county/tenant/date) at very large scale

## License
Internal use within this repository; no external license headers added.
