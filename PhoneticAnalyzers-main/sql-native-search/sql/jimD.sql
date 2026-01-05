CREATE OR REPLACE FUNCTION public.sp_search_names(
    p_county_id integer,
    p_query text,
    p_limit integer DEFAULT 20,
    p_similarity_threshold double precision DEFAULT 0.30,
    p_boost_exact double precision DEFAULT 30,
    p_boost_prefix double precision DEFAULT 10,
    p_boost_phonetic double precision DEFAULT 15,
    p_boost_trigram double precision DEFAULT 20
)
RETURNS TABLE (
    out_id bigint,
    out_nameid bigint,
    out_countyid integer,
    out_fullname varchar,
    out_searchedname varchar,
    out_exact_score double precision,
    out_trigram_score double precision,
    out_phonetic_score double precision,
    out_prefix_score double precision,
    out_total_score double precision,
    out_match_type text,
    out_priority integer
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_normalized_query text;
    v_lower_query text;
    v_phonetic_query text;
    v_prefix_pattern text;
BEGIN
    -- Normalize query
    v_normalized_query := lower(trim(regexp_replace(coalesce(p_query,''), '\s+', ' ', 'g')));
    v_lower_query      := v_normalized_query;
    v_phonetic_query   := public.dm_phonetic(v_normalized_query);
    v_prefix_pattern   := v_normalized_query || '%';

    -- Set trigram similarity threshold for this execution
    PERFORM set_config('pg_trgm.similarity_threshold', p_similarity_threshold::text, true);

    RETURN QUERY
    WITH
    -- Strategy 1: Fast exact match (btree index on lower(searchedName))
    exact_matches AS (
        SELECT
            n.id,
            n.nameId,
            n.countyId,
            n.fullname,
            n.searchedName,
            p_boost_exact::double precision AS exact_score,
            0.0::double precision AS trigram_score,
            0.0::double precision AS phonetic_score,
            0.0::double precision AS prefix_score,
            1 AS priority
        FROM public.names n
        WHERE n.countyId = p_county_id
          AND lower(n.searchedName) = v_normalized_query
        LIMIT p_limit
    ),

    -- Strategy 2: Fast prefix match (text_pattern_ops index)
    prefix_matches AS (
        SELECT
            n.id,
            n.nameId,
            n.countyId,
            n.fullname,
            n.searchedName,
            0.0::double precision AS exact_score,
            0.0::double precision AS trigram_score,
            0.0::double precision AS phonetic_score,
            p_boost_prefix::double precision AS prefix_score,
            2 AS priority
        FROM public.names n
        WHERE n.countyId = p_county_id
          AND lower(n.searchedName) LIKE v_prefix_pattern
          AND NOT EXISTS (SELECT 1 FROM exact_matches e WHERE e.id = n.id)
        ORDER BY n.searchedName
        LIMIT (p_limit * 2)
    ),

    -- Strategy 3: Phonetic match (double metaphone)
    phonetic_matches AS (
        SELECT
            n.id,
            n.nameId,
            n.countyId,
            n.fullname,
            n.searchedName,
            0.0::double precision AS exact_score,
            0.0::double precision AS trigram_score,
            p_boost_phonetic::double precision AS phonetic_score,
            0.0::double precision AS prefix_score,
            3 AS priority
        FROM public.names n
        WHERE n.countyId = p_county_id
          AND public.dm_phonetic(n.searchedName) = v_phonetic_query
          AND NOT EXISTS (SELECT 1 FROM exact_matches e WHERE e.id = n.id)
          AND NOT EXISTS (SELECT 1 FROM prefix_matches p WHERE p.id = n.id)
        LIMIT (p_limit * 2)
    ),

    -- How many results we already have from 1..3?
    current_count AS (
        SELECT
            (SELECT count(*) FROM exact_matches) +
            (SELECT count(*) FROM prefix_matches) +
            (SELECT count(*) FROM phonetic_matches) AS total
    ),

    -- Strategy 4: Trigram match ONLY if not enough results (STRICT cap)
    trigram_matches AS (
        SELECT
            n.id,
            n.nameId,
            n.countyId,
            n.fullname,
            n.searchedName,
            0.0::double precision AS exact_score,
            (similarity(lower(n.searchedName), v_lower_query) * p_boost_trigram)::double precision AS trigram_score,
            0.0::double precision AS phonetic_score,
            0.0::double precision AS prefix_score,
            4 AS priority
        FROM public.names n, current_count cc
        WHERE cc.total < p_limit
          AND n.countyId = p_county_id
          AND lower(n.searchedName) % v_lower_query
          AND NOT EXISTS (SELECT 1 FROM exact_matches e WHERE e.id = n.id)
          AND NOT EXISTS (SELECT 1 FROM prefix_matches p WHERE p.id = n.id)
          AND NOT EXISTS (SELECT 1 FROM phonetic_matches ph WHERE ph.id = n.id)
        ORDER BY similarity(lower(n.searchedName), v_lower_query) DESC
        LIMIT LEAST(p_limit * 3, 500)
    ),

    -- Combine all results (we already deduped with NOT EXISTS)
    all_results AS (
        SELECT * FROM exact_matches
        UNION ALL
        SELECT * FROM prefix_matches
        UNION ALL
        SELECT * FROM phonetic_matches
        UNION ALL
        SELECT * FROM trigram_matches
    ),

    -- Recalculate scores (like your screenshot block)
    scored_results AS (
        SELECT
            r.id,
            r.nameId,
            r.countyId,
            r.fullname,
            r.searchedName,

            -- Exact re-score
            (CASE
                WHEN lower(r.fullname) = v_normalized_query THEN p_boost_exact
                WHEN lower(r.searchedName) = v_normalized_query THEN p_boost_exact * 0.9
                ELSE r.exact_score
             END)::double precision AS exact_score,

            -- Trigram re-score
            (CASE
                WHEN r.trigram_score > 0 THEN r.trigram_score
                ELSE GREATEST(
                    similarity(lower(r.fullname), v_lower_query) * p_boost_trigram,
                    similarity(lower(r.searchedName), v_lower_query) * p_boost_trigram * 0.8
                )
             END)::double precision AS trigram_score,

            -- Phonetic re-score
            (CASE
                WHEN public.dm_phonetic(r.searchedName) = v_phonetic_query THEN p_boost_phonetic
                ELSE r.phonetic_score
             END)::double precision AS phonetic_score,

            -- Prefix re-score
            (CASE
                WHEN lower(r.searchedName) LIKE v_prefix_pattern THEN p_boost_prefix
                WHEN lower(r.fullname) LIKE v_prefix_pattern THEN p_boost_prefix * 0.8
                ELSE r.prefix_score
             END)::double precision AS prefix_score,

            r.priority
        FROM all_results r
    ),

    -- Optional final safety dedupe (keeps best row per id if duplicates ever sneak in)
    deduped AS (
        SELECT DISTINCT ON (s.id)
            s.*
        FROM scored_results s
        ORDER BY s.id,
                 (s.exact_score + s.trigram_score + s.phonetic_score + s.prefix_score) DESC,
                 s.priority ASC
    )

    SELECT
        d.id        AS out_id,
        d.nameId    AS out_nameid,
        d.countyId  AS out_countyid,
        d.fullname  AS out_fullname,
        d.searchedName AS out_searchedname,
        d.exact_score   AS out_exact_score,
        d.trigram_score AS out_trigram_score,
        d.phonetic_score AS out_phonetic_score,
        d.prefix_score   AS out_prefix_score,

        LEAST((d.exact_score + d.trigram_score + d.phonetic_score + d.prefix_score), 100.0)::double precision
            AS out_total_score,

        (CASE
            WHEN d.exact_score >= 30 THEN 'exact'
            WHEN d.trigram_score >= 20 THEN 'trigram'
            WHEN d.phonetic_score >= 15 THEN 'phonetic'
            WHEN d.prefix_score >= 5 THEN 'prefix'
            ELSE 'fuzzy'
         END)::text AS out_match_type,

        d.priority AS out_priority
    FROM deduped d
    WHERE (d.exact_score + d.trigram_score + d.phonetic_score + d.prefix_score) > 0
    ORDER BY
        d.priority ASC,
        (d.exact_score + d.trigram_score + d.phonetic_score + d.prefix_score) DESC,
        d.searchedName
    LIMIT p_limit;

END;
$$;
