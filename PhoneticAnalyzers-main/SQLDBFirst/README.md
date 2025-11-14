# Database-First Phonetic Analyzer

This is a **Database-First** implementation of the Phonetic Analyzer using PostgreSQL. The database schema is managed by DBAs through SQL scripts, and C# models are scaffolded from the existing database.

## 🔄 Database-First vs Code-First

| Aspect | Code-First (../src) | **Database-First (This Folder)** |
|--------|-------------------|--------------------------------|
| **Schema Source of Truth** | C# Entity classes | PostgreSQL Database |
| **Schema Changes** | Modify C# → Add-Migration → Update-Database | Write SQL script → Run on DB → Scaffold models |
| **Migrations** | EF Core migrations (automatic) | SQL scripts (manual, DBA-managed) |
| **Model Generation** | Manual C# classes | Auto-generated from database |
| **DBA Control** | Limited | Full control over schema |
| **Best For** | Rapid development, small teams | Enterprise, compliance, DBA-managed environments |

## 📁 Folder Structure

```
SQLDBFirst/
├── DatabaseScripts/              ← SQL scripts (DBA-managed)
│   ├── 001_CreateSchema.sql      ← Tables, indexes, extensions, triggers
│   ├── 002_SeedNicknames.sql     ← 250+ nickname mappings
│   ├── 003_SeedTestData.sql      ← 30 test persons with nickname variants
│   └── 004_StoredProcs.sql       ← Optional stored procedures
├── Models/                       ← Scaffolded C# models (auto-generated)
│   ├── Person.cs                 ← Generated from database
│   ├── NicknameMap.cs            ← Generated from database
│   └── PhoneticDbContext.cs      ← Generated DbContext
├── scaffold-models.ps1           ← PowerShell script to regenerate models
└── README.md                     ← This file
```

## 🚀 Getting Started

### Prerequisites

1. **PostgreSQL 14+** installed
2. **.NET 8 SDK** installed
3. **EF Core Tools** installed:
   ```powershell
   dotnet tool install --global dotnet-ef
   ```

### Step 1: Create Database

```powershell
# Create a NEW database (separate from Code-First version)
psql -U postgres -c "CREATE DATABASE phonetic_db_dbfirst;"
```

**Important**: This creates a **separate database** called `phonetic_db_dbfirst`. Your existing Code-First database (`phonetic_db`) remains untouched.

### Step 2: Run SQL Scripts

Execute the SQL scripts in order:

```powershell
# Navigate to DatabaseScripts folder
cd SQLDBFirst/DatabaseScripts

# 1. Create schema (tables, indexes, extensions)
psql -U postgres -d phonetic_db_dbfirst -f 001_CreateSchema.sql

# 2. Seed nickname mappings (250+ mappings)
psql -U postgres -d phonetic_db_dbfirst -f 002_SeedNicknames.sql

# 3. Seed test data (30 test persons)
psql -U postgres -d phonetic_db_dbfirst -f 003_SeedTestData.sql
```

**Expected Output**:
```
✓ 6 tables created
✓ 20+ indexes created
✓ pg_trgm extension enabled
✓ 250+ nickname mappings inserted
✓ 30 test persons inserted
```

### Step 3: Scaffold C# Models

Run the PowerShell scaffolding script:

```powershell
# Navigate back to SQLDBFirst folder
cd ..

# Run scaffold script
.\scaffold-models.ps1
```

**What This Does**:
- Connects to `phonetic_db_dbfirst`
- Reads all table schemas
- Generates C# entity classes in `Models/` folder
- Generates `PhoneticDbContext.cs`
- Uses Data Annotations for validation

**Generated Files**:
```
Models/
├── Person.cs              ← Person entity
├── PersonName.cs          ← Name tokens entity
├── PersonBm.cs            ← Beider-Morse codes entity
├── NicknameMap.cs         ← Nickname mappings entity
├── NameAlias.cs           ← Name aliases entity
├── NameAliasCache.cs      ← Alias cache entity
└── PhoneticDbContext.cs   ← DbContext with DbSets
```

### Step 4: Review Generated Models

Open `Models/Person.cs` to see the generated entity:

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneticAnalyzers.SQLDBFirst.Models;

[Table("person")]
public partial class Person
{
    [Key]
    [Column("person_id")]
    public long PersonId { get; set; }

    [Required]
    [Column("external_id")]
    [StringLength(255)]
    public string ExternalId { get; set; }

    [Required]
    [Column("full_name")]
    [StringLength(500)]
    public string FullName { get; set; }

