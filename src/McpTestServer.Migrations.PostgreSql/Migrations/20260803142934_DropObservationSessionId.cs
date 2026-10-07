using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpTestServer.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class DropObservationSessionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionId",
                schema: "api",
                table: "Observations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                schema: "api",
                table: "Observations",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }
    }
}
