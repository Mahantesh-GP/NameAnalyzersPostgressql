-- Test script to verify nickname expansion fix
-- This ensures "NicknameExpansion" only appears when nickname_maps has data

\echo '=== TEST 1: Check if nickname_maps is empty ==='
SELECT COUNT(*) as nickname_map_count FROM nickname_maps;

\echo ''
\echo '=== TEST 2: Search with empty nickname_maps (should show NO NicknameExpansion) ==='
SELECT 
    match_type, 
    COUNT(*) as count,
    ROUND(AVG(similarity_score)::numeric, 2) as avg_score
FROM search_persons('john', 50, 0.3, NULL, NULL, TRUE, TRUE)
GROUP BY match_type
ORDER BY match_type;

\echo ''
\echo '=== TEST 3: Search with nicknames DISABLED (should show NO NicknameExpansion) ==='
SELECT 
    match_type, 
    COUNT(*) as count,
    ROUND(AVG(similarity_score)::numeric, 2) as avg_score
FROM search_persons('john', 50, 0.3, NULL, NULL, TRUE, FALSE)
GROUP BY match_type
ORDER BY match_type;

\echo ''
\echo '=== TEST 4: Search with fuzzy DISABLED (should show only Exact + Nickname if data exists) ==='
SELECT 
    match_type, 
    COUNT(*) as count,
    ROUND(AVG(similarity_score)::numeric, 2) as avg_score
FROM search_persons('john', 50, 0.3, NULL, NULL, FALSE, TRUE)
GROUP BY match_type
ORDER BY match_type;

\echo ''
\echo '=== TEST 5: Full result details for "john" ==='
SELECT 
    full_name,
    match_type,
    ROUND(similarity_score::numeric, 2) as score,
    matched_field
FROM search_persons('john', 10, 0.3, NULL, NULL, TRUE, TRUE)
ORDER BY similarity_score DESC, full_name;

\echo ''
\echo '=== EXPECTED BEHAVIOR ==='
\echo 'With empty nickname_maps:'
\echo '  - Should see: Exact, TrigramSimilarity, Metaphone/DoubleMetaphone/Soundex'
\echo '  - Should NOT see: NicknameExpansion'
\echo ''
\echo 'With include_nicknames=FALSE:'
\echo '  - Should NOT see: NicknameExpansion (even if data exists)'
\echo ''
\echo 'With include_fuzzy=FALSE:'
\echo '  - Should see: Exact only (or + NicknameExpansion if data exists)'
\echo '  - Should NOT see: TrigramSimilarity, Metaphone, DoubleMetaphone, Soundex'
