# SQL Search Function - Complete Beginner's Guide

## 📌 What Does This Function Do?

Imagine you have a **phone book database** with millions of names. When someone searches for "Bill Smith", the function finds the name in multiple ways:

1. **Exact match**: "Bill Smith" = "Bill Smith" ✅
2. **Nickname match**: "Bill" = "William" ✅ (Bill is nickname for William)
3. **Fuzzy match**: "Smyth" ≈ "Smith" ✅ (similar spelling)
4. **Sound match**: "Jon" ≈ "John" ✅ (sounds alike)

---

## 🎯 Function Structure - The 7 Phases

Think of this function as a **7-step search process**:

```
┌─────────────────────────────────────────────────────────┐
│  User types: "Bill Smith"                               │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ PHASE 1: PREPARE DATA                                   │
│ • Normalize the text (uppercase, remove spaces)         │
│ • Split into words/tokens                               │
│ • Assign importance weights                             │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ PHASE 2: TRY EXACT MATCH (FASTEST)                      │
│ • Does "BILL SMITH" exist exactly in database?          │
│ • If YES → Return immediately (score: 1.0)             │
│ • If NO → Continue to next phase                        │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ PHASE 3: TRY NICKNAME EXPANSION                          │
│ • "Bill" → Look up nicknames → Find "William"           │
│ • Search for records with "William"                     │
│ • Score: 0.92 (very high, nicknames are reliable)       │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ PHASE 4: TRY FUZZY/TRIGRAM MATCH                        │
│ • "Smith" vs "Smyth" = 85% similar                      │
│ • "Jon" vs "John" = 75% similar                         │
│ • Score: 0.60-0.89 (depends on match quality)           │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ PHASE 5: TRY PHONETIC MATCH (SOUNDS ALIKE)              │
│ • "Sean" sounds like "Shawn"                            │
│ • "Smith" sounds like "Smyth"                           │
│ • Score: 0.53-0.59 (lowest confidence)                  │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ PHASE 6: DEDUPLICATE & RANK                             │
│ • If same person matched multiple ways:                 │
│   Keep best match (Exact > Nickname > Fuzzy > Phonetic) │
└────────────────┬────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────────┐
│ PHASE 7: FILTER & RETURN RESULTS                        │
│ • Apply county/flag filters                             │
│ • Sort by score (best first)                            │
│ • Return top 50 results                                 │
└─────────────────────────────────────────────────────────┘
```

---

## 📊 Detailed Phase Breakdown

### **PHASE 1: DATA PREPARATION**

#### Step 1A: Normalize Input
```
Input: "  bill   SMITH  "
↓
normalize_name()
↓
Output: "BILL SMITH"
```
- Removes extra spaces
- Converts to uppercase
- Makes comparison consistent

#### Step 1B: Split Into Tokens
```
Input: "BILL SMITH"
↓
tokenize_name()
↓
Output: ["BILL", "SMITH"]

(Each word is a separate "token")
```

#### Step 1C: Assign Token Weights
```
Query tokens: ["BILL", "SMITH"]

Check each token:
├─ "BILL" → Real name word → Weight = 1.0 (full importance)
└─ "SMITH" → Real name word → Weight = 1.0 (full importance)

Special cases (LOW weight):
├─ "LLC", "INC", "CORP" → Weight = 0.2 (business suffixes, not important)
├─ "AND", "THE", "OF" → Weight = 0.1 (common words, avoid noise)
└─ "SOLUTIONS", "COMPANY" → Weight = 0.2 (generic business words)
```

**Why weights?** 
- Imagine searching "John AND Associates"
- "AND" is useless, "John" matters most
- Weight system prioritizes meaningful words

---

### **PHASE 2: EARLY EXACT MATCH (OPTIMIZATION)**

```sql
Does database have: WHERE normalized_name = "BILL SMITH"
```

