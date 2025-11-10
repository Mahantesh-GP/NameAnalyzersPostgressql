using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PhoneticAnalyzers.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultilingualSearchEntitiesFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "name_alias_cache",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    input_query = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    locale = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    cached_aliases = table.Column<string>(type: "jsonb", nullable: false),
                    expires_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    hit_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_accessed_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    query_hash = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_name_alias_cache", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nickname_maps",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    canonical_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nickname = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    locale = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    normalized_canonical_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_nickname = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_bidirectional = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    confidence = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 1.0m),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nickname_maps", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "person_names",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    canonical_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    locale_hint = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    script_hint = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dm_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    bm_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    last_enrichment_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_names", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "name_aliases",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    person_name_id = table.Column<long>(type: "bigint", nullable: false),
                    alias = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    alias_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    locale = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    script = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    confidence = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    normalized_alias = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dm_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    bm_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_name_aliases", x => x.id);
                    table.ForeignKey(
                        name: "FK_name_aliases_person_names_person_name_id",
                        column: x => x.person_name_id,
                        principalTable: "person_names",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_name_alias_cache_active",
                table: "name_alias_cache",
                columns: new[] { "input_query", "locale" });

            migrationBuilder.CreateIndex(
                name: "ix_name_alias_cache_expires",
                table: "name_alias_cache",
                column: "expires_utc");

            migrationBuilder.CreateIndex(
                name: "ix_name_alias_cache_hash_locale",
                table: "name_alias_cache",
                columns: new[] { "query_hash", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_name_alias_cache_hit_count",
                table: "name_alias_cache",
                column: "hit_count");

            migrationBuilder.CreateIndex(
                name: "ix_name_alias_cache_last_accessed",
                table: "name_alias_cache",
                column: "last_accessed_utc");

            migrationBuilder.CreateIndex(
                name: "ix_name_aliases_bm_code",
                table: "name_aliases",
                column: "bm_code",
                filter: "bm_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_name_aliases_dm_code",
                table: "name_aliases",
                column: "dm_code",
                filter: "dm_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_name_aliases_locale_type",
                table: "name_aliases",
                columns: new[] { "locale", "alias_type" });

            migrationBuilder.CreateIndex(
                name: "ix_name_aliases_normalized_alias_gin",
                table: "name_aliases",
                column: "normalized_alias")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_name_aliases_person_name_id",
                table: "name_aliases",
                column: "person_name_id");

            migrationBuilder.CreateIndex(
                name: "ix_name_aliases_source_confidence",
                table: "name_aliases",
                columns: new[] { "source", "confidence" });

            migrationBuilder.CreateIndex(
                name: "ix_name_aliases_unique",
                table: "name_aliases",
                columns: new[] { "person_name_id", "alias", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_nickname_maps_canonical_name_gin",
                table: "nickname_maps",
                column: "normalized_canonical_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_nickname_maps_canonical_name_locale",
                table: "nickname_maps",
                columns: new[] { "normalized_canonical_name", "locale" });

            migrationBuilder.CreateIndex(
                name: "ix_nickname_maps_confidence",
                table: "nickname_maps",
                column: "confidence");

            migrationBuilder.CreateIndex(
                name: "ix_nickname_maps_locale",
                table: "nickname_maps",
                column: "locale");

            migrationBuilder.CreateIndex(
                name: "ix_nickname_maps_nickname_gin",
                table: "nickname_maps",
                column: "normalized_nickname")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_nickname_maps_nickname_locale",
                table: "nickname_maps",
                columns: new[] { "normalized_nickname", "locale" });

            migrationBuilder.CreateIndex(
                name: "ix_nickname_maps_unique",
                table: "nickname_maps",
                columns: new[] { "canonical_name", "nickname", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_person_names_bm_code",
                table: "person_names",
                column: "bm_code",
                filter: "bm_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_person_names_canonical_name",
                table: "person_names",
                column: "canonical_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_person_names_dm_code",
                table: "person_names",
                column: "dm_code",
                filter: "dm_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_person_names_last_enrichment",
                table: "person_names",
                column: "last_enrichment_utc",
                filter: "last_enrichment_utc IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_person_names_locale_script",
                table: "person_names",
                columns: new[] { "locale_hint", "script_hint" });

            migrationBuilder.CreateIndex(
                name: "ix_person_names_normalized_name_gin",
                table: "person_names",
                column: "normalized_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "name_alias_cache");

            migrationBuilder.DropTable(
                name: "name_aliases");

            migrationBuilder.DropTable(
                name: "nickname_maps");

            migrationBuilder.DropTable(
                name: "person_names");
        }
    }
}
