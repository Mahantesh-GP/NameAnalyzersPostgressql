# Mortgage Search Demo Script

## Sample CSV Data for Testing
You can use this sample data to test the mortgage search functionality.

### Sample Records (sample_mortgage_extended.csv)
```csv
ExternalId,FullName,County,CountyId,CountyName,Flag
MTG001,John Michael Smith,Miami-Dade,1,Miami-Dade County,I
MTG002,Smithson Real Estate LLC,Miami-Dade,1,Miami-Dade County,B
MTG003,Maria Elena Garcia,Broward,2,Broward County,I
MTG004,Garcia Holdings Corporation,Broward,2,Broward County,B
MTG005,Robert James Wilson,Palm Beach,3,Palm Beach County,I
MTG006,Wilson Development Group,Palm Beach,3,Palm Beach County,B
MTG007,Catherine Johnson,Orange,4,Orange County,I
MTG008,Johnson & Associates Inc,Orange,4,Orange County,B
MTG009,David Alexander Brown,Hillsborough,5,Hillsborough County,I
MTG010,Brown Investment Partners,Hillsborough,5,Hillsborough County,B
MTG011,Jennifer Marie Davis,Duval,6,Duval County,I
MTG012,Davis Property Management,Duval,6,Duval County,B
MTG013,Michael John Anderson,Miami-Dade,1,Miami-Dade County,I
MTG014,Anderson Financial Services,Miami-Dade,1,Miami-Dade County,B
MTG015,Sarah Elizabeth Martinez,Broward,2,Broward County,I
MTG016,Martinez Consulting LLC,Broward,2,Broward County,B
MTG017,James Robert Thompson,Palm Beach,3,Palm Beach County,I
MTG018,Thompson Construction Corp,Palm Beach,3,Palm Beach County,B
MTG019,Lisa Ann Taylor,Orange,4,Orange County,I
MTG020,Taylor Business Solutions,Orange,4,Orange County,B
MTG021,Daniel Paul Rodriguez,Hillsborough,5,Hillsborough County,I
MTG022,Rodriguez Realty Group,Hillsborough,5,Hillsborough County,B
MTG023,Amanda Rose White,Duval,6,Duval County,I
MTG024,White Property Investments,Duval,6,Duval County,B
MTG025,Christopher Lee Miller,Miami-Dade,1,Miami-Dade County,I
MTG026,Miller Holdings International,Miami-Dade,1,Miami-Dade County,B
MTG027,Unknown Entity Alpha,Miami-Dade,1,Miami-Dade County,U
MTG028,Unknown Entity Beta,Broward,2,Broward County,U
MTG029,Unknown Entity Gamma,Palm Beach,3,Palm Beach County,U
MTG030,Unknown Entity Delta,Orange,4,Orange County,U
```

## Test Scenarios

### Scenario 1: Phonetic Name Variations
Test the system's ability to find phonetic matches:

1. **Search:** "Jon Smith"
   - **Expected:** Should find "John Michael Smith" (MTG001)
   - **Filters:** All Counties, Individual (I)

2. **Search:** "Catherine Johnson"
   - **Expected:** Should find "Catherine Johnson" (MTG007) 
   - **Filters:** Orange County, Individual (I)

3. **Search:** "Kathryn Jonson" (misspelled)
   - **Expected:** Should still find "Catherine Johnson" via phonetic matching
   - **Filters:** All Counties, All Types

### Scenario 2: County-Based Filtering
Test geographic filtering capabilities:

1. **Search:** "Smith"
   - **Filter by County:** Miami-Dade County
   - **Expected:** Find both "John Michael Smith" (MTG001) and "Smithson Real Estate LLC" (MTG002)

2. **Search:** "Garcia"
   - **Filter by County:** Broward County  
   - **Expected:** Find "Maria Elena Garcia" (MTG003) and "Garcia Holdings Corporation" (MTG004)

### Scenario 3: Entity Type Filtering
Test individual vs business filtering:

1. **Search:** "Johnson"
   - **Filter by Type:** Individual (I)
   - **Expected:** Find "Catherine Johnson" (MTG007) only

2. **Search:** "Johnson"  
   - **Filter by Type:** Business (B)
   - **Expected:** Find "Johnson & Associates Inc" (MTG008) only

### Scenario 4: Combined Filtering
Test multiple filters together:

1. **Search:** "Anderson"
   - **County:** Miami-Dade County
   - **Type:** Business (B)
   - **Expected:** Find "Anderson Financial Services" (MTG014)

2. **Search:** "Martinez"
   - **County:** Broward County
   - **Type:** Individual (I) 
   - **Expected:** Find "Sarah Elizabeth Martinez" (MTG015)

### Scenario 5: Fuzzy/Partial Matching
Test partial name matching:

1. **Search:** "Rodriguez"
   - **Expected:** Find both "Daniel Paul Rodriguez" and "Rodriguez Realty Group"

2. **Search:** "Property"
   - **Filter by Type:** Business (B)
   - **Expected:** Find businesses with "Property" in name

### Scenario 6: Unknown Entity Handling
Test unknown record type filtering:

1. **Search:** "Unknown"
   - **Filter by Type:** Unknown (U)
   - **Expected:** Find all unknown entities (MTG027-MTG030)

## Performance Testing

### Load Testing Queries
Use these queries to test performance with larger datasets:

1. **High-Volume County Search:**
   - County: Miami-Dade (typically highest volume)
   - Type: All Types
   - Expected: Fast response even with thousands of records

2. **Cross-County Phonetic Search:**
   - Search: Common surname like "Smith" or "Johnson"
   - County: All Counties
   - Expected: Efficient phonetic matching across all records

3. **Business Entity Search:**
   - Search: "LLC" or "Corporation" or "Inc"
   - Type: Business (B)
   - Expected: Fast business entity identification

## API Testing

### Direct API Calls
You can test the API directly using these examples:

```bash
# Basic name search
curl "http://localhost:7071/api/search?name=John%20Smith&maxResults=10"

# County-filtered search  
curl "http://localhost:7071/api/search?name=Garcia&countyId=2&maxResults=10"

# Entity type filtered search
curl "http://localhost:7071/api/search?name=Johnson&recordType=B&maxResults=10"

# Combined filters
curl "http://localhost:7071/api/search?name=Smith&countyId=1&recordType=I&maxResults=10"
```

## Expected Performance Metrics

### Response Time Targets
- **Simple Name Search:** < 50ms
- **County Filtered Search:** < 25ms  
- **Entity Type Filtered Search:** < 30ms
- **Combined Filters:** < 20ms
- **Phonetic Matching:** < 100ms

### Scalability Targets
- **1M Records:** All searches under 100ms
- **10M Records:** County-filtered searches under 50ms
- **100M Records:** Multi-filter searches under 100ms

## Troubleshooting

### Common Issues and Solutions

1. **Slow Search Performance:**
   - Check index usage with `EXPLAIN ANALYZE`
   - Verify pg_trgm extension is enabled
   - Run `ANALYZE person;` to update statistics

2. **Missing Results:**
   - Test without filters first
   - Check phonetic code generation
   - Verify data import completed successfully

3. **County Filter Not Working:**
   - Verify county IDs match between data and filter
   - Check county endpoint availability
   - Validate county data consistency

This comprehensive test suite will help validate all aspects of the mortgage search enhancement functionality.