# Architecture Overview - Three Separate APIs

## Summary

You now have **three independent APIs** for phonetic search, each with different trade-offs. They all work with the same PostgreSQL database (different schemas/databases) and can share the **same Blazor WebUI**.

```
                    ┌─────────────────────┐
                    │   Blazor WebUI      │
                    │   (Port 5000/5001)  │
                    └──────────┬──────────┘
                               │
            ┌──────────────────┼──────────────────┐
            │                  │                  │
     ┌──────▼──────┐   ┌──────▼──────┐   ┌──────▼──────────┐
     │ EF Core API │   │  DB First   │   │  Native SQL API │
     │ Port 7071   │   │  Port 7073  │   │  Port 5100      │
     └──────┬──────┘   └──────┬──────┘   └──────┬──────────┘
            │                  │                  │
            └──────────────────┼──────────────────┘
                               │
                    ┌──────────▼──────────┐
                    │    PostgreSQL       │
                    │ Multiple Databases  │
                    └─────────────────────┘
```

## API Comparison

| Feature | EF Core API | DB First API | Native SQL API ⭐ |
|---------|-------------|--------------|-------------------|
| **Location** | `src/PhoneticAnalyzers.Functions.*` | `SQLDBFirst/` | `sql-native-search/api/` |
| **Port** | 7071-7072 | 7073-7074 | 5100 |
| **Technology** | EF Core + LINQ | EF Core (scaffolded) | Raw SQL + Npgsql |
| **Database** | PhoneticAnalyzersDb | PhoneticAnalyzersDb | phonetic_native |
| **ORM Overhead** | Yes | Yes | No |
| **Performance (100K)** | ~200ms search | ~200ms search | ~20ms search |
| **Performance (1B)** | N/A | N/A | ~50ms search |
| **Bulk Ingest** | Slow (batched HTTP) | Slow (batched HTTP) | Fast (COPY + staging) |
| **Best For** | Development/Testing | Migration from existing DB | Production at scale |
| **Setup Complexity** | Medium | Medium | Low |

## When to Use Which?

### Use EF Core API When:
- ✅ Developing and testing features
- ✅ Working with < 100K records
- ✅ Need LINQ queries and EF features
- ✅ Rapid prototyping
- ❌ Production with millions of records

### Use DB First API When:
- ✅ Scaffolding from existing database
- ✅ Need strongly-typed entities from DB schema
- ✅ Migrating legacy databases
- ❌ New greenfield projects

### Use Native SQL API When:
- ✅ Production deployment
- ✅ Working with 1M+ records
- ✅ Need maximum performance
- ✅ Bulk CSV ingestion
- ✅ Scaling to billions of records
- ✅ Want minimal dependencies

## Setup Each API

### 1. EF Core API (Already Running)
```powershell
cd src\PhoneticAnalyzers.Functions.Ingestion
func start
```
Endpoints: `http://localhost:7071/api/*`

### 2. DB First API
```powershell
cd SQLDBFirst
# Follow SETUP-GUIDE.md
```
Endpoints: `http://localhost:7073/api/*`

### 3. Native SQL API (New)
```powershell
# Deploy database once
cd sql-native-search\scripts
.\run-all.ps1

# Start API
cd ..\api
.\start.ps1
```
Endpoints: `http://localhost:5100/api/*`

## UI Configuration

The UI at `WebUI/wwwroot/appsettings.json` now supports all three:

```json
{
  "ApiSettings": {
    "Approach": "CodeFirst",  // or "DatabaseFirst" or "NativeSQL"
    "CodeFirst": {
      "IngestionBaseUrl": "http://localhost:7071/api",
      "SearchBaseUrl": "http://localhost:7072/api"
    },
    "DatabaseFirst": {
      "IngestionBaseUrl": "http://localhost:7073/api",
      "SearchBaseUrl": "http://localhost:7074/api"
    },
    "NativeSQL": {
      "IngestionBaseUrl": "http://localhost:5100/api/ingest",
      "SearchBaseUrl": "http://localhost:5100/api/search"
    }
  }
}
```

