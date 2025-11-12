# Nickname Variant Generation - Implementation Guide

## Overview
The system now automatically generates person records for all nickname variants during bulk ingestion when `ExpandNicknames` is enabled.

## How It Works

### Example
When you ingest **"William Smith"** with `ExpandNicknames = true`:

**Original CSV Record:**
```
ExternalId: 12345
FullName: William Smith
```

**System Creates:**
1. `12345` - William Smith (original)
2. `12345-NICK-WILL` - Will Smith
3. `12345-NICK-BILL` - Bill Smith
4. `12345-NICK-BILLY` - Billy Smith
5. `12345-NICK-WILLY` - Willy Smith
6. `12345-NICK-LIAM` - Liam Smith

### Search Benefits
Now when you search for:
- **"William"** → Finds: William Smith (Exact 1.0)
- **"Bill"** → Finds: Bill Smith (Exact 1.0), William Smith (NicknameExpansion 0.93)
- **"Billy"** → Finds: Billy Smith (Exact 1.0), William Smith (NicknameExpansion 0.93)

## Step-by-Step Setup

### Step 1: Clear Existing Data

Run the SQL script to clear all tables:

```powershell
# Using psql
psql -h localhost -U postgres -d PhoneticAnalyzersDb -f "tools/DataSeeder/clear-database.sql"

# Or in DataGrip/pgAdmin, execute:
# tools/DataSeeder/clear-database.sql
```

**What it does:**
- ✅ Clears `person`, `person_bm`, `person_names` tables
- ✅ Clears `name_aliases`, `name_alias_cache` tables
- ✅ Preserves `nickname_maps` table (needed for generation)

### Step 2: Verify Nickname Maps Exist

```sql
SELECT COUNT(*) FROM nickname_maps;
-- Should return 250+ records
```

If count is 0, run DataSeeder:
```powershell
cd tools/DataSeeder
dotnet run
```

### Step 3: Rebuild Projects

```powershell
# Build Application layer (contains new logic)
cd src/PhoneticAnalyzers.Application
dotnet build

# Build Functions projects
cd ../PhoneticAnalyzers.Functions.Ingestion
dotnet build

cd ../PhoneticAnalyzers.Functions.Search
dotnet build

# Build WebUI
cd ../../WebUI
dotnet build
```

### Step 4: Start Services

```powershell
# Terminal 1: Ingestion Functions
cd src/PhoneticAnalyzers.Functions.Ingestion
func start

# Terminal 2: Search Functions  
cd src/PhoneticAnalyzers.Functions.Search
func start --port 7072

# Terminal 3: WebUI
cd WebUI
dotnet run
```

### Step 5: Upload Data with Nickname Expansion

#### Via WebUI (Bulk Upload):
1. Go to Bulk Upload page
2. Select your CSV file
3. **Ensure settings:**
   - ✅ Expand Nicknames: **ON**
   - Max Degree of Parallelism: 4-8
   - Batch Size: 1000

#### Via API:
```json
POST http://localhost:7071/api/ingest
{
  "externalId": "12345",
  "fullName": "William Smith",
  "county": "Pierce County",
  "countyId": 53,
  "countyName": "Pierce County",
  "flag": "I",
  "expandNicknames": true
}
```

#### Via Batch API:
```json
POST http://localhost:7071/api/ingest/batch
{
  "persons": [
    {
      "externalId": "1",
      "fullName": "William Johnson",
      "county": "King County",
      "countyId": 33,
      "countyName": "King County",
      "flag": "I",
      "expandNicknames": true
    },
    {
      "externalId": "2",
      "fullName": "Robert Smith",
      "county": "Pierce County",
      "countyId": 53,
      "countyName": "Pierce County",
      "flag": "I",
      "expandNicknames": true
    }
  ]
}
```

### Step 6: Verify Results

#### Check Database:
```sql
-- See original and variants
SELECT external_id, full_name, normalized_name 
FROM person 
WHERE external_id LIKE '12345%'
ORDER BY external_id;

-- Expected results:
-- 12345          | William Smith | WILLIAM SMITH
-- 12345-NICK-BILL | Bill Smith   | BILL SMITH
-- 12345-NICK-BILLY| Billy Smith  | BILLY SMITH
-- 12345-NICK-WILL | Will Smith   | WILL SMITH
-- 12345-NICK-WILLY| Willy Smith  | WILLY SMITH
-- 12345-NICK-LIAM | Liam Smith   | LIAM SMITH
```

