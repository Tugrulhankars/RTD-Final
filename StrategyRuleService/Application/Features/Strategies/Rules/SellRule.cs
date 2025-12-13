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
    // NRules parameterless constructor gerektirir
    public SellRule()
    {
    }

    public override void Define()
    {
        StockWorkflow ctx = null;

        When()
            .Match<StockWorkflow>(() => ctx, c => c.Step == 2);

        Then()
            .Do(_ => Execute(ctx));
    }

    private async void Execute(StockWorkflow ctx)
    {
        try
        {
            // Aynı Step ve Action için daha önce event oluşturulmuş mu kontrol et
            ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
            bool alreadyProcessed = ctx.StrategyEvents.Any(e => 
                e.RuleName == "SellRule" && 
                e.Step == ctx.Step);
            
            if (alreadyProcessed)
            {
                // Bu Step için zaten işlem yapılmış, tekrar event oluşturma
                return;
            }
            
            // Resimdeki strateji: Şimdiki fiyat > Açılış ise sat
            if (ctx.CurrentPrice > ctx.OpeningPrice)
            {
                // Miktar hesapla (TransactionAmount / CurrentPrice)
                decimal quantity = ctx.TransactionAmount > 0 && ctx.CurrentPrice > 0 
                    ? ctx.TransactionAmount / ctx.CurrentPrice 
                    : 1; // Varsayılan 1 lot
                
                // StrategyEvent oluştur - SATIS YAPILDI
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
                
                Console.WriteLine($"[{ctx.Symbol}] Şimdiki fiyat > Açılış ({ctx.CurrentPrice:F2} > {ctx.OpeningPrice:F2}) → Portföydeki hisseyi sat");
                
                // TradingService'e satış emri gönder
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
                    // Trade service yoksa kullanıcıyı bilgilendir
                    strategyEvent.Action = "SELL_SIMULATED";
                    strategyEvent.Reason += " - TradeService mevcut değil veya AccountId set edilmemiş - Simüle mod (gerçek işlem yapılmadı)";
                    ctx.InPortfolio = false;
                    Console.WriteLine($"[{ctx.Symbol}] Trade service mevcut değil, simüle mod");
                }
                
                // Event'i context'e ekle
                ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
                ctx.StrategyEvents.Add(strategyEvent);
                
                ctx.Step = -1; // workflow sonu
            }
            // Resimdeki strateji: Toplam zarar > %5 ise sat
            else if (ctx.TotalLossPercent > 5)
            {
                // Miktar hesapla
                decimal quantity = ctx.TransactionAmount > 0 && ctx.CurrentPrice > 0 
                    ? ctx.TransactionAmount / ctx.CurrentPrice 
                    : 1;
                
                // StrategyEvent oluştur - ZARAR KESME SATISI
                var strategyEvent = new StrategyEvent
                {
                    StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                    Step = ctx.Step,
                    RuleName = "SellRule",
                    Action = "SELL",
                    Reason = $"Toplam zarar ({ctx.TotalLossPercent:F2}%) > %5 - ZARAR KESME SATISI",
                    Price = ctx.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                
                Console.WriteLine($"[{ctx.Symbol}] Toplam zarar > %5 ({ctx.TotalLossPercent:F2}%) → Portföydeki hisseyi sat");
                
                // TradingService'e satış emri gönder
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
                            Console.WriteLine($"[{ctx.Symbol}] Zarar kesme satış emri başarıyla gönderildi - TradeId: {tradeResponse.TradeId}");
                        }
                        else
                        {
                            strategyEvent.Action = "SELL_FAILED";
                            strategyEvent.Reason += $" - Trade emri başarısız: {tradeResponse?.Message ?? "Bilinmeyen hata"}";
                            Console.WriteLine($"[{ctx.Symbol}] Zarar kesme satış emri başarısız: {tradeResponse?.Message ?? "Bilinmeyen hata"}");
                        }
                    }
                    catch (Exception ex)
                    {
                        strategyEvent.Action = "SELL_ERROR";
                        strategyEvent.Reason += $" - TradeService hatası: {ex.Message}";
                        Console.WriteLine($"[{ctx.Symbol}] Zarar kesme satış emri gönderilirken hata: {ex.Message}");
                    }
                }
                else
                {
                    // Trade service yoksa kullanıcıyı bilgilendir
                    strategyEvent.Action = "SELL_SIMULATED";
                    strategyEvent.Reason += " - TradeService mevcut değil veya AccountId set edilmemiş - Simüle mod (gerçek işlem yapılmadı)";
                    ctx.InPortfolio = false;
                    Console.WriteLine($"[{ctx.Symbol}] Trade service mevcut değil, simüle mod");
                }
                
                // Event'i context'e ekle
                ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
                ctx.StrategyEvents.Add(strategyEvent);
                
                ctx.Step = -1; // workflow sonu
            }
            else
            {
                // StrategyEvent oluştur - SATIS YAPILMADI
                var strategyEvent = new StrategyEvent
                {
                    StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                    Step = ctx.Step,
                    RuleName = "SellRule",
                    Action = "NO_SELL",
                    Reason = $"Satış şartları oluşmadı - Fiyat: {ctx.CurrentPrice:F2}, Zarar: {ctx.TotalLossPercent:F2}% - SATIS YAPILMADI",
                    Price = ctx.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                
                Console.WriteLine($"[{ctx.Symbol}] Satış şartları oluşmadı → Bekle - Fiyat: {ctx.CurrentPrice:F2}, Zarar: {ctx.TotalLossPercent:F2}%");
                
                // Event'i context'e ekle
                ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
                ctx.StrategyEvents.Add(strategyEvent);
                
                ctx.Step = -1; // workflow sonu
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ctx.Symbol}] Satış kontrolü sırasında hata: {ex.Message}");
            ctx.Step = -1; // workflow sonu
        }
    }
}

