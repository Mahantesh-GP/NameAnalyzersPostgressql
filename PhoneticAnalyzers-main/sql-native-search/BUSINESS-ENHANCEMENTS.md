# Business Name Handling Enhancements

## Problems Identified

### Current Issues with flag='B' (Business) Records:
1. **Nickname expansion applied incorrectly**: "Robert's LLC" would get nickname variants like "Bob's LLC"
2. **Phonetic noise from suffixes**: "LLC", "INC", "CORP" create false phonetic matches
3. **No core business name matching**: "ABC Solutions" vs "ABC Solutions LLC" treated as completely different
4. **Suffix tokens weighted too low**: Already downweighted in search, but not excluded from phonetic

## Enhancements Applied

### Schema Changes (09_business_enhancements.sql):
- Added `person.business_core_name`: Normalized name with common suffixes stripped
- Added `person_names.is_business_suffix`: Marks tokens like LLC, INC for special handling
- Added triggers to auto-populate business_core_name on insert/update
- Added index on business_core_name for fast exact matching

### Function Updates:

#### 04_functions.sql - ingest_person():
```sql
-- Skip nickname expansion for businesses (flag='B')
IF NOT v_is_business THEN
  -- nickname expansion logic
END IF;
```

#### 05_search.sql - search_persons():
```sql
-- Added business core name exact matching:
-- "ABC SOLUTIONS" query matches "ABC SOLUTIONS LLC" record

-- Excluded businesses from nickname matching:
-- pr.flag <> 'B' filter added
```

### New Functions:
- `normalize_business_core()`: Strips 25+ common business suffixes
- `mark_business_suffixes()`: Tags suffix tokens for noise reduction
- `set_business_core_name()`: Trigger to maintain business_core_name

## Suffix List (25+ covered):
LIMITED, LTD, PRIVATE, PVT, LLC, INC, CORP, CO, CORPORATION, COMPANY, SOLUTIONS, SERVICES, GROUP, ENTERPRISES, INDUSTRIES, INTERNATIONAL, GLOBAL, PARTNERS, ASSOCIATES, HOLDINGS, VENTURES, CAPITAL, MANAGEMENT, TECHNOLOGIES, SYSTEMS, CONSULTING, FINANCIAL, INSURANCE, INVESTMENTS, PROPERTIES, DEVELOPMENT, CONSTRUCTION, MARKETING

## Search Behavior Changes:

### Before:
- Query: "ABC Solutions"
- Match: "ABC Solutions LLC" → Partial trigram match (lower score)
- Noise: Phonetic match on "LLC" across thousands of businesses

### After:
- Query: "ABC Solutions"
- Match: "ABC Solutions LLC" → **Exact (business core)** match (0.95 score)
- Reduced noise: Nicknames not expanded, suffix-heavy results deprioritized

## Deployment Steps:

```powershell
# 1. Apply updated base functions
$env:PGPASSWORD="postgres"
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -h localhost -U postgres -d phonetic_native -f "sql-native-search/sql/04_functions.sql"

# 2. Apply business enhancements (schema + functions)
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -h localhost -U postgres -d phonetic_native -f "sql-native-search/sql/09_business_enhancements.sql"

# 3. Apply updated search function
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -h localhost -U postgres -d phonetic_native -f "sql-native-search/sql/05_search.sql"

# 4. Verify
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -h localhost -U postgres -d phonetic_native -c "
SELECT COUNT(*) FILTER (WHERE flag='B') as businesses,
       COUNT(*) FILTER (WHERE flag='B' AND business_core_name IS NOT NULL) as with_core_name
FROM person;
"
```

## Future Enhancements (Optional):
1. **Industry/category tags**: Add person.industry for sector-based filtering
2. **Parent-subsidiary tracking**: Link related business entities
3. **Trade name aliases**: Support DBAs / doing-business-as names
4. **Acronym expansion**: "IBM" ↔ "International Business Machines"
5. **Skip phonetic for common business tokens**: Mark "SOLUTIONS", "SERVICES" as is_business_suffix

## Performance Impact:
- **Positive**: Business core exact match is O(1) index lookup
- **Positive**: Fewer nickname variants = smaller person_names table for businesses
- **Neutral**: Added trigger overhead on insert/update (minimal)
- **Recommendation**: Run VACUUM ANALYZE after bulk re-ingest

