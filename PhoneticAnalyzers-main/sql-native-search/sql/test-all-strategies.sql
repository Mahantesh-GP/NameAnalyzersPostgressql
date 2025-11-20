-- Test data covering all 5 search strategies
-- Run this to add diverse records that will trigger different match types
-- Each category has 15-20 records to properly test the UI

-- Exact matches (20 records)
SELECT ingest_person('John Smith', 'KING', 'King County', 'I');
SELECT ingest_person('Jane Doe', 'PIER', 'Pierce County', 'I');
SELECT ingest_person('Robert Johnson', 'SNOH', 'Snohomish County', 'B');
SELECT ingest_person('Mary Williams', 'KING', 'King County', 'I');
SELECT ingest_person('David Brown', 'PIER', 'Pierce County', 'I');
SELECT ingest_person('Jennifer Davis', 'SNOH', 'Snohomish County', 'I');
SELECT ingest_person('Michael Wilson', 'KING', 'King County', 'B');
SELECT ingest_person('Linda Moore', 'PIER', 'Pierce County', 'I');
SELECT ingest_person('James Taylor', 'SNOH', 'Snohomish County', 'I');
SELECT ingest_person('Patricia Anderson', 'KING', 'King County', 'I');
SELECT ingest_person('Charles Thomas', 'PIER', 'Pierce County', 'B');
SELECT ingest_person('Barbara Jackson', 'SNOH', 'Snohomish County', 'I');
SELECT ingest_person('Joseph White', 'KING', 'King County', 'I');
SELECT ingest_person('Susan Harris', 'PIER', 'Pierce County', 'I');
SELECT ingest_person('Thomas Martin', 'SNOH', 'Snohomish County', 'B');
SELECT ingest_person('Sarah Thompson', 'KING', 'King County', 'I');
SELECT ingest_person('Daniel Garcia', 'PIER', 'Pierce County', 'I');
SELECT ingest_person('Nancy Martinez', 'SNOH', 'Snohomish County', 'I');
SELECT ingest_person('Matthew Robinson', 'KING', 'King County', 'B');
SELECT ingest_person('Lisa Clark', 'PIER', 'Pierce County', 'I');

-- Nickname expansion matches (20 records)
SELECT ingest_person('William Anderson', 'KING', 'King County', 'I');  -- Will match "Bill"
SELECT ingest_person('Robert Williams', 'PIER', 'Pierce County', 'I');  -- Will match "Bob"
SELECT ingest_person('James Wilson', 'SNOH', 'Snohomish County', 'I');  -- Will match "Jim"
SELECT ingest_person('Michael Brown', 'KING', 'King County', 'I');  -- Will match "Mike"
SELECT ingest_person('Richard Davis', 'PIER', 'Pierce County', 'B');  -- Will match "Dick"
SELECT ingest_person('Elizabeth Miller', 'SNOH', 'Snohomish County', 'I');  -- Will match "Liz"
SELECT ingest_person('Margaret Jones', 'KING', 'King County', 'I');  -- Will match "Meg"
SELECT ingest_person('Christopher Garcia', 'PIER', 'Pierce County', 'I');  -- Will match "Chris"
SELECT ingest_person('William Thompson', 'SNOH', 'Snohomish County', 'B');  -- Will match "Bill"
SELECT ingest_person('Robert Martinez', 'KING', 'King County', 'I');  -- Will match "Bob"
SELECT ingest_person('James Rodriguez', 'PIER', 'Pierce County', 'I');  -- Will match "Jim"
SELECT ingest_person('Michael Lee', 'SNOH', 'Snohomish County', 'I');  -- Will match "Mike"
SELECT ingest_person('Richard Walker', 'KING', 'King County', 'B');  -- Will match "Dick"
SELECT ingest_person('Elizabeth Hall', 'PIER', 'Pierce County', 'I');  -- Will match "Liz"
SELECT ingest_person('Margaret Allen', 'SNOH', 'Snohomish County', 'I');  -- Will match "Meg"
SELECT ingest_person('Christopher Young', 'KING', 'King County', 'I');  -- Will match "Chris"
SELECT ingest_person('William King', 'PIER', 'Pierce County', 'B');  -- Will match "Bill"
SELECT ingest_person('Robert Wright', 'SNOH', 'Snohomish County', 'I');  -- Will match "Bob"
SELECT ingest_person('James Lopez', 'KING', 'King County', 'I');  -- Will match "Jim"
SELECT ingest_person('Michael Hill', 'PIER', 'Pierce County', 'I');  -- Will match "Mike"

