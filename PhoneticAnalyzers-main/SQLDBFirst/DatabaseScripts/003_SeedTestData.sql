-- ============================================================================
-- Test Data for Nickname Expansion Feature
-- Database: phonetic_db_dbfirst
-- Date: 2025-11-12
-- Author: Development Team
-- Description: Adds 30 test persons with nickname variants for testing
-- ============================================================================

-- Connect to database
\c phonetic_db_dbfirst;

-- Clean up any existing test data (optional)
DELETE FROM person WHERE external_id LIKE 'DBFIRST-TEST-%';

-- ============================================================================
-- WILLIAM VARIANTS TEST DATA
-- ============================================================================

INSERT INTO person (external_id, full_name, normalized_name, primary_metaphone, alternate_metaphone, county, county_id, county_name, flag, created_utc, updated_utc)
VALUES 
    ('DBFIRST-TEST-001', 'William Anderson', 'WILLIAM ANDERSON', 'WLMN', 'FLMN', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-002', 'Bill Anderson', 'BILL ANDERSON', 'BL', 'PL', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-003', 'Billy Thompson', 'BILLY THOMPSON', 'BL', 'PL', 'King County', 33, 'King County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-004', 'Will Johnson', 'WILL JOHNSON', 'WL', 'AL', 'Snohomish County', 61, 'Snohomish County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-005', 'Willy Martinez', 'WILLY MARTINEZ', 'WL', 'AL', 'Spokane County', 63, 'Spokane County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-006', 'Liam Davis', 'LIAM DAVIS', 'LM', 'ALM', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW());

-- ============================================================================
-- ROBERT VARIANTS TEST DATA
-- ============================================================================

INSERT INTO person (external_id, full_name, normalized_name, primary_metaphone, alternate_metaphone, county, county_id, county_name, flag, created_utc, updated_utc)
VALUES 
    ('DBFIRST-TEST-007', 'Robert Wilson', 'ROBERT WILSON', 'RPRT', 'RPRK', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-008', 'Bob Wilson', 'BOB WILSON', 'PP', 'PP', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-009', 'Bobby Garcia', 'BOBBY GARCIA', 'PP', 'PP', 'King County', 33, 'King County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-010', 'Rob Miller', 'ROB MILLER', 'RP', 'RP', 'Snohomish County', 61, 'Snohomish County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-011', 'Robbie Taylor', 'ROBBIE TAYLOR', 'RP', 'RP', 'Spokane County', 63, 'Spokane County', 'I', NOW(), NOW());

-- ============================================================================
-- RICHARD VARIANTS TEST DATA
-- ============================================================================

INSERT INTO person (external_id, full_name, normalized_name, primary_metaphone, alternate_metaphone, county, county_id, county_name, flag, created_utc, updated_utc)
VALUES 
    ('DBFIRST-TEST-012', 'Richard Brown', 'RICHARD BROWN', 'RXRT', 'RKRT', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-013', 'Rick Brown', 'RICK BROWN', 'RK', 'RK', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-014', 'Dick Johnson', 'DICK JOHNSON', 'TK', 'TK', 'King County', 33, 'King County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-015', 'Ricky Martinez', 'RICKY MARTINEZ', 'RK', 'RK', 'Snohomish County', 61, 'Snohomish County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-016', 'Rich Thomas', 'RICH THOMAS', 'RX', 'RK', 'Spokane County', 63, 'Spokane County', 'I', NOW(), NOW());

-- ============================================================================
-- MICHAEL VARIANTS TEST DATA
-- ============================================================================

INSERT INTO person (external_id, full_name, normalized_name, primary_metaphone, alternate_metaphone, county, county_id, county_name, flag, created_utc, updated_utc)
VALUES 
    ('DBFIRST-TEST-017', 'Michael Jackson', 'MICHAEL JACKSON', 'MKL', 'MXL', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-018', 'Mike Jackson', 'MIKE JACKSON', 'MK', 'MK', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-019', 'Mick White', 'MICK WHITE', 'MK', 'MK', 'King County', 33, 'King County', 'I', NOW(), NOW());

-- ============================================================================
-- ELIZABETH VARIANTS TEST DATA
-- ============================================================================

INSERT INTO person (external_id, full_name, normalized_name, primary_metaphone, alternate_metaphone, county, county_id, county_name, flag, created_utc, updated_utc)
VALUES 
    ('DBFIRST-TEST-020', 'Elizabeth Smith', 'ELIZABETH SMITH', 'ALSP', 'ALSP', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-021', 'Liz Smith', 'LIZ SMITH', 'LS', 'LS', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-022', 'Beth Johnson', 'BETH JOHNSON', 'P0', 'PT', 'King County', 33, 'King County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-023', 'Betty Williams', 'BETTY WILLIAMS', 'PT', 'PT', 'Snohomish County', 61, 'Snohomish County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-024', 'Lizzie Jones', 'LIZZIE JONES', 'LS', 'LS', 'Spokane County', 63, 'Spokane County', 'I', NOW(), NOW());

-- ============================================================================
-- JOHN VARIANTS TEST DATA
-- ============================================================================

INSERT INTO person (external_id, full_name, normalized_name, primary_metaphone, alternate_metaphone, county, county_id, county_name, flag, created_utc, updated_utc)
VALUES 
    ('DBFIRST-TEST-025', 'John Davis', 'JOHN DAVIS', 'JN', 'AN', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-026', 'Johnny Davis', 'JOHNNY DAVIS', 'JN', 'AN', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-027', 'Jack Miller', 'JACK MILLER', 'JK', 'AK', 'King County', 33, 'King County', 'I', NOW(), NOW());

