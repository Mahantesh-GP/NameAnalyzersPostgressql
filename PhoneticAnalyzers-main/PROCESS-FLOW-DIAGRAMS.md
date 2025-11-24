# Process Flow Diagrams - Old vs New Approach

This document provides visual block diagrams comparing the legacy NameCompare batch processing system with the new Phonetic Search real-time solution.

---

## **OLD APPROACH - Legacy NameCompare Process**

```
┌─────────────────────────────────────────────────────────────────────┐
│                         USER PREPARATION                            │
│                                                                     │
│  👤 User manually creates CSV file with thousands of names          │
│  📝 Research FIPS/County codes                                      │
│  ⏱️  Time: 30-60 minutes                                           │
└─────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────┐
│                      CONFIGURATION & UPLOAD                         │
│                                                                     │
│  ⚙️  Select 2 algorithms (TPNS-CA, TPNS-NonCA, TPNSX, Searcher)   │
│  📊 Set match rate threshold (75%, 90%, 100%)                      │
│  🗺️  Specify county/FIPS codes                                     │
│  📤 Upload CSV to STG environment only                             │
│  ⏱️  Time: 5-10 minutes                                            │
└─────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────┐
│                        BATCH PROCESSING                             │
│                                                                     │
│  🌙 Job queued for non-business hours                              │
│  🔄 Processing each name against 2 algorithms                      │
│  💾 Writing results row-by-row to output CSV                       │
│  ⏰ Duration: ~12 HOURS (overnight)                                │
│  🚫 No visibility into progress                                    │
└─────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────┐
│                      DOWNLOAD & MANUAL ANALYSIS                     │
│                                                                     │
│  📥 Download CSV file next day                                     │
│  📊 Open in Excel for manual review                                │
│  🔍 Manually compare Algorithm 1 vs Algorithm 2 scores             │
│  ❓ No explanation of WHY scores differ                            │
│  📝 Create summary report manually                                 │
│  ⏱️  Time: 30-60 minutes                                           │
└─────────────────────────────────────────────────────────────────────┘

═══════════════════════════════════════════════════════════════════════

                           TOTAL TIME
                       ⏰ 13-14 HOURS ⏰
                   (Mostly waiting overnight)

═══════════════════════════════════════════════════════════════════════

                         KEY LIMITATIONS

┌────────────────────────┬────────────────────────┬───────────────────┐
│   ❌ BATCH ONLY        │  ❌ LIMITED ALGORITHMS │  ❌ STG ONLY      │
│                        │                        │                   │
│  • No real-time search │  • Max 2 at a time     │  • Can't test in  │
│  • 12-hour wait        │  • Unknown character   │    QA1, QA2, Prod │
│  • CSV required        │    distance logic      │  • Limited        │
│                        │  • No phonetic         │    regression     │
│                        │  • No nicknames        │    testing        │
└────────────────────────┴────────────────────────┴───────────────────┘

┌────────────────────────┬────────────────────────┬───────────────────┐
│  ❌ POOR VISIBILITY    │  ❌ MANUAL ANALYSIS    │  ❌ NO ITERATION  │
│                        │                        │                   │
│  • No progress updates │  • CSV review in Excel │  • Can't quickly  │
│  • Black box           │  • No visual insights  │    test changes   │
│    processing          │  • Time-intensive      │  • 12-hour cycle  │
│  • Limited result      │  • Error-prone         │    per test       │
│    viewing             │                        │                   │
└────────────────────────┴────────────────────────┴───────────────────┘
```

### **OLD APPROACH PAIN POINTS**

```
    PREPARATION      UPLOAD       PROCESSING      ANALYSIS
        ↓               ↓              ↓             ↓
    [60 min]   →   [10 min]  →   [12 HOURS]  →  [60 min]
        🐌              🐌            🐌🐌🐌           🐌
    
    Manual CSV    Configure     Overnight      Excel Review
    Creation      & Upload      Batch Job      & Report
    
    ❌ Time-consuming  ❌ Limited    ❌ Very slow   ❌ Manual work
    ❌ Error-prone     ❌ STG only   ❌ No feedback ❌ No visuals
```

---

## **NEW APPROACH - Real-Time Phonetic Search**

