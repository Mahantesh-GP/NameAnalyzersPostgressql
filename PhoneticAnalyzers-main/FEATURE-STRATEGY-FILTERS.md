# Strategy Filter Checkboxes Feature

## Overview
Added checkbox controls to let users select which match strategies they want to see in search results, with intelligent distribution of results across enabled strategies.

## Implementation

### UI Changes

**SearchForm.razor**
- Added 3 checkboxes with icons:
  - ✅ **Nickname** (Primary, checked by default) - Person icon
  - ☐ **Fuzzy** (Info) - Blur icon  
  - ☐ **Phonetic** (Warning) - RecordVoiceOver icon
- **Exact matches always included** (no checkbox needed - highest priority)

### Model Changes

**ApiModels.cs**
- Added `IncludePhonetic` flag to `AdvancedSearchRequest`
- Default values:
  - `ExpandNicknames = true` (Nickname checked)
  - `IncludeTrigramSimilarity = false` (Fuzzy unchecked)
  - `IncludePhonetic = false` (Phonetic unchecked)

### Backend Changes

**Search.razor**
- Added `FilterAndDistributeResults()` method
- Fetches ALL strategies from API (maxResults = 200)
- Client-side filtering based on checkbox state
- Smart distribution algorithm

## Distribution Logic

### Algorithm

For **50 total results** with different checkbox combinations:

#### Example 1: Only Nickname ✅
```
Exact: All exact matches (up to 50)
Nickname: Remaining slots (e.g., if 5 exact, then 45 nickname)
Fuzzy: 0
Phonetic: 0
```

#### Example 2: Nickname ✅ + Fuzzy ✅
```
Exact: All exact matches (e.g., 10 results)
Remaining 40 slots split equally:
  - Nickname: 20 results
  - Fuzzy: 20 results
```

#### Example 3: All strategies ✅✅✅
```
Exact: All exact matches (e.g., 5 results)
Remaining 45 slots split into 3:
  - Nickname: 15 results (45 / 3)
  - Fuzzy: 15 results (45 / 3)
  - Phonetic: 15 results (45 / 3)
```

#### Example 4: Nickname ✅ + Fuzzy ✅ + Phonetic ✅ (with extra slots)
```
Exact: 2 results
Remaining 48 slots split into 3 = 16 each
  - Nickname: 16 results
  - Fuzzy: 16 results
  - Phonetic: 16 results
```

### Priority Order
1. **Exact** (always first, no limit)
2. **Nickname** (if checked, gets proportional share)
3. **Fuzzy** (if checked, gets proportional share)
4. **Phonetic** (if checked, gets proportional share)

## User Benefits

1. **Focused Results**: See only the match types you care about
2. **Performance**: Nickname-only searches are faster and more relevant
3. **Flexibility**: Enable Fuzzy for typos, Phonetic for alternate spellings
4. **Balanced Mix**: Automatically distributes results fairly across enabled strategies

## Testing Scenarios

### Test 1: Nickname Only (Default)
- Search "Bill"
- See: Exact matches + Nickname expansions (William, etc.)
- No fuzzy typos or phonetic sound-alikes

### Test 2: All Strategies
- Check all boxes
- Search "john"
- See: 
  - Exact: "John Smith" (100%)
  - Nickname: ~15 results (92%+)
  - Fuzzy: ~15 results (60-95%)
  - Phonetic: ~15 results (53-59%)

### Test 3: Fuzzy + Phonetic Only
- Uncheck Nickname, check Fuzzy + Phonetic
- Search "jon"
- See: Exact + 50/50 split of Fuzzy and Phonetic matches

## Code Locations

- **UI**: `WebUI/Components/Search/SearchForm.razor` (lines 104-126)
- **Model**: `WebUI/Models/ApiModels.cs` (lines 23-25)
- **Logic**: `WebUI/Pages/Search.razor` (FilterAndDistributeResults method, lines 315-377)

## Future Enhancements

- [ ] Add result count preview per strategy before clicking Search
- [ ] Persist checkbox state in browser localStorage
- [ ] Add "Quick Presets" buttons (Strict, Balanced, Aggressive)
- [ ] Show distribution breakdown in results header (e.g., "2 Exact, 20 Nickname, 15 Fuzzy")
