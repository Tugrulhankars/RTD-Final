using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace Persistance.Migrations
{
    public partial class AddDurationHoursAndExpiryDateToStrategy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurationHours",
                table: "Strategies",
                type: "int",
                nullable: true);
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "Strategies",
                type: "datetime2",
                nullable: true);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationHours",
                table: "Strategies");
            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "Strategies");
        }
    }
}
