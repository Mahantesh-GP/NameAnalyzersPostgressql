-- Script to clear all data from PhoneticAnalyzers database tables
-- WARNING: This will delete ALL data. Use with caution!

-- Display warning
SELECT '
========================================
WARNING: DATABASE CLEAR OPERATION
========================================
This script will DELETE ALL DATA from:
- person table
- person_bm (Beider-Morse variants)
- person_names table
- name_aliases table
- name_alias_cache table
- nickname_maps table (optional)

Press Ctrl+C to cancel, or continue to execute.
========================================
' AS warning_message;

-- Disable foreign key checks temporarily (if needed)
-- SET session_replication_role = 'replica';

-- Clear all person-related data
TRUNCATE TABLE person_bm CASCADE;
TRUNCATE TABLE person CASCADE;
TRUNCATE TABLE person_names CASCADE;
TRUNCATE TABLE name_aliases CASCADE;
TRUNCATE TABLE name_alias_cache CASCADE;

-- Optional: Clear nickname_maps (comment out if you want to keep seed data)
-- TRUNCATE TABLE nickname_maps CASCADE;

-- Re-enable foreign key checks
-- SET session_replication_role = 'origin';

-- Verify tables are empty
SELECT 'Verification - All tables should show 0 records:' AS info;

SELECT 'person' AS table_name, COUNT(*) AS record_count FROM person
UNION ALL
SELECT 'person_bm', COUNT(*) FROM person_bm
UNION ALL
SELECT 'person_names', COUNT(*) FROM person_names
UNION ALL
SELECT 'name_aliases', COUNT(*) FROM name_aliases
UNION ALL
SELECT 'name_alias_cache', COUNT(*) FROM name_alias_cache
UNION ALL
SELECT 'nickname_maps', COUNT(*) FROM nickname_maps;

SELECT '
========================================
Database cleared successfully!
All person data has been removed.
Nickname maps have been preserved (unless manually cleared).

Next steps:
1. Run DataSeeder to repopulate nickname_maps if needed
2. Run bulk ingestion with nickname expansion enabled
========================================
' AS completion_message;
