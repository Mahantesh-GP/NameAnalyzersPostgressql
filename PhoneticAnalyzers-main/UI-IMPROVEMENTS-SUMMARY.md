Short answer

PostgreSQL is generally the better fit when you need built-in, low-cost fuzzy/trigram + phonetic search (via pg_trgm, fuzzystrmatch) and want open-source flexibility and cheaper scale.
SQL Server can do full‑text and phonetic (Soundex) work, but for trigram-like fuzzy matching you’ll need workarounds or extra components — and licensing/managed costs are usually higher at large scale.
Detailed comparison (focused on your search needs)

Feature parity: trigram / fuzzy matching

PostgreSQL:
pg_trgm extension provides trigram similarity, % operator, similarity() and show_trgm(). Works with GIN (col gin_trgm_ops) and GiST.
fuzzystrmatch provides Soundex, Metaphone, Double Metaphone for phonetic matches.
unaccent available for diacritics.
Planner integrates with trigram indexes so queries usually avoid full-table scans.
SQL Server:
Built-in Full‑Text Search (FTS) supports lexeme-based text search, proximity, and language-aware tokenization.
Built-in phonetic: SOUNDEX() and DIFFERENCE() (Soundex-based only). No native Double Metaphone or richer phonetics.
No native trigram extension like pg_trgm. Trigram-like matching would require:
custom n-gram tokenization + inverted table/index, or
third-party tools/CLR functions, or
relying on FTS which matches tokens not character trigrams.
Some 3rd-party / custom solutions exist but add complexity.
Indexing & query planner behavior

Postgres: GIN trigram indexes act like an inverted index for trigrams; planner can use them to drastically reduce candidate rows. CREATE INDEX CONCURRENTLY avoids locks during index build.
SQL Server: FTS uses its own full-text index and word breakers; it's optimized for token (word) search, not character n-grams. Index-maintenance patterns and planner behavior differ; there's no first-class trigram index.
Accuracy for fuzzy matching & phonetics

Postgres: pg_trgm finds similar spellings (character-level) and fuzzystrmatch gives better phonetic choices. Combination covers both fuzzy and phonetic use-cases well.
SQL Server: FTS is good for lexical/token matches; Soundex is limited (many false positives/negatives). For high-quality phonetics or fuzzy similarity, SQL Server needs additional tooling.
Scale, performance & operational cost

Postgres:
Open-source (no licensing fees).
Can scale with partitioning, parallel queries, tuned indexes, and PostgreSQL-friendly hosting (self-host or managed; costs vary).
For billions of rows, you can denormalize search tables, use partitioning, and add indexes incrementally — lower software-cost baseline.
SQL Server:
Enterprise licensing can be expensive (cores-based or managed Azure costs).
Azure SQL / Managed Instances add convenience but cost more than equivalent Postgres setups at high scale.
SQL Server can scale technically, but TCO often higher.
Ecosystem & extensibility

Postgres: rich ecosystem of extensions (pg_trgm, PostGIS, timescaledb, etc.), active community, easy to add new extensions or custom functions.
SQL Server: strong tooling in MS ecosystem (SSMS, BI, SSIS/SSAS), good for enterprises standardized on Microsoft, but less flexible for installing low-level text-matching extensions.
Operational conveniences

SQL Server: polished enterprise tools, integrated Windows/AD support, easy backups/point-in-time in Azure.
Postgres: excellent tooling too (pgBackRest, Patroni, repmgr), but you choose components; more flexible for cost optimization.
When SQL Server makes sense

Your organization already has large SQL Server investments, enterprise licensing, or strict MS/Windows integration requirements.
Your search needs are satisfied by Full-Text Search and Soundex-level phonetics (i.e., token-based search and basic phonetics are enough).
You prefer the managed conveniences of Azure SQL and are willing to accept higher cost.
When PostgreSQL is preferable (for your project constraints)

You need character-level fuzzy search and high-quality phonetic algorithms (pg_trgm + fuzzystrmatch) out of the box.
You’re cost/constrained and prefer open-source to avoid high licensing/managed search costs.
You want flexibility: add custom indexes, denormalized search tables, partitioning, plug-in external search engines later if needed.


Short summary first

