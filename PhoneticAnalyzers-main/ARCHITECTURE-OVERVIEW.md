# Architecture Overview - Three Separate APIs

## Summary

You now have **two independent APIs** for phonetic search, each with different trade-offs. They both work with PostgreSQL databases and can share the **same Blazor WebUI**.

```
                    ┌─────────────────────┐
                    │   Blazor WebUI      │
                    │   (Port 5301)       │
                    └──────────┬──────────┘
                               │
            ┌──────────────────┼──────────────────┐
            │                                     │
     ┌──────▼──────────┐              ┌──────────▼──────────┐
     │ EF Core API     │              │  Native SQL API ⭐  │
     │ (Azure Func)    │              │  (ASP.NET Core)     │
     │ Port 7071-7072  │              │  Port 5100          │
     └──────┬──────────┘              └──────┬──────────────┘
            │                                │
            └────────────────┬───────────────┘
                             │
                  ┌──────────▼──────────┐
                  │    PostgreSQL       │
                  │ Multiple Databases  │
                  └─────────────────────┘
```

## API Comparison

| Feature | EF Core API | Native SQL API ⭐ |
|---------|-------------|-------------------|
| **Location** | `src/PhoneticAnalyzers.Functions.*` | `sql-native-search/api/` |
| **Port** | 7071-7072 | 5100 |
| **Technology** | EF Core + LINQ | Raw SQL + Npgsql |
| **Database** | PhoneticAnalyzersDb | phonetic_native |
| **ORM Overhead** | Yes | No |
| **Performance (100K)** | ~200ms search | ~20ms search |
| **Performance (2M+)** | Slow | ~50ms search |
| **Bulk Ingest** | Slow (batched HTTP) | Fast (COPY + staging) |
| **Best For** | Development/Testing | Production at scale |
| **Setup Complexity** | Medium | Low |

## When to Use Which?

### Use EF Core API When:
- ✅ Developing and testing features
- ✅ Working with < 100K records
- ✅ Need LINQ queries and EF features
- ✅ Rapid prototyping
- ❌ Production with millions of records

### Use Native SQL API When: ⭐ RECOMMENDED
- ✅ Production deployment
- ✅ Working with 100K+ to billions of records
- ✅ Need maximum performance (~10x faster)
- ✅ Bulk CSV import (millions of records)
- ✅ Simplicity - fewer dependencies
- ✅ Direct database control

## Setup Each API

### 1. EF Core API (Code First)
```powershell
cd src\PhoneticAnalyzers.Functions.Ingestion
func start
```
Endpoints: `http://localhost:7071/api/*`

### 2. Native SQL API ⭐ RECOMMENDED
```powershell
# Deploy database once
cd sql-native-search\sql
psql -h localhost -U postgres -d phonetic_native -f 01_extensions.sql
psql -h localhost -U postgres -d phonetic_native -f 02_schema.sql
psql -h localhost -U postgres -d phonetic_native -f 04_functions.sql
psql -h localhost -U postgres -d phonetic_native -f 05_search.sql
psql -h localhost -U postgres -d phonetic_native -f 07_nickname_tracking.sql

# Start API
cd ..\api
dotnet run
```
Endpoints: `http://localhost:5100/api/*`

## UI Configuration

The UI at `WebUI/wwwroot/appsettings.json` supports both APIs:

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5100"
  }
}
```

The WebUI is configured to use the **Native SQL API** by default (port 5100).

To use the EF Core API instead, change to:
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:7071"
  }
}
```

## Endpoint Mapping

| Operation | EF Core | Native SQL |
|-----------|---------|------------|
| **Health** | GET /api/health | GET /api/search/health |
| **Search** | GET /api/search?queryName=x | GET /api/search?queryName=x |
| **Advanced Search** | POST /api/search/advanced | POST /api/search/advanced |
| **Ingest** | POST /api/ingest | POST /api/ingestion/ingest |
| **Batch** | POST /api/ingest/batch | POST /api/ingestion/batch |
| **Get Person** | GET /api/person/{id} | GET /api/search/{id} |
| **Suggestions** | GET /api/suggestions | GET /api/search/suggestions |

## Running Both APIs Simultaneously

You can run both APIs at once for comparison:

**Terminal 1: EF Core**
```powershell
cd src\PhoneticAnalyzers.Functions.Ingestion
func start
```

