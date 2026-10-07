using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpTestServer.Migrations.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class ObservationStatusAndRequestParams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResponseJson",
                schema: "api",
                table: "Observations",
                newName: "RequestParamsJson");

            migrationBuilder.AddColumn<int>(
                name: "StatusCode",
                schema: "api",
                table: "Observations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WasCancelled",
                schema: "api",
                table: "Observations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatusCode",
                schema: "api",
                table: "Observations");

            migrationBuilder.DropColumn(
                name: "WasCancelled",
                schema: "api",
                table: "Observations");

            migrationBuilder.RenameColumn(
                name: "RequestParamsJson",
                schema: "api",
                table: "Observations",
                newName: "ResponseJson");
        }
    }
}
