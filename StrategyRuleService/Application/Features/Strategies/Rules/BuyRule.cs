using Application.Features.Strategies.Dtos;
using Application.Services;
using Domain.Entities;
using Infrastructure.Services.Grpc.Services;
using NRules.Fluent.Dsl;
using StrategyRuleService.Protos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Application.Features.Strategies.Rules;
public class BuyRule : Rule
{
    public BuyRule()
    {
    }
    public override void Define()
    {
        StockWorkflow ctx = null;
        UserPreference userPref = null;
        When()
            .Match<StockWorkflow>(() => ctx, c => c.Step == 3)
            .Match<UserPreference>(() => userPref, 
                pref => pref.StrategyId == ctx.StrategyId && 
                        pref.Ticker == ctx.Symbol);
        Then()
            .Do(_ => Execute(ctx, userPref));
    }
    private async void Execute(StockWorkflow ctx, UserPreference userPref)
    {
        try
        {
            ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
            bool alreadyProcessed = ctx.StrategyEvents.Any(e => 
                e.RuleName == "BuyRule" && 
                e.Step == ctx.Step);
            if (alreadyProcessed)
            {
                return;
            }
            decimal entryThresholdPercent = (userPref != null && userPref.EntryThresholdPercentage != 0) 
                ? userPref.EntryThresholdPercentage 
                : (ctx.EntryThresholdPercent != 0 ? ctx.EntryThresholdPercent : -5.0m);
            if (ctx.OpeningPrice <= 0 || ctx.CurrentPrice <= 0)
            {
                var strategyEvent = new StrategyEvent
                {
                    StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                    Step = ctx.Step,
                    RuleName = "BuyRule",
                    Action = "NO_DATA",
                    Reason = $"Fiyat verisi eksik - OpeningPrice: {ctx.OpeningPrice:F2}, CurrentPrice: {ctx.CurrentPrice:F2}",
                    Price = ctx.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
                ctx.StrategyEvents.Add(strategyEvent);
                Console.WriteLine($"[{ctx.Symbol}] Fiyat verisi eksik - Alım kontrolü yapılamıyor");
                ctx.Step = -1;
                return;
            }
            string tickerFromPref = userPref?.Ticker ?? ctx.Symbol;
            bool tickerMatch = string.Equals(tickerFromPref, ctx.Symbol, StringComparison.OrdinalIgnoreCase);
            if (!tickerMatch)
            {
                Console.WriteLine($"[{ctx.Symbol}] WARNING: Ticker mismatch - UserPref.Ticker: {tickerFromPref}, StockWorkflow.Symbol: {ctx.Symbol}");
            }
            Console.WriteLine($"[BuyRule] Rule Triggered: User {ctx.UserId}, Ticker {ctx.Symbol}, Opening Price: {ctx.OpeningPrice:F2}, Current Price: {ctx.CurrentPrice:F2}");
            bool priceCondition = ctx.OpeningPrice > 0 && ctx.CurrentPrice < ctx.OpeningPrice;
            if (priceCondition)
            {
                decimal quantity = ctx.TransactionAmount > 0 && ctx.CurrentPrice > 0 
                    ? ctx.TransactionAmount / ctx.CurrentPrice 
                    : 1;
                decimal totalCost = quantity * ctx.CurrentPrice;
                var strategyEvent = new StrategyEvent
                {
                    StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                    Step = ctx.Step,
                    RuleName = "BuyRule",
                    Action = "BUY",
                    Reason = $"Şimdiki fiyat ({ctx.CurrentPrice:F2}) < Açılış fiyatı ({ctx.OpeningPrice:F2}) - ALIM YAPILDI",
                    Price = ctx.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                Console.WriteLine($"[{ctx.Symbol}] Şimdiki fiyat ({ctx.CurrentPrice:F2}) < Açılış fiyatı ({ctx.OpeningPrice:F2}) → Hisse senedi al");
                bool hasEnoughBalance = false;
                decimal accountBalance = 0;
                if (ctx.AccountService != null && ctx.AccountId > 0)
                {
                    try
                    {
                        accountBalance = await ctx.AccountService(ctx.AccountId);
                        hasEnoughBalance = accountBalance >= totalCost;
                        if (!hasEnoughBalance)
                        {
                            strategyEvent.Action = "BUY_INSUFFICIENT_BALANCE";
                            strategyEvent.Reason = $"Yetersiz bakiye - Gerekli: {totalCost:F2} TL, Mevcut: {accountBalance:F2} TL";
                            ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
                            ctx.StrategyEvents.Add(strategyEvent);
                            Console.WriteLine($"[{ctx.Symbol}] Yetersiz bakiye - Gerekli: {totalCost:F2} TL, Mevcut: {accountBalance:F2} TL");
                            ctx.Step = -1;
                            return;
                        }
                        Console.WriteLine($"[{ctx.Symbol}] Bakiye yeterli - Mevcut: {accountBalance:F2} TL, Gerekli: {totalCost:F2} TL");
                    }
                    catch (Exception ex)
                    {
                        strategyEvent.Action = "BUY_BALANCE_CHECK_ERROR";
                        strategyEvent.Reason = $"Bakiye kontrolü sırasında hata: {ex.Message}";
                        ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
                        ctx.StrategyEvents.Add(strategyEvent);
                        Console.WriteLine($"[{ctx.Symbol}] Bakiye kontrolü sırasında hata: {ex.Message}");
                        ctx.Step = -1;
                        return;
                    }
                }
                else
                {
                    Console.WriteLine($"[{ctx.Symbol}] AccountService mevcut değil, bakiye kontrolü yapılmadan devam ediliyor");
                }
                if (ctx.TradeService != null && ctx.AccountId > 0)
                {
                    try
                    {
                        var tradeRequest = new CreateTradeRequest
                        {
                            AccountId = ctx.AccountId,
                            Symbol = ctx.Symbol,
                            Quantity = (float)quantity,
                            Price = (float)ctx.CurrentPrice,
                            Type = TradeType.Buy
                        };
                        var tradeResponse = await ctx.TradeService(tradeRequest);
                        if (tradeResponse != null && tradeResponse.TradeId > 0)
                        {
                            strategyEvent.Reason += $" - TradeId: {tradeResponse.TradeId} - TradeService başarılı";
                            ctx.InPortfolio = true;
                            Console.WriteLine($"[{ctx.Symbol}] Alım emri başarıyla gönderildi - TradeId: {tradeResponse.TradeId}");
                        }
                        else
                        {
                            strategyEvent.Action = "BUY_FAILED";
                            strategyEvent.Reason += $" - Trade emri başarısız: {tradeResponse?.Message ?? "Bilinmeyen hata"}";
                            Console.WriteLine($"[{ctx.Symbol}] Alım emri başarısız: {tradeResponse?.Message ?? "Bilinmeyen hata"}");
                        }
                    }
                    catch (Exception ex)
                    {
                        strategyEvent.Action = "BUY_ERROR";
                        strategyEvent.Reason += $" - TradeService hatası: {ex.Message}";
                        Console.WriteLine($"[{ctx.Symbol}] Alım emri gönderilirken hata: {ex.Message}");
                    }
                }
                else
                {
                    strategyEvent.Action = "BUY_SIMULATED";
                    strategyEvent.Reason += " - TradeService mevcut değil veya AccountId set edilmemiş - Simüle mod (gerçek işlem yapılmadı)";
                    ctx.InPortfolio = true;
                    Console.WriteLine($"[{ctx.Symbol}] Trade service mevcut değil, simüle mod");
                }
                ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
                ctx.StrategyEvents.Add(strategyEvent);
                ctx.Step = 0;
            }
            else
            {
                var strategyEvent = new StrategyEvent
                {
                    StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                    Step = ctx.Step,
                    RuleName = "BuyRule",
                    Action = "NO_BUY",
                    Reason = $"Şimdiki fiyat ({ctx.CurrentPrice:F2}) >= Açılış fiyatı ({ctx.OpeningPrice:F2}) - Döngüye dön (Step 0)",
                    Price = ctx.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                Console.WriteLine($"[{ctx.Symbol}] Alım yapılmadı → Döngüye dön (Step 0) - Şimdiki fiyat ({ctx.CurrentPrice:F2}) >= Açılış fiyatı ({ctx.OpeningPrice:F2})");
                ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
                ctx.StrategyEvents.Add(strategyEvent);
                ctx.Step = 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ctx.Symbol}] Alım kontrolü sırasında hata: {ex.Message}");
            ctx.Step = -1;
        }
    }
}