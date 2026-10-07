using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpTestServer.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class CodeFirstScenarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ScenarioProfiles_IsActive",
                schema: "api",
                table: "ScenarioProfiles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "api",
                table: "ScenarioProfiles");

            migrationBuilder.RenameColumn(
                name: "ProfileJson",
                schema: "api",
                table: "ScenarioProfiles",
                newName: "ParamsJson");

            migrationBuilder.AddColumn<string>(
                name: "ScenarioId",
                schema: "api",
                table: "ScenarioProfiles",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE api."ScenarioProfiles"
                SET "ScenarioId" = 'baseline'
                WHERE "ScenarioId" = '';
                """);

            migrationBuilder.AddColumn<string>(
                name: "ScenarioId",
                schema: "api",
                table: "Observations",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserScenarioSelections",
                schema: "api",
                columns: table => new
                {
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ScenarioId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ParamsJson = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserScenarioSelections", x => x.Email);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserScenarioSelections",
                schema: "api");

            migrationBuilder.DropColumn(
                name: "ScenarioId",
                schema: "api",
                table: "ScenarioProfiles");

            migrationBuilder.DropColumn(
                name: "ScenarioId",
                schema: "api",
                table: "Observations");

            migrationBuilder.RenameColumn(
                name: "ParamsJson",
                schema: "api",
                table: "ScenarioProfiles",
                newName: "ProfileJson");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "api",
                table: "ScenarioProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioProfiles_IsActive",
                schema: "api",
                table: "ScenarioProfiles",
                column: "IsActive");
        }
    }
}
