# LLM Nickname Enrichment - Cost Analysis

## Overview

This document provides detailed cost calculations for enriching nickname mappings using Azure OpenAI's batch API. The analysis covers different data volumes and token usage patterns to help estimate budget requirements for large-scale nickname enrichment operations.

## Key Assumptions

- **Batch Processing**: Using Azure OpenAI Batch API with batch size of 500 names per call
- **Processing Strategy**: Process **unique first names only** to minimize redundant API calls
- **Token Usage**: Based on empirical measurements from actual batch operations
  - Input tokens per batch: **450 tokens**
  - Output tokens per batch: **150 tokens**
  - Total tokens per batch: **600 tokens**

## Cost Breakdown by Data Volume

### 1. 1 Billion Records (1,000,000,000)

Assuming deduplication to ~1M unique first names:

**Batching Math**
- Unique names: 1,000,000
- Batch size: 500
- Number of API calls: 1,000,000 ÷ 500 = **2,000 batches**

**Token Usage**
- Total input tokens: 2,000 × 450 = **900,000 tokens** (0.9M)
- Total output tokens: 2,000 × 150 = **300,000 tokens** (0.3M)
- Combined total: **1,200,000 tokens** (1.2M)

**Cost Estimates**

| Model | Input Cost | Output Cost | Total Cost |
|-------|-----------|-------------|------------|
| GPT-4o ($2.50/1M input, $10/1M output) | $2.25 | $3.00 | **$5.25** |
| GPT-3.5-Turbo ($0.50/1M input, $1.50/1M output) | $0.45 | $0.45 | **$0.90** |

---

### 2. 50 Million Records (50,000,000)

All 50M are unique names (worst-case scenario):

**Batching Math**
- Unique names: 50,000,000
- Batch size: 500
- Number of API calls: 50,000,000 ÷ 500 = **100,000 batches**

**Token Usage**
- Total input tokens: 100,000 × 450 = **45,000,000 tokens** (45M)
- Total output tokens: 100,000 × 150 = **15,000,000 tokens** (15M)
- Combined total: **60,000,000 tokens** (60M)

**Cost Estimates**

| Model | Input Cost | Output Cost | Total Cost |
|-------|-----------|-------------|------------|
| GPT-4o ($2.50/1M input, $10/1M output) | $112.50 | $150.00 | **$262.50** |
| GPT-3.5-Turbo ($0.50/1M input, $1.50/1M output) | $22.50 | $22.50 | **$45.00** |

---

### 3. 1 Million Records (1,000,000)

Assuming deduplication to ~12K unique first names:

**Batching Math**
- Unique names: 12,000
- Batch size: 500
- Number of API calls: 12,000 ÷ 500 = **24 batches**

**Token Usage**
- Total input tokens: 24 × 450 = **10,800 tokens** (0.0108M)
- Total output tokens: 24 × 150 = **3,600 tokens** (0.0036M)
- Combined total: **14,400 tokens** (0.0144M)

**Cost Estimates**

| Model | Input Cost | Output Cost | Total Cost |
|-------|-----------|-------------|------------|
| GPT-4o ($2.50/1M input, $10/1M output) | $0.027 | $0.036 | **$0.063** |
| GPT-3.5-Turbo ($0.50/1M input, $1.50/1M output) | $0.0054 | $0.0054 | **$0.0108** |

---

## Cost Optimization Strategies

### 1. **Deduplicate to Unique First Names**
- **Impact**: 99%+ cost reduction for large datasets
- **Implementation**: Extract unique first names before batch processing
- **Example**: 1B records → ~1M unique names reduces cost from $26,250 to $5.25 (GPT-4o)

### 2. **Use Fixed Nickname Catalog**
- Maintain a curated list of common nicknames (e.g., top 1,000 names)
- Only use LLM for unknown or rare names
- Can reduce LLM calls by 80-90%

### 3. **Model Selection**
- Use GPT-3.5-Turbo for initial enrichment (80%+ cheaper)
- Reserve GPT-4o for complex edge cases or validation
- Consider fine-tuned models for nickname-specific tasks

### 4. **Prompt Optimization**
- Minimize prompt verbosity while maintaining quality
- Use structured JSON output formats
- Batch multiple names per request (currently 500)

