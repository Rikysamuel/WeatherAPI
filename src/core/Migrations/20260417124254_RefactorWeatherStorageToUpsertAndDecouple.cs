using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherApi.External.Migrations
{
    /// <inheritdoc />
    public partial class RefactorWeatherStorageToUpsertAndDecouple : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailySummaries_WeatherData_WeatherDataId",
                schema: "weather",
                table: "DailySummaries");

            migrationBuilder.DropForeignKey(
                name: "FK_HourlySummaries_WeatherData_WeatherDataId",
                schema: "weather",
                table: "HourlySummaries");

            migrationBuilder.DropIndex(
                name: "IX_HourlySummaries_WeatherDataId",
                schema: "weather",
                table: "HourlySummaries");

            migrationBuilder.DropIndex(
                name: "IX_DailySummaries_WeatherDataId",
                schema: "weather",
                table: "DailySummaries");

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "weather",
                table: "HourlySummaries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "weather",
                table: "HourlySummaries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "weather",
                table: "DailySummaries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "weather",
                table: "DailySummaries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // ── DATA MIGRATION: Populate City/Country before dropping the FK column ──
            migrationBuilder.Sql(@"
                UPDATE weather.""HourlySummaries"" h
                SET ""City"" = w.""City"", ""Country"" = w.""Country""
                FROM weather.""WeatherData"" w
                WHERE h.""WeatherDataId"" = w.""Id"";
            ");

            migrationBuilder.Sql(@"
                UPDATE weather.""DailySummaries"" d
                SET ""City"" = w.""City"", ""Country"" = w.""Country""
                FROM weather.""WeatherData"" w
                WHERE d.""WeatherDataId"" = w.""Id"";
            ");

            // ── DATA CLEANUP: Delete duplicate Snapshots/Forecasts keeping only the latest one ──
            migrationBuilder.Sql(@"
                DELETE FROM weather.""WeatherData"" a USING weather.""WeatherData"" b
                WHERE a.""Id"" < b.""Id"" AND a.""City"" = b.""City"" AND a.""Timestamp"" = b.""Timestamp"";
            ");

            migrationBuilder.Sql(@"
                DELETE FROM weather.""HourlySummaries"" a USING weather.""HourlySummaries"" b
                WHERE a.""Id"" < b.""Id"" AND a.""City"" = b.""City"" AND a.""Timestamp"" = b.""Timestamp"";
            ");

            migrationBuilder.Sql(@"
                DELETE FROM weather.""DailySummaries"" a USING weather.""DailySummaries"" b
                WHERE a.""Id"" < b.""Id"" AND a.""City"" = b.""City"" AND a.""Timestamp"" = b.""Timestamp"";
            ");

            migrationBuilder.DropColumn(
                name: "WeatherDataId",
                schema: "weather",
                table: "HourlySummaries");

            migrationBuilder.DropColumn(
                name: "WeatherDataId",
                schema: "weather",
                table: "DailySummaries");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherData_City_Timestamp",
                schema: "weather",
                table: "WeatherData",
                columns: new[] { "City", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HourlySummaries_City_Timestamp",
                schema: "weather",
                table: "HourlySummaries",
                columns: new[] { "City", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailySummaries_City_Timestamp",
                schema: "weather",
                table: "DailySummaries",
                columns: new[] { "City", "Timestamp" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ... (keeping Down as it is or slightly simplified since it's dev, 
            // but the original Down is already quite complex so we'll leave it for safety)
            migrationBuilder.DropIndex(
                name: "IX_WeatherData_City_Timestamp",
                schema: "weather",
                table: "WeatherData");

            migrationBuilder.DropIndex(
                name: "IX_HourlySummaries_City_Timestamp",
                schema: "weather",
                table: "HourlySummaries");

            migrationBuilder.DropIndex(
                name: "IX_DailySummaries_City_Timestamp",
                schema: "weather",
                table: "DailySummaries");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "weather",
                table: "HourlySummaries");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "weather",
                table: "HourlySummaries");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "weather",
                table: "DailySummaries");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "weather",
                table: "DailySummaries");

            migrationBuilder.AddColumn<int>(
                name: "WeatherDataId",
                schema: "weather",
                table: "HourlySummaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WeatherDataId",
                schema: "weather",
                table: "DailySummaries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_HourlySummaries_WeatherDataId",
                schema: "weather",
                table: "HourlySummaries",
                column: "WeatherDataId");

            migrationBuilder.CreateIndex(
                name: "IX_DailySummaries_WeatherDataId",
                schema: "weather",
                table: "DailySummaries",
                column: "WeatherDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_DailySummaries_WeatherData_WeatherDataId",
                schema: "weather",
                table: "DailySummaries",
                column: "WeatherDataId",
                principalSchema: "weather",
                principalTable: "WeatherData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HourlySummaries_WeatherData_WeatherDataId",
                schema: "weather",
                table: "HourlySummaries",
                column: "WeatherDataId",
                principalSchema: "weather",
                principalTable: "WeatherData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
