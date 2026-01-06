using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioService.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Portfolios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portfolios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockCertificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lot = table.Column<int>(type: "int", nullable: false),
                    IsSell = table.Column<bool>(type: "bit", nullable: false),
                    PricePerShare = table.Column<double>(type: "float", nullable: false),
                    BuyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SellDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PortfolioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockCertificates_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockCertificates_PortfolioId",
                table: "StockCertificates",
                column: "PortfolioId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockCertificates");

            migrationBuilder.DropTable(
                name: "Portfolios");
        }
    }
}
