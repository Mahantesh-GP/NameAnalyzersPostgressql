# 🚀 Database-First Setup Guide - Step by Step

This guide will walk you through setting up the Database-First approach from scratch.

## 📋 Prerequisites

- ✅ PostgreSQL 17 installed at: `C:\Program Files\PostgreSQL\17`
- ✅ .NET 8 SDK installed
- ✅ Azure Functions Core Tools installed

## 🎯 Quick Start (Automated)

### Step 1: Run the Setup Script

Open PowerShell in the `SQLDBFirst` folder and run:

```powershell
.\SETUP-DATABASE-FIRST.ps1
```

This script will:
1. ✅ Check PostgreSQL connection
2. ✅ Create `phonetic_db_dbfirst` database
3. ✅ Run all SQL scripts (schema, nicknames, test data)
4. ✅ Verify database content
5. ✅ Build all SQLDBFirst projects

**Note**: You'll be prompted for your PostgreSQL password when connecting.

---

### Step 2: Update PostgreSQL Password

Update the password in configuration files:

```powershell
.\UPDATE-PASSWORD.ps1 -Password "your_postgres_password"
```

This updates:
- `src\PhoneticAnalyzers.SQLDBFirst.Functions.Search\local.settings.json`
- `src\PhoneticAnalyzers.SQLDBFirst.Functions.Ingestion\local.settings.json`

---

### Step 3: Start the Functions

**Terminal 1 - Search Function:**
```powershell
cd src\PhoneticAnalyzers.SQLDBFirst.Functions.Search
func start --port 7072
```

**Terminal 2 - Ingestion Function:**
```powershell
cd src\PhoneticAnalyzers.SQLDBFirst.Functions.Ingestion
func start --port 7073
```

---

### Step 4: Update WebUI (Optional)

To use Database-First with the WebUI, update `WebUI\wwwroot\appsettings.json`:

```json
{
  "ApiBaseUrl": "http://localhost:7072/api"
}
```

Then start the WebUI:
```powershell
cd WebUI
dotnet run
```

---

## 🔧 Manual Setup (Step by Step)

If you prefer to run each step manually:

### 1. Create Database

```powershell
$pgPath = "C:\Program Files\PostgreSQL\17\bin"
& "$pgPath\psql.exe" -U postgres -c "CREATE DATABASE phonetic_db_dbfirst;"
```

### 2. Run SQL Scripts

```powershell
cd DatabaseScripts

# Create schema (tables, indexes, extensions)
& "$pgPath\psql.exe" -U postgres -d phonetic_db_dbfirst -f 001_CreateSchema.sql

# Seed nickname mappings (250+ nicknames)
& "$pgPath\psql.exe" -U postgres -d phonetic_db_dbfirst -f 002_SeedNicknames.sql

# Seed test data (30 persons)
& "$pgPath\psql.exe" -U postgres -d phonetic_db_dbfirst -f 003_SeedTestData.sql

cd ..
```

### 3. Verify Database

```powershell
# Check person records
& "$pgPath\psql.exe" -U postgres -d phonetic_db_dbfirst -c "SELECT COUNT(*) FROM person;"
# Should return: 30

# Check nickname mappings
& "$pgPath\psql.exe" -U postgres -d phonetic_db_dbfirst -c "SELECT COUNT(*) FROM nickname_maps;"
# Should return: 250+

# List some test persons
& "$pgPath\psql.exe" -U postgres -d phonetic_db_dbfirst -c "SELECT person_id, full_name, county FROM person LIMIT 10;"
```

### 4. Update Connection Strings

Edit both files and replace `your_password_here` with your PostgreSQL password:

