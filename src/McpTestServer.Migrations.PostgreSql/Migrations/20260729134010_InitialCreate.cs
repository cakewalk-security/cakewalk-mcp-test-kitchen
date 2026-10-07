using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace McpTestServer.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "api");

            migrationBuilder.CreateTable(
                name: "Observations",
                schema: "api",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Method = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Phase = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    HeadersJson = table.Column<string>(type: "text", nullable: true),
                    ResponseJson = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioProfiles",
                schema: "api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ProfileJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Observations_OccurredAt",
                schema: "api",
                table: "Observations",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioProfiles_IsActive",
                schema: "api",
                table: "ScenarioProfiles",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Observations",
                schema: "api");

            migrationBuilder.DropTable(
                name: "ScenarioProfiles",
                schema: "api");
        }
    }
}
