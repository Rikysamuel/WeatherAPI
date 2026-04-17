using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherApi.External.Migrations
{
    /// <inheritdoc />
    public partial class ChangeColName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "weather",
                table: "Locations",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                schema: "weather",
                table: "Alerts",
                newName: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "weather",
                table: "Locations",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "weather",
                table: "Alerts",
                newName: "CreatedAtUtc");
        }
    }
}
