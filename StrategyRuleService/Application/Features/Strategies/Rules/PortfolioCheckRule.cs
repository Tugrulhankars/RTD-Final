using Application.Features.Strategies.Dtos;
using Domain.Entities;
using Infrastructure.Services.Grpc.Services;
using NRules.Fluent.Dsl;
using NRules;
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
        StockWorkflow ctx = null;
        When()
            .Match<StockWorkflow>(() => ctx, c => c.Step == 1);
        Then()
            .Do(_ => Execute(ctx));
    }
    private async void Execute(StockWorkflow ctx)
    {
        try
        {
            Console.WriteLine($"[PortfolioCheckRule] Execute çağrıldı - Symbol: {ctx.Symbol}, Step: {ctx.Step}, AccountId: {ctx.AccountId}");
            ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
            bool alreadyProcessed = ctx.StrategyEvents.Any(e => 
                e.RuleName == "PortfolioCheckRule" && 
                e.Step == ctx.Step && 
                e.Action == "CHECK");
            if (alreadyProcessed)
            {
                Console.WriteLine($"[PortfolioCheckRule] Zaten işlendi, atlanıyor - Symbol: {ctx.Symbol}, Step: {ctx.Step}");
                return;
            }
            bool inPortfolio = false;
            bool portfolioServiceAvailable = false;
            string reason = "Portföy kontrolü";
            string serviceStatus = "";
            Console.WriteLine($"[PortfolioCheckRule] PortfolioService kontrolü - PortfolioService null? {ctx.PortfolioService == null}, PortfolioId: {ctx.PortfolioId}, Symbol: {ctx.Symbol}");
            
            if (ctx.PortfolioService != null && ctx.PortfolioId > 0)
            {
                try
                {
                    portfolioServiceAvailable = true;
                    Console.WriteLine($"[PortfolioCheckRule] PortfolioService çağrılıyor - PortfolioId: {ctx.PortfolioId}, Symbol: {ctx.Symbol}");
                    inPortfolio = await ctx.PortfolioService(ctx.PortfolioId, ctx.Symbol);
                    Console.WriteLine($"[PortfolioCheckRule] PortfolioService sonucu - PortfolioId: {ctx.PortfolioId}, Symbol: {ctx.Symbol}, InPortfolio: {inPortfolio}");
                    serviceStatus = $"PortfolioService kullanıldı - Sonuç: {(inPortfolio ? "VAR" : "YOK")}";
                }
                catch (Exception ex)
                {
                    portfolioServiceAvailable = false;
                    serviceStatus = $"PortfolioService hatası: {ex.Message}";
                    Console.WriteLine($"[PortfolioCheckRule] ❌ PortfolioService EXCEPTION - PortfolioId: {ctx.PortfolioId}, Symbol: {ctx.Symbol}, Error: {ex.Message}");
                }
            }
            else
            {
                serviceStatus = $"PortfolioService mevcut değil veya PortfolioId set edilmemiş (PortfolioService null? {ctx.PortfolioService == null}, PortfolioId: {ctx.PortfolioId}) - Varsayılan: Hisse portföyde yok";
                Console.WriteLine($"[PortfolioCheckRule] ⚠️ PortfolioService veya PortfolioId eksik - PortfolioService null? {ctx.PortfolioService == null}, PortfolioId: {ctx.PortfolioId}");
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
                    Console.WriteLine($"[{ctx.Symbol}] ✅ Step 3'e geçildi - BuyRule tetiklenmeli - AccountId: {ctx.AccountId}");
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