**File 1:** `src\PhoneticAnalyzers.SQLDBFirst.Functions.Search\local.settings.json`
```json
{
  "Values": {
    "ConnectionStrings__PhoneticDb": "Host=localhost;Database=phonetic_db_dbfirst;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

**File 2:** `src\PhoneticAnalyzers.SQLDBFirst.Functions.Ingestion\local.settings.json`
```json
{
  "Values": {
    "ConnectionStrings__PhoneticDb": "Host=localhost;Database=phonetic_db_dbfirst;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

### 5. Build Projects

```powershell
cd src

dotnet build PhoneticAnalyzers.SQLDBFirst.Domain\PhoneticAnalyzers.SQLDBFirst.Domain.csproj
dotnet build PhoneticAnalyzers.SQLDBFirst.Infrastructure\PhoneticAnalyzers.SQLDBFirst.Infrastructure.csproj
dotnet build PhoneticAnalyzers.SQLDBFirst.Application\PhoneticAnalyzers.SQLDBFirst.Application.csproj
dotnet build PhoneticAnalyzers.SQLDBFirst.Functions.Search\PhoneticAnalyzers.SQLDBFirst.Functions.Search.csproj
dotnet build PhoneticAnalyzers.SQLDBFirst.Functions.Ingestion\PhoneticAnalyzers.SQLDBFirst.Functions.Ingestion.csproj

cd ..
```

---

## ✅ Verification Steps

### Test Database Connection

```powershell
$pgPath = "C:\Program Files\PostgreSQL\17\bin"
& "$pgPath\psql.exe" -U postgres -d phonetic_db_dbfirst -c "
SELECT 
    'Persons' as table_name, COUNT(*) as count FROM person
UNION ALL
SELECT 
    'Nicknames' as table_name, COUNT(*) as count FROM nickname_maps
UNION ALL
SELECT 
    'Person Names' as table_name, COUNT(*) as count FROM person_names;
"
```

Expected output:
```
   table_name   | count
----------------+-------
 Persons        |    30
 Nicknames      |   250+
 Person Names   |   60+
```

### Test Search Function

Once the Search Function is running on port 7072:

```powershell
# Test counties endpoint
curl http://localhost:7072/api/counties

# Test search endpoint
curl -X POST http://localhost:7072/api/search `
  -H "Content-Type: application/json" `
  -d '{
    "QueryName": "john",
    "MaxResults": 10,
    "MinSimilarityThreshold": 0.3,
    "IncludeTrigramSimilarity": true,
    "ExpandNicknames": true,
    "IncludeMatchDetails": true
  }'
```

---

## 🗂️ Database Schema Overview

### Tables Created:

1. **person** - Main person records with phonetic codes
   - 30 test records seeded
   - Includes variants with nicknames (e.g., "John Smith", "Johnny Smith", "Jack Smith")

2. **nickname_maps** - Canonical name to nickname mappings
   - 250+ nickname mappings
   - Examples: Robert→Bob, William→Bill, Elizabeth→Liz

3. **person_names** - Token-based name storage
   - Each word in a name stored separately
   - Enables partial name matching

4. **person_beider_morse** - Beider-Morse phonetic codes
   - Multiple phonetic variations per name

### Key Features:

- ✅ pg_trgm extension enabled for fuzzy matching
- ✅ GIN indexes on normalized_name for fast searches
- ✅ Triggers for automatic timestamp updates
- ✅ Foreign key constraints with CASCADE delete
- ✅ Comments on tables and columns for documentation

---

## 🆚 Database-First vs Code-First

| Feature | Code-First (src/) | Database-First (SQLDBFirst/) |
|---------|-------------------|------------------------------|
| **Database** | phonetic_db | phonetic_db_dbfirst |
| **Port (Search)** | 7071 | 7072 |
| **Port (Ingestion)** | 7071 | 7073 |
| **Schema Management** | EF Core Migrations | SQL Scripts |
| **Model Generation** | Manual C# classes | Scaffolded from DB |

Both approaches share the **same WebUI** - just update the `ApiBaseUrl` to switch between them!

---

## 🐛 Troubleshooting

### Issue: "psql is not recognized"

The setup script automatically adds PostgreSQL to PATH. If you still get this error:

```powershell
$env:Path = "C:\Program Files\PostgreSQL\17\bin;$env:Path"
```

### Issue: "password authentication failed"

1. Check your PostgreSQL password
2. Update local.settings.json files:
   ```powershell
   .\UPDATE-PASSWORD.ps1 -Password "correct_password"
   ```

### Issue: "database already exists"

The setup script will prompt you to drop and recreate. Or manually:

```powershell
$pgPath = "C:\Program Files\PostgreSQL\17\bin"
& "$pgPath\psql.exe" -U postgres -c "DROP DATABASE IF EXISTS phonetic_db_dbfirst;"
& "$pgPath\psql.exe" -U postgres -c "CREATE DATABASE phonetic_db_dbfirst;"
```

### Issue: Build errors in Functions

Ensure all dependencies are restored:

```powershell
cd src
dotnet restore
dotnet build
```

### Issue: Function won't start

1. Check if Azure Storage Emulator is running (or use `UseDevelopmentStorage=true`)
2. Verify port is not already in use:
   ```powershell
   netstat -ano | findstr "7072"
   ```

---

## 📚 Next Steps

Once setup is complete:

1. ✅ Browse to http://localhost:7072/api/counties to verify API is working
2. ✅ Test search with sample queries
3. ✅ Connect WebUI to Database-First endpoint
4. ✅ Compare results with Code-First approach

For more details, see:
- `QUICK-START.md` - Quick reference guide
- `README.md` - Full Database-First documentation
- `DatabaseScripts/` - All SQL scripts with comments

---

## 🎉 Success!

If everything is working, you should see:
- ✅ Database created with schema, nicknames, and test data
- ✅ Both Functions running on ports 7072 and 7073
- ✅ WebUI can search using Database-First backend
- ✅ Results showing phonetic matches for test names like "John", "Bob", "Catherine"

**Happy searching! 🔍**
