# Database Index Recommendations

## Current Situation

The optimized query now filters on `county` and `flag` early in the CTEs. To maximize performance, ensure these indexes exist.

---

## Critical Indexes (Must Have)

### 1. Index on person.county
```sql
CREATE INDEX CONCURRENTLY idx_person_county 
ON person(county);

-- Rationale: Most queries use county_filter
-- Selectivity: ~50 counties means 2% of data per query
-- Expected gain: 10-50x faster on county filters
```

### 2. Index on person.flag
```sql
CREATE INDEX CONCURRENTLY idx_person_flag 
ON person(flag);

-- Rationale: Filters by person type (I=Individual, B=Business, etc)
-- Selectivity: ~3-5 flags means 20-33% of data per query
-- Expected gain: 30-50% faster on flag filters
```

### 3. Index on person.normalized_name
```sql
CREATE INDEX CONCURRENTLY idx_person_normalized_name 
ON person(normalized_name);

-- Rationale: Used in exact_matches CTE for exact match lookup
-- Selectivity: Very high (unique or near-unique)
-- Expected gain: Very fast exact match lookup
```

---

## High-Priority Indexes (Highly Recommended)

### 4. Composite Index: county + flag
```sql
-- For combined filters (the most common case)
CREATE INDEX CONCURRENTLY idx_person_county_flag 
ON person(county, flag);

-- Rationale: Optimizes queries filtering by both county AND flag
-- Expected gain: Even faster for combined filters
-- Query example: search_persons('John', county_filter='CA', flag_filter='I')
```

### 5. Composite Index: flag + county
```sql
-- Alternate ordering if flag is more selective
CREATE INDEX CONCURRENTLY idx_person_flag_county 
ON person(flag, county);

-- Rationale: If flag is more selective than county
-- Use this if: "Most queries specify flag first"
-- Choose ONE of #4 or #5, not both
```

### 6. Partial Index: Business core name
```sql
-- For business-specific searches (flag='B')
CREATE INDEX CONCURRENTLY idx_person_business_core 
ON person(business_core_name) 
WHERE flag = 'B' AND business_core_name IS NOT NULL;

-- Rationale: Used in exact_matches CTE for business searches
-- Expected gain: Very fast business name lookups
-- Size benefit: Only indexes businesses (smaller index)
```

---

## Medium-Priority Indexes (Recommended)

### 7. Index on person_names.name_token
```sql
CREATE INDEX CONCURRENTLY idx_person_names_token 
ON person_names(name_token);

-- Rationale: Used in token_matches and nickname_matches CTEs
-- Expected gain: Faster trigram similarity lookups
-- Note: Only if not already indexed
```

### 8. Index on person_names.metaphone_code
```sql
CREATE INDEX CONCURRENTLY idx_person_names_metaphone 
ON person_names(metaphone_code);

-- Rationale: Used in phonetic_matches CTE (Metaphone algorithm)
-- Expected gain: Faster phonetic lookups
-- Note: Only useful if phonetic matching is frequent
```

### 9. Index on person_names.soundex_code
```sql
CREATE INDEX CONCURRENTLY idx_person_names_soundex 
ON person_names(soundex_code);

-- Rationale: Used in phonetic_matches CTE (Soundex algorithm)
-- Expected gain: Faster phonetic lookups
-- Note: Only useful if phonetic matching is frequent
```

### 10. Index on person_names.double_metaphone_code
```sql
CREATE INDEX CONCURRENTLY idx_person_names_dmetaphone 
ON person_names(double_metaphone_code);

-- Rationale: Used in phonetic_matches CTE (DoubleMetaphone algorithm)
-- Expected gain: Fastest phonetic lookups (DoubleMetaphone is best)
-- Note: Only useful if phonetic matching is frequent
```

---

## Optional Indexes (Performance Tuning)

### 11. Composite Index: person_id + flag
```sql
CREATE INDEX CONCURRENTLY idx_person_id_flag 
ON person(person_id, flag);

-- Rationale: Used in nickname_matches for filtering businesses
-- Expected gain: Marginal (if person_id is PK, likely already fast)
```

### 12. Partial Index: Individuals only
```sql
CREATE INDEX CONCURRENTLY idx_person_individuals 
ON person(normalized_name, county) 
WHERE flag = 'I';

-- Rationale: Fast lookup for individual-only queries
-- Expected gain: Marginal (only if many individual-specific queries)
```

---

## Index Creation Plan

### Phase 1: Critical Indexes (Deploy immediately)
```sql
-- These 3 indexes provide maximum benefit
CREATE INDEX CONCURRENTLY idx_person_county 
ON person(county);

CREATE INDEX CONCURRENTLY idx_person_flag 
ON person(flag);

CREATE INDEX CONCURRENTLY idx_person_normalized_name 
ON person(normalized_name);
```

**Expected improvement:** 90% faster for county/flag filters

### Phase 2: High-Priority Indexes (Deploy after testing)
```sql
-- Composite indexes for combined filters
CREATE INDEX CONCURRENTLY idx_person_county_flag 
ON person(county, flag);

CREATE INDEX CONCURRENTLY idx_person_business_core 
ON person(business_core_name) 
WHERE flag = 'B' AND business_core_name IS NOT NULL;
```

