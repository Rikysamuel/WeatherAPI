using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherApi.External.Migrations
{
    /// <inheritdoc />
    public partial class addzipcode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                schema: "weather",
                table: "Locations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ZipCode",
                schema: "weather",
                table: "Locations");
        }
    }
}