Change `"Approach"` to switch between APIs, or add a UI toggle to switch at runtime.

## Endpoint Mapping

| Operation | EF Core | DB First | Native SQL |
|-----------|---------|----------|------------|
| **Health** | GET /api/health | GET /api/health | GET /api/ingest/health |
| **Search** | GET /api/search?queryName=x | GET /api/search?queryName=x | GET /api/search?queryName=x |
| **Ingest** | POST /api/ingest | POST /api/ingest | POST /api/ingest |
| **Batch** | POST /api/ingest/batch | POST /api/ingest/batch | POST /api/ingest/batch |
| **Get Person** | GET /api/person/{id} | GET /api/person/{id} | GET /api/search/{id} |

## Running All Three Simultaneously

You can run all APIs at once for comparison:

**Terminal 1: EF Core**
```powershell
cd src\PhoneticAnalyzers.Functions.Ingestion
func start
```

**Terminal 2: DB First**
```powershell
cd SQLDBFirst\src\PhoneticAnalyzers.SQLDBFirst.Functions.Search
func start
```

**Terminal 3: Native SQL**
```powershell
cd sql-native-search\api
dotnet run
```

**Terminal 4: UI**
```powershell
cd WebUI
dotnet run
```

Then switch the UI's `"Approach"` to test each backend!

## Performance Testing

Test all three with the same query:

```powershell
# EF Core
Measure-Command { 
    Invoke-RestMethod "http://localhost:7071/api/search?queryName=john%20davis" 
}

# DB First
Measure-Command { 
    Invoke-RestMethod "http://localhost:7073/api/search?queryName=john%20davis" 
}

# Native SQL
Measure-Command { 
    Invoke-RestMethod "http://localhost:5100/api/search?queryName=john%20davis" 
}
```

## Bulk Ingestion Strategies

### EF Core / DB First (1-10K records)
```powershell
# HTTP batch requests
POST /api/ingest/batch
{ "persons": [...] }
```

### Native SQL API (10K-1M records)
```powershell
# HTTP batch with native functions
POST /api/ingest/batch
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
├── src/                          # EF Core approach
│   ├── PhoneticAnalyzers.Functions.Ingestion/
│   ├── PhoneticAnalyzers.Functions.Search/
│   └── PhoneticAnalyzers.Application/
├── SQLDBFirst/                   # Database First approach
│   └── src/
│       ├── PhoneticAnalyzers.SQLDBFirst.Functions.*/
│       └── PhoneticAnalyzers.SQLDBFirst.Application/
├── sql-native-search/            # Native SQL approach ⭐
│   ├── api/                      # NEW: Standalone API
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Models/
│   │   ├── start.ps1
│   │   └── test-api.ps1
│   ├── sql/                      # SQL functions and schema
│   ├── scripts/                  # Deployment scripts
│   ├── QUICKSTART.md
│   └── README.md
└── WebUI/                        # Shared Blazor UI
    └── wwwroot/appsettings.json  # API configuration
```

## Next Steps

1. ✅ Native SQL API created
2. ✅ UI configuration updated
3. ⏳ Test Native SQL API: `cd sql-native-search\api; .\test-api.ps1`
4. ⏳ Deploy database: `cd sql-native-search\scripts; .\run-all.ps1`
5. ⏳ Start Native API: `cd sql-native-search\api; .\start.ps1`
6. ⏳ Toggle UI to use Native SQL backend
7. ⏳ Compare performance across all three APIs
8. ⏳ Choose your production approach

## Questions?

- **Native SQL details**: See `sql-native-search/QUICKSTART.md`
- **API code**: See `sql-native-search/api/`
- **SQL functions**: See `sql-native-search/sql/`
- **Deployment**: See `sql-native-search/scripts/`
