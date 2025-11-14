# PhoneticAnalyzers Database-First Ingestion Functions

## Overview

Azure Functions for person data ingestion using **Database-First approach**.

- **Port**: 7073
- **Database**: phonetic_db_dbfirst
- **Runtime**: .NET 8 Isolated
- **Approach**: Database-First (SQL scripts, scaffolded models)

## Endpoints

### POST /api/persons
Ingest a single person with optional nickname variant generation.

**Request:**
```json
{
  "externalId": "EMP-12345",
  "fullName": "William Smith",
  "county": "King County",
  "expandNicknames": true
}
```

**Response (201 Created):**
```json
{
  "personId": 42,
  "messages": ["Person 'William Smith' ingested successfully"],
  "warnings": ["Generated 5 nickname variant(s) for 'William Smith'"]
}
```

### POST /api/persons/batch
Batch ingest multiple persons.

**Request:**
```json
[
  {
    "externalId": "EMP-001",
    "fullName": "Robert Johnson",
    "county": "Pierce County",
    "expandNicknames": true
  },
  {
    "externalId": "EMP-002",
    "fullName": "Elizabeth Davis",
    "expandNicknames": false
  }
]
```

**Response (200 OK):**
```json
{
  "successCount": 2,
  "isSuccess": true,
  "messages": ["Batch ingestion completed: 2 succeeded, 0 failed"],
  "warnings": [],
  "errors": []
}
```

### GET /api/health
Health check endpoint.

**Response (200 OK):**
```
Database-First Ingestion Functions - Healthy
```

### GET /api/diagnostics
Service diagnostics and feature information.

**Response (200 OK):**
```json
{
  "service": "PhoneticAnalyzers Database-First Ingestion",
  "version": "1.0.0",
  "approach": "Database-First",
  "port": 7073,
  "database": "phonetic_db_dbfirst",
  "endpoints": [...],
  "features": [...]
}
```

## Configuration

### local.settings.json

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings__PhoneticDb": "Host=localhost;Database=phonetic_db_dbfirst;Username=postgres;Password=your_password"
  },
  "Host": {
    "LocalHttpPort": 7073,
    "CORS": "*"
  }
}
```

## Running Locally

```powershell
# Navigate to function directory
cd SQLDBFirst/src/PhoneticAnalyzers.SQLDBFirst.Functions.Ingestion

# Start function
func start --port 7073
```

Expected output:
```
Functions:
  BatchIngest: [POST] http://localhost:7073/api/persons/batch
  DiagnosticsInfo: [GET] http://localhost:7073/api/diagnostics
  Health: [GET] http://localhost:7073/api/health
  IngestPerson: [POST] http://localhost:7073/api/persons
```

## Testing

### Test Single Person Ingestion

```powershell
$body = @{
    externalId = "TEST-001"
    fullName = "William Johnson"
    county = "King County"
    expandNicknames = $true
} | ConvertTo-Json

Invoke-RestMethod -Method Post `
    -Uri "http://localhost:7073/api/persons" `
    -Body $body `
    -ContentType "application/json"
```

### Test Batch Ingestion

```powershell
$batch = @(
    @{ externalId = "BATCH-001"; fullName = "Robert Smith"; expandNicknames = $true },
    @{ externalId = "BATCH-002"; fullName = "Elizabeth Jones"; expandNicknames = $true }
) | ConvertTo-Json

Invoke-RestMethod -Method Post `
    -Uri "http://localhost:7073/api/persons/batch" `
    -Body $batch `
    -ContentType "application/json"
```

### Test Health Check

```powershell
Invoke-RestMethod -Uri "http://localhost:7073/api/health"
```

## Features

### Nickname Variant Generation

When `expandNicknames: true`, automatically creates nickname variants:

**Input:**
```json
{
  "externalId": "12345",
  "fullName": "William Smith",
  "expandNicknames": true
}
```

**Generated Persons:**
- William Smith (12345)
- Bill Smith (12345-NICK-BILL)
- Billy Smith (12345-NICK-BILLY)
- Will Smith (12345-NICK-WILL)
- Willy Smith (12345-NICK-WILLY)
- Liam Smith (12345-NICK-LIAM)

### Phonetic Encoding

Automatically generates phonetic codes:
- **Double Metaphone**: Primary and alternate codes
- **Beider-Morse**: Multiple phonetic variations
- **Name Tokens**: Individual word analysis

### Validation

FluentValidation ensures data quality:
- ExternalId required, max 100 characters
- FullName required, max 200 characters, must have at least 2 names
- County max 50 characters (optional)

## Database-First Benefits

1. **DBA Control**: Schema changes managed via SQL scripts
2. **Audit Trail**: All schema changes tracked in version control
3. **No Migrations**: No EF Core migrations to manage
4. **Compliance**: Meets financial organization requirements
5. **Scaffolding**: Models auto-generated from database schema

## Comparison with Code-First

| Aspect | Code-First (Port 7071) | Database-First (Port 7073) |
|--------|------------------------|----------------------------|
| Database | phonetic_db | phonetic_db_dbfirst |
| Schema Management | EF Core Migrations | SQL Scripts |
| Models | Manual C# classes | Scaffolded from DB |
| Schema Changes | Add-Migration, Update-Database | SQL script, re-scaffold |
| DBA Control | Limited | Full |
| Best For | Development, Rapid prototyping | Enterprise, Production |

## Troubleshooting

### Connection String Error
```
InvalidOperationException: ConnectionStrings__PhoneticDb not configured
```
**Solution:** Update `ConnectionStrings__PhoneticDb` in local.settings.json

### Port 7073 Already in Use
```
System.IO.IOException: Failed to bind to address
```
**Solution:** Stop other services on port 7073 or change port in local.settings.json

### Database Does Not Exist
```
Npgsql.NpgsqlException: database "phonetic_db_dbfirst" does not exist
```
**Solution:** Run SQL scripts to create database:
```powershell
cd SQLDBFirst/DatabaseScripts
psql -U postgres -f 001_CreateSchema.sql
psql -U postgres -d phonetic_db_dbfirst -f 002_SeedNicknames.sql
psql -U postgres -d phonetic_db_dbfirst -f 003_SeedTestData.sql
```

## Dependencies

- Microsoft.Azure.Functions.Worker (1.21.0)
- MediatR (12.2.0)
- FluentValidation (11.9.0)
- Npgsql.EntityFrameworkCore.PostgreSQL (8.0.0)
- SharpNL.Extensions.PhoneticMatching (1.2.0)

## See Also

- [Search Functions (Port 7074)](../PhoneticAnalyzers.SQLDBFirst.Functions.Search/README.md)
- [Application Layer](../PhoneticAnalyzers.SQLDBFirst.Application/README.md)
- [Infrastructure Layer](../PhoneticAnalyzers.SQLDBFirst.Infrastructure/README.md)
- [SQLDBFirst Setup Guide](../../README.md)
