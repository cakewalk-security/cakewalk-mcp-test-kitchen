using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpTestServer.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMcpPatAndCallerEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CallerEmail",
                schema: "api",
                table: "Observations",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserMcpPats",
                schema: "api",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PatHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProtectedPat = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMcpPats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMcpPats_Email",
                schema: "api",
                table: "UserMcpPats",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMcpPats_PatHash",
                schema: "api",
                table: "UserMcpPats",
                column: "PatHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserMcpPats",
                schema: "api");

            migrationBuilder.DropColumn(
                name: "CallerEmail",
                schema: "api",
                table: "Observations");
        }
    }
}
