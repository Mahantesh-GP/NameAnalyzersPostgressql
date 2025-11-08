using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhoneticAnalyzers.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeMortgageSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable trigram extension for fuzzy text search
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // Create composite B-tree index for county + record type filtering
            migrationBuilder.CreateIndex(
                name: "ix_person_county_flag_btree",
                table: "person",
                columns: new[] { "county_id", "flag", "normalized_name" });

            // Create composite index for phonetic search within counties
            migrationBuilder.CreateIndex(
                name: "ix_person_dm_primary_county",
                table: "person",
                columns: new[] { "dm_primary", "county_id" },
                filter: "dm_primary IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_person_dm_alternate_county", 
                table: "person",
                columns: new[] { "dm_alternate", "county_id" },
                filter: "dm_alternate IS NOT NULL");

            // Create separate GIN index for trigram search on normalized names (if not exists)
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_person_normalized_name_gin_new
                ON person USING gin (normalized_name gin_trgm_ops);");

            // Create partial indexes for common record types with trigram search
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_person_individuals_name_gin
                ON person USING gin (normalized_name gin_trgm_ops)
                WHERE flag = 'I';");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_person_businesses_name_gin
                ON person USING gin (normalized_name gin_trgm_ops) 
                WHERE flag = 'B';");

            // Create composite index for external ID searches with county
            migrationBuilder.CreateIndex(
                name: "ix_person_external_id_county_btree",
                table: "person",
                columns: new[] { "external_id", "county_id" });

            // Create covering index for common search patterns
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_person_search_covering
                ON person (county_id, flag) 
                INCLUDE (external_id, full_name, normalized_name, county_name, dm_primary);");
                
            // Update table statistics for better query planning
            migrationBuilder.Sql("ANALYZE person;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop custom SQL indexes first
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_person_normalized_name_gin_new;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_person_individuals_name_gin;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_person_businesses_name_gin;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_person_search_covering;");
            
            // Drop EF-created indexes
            migrationBuilder.DropIndex(
                name: "ix_person_county_flag_btree",
                table: "person");
                
            migrationBuilder.DropIndex(
                name: "ix_person_dm_primary_county",
                table: "person");
                
            migrationBuilder.DropIndex(
                name: "ix_person_dm_alternate_county",
                table: "person");
                
            migrationBuilder.DropIndex(
                name: "ix_person_external_id_county_btree",
                table: "person");
        }
    }
}
