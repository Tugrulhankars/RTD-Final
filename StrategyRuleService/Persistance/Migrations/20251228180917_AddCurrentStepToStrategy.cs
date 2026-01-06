using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace Persistance.Migrations
{
    public partial class AddCurrentStepToStrategy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentStep",
                table: "Strategies",
                type: "int",
                nullable: true);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentStep",
                table: "Strategies");
        }
    }
}
