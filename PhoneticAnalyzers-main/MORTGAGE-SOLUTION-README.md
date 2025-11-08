# Mortgage Solution - Bulk Data Upload

## Database Schema Changes

The database has been updated with new fields for the mortgage solution:

### New Fields Added to Person Table:
- **county** (varchar(100)): County name
- **county_id** (integer): County identifier 
- **county_name** (varchar(150)): County display name
- **flag** (char(1)): Record type flag
  - 'I' = Individual
  - 'B' = Business  
  - 'U' = Unknown

### New Indexes for Performance:
- `ix_person_county_id`: Index on county_id
- `ix_person_flag`: Index on flag
- `ix_person_county_flag`: Composite index on county_id and flag

## CSV File Format for Bulk Upload

The bulk upload system expects a CSV file with the following columns:

```csv
Id,FullName,County,CountyId,CountyName,Flag
12345,"John Smith","Cook County",17,"Cook County","I"
67890,"Acme Corporation","DuPage County",43,"DuPage County","B"
11111,"Jane Doe","Lake County",97,"Lake County","I"
```

### Column Descriptions:
- **Id**: External identifier (unique)
- **FullName**: Full name of person or business
- **County**: County name (abbreviated form)
- **CountyId**: Numeric county identifier
- **CountyName**: Full county display name
- **Flag**: Record type ('I'=Individual, 'B'=Business, 'U'=Unknown)

## Batch Upload Features

### Performance Optimizations:
- **Batch Processing**: Processes records in configurable batches (default: 1000)
- **Parallel Processing**: Uses multiple threads for faster processing
- **Bulk Operations**: Optimized database operations for millions of records
- **Memory Efficient**: Streams CSV data to avoid loading entire file into memory

### Configuration Options:
- `BatchSize`: Number of records per batch (default: 1000)
- `MaxDegreeOfParallelism`: Number of parallel threads (default: CPU count)
- `SkipPhoneticEncoding`: Skip phonetic encoding for faster initial load (can be done later)
- `ContinueOnError`: Continue processing even if individual records fail

### Upload Methods:
1. **File Upload**: Upload CSV file via API endpoint
2. **Direct Data**: Send JSON array of records directly
3. **Streaming**: For very large files, use streaming upload

## Usage Examples

### Via Function App API:
```http
POST /api/bulk-ingest
Content-Type: multipart/form-data

file: mortgage_data.csv
batchSize: 1000
continueOnError: true
```

### Processing Statistics:
The system returns detailed statistics:
- Total records processed
- Records inserted vs updated
- Processing speed (records per second)
- Failed record samples
- Error details

## Migration Applied

Migration `MortgageSolutionSchemaUpdate` has been created and is ready to apply to your PostgreSQL database:

```bash
dotnet ef database update --startup-project ../../Web/PhoneticAnalyzers.Web.csproj
```