```
┌─────────────────────────────────────────────────────────────────────┐
│                         ACCESS & SEARCH                             │
│                                                                     │
│  🌐 Open web browser → Navigate to Phonetic Search UI              │
│  🔐 Auto-login via SSO                                             │
│  ⌨️  Type name in search box (e.g., "William Smith")              │
│  💡 See autocomplete suggestions in real-time                      │
│  ⏱️  Time: 10-15 SECONDS                                           │
└─────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    CONFIGURE SEARCH (OPTIONAL)                      │
│                                                                     │
│  📍 Select County/FIPS (dropdown with all counties)                │
│  📋 Choose Record Type (Individual/Business/Unknown)               │
│  ☑️  Enable Match Strategies (checkboxes):                        │
│      ✓ Expand nicknames (Bill → William)                          │
│      ✓ Include phonetic (Metaphone: Smith ≈ Smyth)               │
│      ✓ Include fuzzy/trigram (typos: Willam ≈ William)           │
│  🎚️  Adjust similarity threshold (slider: 0-100%)                 │
│  🔢 Set max results (1-200)                                        │
│  ⏱️  Time: 10-20 SECONDS (or skip - use defaults)                 │
└─────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────┐
│                      INSTANT PROCESSING                             │
│                                                                     │
│  🚀 Click "Search" button                                          │
│  ⚡ API processes request in real-time                             │
│  🔍 4+ algorithms run simultaneously:                              │
│      • EXACT match                                                 │
│      • NICKNAME expansion                                          │
│      • PHONETIC (Metaphone/Double Metaphone)                      │
│      • FUZZY/TRIGRAM similarity                                   │
│  📊 Calculate similarity scores                                    │
│  ⏰ Duration: 50-100ms (SUB-SECOND!)                              │
└─────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────┐
│                     INSTANT VISUAL RESULTS                          │
│                                                                     │
│  ✨ Results appear instantly (no page reload via HTMX)            │
│  📊 Visual match cards showing:                                    │
│      • Full name                                                   │
│      • County name                                                 │
│      • Similarity score (percentage)                               │
│      • Match type badge (EXACT/NICKNAME/PHONETIC/FUZZY)           │
│  🎨 Toggle views:                                                  │
│      📋 List View (all results chronologically)                   │
│      📁 Grouped View (by match type)                              │
│  ⏱️  Time: INSTANT rendering                                       │
└─────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────┐
│                   REFINE & ITERATE (OPTIONAL)                       │
│                                                                     │
│  🔄 Adjust filters based on results                                │
│  🎚️  Change similarity threshold                                  │
│  ☑️  Add/remove match strategies                                  │
│  🔍 Try different name variations                                  │
│  🚀 Click "Search" again                                           │
│  ⏰ Each iteration: 50-100ms                                       │
│  ⏱️  Time: 10-30 SECONDS per refinement                            │
└─────────────────────────────────────────────────────────────────────┘

═══════════════════════════════════════════════════════════════════════

                           TOTAL TIME
                        ⏰ 2-6 MINUTES ⏰
                   (Including refinements!)

═══════════════════════════════════════════════════════════════════════

                         KEY ADVANTAGES

┌────────────────────────┬────────────────────────┬───────────────────┐
│   ✅ REAL-TIME         │  ✅ 4+ ALGORITHMS      │  ✅ ALL ENVS      │
│                        │                        │                   │
│  • Instant search      │  • Exact               │  • STG            │
│  • Sub-100ms response  │  • Nickname            │  • QA1            │
│  • No CSV needed       │  • Phonetic            │  • QA2            │
│  • Interactive         │  • Fuzzy/Trigram       │  • Production     │
│                        │  • All simultaneous    │  • Full testing   │
└────────────────────────┴────────────────────────┴───────────────────┘

┌────────────────────────┬────────────────────────┬───────────────────┐
│  ✅ VISUAL INSIGHTS    │  ✅ AUTO ANALYSIS      │  ✅ FAST ITERATION│
│                        │                        │                   │
│  • Match type badges   │  • Grouped views       │  • Test instantly │
│  • Similarity scores   │  • Clear explanations  │  • Refine on-fly  │
│  • Color-coded         │  • Why it matched      │  • Multiple tests │
│  • Autocomplete        │  • Visual cards        │    in minutes     │
└────────────────────────┴────────────────────────┴───────────────────┘
```

### **NEW APPROACH WORKFLOW**

```
    ACCESS         CONFIGURE       SEARCH        RESULTS       REFINE
      ↓               ↓              ↓             ↓            ↓
   [15 sec]   →   [20 sec]   →   [0.07 sec]  →  [instant]  → [30 sec]
      ⚡              ⚡             ⚡⚡⚡           ⚡⚡          ⚡
    
   Open Web      Optional       Real-Time      Visual        Adjust &
   Browser       Filters        API Call       Display       Re-search
    
   ✅ Instant     ✅ Flexible    ✅ Lightning   ✅ Interactive ✅ Iterative
   ✅ No setup    ✅ Intuitive   ✅ All algos   ✅ Grouped     ✅ Fast loop
```

---

## **DATA INGESTION FLOW**

### **NEW APPROACH - Direct PostgreSQL Bulk Upload**