**Check database:**
```
Database records:
├─ ID: 1, Name: "BILL SMITH" ✅ FOUND! Score = 1.0
└─ ID: 2, Name: "WILLIAM SMITH" ✗ Not exact match
```

**If exact match found:**
```
Return immediately ✅
├─ person_id: 1
├─ full_name: "BILL SMITH"
├─ match_type: "Exact"
├─ similarity_score: 1.0 ← Perfect score!
└─ stop searching (optimization!)
```

**Why stop here?**
- If we found exact match, no need for expensive fuzzy matching
- Saves database resources
- Returns fastest result

---

### **PHASE 3: NICKNAME EXPANSION**

**What is nickname expansion?**
```
Lookup table: nickname_maps
┌────────────┬──────────────┐
│ Canonical  │ Nickname     │
├────────────┼──────────────┤
│ WILLIAM    │ BILL         │
│ WILLIAM    │ WILL         │
│ WILLIAM    │ LIAM         │
│ ROBERT     │ BOB          │
│ ROBERT     │ BOBBY        │
│ ELIZABETH  │ BETH         │
│ ELIZABETH  │ LIZ          │
└────────────┴──────────────┘
```

**Search process:**
```
Query token: "BILL"

Step 1: Look up "BILL" in nickname_maps
        ↓
        Found: Canonical name = "WILLIAM"

Step 2: Now search database for people with name token = "WILLIAM"
        ↓
        Found person records with "WILLIAM" in their name

Step 3: Score the match
        ├─ Single token query ("Bill") → Score: 0.92 (very high)
        └─ Multi-token query ("Bill Smith") → Score: depends on coverage
```

**Example:**
```
Query: "Bill Johnson"
Database record: "William Johnson"

Process:
1. "Bill" → Expand to "William" (nickname match!)
2. "Johnson" → Exact match
3. Both tokens matched! Coverage = 100%
4. For 2-token query: Score = 0.75 + 0.23 × 1.0 = 0.98 ✅ (very high!)
```

---

### **PHASE 4: FUZZY/TRIGRAM MATCHING**

#### What is Trigram Similarity?

**Trigram = 3-character sequence**

```
Word: "SMITH"

All trigrams:
┌─────┐
│ **S │  (start marker + S)
├─────┤
│ SM  │
├─────┤
│ MI  │
├─────┤
│ IT  │
├─────┤
│ TH  │
├─────┤
│ H*  │  (H + end marker)
└─────┘
```

#### Compare Two Words

```
Word 1: "SMITH"
Trigrams: {**S, SM, MI, IT, TH, H*}

Word 2: "SMYTH"
Trigrams: {**S, SM, MY, YT, TH, H*}

Common trigrams: {**S, SM, TH, H*} = 4 matches
Total unique: 8
Similarity = 4/8 = 50% ✓ Close match!
```

#### Example Fuzzy Matches

```
Query token: "SMITH"

Database records:
├─ "SMITH" → 100% similar ✅ (exact within fuzzy)
├─ "SMYTH" → 75% similar ✓ (common typo)
├─ "SMYTHE" → 65% similar ✓ (variant spelling)
├─ "SMYTH" → 75% similar ✓ (Scottish spelling)
└─ "JONES" → 10% similar ✗ (too different)
```

#### Typo Tolerance (Levenshtein Distance)

For longer words (≥5 characters), allow **1 typo**:

```
Query: "SMITH" (5 characters)

Database records:
├─ "SMITH" → 0 edits ✅ (exact)
├─ "SMYTH" → 1 edit ✓ (1 substitution: I→Y)
├─ "SMIHT" → 1 edit ✓ (1 transposition: TH→HT)
├─ "SMIT" → 1 edit ✓ (1 deletion: missing H)
├─ "SMITH" → 0 edits ✅ (exact)
└─ "SMYTHE" → 2 edits ✗ (E+something, too many)
```

#### Fuzzy Match Scoring Rules

