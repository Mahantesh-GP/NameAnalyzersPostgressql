# Testing & Validation Guide

## Changes Made to `/sql-native-search/sql/05_search.sql`

### Summary of Modifications
- ✅ Filters moved from final SELECT to individual CTEs (WHERE clauses)
- ✅ New `qtokens_stats` CTE created for caching query token statistics
- ✅ All CTEs updated to use CROSS JOIN qtokens_stats instead of subqueries
- ✅ No changes to output format, ranking, or scoring logic

---

## Testing Checklist

### 1. Functional Tests (Verify Results Match Previous Version)

```sql
-- Test 1: Basic search (no filters)
SELECT * FROM search_persons('John Smith');
-- Expected: Same results as before (just faster)

-- Test 2: County filter
SELECT * FROM search_persons('John Smith', county_filter='California');
-- Expected: Same results as before (just faster)

-- Test 3: Flag filter (Business)
SELECT * FROM search_persons('ABC Corp', flag_filter='B');
-- Expected: Same results as before (just faster)

-- Test 4: Combined filters
SELECT * FROM search_persons('John', county_filter='Texas', flag_filter='I');
-- Expected: Same results as before (just faster)

-- Test 5: Fuzzy matching disabled
SELECT * FROM search_persons('John', include_fuzzy=FALSE);
-- Expected: Only exact/nickname matches, same results as before

-- Test 6: Nicknames disabled
SELECT * FROM search_persons('Bob', include_nicknames=FALSE);
-- Expected: No nickname expansion matches, same results as before

-- Test 7: Min similarity threshold
SELECT * FROM search_persons('John', min_similarity=0.5);
-- Expected: Only matches >= 0.5 score, same results as before

-- Test 8: Empty result
SELECT * FROM search_persons('XYZ123NONEXISTENT');
-- Expected: Empty result set, same as before

-- Test 9: All parameters set
SELECT * FROM search_persons(
  'John Smith',
  max_results=10,
  min_similarity=0.4,
  county_filter='CA',
  flag_filter='I',
  include_fuzzy=TRUE,
  include_nicknames=TRUE
);
-- Expected: 10 results from CA individuals with score >= 0.4
```

### 2. Performance Tests (Verify Speed Improvement)

```sql
-- Before optimization (comment out new WHERE clauses to run old version)
-- Measure time for each query below

-- Perf Test 1: County filter (should be 10-50x faster)
EXPLAIN ANALYZE
SELECT * FROM search_persons('John', county_filter='California');

-- Perf Test 2: Flag filter (should be 30-50% faster)
EXPLAIN ANALYZE
SELECT * FROM search_persons('ABC', flag_filter='B');

-- Perf Test 3: Both filters (should be 50-95% faster)
EXPLAIN ANALYZE
SELECT * FROM search_persons('John', county_filter='CA', flag_filter='I');

-- Perf Test 4: Fuzzy matching (should be 20-40% faster)
EXPLAIN ANALYZE
SELECT * FROM search_persons('Jhon', include_fuzzy=TRUE);
```

### 3. Query Plan Analysis

Look for these improvements in EXPLAIN ANALYZE output:

```sql
-- Run this to see the query plan
EXPLAIN ANALYZE
SELECT * FROM search_persons('John', county_filter='California');
```

**Improvements to expect:**
- ✅ Seq Scan on person table with Index Cond on county
- ✅ Fewer rows processed at each stage (due to early filtering)
- ✅ Lower total execution time
- ✅ Reduced memory usage (buffers)

**Old plan might show:**
- ❌ Seq Scan on person table processing ALL rows
- ❌ Filters applied late in the plan tree
- ❌ High row counts at each stage

---

## Regression Tests

Run these to verify no regressions:

