# 📊 Sample Mortgage Data

This directory contains sample mortgage data that demonstrates the phonetic name matching capabilities of the Phonetic Analyzers system.

## 📄 Sample Data File

**File**: `sample_mortgage_data.csv`

**Contents**: 10 sample mortgage records with the following structure:
- **ExternalId**: Unique identifier (MTG001, MTG002, etc.)
- **FullName**: Person or business name
- **County**: County abbreviation (Miami-Dade, Broward, etc.)
- **CountyId**: Numeric county identifier
- **CountyName**: Full county name
- **Flag**: Record type (I=Individual, B=Business, U=Unknown)

## 🎯 Sample Data Preview

| External ID | Full Name | County | Type |
|------------|-----------|--------|------|
| MTG001 | John Smith | Miami-Dade | Individual |
| MTG002 | Jane Johnson Corporation | Miami-Dade | Business |
| MTG003 | Robert Brown | Broward | Individual |
| MTG004 | Sarah Davis LLC | Broward | Business |
| MTG005 | Michael Wilson | Palm Beach | Individual |
| ... | ... | ... | ... |

## 🚀 How to Load Sample Data

### Method 1: Web Interface (Recommended)

1. **Start the web application**:
   ```bash
   cd Web
   dotnet run
   ```

2. **Navigate to the sample data loader**:
   - Open: http://localhost:5000/load-sample-data
   - Click "Load Sample Data" button
   - View loading progress and results

3. **Test the loaded data**:
   - Navigate to: http://localhost:5000/mortgage-search
   - Try searching for names like "John", "Smith", "Jane Johnson", etc.

### Method 2: PowerShell Script

1. **Run the sample data preview script**:
   ```powershell
   .\load-sample-data.ps1
   ```

   This script will:
   - Display sample data preview
   - Show statistics
   - Provide loading instructions
   - Check database connectivity

### Method 3: Azure Functions API

1. **Start the Ingestion Function**:
   ```bash
   cd src/PhoneticAnalyzers.Functions.Ingestion
   func start --port 7071
   ```

2. **POST to the bulk ingestion endpoint**:
   ```bash
   curl -X POST http://localhost:7071/api/bulk-ingest \
     -H "Content-Type: application/json" \
     -d '{"filePath": "sample_mortgage_data.csv"}'
   ```

### Method 4: Direct Database Loading (Advanced)

If you have direct database access, you can use MediatR's `BulkIngestCommand`:

```csharp
var command = new BulkIngestCommand
{
    DataSource = "sample_mortgage_data.csv",
    BatchSize = 100,
    SkipPhoneticEncoding = false, // Generate phonetic codes
    ContinueOnError = true,
    SourceSystem = "SampleData"
};

var result = await mediator.Send(command);
```

## 🔍 Testing Phonetic Matching

Once the sample data is loaded, you can test the phonetic matching capabilities:

### Exact Name Matches
- Search for "John Smith" → Should find MTG001
- Search for "Jane Johnson Corporation" → Should find MTG002

### Partial Name Matches
- Search for "John" → Should find "John Smith"
- Search for "Johnson" → Should find "Jane Johnson Corporation"

### Phonetic Matches (Similar sounding names)
- Search for "Jon Smith" → Should find "John Smith" (phonetic match)
- Search for "Jane Jonson" → Should find "Jane Johnson" (phonetic match)

### County Filtering
- Filter by "Miami-Dade" → Should show MTG001, MTG002
- Filter by "Broward" → Should show MTG003, MTG004

### Record Type Filtering
- Filter by "Individual" → Should show personal names
- Filter by "Business" → Should show corporate entities

## 🔧 Prerequisites

1. **PostgreSQL Database**:
   - Running on localhost:5432
   - Database: `phonetic_analyzers_dev`
   - User: `postgres` / Password: `postgres`

2. **.NET 8.0 SDK**: For running the web application

3. **Azure Functions Core Tools** (optional): For Function App testing

## 📈 Expected Results

After loading the sample data, you should see:
- **Total Records**: 10
- **Individuals**: 4 records with Flag='I'
- **Businesses**: 5 records with Flag='B'  
- **Unknown**: 1 record with Flag='U'
- **Counties**: Miami-Dade, Broward, Palm Beach, Orange, Hillsborough, Duval

## 🎉 What's Happening Behind the Scenes

When you load the sample data, the system:

1. **Parses the CSV** using CsvHelper
2. **Validates each record** using FluentValidation
3. **Generates phonetic codes** using Double Metaphone and Beider-Morse algorithms
4. **Bulk inserts** records using optimized PostgreSQL operations
5. **Creates database indexes** for fast phonetic searching
6. **Enables fuzzy matching** using PostgreSQL's pg_trgm extension

## 🐛 Troubleshooting

### Common Issues:

1. **File not found**: Ensure `sample_mortgage_data.csv` exists in the project root
2. **Database connection error**: Check PostgreSQL is running on port 5432
3. **Permission errors**: Ensure the database user has write permissions
4. **Build errors**: Run `dotnet restore` and `dotnet build` in the Web directory

### Getting Help:

- Check application logs for detailed error messages
- Verify database connectivity using the PowerShell script
- Ensure all required NuGet packages are restored

---

🎯 **Ready to test?** Start with the web interface method - it's the easiest way to get started!