```
Scoring Logic:

Case 1: All tokens exact + same token count
├─ "John Smith" → "John Smith"
└─ Score: 0.95

Case 2: All tokens exact + extra tokens
├─ "John Smith" → "John Michael Smith" (1 extra token)
├─ Penalty for extra tokens
└─ Score: 0.90 × penalty = ~0.84

Case 3: High coverage (≥80% of query matched)
├─ "John Smith" → "Jon Smyth" (fuzzy both)
├─ Matched weight: 2.0, Total weight: 2.0 = 100% coverage
└─ Score: 0.75 + 0.14 × 1.0 × similarity = ~0.87

Case 4: Partial match
├─ "John Smith" → "Jon Doe" (only "Jon" matched)
├─ Coverage: 50%
└─ Score: 0.60 + 0.14 × 0.5 × similarity = ~0.67

Case 5: No match
└─ Score: 0.0
```

---

### **PHASE 5: PHONETIC MATCHING**

**What is phonetic matching?**

Words that are spelled completely differently but **sound the same**:

```
Examples:
├─ "Sean" sounds like "Shawn"
├─ "Cecilia" sounds like "Sesilia"
├─ "Smith" sounds like "Smyth"
├─ "Knight" sounds like "Night"
└─ "Wright" sounds like "Right"
```

#### Three Phonetic Algorithms

**1. DoubleMetaphone (Score: 0.59) - Most accurate**
```
How it works: Encodes pronunciation in codes

Examples:
├─ "Smith" → ["SM0", "XMF0"]
├─ "Schmidt" → ["XM0", "XMF0"] ← Similar!
├─ "Phillip" → ["FL", "FL"]
└─ "Philip" → ["FL", "FL"] ← Same! ✓
```

**2. Metaphone (Score: 0.56) - Medium accuracy**
```
Examples:
├─ "Johnson" → "JNSN"
├─ "Jonson" → "JNSN" ← Same! ✓
├─ "Catherine" → "K0RN"
└─ "Katherine" → "K0RN" ← Same! ✓
```

**3. Soundex (Score: 0.53) - Oldest, least accurate**
```
Examples:
├─ "Smith" → "S530"
├─ "Smyth" → "S530" ← Same! ✓
├─ "Robert" → "R163"
└─ "Rupert" → "R163" ← Same! ✓
```

#### Phonetic Scoring Example

```
Query: "Sean" (1 token)
Database: "Shawn"

Step 1: Encode "Sean" using DoubleMetaphone
        Result: ["SN", "XN"]

Step 2: Encode "Shawn" using DoubleMetaphone
        Result: ["SN", "XN"]

Step 3: Match found! (first encoding matches)

Step 4: Calculate score
        ├─ DoubleMetaphone base score: 0.59
        ├─ Coverage (1 out of 1 token): 100%
        ├─ Final score: 0.59 × 1.0 = 0.59 ✓
        └─ Acceptable match (above minimum 0.3)
```

---

### **PHASE 6: DEDUPLICATION & RANKING**

**Problem:** Same person can match multiple ways!

```
Query: "Bill Smith"
Database record: "William Smith Jr"

Can match via:
├─ NicknameExpansion: "Bill" → "William" (Score: 0.92)
├─ TrigramSimilarity: "Smith" exact + "William" fuzzy (Score: 0.87)
└─ Phonetic: Matches via Soundex (Score: 0.54)

Question: Which one do we return?
Answer: Return the BEST match (Exact > Nickname > Trigram > Phonetic)
```

#### Priority System

```
Match Priority (1 = best, 4 = worst):

Priority 1 (BEST) → Exact Match
  └─ "Bill Smith" = "Bill Smith" exactly
  └─ Score: 1.0

Priority 2 → Nickname Expansion
  └─ "Bill" expanded to "William" (reliable)
  └─ Score: 0.92-0.98

Priority 3 → Fuzzy/Trigram Match
  └─ "Smyth" similar to "Smith" (approximate)
  └─ Score: 0.60-0.95

Priority 4 (WORST) → Phonetic Match
  └─ "Sean" sounds like "Shawn" (least precise)
  └─ Score: 0.53-0.59
```