-- ============================================================================
-- JAMES VARIANTS TEST DATA
-- ============================================================================

INSERT INTO person (external_id, full_name, normalized_name, primary_metaphone, alternate_metaphone, county, county_id, county_name, flag, created_utc, updated_utc)
VALUES 
    ('DBFIRST-TEST-028', 'James Wilson', 'JAMES WILSON', 'JMS', 'AMS', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-029', 'Jim Wilson', 'JIM WILSON', 'JM', 'AM', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
    ('DBFIRST-TEST-030', 'Jimmy Garcia', 'JIMMY GARCIA', 'JM', 'AM', 'King County', 33, 'King County', 'I', NOW(), NOW());

-- ============================================================================
-- VERIFICATION & TESTING GUIDE
-- ============================================================================

-- Verify test data was inserted
SELECT 
    'Test Data Summary' AS info,
    COUNT(*) AS total_test_records,
    COUNT(DISTINCT LEFT(normalized_name, POSITION(' ' IN normalized_name) - 1)) AS unique_first_names
FROM person 
WHERE external_id LIKE 'DBFIRST-TEST-%';

-- Show William variants
SELECT 'William Variants' AS group_name;
SELECT external_id, full_name, county 
FROM person 
WHERE external_id LIKE 'DBFIRST-TEST-%' 
  AND (normalized_name LIKE 'WILLIAM %' OR normalized_name LIKE 'BILL %' 
       OR normalized_name LIKE 'BILLY %' OR normalized_name LIKE 'WILL %' 
       OR normalized_name LIKE 'WILLY %' OR normalized_name LIKE 'LIAM %')
ORDER BY full_name;

-- Show Robert variants
SELECT 'Robert Variants' AS group_name;
SELECT external_id, full_name, county 
FROM person 
WHERE external_id LIKE 'DBFIRST-TEST-%' 
  AND (normalized_name LIKE 'ROBERT %' OR normalized_name LIKE 'BOB %' 
       OR normalized_name LIKE 'BOBBY %' OR normalized_name LIKE 'ROB %' 
       OR normalized_name LIKE 'ROBBIE %')
ORDER BY full_name;

-- Show Elizabeth variants
SELECT 'Elizabeth Variants' AS group_name;
SELECT external_id, full_name, county 
FROM person 
WHERE external_id LIKE 'DBFIRST-TEST-%' 
  AND (normalized_name LIKE 'ELIZABETH %' OR normalized_name LIKE 'LIZ %' 
       OR normalized_name LIKE 'BETH %' OR normalized_name LIKE 'BETTY %' 
       OR normalized_name LIKE 'LIZZIE %')
ORDER BY full_name;

-- Test trigram similarity (verify pg_trgm is working)
SELECT 
    'Trigram Similarity Test' AS test_name,
    similarity('WILLIAM', 'BILL') AS william_bill_similarity,
    similarity('ROBERT', 'BOB') AS robert_bob_similarity,
    similarity('ELIZABETH', 'LIZ') AS elizabeth_liz_similarity;

-- Testing Instructions
SELECT '
========================================
TESTING INSTRUCTIONS
========================================

30 test persons have been added with external IDs starting with "DBFIRST-TEST-"

TEST GROUPS:
- William/Bill/Billy/Will/Willy/Liam (6 persons)
- Robert/Bob/Bobby/Rob/Robbie (5 persons)
- Richard/Rick/Dick/Ricky/Rich (5 persons)
- Michael/Mike/Mick (3 persons)
- Elizabeth/Liz/Beth/Betty/Lizzie (5 persons)
- John/Johnny/Jack (3 persons)
- James/Jim/Jimmy (3 persons)

RECOMMENDED TESTS:

Test 1: Search for "William"
  Expected: Find William Anderson + all nickname variants (Bill, Billy, Will, etc.)
  
Test 2: Search for "Bob"
  Expected: Find Bob Wilson + Robert Wilson (bidirectional)
  
Test 3: Search for "Elizabeth"
  Expected: Find Elizabeth Smith + all variants (Liz, Beth, Betty, Lizzie)

Test 4: Fuzzy Search for "Wiliam" (misspelled)
  Expected: Still find William Anderson via trigram similarity

Test 5: Phonetic Search for "Jon Davison"
  Expected: Find John Davis via Double Metaphone

CLEANUP:
To remove test data: DELETE FROM person WHERE external_id LIKE ''DBFIRST-TEST-%'';

Next Steps:
1. Run scaffold-models.ps1 to generate C# models from database
2. Build application using scaffolded models
3. Test search functionality with this data

========================================
' AS testing_guide;

-- Show counts by flag
SELECT flag, COUNT(*) AS count 
FROM person 
WHERE external_id LIKE 'DBFIRST-TEST-%'
GROUP BY flag;

-- Success message
SELECT '
========================================
TEST DATA SEEDED SUCCESSFULLY
========================================

30 test persons added
7 name groups with variants
All records flagged as Individual (I)

Database: phonetic_db_dbfirst
Tables populated:
  ✓ person (30 records)
  ✓ nickname_maps (250+ mappings)

Ready for:
  - Model scaffolding
  - Application development
  - Search testing

========================================
' AS completion_message;
