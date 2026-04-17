using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherApi.External.Migrations
{
    /// <inheritdoc />
    public partial class addalertsunique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Alerts_LocationId",
                schema: "weather",
                table: "Alerts");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_LocationId_CreatedAt_Message",
                schema: "weather",
                table: "Alerts",
                columns: new[] { "LocationId", "CreatedAt", "Message" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Alerts_LocationId_CreatedAt_Message",
                schema: "weather",
                table: "Alerts");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_LocationId",
                schema: "weather",
                table: "Alerts",
                column: "LocationId");
        }
    }
}
