-- Test Data for Nickname Expansion Feature
-- This script adds persons with nickname variants to verify nickname matching works correctly
-- Run this in your PostgreSQL database after the main DataSeeder has run

-- Clean up any existing test data (optional)
DELETE FROM person WHERE external_id LIKE 'NICKNAME-TEST-%';

-- Insert people with William and its nicknames
INSERT INTO person (external_id, full_name, normalized_name, county, county_id, county_name, flag, created_utc, updated_utc)
VALUES 
  ('NICKNAME-TEST-001', 'William Anderson', 'WILLIAM ANDERSON', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-002', 'Bill Anderson', 'BILL ANDERSON', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-003', 'Billy Thompson', 'BILLY THOMPSON', 'King County', 33, 'King County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-004', 'Will Johnson', 'WILL JOHNSON', 'Snohomish County', 61, 'Snohomish County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-005', 'Willy Martinez', 'WILLY MARTINEZ', 'Spokane County', 63, 'Spokane County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-006', 'Liam Davis', 'LIAM DAVIS', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW());

-- Insert people with Robert and its nicknames
INSERT INTO person (external_id, full_name, normalized_name, county, county_id, county_name, flag, created_utc, updated_utc)
VALUES 
  ('NICKNAME-TEST-007', 'Robert Wilson', 'ROBERT WILSON', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-008', 'Bob Wilson', 'BOB WILSON', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-009', 'Bobby Garcia', 'BOBBY GARCIA', 'King County', 33, 'King County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-010', 'Rob Miller', 'ROB MILLER', 'Snohomish County', 61, 'Snohomish County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-011', 'Robbie Taylor', 'ROBBIE TAYLOR', 'Spokane County', 63, 'Spokane County', 'I', NOW(), NOW());

-- Insert people with Richard and its nicknames
INSERT INTO person (external_id, full_name, normalized_name, county, county_id, county_name, flag, created_utc, updated_utc)
VALUES 
  ('NICKNAME-TEST-012', 'Richard Brown', 'RICHARD BROWN', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-013', 'Rick Brown', 'RICK BROWN', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-014', 'Dick Johnson', 'DICK JOHNSON', 'King County', 33, 'King County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-015', 'Ricky Martinez', 'RICKY MARTINEZ', 'Snohomish County', 61, 'Snohomish County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-016', 'Rich Thomas', 'RICH THOMAS', 'Spokane County', 63, 'Spokane County', 'I', NOW(), NOW());

-- Insert people with Michael and its nicknames
INSERT INTO person (external_id, full_name, normalized_name, county, county_id, county_name, flag, created_utc, updated_utc)
VALUES 
  ('NICKNAME-TEST-017', 'Michael Jackson', 'MICHAEL JACKSON', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-018', 'Mike Jackson', 'MIKE JACKSON', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-019', 'Mick White', 'MICK WHITE', 'King County', 33, 'King County', 'I', NOW(), NOW());

-- Insert people with Elizabeth and its nicknames
INSERT INTO person (external_id, full_name, normalized_name, county, county_id, county_name, flag, created_utc, updated_utc)
VALUES 
  ('NICKNAME-TEST-020', 'Elizabeth Smith', 'ELIZABETH SMITH', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-021', 'Liz Smith', 'LIZ SMITH', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-022', 'Beth Johnson', 'BETH JOHNSON', 'King County', 33, 'King County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-023', 'Betty Williams', 'BETTY WILLIAMS', 'Snohomish County', 61, 'Snohomish County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-024', 'Lizzie Jones', 'LIZZIE JONES', 'Spokane County', 63, 'Spokane County', 'I', NOW(), NOW());

-- Insert people with John and its nicknames
INSERT INTO person (external_id, full_name, normalized_name, county, county_id, county_name, flag, created_utc, updated_utc)
VALUES 
  ('NICKNAME-TEST-025', 'John Davis', 'JOHN DAVIS', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-026', 'Johnny Davis', 'JOHNNY DAVIS', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-027', 'Jack Miller', 'JACK MILLER', 'King County', 33, 'King County', 'I', NOW(), NOW());

-- Insert people with James and its nicknames
INSERT INTO person (external_id, full_name, normalized_name, county, county_id, county_name, flag, created_utc, updated_utc)
VALUES 
  ('NICKNAME-TEST-028', 'James Wilson', 'JAMES WILSON', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-029', 'Jim Wilson', 'JIM WILSON', 'Pierce County', 53, 'Pierce County', 'I', NOW(), NOW()),
  ('NICKNAME-TEST-030', 'Jimmy Garcia', 'JIMMY GARCIA', 'King County', 33, 'King County', 'I', NOW(), NOW());

-- Verify the data was inserted
SELECT 'Test data summary:' AS info;
SELECT 
    COUNT(*) as total_test_records,
    COUNT(DISTINCT LEFT(normalized_name, POSITION(' ' IN normalized_name) - 1)) as unique_first_names
FROM person 
WHERE external_id LIKE 'NICKNAME-TEST-%';

-- Show sample records grouped by base name
SELECT 'William variants:' AS info;
SELECT external_id, full_name, county FROM person 
WHERE external_id LIKE 'NICKNAME-TEST-%' 
  AND (normalized_name LIKE 'WILLIAM %' OR normalized_name LIKE 'BILL %' 
       OR normalized_name LIKE 'BILLY %' OR normalized_name LIKE 'WILL %' 
       OR normalized_name LIKE 'WILLY %' OR normalized_name LIKE 'LIAM %')
ORDER BY full_name;

SELECT 'Robert variants:' AS info;
SELECT external_id, full_name, county FROM person 
WHERE external_id LIKE 'NICKNAME-TEST-%' 
  AND (normalized_name LIKE 'ROBERT %' OR normalized_name LIKE 'BOB %' 
       OR normalized_name LIKE 'BOBBY %' OR normalized_name LIKE 'ROB %' 
       OR normalized_name LIKE 'ROBBIE %')
ORDER BY full_name;

-- Instructions for testing
SELECT '
========================================
NICKNAME EXPANSION TESTING INSTRUCTIONS
========================================

The test data has been loaded. Now test nickname expansion:

TEST 1: Search for "William"
Expected Results (with Nicknames ON):
  - William Anderson (Exact match, score 1.0)
  - Bill Anderson (NicknameExpansion, score 0.93)
  - Billy Thompson (NicknameExpansion, score 0.93)
  - Will Johnson (NicknameExpansion, score 0.93)
  - Willy Martinez (NicknameExpansion, score 0.93)
  - Liam Davis (NicknameExpansion, score 0.93)

TEST 2: Search for "Bob"
Expected Results (with Nicknames ON):
  - Bob Wilson (Exact match, score 1.0)
  - Robert Wilson (NicknameExpansion, score 0.93) [reverse lookup]
  - Bobby Garcia (may appear via phonetic match)

TEST 3: Search for "Elizabeth"
Expected Results (with Nicknames ON):
  - Elizabeth Smith (Exact match, score 1.0)
  - Liz Smith (NicknameExpansion, score 0.93)
  - Beth Johnson (NicknameExpansion, score 0.93)
  - Betty Williams (NicknameExpansion, score 0.93)
  - Lizzie Jones (NicknameExpansion, score 0.93)

Settings to use:
  - Min Similarity: 0.3
  - Trigram: ON
  - Nicknames: ON
  - Details: ON (to see match types)
  - Max Results: 50

========================================
' AS testing_guide;
