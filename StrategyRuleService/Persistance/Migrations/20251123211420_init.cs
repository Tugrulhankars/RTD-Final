using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace Persistance.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Strategies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StrategyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockSymbol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BuyThresholdPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProfitTargetPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StopLossPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinishTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BuyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SellPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ProfitLoss = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsPositionOpen = table.Column<bool>(type: "bit", nullable: false),
                    TotalProfit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalLoss = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTransactions = table.Column<int>(type: "int", nullable: false),
                    SuccessfulTransactions = table.Column<int>(type: "int", nullable: false),
                    MaxTotalLoss = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RuleCount = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strategies", x => x.Id);
                });
            migrationBuilder.CreateTable(
                name: "StrategyEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StrategyId = table.Column<int>(type: "int", nullable: false),
                    Step = table.Column<int>(type: "int", nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyEvents", x => x.Id);
                });
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Strategies");
            migrationBuilder.DropTable(
                name: "StrategyEvents");
        }
    }
}
