using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WeatherService.Data;

#nullable disable

namespace WeatherService.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260409170000_AddRequestIndexesAndDisplayCity")]
    public partial class AddRequestIndexesAndDisplayCity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayCity",
                table: "Requests",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_City_Date",
                table: "Requests",
                columns: new[] { "City", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Requests_TimestampUtc",
                table: "Requests",
                column: "TimestampUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_City_Date",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Requests_TimestampUtc",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "DisplayCity",
                table: "Requests");
        }
    }
}
