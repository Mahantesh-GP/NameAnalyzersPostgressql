# PhoneticAnalyzers Database-First Search Functions

## Overview

Azure Functions for phonetic person search using **Database-First approach**.

- **Port**: 7074
- **Database**: phonetic_db_dbfirst
- **Runtime**: .NET 8 Isolated
- **Approach**: Database-First (SQL scripts, scaffolded models)

## Endpoints

### GET /api/persons/search
Search for persons using phonetic matching and fuzzy search algorithms.

**Query Parameters:**
- `name` (required): Name to search for
- `minSimilarity` (optional, default: 0.3): Minimum trigram similarity score (0.0-1.0)
- `expandNicknames` (optional, default: false): Enable nickname expansion

**Example Request:**
```
GET http://localhost:7074/api/persons/search?name=Bill&minSimilarity=0.5&expandNicknames=true
```

**Response (200 OK):**
```json
{
  "searchName": "Bill",
  "minSimilarity": 0.5,
  "expandNicknames": true,
  "resultCount": 12,
  "results": [
    {
      "personId": 42,
      "externalId": "EMP-12345",
      "fullName": "William Smith",
      "county": "King County",
      "matchType": "NicknameExpansion",
      "matchScore": 0.93,
      "matchedField": "NormalizedName",
      "matchedValue": "WILLIAM SMITH"
    },
    {
      "personId": 43,
      "externalId": "EMP-12346",
      "fullName": "Bill Johnson",
      "county": "Pierce County",
      "matchType": "Exact",
      "matchScore": 1.0,
      "matchedField": "NormalizedName",
      "matchedValue": "BILL JOHNSON"
    }
  ]
}
```

### GET /api/stats
Get database statistics.

**Response (200 OK):**
```json
{
  "totalPersons": 1247,
  "totalNicknameMappings": 256,
  "lastUpdated": "2025-11-12T10:30:00Z"
}
```

### GET /api/health
Health check endpoint.

**Response (200 OK):**
```
Database-First Search Functions - Healthy
```

### GET /api/diagnostics
Service diagnostics and feature information.

**Response (200 OK):**
```json
{
  "service": "PhoneticAnalyzers Database-First Search",
  "version": "1.0.0",
  "approach": "Database-First",
  "port": 7074,
  "database": "phonetic_db_dbfirst",
  "endpoints": [...],
  "algorithms": [...],
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
    "LocalHttpPort": 7074,
    "CORS": "*"
  }
}
```

## Running Locally

```powershell
# Navigate to function directory
cd SQLDBFirst/src/PhoneticAnalyzers.SQLDBFirst.Functions.Search

# Start function
func start --port 7074
```

Expected output:
```
Functions:
  DiagnosticsInfo: [GET] http://localhost:7074/api/diagnostics
  GetDatabaseStats: [GET] http://localhost:7074/api/stats
  Health: [GET] http://localhost:7074/api/health
  SearchPersons: [GET] http://localhost:7074/api/persons/search
```

## Testing

### Test Basic Search

```powershell
Invoke-RestMethod -Uri "http://localhost:7074/api/persons/search?name=William"
```

### Test Fuzzy Search with Similarity Threshold

```powershell
Invoke-RestMethod -Uri "http://localhost:7074/api/persons/search?name=Wiliam&minSimilarity=0.5"
```

### Test Nickname Expansion

```powershell
Invoke-RestMethod -Uri "http://localhost:7074/api/persons/search?name=Bill&expandNicknames=true"
```

### Test Database Stats

```powershell
Invoke-RestMethod -Uri "http://localhost:7074/api/stats"
```

### Test Health Check

```powershell
Invoke-RestMethod -Uri "http://localhost:7074/api/health"
```

## Search Algorithms

### 1. Exact Match (1.0 confidence)
Direct match on normalized name.

### 2. Token Contains (0.95 confidence)
Search name tokens present in person name.

### 3. Nickname Expansion (0.93 confidence)
Searches using all nickname variants:
- Bill → William, Billy, Will, Willy, Liam
- Bob → Robert, Bobby, Rob, Robbie

### 4. Primary Double Metaphone (0.9 confidence)
Phonetic matching using primary metaphone code.

### 5. Alternate Double Metaphone (0.85 confidence)
Phonetic matching using alternate metaphone code.