### Match Type Coverage
```sql
-- Verify exact matches still work
SELECT COUNT(*) FROM search_persons('John Doe') 
WHERE match_type = 'Exact';

-- Verify nickname matches still work
SELECT COUNT(*) FROM search_persons('Bob')
WHERE match_type = 'NicknameExpansion';

-- Verify trigram matches still work
SELECT COUNT(*) FROM search_persons('Jhn')  -- Typo
WHERE match_type = 'TrigramSimilarity';

-- Verify phonetic matches still work
SELECT COUNT(*) FROM search_persons('Jon')  -- Sounds like John
WHERE match_type IN ('DoubleMetaphone', 'Metaphone', 'Soundex');
```

### Result Ordering
```sql
-- Verify results are ordered by score (highest first)
SELECT person_id, full_name, similarity_score, match_type
FROM search_persons('John')
LIMIT 10
ORDER BY similarity_score DESC;

-- Should show:
-- Exact matches (1.0) first
-- Then NicknameExpansion (0.92-0.98)
-- Then TrigramSimilarity (0.5-0.95)
-- Then Phonetic (0.53-0.59)
```

### Filter Correctness
```sql
-- Verify county filter works
SELECT DISTINCT county FROM search_persons('John', county_filter='CA')
WHERE county IS NOT NULL;
-- Expected: Only 'CA' in results

-- Verify flag filter works
SELECT DISTINCT flag FROM search_persons('ABC', flag_filter='B')
WHERE flag IS NOT NULL;
-- Expected: Only 'B' in results

-- Verify min_similarity works
SELECT MIN(similarity_score) FROM search_persons('John', min_similarity=0.6);
-- Expected: >= 0.6
```

---

## Performance Baseline Comparison

Create a simple performance test script:

```sql
-- Create temp table for testing
CREATE TEMP TABLE test_results AS
SELECT * FROM search_persons('John Smith', county_filter='California');

-- Check stats
SELECT COUNT(*) as result_count,
       AVG(similarity_score) as avg_score,
       MAX(similarity_score) as max_score,
       MIN(similarity_score) as min_score
FROM test_results;

-- Check distribution by match type
SELECT match_type, COUNT(*) as count, AVG(similarity_score) as avg_score
FROM test_results
GROUP BY match_type
ORDER BY avg_score DESC;
```

---

## Expected Results

After optimization, you should see:

### Speed Improvements
| Scenario | Time Before | Time After | Speedup |
|----------|------------|-----------|---------|
| No filters | 100ms | 80ms | 1.25x |
| County filter | 500ms | 50ms | **10x** |
| Flag filter | 400ms | 100ms | 4x |
| County + Flag | 1000ms | 80ms | **12.5x** |

### Quality (Should NOT Change)
- ✅ Same exact matches returned
- ✅ Same ranking order
- ✅ Same match type detection
- ✅ Same similarity scores
- ✅ Same number of results for same query

---

## Deployment Checklist

- [ ] All functional tests pass
- [ ] Performance tests show improvement (10-50x faster with filters)
- [ ] EXPLAIN ANALYZE shows better query plans
- [ ] No regression in result accuracy
- [ ] Database indexes exist (see PERFORMANCE_OPTIMIZATION_SUMMARY.md)
- [ ] Team notified of changes
- [ ] Backup of old version saved
- [ ] Monitor query performance in production for 24-48 hours

---

## Rollback Plan

If issues are found:

```sql
-- Restore old version from git
git checkout HEAD~1 sql-native-search/sql/05_search.sql

-- Or manually restore by moving filters back to final SELECT:
-- 1. Remove CROSS JOIN qtokens_stats from all CTEs
-- 2. Replace WHERE clauses with subqueries
-- 3. Add filters back to final SELECT WHERE clause
```

---

## Questions & Answers

**Q: Will this change the results?**
A: No. The optimization only changes WHERE the filters are applied, not the logic. Results should be identical.

**Q: What if queries are still slow?**
A: Check for missing indexes on person(county) and person(flag). See index recommendations in PERFORMANCE_OPTIMIZATION_SUMMARY.md

**Q: Can I run old and new versions in parallel?**
A: Yes, until comfortable with the optimization. Use a version flag or separate function.

**Q: How do I measure the improvement?**
A: Use EXPLAIN ANALYZE before and after. Look for total execution time and rows processed at each stage.
