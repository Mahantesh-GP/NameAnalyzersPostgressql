-- Search function performing exact, trigram, and phonetic matches with nickname coverage

CREATE OR REPLACE FUNCTION search_persons(
  query_name TEXT,
  max_results INT DEFAULT 50,
  min_similarity DOUBLE PRECISION DEFAULT 0.3
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
  SELECT t.token, t.token_position
  FROM params p, tokenize_name(p.q) AS t
), phonetic AS (
  SELECT
    soundex(p.q) AS sx,
    metaphone(p.q, 4) AS mp,
    dmetaphone(p.q) AS dmp
  FROM params p
), exact_matches AS (
  -- Full name exact match
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
), nickname_matches AS (
  -- Matches via nickname expansion (e.g., bob → robert)
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
  FROM qtokens qt
  JOIN person_names pn ON pn.name_token = qt.token AND pn.is_nickname = TRUE
  JOIN person pr ON pr.person_id = pn.person_id
), trigram_matches AS (
  -- Match tokens from query against person name tokens with similarity threshold
  SELECT DISTINCT ON (pn.person_id, pn.name_token)
         pn.person_id, pr.full_name, 'TrigramSimilarity'::text AS match_type,
         similarity(pn.name_token, qt.token) AS similarity_score,
         'TokenOrName'::text AS matched_field,
         pn.name_token AS matched_value,
         pr.county,
         pr.flag,
         jsonb_build_object(
           'explanation', 'Fuzzy match using trigram similarity',
           'score', similarity(pn.name_token, qt.token),
           'searchedToken', qt.token,
           'matchedToken', pn.name_token,
           'displayText', 'Token "' || qt.token || '" matched "' || pn.name_token || '" (' || ROUND((similarity(pn.name_token, qt.token) * 100)::numeric, 1) || '% similarity)'
         ) AS match_metadata
  FROM qtokens qt
  JOIN person_names pn ON similarity(pn.name_token, qt.token) >= min_similarity
  JOIN person pr ON pr.person_id = pn.person_id
  ORDER BY pn.person_id, pn.name_token, similarity(pn.name_token, qt.token) DESC
), phonetic_matches AS (
  SELECT pn.person_id, pr.full_name, 'DoubleMetaphone'::text AS match_type,
         0.9::float8 AS similarity_score,
         'DoubleMetaphone'::text AS matched_field,
         pn.double_metaphone_code AS matched_value,
         pr.county,
         pr.flag,
         jsonb_build_object(
           'explanation', 'Phonetic match using Double Metaphone algorithm',
           'code', pn.double_metaphone_code,
           'displayText', 'Matched via Double Metaphone phonetic code'
         ) AS match_metadata
  FROM phonetic ph
  JOIN person_names pn ON pn.double_metaphone_code = ph.dmp
  JOIN person pr ON pr.person_id = pn.person_id
  UNION ALL
  SELECT pn.person_id, pr.full_name, 'Metaphone'::text AS match_type,
         0.85::float8 AS similarity_score,
         'Metaphone'::text AS matched_field,
         pn.metaphone_code AS matched_value,
         pr.county,
         pr.flag,
         jsonb_build_object(
           'explanation', 'Phonetic match using Metaphone algorithm',
           'code', pn.metaphone_code,
           'displayText', 'Matched via Metaphone phonetic code'
         ) AS match_metadata
  FROM phonetic ph
  JOIN person_names pn ON pn.metaphone_code = ph.mp
  JOIN person pr ON pr.person_id = pn.person_id
  UNION ALL
  SELECT pn.person_id, pr.full_name, 'Soundex'::text AS match_type,
         0.8::float8 AS similarity_score,
         'Soundex'::text AS matched_field,
         pn.soundex_code AS matched_value,
         pr.county,
         pr.flag,
         jsonb_build_object(
           'explanation', 'Phonetic match using Soundex algorithm',
           'code', pn.soundex_code,
           'displayText', 'Matched via Soundex phonetic code'
         ) AS match_metadata
  FROM phonetic ph
  JOIN person_names pn ON pn.soundex_code = ph.sx
  JOIN person pr ON pr.person_id = pn.person_id
), all_matches AS (
  SELECT * FROM exact_matches
  UNION ALL
  SELECT * FROM nickname_matches
  UNION ALL
  SELECT * FROM trigram_matches
  UNION ALL
  SELECT * FROM phonetic_matches
), ranked AS (
  SELECT DISTINCT ON (person_id)
         person_id, full_name, match_type, similarity_score, matched_field, matched_value, county, flag, match_metadata
  FROM all_matches
  ORDER BY person_id, similarity_score DESC
)
SELECT *
FROM ranked
ORDER BY similarity_score DESC, full_name ASC
LIMIT max_results;
$$;
