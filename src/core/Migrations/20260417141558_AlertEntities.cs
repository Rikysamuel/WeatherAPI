using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WeatherApi.External.Migrations
{
    /// <inheritdoc />
    public partial class AlertEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Alerts_City",
                schema: "weather",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "weather",
                table: "Alerts");

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                schema: "weather",
                table: "Alerts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AlertSubscriptions",
                schema: "weather",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LocationId = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertSubscriptions_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "weather",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_LocationId",
                schema: "weather",
                table: "Alerts",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertSubscriptions_LocationId_Email",
                schema: "weather",
                table: "AlertSubscriptions",
                columns: new[] { "LocationId", "Email" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_Locations_LocationId",
                schema: "weather",
                table: "Alerts",
                column: "LocationId",
                principalSchema: "weather",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_Locations_LocationId",
                schema: "weather",
                table: "Alerts");

            migrationBuilder.DropTable(
                name: "AlertSubscriptions",
                schema: "weather");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_LocationId",
                schema: "weather",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "LocationId",
                schema: "weather",
                table: "Alerts");

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "weather",
                table: "Alerts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_City",
                schema: "weather",
                table: "Alerts",
                column: "City");
        }
    }
}