#### Deduplication Process

```
Step 1: Collect all matches
┌─────────────────────────────────────────┐
│ Person: William Smith (ID: 42)          │
├─────────────┬─────────────────────────┤
│ Match Type  │ Score │ Priority        │
├─────────────┼───────┼─────────────────┤
│ Exact       │ 1.0   │ 1 (best) ✅     │
│ Nickname    │ 0.92  │ 2               │
│ Trigram     │ 0.87  │ 3               │
│ Phonetic    │ 0.54  │ 4               │
└─────────────┴───────┴─────────────────┘
```

```
Step 2: Keep only the best match per person
┌─────────────────────────────────────┐
│ Return: EXACT Match (Priority 1)    │
│ Score: 1.0                          │
│ Reason: Highest priority!           │
└─────────────────────────────────────┘
```

---

### **PHASE 7: FILTER & RETURN RESULTS**

```sql
Final filtering:
├─ County filter (if specified)
│  └─ Only return records from specified county
├─ Flag filter (if specified)
│  └─ Only return individuals ('P') or businesses ('B')
└─ Minimum similarity threshold (default: 0.3)
   └─ Only return scores ≥ 0.3

Sorting:
├─ 1st: By match priority (1 = best first)
├─ 2nd: By score (highest first)
└─ 3rd: By name (alphabetical, for consistency)

Limit:
└─ Return maximum 50 results (default)
```

---

## 🎓 Real-World Example Walkthrough

### **Scenario: Search for "Bill Jonson"**

```
Input Parameters:
├─ query_name: "Bill Jonson"
├─ max_results: 50
├─ min_similarity: 0.3 (default)
├─ county_filter: NULL (none)
├─ flag_filter: NULL (show all)
├─ include_fuzzy: TRUE
└─ include_nicknames: TRUE
```

#### Step 1: Normalize & Prepare
```
"Bill Jonson" 
→ normalize_name() 
→ "BILL JONSON"

Tokenize: ["BILL", "JONSON"]
Weights: [1.0, 1.0] (both real names)
```

#### Step 2: Check Exact Match
```
Database check: WHERE normalized_name = "BILL JONSON"
Result: NOT FOUND ✗
Continue to next phase...
```

#### Step 3: Nickname Expansion
```
"BILL" → Check nickname_maps

Found:
  Canonical: WILLIAM
  Canonical: WILBERT
  ... (other BILL nicknames)

Now search: WHERE name_token IN ("WILLIAM", "WILBERT", ...)
```

#### Step 4: Fuzzy Matching
```
"JONSON" → Find similar names

Trigram comparison:
├─ "JOHNSON" → Trigrams match: 80% similar ✓
├─ "JONSEN" → Trigrams match: 75% similar ✓
├─ "JANSEN" → Trigrams match: 65% similar ✓
└─ "SMITH" → Trigrams match: 5% similar ✗ (too different)
```

#### Step 5: Combine Results
```
Candidate records:
┌─────────────────────────────────────────────────┐
│ Record 1: WILLIAM JOHNSON                       │
├──────────────────┬──────────────────────────────┤
│ Match path       │ Via Nickname + Fuzzy         │
│ Score            │ 0.96                         │
│ Matched field    │ "WILLIAM" + "JOHNSON"        │
└──────────────────┴──────────────────────────────┘

┌─────────────────────────────────────────────────┐
│ Record 2: BILL JONSEN                           │
├──────────────────┬──────────────────────────────┤
│ Match path       │ Via Fuzzy match              │
│ Score            │ 0.94                         │
│ Matched field    │ "BILL" exact + "JONSEN" fuzzy│
└──────────────────┴──────────────────────────────┘

┌─────────────────────────────────────────────────┐
│ Record 3: WILBERT JANSEN                        │
├──────────────────┬──────────────────────────────┤
│ Match path       │ Via Nickname + Fuzzy         │
│ Score            │ 0.85                         │
│ Matched field    │ "WILBERT" + "JANSEN" fuzzy   │
└──────────────────┴──────────────────────────────┘
```

