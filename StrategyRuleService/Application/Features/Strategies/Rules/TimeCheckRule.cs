using Application.Features.Strategies.Dtos;
using Domain.Entities;
using NRules.Fluent.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Strategies.Rules;

public class TimeCheckRule : Rule
{
    public override void Define()
    {
        StockWorkflow ctx = null;

        When()
            .Match<StockWorkflow>(() => ctx, c => c.Step == 0);

        Then()
            .Do(_ => Execute(ctx));
    }

    private void Execute(StockWorkflow ctx)
    {
        try
        {
            // Aynı Step ve Action için daha önce event oluşturulmuş mu kontrol et
            ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
            bool alreadyProcessed = ctx.StrategyEvents.Any(e => 
                e.RuleName == "TimeCheckRule" && 
                e.Step == ctx.Step);
            
            if (alreadyProcessed)
            {
                // Bu Step için zaten işlem yapılmış, tekrar event oluşturma
                return;
            }
            
            // StrategyEvent oluştur
            var strategyEvent = new StrategyEvent
            {
                StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                Step = ctx.Step,
                RuleName = "TimeCheckRule",
                Action = ctx.MarketOpen ? "CONTINUE" : "MARKET_CLOSED",
                Reason = ctx.MarketOpen ? "Piyasa açık (10:00-17:59)" : "Piyasa kapalı - Strateji gerçekleştirilemedi. Piyasa saatleri: 10:00-17:59",
                Price = ctx.CurrentPrice,
                Timestamp = DateTime.Now
            };
            
            //saat kontrolü işlem saati içerisind eise devam hisse senedi portföyde var mı bakılır. değil ise kapat
            if (ctx.MarketOpen)
            {
                Console.WriteLine($"[{ctx.Symbol}] Saat uygun (10:00 - 17:59) → Step 1 - Fiyat: {ctx.CurrentPrice:F2}, Zarar: {ctx.TotalLossPercent:F2}%");
                ctx.Step = 1;
            }
            else
            {
                Console.WriteLine($"[{ctx.Symbol}] Saat uygun değil → Kapat - Fiyat: {ctx.CurrentPrice:F2}");
                ctx.Step = -1;
            }
            
            // Event'i context'e ekle
            ctx.StrategyEvents.Add(strategyEvent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ctx.Symbol}] Zaman kontrolü sırasında hata: {ex.Message}");
            ctx.Step = -1;
        }
    }
}
