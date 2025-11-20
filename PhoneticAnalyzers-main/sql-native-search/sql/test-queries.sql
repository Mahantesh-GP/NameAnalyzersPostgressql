-- Test queries to verify all 5 search strategies are working
-- Run these after loading test-all-strategies.sql

-- 1. Test Exact Match
-- Should return: John Smith (Exact match)
SELECT 
    full_name,
    match_type,
    similarity_score,
    ranking
FROM search_persons('John Smith', NULL, NULL, NULL, 50)
WHERE match_type = 'Exact'
ORDER BY ranking, similarity_score DESC;

-- 2. Test Nickname Expansion
-- Search "Bill" should match "William Anderson"
-- Search "Bob" should match "Robert Williams"
SELECT 
    full_name,
    match_type,
    similarity_score,
    ranking
FROM search_persons('Bill Anderson', NULL, NULL, NULL, 50)
WHERE match_type = 'NicknameExpansion'
ORDER BY ranking, similarity_score DESC;

SELECT 
    full_name,
    match_type,
    similarity_score,
    ranking
FROM search_persons('Bob Williams', NULL, NULL, NULL, 50)
WHERE match_type = 'NicknameExpansion'
ORDER BY ranking, similarity_score DESC;

-- 3. Test Fuzzy/Trigram Similarity
-- Should match typos and close spellings
SELECT 
    full_name,
    match_type,
    similarity_score,
    ranking
FROM search_persons('Wiliam Andersson', NULL, NULL, NULL, 50)
WHERE match_type = 'TrigramSimilarity'
ORDER BY ranking, similarity_score DESC
LIMIT 10;

-- 4. Test Phonetic Matches (Metaphone, Soundex, DoubleMetaphone)
-- "Jon Smyth" sounds like "John Smith"
-- "Jayne Dough" sounds like "Jane Doe"
SELECT 
    full_name,
    match_type,
    similarity_score,
    ranking
FROM search_persons('John Smith', NULL, NULL, NULL, 50)
WHERE match_type IN ('DoubleMetaphone', 'Metaphone', 'Soundex', 'PrimaryDoubleMetaphone', 'AlternateDoubleMetaphone', 'BeiderMorse')
ORDER BY ranking, similarity_score DESC
LIMIT 10;

-- 5. Test Business Core Matching
-- "Smith Associates" should match "Smith & Associates LLC"
SELECT 
    full_name,
    match_type,
    similarity_score,
    ranking
FROM search_persons('Smith Associates', NULL, NULL, 'B', 50)
ORDER BY ranking, similarity_score DESC
LIMIT 10;

-- 6. Comprehensive test showing all strategy types for one search
-- This should demonstrate all 5 strategies in action
SELECT 
    full_name,
    match_type,
    similarity_score,
    ranking,
    CASE 
        WHEN match_type = 'Exact' THEN '1-Exact'
        WHEN match_type = 'NicknameExpansion' THEN '2-Nickname'
        WHEN match_type = 'TrigramSimilarity' THEN '3-Fuzzy'
        WHEN match_type IN ('DoubleMetaphone', 'Metaphone', 'Soundex', 'PrimaryDoubleMetaphone', 'AlternateDoubleMetaphone', 'BeiderMorse') THEN '4-Phonetic'
        ELSE '5-Other'
    END as strategy_bucket
FROM search_persons('John Smith', NULL, NULL, NULL, 100)
ORDER BY 
    CASE strategy_bucket
        WHEN '1-Exact' THEN 1
        WHEN '2-Nickname' THEN 2
        WHEN '3-Fuzzy' THEN 3
        WHEN '4-Phonetic' THEN 4
        ELSE 5
    END,
    similarity_score DESC;

-- 7. Test Catherine/Katherine variations (multiple phonetic spellings)
SELECT 
    full_name,
    match_type,
    similarity_score,
    ranking
FROM search_persons('Catherine Smith', NULL, NULL, NULL, 50)
ORDER BY ranking, similarity_score DESC
LIMIT 15;

-- 8. Test with special characters
SELECT 
    full_name,
    match_type,
    similarity_score,
    ranking
FROM search_persons('O Brien Patrick', NULL, NULL, NULL, 50)
ORDER BY ranking, similarity_score DESC
LIMIT 10;

-- 9. Summary by strategy type
SELECT 
    CASE 
        WHEN match_type = 'Exact' THEN 'Exact'
        WHEN match_type = 'NicknameExpansion' THEN 'Nickname'
        WHEN match_type = 'TrigramSimilarity' THEN 'Fuzzy'
        WHEN match_type IN ('DoubleMetaphone', 'Metaphone', 'Soundex', 'PrimaryDoubleMetaphone', 'AlternateDoubleMetaphone', 'BeiderMorse') THEN 'Phonetic'
        ELSE 'Other'
    END as strategy,
    COUNT(*) as match_count,
    AVG(similarity_score) as avg_score
FROM search_persons('William Anderson', NULL, NULL, NULL, 100)
GROUP BY 
    CASE 
        WHEN match_type = 'Exact' THEN 'Exact'
        WHEN match_type = 'NicknameExpansion' THEN 'Nickname'
        WHEN match_type = 'TrigramSimilarity' THEN 'Fuzzy'
        WHEN match_type IN ('DoubleMetaphone', 'Metaphone', 'Soundex', 'PrimaryDoubleMetaphone', 'AlternateDoubleMetaphone', 'BeiderMorse') THEN 'Phonetic'
        ELSE 'Other'
    END
ORDER BY 
    CASE strategy
        WHEN 'Exact' THEN 1
        WHEN 'Nickname' THEN 2
        WHEN 'Fuzzy' THEN 3
        WHEN 'Phonetic' THEN 4
        ELSE 5
    END;
