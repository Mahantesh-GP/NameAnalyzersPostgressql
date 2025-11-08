using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhoneticAnalyzers.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MortgageSolutionUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "county",
                table: "person",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "county_id",
                table: "person",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "county_name",
                table: "person",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<char>(
                name: "flag",
                table: "person",
                type: "character(1)",
                maxLength: 1,
                nullable: false,
                defaultValue: 'U');

            migrationBuilder.CreateIndex(
                name: "ix_person_county_flag",
                table: "person",
                columns: new[] { "county_id", "flag" });

            migrationBuilder.CreateIndex(
                name: "ix_person_county_id",
                table: "person",
                column: "county_id");

            migrationBuilder.CreateIndex(
                name: "ix_person_flag",
                table: "person",
                column: "flag");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_person_county_flag",
                table: "person");

            migrationBuilder.DropIndex(
                name: "ix_person_county_id",
                table: "person");

            migrationBuilder.DropIndex(
                name: "ix_person_flag",
                table: "person");

            migrationBuilder.DropColumn(
                name: "county",
                table: "person");

            migrationBuilder.DropColumn(
                name: "county_id",
                table: "person");

            migrationBuilder.DropColumn(
                name: "county_name",
                table: "person");

            migrationBuilder.DropColumn(
                name: "flag",
                table: "person");
        }
    }
}
