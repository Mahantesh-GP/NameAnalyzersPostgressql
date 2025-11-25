# Search Strategy Fix - Summary

## Problem
The search function was showing "NicknameExpansion" results even when the `nickname_maps` table was empty. This was misleading because:
- Users saw "NicknameExpansion" matches for regular token matches
- The UI checkboxes didn't properly filter results by strategy
- No actual nickname expansion was happening

## Root Cause
The `expanded_qtokens` CTE included original query tokens via UNION, so when `nickname_maps` was empty, it still matched tokens directly against `person_names`, labeling them as "NicknameExpansion".

## Solution
Modified `sql-native-search/sql/05_search.sql`:

### Change 1: Separate Nickname Expansion CTE
```sql
-- OLD: Mixed original tokens with nickname expansions
expanded_qtokens AS (
  SELECT token FROM qtokens_weighted  -- Always included originals
  UNION
  SELECT nm.nickname FROM ... WHERE include_nicknames = TRUE
)

-- NEW: ONLY nickname-expanded tokens
expanded_qtokens_via_nicknames AS (
  SELECT nm.nickname, qt.token_weight, qt.token AS original_token
  FROM qtokens_weighted qt
  JOIN nickname_maps nm ON nm.canonical_name = qt.token
  WHERE include_nicknames = TRUE
  -- No UNION with original tokens!
)
```

### Change 2: Match Only Expanded Nicknames
```sql
-- OLD: Matched any token (including originals)
nickname_matches_raw AS (
  SELECT pn.person_id, eqt.token
  FROM expanded_qtokens eqt
  JOIN person_names pn ON pn.name_token = eqt.token
)

-- NEW: Match only when expansion occurred
nickname_matches_raw AS (
  SELECT pn.person_id, eqn.original_token
  FROM expanded_qtokens_via_nicknames eqn
  JOIN person_names pn ON pn.name_token = eqn.token
  WHERE eqn.token != eqn.original_token  -- Ensure it's expanded
)
```

### Change 3: Gate Results by Data Existence
```sql
-- OLD: Always included if include_nicknames=TRUE
SELECT * FROM nickname_matches WHERE include_nicknames = TRUE

-- NEW: Check if nickname data exists
SELECT * FROM nickname_matches 
WHERE include_nicknames = TRUE 
  AND EXISTS (SELECT 1 FROM expanded_qtokens_via_nicknames LIMIT 1)
```

## Result Behavior (After Fix)

### With Empty `nickname_maps` Table:
| Checkboxes Selected | Results Shown |
|---------------------|---------------|
| Exact only | Exact matches only (100% score) |
| Exact + Nickname | Exact matches only (nickname has no data) |
| Exact + Fuzzy | Exact + TrigramSimilarity + Phonetic |
| All enabled | Exact + TrigramSimilarity + Phonetic (no NicknameExpansion) |

### With Populated `nickname_maps` Table:
| Checkboxes Selected | Results Shown |
|---------------------|---------------|
| Exact only | Exact matches only |
| Exact + Nickname | Exact + NicknameExpansion (92-98% score) |
| Exact + Fuzzy | Exact + TrigramSimilarity + Phonetic (no nickname) |
| All enabled | Exact + NicknameExpansion + TrigramSimilarity + Phonetic |

## Strategy Priority (Deduplication)
When the same person matches via multiple strategies:
1. Exact (priority 1) - always wins
2. NicknameExpansion (priority 2) - beats fuzzy/phonetic
3. TrigramSimilarity (priority 3) - beats phonetic
4. Phonetic (priority 4) - lowest priority

## Testing
Run the test script:
```powershell
cd sql-native-search\sql
psql -h localhost -U postgres -d phonetic_native -f test-nickname-fix.sql
```

Expected output:
- With empty `nickname_maps`: NO "NicknameExpansion" in results
- With `include_nicknames=FALSE`: NO "NicknameExpansion" regardless of data
- With `include_fuzzy=FALSE`: NO phonetic or trigram results

## How to Enable Nickname Expansion

### Step 1: Populate `nickname_maps`
```powershell
cd tools\NicknameEnrichment
# Configure appsettings.json with Azure OpenAI credentials
dotnet run
```

### Step 2: Re-ingest Data to Expand Tokens
```powershell
# Re-ingest all existing persons
psql -h localhost -U postgres -d phonetic_native -c \
  "SELECT ingest_person(external_id, full_name, county, flag) FROM person;"
```

### Step 3: Verify
```sql
-- Check nickname mappings exist
SELECT * FROM nickname_maps LIMIT 10;

-- Check expanded tokens were created
SELECT pn.name_token, pn.is_nickname, pn.original_token
FROM person_names pn
WHERE pn.is_nickname = TRUE
LIMIT 10;

-- Test search
SELECT full_name, match_type, similarity_score
FROM search_persons('Bill', 10, 0.3, NULL, NULL, TRUE, TRUE)
WHERE match_type = 'NicknameExpansion';
-- Should find "William" names
```

## Files Modified
- `sql-native-search/sql/05_search.sql` - Search function logic
- `.github/copilot-instructions.md` - Updated documentation
- `sql-native-search/sql/test-nickname-fix.sql` - New test script

## Breaking Changes
None - this is a bug fix that makes the behavior match the intended design.
