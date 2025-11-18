-- Search function performing exact, trigram, and phonetic matches with nickname coverage
-- Fixed: Skip short tokens, aggregate match quality per person, prioritize query name matches

CREATE OR REPLACE FUNCTION search_persons(
  query_name TEXT,
  max_results INT DEFAULT 50,
  min_similarity DOUBLE PRECISION DEFAULT 0.3,
  county_filter TEXT DEFAULT NULL,
  flag_filter TEXT DEFAULT NULL,
  include_fuzzy BOOLEAN DEFAULT TRUE,
  include_nicknames BOOLEAN DEFAULT TRUE
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
) LANGUAGE sql STABLE AS $$
WITH params AS (
  SELECT normalize_name(query_name) AS q
), qtokens AS (
  -- Filter out very short tokens (< 2 chars) to avoid noise like "C", "A", etc.
  SELECT t.token, t.token_position
  FROM params p, tokenize_name(p.q) AS t
  WHERE length(t.token) >= 2
), qtokens_weighted AS (
  -- Assign weights: common suffixes/words get lower priority
  SELECT 
    token,
    token_position,
    CASE 
      -- Common company/legal suffixes (low weight)
      WHEN token IN ('LIMITED', 'LTD', 'PRIVATE', 'PVT', 'LLC', 'INC', 'CORP', 'CO', 
                     'CORPORATION', 'COMPANY', 'SOLUTIONS', 'SERVICES', 'GROUP', 
                     'ENTERPRISES', 'INDUSTRIES', 'INTERNATIONAL', 'GLOBAL') THEN 0.2
      -- Common connector words (very low weight)
      WHEN token IN ('AND', 'THE', 'OF', 'FOR', 'WITH', 'BY') THEN 0.1
      -- Actual name tokens (full weight)
      ELSE 1.0
    END AS token_weight
  FROM qtokens
), exact_matches AS (
  -- Full name exact match (individuals and businesses)
  SELECT pr.person_id, pr.full_name, 'Exact'::text AS match_type,
         1.0::float8 AS similarity_score,
         'FullName'::text AS matched_field,
         pr.normalized_name AS matched_value,
         pr.county,
         pr.flag,
         jsonb_build_object(
           'explanation', 'Full name exact match',
           'displayText', 'Exact match on full normalized name'
         ) AS match_metadata
  FROM params p
  JOIN person pr ON pr.normalized_name = p.q
  UNION ALL
  -- Business core name match (e.g., "ABC SOLUTIONS" matches "ABC SOLUTIONS LLC")
  SELECT pr.person_id, pr.full_name, 'Exact'::text AS match_type,
         0.95::float8 AS similarity_score,
         'BusinessCore'::text AS matched_field,
         pr.business_core_name AS matched_value,
         pr.county,
         pr.flag,
         jsonb_build_object(
           'explanation', 'Business core name match (suffix variants)',
           'displayText', 'Matched core business name (ignoring LLC/INC/etc)'
         ) AS match_metadata
  FROM params p
  JOIN person pr ON pr.flag = 'B' 
    AND pr.business_core_name IS NOT NULL
    AND (pr.business_core_name = p.q OR pr.business_core_name = normalize_business_core(p.q))
), nickname_matches AS (
  -- Matches via nickname expansion (e.g., bob → robert) - INDIVIDUALS ONLY
  SELECT pn.person_id, pr.full_name, 'Exact'::text AS match_type,
         1.0::float8 AS similarity_score,
         'NicknameExpansion'::text AS matched_field,
         pn.original_token AS matched_value,
         pr.county,
         pr.flag,
         jsonb_build_object(
           'explanation', 'Nickname expansion',
           'searchedName', qt.token,
           'matchedName', pn.original_token,
           'displayText', qt.token || ' → ' || pn.original_token || ' (nickname)'
         ) AS match_metadata
  FROM qtokens_weighted qt
  JOIN person_names pn ON pn.name_token = qt.token AND pn.is_nickname = TRUE
  JOIN person pr ON pr.person_id = pn.person_id AND pr.flag <> 'B'  -- Exclude businesses
), token_matches AS (
  -- Collect all token-level matches with their similarity scores and weights
  SELECT 
    pn.person_id,
    qt.token AS query_token,
    qt.token_weight,
    pn.name_token AS matched_token,
    similarity(pn.name_token, qt.token) AS sim_score
  FROM qtokens_weighted qt
  JOIN person_names pn 
    ON (
         -- Primary: trigram similarity meets threshold
         similarity(pn.name_token, qt.token) >= min_similarity
         -- Fallback: allow single-edit typos for longer tokens (e.g., SMIOTH -> SMITH)
         OR (
              length(qt.token) >= 5 
              AND levenshtein_less_equal(pn.name_token, qt.token, 1) <= 1
            )
       )
), token_best_matches AS (
  -- Choose the best match per person and query token with kind priority: exact > lev1 > fuzzy
  SELECT DISTINCT ON (tm.person_id, tm.query_token)
    tm.person_id,
    tm.query_token,
    tm.token_weight,
    tm.matched_token,
    tm.sim_score,
    (tm.matched_token = tm.query_token) AS is_exact,
    (
      length(tm.matched_token) > length(tm.query_token)
      AND left(tm.matched_token, length(tm.query_token)) = tm.query_token
      AND (length(tm.matched_token) - length(tm.query_token)) >= 2
    ) AS is_superstring,
    CASE 
      WHEN tm.matched_token = tm.query_token THEN 1
      WHEN (length(tm.query_token) >= 5 AND levenshtein_less_equal(tm.matched_token, tm.query_token, 1) <= 1) THEN 2
      ELSE 3
    END AS kind_rank
  FROM token_matches tm
  ORDER BY tm.person_id, tm.query_token, kind_rank, tm.sim_score DESC
), person_token_stats AS (
  -- Aggregate token-level stats per person for rule-based scoring
  SELECT 
    tbm.person_id,
    SUM(tbm.token_weight) AS matched_weight,
    SUM(tbm.token_weight) FILTER (WHERE tbm.is_exact) AS exact_weight,
    SUM(tbm.token_weight) FILTER (WHERE NOT tbm.is_exact) AS non_exact_weight,
    AVG(tbm.sim_score) AS avg_sim,
    BOOL_OR(tbm.is_superstring) AS has_superstring,
    COUNT(*) AS matched_token_count,
    (SELECT SUM(token_weight) FROM qtokens_weighted) AS total_query_weight,
    (SELECT COUNT(*) FROM qtokens_weighted) AS qtoken_count
  FROM token_best_matches tbm
  GROUP BY tbm.person_id
), rule_based_matches AS (
  -- Apply clear classes and scores so only full name exact reaches 1.0
  SELECT 
    pts.person_id,
    pr.full_name,
    'TrigramSimilarity'::text AS match_type,
    LEAST(
      CASE 
        -- All tokens exact and no extra tokens
        WHEN pts.exact_weight = pts.total_query_weight AND ptc.person_token_count = pts.qtoken_count THEN 0.95
        -- All tokens exact but candidate has extra tokens: penalize extras proportionally
        WHEN pts.exact_weight = pts.total_query_weight AND ptc.person_token_count > pts.qtoken_count THEN 
          0.90 * (1 - 0.5 * ((ptc.person_token_count - pts.qtoken_count)::float / NULLIF(ptc.person_token_count::float, 0)))
        -- High coverage fuzzy (>=80% of query weight matched via exact/fuzzy)
        WHEN (pts.matched_weight / NULLIF(pts.total_query_weight, 0)) >= 0.8 THEN 
          LEAST(0.75 + 0.14 * (pts.matched_weight / NULLIF(pts.total_query_weight, 0)) * COALESCE(pts.avg_sim, 0.8), 0.89)
        -- Partial exact or low coverage fuzzy
        WHEN pts.matched_weight > 0 THEN 
          LEAST(0.60 + 0.14 * (pts.matched_weight / NULLIF(pts.total_query_weight, 0)) * COALESCE(pts.avg_sim, 0.7), 0.74)
        ELSE 0.0
      END 
      * (CASE WHEN pts.has_superstring THEN 0.85 ELSE 1 END),
      1.0
    ) AS similarity_score,
    'TokenSet'::text AS matched_field,
    STRING_AGG(DISTINCT tbm.matched_token, ', ' ORDER BY tbm.matched_token) AS matched_value,
    pr.county,
    pr.flag,
    jsonb_build_object(
      'explanation', 'Rule-based fuzzy match with coverage and exactness penalties',
      'classification', CASE 
        WHEN pts.exact_weight = pts.total_query_weight AND ptc.person_token_count = pts.qtoken_count THEN 'AllTokensExact'
        WHEN pts.exact_weight = pts.total_query_weight AND ptc.person_token_count > pts.qtoken_count THEN 'AllTokensExactPlusExtra'
        WHEN (pts.matched_weight / NULLIF(pts.total_query_weight, 0)) >= 0.8 THEN 'HighCoverageFuzzy'
        WHEN pts.matched_weight > 0 THEN 'PartialExact'
        ELSE 'Unclassified'
      END,
      'coveragePct', ROUND(((pts.matched_weight / NULLIF(pts.total_query_weight, 0)) * 100)::numeric, 1),
      'exactTokenPct', ROUND(((COALESCE(pts.exact_weight,0) / NULLIF(pts.total_query_weight, 0)) * 100)::numeric, 1),
      'avgSimilarity', ROUND((COALESCE(pts.avg_sim,0) * 100)::numeric, 1),
      'queryTokenCount', pts.qtoken_count,
      'personTokenCount', ptc.person_token_count,
      'hasSuperstringPenalty', pts.has_superstring,
      'displayText', 'Coverage ' || ROUND(((pts.matched_weight / NULLIF(pts.total_query_weight, 0)) * 100)::numeric, 1) || '%; ' ||
                     'Exact ' || ROUND(((COALESCE(pts.exact_weight,0) / NULLIF(pts.total_query_weight, 0)) * 100)::numeric, 1) || '%; ' ||
                     CASE WHEN pts.has_superstring THEN 'Superstring penalty applied' ELSE 'No superstring penalty' END
    ) AS match_metadata
  FROM person_token_stats pts
  JOIN person pr ON pr.person_id = pts.person_id
  LEFT JOIN token_best_matches tbm ON tbm.person_id = pts.person_id
  CROSS JOIN params p
  CROSS JOIN LATERAL (SELECT COUNT(*) AS person_token_count FROM tokenize_name(pr.normalized_name)) ptc
  -- Exclude exact full-name and business-core exact; they are provided by exact_matches already
  WHERE pr.normalized_name <> p.q
    AND NOT (
      pr.flag = 'B' AND pr.business_core_name IS NOT NULL AND 
      (pr.business_core_name = p.q OR pr.business_core_name = normalize_business_core(p.q))
    )
  GROUP BY pts.person_id, pr.full_name, pr.county, pr.flag, pts.matched_weight, pts.total_query_weight, pts.exact_weight, pts.avg_sim, pts.qtoken_count, pts.has_superstring, ptc.person_token_count
), phonetic_token_matches AS (
  -- Phonetic matching on individual tokens (not full query) for better precision
  -- Include token weights
  SELECT 
    pn.person_id,
    qt.token AS query_token,
    qt.token_weight,
    pn.name_token AS matched_token,
    'DoubleMetaphone' AS phonetic_type
  FROM qtokens_weighted qt
  JOIN person_names pn ON pn.double_metaphone_code = dmetaphone(qt.token)
  UNION ALL
  SELECT 
    pn.person_id,
    qt.token AS query_token,
    qt.token_weight,
    pn.name_token AS matched_token,
    'Metaphone' AS phonetic_type
  FROM qtokens_weighted qt
  JOIN person_names pn ON pn.metaphone_code = metaphone(qt.token, 4)
  UNION ALL
  SELECT 
    pn.person_id,
    qt.token AS query_token,
    qt.token_weight,
    pn.name_token AS matched_token,
    'Soundex' AS phonetic_type
  FROM qtokens_weighted qt
  JOIN person_names pn ON pn.soundex_code = soundex(qt.token)
), phonetic_matches AS (
  SELECT 
    ptm.person_id,
    pr.full_name,
    ptm.phonetic_type AS match_type,
    LEAST(
      CASE ptm.phonetic_type
        WHEN 'DoubleMetaphone' THEN 0.75
        WHEN 'Metaphone' THEN 0.70
        WHEN 'Soundex' THEN 0.65
      END * (SUM(ptm.token_weight) / NULLIF((SELECT SUM(token_weight) FROM qtokens_weighted), 0)),
      1.0
    ) AS similarity_score,
    ptm.phonetic_type AS matched_field,
    STRING_AGG(DISTINCT ptm.matched_token, ', ' ORDER BY ptm.matched_token) AS matched_value,
    pr.county,
    pr.flag,
    jsonb_build_object(
      'explanation', 'Phonetic match using ' || ptm.phonetic_type || ' algorithm',
      'matchedTokens', COUNT(DISTINCT ptm.query_token),
      'totalQueryTokens', (SELECT COUNT(*) FROM qtokens_weighted),
      'displayText', 'Matched ' || COUNT(DISTINCT ptm.query_token) || ' tokens via ' || ptm.phonetic_type
    ) AS match_metadata
  FROM phonetic_token_matches ptm
  JOIN person pr ON pr.person_id = ptm.person_id
  GROUP BY ptm.person_id, pr.full_name, ptm.phonetic_type, pr.county, pr.flag
), all_matches AS (
  SELECT * FROM exact_matches
  UNION ALL
  SELECT * FROM nickname_matches WHERE include_nicknames = TRUE
  UNION ALL
  SELECT * FROM rule_based_matches WHERE include_fuzzy = TRUE
  UNION ALL
  SELECT * FROM phonetic_matches WHERE include_fuzzy = TRUE
), ranked AS (
  -- Keep best match per person, with priority ordering
  SELECT DISTINCT ON (person_id)
         person_id, 
         full_name, 
         match_type, 
         similarity_score,
         matched_field, 
         matched_value, 
         county, 
         flag, 
         match_metadata,
         -- Add sort priority: Exact=1, Trigram=2, Phonetic=3
         CASE match_type 
           WHEN 'Exact' THEN 1
           WHEN 'TrigramSimilarity' THEN 2
           ELSE 3
         END AS match_priority
  FROM all_matches
  ORDER BY person_id, 
           CASE match_type WHEN 'Exact' THEN 1 WHEN 'TrigramSimilarity' THEN 2 ELSE 3 END,
           similarity_score DESC
)
SELECT person_id, full_name, match_type, similarity_score, matched_field, matched_value, county, flag, match_metadata
FROM ranked
WHERE (county_filter IS NULL OR county = county_filter)
  AND (flag_filter IS NULL OR flag = flag_filter)
ORDER BY match_priority ASC, similarity_score DESC, full_name ASC
LIMIT max_results;
$$;
