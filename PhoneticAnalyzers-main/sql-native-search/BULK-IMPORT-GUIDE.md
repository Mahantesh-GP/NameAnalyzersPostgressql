# Bulk Import Workflow for 2M Records

## Step 1: Prepare Your CSV File
Ensure your CSV has these columns (with header row):
```
external_id,full_name,county,flag
```

Example:
```csv
external_id,full_name,county,flag
EXT-001,William Smith,TestCounty,Individual
EXT-002,Robert Johnson,TestCounty,Individual
```

## Step 2: Fast Bulk Import (2M records in ~10-20 minutes)

```powershell
# Edit fast-bulk-import.sql and update the CSV path on line 5:
# \COPY person(external_id, full_name, county, flag) FROM 'C:/YOUR/PATH/data.csv' ...

# Run the import
$env:PGPASSWORD='postgres'
& 'C:\Program Files\PostgreSQL\17\bin\psql.exe' -h localhost -U postgres -d phonetic_native -f "sql-native-search\scripts\fast-bulk-import.sql"
```

## Step 3: LLM Nickname Enrichment (Run separately)

### 3a. Start Ollama (if using local LLM)
```powershell
# Make sure Ollama is running with your model
ollama pull llama3.2:latest
ollama serve
```

### 3b. Run Nickname Enrichment Tool
```powershell
cd tools\NicknameEnrichment
dotnet run
```

This will:
- Extract all unique first names from your 2M records
- Call LLM to get nickname variants
- Populate `nickname_map` table
- Takes ~1-2 hours depending on unique names

## Step 4: Apply Nicknames to Existing Data

After `nickname_map` is populated:

```powershell
$env:PGPASSWORD='postgres'
& 'C:\Program Files\PostgreSQL\17\bin\psql.exe' -h localhost -U postgres -d phonetic_native -f "sql-native-search\sql\08_apply_nicknames_bulk.sql"
```

This expands all person_names with their nickname variants.

## Step 5: Verify Results

```sql
-- Check totals
SELECT 
    (SELECT COUNT(*) FROM person) as total_persons,
    (SELECT COUNT(*) FROM person_names) as total_name_tokens,
    (SELECT COUNT(*) FROM nickname_map) as total_nickname_mappings,
    (SELECT COUNT(*) FROM person_names WHERE is_nickname = TRUE) as nickname_variants;

-- Test search with nicknames
SELECT * FROM search_persons('Bob Smith', 50, 0.3);
```

## Performance Notes

- **CSV Import**: ~10-20 minutes for 2M records
- **Phonetic Processing**: Already included in import
- **LLM Enrichment**: 1-2 hours (can be done in parallel, doesn't block usage)
- **Nickname Application**: ~10-30 minutes depending on nickname_map size

## Changes Needed to Existing Code

### ✓ No changes needed to:
- Search function (already supports nickname expansion)
- API (already returns match_metadata)
- WebUI (already displays nickname explanations)

### Optional optimizations:
1. Add more indexes if search is slow:
   ```sql
   CREATE INDEX CONCURRENTLY idx_person_county ON person(county);
   CREATE INDEX CONCURRENTLY idx_person_flag ON person(flag);
   ```

2. Use parallel import for even faster loading:
   ```sql
   SET max_parallel_workers_per_gather = 4;
   ```

## Troubleshooting

### Import is slow
- Use `fast-bulk-import.sql` instead of `bulk-import-csv.sql`
- Disable indexes during import, rebuild after

### LLM returns incorrect format
- Adjust temperature (lower = more consistent)
- Improve prompt in `NicknameEnrichmentService.cs`
- Add manual validation step

### Out of memory
- Process in batches
- Increase PostgreSQL `shared_buffers` and `work_mem`
