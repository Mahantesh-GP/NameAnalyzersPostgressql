-- Search function performing exact, trigram, and phonetic matches with progressive execution
-- OPTIMIZED: Early bailout strategy, pre-normalized queries, conditional execution based on result count

CREATE OR REPLACE FUNCTION search_persons(
  query_name TEXT,
  max_results INT DEFAULT 50,
  min_similarity DOUBLE PRECISION DEFAULT 0.3,
  county_filter TEXT DEFAULT NULL,
  flag_filter TEXT DEFAULT NULL,
  include_fuzzy BOOLEAN DEFAULT TRUE,
  include_nicknames BOOLEAN DEFAULT FALSE
) RETURNS TABLE (
  person_id BIGINT,
  full_name TEXT,
  match_type TEXT,
  similarity_score DOUBLE PRECISION,
  matched_field TEXT,
  matched_value TEXT,
  county TEXT,
  flag TEXT,
  match_metadata JSONB
) LANGUAGE plpgsql STABLE AS $$
DECLARE
  v_normalized_query TEXT;
  v_lower_query TEXT;
  v_phonetic_query TEXT;
  v_prefix_pattern TEXT;
  v_current_count INT;
BEGIN
  -- OPTIMIZATION: Pre-normalize query ONCE, reuse throughout
  v_normalized_query := normalize_name(query_name);
  v_lower_query := LOWER(query_name);
  v_phonetic_query := dmetaphone(v_normalized_query);
  v_prefix_pattern := v_normalized_query || '%';
  
  -- Validate input
  IF query_name IS NULL OR length(trim(query_name)) = 0 THEN
    RETURN;
  END IF;

  RETURN QUERY