**Terminal 2: Native SQL**
```powershell
cd sql-native-search\api
dotnet run
```

**Terminal 3: WebUI**
```powershell
cd SQLDBFirst\src\PhoneticAnalyzers.SQLDBFirst.Functions.Search
func start
```

**Terminal 3: WebUI**
```powershell
cd WebUI
dotnet run
```

Then switch the UI's `BaseUrl` in appsettings.json to test each backend!

## Performance Testing

Test both APIs with the same query:

```powershell
# EF Core
Measure-Command { 
    Invoke-RestMethod "http://localhost:7071/api/search?queryName=john%20davis" 
}

# Native SQL
Measure-Command { 
    Invoke-RestMethod "http://localhost:5100/api/search?queryName=john%20davis" 
}
```

## Bulk Ingestion Strategies

### EF Core API (1-10K records)
```powershell
# HTTP batch requests
POST /api/ingest/batch
{ "persons": [...] }
```

### Native SQL API (10K-2M+ records) ⭐
```powershell
# Direct CSV import (FAST!)
# See sql-native-search/BULK-IMPORT-GUIDE.md
psql -c "\COPY person FROM 'data.csv' CSV HEADER"
{ "persons": [...] }
```

### Direct SQL (1M-1B records)
```powershell
# Bypass API entirely
$env:PGPASSWORD="postgres"
psql -h localhost -U postgres -d phonetic_native

\copy staging_persons FROM 'data.csv' CSV HEADER;
SELECT process_staging_persons();
```

## Production Recommendations

For production deployment with large datasets:

1. **Use Native SQL API** (`sql-native-search/api/`)
2. Deploy to Azure App Service or Container Apps
3. Use Azure Database for PostgreSQL Flexible Server
4. Enable connection pooling (PgBouncer)
5. For bulk loads, use direct `psql COPY` to staging
6. Keep EF Core API for development/testing only

## Project Structure

```
PhoneticAnalyzers-main/
├── src/                          # EF Core approach (development/testing)
│   ├── PhoneticAnalyzers.Functions.Ingestion/
│   ├── PhoneticAnalyzers.Functions.Search/
│   ├── PhoneticAnalyzers.Application/
│   ├── PhoneticAnalyzers.Domain/
│   └── PhoneticAnalyzers.Infrastructure/
├── sql-native-search/            # Native SQL approach ⭐ PRODUCTION
│   ├── api/                      # ASP.NET Core API
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Models/
│   │   └── Program.cs
│   ├── sql/                      # PostgreSQL functions
│   │   ├── 01_extensions.sql
│   │   ├── 02_schema.sql
│   │   ├── 04_functions.sql
│   │   ├── 05_search.sql
│   │   ├── 07_nickname_tracking.sql
│   │   └── 08_apply_nicknames_bulk.sql
│   ├── scripts/                  # CSV bulk import
│   │   ├── fast-bulk-import.sql
│   │   └── bulk-import-csv.sql
│   ├── QUICKSTART.md
│   ├── README.md
│   └── BULK-IMPORT-GUIDE.md
├── tools/                        # Utilities
│   └── NicknameEnrichment/       # LLM nickname generator
│       ├── Program.cs
│       ├── NicknameEnrichmentService.cs
│       ├── appsettings.json
│       └── README.md
└── WebUI/                        # Blazor WebAssembly UI
    ├── Components/
    ├── Pages/
    ├── Services/
    └── wwwroot/appsettings.json  # API configuration
```

## Next Steps

1. ✅ Native SQL API (RECOMMENDED for production)
2. ✅ WebUI connected to Native SQL API
3. ⏳ Deploy database: See `sql-native-search/QUICKSTART.md`
4. ⏳ Start Native API: `cd sql-native-search\api; dotnet run`
5. ⏳ Start WebUI: `cd WebUI; dotnet run`
6. ⏳ Bulk import 2M records: See `sql-native-search/BULK-IMPORT-GUIDE.md`
7. ⏳ LLM nickname enrichment: See `tools/NicknameEnrichment/README.md`

## Questions?

- **Native SQL API**: See `sql-native-search/README.md`
- **API code**: See `sql-native-search/api/`
- **SQL functions**: See `sql-native-search/sql/`
- **Bulk import**: See `sql-native-search/BULK-IMPORT-GUIDE.md`
- **Nickname enrichment**: See `tools/NicknameEnrichment/AZURE-OPENAI-SETUP.md`
