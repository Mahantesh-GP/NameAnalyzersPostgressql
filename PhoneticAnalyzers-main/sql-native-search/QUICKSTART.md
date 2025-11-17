# Quick Start Guide - SQL Native API

## What You Have Now

You have **3 separate APIs** that can all connect to the same UI:

1. **EF Core API** (existing) - Port 7071-7074
2. **Database First API** (existing) - Port 7073-7074  
3. **Native SQL API** (new) - Port 5100 ⭐

## Quick Start Steps

### Step 1: Deploy the Database (One Time)

```powershell
cd C:\Learnings\PhoneticAnalyzer-short\PhoneticAnalyzers-main\sql-native-search\scripts
.\run-all.ps1
```

This creates database `phonetic_native` with all functions and indexes.

### Step 2: Start the Native API

```powershell
cd C:\Learnings\PhoneticAnalyzer-short\PhoneticAnalyzers-main\sql-native-search\api
.\start.ps1
```

API will be available at: `http://localhost:5100`  
Swagger UI: `http://localhost:5100/swagger`

### Step 3: Test It

Open browser to `http://localhost:5100/swagger` and try:

**Health Check:**
```
GET /api/ingest/health
```

**Ingest a Person:**
```
POST /api/ingest
{
  "externalId": "TEST-001",
  "fullName": "John Davis"
}
```

**Search:**
```
GET /api/search?queryName=john%20davis
```

### Step 4: Use with UI (Optional)

Your existing UI in `WebUI/` can already connect to this API!

Just update `WebUI/wwwroot/appsettings.json` to set:
```json
{
  "ApiSettings": {
    "Approach": "NativeSQL"
  }
}
```

Or add a toggle in your UI to switch between APIs at runtime.

## What's Different?

| Feature | EF Core API | Native SQL API |
|---------|-------------|----------------|
| Technology | Entity Framework | Raw SQL Functions |
| Performance | Good (<100K) | Excellent (1B+) |
| Port | 7071 | 5100 |
| Database | Any name | phonetic_native |
| Best For | Development | Production |

## All Endpoints

Base URL: `http://localhost:5100`

- `GET /api/ingest/health` - Health check
- `POST /api/ingest` - Ingest single person
- `POST /api/ingest/batch` - Ingest multiple
- `GET /api/search?queryName={name}` - Search
- `GET /api/search/{id}` - Get by ID

## Bulk Loading (Million+ Records)

For CSV files, bypass the API:

```powershell
$env:PGPASSWORD="postgres"
psql -h localhost -U postgres -d phonetic_native
```

Then in psql:
```sql
\copy staging_persons(external_id, full_name, county) 
FROM 'C:\\path\\to\\data.csv' 
WITH (FORMAT csv, HEADER true);

SELECT process_staging_persons();
```

## Troubleshooting

**Port 5100 already in use?**
Update `Properties/launchSettings.json` to use a different port.

**Cannot connect to database?**
Check PostgreSQL is running: `Get-Service postgresql*`

**Functions missing?**
Re-run: `.\scripts\run-all.ps1`
