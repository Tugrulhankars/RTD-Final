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
    // NRules parameterless constructor gerektirir
    public BuyRule()
    {
    }

    public override void Define()
    {
        StockWorkflow ctx = null;

        When()
            .Match<StockWorkflow>(() => ctx, c => c.Step == 3);

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
                e.RuleName == "BuyRule" && 
                e.Step == ctx.Step);
            
            if (alreadyProcessed)
            {
                // Bu Step için zaten işlem yapılmış, tekrar event oluşturma
                return;
            }
            
            // Resimdeki strateji: Şimdiki fiyat < Açılış ise al
            if (ctx.OpeningPrice > 0 && ctx.CurrentPrice < ctx.OpeningPrice)
            {
                // Miktar hesapla (TransactionAmount / CurrentPrice)
                decimal quantity = ctx.TransactionAmount > 0 && ctx.CurrentPrice > 0 
                    ? ctx.TransactionAmount / ctx.CurrentPrice 
                    : 1; // Varsayılan 1 lot
                
                decimal totalCost = quantity * ctx.CurrentPrice;
                
                // StrategyEvent oluştur - ALIM YAPILACAK
                var strategyEvent = new StrategyEvent
                {
                    StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                    Step = ctx.Step,
                    RuleName = "BuyRule",
                    Action = "BUY",
                    Reason = $"Şimdiki fiyat ({ctx.CurrentPrice:F2}) < Açılış fiyatı ({ctx.OpeningPrice:F2}) - ALIM KONTROLÜ",
                    Price = ctx.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                
                Console.WriteLine($"[{ctx.Symbol}] Şimdiki fiyat < Açılış ({ctx.CurrentPrice:F2} < {ctx.OpeningPrice:F2}) → Hisse senedi al");
                
                // Önce hesapta yeterli para var mı kontrol et
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
                
                // TradingService'e alım emri gönder
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
                    // Trade service yoksa kullanıcıyı bilgilendir
                    strategyEvent.Action = "BUY_SIMULATED";
                    strategyEvent.Reason += " - TradeService mevcut değil veya AccountId set edilmemiş - Simüle mod (gerçek işlem yapılmadı)";
                    ctx.InPortfolio = true;
                    Console.WriteLine($"[{ctx.Symbol}] Trade service mevcut değil, simüle mod");
                }
                
                // Event'i context'e ekle (NRules session'da saklanacak)
                ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
                ctx.StrategyEvents.Add(strategyEvent);
                
                ctx.Step = -1; // workflow sonu
            }
            else if (ctx.OpeningPrice <= 0)
            {
                var strategyEvent = new StrategyEvent
                {
                    StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                    Step = ctx.Step,
                    RuleName = "BuyRule",
                    Action = "NO_DATA",
                    Reason = $"Açılış fiyatı alınamadı - MarketDataService yanıt vermiyor ya da fiyat 0 döndü. Şimdiki fiyat: {ctx.CurrentPrice:F2}",
                    Price = ctx.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                
                ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
                ctx.StrategyEvents.Add(strategyEvent);
                Console.WriteLine($"[{ctx.Symbol}] Açılış fiyatı bilinmediği için alım yapılmadı");
                ctx.Step = -1;
            }
            else
            {
                // StrategyEvent oluştur - ALIM YAPILMADI
                var strategyEvent = new StrategyEvent
                {
                    StrategyId = ctx.StrategyId > 0 ? ctx.StrategyId : 1,
                    Step = ctx.Step,
                    RuleName = "BuyRule",
                    Action = "NO_BUY",
                    Reason = $"Şimdiki fiyat ({ctx.CurrentPrice:F2}) >= Açılış fiyatı ({ctx.OpeningPrice:F2}) - ALIM YAPILMADI",
                    Price = ctx.CurrentPrice,
                    Timestamp = DateTime.Now
                };
                
                Console.WriteLine($"[{ctx.Symbol}] Alım yapılmadı - Fiyat çok yüksek ({ctx.CurrentPrice:F2} >= {ctx.OpeningPrice:F2})");
                
                // Event'i context'e ekle
                ctx.StrategyEvents = ctx.StrategyEvents ?? new List<StrategyEvent>();
                ctx.StrategyEvents.Add(strategyEvent);
                
                ctx.Step = -1; // workflow sonu
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{ctx.Symbol}] Alım kontrolü sırasında hata: {ex.Message}");
            ctx.Step = -1; // workflow sonu
        }
    }
}