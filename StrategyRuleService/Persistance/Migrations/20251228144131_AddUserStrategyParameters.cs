using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace Persistance.Migrations
{
    public partial class AddUserStrategyParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EntryThresholdPercentage",
                table: "Strategies",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(
                name: "StopLossPercentage",
                table: "Strategies",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(
                name: "TakeProfitPercentage",
                table: "Strategies",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntryThresholdPercentage",
                table: "Strategies");
            migrationBuilder.DropColumn(
                name: "StopLossPercentage",
                table: "Strategies");
            migrationBuilder.DropColumn(
                name: "TakeProfitPercentage",
                table: "Strategies");
        }
    }
}