Trigrams are overlapping 3-character slices of a (padded) string. Postgres (pg_trgm) compares two strings by their trigram sets using a Dice-like coefficient.
If a GIN trigram index exists, Postgres looks up the query’s trigrams in the index to fetch candidate row IDs — it does not compute similarity for every row.
Below are clear, step-by-step examples (manual trigram construction, set math, index posting lists, and how candidates get filtered).
How trigrams are produced (concept)
Preprocessing: you typically normalize text (lowercase, trim, remove accents) before searching so comparisons are consistent:
e.g., use lower(unaccent(name)).
Padding & sliding window:
The string is conceptually padded so short strings still produce trigrams. For example, show_trgm('John') yields trigrams like {'  j', ' jo', 'joh', 'ohn', 'hn '} (pg_trgm output).
The trigrams are every 3-character window across the padded string.
How similarity is computed (Sørensen–Dice)
Let A = set of trigrams for string S, B = set for string T.
pg_trgm similarity ~ (2 * |A ∩ B|) / (|A| + |B|)
Example 1 — manual computation for 'John' vs 'Jon':
show_trgm('John') → A = { ' j', ' jo', 'joh', 'ohn', 'hn ' } (|A| = 5)
show_trgm('Jon') → B = { ' j', ' jo', 'jon', 'on ' } (|B| = 4)
Intersection A ∩ B = { ' j', ' jo' } → |A ∩ B| = 2
similarity = 2 * 2 / (5 + 4) = 4/9 ≈ 0.444
If you SELECT similarity(name, 'john') you’ll get roughly this numeric score for a stored 'Jon' row.
Example 2 — 'John' vs 'Johnny':
show_trgm('Johnny') might be C = { ' j',' jo','joh','ohn','hnn','nny','ny ' } (|C| = 7)
Intersection with A = { ' j',' jo','joh','ohn','hn ' } ∩ C ≈ { ' j',' jo','joh','ohn' } (depending on exact padding) → say |A ∩ C| = 4
similarity = 2 * 4 / (5 + 7) = 8/12 = 0.666...
Why this is useful
Character-level similarity handles misspellings (John vs Jon, Jonh etc.) that token-based full-text search often misses.
The similarity score is numeric and sortable — you can rank results by similarity.
What happens at insert/update (index maintenance)
If you create CREATE INDEX ON person_names USING gin (name gin_trgm_ops), the index stores, for each distinct trigram seen in the column values, a posting list of row references (TIDs).
Example index excerpt (conceptual):
' j' → [row 1, row 5, row 1000]
' jo' → [row 1, row 30, row 1000, row 1005]
'joh' → [row 1, row 1000]
'ohn' → [row 1, row 2, row 1000]
(these lists are internal; GIN stores compressed posting lists)
Indexing work is done on writes; that precomputation is what makes queries fast.
What happens at query time when you search for 'john'
Step A: Planner decides whether to use the trigram index (based on stats and set_limit()).
Step B (index chosen):
Compute query trigrams Q = show_trgm('john') = { ' j', ' jo', 'joh', 'ohn', 'hn ' }.
Look up each trigram in the GIN index to retrieve posting lists.
Combine posting lists to produce a candidate set. The planner uses heuristics (and set_limit) to determine which candidates are promising; it may require a minimum overlap count.
Fetch only the candidate rows from the heap and compute similarity() to get the exact numeric score and apply any ordering/limit.
Step C (no index or planner chooses seq scan):
The database computes trigrams and similarity for every row (sequential scan) and filters — this is O(N) work and slow for large tables.
Example of candidate selection (small mock)
Table (id, name):
1, 'John'
2, 'Johan'
3, 'Jon'
4, 'Joan'
5, 'Alice'
Query trigrams for 'john' = Q = { ' j', ' jo', 'joh', 'ohn', 'hn ' }
Posting lists (conceptual):
' j' → [1,2,3,4]
' jo' → [1,2,3,4]
'joh' → [1,2]
'ohn' → [1,2]
'hn ' → [1,2]
Candidate scoring by overlap (count of matched trigrams):
Row 1: matches all 5 → overlap = 5
Row 2: matches 4 (maybe missing 'joh' depending on exact content) → overlap = 4
Row 3: matches 2 ( ' j', ' jo') → overlap = 2
Row 4: matches 2 ( ' j', ' jo') → overlap = 2
Row 5: matches 0
The engine will compute the exact similarity for these candidates and then return the top ones. Only rows 1–4 are fetched from disk; row 5 is never touched if index yields no postings for its trigrams.