#### Step 6: Sort by Score
```
Final results (top 3):
┌─────┬─────────────────────────┬───────┐
│ Rank│ Name                    │ Score │
├─────┼─────────────────────────┼───────┤
│ 1.  │ WILLIAM JOHNSON         │ 0.96  │
│ 2.  │ BILL JONSEN             │ 0.94  │
│ 3.  │ WILBERT JANSEN          │ 0.85  │
└─────┴─────────────────────────┴───────┘
```

#### Step 7: Return to User
```
User sees:
┌──────────────────────────────────────────┐
│ "Bill Jonson" Search Results             │
├──────────────────────────────────────────┤
│ 1. WILLIAM JOHNSON (96% match)           │
│    └─ Matched via: Nickname + Fuzzy     │
│                                          │
│ 2. BILL JONSEN (94% match)               │
│    └─ Matched via: Fuzzy match           │
│                                          │
│ 3. WILBERT JANSEN (85% match)            │
│    └─ Matched via: Nickname + Fuzzy     │
└──────────────────────────────────────────┘
```

---

## 📈 Score Reference Guide

### **Understanding Similarity Scores**

```
1.0 ═══════════════════════════════════════ Perfect Match (Exact)
  │
  ├─ 0.95-0.99: Excellent (Exact all tokens, maybe extra)
  │
  ├─ 0.92-0.94: Very Good (Nickname expansion, single token)
  │
  ├─ 0.85-0.91: Good (Fuzzy match, high coverage)
  │
  ├─ 0.70-0.84: Fair (Partial fuzzy match)
  │
  ├─ 0.53-0.69: Okay (Phonetic match)
  │
  └─ 0.30-0.52: Borderline (minimum accepted)
0.0 ═══════════════════════════════════════ No Match
```

### **What Each Score Means**

```
Score 1.0 → "This IS the person you searched for"
Score 0.9 → "This is almost certainly the person"
Score 0.8 → "This is probably the person"
Score 0.7 → "This could be the person"
Score 0.5 → "This might be the person, but unlikely"
Score 0.3 → "This is a long shot, include anyway"
```

---

## 🔧 Function Parameters Explained

```sql
FUNCTION search_persons(
  query_name TEXT                          ← What user types
  max_results INT DEFAULT 50               ← How many results to show (max)
  min_similarity DOUBLE PRECISION DEFAULT 0.3  ← Minimum score (0.0-1.0)
  county_filter TEXT DEFAULT NULL          ← Show only this county (optional)
  flag_filter TEXT DEFAULT NULL            ← 'P'=person or 'B'=business (optional)
  include_fuzzy BOOLEAN DEFAULT TRUE       ← Enable typo/phonetic matching?
  include_nicknames BOOLEAN DEFAULT TRUE   ← Enable nickname expansion?
)
```

### **Usage Examples**

```sql
-- Example 1: Simple search
SELECT * FROM search_persons('Bill Smith');
-- Uses all defaults: 50 results, any county/flag, fuzzy ON, nicknames ON

-- Example 2: Strict search (exact matches only)
SELECT * FROM search_persons(
  'Bill Smith',
  include_fuzzy := FALSE,
  include_nicknames := FALSE
);
-- Only finds exact matches, fastest but fewer results

-- Example 3: County-specific search
SELECT * FROM search_persons(
  'Bill Smith',
  county_filter := 'CALIFORNIA'
);
-- Only returns people from California

-- Example 4: Business search with higher threshold
SELECT * FROM search_persons(
  'ABC Solutions',
  max_results := 10,
  min_similarity := 0.7,
  flag_filter := 'B'
);
-- Only businesses, minimum 70% match, return top 10 only
```

---

## 💡 Key Concepts Summary

