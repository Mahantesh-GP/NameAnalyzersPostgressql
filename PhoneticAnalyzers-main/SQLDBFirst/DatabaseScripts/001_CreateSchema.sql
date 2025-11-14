-- ============================================================================
-- Database-First Schema Creation Script
-- Database: phonetic_db_dbfirst (to keep separate from Code-First version)
-- PostgreSQL Version: 14+
-- Date: 2025-11-12
-- Author: Development Team
-- Description: Initial schema for Phonetic Analyzer (Database-First approach)
-- ============================================================================

-- Create database (run separately if needed)
-- CREATE DATABASE phonetic_db_dbfirst;

-- Connect to the database
\c phonetic_db_dbfirst;

-- ============================================================================
-- EXTENSIONS
-- ============================================================================

-- Enable pg_trgm for trigram similarity matching (CRITICAL for fuzzy search)
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Verify extension is enabled
SELECT extname, extversion FROM pg_extension WHERE extname = 'pg_trgm';

-- ============================================================================
-- TABLES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Table: person
-- Description: Main table storing person records with phonetic data
-- ----------------------------------------------------------------------------
CREATE TABLE person (
    person_id BIGSERIAL PRIMARY KEY,
    external_id VARCHAR(255) NOT NULL,
    full_name VARCHAR(500) NOT NULL,
    normalized_name VARCHAR(500) NOT NULL,
    primary_metaphone VARCHAR(50),
    alternate_metaphone VARCHAR(50),
    county VARCHAR(100),
    county_id INTEGER,
    county_name VARCHAR(100),
    flag VARCHAR(10) DEFAULT 'U',
    created_utc TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT person_external_id_unique UNIQUE (external_id)
);

-- Add comments for documentation
COMMENT ON TABLE person IS 'Stores person records with phonetic encodings for fuzzy name matching';
COMMENT ON COLUMN person.external_id IS 'External reference ID, unique across all persons. Variants use suffix pattern: ID-NICK-NICKNAME';
COMMENT ON COLUMN person.normalized_name IS 'Uppercase normalized name used for matching';
COMMENT ON COLUMN person.primary_metaphone IS 'Primary Double Metaphone phonetic code';
COMMENT ON COLUMN person.alternate_metaphone IS 'Alternate Double Metaphone phonetic code';
COMMENT ON COLUMN person.flag IS 'Record flag: I=Individual, B=Business, U=Unknown';

-- ----------------------------------------------------------------------------
-- Table: person_names
-- Description: Token-based name storage for partial name matching
-- ----------------------------------------------------------------------------
CREATE TABLE person_names (
    person_name_id BIGSERIAL PRIMARY KEY,
    person_id BIGINT NOT NULL,
    name_token VARCHAR(255) NOT NULL,
    token_position INTEGER NOT NULL,
    primary_metaphone VARCHAR(50),
    alternate_metaphone VARCHAR(50),
    created_utc TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_person_names_person 
        FOREIGN KEY (person_id) 
        REFERENCES person(person_id) 
        ON DELETE CASCADE
);

COMMENT ON TABLE person_names IS 'Stores individual name tokens for partial name matching';
COMMENT ON COLUMN person_names.name_token IS 'Individual word/token from full name';
COMMENT ON COLUMN person_names.token_position IS '0-based position of token in full name';

-- ----------------------------------------------------------------------------
-- Table: person_bm
-- Description: Beider-Morse phonetic encodings for multi-language support
-- ----------------------------------------------------------------------------
CREATE TABLE person_bm (
    person_bm_id BIGSERIAL PRIMARY KEY,
    person_id BIGINT NOT NULL,
    beider_morse_code VARCHAR(255) NOT NULL,
    created_utc TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT fk_person_bm_person 
        FOREIGN KEY (person_id) 
        REFERENCES person(person_id) 
        ON DELETE CASCADE
);

COMMENT ON TABLE person_bm IS 'Stores Beider-Morse phonetic codes for multi-language name matching';
COMMENT ON COLUMN person_bm.beider_morse_code IS 'Beider-Morse phonetic encoding';

