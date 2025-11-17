-- Apply nickname expansion to existing person_names data
-- Run this AFTER you've populated nickname_map with LLM data

-- This function expands nicknames for already-imported persons
CREATE OR REPLACE FUNCTION expand_nicknames_bulk()
RETURNS TABLE(persons_updated BIGINT, nicknames_added BIGINT) 
LANGUAGE plpgsql AS $$
DECLARE
    v_persons_updated BIGINT := 0;
    v_nicknames_added BIGINT := 0;
    rec RECORD;
BEGIN
    RAISE NOTICE 'Starting bulk nickname expansion...';
    
    -- For each person_name token, check if it has nickname variants in nickname_map
    -- For each original token (e.g., 'robert'), find its nicknames (e.g., 'bob')
    FOR rec IN 
        SELECT DISTINCT pn.person_id, pn.name_token AS original_token, nm.normalized_nickname AS nickname
        FROM person_names pn
        JOIN nickname_map nm ON nm.normalized_original = pn.name_token
        WHERE pn.is_nickname = FALSE  -- Don't expand already-expanded nicknames
    LOOP
        -- Insert the nickname token for this person (e.g., add 'bob' for a person with 'robert')
        INSERT INTO person_names (
            person_id, 
            name_token, 
            soundex_code, 
            metaphone_code, 
            double_metaphone_code,
            is_nickname,
            original_token,
            token_position
        )
        SELECT 
            rec.person_id,
            rec.nickname,
            soundex(rec.nickname),
            metaphone(rec.nickname, 4),
            dmetaphone(rec.nickname),
            TRUE,  -- Mark as nickname
            rec.original_token,  -- Store original token (e.g., 'robert')
            -1  -- Nickname expansions don't have position
        ON CONFLICT (person_id, name_token) DO NOTHING;
        
        GET DIAGNOSTICS v_nicknames_added = v_nicknames_added + ROW_COUNT;
        v_persons_updated := v_persons_updated + 1;
        
        -- Progress update every 1000 persons
        IF v_persons_updated % 1000 = 0 THEN
            RAISE NOTICE 'Processed % persons, added % nickname variants', 
                v_persons_updated, v_nicknames_added;
        END IF;
    END LOOP;
    
    RAISE NOTICE 'Completed: Updated % persons, added % nickname variants', 
        v_persons_updated, v_nicknames_added;
    
    RETURN QUERY SELECT v_persons_updated, v_nicknames_added;
END;
$$;

-- Execute the bulk expansion
SELECT * FROM expand_nicknames_bulk();

-- Verify results
SELECT 
    COUNT(DISTINCT person_id) as total_persons,
    COUNT(*) FILTER (WHERE is_nickname = FALSE) as original_tokens,
    COUNT(*) FILTER (WHERE is_nickname = TRUE) as nickname_variants
FROM person_names;
