using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace Persistance.Migrations
{
    public partial class AddAccountIdAndPortfolioIdToStrategy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "Strategies",
                type: "int",
                nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "PortfolioId",
                table: "Strategies",
                type: "int",
                nullable: true);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Strategies");
            migrationBuilder.DropColumn(
                name: "PortfolioId",
                table: "Strategies");
        }
    }
}