What I’ll explain

Exactly what the GIN trigram index stores and how it’s updated on INSERT/UPDATE.
Exactly what the planner does at query time for name % 'john'.
Concrete examples you can run to see posting-like data, overlap counts, and manual similarity calculations.
1) Index maintenance on INSERT / UPDATE (step-by-step, simple example)

Scenario: table trigram_demo(id, name) with these rows before any new insert:

Row 1: John
Row 2: Johan
Row 3: Jon
Row 4: Joan
a) Trigram sets produced (conceptual — show_trgm):

show_trgm('John') → A1 = { ' j', ' jo', 'joh', 'ohn', 'hn ' } (|A1| = 5)
show_trgm('Johan') → A2 = { ' j',' jo','joh','oha','han','an ' } (example; |A2| = 6)
show_trgm('Jon') → A3 = { ' j', ' jo', 'jon', 'on ' } (|A3| = 4)
show_trgm('Joan') → A4 = { ' j',' jo','joa','oan','an ' } (|A4| = 5)
b) Conceptual posting lists the GIN index would contain (trigram → list of row ids):

'  j' → [1,2,3,4]
' jo' → [1,2,3,4]
'joh' → [1,2]
'ohn' → [1,2]
'jon' → [3]
'joa' → [4]
'han' → [2]
'an ' → [2,4]
(Real GIN stores these lists compressed and under tree pages; this is a readable abstraction.)
c) Now INSERT a new row:

INSERT Row 5: Johnathon
Compute show_trgm('Johnathon') → e.g., Q5 = { ' j',' jo','joh','ohn','hna','nat','ath','tho','hon','on ' } (|Q5| ≈ 10)
Index update steps (transactional, atomic):
For each trigram in Q5, append Row 5 to the posting list for that trigram (create the posting entry if trigram was not present).
WAL records describe the index changes so they can be replayed on replicas.
GIN has an internal structure: a B-tree of keys (trigrams) with posting lists. For heavy-write workloads a pending list may temporarily buffer new entries for bulk merging by gin_clean_pending_list.
Resulting posting lists (conceptual):
'  j' → [1,2,3,4,5] (row 5 added)
' jo' → [1,2,3,4,5]
'joh' → [1,2,5]
'ohn' → [1,2,5]
'hn ' → [1,5] (if present)
New trigrams 'hna','nat',... → [5] (new entries created)
Points to note:

This per-trigram update work happens on write. That cost is why indexes increase insert/update cost.
GIN compresses posting lists and may store them in a posting tree if very large.
The index update is transactional: if the transaction rolls back, index changes are rolled back too.
2) Query-time: searching for john (index-chosen flow with numeric example)

Query:

Step-by-step (index path):

Planner decision:

Postgres calculates cost estimates (table size, index selectivity, pg_trgm set_limit/threshold).
If index looks beneficial it chooses a plan using the trigram GIN index (often a Bitmap Index Scan → Bitmap Heap Scan).
Compute query trigrams:

Q = show_trgm('john') = { ' j', ' jo', 'joh', 'ohn', 'hn ' } (|Q| = 5)
Index lookups:

For each trigram in Q, the engine looks up posting lists in the GIN index:
'  j' → [1,2,3,4,5]
' jo' → [1,2,3,4,5]
'joh' → [1,2,5]
'ohn' → [1,2,5]
'hn ' → [1,5]
These per-trigram lists are read from the index (not the main table rows).
Combine posting lists to form candidate set:

A simple combination could be "union all" then count matches per row (how many trigrams matched). Conceptually:
Row 1: matched 5 trigrams → match_count = 5
Row 2: matched 4 trigrams → match_count = 4
Row 3: matched 2 trigrams → match_count = 2
Row 4: matched 2 trigrams → match_count = 2
Row 5: matched 5 trigrams → match_count = 5
The planner/GIN machinery often builds a bitmap (bit per heap page or tuple) marking pages/tuples that are candidates; repeated trigram matches set bits multiple times but the bitmap deduplicates.
Fetch candidate rows (Bitmap Heap Scan):