-- Phonetic matches (similar sounding names - 20 records)
SELECT ingest_person('Jon Smyth', 'KING', 'King County', 'I');  -- Sounds like "John Smith"
SELECT ingest_person('Jayne Dough', 'PIER', 'Pierce County', 'I');  -- Sounds like "Jane Doe"
SELECT ingest_person('Kathrine Peterson', 'SNOH', 'Snohomish County', 'I');  -- Sounds like "Catherine"
SELECT ingest_person('Steven Thompson', 'KING', 'King County', 'B');  -- Sounds like "Stephen"
SELECT ingest_person('Phillip Martinez', 'PIER', 'Pierce County', 'I');  -- Sounds like "Philip"
SELECT ingest_person('Kristopher White', 'SNOH', 'Snohomish County', 'I');  -- Sounds like "Christopher"
SELECT ingest_person('Geoffrey Harris', 'KING', 'King County', 'I');  -- Sounds like "Jeffrey"
SELECT ingest_person('Alison Clark', 'PIER', 'Pierce County', 'I');  -- Sounds like "Allison"
SELECT ingest_person('Stephani Lewis', 'SNOH', 'Snohomish County', 'I');  -- Sounds like "Stephanie"
SELECT ingest_person('Kristine Robinson', 'KING', 'King County', 'I');  -- Sounds like "Christine"
SELECT ingest_person('Kathryn Walker', 'PIER', 'Pierce County', 'B');  -- Sounds like "Catherine"
SELECT ingest_person('Jeffery Perez', 'SNOH', 'Snohomish County', 'I');  -- Sounds like "Jeffrey"
SELECT ingest_person('Filip Turner', 'KING', 'King County', 'I');  -- Sounds like "Philip"
SELECT ingest_person('Kris Phillips', 'PIER', 'Pierce County', 'I');  -- Sounds like "Chris"
SELECT ingest_person('Stefan Campbell', 'SNOH', 'Snohomish County', 'B');  -- Sounds like "Stephen"
SELECT ingest_person('Allisson Parker', 'KING', 'King County', 'I');  -- Sounds like "Allison"
SELECT ingest_person('Katharina Evans', 'PIER', 'Pierce County', 'I');  -- Sounds like "Catherine"
SELECT ingest_person('Kristoffer Edwards', 'SNOH', 'Snohomish County', 'I');  -- Sounds like "Christopher"
SELECT ingest_person('Stefani Collins', 'KING', 'King County', 'I');  -- Sounds like "Stephanie"
SELECT ingest_person('Jefrey Stewart', 'PIER', 'Pierce County', 'B');  -- Sounds like "Jeffrey"

