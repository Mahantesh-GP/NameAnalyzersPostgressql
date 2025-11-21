-- Test search for 'john' with min similarity 95% to see phonetic scores
-- This should show NO phonetic matches since they max out at 59%

\echo '==== DIAGNOSTIC: Check if businesses with JOHN in name appear multiple times ===='
\echo ''
WITH all_results AS (
    SELECT * FROM search_persons('john', 50, 0.30, NULL, NULL, TRUE, TRUE)
)
SELECT 
    person_id,
    full_name,
    match_type,
    similarity_score,
    ROUND((similarity_score * 100)::numeric, 0) as score_pct,
    COUNT(*) OVER (PARTITION BY person_id) as match_count
FROM all_results
WHERE full_name ILIKE '%JOHN%BATCHELOR%' OR full_name ILIKE '%JUAN%JOHN%'
ORDER BY person_id, similarity_score DESC;

\echo ''
\echo '==== Test 1: Search john with 95% threshold ===='
\echo 'Expected: Only Exact + high Nickname matches (no phonetic)'
\echo ''
SELECT 
    person_id,
    full_name,
    match_type,
    similarity_score,
    ROUND((similarity_score * 100)::numeric, 0) as score_pct,
    matched_value
FROM search_persons('john', 10, 0.95, NULL, NULL, TRUE, TRUE)
ORDER BY similarity_score DESC;

\echo ''
\echo '==== Test 2: Search john with 30% threshold ===='
\echo 'Expected: All match types including phonetic (53-59%)'
\echo ''
SELECT 
    full_name,
    match_type,
    similarity_score,
    ROUND((similarity_score * 100)::numeric, 0) as score_pct
FROM search_persons('john', 20, 0.30, NULL, NULL, TRUE, TRUE)
ORDER BY similarity_score DESC;

\echo ''
\echo '==== Test 3: Check if phonetic matches are actually being computed ===='
\echo 'Direct query to phonetic_token_matches CTE logic'
\echo ''
-- Simulate what the phonetic matching does
SELECT DISTINCT
    p.full_name,
    'DoubleMetaphone' as algorithm,
    pn.name_token as matched_token,
    pn.double_metaphone_code as phonetic_code,
    dmetaphone('john') as query_code
FROM person_names pn
JOIN person p ON p.person_id = pn.person_id
WHERE pn.double_metaphone_code = dmetaphone('john')
LIMIT 5;

\echo ''
\echo '==== Test 4: Check specific persons that appear in UI ===='
SELECT 
    p.person_id,
    p.full_name,
    pn.name_token,
    pn.soundex_code,
    pn.metaphone_code,
    pn.double_metaphone_code,
    soundex('john') as john_soundex,
    metaphone('john', 4) as john_metaphone,
    dmetaphone('john') as john_dmetaphone
FROM person p
JOIN person_names pn ON pn.person_id = p.person_id
WHERE p.full_name ILIKE '%JAIME%' OR p.full_name ILIKE '%JIMMY%'
ORDER BY p.full_name;
