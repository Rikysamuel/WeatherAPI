using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WeatherApi.External.Migrations
{
    /// <inheritdoc />
    public partial class add_weather_entities_nullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeatherData",
                schema: "weather",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Temperature = table.Column<double>(type: "double precision", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NextEventNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FeelsLike = table.Column<double>(type: "double precision", nullable: false),
                    Humidity = table.Column<double>(type: "double precision", nullable: false),
                    Pressure = table.Column<double>(type: "double precision", nullable: false),
                    WindSpeed = table.Column<double>(type: "double precision", nullable: false),
                    WindDirection = table.Column<double>(type: "double precision", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    UVI = table.Column<double>(type: "double precision", nullable: false),
                    Sunrise = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Sunset = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailySummaries",
                schema: "weather",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WeatherDataId = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    MinTemperature = table.Column<double>(type: "double precision", nullable: false),
                    MaxTemperature = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailySummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailySummaries_WeatherData_WeatherDataId",
                        column: x => x.WeatherDataId,
                        principalSchema: "weather",
                        principalTable: "WeatherData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HourlySummaries",
                schema: "weather",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WeatherDataId = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Temperature = table.Column<double>(type: "double precision", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourlySummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HourlySummaries_WeatherData_WeatherDataId",
                        column: x => x.WeatherDataId,
                        principalSchema: "weather",
                        principalTable: "WeatherData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailySummaries_WeatherDataId",
                schema: "weather",
                table: "DailySummaries",
                column: "WeatherDataId");

            migrationBuilder.CreateIndex(
                name: "IX_HourlySummaries_WeatherDataId",
                schema: "weather",
                table: "HourlySummaries",
                column: "WeatherDataId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailySummaries",
                schema: "weather");

            migrationBuilder.DropTable(
                name: "HourlySummaries",
                schema: "weather");

            migrationBuilder.DropTable(
                name: "WeatherData",
                schema: "weather");
        }
    }
}
