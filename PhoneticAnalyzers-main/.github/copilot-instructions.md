# PhoneticAnalyzers - AI Coding Agent Instructions

## Project Overview

PhoneticAnalyzers is a **multi-architecture name search system** with PostgreSQL-native phonetic matching. The project offers **three distinct API implementations** (EF Core, Database-First, Native SQL) that share common UI frontends (Blazor WebUI and Python FastAPI+HTMX).

**Key Insight**: This is NOT a typical monolith. It's a deliberate architectural experiment comparing ORM-based vs. SQL-native performance at scale (100K → 1B+ names).

## Critical Architecture Decisions

### Three API Approaches (Run Independently)

1. **EF Core Functions API** (`src/PhoneticAnalyzers.Functions.*`) - Port 7071-7072
   - Azure Functions with Entity Framework Core
   - CQRS pattern, DDD entities, repository abstraction
   - Best for: Development, <100K records
   
2. **Database-First Functions** (`SQLDBFirst/`) - Port 7073-7074
   - Code generated from existing database schema
   - Middle ground between EF and native SQL
   
3. **Native SQL API** (`sql-native-search/api/`) - Port 5100 ⭐ **PRODUCTION CHOICE**
   - ASP.NET Core with raw Npgsql, zero ORM overhead
   - All logic in PostgreSQL stored functions (`search_persons()`, `upsert_persons()`)
   - 10-200x faster for bulk operations
   - Best for: Production, 100K-1B+ records

**When modifying search/ingest logic**: Update the corresponding approach. Don't assume EF changes propagate to Native SQL—they use entirely different data access patterns.

### Database-First Development (Native SQL)

The Native SQL API follows **database-first** design:
- Schema defined in `sql-native-search/sql/02_schema.sql`
- Business logic in SQL stored functions (`04_functions.sql`, `05_search.sql`)
- API Controllers are thin wrappers calling `search_persons()` SQL function
- Deploy database changes BEFORE API changes: `sql-native-search/scripts/run-all.ps1`

**Pattern**: When adding features, implement in SQL first, then expose via API endpoint.

## Essential Developer Workflows

### Starting the System (PowerShell)

**Option A: Native SQL API (Recommended)**
```powershell
# Terminal 1: Deploy database (first time only)
cd sql-native-search\scripts
.\run-all.ps1

# Terminal 2: Start Native API
cd ..\api
dotnet run  # Runs on http://localhost:5100

# Terminal 3: Start Blazor WebUI
cd ..\..\WebUI
dotnet run  # Check appsettings.json points to correct API
```

**Option B: EF Core Functions (Development)**
```powershell
# Terminal 1: Ingestion Functions
cd src\PhoneticAnalyzers.Functions.Ingestion
func start  # Port 7071

# Terminal 2: Search Functions
cd ..\PhoneticAnalyzers.Functions.Search
func start  # Port 7072

# Terminal 3: WebUI
cd ..\..\WebUI
dotnet run
```

**Python UI** (FastAPI alternative):
```powershell
cd python-ui
# First time: poetry install or pip install -r requirements.txt
python run.py  # Port 8000
```

### Database Deployment (PostgreSQL)

**Critical**: The Native SQL approach requires database functions to be deployed before API starts.

```powershell
# Full deployment (creates phonetic_native database)
cd sql-native-search\scripts
.\run-all.ps1

# Reload single SQL file after changes
cd ..\sql
psql -h localhost -U postgres -d phonetic_native -f 05_search.sql
```

**Verify deployment**:
```sql
-- Check search function exists
SELECT proname FROM pg_proc WHERE proname = 'search_persons';

-- Test search directly
SELECT * FROM search_persons('john', 10, 0.3, NULL, NULL, TRUE, TRUE);
```

### Testing Search Strategies

The search system uses **4 strategies with UI-controlled filtering**:
- **EXACT**: Always included - normalized uppercase exact match on full name (100% score)
- **NICKNAME**: Requires `include_nicknames=TRUE` AND `nickname_maps` has data (92-98% score)
- **FUZZY** (TrigramSimilarity): Requires `include_fuzzy=TRUE` - pg_trgm token matching (60-89% score)
- **PHONETIC**: Requires `include_fuzzy=TRUE` - Metaphone/DoubleMetaphone codes (53-59% score)

**UI Checkbox Behavior (FIXED)**:
- Exact only: Returns only 100% matches
- Exact + Nickname: Returns exact + nickname expansion (if `nickname_maps` populated)
- Exact + Fuzzy: Returns exact + trigram + phonetic matches
- All enabled: Returns all match types

