using Application.Features.Strategies.Dtos;
using Domain.Entities;
using Infrastructure.Services.Grpc.Services;
using NRules.Fluent.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Application.Features.Strategies.Rules;
public class PortfolioCheckRule : Rule
{
    public PortfolioCheckRule()
    {
    }
    public override void Define()
    {
        StockWorkflow ctx = new StockWorkflow();
        When()
            .Match<StockWorkflow>(() => ctx, c => c.Step == 1);
        Then()
            .Do(_ => Execute(ctx));
    }
    private async void Execute(StockWorkflow ctx)
    {
        try
        {
            ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
            bool alreadyProcessed = ctx.StrategyEvents.Any(e => 
                e.RuleName == "PortfolioCheckRule" && 
                e.Step == ctx.Step && 
                e.Action == "CHECK");
            if (alreadyProcessed)
            {
                return;
            }
            bool inPortfolio = false;
            bool portfolioServiceAvailable = false;
            string reason = "Portföy kontrolü";
            string serviceStatus = "";
            if (ctx.PortfolioService != null && ctx.PortfolioId > 0)
            {
                try
                {
                    portfolioServiceAvailable = true;
                    inPortfolio = await ctx.PortfolioService(ctx.PortfolioId, ctx.Symbol);
                    serviceStatus = "PortfolioService kullanıldı";
                }
                catch (Exception ex)
                {
                    portfolioServiceAvailable = false;
                    serviceStatus = $"PortfolioService hatası: {ex.Message}";
                }
            }
            else
            {
                serviceStatus = "PortfolioService mevcut değil veya PortfolioId set edilmemiş - Varsayılan: Hisse portföyde yok";
            }
            var portfolioCheckEvent = new StrategyEvent
            {
                StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                Step = ctx.Step,
                RuleName = "PortfolioCheckRule",
                Action = "CHECK",
                Reason = $"{reason} - Hisse {(inPortfolio ? "var" : "yok")} - {serviceStatus}",
                Price = ctx.CurrentPrice,
                Timestamp = DateTime.Now
            };
            ctx.InPortfolio = inPortfolio;
            ctx.StrategyEvents.Add(portfolioCheckEvent);
            if (ctx.InPortfolio)
            {
                Console.WriteLine($"[{ctx.Symbol}] Hisse senedi portföyde var → Step 2 (satış kontrolü) - Fiyat: {ctx.CurrentPrice:F2}, Zarar: {ctx.TotalLossPercent:F2}%");
                bool stepChangeExists = ctx.StrategyEvents.Any(e => 
                    e.RuleName == "PortfolioCheckRule" && 
                    e.Step == 2 && 
                    e.Action == "STEP_CHANGE");
                if (!stepChangeExists)
                {
                    ctx.Step = 2;
                    var stepChangeEvent = new StrategyEvent
                    {
                        StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                        Step = 2,
                        RuleName = "PortfolioCheckRule",
                        Action = "STEP_CHANGE",
                        Reason = "Portföyde hisse var - Satış kontrolüne geçiliyor (Step 2)",
                        Price = ctx.CurrentPrice,
                        Timestamp = DateTime.Now
                    };
                    ctx.StrategyEvents.Add(stepChangeEvent);
                }
            }
            else
            {
                Console.WriteLine($"[{ctx.Symbol}] Hisse senedi portföyde yok → Step 3 (alım kontrolü) - Fiyat: {ctx.CurrentPrice:F2}, Zarar: {ctx.TotalLossPercent:F2}%");
                bool stepChangeExists = ctx.StrategyEvents.Any(e => 
                    e.RuleName == "PortfolioCheckRule" && 
                    e.Step == 3 && 
                    e.Action == "STEP_CHANGE");
                if (!stepChangeExists)
                {
                    ctx.Step = 3;
                    var stepChangeEvent = new StrategyEvent
                    {
                        StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                        Step = 3,
                        RuleName = "PortfolioCheckRule",
                        Action = "STEP_CHANGE",
                        Reason = "Portföyde hisse yok - Alım kontrolüne geçiliyor (Step 3)",
                        Price = ctx.CurrentPrice,
                        Timestamp = DateTime.Now
                    };
                    ctx.StrategyEvents.Add(stepChangeEvent);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ctx.Symbol}] Portföy kontrolü sırasında hata: {ex.Message}");
            var errorEvent = new StrategyEvent
            {
                StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                Step = ctx.Step,
                RuleName = "PortfolioCheckRule",
                Action = "ERROR",
                Reason = $"Portföy kontrolü sırasında hata oluştu: {ex.Message}",
                Price = ctx.CurrentPrice,
                Timestamp = DateTime.Now
            };
            ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
            ctx.StrategyEvents.Add(errorEvent);
            ctx.Step = -1;
        }
    }
}