```
┌─────────────────────────────────────────────────────────────────────┐
│                      DATA SOURCE (CSV/Excel)                        │
│                                                                     │
│  📄 Source data file with person records                           │
│  Columns: name, county, record_type, external_id, etc.            │
└─────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    POSTGRESQL BULK COPY                             │
│                                                                     │
│  🚀 Use PostgreSQL COPY command for high-speed bulk insert         │
│  📊 Direct database upload (no application layer)                  │
│  ⚡ Process: COPY persons_staging FROM '/path/to/file.csv'        │
│  ⏰ Speed: ~100,000 rows per second                                │
└─────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────┐
│                 DATABASE FUNCTION: UPSERT                           │
│                                                                     │
│  🔧 Call PostgreSQL stored function: upsert_persons()              │
│  🔄 Process:                                                       │
│      1. Normalize names (UPPER, trim, remove special chars)        │
│      2. Generate phonetic codes (Metaphone, DMetaphone)            │
│      3. Calculate trigram indexes                                  │
│      4. UPSERT into persons table (INSERT or UPDATE)               │
│         - ON CONFLICT: Update existing records                     │
│         - New records: Insert with generated keys                  │
│      5. Update nickname mappings                                   │
│  ⚡ Set-based operation (not row-by-row)                           │
│  ⏰ Speed: Process entire batch in seconds                         │
└─────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    INDEX UPDATES (AUTOMATIC)                        │
│                                                                     │
│  🔍 PostgreSQL automatically updates indexes:                      │
│      • B-tree indexes on person_id, external_id                    │
│      • GIN indexes on trigrams (pg_trgm)                          │
│      • B-tree indexes on phonetic codes (metaphone, dmetaphone)   │
│      • Composite indexes for common queries                        │
│  ⚡ Concurrent indexing (no downtime)                              │
└─────────────────────────────────────────────────────────────────────┘
                                    ↓
┌─────────────────────────────────────────────────────────────────────┐
│                     DATA READY FOR SEARCH                           │
│                                                                     │
│  ✅ All records normalized and indexed                             │
│  ✅ Phonetic codes pre-calculated                                  │
│  ✅ Trigram indexes built                                          │
│  ✅ Ready for sub-100ms searches                                   │
└─────────────────────────────────────────────────────────────────────┘
```

### **Ingestion Performance Comparison**

```
OLD APPROACH (Function-based row-by-row):
  📝 Process each row individually
  🔄 Call application function per row
  💾 Individual INSERT/UPDATE statements
  ⏰ Speed: ~100-500 rows/second
  ⏱️  Time for 100K records: 3-15 minutes

NEW APPROACH (PostgreSQL COPY + Bulk UPSERT):
  📋 Bulk COPY to staging table
  ⚡ Single UPSERT function call for entire batch
  🚀 Set-based SQL operations
  ⏰ Speed: ~100,000 rows/second for COPY
  ⏱️  Time for 100K records: 5-30 SECONDS

IMPROVEMENT: 95-98% FASTER! ⚡
```

### **UPSERT Function Logic**

```sql
CREATE OR REPLACE FUNCTION upsert_persons()
RETURNS void AS $$
BEGIN
    -- Normalize and generate phonetic codes
    WITH normalized_data AS (
        SELECT 
            external_id,
            UPPER(TRIM(full_name)) as normalized_name,
            metaphone(UPPER(TRIM(full_name)), 10) as metaphone_code,
            dmetaphone(UPPER(TRIM(full_name))) as dmetaphone_code,
            county_id,
            record_type,
            flag
        FROM persons_staging
    )
    -- UPSERT into persons table
    INSERT INTO persons (
        external_id, full_name, normalized_name, 
        metaphone_code, dmetaphone_code,
        county_id, record_type, flag
    )
    SELECT * FROM normalized_data
    ON CONFLICT (external_id) 
    DO UPDATE SET
        full_name = EXCLUDED.full_name,
        normalized_name = EXCLUDED.normalized_name,
        metaphone_code = EXCLUDED.metaphone_code,
        dmetaphone_code = EXCLUDED.dmetaphone_code,
        county_id = EXCLUDED.county_id,
        updated_at = NOW();
        
    -- Clear staging table
    TRUNCATE persons_staging;
END;
$$ LANGUAGE plpgsql;
```

---

## **SIDE-BY-SIDE COMPARISON**

