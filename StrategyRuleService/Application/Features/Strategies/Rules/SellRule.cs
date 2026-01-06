using Application.Features.Strategies.Dtos;
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
public class SellRule : Rule
{
    public SellRule()
    {
    }
    public override void Define()
    {
        StockWorkflow ctx = null;
        UserPreference userPref = null;
        When()
            .Match<StockWorkflow>(() => ctx, c => c.Step == 2)
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
                e.RuleName == "SellRule" && 
                e.Step == ctx.Step);
            if (alreadyProcessed)
            {
                return;
            }
            decimal stopLossPercent = (userPref != null && userPref.StopLossPercentage > 0) 
                ? userPref.StopLossPercentage 
                : (ctx.StopLossPercent > 0 ? ctx.StopLossPercent : 2.0m);
            decimal takeProfitPercent = (userPref != null && userPref.TakeProfitPercentage > 0) 
                ? userPref.TakeProfitPercentage 
                : (ctx.ProfitTargetPercent > 0 ? ctx.ProfitTargetPercent : 5.0m);
            decimal maxLossLimit = (userPref != null && userPref.MaxLossLimitPercentage > 0) 
                ? userPref.MaxLossLimitPercentage 
                : (ctx.MaxTotalLoss > 0 ? ctx.MaxTotalLoss : 5.0m);
            if (ctx.CurrentPrice <= 0)
            {
                var strategyEvent = new StrategyEvent
                {
                    StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                    Step = ctx.Step,
                    RuleName = "SellRule",
                    Action = "NO_DATA",
                    Reason = $"Mevcut fiyat bilgisi eksik - CurrentPrice: {ctx.CurrentPrice:F2}",
                    Price = ctx.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
                ctx.StrategyEvents.Add(strategyEvent);
                Console.WriteLine($"[{ctx.Symbol}] Mevcut fiyat bilgisi eksik - Satış kontrolü yapılamıyor");
                ctx.Step = -1;
                return;
            }
            if (!ctx.BuyPrice.HasValue || ctx.BuyPrice.Value <= 0)
            {
                var strategyEvent = new StrategyEvent
                {
                    StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                    Step = ctx.Step,
                    RuleName = "SellRule",
                    Action = "NO_SELL",
                    Reason = $"Alış fiyatı bilinmediği için satış yapılamıyor - BuyPrice: {ctx.BuyPrice}",
                    Price = ctx.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
                ctx.StrategyEvents.Add(strategyEvent);
                Console.WriteLine($"[{ctx.Symbol}] Alış fiyatı bilinmediği için satış yapılamıyor");
                ctx.Step = -1;
                return;
            }
            decimal buyPrice = ctx.BuyPrice.Value;
            decimal stopLossPrice = buyPrice * (1 - stopLossPercent / 100);
            decimal takeProfitPrice = buyPrice * (1 + takeProfitPercent / 100);
            bool isValidDirection = takeProfitPrice > buyPrice && stopLossPrice < buyPrice;
            if (!isValidDirection)
            {
                var strategyEvent = new StrategyEvent
                {
                    StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                    Step = ctx.Step,
                    RuleName = "SellRule",
                    Action = "INVALID_PARAMETERS",
                    Reason = $"Yön tutarsızlığı - BuyPrice: {buyPrice:F2}, StopLoss: {stopLossPrice:F2}, TakeProfit: {takeProfitPrice:F2}. TakeProfit > BuyPrice ve StopLoss < BuyPrice olmalı",
                    Price = ctx.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
                ctx.StrategyEvents.Add(strategyEvent);
                Console.WriteLine($"[{ctx.Symbol}] Yön tutarsızlığı - Satış parametreleri geçersiz");
                ctx.Step = -1;
                return;
            }
            string tickerFromPref = userPref?.Ticker ?? ctx.Symbol;
            bool tickerMatch = string.Equals(tickerFromPref, ctx.Symbol, StringComparison.OrdinalIgnoreCase);
            if (!tickerMatch)
            {
                Console.WriteLine($"[{ctx.Symbol}] WARNING: Ticker mismatch - UserPref.Ticker: {tickerFromPref}, StockWorkflow.Symbol: {ctx.Symbol}");
            }
            Console.WriteLine($"[SellRule] Rule Triggered: User {ctx.UserId}, Ticker {ctx.Symbol}, BuyPrice: {buyPrice:F2}, Expected Stop Loss: {stopLossPrice:F2}, Expected Take Profit: {takeProfitPrice:F2}, Current Price: {ctx.CurrentPrice:F2}, Total Loss: {ctx.TotalLossPercent:F2}%, Max Loss Limit: {maxLossLimit:F2}%");
            if (ctx.TotalLossPercent <= -maxLossLimit)
            {
                decimal quantity = ctx.TransactionAmount > 0 && ctx.CurrentPrice > 0 
                    ? ctx.TransactionAmount / ctx.CurrentPrice 
                    : 1;
                var strategyEvent = new StrategyEvent
                {
                    StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                    Step = ctx.Step,
                    RuleName = "SellRule",
                    Action = "SELL",
                    Reason = $"Toplam zarar limiti aşıldı - Toplam Zarar: {ctx.TotalLossPercent:F2}% >= Maksimum Limit: {maxLossLimit:F2}% - ACİL SATIŞ EMRİ",
                    Price = ctx.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                Console.WriteLine($"[{ctx.Symbol}] ⚠️ TOPLAM ZARAR LİMİTİ AŞILDI - Toplam Zarar: {ctx.TotalLossPercent:F2}% >= Limit: {maxLossLimit:F2}% → ACİL SATIŞ EMRİ");
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
                            Type = TradeType.Sell
                        };
                        var tradeResponse = await ctx.TradeService(tradeRequest);
                        if (tradeResponse != null && tradeResponse.TradeId > 0)
                        {
                            strategyEvent.Reason += $" - TradeId: {tradeResponse.TradeId} - TradeService başarılı";
                            ctx.InPortfolio = false;
                            Console.WriteLine($"[{ctx.Symbol}] Toplam zarar limiti satış emri başarıyla gönderildi - TradeId: {tradeResponse.TradeId}");
                        }
                        else
                        {
                            strategyEvent.Action = "SELL_FAILED";
                            strategyEvent.Reason += $" - Trade emri başarısız: {tradeResponse?.Message ?? "Bilinmeyen hata"}";
                            Console.WriteLine($"[{ctx.Symbol}] Toplam zarar limiti satış emri başarısız: {tradeResponse?.Message ?? "Bilinmeyen hata"}");
                        }
                    }
                    catch (Exception ex)
                    {
                        strategyEvent.Action = "SELL_ERROR";
                        strategyEvent.Reason += $" - TradeService hatası: {ex.Message}";
                        Console.WriteLine($"[{ctx.Symbol}] Toplam zarar limiti satış emri gönderilirken hata: {ex.Message}");
                    }
                }
                else
                {
                    strategyEvent.Action = "SELL_SIMULATED";
                    strategyEvent.Reason += " - TradeService mevcut değil veya AccountId set edilmemiş - Simüle mod (gerçek işlem yapılmadı)";
                    ctx.InPortfolio = false;
                    Console.WriteLine($"[{ctx.Symbol}] Trade service mevcut değil, simüle mod");
                }
                ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
                ctx.StrategyEvents.Add(strategyEvent);
                ctx.Step = 0;
                return;
            }
            bool priceCondition = ctx.OpeningPrice > 0 && ctx.CurrentPrice > ctx.OpeningPrice;
            if (priceCondition)
            {
                decimal quantity = ctx.TransactionAmount > 0 && ctx.CurrentPrice > 0 
                    ? ctx.TransactionAmount / ctx.CurrentPrice 
                    : 1;
                var strategyEvent = new StrategyEvent
                {
                    StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                    Step = ctx.Step,
                    RuleName = "SellRule",
                    Action = "SELL",
                    Reason = $"Şimdiki fiyat ({ctx.CurrentPrice:F2}) > Açılış fiyatı ({ctx.OpeningPrice:F2}) - SATIS YAPILDI",
                    Price = ctx.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                Console.WriteLine($"[{ctx.Symbol}] Şimdiki fiyat ({ctx.CurrentPrice:F2}) > Açılış fiyatı ({ctx.OpeningPrice:F2}) → Portföydeki hisseyi sat");
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
                            Type = TradeType.Sell
                        };
                        var tradeResponse = await ctx.TradeService(tradeRequest);
                        if (tradeResponse != null && tradeResponse.TradeId > 0)
                        {
                            strategyEvent.Reason += $" - TradeId: {tradeResponse.TradeId} - TradeService başarılı";
                            ctx.InPortfolio = false;
                            Console.WriteLine($"[{ctx.Symbol}] Satış emri başarıyla gönderildi - TradeId: {tradeResponse.TradeId}");
                        }
                        else
                        {
                            strategyEvent.Action = "SELL_FAILED";
                            strategyEvent.Reason += $" - Trade emri başarısız: {tradeResponse?.Message ?? "Bilinmeyen hata"}";
                            Console.WriteLine($"[{ctx.Symbol}] Satış emri başarısız: {tradeResponse?.Message ?? "Bilinmeyen hata"}");
                        }
                    }
                    catch (Exception ex)
                    {
                        strategyEvent.Action = "SELL_ERROR";
                        strategyEvent.Reason += $" - TradeService hatası: {ex.Message}";
                        Console.WriteLine($"[{ctx.Symbol}] Satış emri gönderilirken hata: {ex.Message}");
                    }
                }
                else
                {
                    strategyEvent.Action = "SELL_SIMULATED";
                    strategyEvent.Reason += " - TradeService mevcut değil veya AccountId set edilmemiş - Simüle mod (gerçek işlem yapılmadı)";
                    ctx.InPortfolio = false;
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
                    RuleName = "SellRule",
                    Action = "NO_SELL",
                    Reason = $"Satış şartları oluşmadı - Fiyat: {ctx.CurrentPrice:F2}, Stop Loss: {stopLossPrice:F2}, Take Profit: {takeProfitPrice:F2}, Toplam Zarar: {ctx.TotalLossPercent:F2}% (Limit: {maxLossLimit:F2}%) - Döngüye dön (Step 0)",
                    Price = ctx.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                Console.WriteLine($"[{ctx.Symbol}] Satış şartları oluşmadı → Döngüye dön (Step 0) - Fiyat: {ctx.CurrentPrice:F2}, Stop Loss: {stopLossPrice:F2}, Take Profit: {takeProfitPrice:F2}, Zarar: {ctx.TotalLossPercent:F2}% (Limit: {maxLossLimit:F2}%)");
                ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
                ctx.StrategyEvents.Add(strategyEvent);
                ctx.Step = 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ctx.Symbol}] Satış kontrolü sırasında hata: {ex.Message}");
            ctx.Step = -1;
        }
    }
}