-- Fuzzy/Trigram matches (similar spelling, typos - 20 records)
SELECT ingest_person('John Smithe', 'KING', 'King County', 'I');  -- Close to "John Smith"
SELECT ingest_person('Jane Deo', 'PIER', 'Pierce County', 'I');  -- Close to "Jane Doe"
SELECT ingest_person('Robrt Johnson', 'SNOH', 'Snohomish County', 'B');  -- Missing letter
SELECT ingest_person('Wiliam Anderson', 'KING', 'King County', 'I');  -- Typo
SELECT ingest_person('Elizabet Miller', 'PIER', 'Pierce County', 'I');  -- Missing letter
SELECT ingest_person('Margret Jones', 'SNOH', 'Snohomish County', 'I');  -- Missing 'a'
SELECT ingest_person('Christophr Garcia', 'KING', 'King County', 'I');  -- Missing 'e'
SELECT ingest_person('Michal Brown', 'PIER', 'Pierce County', 'I');  -- Missing 'e'
SELECT ingest_person('Jhn Smith', 'SNOH', 'Snohomish County', 'B');  -- Missing 'o'
SELECT ingest_person('Jame Doe', 'KING', 'King County', 'I');  -- Missing 's'
SELECT ingest_person('Robet Williams', 'PIER', 'Pierce County', 'I');  -- Missing 'r'
SELECT ingest_person('Willliam Thompson', 'SNOH', 'Snohomish County', 'I');  -- Extra 'l'
SELECT ingest_person('Elizabth Davis', 'KING', 'King County', 'B');  -- Missing 'e'
SELECT ingest_person('Margarett Wilson', 'PIER', 'Pierce County', 'I');  -- Extra 't'
SELECT ingest_person('Christofer Martinez', 'SNOH', 'Snohomish County', 'I');  -- Missing 'ph'
SELECT ingest_person('Micheal Rodriguez', 'KING', 'King County', 'I');  -- Swapped 'ae'
SELECT ingest_person('Richrd Lee', 'PIER', 'Pierce County', 'B');  -- Missing 'a'
SELECT ingest_person('Patrica Walker', 'SNOH', 'Snohomish County', 'I');  -- Missing 'i'
SELECT ingest_person('Barbra Hall', 'KING', 'King County', 'I');  -- Missing 'a'
SELECT ingest_person('Danniel Allen', 'PIER', 'Pierce County', 'I');  -- Extra 'n'

-- Business names (for testing business-core matching)
SELECT ingest_person('Smith & Associates LLC', 'KING', 'King County', 'B');
SELECT ingest_person('Johnson Enterprises Inc', 'PIER', 'Pierce County', 'B');
SELECT ingest_person('Anderson & Co', 'SNOH', 'Snohomish County', 'B');
SELECT ingest_person('Williams Group LLC', 'KING', 'King County', 'B');
SELECT ingest_person('Miller Industries Inc', 'PIER', 'Pierce County', 'B');

-- Names with special characters and variations
SELECT ingest_person('O''Brien Patrick', 'KING', 'King County', 'I');
SELECT ingest_person('Mary-Jane Watson', 'PIER', 'Pierce County', 'I');
SELECT ingest_person('José García', 'SNOH', 'Snohomish County', 'I');
SELECT ingest_person('François Dubois', 'KING', 'King County', 'I');
SELECT ingest_person('Müller Schmidt', 'PIER', 'Pierce County', 'I');

-- Longer composite names
SELECT ingest_person('Alexander Benjamin Christopher', 'KING', 'King County', 'I');
SELECT ingest_person('Mary Elizabeth Catherine', 'PIER', 'Pierce County', 'I');
SELECT ingest_person('John Paul George Ringo', 'SNOH', 'Snohomish County', 'I');

-- Common variations
SELECT ingest_person('Catherine Smith', 'KING', 'King County', 'I');
SELECT ingest_person('Katherine Smith', 'PIER', 'Pierce County', 'I');
SELECT ingest_person('Kathryn Smith', 'SNOH', 'Snohomish County', 'I');
SELECT ingest_person('Steven Johnson', 'KING', 'King County', 'I');
SELECT ingest_person('Stephen Johnson', 'PIER', 'Pierce County', 'I');

COMMIT;

-- Verify ingestion
SELECT COUNT(*) as total_records FROM person;
SELECT COUNT(*) as total_name_entries FROM person_names;

-- Show sample of what was inserted
SELECT full_name, county, county_name, flag 
FROM person 
ORDER BY created_at DESC 
LIMIT 20;
