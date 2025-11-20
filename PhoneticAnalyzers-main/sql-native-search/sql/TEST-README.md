# Test Data & Queries for All Search Strategies

This folder contains SQL scripts to verify all 5 search strategies work correctly with the grouped UI.

## Quick Start

1. **Load test data** (adds ~50 diverse records):
   ```bash
   psql -h localhost -U postgres -d your_database -f test-all-strategies.sql
   ```

2. **Run test queries** to verify strategies:
   ```bash
   psql -h localhost -U postgres -d your_database -f test-queries.sql
   ```

## What Gets Tested

### 1. **Exact Match**
- `John Smith` → exact match
- `Jane Doe` → exact match
- Business core matching: `Smith Associates` matches `Smith & Associates LLC`

### 2. **Nickname Expansion**
- `Bill` → matches `William Anderson`
- `Bob` → matches `Robert Williams`
- `Jim` → matches `James Wilson`
- `Mike` → matches `Michael Brown`
- `Liz` → matches `Elizabeth Miller`
- `Chris` → matches `Christopher Garcia`

### 3. **Fuzzy/Trigram Similarity**
- `John Smithe` (extra 'e')
- `Wiliam Anderson` (missing 'l')
- `Robrt Johnson` (missing 'e')
- `Elizabet Miller` (missing 't')
- `Margret Jones` (missing 'a')

### 4. **Phonetic Matches**
- `Jon Smyth` → sounds like `John Smith`
- `Jayne Dough` → sounds like `Jane Doe`
- `Steven` ↔ `Stephen`
- `Phillip` ↔ `Philip`
- `Katherine` ↔ `Catherine` ↔ `Kathryn`
- `Kristopher` ↔ `Christopher`
- `Geoffrey` ↔ `Jeffrey`

### 5. **Other/Composite**
- Special characters: `O'Brien`, `Mary-Jane`
- Accented names: `José García`, `François Dubois`, `Müller Schmidt`
- Long composite names: `Alexander Benjamin Christopher`

## UI Testing

After loading data, test the grouped view in the WebUI:

1. Search for **"John Smith"**:
   - Exact: `John Smith`
   - Phonetic: `Jon Smyth`
   - Fuzzy: `John Smithe`

2. Search for **"Bill Anderson"**:
   - Nickname: `William Anderson`
   - Fuzzy: `Wiliam Anderson`

3. Search for **"Catherine Smith"**:
   - Exact: `Catherine Smith`
   - Phonetic: `Katherine Smith`, `Kathryn Smith`

4. Toggle **"Group by strategy"** to see results organized by:
   - Top Exact match (highlighted)
   - Nickname column (top 5)
   - Fuzzy column (top 5)
   - Phonetic column (top 5)
   - Other column (top 5)

## Expected Results

Each search should populate multiple strategy buckets, demonstrating:
- Clear separation of match types
- Proper ranking within each bucket
- Visual distinction via color-coded similarity scores
- Toggle between unified list and grouped columns

## Cleanup

To remove test data:
```sql
-- Be careful! This removes all data
TRUNCATE person, person_names, nickname_maps CASCADE;
```