-- ----------------------------------------------------------------------------
-- Table: nickname_maps
-- Description: Canonical name to nickname mappings
-- ----------------------------------------------------------------------------
CREATE TABLE nickname_maps (
    nickname_map_id BIGSERIAL PRIMARY KEY,
    canonical_name VARCHAR(100) NOT NULL,
    nickname VARCHAR(100) NOT NULL,
    locale VARCHAR(10) DEFAULT 'en-US',
    confidence DECIMAL(3,2) DEFAULT 0.95,
    is_bidirectional BOOLEAN DEFAULT TRUE,
    created_utc TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT nickname_maps_canonical_nickname_unique 
        UNIQUE (canonical_name, nickname)
);

COMMENT ON TABLE nickname_maps IS 'Stores nickname mappings for name expansion (William->Bill, Robert->Bob, etc.)';
COMMENT ON COLUMN nickname_maps.canonical_name IS 'Standard/formal name (e.g., WILLIAM)';
COMMENT ON COLUMN nickname_maps.nickname IS 'Nickname variant (e.g., BILL)';
COMMENT ON COLUMN nickname_maps.confidence IS 'Confidence score for this mapping (0.0-1.0)';
COMMENT ON COLUMN nickname_maps.is_bidirectional IS 'If true, search works both ways (WILLIAM finds BILL and vice versa)';

-- ----------------------------------------------------------------------------
-- Table: name_aliases
-- Description: Alternative name spellings and aliases
-- ----------------------------------------------------------------------------
CREATE TABLE name_aliases (
    name_alias_id BIGSERIAL PRIMARY KEY,
    canonical_name VARCHAR(255) NOT NULL,
    alias_name VARCHAR(255) NOT NULL,
    alias_type VARCHAR(50),
    confidence DECIMAL(3,2) DEFAULT 0.90,
    created_utc TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT name_aliases_canonical_alias_unique 
        UNIQUE (canonical_name, alias_name)
);

COMMENT ON TABLE name_aliases IS 'Stores alternative spellings and aliases for names';
COMMENT ON COLUMN name_aliases.alias_type IS 'Type of alias: spelling_variant, cultural_variant, transliteration';

-- ----------------------------------------------------------------------------
-- Table: name_alias_cache
-- Description: Performance cache for frequently accessed aliases
-- ----------------------------------------------------------------------------
CREATE TABLE name_alias_cache (
    name_alias_cache_id BIGSERIAL PRIMARY KEY,
    original_name VARCHAR(255) NOT NULL,
    cached_aliases TEXT,
    last_updated_utc TIMESTAMP NOT NULL DEFAULT NOW(),
    
    CONSTRAINT name_alias_cache_original_unique 
        UNIQUE (original_name)
);

COMMENT ON TABLE name_alias_cache IS 'Cache table for frequently accessed name aliases to improve performance';
COMMENT ON COLUMN name_alias_cache.cached_aliases IS 'Comma-separated list of aliases for quick lookup';

-- ============================================================================
-- INDEXES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Primary Indexes (already created via PRIMARY KEY)
-- ----------------------------------------------------------------------------
-- person: person_id (BIGSERIAL PRIMARY KEY)
-- person_names: person_name_id (BIGSERIAL PRIMARY KEY)
-- person_bm: person_bm_id (BIGSERIAL PRIMARY KEY)
-- nickname_maps: nickname_map_id (BIGSERIAL PRIMARY KEY)
-- name_aliases: name_alias_id (BIGSERIAL PRIMARY KEY)
-- name_alias_cache: name_alias_cache_id (BIGSERIAL PRIMARY KEY)

-- ----------------------------------------------------------------------------
-- Foreign Key Indexes
-- ----------------------------------------------------------------------------
CREATE INDEX idx_person_names_person_id ON person_names(person_id);
CREATE INDEX idx_person_bm_person_id ON person_bm(person_id);

-- ----------------------------------------------------------------------------
-- Search Performance Indexes
-- ----------------------------------------------------------------------------

-- Critical: GIN trigram index for fuzzy text matching (MOST IMPORTANT)
CREATE INDEX idx_person_normalized_name_trgm 
    ON person USING gin(normalized_name gin_trgm_ops);

