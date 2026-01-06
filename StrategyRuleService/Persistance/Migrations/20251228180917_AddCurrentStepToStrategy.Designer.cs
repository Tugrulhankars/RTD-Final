
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Persistance.DatabaseContext;
#nullable disable
namespace Persistance.Migrations
{
    [DbContext(typeof(Context))]
    [Migration("20251228180917_AddCurrentStepToStrategy")]
    partial class AddCurrentStepToStrategy
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);
            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);
            modelBuilder.Entity("Domain.Entities.Strategy", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");
                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));
                    b.Property<decimal?>("BuyPrice")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");
                    b.Property<decimal>("BuyThresholdPercent")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");
                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");
                    b.Property<int?>("CurrentStep")
                        .HasColumnType("int");
                    b.Property<DateTime>("DeletedDate")
                        .HasColumnType("datetime2");
                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");
                    b.Property<int?>("DurationHours")
                        .HasColumnType("int");
                    b.Property<TimeSpan?>("EndTime")
                        .HasColumnType("time");
                    b.Property<decimal>("EntryThresholdPercentage")
                        .HasPrecision(18, 4)
                        .HasColumnType("decimal(18,4)");
                    b.Property<DateTime?>("ExpiryDate")
                        .HasColumnType("datetime2");
                    b.Property<DateTime?>("FinishTime")
                        .HasColumnType("datetime2");
                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");
                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");
                    b.Property<bool>("IsPositionOpen")
                        .HasColumnType("bit");
                    b.Property<decimal>("MaxTotalLoss")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");
                    b.Property<decimal?>("ProfitLoss")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");
                    b.Property<decimal>("ProfitTargetPercent")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");
                    b.Property<int>("RuleCount")
                        .HasColumnType("int");
                    b.Property<decimal?>("SellPrice")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");
                    b.Property<DateTime?>("StartDate")
                        .HasColumnType("datetime2");
                    b.Property<TimeSpan?>("StartTime")
                        .HasColumnType("time");
                    b.Property<int>("Status")
                        .HasColumnType("int");
                    b.Property<string>("StockSymbol")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");
                    b.Property<decimal>("StopLossPercent")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");
                    b.Property<decimal>("StopLossPercentage")
                        .HasPrecision(18, 4)
                        .HasColumnType("decimal(18,4)");
                    b.Property<string>("StrategyName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");
                    b.Property<int>("SuccessfulTransactions")
                        .HasColumnType("int");
                    b.Property<decimal>("TakeProfitPercentage")
                        .HasPrecision(18, 4)
                        .HasColumnType("decimal(18,4)");
                    b.Property<decimal>("TotalLoss")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");
                    b.Property<decimal>("TotalProfit")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");
                    b.Property<int>("TotalTransactions")
                        .HasColumnType("int");
                    b.Property<decimal>("TransactionAmount")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");
                    b.Property<decimal>("TransactionPercentage")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");
                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("datetime2");
                    b.Property<int>("UserId")
                        .HasColumnType("int");
                    b.HasKey("Id");
                    b.ToTable("Strategies");
                });
            modelBuilder.Entity("Domain.Entities.StrategyEvent", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");
                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));
                    b.Property<string>("Action")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");
                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");
                    b.Property<DateTime>("DeletedDate")
                        .HasColumnType("datetime2");
                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");
                    b.Property<decimal>("Price")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");
                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");
                    b.Property<string>("RuleName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");
                    b.Property<int>("Step")
                        .HasColumnType("int");
                    b.Property<int>("StrategyId")
                        .HasColumnType("int");
                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime2");
                    b.Property<DateTime>("UpdatedDate")
                        .HasColumnType("datetime2");
                    b.HasKey("Id");
                    b.ToTable("StrategyEvents");
                });
#pragma warning restore 612, 618
        }
    }
}