WITH params AS (
  -- Use pre-normalized variables from DECLARE block
  SELECT v_normalized_query AS q, v_lower_query AS lq, v_phonetic_query AS pq, v_prefix_pattern AS prefix
),
-- STRATEGY 1: Fast exact match (uses btree index on normalized_name)
exact_matches AS (
  SELECT pr.person_id, pr.full_name, 'Exact'::text AS match_type,
         1.0::float8 AS similarity_score,
         'FullName'::text AS matched_field,
         pr.normalized_name AS matched_value,
         pr.county, pr.flag,
         jsonb_build_object(
           'explanation', 'Full name exact match',
           'displayText', 'Exact match on full normalized name'
         ) AS match_metadata,
         1 AS priority
  FROM params p, person pr
  WHERE pr.normalized_name = p.q
    AND (county_filter IS NULL OR pr.county = county_filter)
    AND (flag_filter IS NULL OR pr.flag = flag_filter)
  UNION ALL
  -- Business core name match
  SELECT pr.person_id, pr.full_name, 'Exact'::text AS match_type,
         0.95::float8 AS similarity_score,
         'BusinessCore'::text AS matched_field,
         pr.business_core_name AS matched_value,
         pr.county, pr.flag,
         jsonb_build_object(
           'explanation', 'Business core name match (suffix variants)',
           'displayText', 'Matched core business name (ignoring LLC/INC/etc)'
         ) AS match_metadata,
         1 AS priority
  FROM params p, person pr
  WHERE pr.flag = 'B' 
    AND pr.business_core_name IS NOT NULL
    AND (pr.business_core_name = p.q OR pr.business_core_name = normalize_business_core(p.q))
    AND (county_filter IS NULL OR pr.county = county_filter)
    AND (flag_filter IS NULL OR pr.flag = flag_filter)
  LIMIT max_results + 10  -- Hard cap to prevent excessive results
),
-- Check if we have enough results already
current_count AS (
  SELECT COUNT(*)::int AS total FROM exact_matches
),
-- STRATEGY 2: Phonetic match - ONLY if we don't have enough exact matches
-- Uses btree index on double_metaphone_code (faster than trigram)
phonetic_matches AS (
  SELECT 
    pn.person_id,
    pr.full_name,
    'Phonetic'::text AS match_type,
    0.59::float8 AS similarity_score,
    'PhoneticToken'::text AS matched_field,
    STRING_AGG(DISTINCT pn.name_token, ', ') AS matched_value,
    pr.county,
    pr.flag,
    jsonb_build_object(
      'explanation', 'Phonetic match using DoubleMetaphone algorithm',
      'displayText', 'Phonetic similarity match'
    ) AS match_metadata,
    3 AS priority
  FROM params p, person_names pn, person pr, current_count cc
  WHERE pn.double_metaphone_code = p.pq
    AND pr.person_id = pn.person_id
    AND cc.total < max_results  -- CRITICAL: Only run if not enough exact matches
    AND (county_filter IS NULL OR pr.county = county_filter)
    AND (flag_filter IS NULL OR pr.flag = flag_filter)
    AND NOT EXISTS (SELECT 1 FROM exact_matches e WHERE e.person_id = pn.person_id)
  GROUP BY pn.person_id, pr.full_name, pr.county, pr.flag
  LIMIT max_results * 2  -- Hard cap
),
-- Update current count
current_count2 AS (
  SELECT (SELECT COUNT(*) FROM exact_matches) + (SELECT COUNT(*) FROM phonetic_matches) AS total
),
-- STRATEGY 3: Trigram fuzzy match - ONLY if we STILL don't have enough results
-- This is the most expensive operation, so we run it last
token_matches AS (
  SELECT 
    pn.person_id,
    pn.name_token,
    similarity(pn.name_token, qt.token) AS sim_score,
    qt.token AS query_token
  FROM (
    SELECT t.token 
    FROM params p, tokenize_name(p.q) AS t 
    WHERE length(t.token) >= 2
  ) qt,
  person_names pn,
  current_count2 cc
  WHERE include_fuzzy = TRUE
    AND cc.total < max_results  -- CRITICAL: Only run if not enough results yet
    AND similarity(pn.name_token, qt.token) >= min_similarity
    AND NOT EXISTS (SELECT 1 FROM exact_matches e WHERE e.person_id = pn.person_id)
    AND NOT EXISTS (SELECT 1 FROM phonetic_matches ph WHERE ph.person_id = pn.person_id)
  LIMIT 2000  -- Hard cap to prevent explosion
),
trigram_matches AS (
  SELECT 
    tm.person_id,
    pr.full_name,
    'TrigramSimilarity'::text AS match_type,
    AVG(tm.sim_score)::float8 AS similarity_score,
    'TokenSet'::text AS matched_field,
    STRING_AGG(DISTINCT tm.name_token, ', ') AS matched_value,
    pr.county,
    pr.flag,
    jsonb_build_object(
      'explanation', 'Fuzzy trigram match',
      'avgSimilarity', ROUND((AVG(tm.sim_score) * 100)::numeric, 1),
      'displayText', 'Fuzzy match on name tokens'
    ) AS match_metadata,
    4 AS priority
  FROM token_matches tm, person pr
  WHERE pr.person_id = tm.person_id
    AND (county_filter IS NULL OR pr.county = county_filter)
    AND (flag_filter IS NULL OR pr.flag = flag_filter)
  GROUP BY tm.person_id, pr.full_name, pr.county, pr.flag
  HAVING AVG(tm.sim_score) >= min_similarity
  LIMIT max_results * 2
),
-- Combine all results
all_matches AS (
  SELECT * FROM exact_matches
  UNION ALL
  SELECT * FROM phonetic_matches WHERE include_fuzzy = TRUE
  UNION ALL
  SELECT * FROM trigram_matches WHERE include_fuzzy = TRUE
),
ranked AS (
  SELECT DISTINCT ON (am.person_id)
    am.person_id,
    am.full_name,
    am.match_type,
    am.similarity_score,
    am.matched_field,
    am.matched_value,
    am.county,
    am.flag,
    am.match_metadata
  FROM all_matches am
  ORDER BY am.person_id, am.priority ASC, am.similarity_score DESC
)
-- Final output: order by score and apply limit
SELECT 
  person_id,
  full_name,
  match_type,
  similarity_score,
  matched_field,
  matched_value,
  county,
  flag,
  match_metadata
FROM ranked
WHERE similarity_score >= min_similarity
ORDER BY similarity_score DESC, full_name ASC
LIMIT max_results;

END;
$$;