-- Standard B-tree indexes for exact lookups
CREATE INDEX idx_person_external_id ON person(external_id);
CREATE INDEX idx_person_normalized_name ON person(normalized_name);
CREATE INDEX idx_person_county_id ON person(county_id);
CREATE INDEX idx_person_flag ON person(flag);

-- Metaphone indexes for phonetic matching
CREATE INDEX idx_person_primary_metaphone ON person(primary_metaphone);
CREATE INDEX idx_person_alternate_metaphone ON person(alternate_metaphone);

-- Token search indexes
CREATE INDEX idx_person_names_name_token ON person_names(name_token);
CREATE INDEX idx_person_names_name_token_trgm 
    ON person_names USING gin(name_token gin_trgm_ops);
CREATE INDEX idx_person_names_primary_metaphone ON person_names(primary_metaphone);
CREATE INDEX idx_person_names_alternate_metaphone ON person_names(alternate_metaphone);

-- Beider-Morse index
CREATE INDEX idx_person_bm_code ON person_bm(beider_morse_code);

-- Nickname lookup indexes
CREATE INDEX idx_nickname_maps_canonical ON nickname_maps(canonical_name);
CREATE INDEX idx_nickname_maps_nickname ON nickname_maps(nickname);

-- Alias indexes
CREATE INDEX idx_name_aliases_canonical ON name_aliases(canonical_name);
CREATE INDEX idx_name_aliases_alias ON name_aliases(alias_name);

-- Cache index
CREATE INDEX idx_name_alias_cache_original ON name_alias_cache(original_name);

-- ============================================================================
-- FUNCTIONS
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Function: update_updated_utc
-- Description: Automatically updates updated_utc timestamp on row modification
-- ----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION update_updated_utc()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_utc = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION update_updated_utc() IS 'Trigger function to auto-update updated_utc timestamp';

-- ============================================================================
-- TRIGGERS
-- ============================================================================

-- Auto-update updated_utc on person table modifications
CREATE TRIGGER trg_person_updated_utc
    BEFORE UPDATE ON person
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_utc();

-- ============================================================================
-- GRANTS (Optional - adjust based on your security requirements)
-- ============================================================================

-- Grant permissions to application user (create this user separately)
-- CREATE USER phonetic_app_user WITH PASSWORD 'your_secure_password';
-- GRANT CONNECT ON DATABASE phonetic_db_dbfirst TO phonetic_app_user;
-- GRANT USAGE ON SCHEMA public TO phonetic_app_user;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO phonetic_app_user;
-- GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO phonetic_app_user;

-- ============================================================================
-- VERIFICATION
-- ============================================================================

-- Verify all tables are created
SELECT 
    schemaname,
    tablename,
    tableowner
FROM pg_tables 
WHERE schemaname = 'public'
ORDER BY tablename;

-- Verify all indexes are created
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes 
WHERE schemaname = 'public'
ORDER BY tablename, indexname;

-- Verify pg_trgm extension functions are available
SELECT proname 
FROM pg_proc 
WHERE proname LIKE '%trgm%' 
ORDER BY proname;

-- Test trigram similarity function
SELECT similarity('JOHN SMITH', 'JON SMYTH') AS similarity_score;

-- ============================================================================
-- COMPLETION MESSAGE
-- ============================================================================

SELECT '
========================================
DATABASE SCHEMA CREATED SUCCESSFULLY
========================================

Database: phonetic_db_dbfirst
Tables Created: 6
  - person (main person records)
  - person_names (name tokens)
  - person_bm (Beider-Morse codes)
  - nickname_maps (nickname mappings)
  - name_aliases (name aliases)
  - name_alias_cache (performance cache)

Indexes Created: 20+
  - GIN trigram indexes for fuzzy matching
  - B-tree indexes for exact lookups
  - Foreign key indexes for joins

Extensions Enabled:
  - pg_trgm (trigram similarity matching)

Next Steps:
1. Run 002_SeedNicknames.sql to populate nickname mappings
2. Run 003_SeedTestData.sql to add sample data
3. Run scaffold-models.ps1 to generate C# models
4. Update connection strings in application

========================================
' AS completion_message;
