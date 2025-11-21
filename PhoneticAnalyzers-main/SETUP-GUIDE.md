# Complete Setup Guide for New Machine

This guide helps you set up the Phonetic Analyzer project on a new laptop/machine.

## Prerequisites

1. **PostgreSQL 14+** installed
   - Default: localhost:5432
   - User: postgres
   - Password: postgres (or set your own)

2. **.NET 9.0 SDK** installed
   - Check: `dotnet --version`

3. **Git** (to clone the repository)

## Step-by-Step Setup

### 1. Clone the Repository

```powershell
git clone https://github.com/Mahantesh-GP/NameAnalyzersPostgressql.git
cd NameAnalyzersPostgressql
```

### 2. Set Up the Database

Run the deployment script to create database, tables, functions, and indexes:

```powershell
cd sql-native-search\scripts
.\run-all.ps1
```

**What this does:**
- Creates `phonetic_native` database
- Installs PostgreSQL extensions (pg_trgm, fuzzystrmatch, unaccent)
- Creates tables: `person`, `person_names`, `nickname_maps`, `staging_persons`
- Creates all search functions including the main `search_persons()` function
- Creates indexes for performance
- Sets up nickname tracking and business enhancements

**Custom database settings (optional):**
```powershell
# If your PostgreSQL settings are different:
$env:PGHOST = "localhost"
$env:PGPORT = 5432
$env:PGDATABASE = "phonetic_native"
$env:PGUSER = "postgres"
$env:PGPASSWORD = "your_password"
.\run-all.ps1
```

### 3. Load Test Data (96 Records)

```powershell
cd ..\sql
dotnet run --project LoadTestData.csproj
```

**What this loads:**
- 20 exact matches (John Smith, Jane Doe, etc.)
- 20 nickname variations (William, Robert, Elizabeth, etc.)
- 25 phonetic variants (Jon Smyth, Katherine, Steven, etc.)
- 20 fuzzy typos (Micheal, Wiliam, Jhon Smith, etc.)
- Business records with various flags (I=Individual, B=Business)

**Verify the data:**
```powershell
dotnet run --project LoadTestData.csproj check
```

### 4. Configure API Settings

Edit `sql-native-search\api\appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=phonetic_native;Username=postgres;Password=postgres"
  }
}
```

### 5. Run the API

```powershell
cd ..\api
dotnet run
```

API will start on: `http://localhost:5100`

### 6. Configure WebUI Settings

Edit `WebUI\appsettings.json`:

```json
{
  "ApiSettings": {
    "Approach": "NativeSQL",
    "NativeSQL": {
      "SearchBaseUrl": "http://localhost:5100"
    }
  }
}
```

### 7. Run the WebUI

```powershell
cd ..\..\WebUI
dotnet run
```

WebUI will start on: `http://localhost:5000` (or check console output)

## Quick Verification

### Test the Search Function Directly

```powershell
# From sql-native-search\sql directory
psql -h localhost -U postgres -d phonetic_native

-- Test query
SELECT full_name, match_type, similarity_score 
FROM search_persons('Bill', 10, 0.3, NULL, NULL, TRUE, TRUE);
```

**Expected results:**
- William Anderson (92% - NicknameExpansion)
- William Smith (92% - NicknameExpansion)
- Other Williams...

### Test API Endpoint

```powershell
curl http://localhost:5100/api/search?queryName=Bill&maxResults=10
```

### Test WebUI

Open browser: `http://localhost:5000`
- Search: "Bill"
- Toggle "Group by strategy" to see 4-column view
- Try min similarity: 85% (should show only exact + nicknames)

## Features to Test

### 1. List View (Default)
- Shows all results sorted by score
- Displays: Name, Score, County, Type, Why This Matched
- Color-coded: Green (95-100%), Blue (80-94%), Orange (50-79%), Red (30-59%)

### 2. Grouped View (Toggle ON)
- Top exact match highlighted
- 4 columns: Nickname | Fuzzy | Phonetic | Other
- Top 5 per column

### 3. Min Similarity Filter
- **30%** (default): Shows all match types including phonetic
- **50%**: Good fuzzy matches and better
- **85%**: Exact matches and nicknames only

### 4. Search Examples
- `Bill` → finds William (nickname)
- `John Smith` → finds exact + typos
- `Smith` → finds Smyth, Smythe (phonetic)
- `Katherine` → finds Catherine, Kathryn (alternate spellings)

## Troubleshooting

### Database Connection Issues

```powershell
# Test PostgreSQL connection
psql -h localhost -U postgres -d phonetic_native -c "SELECT 1"
```

### Check Database Objects

```sql
-- List all functions
SELECT proname FROM pg_proc WHERE proname LIKE '%person%';

-- Check tables
\dt

-- Check extensions
SELECT * FROM pg_extension;
```

### Reload Search Function (if modified)

```powershell
cd sql-native-search\sql
dotnet run --project LoadTestData.csproj reload
```

### Clean Duplicate Data

```powershell
cd sql-native-search\sql
# Note: CleanupDuplicates tool was used once, not included in repo
# If needed, run LoadTestData only once to avoid duplicates
```

## Project Structure

```
PhoneticAnalyzers-main/
├── sql-native-search/
│   ├── api/               # ASP.NET Core API (port 5100)
│   ├── sql/               # SQL scripts and utilities
│   │   ├── 01_extensions.sql
│   │   ├── 02_schema.sql
│   │   ├── 03_indexes.sql
│   │   ├── 04_functions.sql
│   │   ├── 05_search.sql  # Main search function
│   │   ├── 06_staging.sql
│   │   ├── 07_nickname_tracking.sql
│   │   ├── 08_apply_nicknames_bulk.sql
│   │   ├── 09_business_enhancements.sql
│   │   ├── LoadTestData.cs
│   │   ├── LoadTestData.csproj
│   │   ├── test-all-strategies.sql
│   │   └── test-queries.sql
│   └── scripts/
│       ├── run-all.ps1    # Main deployment script
│       └── deploy.ps1
└── WebUI/                 # Blazor WebAssembly UI
    ├── Pages/Search.razor
    └── Components/Search/
        ├── SearchForm.razor
        └── SearchResults.razor
```

## Environment Variables (Optional)

Set these if your PostgreSQL setup differs:

```powershell
$env:PGHOST = "localhost"
$env:PGPORT = 5432
$env:PGDATABASE = "phonetic_native"
$env:PGUSER = "postgres"
$env:PGPASSWORD = "your_password"
```

## Next Steps

After setup is complete:
1. Try all search examples from the help guide (click ? icon in UI)
2. Toggle between list and grouped views
3. Experiment with min similarity settings
4. Check performance with larger datasets

## Support

If you encounter issues:
1. Check PostgreSQL is running: `pg_ctl status`
2. Verify database exists: `psql -l | grep phonetic`
3. Check API logs in terminal
4. Review browser console for WebUI errors

---

**Setup complete!** Your Phonetic Analyzer is ready to use. 🎉