    // ... more properties
}
```

### Step 5: Add Custom Logic (Optional)

Create **partial classes** for custom logic that won't be overwritten when re-scaffolding:

```csharp
// Models/Person.Custom.cs
namespace PhoneticAnalyzers.SQLDBFirst.Models;

public partial class Person
{
    // Add custom methods, computed properties, etc.
    public string FirstName => FullName.Split(' ')[0];
    
    public bool IsNicknameVariant() => 
        ExternalId.Contains("-NICK-");
}
```

## 🔄 Schema Change Workflow

When database schema needs to change:

### 1. DBA Creates SQL Script

```sql
-- DatabaseScripts/005_AddEmailColumn.sql
-- Date: 2025-11-15
-- Description: Add email column to person table

\c phonetic_db_dbfirst;

-- Add email column
ALTER TABLE person 
ADD COLUMN email VARCHAR(255);

-- Add index
CREATE INDEX idx_person_email ON person(email);

-- Add comment
COMMENT ON COLUMN person.email IS 'Person email address';
```

### 2. DBA Runs Script

```powershell
psql -U postgres -d phonetic_db_dbfirst -f DatabaseScripts/005_AddEmailColumn.sql
```

### 3. Developer Re-scaffolds Models

```powershell
.\scaffold-models.ps1
```

**Result**: `Person.cs` now includes the `Email` property automatically!

### 4. Commit to Git

```powershell
git add DatabaseScripts/005_AddEmailColumn.sql
git add Models/
git commit -m "Add email column to person table"
```

## 📝 SQL Script Naming Convention

```
NNN_DescriptiveName.sql

Examples:
001_CreateSchema.sql          ← Initial schema
002_SeedNicknames.sql         ← Reference data
003_SeedTestData.sql          ← Test data
004_StoredProcs.sql           ← Stored procedures
005_AddEmailColumn.sql        ← Future change
006_ModifyIndexes.sql         ← Future change
```

**Rules**:
- ✅ Number scripts sequentially
- ✅ Include date and description in comments
- ✅ Make scripts idempotent (can run multiple times safely)
- ❌ Never modify old scripts - create new ones
- ✅ Include rollback scripts for destructive changes

## 🎯 Advantages for Financial Organizations

### 1. **DBA Control**
- DBAs have full control over schema
- All changes reviewed by database team
- Performance tuning by database experts

### 2. **Compliance & Audit**
- Explicit SQL scripts are auditable
- Clear change history in source control
- No "magic" migrations - everything is visible

### 3. **Enterprise Standards**
- Follows traditional database-first methodology
- Compatible with existing DBA workflows
- Integrates with database deployment pipelines

### 4. **Rollback Strategy**
```sql
-- 005_AddEmailColumn.sql
ALTER TABLE person ADD COLUMN email VARCHAR(255);

-- 005_AddEmailColumn_Rollback.sql
ALTER TABLE person DROP COLUMN email;
```

### 5. **Multi-Environment Deployment**
```powershell
# Dev environment
psql -h dev-db -U postgres -d phonetic_db_dbfirst -f 005_AddEmailColumn.sql

# QA environment  
psql -h qa-db -U postgres -d phonetic_db_dbfirst -f 005_AddEmailColumn.sql

# Production environment (after approval)
psql -h prod-db -U postgres -d phonetic_db_dbfirst -f 005_AddEmailColumn.sql
```

## 🔍 Testing the Setup

### Test 1: Verify Database

```sql
-- Connect to database
psql -U postgres -d phonetic_db_dbfirst

-- Check tables
\dt

-- Expected: person, person_names, person_bm, nickname_maps, name_aliases, name_alias_cache

-- Count records
SELECT COUNT(*) FROM person;           -- Should be 30 (test data)
SELECT COUNT(*) FROM nickname_maps;    -- Should be 250+
```

### Test 2: Test Trigram Search

```sql
-- Test pg_trgm extension
SELECT similarity('WILLIAM', 'BILL');    -- Should return ~0.4

-- Find similar names
SELECT full_name, similarity(normalized_name, 'WILLIAM ANDERSON') AS score
FROM person
WHERE similarity(normalized_name, 'WILLIAM ANDERSON') > 0.3
ORDER BY score DESC
LIMIT 10;
```

### Test 3: Test Nickname Mappings

```sql
-- Find all nicknames for WILLIAM
SELECT canonical_name, nickname, confidence
FROM nickname_maps
WHERE canonical_name = 'WILLIAM'
ORDER BY nickname;