### 6. Beider-Morse (0.8 confidence)
Multi-linguistic phonetic matching.

### 7. Trigram Similarity (variable confidence)
PostgreSQL pg_trgm similarity scoring:
- Uses `similarity(name1, name2)` function
- Threshold controlled by `minSimilarity` parameter
- Handles typos and variations

## Match Type Classification

Results include match type for transparency:

```json
{
  "personId": 42,
  "externalId": "EMP-12345",
  "fullName": "William Smith",
  "matchType": "NicknameExpansion",
  "matchScore": 0.93,
  "matchedField": "NormalizedName",
  "matchedValue": "WILLIAM SMITH"
}
```

**Match Types:**
- `Exact` - Perfect match
- `TokenContains` - Name tokens match
- `NicknameExpansion` - Matched via nickname variant
- `PrimaryDoubleMetaphone` - Primary phonetic match
- `AlternateDoubleMetaphone` - Alternate phonetic match
- `BeiderMorse` - Beider-Morse phonetic match
- `TrigramSimilarity` - Fuzzy similarity match

## Performance

### Indexes Used

The search leverages multiple database indexes:

1. **GIN Trigram Index** on `normalized_name`
   - Fast fuzzy matching
   - Used by `similarity()` function

2. **B-tree Indexes** on phonetic fields
   - `primary_metaphone`
   - `alternate_metaphone`
   - `beider_morse`

3. **GIN Index** on `person_names.name_token`
   - Token-based searching

### Query Optimization

```sql
-- Efficient trigram search with index
SELECT * FROM person 
WHERE similarity(normalized_name, 'WILIAM') >= 0.5
ORDER BY similarity(normalized_name, 'WILIAM') DESC
LIMIT 100;

-- Metaphone index usage
SELECT * FROM person 
WHERE primary_metaphone = 'WLMN';
```

## Database-First Benefits

1. **DBA Control**: Schema changes managed via SQL scripts
2. **Performance Tuning**: DBAs can optimize indexes directly
3. **Audit Trail**: All changes tracked in SQL files
4. **No Migrations**: Scaffold models from database
5. **Compliance**: Meets enterprise security requirements

## Comparison with Code-First

| Aspect | Code-First (Port 7072) | Database-First (Port 7074) |
|--------|------------------------|----------------------------|
| Database | phonetic_db | phonetic_db_dbfirst |
| Schema Management | EF Core Migrations | SQL Scripts |
| Models | Manual C# classes | Scaffolded from DB |
| Index Management | EF Core fluent API | Direct SQL |
| Query Optimization | Limited | Full DBA control |
| Best For | Development | Enterprise, Production |

## Troubleshooting

### Connection String Error
```
InvalidOperationException: ConnectionStrings__PhoneticDb not configured
```
**Solution:** Update `ConnectionStrings__PhoneticDb` in local.settings.json

### Port 7074 Already in Use
```
System.IO.IOException: Failed to bind to address
```
**Solution:** Stop other services on port 7074 or change port in local.settings.json

### No Search Results
**Possible Causes:**
1. Database empty - Run seed scripts
2. Similarity threshold too high - Lower `minSimilarity` parameter
3. pg_trgm extension not installed - Check database setup

**Solution:**
```powershell
# Verify database has data
psql -U postgres -d phonetic_db_dbfirst -c "SELECT COUNT(*) FROM person;"

# Verify pg_trgm extension
psql -U postgres -d phonetic_db_dbfirst -c "SELECT * FROM pg_extension WHERE extname='pg_trgm';"
```

## Dependencies

- Microsoft.Azure.Functions.Worker (1.21.0)
- MediatR (12.2.0)
- FluentValidation (11.9.0)
- Npgsql.EntityFrameworkCore.PostgreSQL (8.0.0)
- SharpNL.Extensions.PhoneticMatching (1.2.0)

## See Also

- [Ingestion Functions (Port 7073)](../PhoneticAnalyzers.SQLDBFirst.Functions.Ingestion/README.md)
- [Application Layer](../PhoneticAnalyzers.SQLDBFirst.Application/README.md)
- [Infrastructure Layer](../PhoneticAnalyzers.SQLDBFirst.Infrastructure/README.md)
- [SQLDBFirst Setup Guide](../../README.md)