**Test all strategies**:
```powershell
cd sql-native-search\sql
psql -h localhost -U postgres -d phonetic_native -f test-all-strategies.sql
```

**When debugging search**: Check `sql-native-search/sql/05_search.sql` - all ranking logic is SQL, not application code.

## Project-Specific Conventions

### Configuration Pattern (Multi-API)

WebUI switches APIs via `wwwroot/appsettings.json`:
```json
{
  "ApiSettings": {
    "Approach": "NativeSQL",  // or "EFCore"
    "NativeSQL": {
      "SearchBaseUrl": "http://localhost:5100"
    },
    "EFCore": {
      "IngestionBaseUrl": "http://localhost:7071",
      "SearchBaseUrl": "http://localhost:7072"
    }
  }
}
```

**Never hardcode API URLs** in Blazor components—inject via `IConfiguration`.

### Phonetic Codes & Tokens Are Pre-Calculated

Unlike traditional apps that calculate phonetics on-the-fly, this system **persists everything**:
- `person_names.soundex_code`, `.metaphone_code`, `.double_metaphone_code`
- `person_names.name_token` - individual words from full name (one row per token)
- Generated during `ingest_person()` SQL function (called by `upsert_persons()`)
- Indexed for fast lookup

**Example**: Insert "John Davis" creates two `person_names` rows:
```
person_id | name_token | soundex_code | metaphone_code | ...
---------|-----------|--------------|----------------|----
123      | JOHN      | J500         | JN             | ...
123      | DAVIS     | D120         | TFS            | ...
```

**When ingesting data**: Codes and tokens are auto-generated. Never manually populate `person_names`.

### Nickname Expansion (FIXED: Now Requires Real Data)

**FIXED**: As of the latest update, "NicknameExpansion" results **only appear when `nickname_maps` table has actual data**.

**How it works**:
1. The search function checks if `nickname_maps` table has entries
2. If empty → no "NicknameExpansion" results (works correctly now)
3. If populated → expands query tokens via nickname mappings

**Two-Stage Nickname System**:

**Stage 1: Populate `nickname_maps` (one-time)**:
```powershell
cd tools\NicknameEnrichment
# Configure appsettings.json with Azure OpenAI credentials
dotnet run
# This generates mappings like: William → Bill, Will, Billy
```

**Stage 2: Ingest/re-ingest persons to expand tokens**:
```powershell
# For new data - tokens are auto-expanded during ingestion
psql -d phonetic_native -c "SELECT ingest_person('ID001', 'William Smith', 'Los Angeles', 'I');"

# For existing data - re-ingest to expand
psql -d phonetic_native -c "SELECT ingest_person(external_id, full_name, county, flag) FROM person;"
```

**Example workflow**:
- Insert "William Smith" → creates tokens: WILLIAM, SMITH
- If `nickname_maps` has William→Bill: also creates token: BILL
- Search "Bill" → finds "William Smith" via nickname expansion
- Match type: "NicknameExpansion" with 92-98% score

### Bulk Ingestion (Million+ Records)

**Do NOT use API for bulk loads**. Use PostgreSQL COPY:
```powershell
# Stage data
psql -h localhost -U postgres -d phonetic_native -c "\copy staging_persons FROM 'data.csv' CSV HEADER"

# Process (generates phonetic codes + nicknames)
psql -h localhost -U postgres -d phonetic_native -c "SELECT process_staging_persons();"
```

Speed: ~100K rows/second vs. ~500 rows/second via API.

## Integration Points

### API ↔ UI Communication

**Blazor WebUI** → Any API (configured via `ApiSettings.Approach`)
- Uses `HttpClient` with `IConfiguration` for base URL
- Search: `GET /api/search?queryName=...`
- Returns: `SearchResponse` with `match_type` (EXACT|NICKNAME|PHONETIC|FUZZY)

**Python UI** → Native SQL API (hardcoded in `app/config.py`)
- FastAPI backend with HTMX frontend
- Direct `asyncpg` connection pool to PostgreSQL
- Calls `search_persons()` SQL function directly (no REST API in between)

### Database Schema Sync

**Important**: The three API approaches use **different databases**:
- EF Core: Any name (auto-migrated via EF migrations)
- Native SQL: `phonetic_native` (deployed via psql scripts)

**Migration workflow** (Native SQL):
1. Edit `sql-native-search/sql/*.sql`
2. Run `scripts\run-all.ps1`
3. API automatically picks up changes (no code changes needed if function signature unchanged)