**Expected improvement:** Even more dramatic speedup for combined filters

### Phase 3: Medium-Priority Indexes (Optional)
```sql
-- Only if phonetic/token matching is heavily used
CREATE INDEX CONCURRENTLY idx_person_names_token 
ON person_names(name_token);

CREATE INDEX CONCURRENTLY idx_person_names_dmetaphone 
ON person_names(double_metaphone_code);

CREATE INDEX CONCURRENTLY idx_person_names_metaphone 
ON person_names(metaphone_code);

CREATE INDEX CONCURRENTLY idx_person_names_soundex 
ON person_names(soundex_code);
```

---

## Index Creation Syntax

### Safe Creation (No Downtime)
```sql
-- Create indexes without locking the table
CREATE INDEX CONCURRENTLY idx_person_county 
ON person(county);

-- Shows progress but takes longer
-- Safe for production while queries are running
```

### Fast Creation (With Downtime)
```sql
-- Create indexes with table lock (faster but blocks queries)
CREATE INDEX idx_person_county 
ON person(county);

-- Only use during maintenance window!
```

### Verify Index Creation
```sql
-- Check if index exists
SELECT * FROM pg_indexes 
WHERE tablename = 'person' 
AND indexname LIKE 'idx_person%';

-- Check index usage
SELECT schemaname, tablename, indexname, idx_scan, idx_tup_read, idx_tup_fetch
FROM pg_stat_user_indexes
WHERE tablename = 'person';
```

---

## Expected Index Sizes

Approximate disk space for indexes (varies by data size):

```
idx_person_county:           2-10 MB
idx_person_flag:             500 KB - 2 MB
idx_person_normalized_name:  5-20 MB
idx_person_county_flag:      5-15 MB
idx_person_business_core:    1-5 MB (partial)
idx_person_names_token:      10-50 MB
```

**Total for all indexes:** ~30-120 MB depending on table size

---

## Performance Impact Examples

### Before Indexes
```sql
SELECT * FROM search_persons('John', county_filter='California');
-- Query time: 500-3500 ms (full table scan)
-- Plan: Seq Scan on person
```

### After Phase 1 Indexes
```sql
SELECT * FROM search_persons('John', county_filter='California');
-- Query time: 50-100 ms (index scan)
-- Plan: Index Scan using idx_person_county
-- Improvement: 5-10x faster ⚡
```

### After Phase 2 Indexes
```sql
SELECT * FROM search_persons('John', county_filter='California', flag_filter='I');
-- Query time: 20-50 ms (composite index)
-- Plan: Index Scan using idx_person_county_flag
-- Improvement: 10-50x faster ⚡⚡
```

---

## Maintenance

### Regular Index Maintenance
```sql
-- Analyze indexes for query planner
ANALYZE person;

-- Rebuild fragmented indexes (PostgreSQL 13+)
REINDEX INDEX CONCURRENTLY idx_person_county;

-- Check index bloat
SELECT schemaname, tablename, indexname, 
       pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes
WHERE tablename = 'person'
ORDER BY pg_relation_size(indexrelid) DESC;
```

### Monitoring Index Performance
```sql
-- Find unused indexes (safe to drop)
SELECT schemaname, tablename, indexname, idx_scan
FROM pg_stat_user_indexes
WHERE idx_scan = 0
ORDER BY pg_relation_size(indexrelid) DESC;

-- Find missing indexes (query planner recommendations)
-- Run EXPLAIN ANALYZE to see suggestions
```

---

## Recommendations Summary

| Priority | Indexes | Estimated Gain |
|----------|---------|----------------|
| **Critical** | idx_person_county, idx_person_flag, idx_person_normalized_name | **90% faster** |
| **High** | idx_person_county_flag, idx_person_business_core | **95% faster** |
| **Medium** | person_names indexes (token, metaphone, soundex, dmetaphone) | 10-30% faster |
| **Optional** | Partial indexes | Marginal |

---

## Quick Start Command

```sql
-- Run this to create all critical + high-priority indexes
-- (Safe with CONCURRENTLY - no downtime)

CREATE INDEX CONCURRENTLY idx_person_county 
ON person(county);

CREATE INDEX CONCURRENTLY idx_person_flag 
ON person(flag);

CREATE INDEX CONCURRENTLY idx_person_normalized_name 
ON person(normalized_name);

CREATE INDEX CONCURRENTLY idx_person_county_flag 
ON person(county, flag);

CREATE INDEX CONCURRENTLY idx_person_business_core 
ON person(business_core_name) 
WHERE flag = 'B' AND business_core_name IS NOT NULL;

-- Verify creation
SELECT * FROM pg_indexes 
WHERE tablename = 'person' 
AND indexname LIKE 'idx_person%'
ORDER BY indexname;
```

---

## Related Documentation

- Optimization changes: `PERFORMANCE_OPTIMIZATION_SUMMARY.md`
- Query flow: `DETAILED_EXECUTION_DIAGRAMS.md`
- Testing: `TESTING_AND_VALIDATION.md`

---

**Note:** The optimized query will work without these indexes, but will be significantly slower. The indexes unlock the full performance potential of the optimization!

Create indexes in Phase 1 for immediate benefit. Phase 2 and 3 are optional but provide additional optimization opportunity.