### 5. **Caching & Reuse**
- Store and reuse nickname mappings across datasets
- Build a shared nickname repository
- Implement version control for nickname maps

### 6. **Incremental Processing**
- Process new/unknown names only
- Skip names already in nickname_maps table
- Implement checkpointing for large batch jobs

### 7. **Quality Gating**
- Monitor hit rate (% of searches using nicknames)
- Stop enrichment when marginal utility drops
- Focus on high-frequency names first

---

## Processing Time Estimates

### API Rate Limits
- Azure OpenAI Batch API: ~3,000 requests/minute (varies by tier)
- Processing 100,000 batches: ~33 minutes at max throughput
- Recommended: Use slower pace (500-1000 req/min) for stability

### Realistic Timelines
- **1M records (12K unique)**: 1-5 minutes
- **50M records (all unique)**: 2-4 hours
- **1B records (1M unique)**: 15-30 minutes

---

## Implementation Checklist

- [ ] Extract unique first names from dataset
- [ ] Configure Azure OpenAI endpoint and API key
- [ ] Set up batch processing with size = 500
- [ ] Implement token usage tracking (`prompt_tokens`, `completion_tokens`)
- [ ] Add cost estimation logging
- [ ] Configure retry logic and error handling
- [ ] Set up progress monitoring and checkpointing
- [ ] Validate output quality with sample reviews
- [ ] Load enriched nicknames into `nickname_maps` table
- [ ] Test search function with new nickname data

---

## Monitoring & Validation

### Track These Metrics
1. **API Usage**
   - Total batches processed
   - Input/output tokens consumed
   - Actual cost vs. estimate
   - API error rate

2. **Data Quality**
   - Nicknames generated per name
   - Validation pass rate
   - Manual review sample results

3. **Business Impact**
   - Search hit rate improvement
   - Nickname expansion usage in queries
   - User satisfaction metrics

### SQL Query to Check Nickname Coverage
```sql
-- Check how many unique first names have nickname mappings
SELECT 
    COUNT(DISTINCT first_name) as total_unique_names,
    COUNT(DISTINCT nm.original_token) as names_with_nicknames,
    ROUND(100.0 * COUNT(DISTINCT nm.original_token) / 
          NULLIF(COUNT(DISTINCT first_name), 0), 2) as coverage_pct
FROM person_names pn
LEFT JOIN nickname_maps nm ON nm.original_token = pn.first_name;
```

---

## Conclusion

**Key Takeaway**: Processing unique first names makes LLM-based nickname enrichment highly affordable even at massive scale.

- **1B records**: $5.25 (GPT-4o) or $0.90 (GPT-3.5-Turbo)
- **50M unique names**: $262.50 (GPT-4o) or $45.00 (GPT-3.5-Turbo)
- **Cost-effective at any scale** with proper deduplication strategy

**Recommended Approach**:
1. Start with GPT-3.5-Turbo for bulk enrichment
2. Process unique first names only
3. Use fixed catalog for top 1,000 names
4. Monitor quality and adjust as needed
5. Scale incrementally based on search improvement metrics

---

*Document Created: November 26, 2025*  
*Last Updated: November 26, 2025*




Model	Total Input (45M) Cost	Total Output (15M) Cost	Total Cost	Cost per Name (~50M)
GPT-4o-Mini	$0.15 × 45 = $6.75	$0.60 × 15 = $9.00	$15.75	~ $0.000315
GPT-4.1-Mini	$0.30 × 45 = $13.50	$1.20 × 15 = $18.00	$31.50	~ $0.00063
GPT-3.5-Turbo	$0.50 × 45 = $22.50	$1.50 × 15 = $22.50	$45.00	~ $0.00090
(full) GPT-4o	$2.50 × 45 = $112.50	$10.00 × 15 = $150.00	$262.50	~ $0.00525
GPT-5-Mini	$0.25 × 45 = $11.25	$2.00 × 15 = $30.00	$41.25	~ $0.00083
(full) GPT-5	$1.25 × 45 = $56.25	$10.00 × 15 = $150.00	$206.25	~ $0.004125