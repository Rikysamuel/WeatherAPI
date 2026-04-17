using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherApi.External.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "weather");

            migrationBuilder.RenameTable(
                name: "Locations",
                newName: "Locations",
                newSchema: "weather");

            migrationBuilder.RenameTable(
                name: "Alerts",
                newName: "Alerts",
                newSchema: "weather");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Locations",
                schema: "weather",
                newName: "Locations");

            migrationBuilder.RenameTable(
                name: "Alerts",
                schema: "weather",
                newName: "Alerts");
        }
    }
}
