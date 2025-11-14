# Phonetic Analyzers - Setup Guide

Complete setup instructions for running the Phonetic Analyzers application on a new machine.

## 📋 Table of Contents
- [Prerequisites](#prerequisites)
- [Database Setup](#database-setup)
- [Project Configuration](#project-configuration)
- [Database Seeding](#database-seeding)
- [Running the Application](#running-the-application)
- [Verification](#verification)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

Install the following software before proceeding:

### 1. .NET SDK
- **.NET 8 SDK** (for Azure Functions)
  - Download: https://dotnet.microsoft.com/download/dotnet/8.0
  - Verify: `dotnet --version` (should show 8.0.x)

- **.NET 9 SDK** (for Blazor WebUI)
  - Download: https://dotnet.microsoft.com/download/dotnet/9.0
  - Verify: `dotnet --list-sdks` (should show both 8.0.x and 9.0.x)

### 2. PostgreSQL Database
- **PostgreSQL 14 or higher**
  - Download: https://www.postgresql.org/download/
  - During installation, remember your `postgres` user password
  - Verify: `psql --version`

### 3. Azure Functions Core Tools
```powershell
npm install -g azure-functions-core-tools@4 --unsafe-perm true
```
- Verify: `func --version` (should show 4.x)

### 4. Git (if not already installed)
- Download: https://git-scm.com/downloads

---

## Database Setup

### Step 1: Clone the Repository
```powershell
git clone https://github.com/Mahantesh-GP/NameAnalyzersPostgressql.git
cd NameAnalyzersPostgressql
```

### Step 2: Create PostgreSQL Database
```powershell
# Open PowerShell and create the database
psql -U postgres -c "CREATE DATABASE phonetic_db;"

# Enable the pg_trgm extension (required for trigram similarity matching)
psql -U postgres -d phonetic_db -c "CREATE EXTENSION IF NOT EXISTS pg_trgm;"

# Verify the extension is installed
psql -U postgres -d phonetic_db -c "\dx"
```

**Note:** The database tables will be created automatically by Entity Framework Core migrations when you first run the application.

---

## Project Configuration

You need to configure connection strings in **4 files**. Replace `YOUR_PASSWORD` with your actual PostgreSQL password.

### 1. Ingestion Functions Configuration

**File:** `src/PhoneticAnalyzers.Functions.Ingestion/local.settings.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "PostgresConnection": "Host=localhost;Database=phonetic_db;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

### 2. Search Functions Configuration

**File:** `src/PhoneticAnalyzers.Functions.Search/local.settings.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "PostgresConnection": "Host=localhost;Database=phonetic_db;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

### 3. DataSeeder Configuration

**File:** `tools/DataSeeder/appsettings.json`

```json
{
  "ConnectionStrings": {
    "PostgresConnection": "Host=localhost;Database=phonetic_db;Username=postgres;Password=YOUR_PASSWORD"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

### 4. WebUI Configuration (Optional - Already Configured)

**File:** `WebUI/appsettings.Development.json`

This file should already have the correct settings:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ApiSettings": {
    "IngestionBaseUrl": "http://localhost:7071",
    "SearchBaseUrl": "http://localhost:7072"
  }
}
```

---

## Build the Solution

```powershell
# Restore NuGet packages
dotnet restore PhoneticAnalyzers.sln

# Build all projects
dotnet build PhoneticAnalyzers.sln
```

**Expected Output:** All projects should build successfully with 0 errors (warnings are OK).

---

## Database Seeding

### Step 1: Run the DataSeeder Tool

The DataSeeder will:
- Create all database tables (via EF Core migrations)
- Seed 250+ nickname mappings (William→Bill, Robert→Bob, etc.)
- Load sample person data from `sample_mortgage_data.csv`

```powershell
cd tools/DataSeeder
dotnet run
```

**Expected Output:**
```
Starting Database Seeding...
✓ Applying migrations...
✓ Seeding nickname mappings... (250+ mappings added)
✓ Loading person data from CSV... (1000+ records loaded)
Database seeding completed successfully!
```

### Step 2: Add Test Nickname Data

This adds 30 test persons with nickname variants (William/Bill/Billy, Robert/Bob/Bobby, etc.) for testing nickname expansion:

```powershell
# Navigate back to root directory
cd ../..

# Run the test data SQL script
psql -U postgres -d phonetic_db -f "tools/DataSeeder/test-nickname-data.sql"
```

**Expected Output:**
```
DELETE 30
INSERT 0 6
INSERT 0 5
INSERT 0 5
...
Test data summary: 30 test records, 7 unique first names
```

---

## Running the Application

You need to run **3 services** simultaneously. Open **3 separate PowerShell terminals**:

### Terminal 1: Ingestion Functions (Port 7071)

```powershell
cd src/PhoneticAnalyzers.Functions.Ingestion
func start
```

**Expected Output:**
```
Azure Functions Core Tools
Core Tools Version:       4.x.x
Function Runtime Version: 4.x.x

Functions:
  BatchIngest: [POST] http://localhost:7071/api/batch-ingest
  IngestPerson: [POST] http://localhost:7071/api/ingest
```

### Terminal 2: Search Functions (Port 7072)

```powershell
cd src/PhoneticAnalyzers.Functions.Search
func start --port 7072
```

**Expected Output:**
```
Azure Functions Core Tools
Core Tools Version:       4.x.x
Function Runtime Version: 4.x.x

Functions:
  SearchPersons: [POST] http://localhost:7072/api/search
```

### Terminal 3: Blazor WebUI (Port 5000/5001)

```powershell
cd WebUI
dotnet run
```

**Expected Output:**
```
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
      Now listening on: https://localhost:5001
```

---

## Verification

### Test 1: Open the Web Application
1. Open your browser to **http://localhost:5000**
2. You should see the Phonetic Analyzers homepage
3. Navigate to **Advanced Search** from the menu

### Test 2: Search for "Bill"
**Search Parameters:**
- Name to Search: `Bill`
- Min Similarity: `0.30`
- Max Results: `50`
- Checkboxes: ✅ Trigram, ✅ Nicknames, ✅ Details

**Expected Results:**
- **Bill Anderson** (Exact match, score 1.0)
- **Bill Thompson** (Exact match, score 1.0)
- **William Anderson** (NicknameExpansion, score 0.93) - reverse lookup
- Additional results via phonetic matching

### Test 3: Bulk Upload with Nickname Variants
1. Go to **Bulk Upload** page
2. Create a test CSV file `test.csv`:
   ```csv
   externalId,fullName,county,countyId,countyName,flag
   TEST-001,William Smith,Pierce County,53,Pierce County,I
   TEST-002,Robert Jones,King County,33,King County,I
   ```
3. Upload the file with **✅ Generate Nickname Variants During Upload** checked
4. Verify success message: "Uploaded X/X records"
5. Search for "William Smith" - should find William, Will, Bill, Billy, Willy, Liam Smith
6. Search for "Bill Smith" - should find Bill Smith (Exact) and William Smith (NicknameExpansion)

---

## Troubleshooting

### Issue: "Database does not exist"
**Solution:**
```powershell
psql -U postgres -c "CREATE DATABASE phonetic_db;"
psql -U postgres -d phonetic_db -c "CREATE EXTENSION IF NOT EXISTS pg_trgm;"
```

### Issue: "Unable to connect to PostgreSQL"
**Solution:** Check that PostgreSQL service is running:
```powershell
# Windows
Get-Service postgresql*

# If not running, start it:
Start-Service postgresql-x64-14  # Adjust version number
```

### Issue: "Functions not starting - Port already in use"
**Solution:** Kill the process using the port:
```powershell
# Find process using port 7071 or 7072
netstat -ano | findstr :7071

# Kill the process (replace PID with actual process ID)
taskkill /PID <PID> /F
```

### Issue: "No results when searching for Bill"
**Possible Causes:**
1. Database not seeded → Run DataSeeder: `cd tools/DataSeeder; dotnet run`
2. Test data not loaded → Run SQL script: `psql -U postgres -d phonetic_db -f "tools/DataSeeder/test-nickname-data.sql"`
3. Nicknames checkbox not enabled → Check ✅ Nicknames in search form
4. Min Similarity too high → Use 0.30 or lower

### Issue: "Unable to resolve service for type 'INicknameMapRepository'"
**Solution:** Rebuild the Ingestion Functions:
```powershell
cd src/PhoneticAnalyzers.Functions.Ingestion
dotnet build
```
This dependency is already registered in `Program.cs`.

### Issue: WebUI shows "Failed to connect to API"
**Solution:** Verify all 3 services are running:
- Ingestion Functions on http://localhost:7071
- Search Functions on http://localhost:7072
- WebUI on http://localhost:5000

Check API URLs in `WebUI/appsettings.Development.json`.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      Blazor WebUI                           │
│                  (Port 5000/5001)                           │
└──────────────┬─────────────────────┬────────────────────────┘
               │                     │
               ▼                     ▼
┌──────────────────────┐  ┌──────────────────────┐
│ Ingestion Functions  │  │  Search Functions    │
│    (Port 7071)       │  │    (Port 7072)       │
│                      │  │                      │
│ - BatchIngest        │  │ - SearchPersons      │
│ - IngestPerson       │  │                      │
└──────────┬───────────┘  └──────────┬───────────┘
           │                         │
           └──────────┬──────────────┘
                      ▼
           ┌────────────────────────┐
           │  PostgreSQL Database   │
           │    (phonetic_db)       │
           │                        │
           │ - person               │
           │ - person_names         │
           │ - person_bm            │
           │ - nickname_maps        │
           │ - name_aliases         │
           │ - name_alias_cache     │
           └────────────────────────┘
```

---

## Feature Highlights

### 1. **Nickname Expansion**
- Automatic nickname variant generation during bulk upload
- Bidirectional nickname matching (Bill ↔ William)
- 250+ nickname mappings for common names
- Configurable via "Generate Nickname Variants" checkbox

### 2. **Phonetic Matching**
- **Double Metaphone**: Primary and alternate phonetic codes
- **Beider-Morse**: Multi-language phonetic encoding
- **Trigram Similarity**: Character-level fuzzy matching using pg_trgm

### 3. **Match Types & Scoring**
- Exact: 1.0
- TokenContains: 0.95
- NicknameExpansion: 0.93
- PrimaryDoubleMetaphone: 0.9
- AlternateDoubleMetaphone: 0.85
- BeiderMorse: 0.8
- TrigramSimilarity: 0.3-0.99

---

## Additional Documentation

- **README.md** - Project overview and features
- **NICKNAME-VARIANT-GENERATION.md** - Detailed guide on nickname variant generation
- **Business-Architecture-Overview.md** - System architecture and design decisions
- **LLM-CALL-TIMING.md** - LLM enrichment configuration (optional feature)
- **OLLAMA-ENRICHMENT-GUIDE.md** - Local LLM setup for name enrichment (optional)

---

## Need Help?

If you encounter issues not covered in this guide:
1. Check the **Troubleshooting** section above
2. Review the logs in the terminal windows
3. Verify database connectivity: `psql -U postgres -d phonetic_db -c "SELECT COUNT(*) FROM person;"`
4. Ensure all 3 services are running without errors

---

**Last Updated:** November 12, 2025  
**Version:** 1.0.0