### Azure Functions vs ASP.NET Core

**EF Core/DB-First APIs**: Azure Functions v4 (.NET 8)
- `local.settings.json` for configuration
- Dependency injection via `Startup.cs` or Program.cs minimal hosting
- Start with `func start`

**Native SQL API**: ASP.NET Core 8
- `appsettings.json` for configuration
- Standard DI in `Program.cs`
- Start with `dotnet run`

**When adding controllers**: Native API uses standard ASP.NET Core MVC pattern. Functions APIs use `[Function]` attribute triggers.

## Debugging Tips

### Search Returns No Results

1. Check which strategies are enabled (UI toggles)
2. Test SQL function directly: `SELECT * FROM search_persons('name', 10, 0.3, NULL, NULL, TRUE, TRUE);`
3. **MOST COMMON**: Verify `person_names` table has tokenized data:
   ```sql
   SELECT * FROM person_names WHERE person_id = X;
   -- Should have one row per word in the name
   ```
4. Check if data was ingested properly: `SELECT * FROM person WHERE external_id = 'xyz';`
5. (Optional) Check nickname mappings if you're using that feature: `SELECT * FROM nickname_maps LIMIT 10;`

### API Not Starting

**Native SQL API**:
```powershell
# Check PostgreSQL running
Get-Service postgresql*

# Verify database exists
psql -h localhost -U postgres -l | Select-String phonetic_native

# Test connection string
psql -h localhost -U postgres -d phonetic_native -c "SELECT 1"
```

**EF Core Functions**:
```powershell
# Check Azure Functions Core Tools installed
func --version

# Verify local.settings.json exists
Test-Path src\PhoneticAnalyzers.Functions.Ingestion\local.settings.json
```

### Performance Issues

**Slow searches (>500ms)**: 
- Check indexes: `sql-native-search/sql/03_indexes.sql`
- Run `ANALYZE person; ANALYZE person_names;` to update statistics
- Check query plan: `EXPLAIN ANALYZE SELECT * FROM search_persons(...);`

**Slow ingestion**:
- Use bulk COPY method, not API for >1K records
- Disable indexes during bulk load, rebuild after

## Testing Strategy

**No comprehensive test suite exists** (acknowledged tech debt). Manual testing via:
- Swagger UI: `http://localhost:5100/swagger` (Native SQL)
- SQL scripts: `sql-native-search/sql/test-all-strategies.sql`
- WebUI interactive testing

**When adding features**: Add test queries to `test-queries.sql` for regression testing.

## Common Mistakes to Avoid

1. ❌ **Don't mix API databases**: EF Core and Native SQL use separate databases
2. ❌ **Don't skip `run-all.ps1`**: Native API won't work without deployed SQL functions
3. ❌ **Don't use API for bulk ingestion**: Use PostgreSQL COPY for >1K records
4. ❌ **Don't calculate phonetics in app code**: They're pre-calculated during ingestion
5. ❌ **Don't expect nickname results without populating `nickname_maps`**: It's now properly gated
6. ❌ **Don't forget to re-ingest after populating `nickname_maps`**: Tokens need expansion
7. ❌ **Don't assume EF patterns apply to Native SQL**: Completely different data access

## Quick Reference

| Task | Command | Port |
|------|---------|------|
| Deploy DB | `sql-native-search\scripts\run-all.ps1` | - |
| Native API | `cd sql-native-search\api; dotnet run` | 5100 |
| EF Ingestion | `cd src\PhoneticAnalyzers.Functions.Ingestion; func start` | 7071 |
| EF Search | `cd src\PhoneticAnalyzers.Functions.Search; func start` | 7072 |
| Blazor UI | `cd WebUI; dotnet run` | 5000 |
| Python UI | `cd python-ui; python run.py` | 8000 |
| Test DB | `psql -h localhost -U postgres -d phonetic_native` | 5432 |

## Key Files

- `ARCHITECTURE-OVERVIEW.md` - Three-API architecture comparison
- `SETUP-GUIDE.md` - New machine setup checklist
- `sql-native-search/README.md` - Native SQL approach details
- `sql-native-search/sql/05_search.sql` - Core search logic (SQL function)
- `sql-native-search/scripts/run-all.ps1` - Database deployment script
- `WebUI/wwwroot/appsettings.json` - API endpoint configuration
- `PROCESS-FLOW-DIAGRAMS.md` - Old vs. new system comparison

**When in doubt**: Check `ARCHITECTURE-OVERVIEW.md` for which API approach to use.