#### Test Search:
1. Open WebUI → Advanced Search
2. Search for **"William"**
3. Settings:
   - Min Similarity: 0.3
   - Nicknames: **ON** or **OFF** (works either way now!)
   - Trigram: ON

**Expected Results:**
- ✅ William Smith - Exact (1.0)
- ✅ Will Smith - TokenContains (0.95) or Exact (1.0)
- ✅ Bill Smith - TokenContains (0.95) or Exact (1.0)
- ✅ Billy Smith - TokenContains (0.95) or Exact (1.0)
- ✅ Willy Smith - TokenContains (0.95) or Exact (1.0)
- ✅ Liam Smith - TokenContains (0.95) or Exact (1.0)

## Data Size Impact

### Storage Overhead
For names with many nicknames:
- **William** → 6 records (1 original + 5 nicknames)
- **Robert** → 6 records (1 original + 5 nicknames)
- **Elizabeth** → 8 records (1 original + 7 nicknames)
- **Names without nicknames** → 1 record only

**Average:** ~2-3x storage for common first names

### Performance Benefits
✅ **Faster Exact Matches** - Direct lookup instead of nickname expansion query  
✅ **Simpler Queries** - No need for complex nickname joins during search  
✅ **Better Token Matching** - "Bill" appears as whole word, not just via expansion  

### Trade-offs
❌ More disk space (2-3x for names with nicknames)  
❌ Slightly slower ingestion (generates variants)  
❌ External IDs have suffixes (e.g., `12345-NICK-BILL`)  
✅ Much faster and more accurate search results  
✅ Better user experience  

## Testing Checklist

- [ ] Database cleared successfully
- [ ] NicknameMaps table has 250+ records
- [ ] All projects build without errors
- [ ] Ingestion Functions start on port 7071
- [ ] Search Functions start on port 7072
- [ ] WebUI starts successfully
- [ ] Bulk upload creates variant records
- [ ] Searching for "William" finds all variants
- [ ] Searching for "Bill" finds Bill and William records
- [ ] External IDs have proper -NICK- suffix for variants

## Common Names Coverage

The system generates variants for these common names:

**Male:** William, Robert, Richard, Michael, James, John, David, Joseph, Thomas, Charles, Christopher, Daniel, Matthew, Anthony, Donald, Kenneth, Steven, Andrew, Edward, Joshua, George, Kevin, Timothy, Lawrence, Raymond, Patrick, Benjamin, Nicholas, Samuel, Gregory, Alexander, Jonathan, Ronald, Frederick, Jeremy, Gerald, Eugene, Albert, Henry, Douglas, Peter

**Female:** Elizabeth, Margaret, Catherine, Katherine, Jennifer, Susan, Jessica, Sarah, Nancy, Patricia, Linda, Barbara, Dorothy, Helen, Sandra, Deborah, Rebecca, Kimberly, Michelle, Amanda, Stephanie, Nicole, Melissa, Christine, Christina, Rachel, Samantha, Victoria, Abigail, Emily, Danielle, Virginia

## Troubleshooting

### "No variants created" warning
- Check if NicknameMaps table is populated
- Verify first name matches canonical name in nickname_maps
- Check logs for "No nickname mappings found"

### Duplicate external ID errors
- Each variant gets unique ID with `-NICK-{NICKNAME}` suffix
- If reimporting, clear database first

### Too many records created
- This is expected! William Smith creates 6 records
- Disable ExpandNicknames if you want 1:1 ingestion

### Search not finding variants
- Verify records exist: `SELECT * FROM person WHERE full_name LIKE 'Bill%'`
- Check Search Functions are running (port 7072)
- Lower Min Similarity to 0.3

## Rollback (If Needed)

To revert to search-time nickname expansion only:

1. Clear database: Run `clear-database.sql`
2. Restore previous IngestPersonCommandHandler from git
3. Upload data with ExpandNicknames OFF
4. Nickname expansion will work during search only

---

**Implementation Complete!** 🎉

You now have automatic nickname variant generation during bulk ingestion.