| Concept | Meaning | Score Range |
|---------|---------|-------------|
| **Exact Match** | Perfect character-by-character match | 1.0 |
| **Nickname** | Query expanded using nickname mappings | 0.92-0.98 |
| **Fuzzy/Trigram** | Similar spelling (catches typos) | 0.60-0.95 |
| **Phonetic** | Sounds similar (DoubleMetaphone/Metaphone/Soundex) | 0.53-0.59 |
| **Token Weight** | Importance of each word (1.0 = real name, 0.1-0.2 = filler) | varies |
| **Coverage** | Percentage of query words that matched | varies |
| **Priority** | Which match type wins if multiple match | 1-4 |

---

## ⚠️ Common Mistakes & How Function Handles Them

```
Mistake 1: User types extra spaces
Input: "  Bill   Smith  "
Function does: Normalize to "BILL SMITH" ✓

Mistake 2: User types lowercase
Input: "bill smith"
Function does: Convert to "BILL SMITH" ✓

Mistake 3: User types "Jon" but database has "John"
Input: "Jon"
Function does: Fuzzy match (89% similar) ✓

Mistake 4: User types "Bill" but database has "William"
Input: "Bill"
Function does: Nickname expansion (nickname_maps lookup) ✓

Mistake 5: User types "Smyth" but database has "Smith"
Input: "Smyth"
Function does: Phonetic match (same sound) ✓

Mistake 6: User types "Bill LLC" searching for business
Input: "Bill LLC"
Function does: 
  • "Bill" weighted as 1.0
  • "LLC" weighted as 0.2 (ignored mostly)
  • Results prioritize records with "Bill" ✓
```

---

## 🎯 When to Use Different Settings

```
Use Case 1: Finding a specific person (you know their name)
  include_fuzzy := FALSE
  include_nicknames := FALSE
  min_similarity := 0.95
  Reason: Want exact matches only, fastest performance

Use Case 2: Searching with typos (user might misspell)
  include_fuzzy := TRUE
  include_nicknames := TRUE
  min_similarity := 0.3 (default)
  Reason: Catches variations and common mistakes

Use Case 3: Business name search
  flag_filter := 'B'
  include_fuzzy := TRUE
  min_similarity := 0.7
  Reason: Businesses have many name variations

Use Case 4: International names (phonetic is important)
  include_fuzzy := TRUE
  include_nicknames := FALSE
  Reason: Phonetic helps with different spellings

Use Case 5: Duplicate detection
  include_fuzzy := TRUE
  include_nicknames := TRUE
  min_similarity := 0.5
  Reason: Find similar records for cleanup
```

---

## 📚 Database Tables Needed

This function requires these tables to exist:

```sql
-- Main person records
TABLE person (
  person_id BIGINT PRIMARY KEY,
  full_name TEXT,
  normalized_name TEXT,
  business_core_name TEXT (nullable),
  flag TEXT ('P' or 'B'),
  county TEXT
);

-- Individual name tokens per person
TABLE person_names (
  person_id BIGINT,
  name_token TEXT,
  double_metaphone_code TEXT,
  metaphone_code TEXT,
  soundex_code TEXT
);

-- Nickname mappings
TABLE nickname_maps (
  canonical_name TEXT,
  nickname TEXT
);
```

---

## 🚀 Performance Tips

```
1. Exact matches are fastest
   └─ Returns in milliseconds

2. Single-word searches faster than multi-word
   └─ Less tokenization, fewer comparisons

3. Fuzzy matching is slower than exact
   └─ CPU-intensive trigram calculations

4. Phonetic matching is slowest
   └─ Checks three algorithms (DoubleMetaphone, Metaphone, Soundex)

5. Using filters speeds up results
   └─ WHERE county_filter OR flag_filter reduce candidates

6. Lower min_similarity = more results = slower
   └─ More candidates to process

7. Smaller max_results = faster
   └─ Database can stop searching sooner
```

---

This guide should make the function much clearer! 🎓

If you have questions about any specific part, just ask!
