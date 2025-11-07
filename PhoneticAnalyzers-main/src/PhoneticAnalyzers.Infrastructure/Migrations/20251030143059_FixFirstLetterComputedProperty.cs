using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PhoneticAnalyzers.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixFirstLetterComputedProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "person",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dm_primary = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    dm_alternate = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    dm_hash = table.Column<int>(type: "integer", nullable: false, computedColumnSql: "ABS(HASHTEXT(normalized_name)) % 64", stored: true),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person", x => x.id);
                },
                comment: "Person table partitioned by dm_hash for better performance");

            migrationBuilder.CreateTable(
                name: "person_bm",
                columns: table => new
                {
                    person_id = table.Column<long>(type: "bigint", nullable: false),
                    bm_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_bm", x => new { x.person_id, x.bm_code });
                    table.ForeignKey(
                        name: "FK_person_bm_person_person_id",
                        column: x => x.person_id,
                        principalTable: "person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Beider-Morse variants table partitioned by first_letter");

            migrationBuilder.CreateIndex(
                name: "ix_person_dm_alternate",
                table: "person",
                column: "dm_alternate",
                filter: "dm_alternate IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_person_dm_primary",
                table: "person",
                column: "dm_primary",
                filter: "dm_primary IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_person_external_id",
                table: "person",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_person_normalized_name_gin",
                table: "person",
                column: "normalized_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_person_bm_code",
                table: "person_bm",
                column: "bm_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "person_bm");

            migrationBuilder.DropTable(
                name: "person");
        }
    }
}