The engine fetches only the candidate rows from the heap (main table), not the whole table.
For each fetched candidate, compute exact similarity(name, 'john') using the Dice-like formula:
manual similarity = 2 * overlap / (|name_trigrams| + |Q|)
Example for Row 3 (Jon):
show_trgm('Jon') = { ' j',' jo','jon','on ' } (|=4)
overlap with Q = { ' j', ' jo' } → overlap = 2
similarity = 2*2/(4+5) = 4/9 ≈ 0.444
Example for Row 5 (Johnathon):
assume |name_trigrams| = 10, overlap = 5 → similarity = 2*5/(10+5) = 10/15 = 0.666...
Apply predicate & ordering:

If predicate was name % 'john' then pg_trgm's threshold is applied (so only rows with similarity >= threshold pass).
Results are ordered by sim DESC and limited.
Key performance effect:

Only rows that appear in posting lists are read; if posting lists are small relative to the table, much less IO and CPU are used than a full sequential scan.
3) Query-time: sequential-scan flow (no index or planner skips it)

If no trigram index exists (or planner thinks seq-scan cheaper), the engine does:

Seq scan: For each row in the table
Compute show_trgm(name) (on-the-fly) → set size |name_trigrams|
Compute overlap with Q (count matches)
Compute similarity = 2*overlap / (|name_trigrams| + |Q|)
Check predicate similarity >= threshold (or name % 'john')
This touches every row — O(N) work — which is slow for big tables.


Phonetic algorithms map words to simplified codes that capture "how they sound" so names that sound similar (e.g., "Smith" / "Smyth") produce the same or similar codes.
Common algorithms: Soundex (simple, coarse), Metaphone (better for English), Double Metaphone (primary + alternate codes for ambiguous sounds), NYSIIS (another phonetic variant).
In Postgres use the fuzzystrmatch extension to compute phonetic codes, precompute them at ingest, index the code columns (B-tree), and combine phonetic matches with trigram matches in your ranking.


How the main phonetic algorithms work (concise, with steps and examples)


Metaphone

Goal: more linguistically aware encoding for English; handles silent letters, digraphs (ph, kn), and consonant clusters.
Steps: detailed set of rules mapping letters/digraphs to phonetic tokens, removing silent initial letters, mapping different sounds to same code.
Example outcome:
"Smith" → SM0 (illustrative; actual output depends on implementation and requested length).
"Smyth" → same or very similar code.
Pros/cons:
Pros: better precision for English names than Soundex.
Cons: language-specific and still imperfect for many international names.
Double Metaphone

Produces a primary and a secondary code for ambiguous pronunciations (e.g., "Smith" might have one code; "Schmidt" may produce a different pair).
Very useful when spelling variants have multiple plausible pronunciations.
Pros: better recall for ambiguous names.
Cons: more complexity and you must handle two codes per row.

Practical examples of when phonetic helps vs when trigram helps

Phonetic helps:
Different spellings that sound same: Smith / Smyth, Seán / Shawn, Katherine / Catherine (language permitting).
Short names where character n-grams fail (e.g., Sean vs Shaun).
Trigram helps:
Typo corrections, character transpositions (e.g., Jonh for John), partial matches and fuzzy substrings.
Best combined:
Use phonetic to capture pronounced-similar matches, trigram to capture misspellings and partial matches, and combine scores so phonetic-similar names aren't drowned out.


Why Short answer

B-tree indexes are ideal for phonetic code columns because you usually query them with equality (e.g., soundex_code = soundex('john')) and B-tree gives very fast, compact lookups and supports index-only scans, composite keys, and functional indexes.
They’re simpler and cheaper than full-text/GIN indexes for this exact-match use case, but remember phonetic codes often have low cardinality so combine them with other filters or use ranking to avoid large result sets.
Details and examples

Why B-tree fits phonetic columns

Equality lookups: Typical phonetic queries are equality comparisons (=) against a short code. B-tree is optimized for exact matches and range scans.
Small, fixed-size keys: Soundex/Metaphone codes are short strings → keys are compact, lower I/O and better cache locality.
Index-only scans: If your query can be satisfied from columns stored in the index (code + whatever you include), Postgres can avoid heap fetches entirely.
Composite & functional indexes: You can build composite indexes (e.g., (soundex_code, country_code)) or functional indexes on expressions (soundex(lower(unaccent(name)))), which B-tree supports well.
Low complexity & cost: B-tree index maintenance is straightforward and usually cheaper than maintaining large GIN trigram indexes.