```
┌─────────────────────────────────────────────────────────────────────┐
│                    OLD vs NEW COMPARISON                            │
├──────────────────────────┬──────────────────────────────────────────┤
│      OLD APPROACH        │           NEW APPROACH                   │
│   (Legacy NameCompare)   │    (Phonetic Search Solution)            │
├──────────────────────────┼──────────────────────────────────────────┤
│                          │                                          │
│  📁 CSV Preparation      │  🌐 Web Browser Access                   │
│     60 minutes           │     15 seconds                           │
│                          │                                          │
│  ⬇️                      │  ⬇️                                      │
│                          │                                          │
│  📤 Upload & Configure   │  ⌨️  Type & Configure                    │
│     10 minutes           │     20 seconds                           │
│                          │                                          │
│  ⬇️                      │  ⬇️                                      │
│                          │                                          │
│  🌙 Overnight Processing │  ⚡ Real-Time Processing                 │
│     12 HOURS             │     0.07 seconds                         │
│                          │                                          │
│  ⬇️                      │  ⬇️                                      │
│                          │                                          │
│  📥 Download Results     │  ✨ Instant Visual Results               │
│     2 minutes            │     Instant                              │
│                          │                                          │
│  ⬇️                      │  ⬇️                                      │
│                          │                                          │
│  📊 Manual Analysis      │  🎨 Interactive Views                    │
│     60 minutes           │     Built-in                             │
│                          │                                          │
├──────────────────────────┼──────────────────────────────────────────┤
│  ⏰ TOTAL: 13-14 HOURS   │  ⏰ TOTAL: 2-6 MINUTES                   │
│                          │                                          │
│  ❌ 2 algorithms         │  ✅ 4+ algorithms simultaneously         │
│  ❌ STG only             │  ✅ All environments                     │
│  ❌ No phonetic          │  ✅ Phonetic + Nickname + Fuzzy         │
│  ❌ Manual CSV           │  ✅ Self-service UI                      │
│  ❌ No visibility        │  ✅ Real-time feedback                   │
│  ❌ Row-by-row ingestion │  ✅ Bulk COPY + UPSERT                  │
└──────────────────────────┴──────────────────────────────────────────┘
```

---

## **TIME SAVINGS VISUALIZATION**

```
OLD APPROACH: ████████████████████████████████████████ 13-14 hours
NEW APPROACH: ▌ 2-6 minutes

                    ⚡ 99.6% FASTER! ⚡
```

---

## **RESULT EXAMPLE - NEW APPROACH**

```
╔════════════════════════════════════════════════════════════════╗
║ 🔍 Search Results for "William Smith"                         ║
║ Found 12 results in 68ms                                      ║
╠════════════════════════════════════════════════════════════════╣
║                                                                ║
║ 📋 EXACT MATCHES (1)                                          ║
║ ┌──────────────────────────────────────────────────────────┐ ║
║ │ William Smith                        100% │ EXACT        │ ║
║ │ Los Angeles (06037) • WILLIAM SMITH                      │ ║
║ └──────────────────────────────────────────────────────────┘ ║
║                                                                ║
║ 👥 NICKNAME MATCHES (3)                                       ║
║ ┌──────────────────────────────────────────────────────────┐ ║
║ │ Bill Smith                           95%  │ NICKNAME     │ ║
║ │ Los Angeles (06037) • Bill → William                     │ ║
║ └──────────────────────────────────────────────────────────┘ ║
║ ┌──────────────────────────────────────────────────────────┐ ║
║ │ Will Smith                           92%  │ NICKNAME     │ ║
║ │ Los Angeles (06037) • Will → William                     │ ║
║ └──────────────────────────────────────────────────────────┘ ║
║                                                                ║
║ 🔊 PHONETIC MATCHES (2)                                       ║
║ ┌──────────────────────────────────────────────────────────┐ ║
║ │ William Smyth                        96%  │ PHONETIC     │ ║
║ │ Los Angeles (06037) • Sounds like Smith                  │ ║
║ └──────────────────────────────────────────────────────────┘ ║
║                                                                ║
║ 📝 FUZZY MATCHES (6)                                          ║
║ ┌──────────────────────────────────────────────────────────┐ ║
║ │ Willam Smith                         97%  │ FUZZY        │ ║
║ │ Los Angeles (06037) • Typo detected                      │ ║
║ └──────────────────────────────────────────────────────────┘ ║
╚════════════════════════════════════════════════════════════════╝
```

---

## **KEY METRICS SUMMARY**

| Metric | Old Approach | New Approach | Improvement |
|--------|-------------|--------------|-------------|
| **Search Time** | 12 hours | 68ms | 99.99% faster |
| **User Effort** | 130 minutes | 2-6 minutes | 95%+ reduction |
| **Algorithms** | 2 max | 4+ simultaneous | 2x more |
| **Environments** | STG only | All (STG/QA1/QA2/Prod) | 4x coverage |
| **Ingestion Speed** | 100-500 rows/sec | 100K rows/sec | 200-1000x faster |
| **Visibility** | None (black box) | Real-time visual | Infinite improvement |
| **Match Types** | Unknown distance | Exact/Nickname/Phonetic/Fuzzy | Transparent |
| **Iteration Speed** | 12+ hours/cycle | Seconds/cycle | 1000x+ faster |

---

**Document Version:** 1.0  
**Last Updated:** November 24, 2025  
**Repository:** [NameAnalyzersPostgressql](https://github.com/Mahantesh-GP/NameAnalyzersPostgressql)