-- Should show: BILL, BILLY, LIAM, WILL, WILLY
```

### Test 4: Verify Scaffolded Models

```powershell
# Build a simple test console app
dotnet new console -n DBFirstTest
cd DBFirstTest

# Add EF Core package
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# Copy scaffolded models
# Create Program.cs to test:
```

```csharp
using PhoneticAnalyzers.SQLDBFirst.Models;
using Microsoft.EntityFrameworkCore;

var optionsBuilder = new DbContextOptionsBuilder<PhoneticDbContext>();
optionsBuilder.UseNpgsql("Host=localhost;Database=phonetic_db_dbfirst;Username=postgres;Password=YOUR_PASSWORD");

using var context = new PhoneticDbContext(optionsBuilder.Options);

// Test query
var persons = await context.Persons
    .Where(p => p.NormalizedName.Contains("WILLIAM"))
    .ToListAsync();

Console.WriteLine($"Found {persons.Count} persons with WILLIAM in name");
foreach (var person in persons)
{
    Console.WriteLine($"- {person.FullName} (ID: {person.ExternalId})");
}
```

## 📚 Key Features

### 1. **pg_trgm Extension**
- ✅ Trigram similarity matching
- ✅ GIN indexes for fast fuzzy search
- ✅ `similarity()` function for scoring

### 2. **Nickname Expansion**
- ✅ 250+ nickname mappings
- ✅ Bidirectional lookup (WILLIAM ↔ BILL)
- ✅ High confidence scores (0.95)

### 3. **Phonetic Encoding**
- ✅ Double Metaphone (primary + alternate codes)
- ✅ Beider-Morse (multi-language support)
- ✅ Indexed for fast lookup

### 4. **Performance Optimizations**
- ✅ GIN trigram indexes
- ✅ B-tree indexes on common columns
- ✅ Foreign key indexes
- ✅ Automatic `updated_utc` trigger

## 🔧 Maintenance

### Re-scaffolding Models

Run anytime database schema changes:

```powershell
.\scaffold-models.ps1
```

### Checking Database Version

```sql
-- View all applied scripts (manual tracking)
SELECT * FROM schema_version;  -- If you create this table

-- Or use comments:
SELECT 
    obj_description(oid, 'pg_class') as table_description
FROM pg_class
WHERE relname = 'person';
```

### Backup & Restore

```powershell
# Backup
pg_dump -U postgres -d phonetic_db_dbfirst -f backup.sql

# Restore
psql -U postgres -d phonetic_db_dbfirst -f backup.sql
```

## 🆚 Comparison with Code-First

**Use Database-First When**:
- ✅ Enterprise/financial organization
- ✅ DBA-controlled environments
- ✅ Compliance requirements
- ✅ Existing database schema
- ✅ Multiple applications sharing database
- ✅ Complex stored procedures needed

**Use Code-First When**:
- ✅ Rapid development
- ✅ Small teams without dedicated DBAs
- ✅ Frequent schema changes
- ✅ Greenfield projects
- ✅ Microservices with isolated databases

## 📖 Additional Resources

- **PostgreSQL Documentation**: https://www.postgresql.org/docs/
- **pg_trgm Extension**: https://www.postgresql.org/docs/current/pgtrgm.html
- **EF Core Scaffolding**: https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding
- **Database-First Best Practices**: https://learn.microsoft.com/en-us/ef/core/managing-schemas/
- **Parent Project README**: ../README.md

## 🤝 Contributing

When contributing to this Database-First version:

1. **Schema Changes**: Always create new SQL scripts, never modify existing ones
2. **Numbering**: Use next sequential number (005, 006, etc.)
3. **Documentation**: Include comments in SQL scripts
4. **Testing**: Test scripts in dev environment first
5. **Rollback**: Create rollback scripts for destructive changes
6. **Re-scaffold**: Remember to re-run `scaffold-models.ps1` after schema changes

## 📞 Support

If you encounter issues:

1. Check the **Troubleshooting** section in the main SETUP.md
2. Verify database connectivity: `psql -U postgres -d phonetic_db_dbfirst -c "SELECT version();"`
3. Ensure pg_trgm is enabled: `psql -U postgres -d phonetic_db_dbfirst -c "\dx"`
4. Check that all SQL scripts ran successfully

---

**Last Updated**: November 12, 2025  
**Version**: 1.0.0  
**Database**: PostgreSQL 14+  
**Approach**: Database-First with EF Core Scaffolding
