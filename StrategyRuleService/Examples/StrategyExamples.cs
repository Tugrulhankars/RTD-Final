using Application.Services;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Examples;

public class StrategyExamples
{
    /// <summary>
    /// Basit alım-satım stratejisi örneği
    /// 1. Açılışın %5 altına düşerse al
    /// 2. %5 kar ederse sat
    /// 3. %2 zarar ederse sat (stop loss)
    /// </summary>
    public static Strategy CreateSimpleBuySellStrategy(string stockSymbol, decimal transactionAmount)
    {
        var strategy = new Strategy
        {
            StrategyName = $"Basit Strateji - {stockSymbol}",
            Description = $"Hisse senedi {stockSymbol} için basit alım-satım stratejisi",
            StockSymbol = stockSymbol,
            TransactionAmount = transactionAmount,
            BuyThresholdPercent = -5.0m,  // Açılışın %5 altına düşerse al
            ProfitTargetPercent = 5.0m,   // %5 kar hedefi
            StopLossPercent = 2.0m,       // %2 zarar kesme
            Status = StrategyStatus.Active,
            StartDate = DateTime.Now,
            IsPositionOpen = false
        };

        return strategy;
    }

    /// <summary>
    /// Resimdeki algoritma akışına göre THYAD stratejisi örneği
    /// </summary>
    public static List<Rule> CreateTHYADStrategy(int strategyId)
    {
        var rules = new List<Rule>();

        // 1. Toplam zarar kontrolü (%5)
        var totalLossRule = RuleBuilder.CreateTotalLossRule()
            .ForStrategy(strategyId)
            .WithOperator(OperatorType.GreaterThanOrEqual)
            .WithValue(5.0m) // %5
            .WithAction(ActionType.CLOSE)
            .WithOrder(1)
            .WithDescription("Toplam zarar %5'i geçerse stratejiyi kapat")
            .Build();
        rules.Add(totalLossRule);

        // 2. Fiyat kontrolü (Mevcut fiyat > Açılış fiyatı)
        var priceCheckRule = RuleBuilder.CreatePriceRule()
            .ForStrategy(strategyId)
            .WithOperator(OperatorType.GreaterThan)
            .WithField(FieldType.OpeningPrice) // Açılış fiyatı ile karşılaştır
            .WithAction(ActionType.WAIT) // StopLoss/TakeProfit kontrolüne git
            .WithOrder(2)
            .WithDescription("Mevcut fiyat açılış fiyatından yüksekse StopLoss/TakeProfit kontrol et")
            .Build();
        rules.Add(priceCheckRule);

        // 3. Hacim kontrolü (1 dakikalık hacim > Günlük hacim ortalaması)
        var volumeRule = RuleBuilder.CreateMinuteVolumeRule()
            .ForStrategy(strategyId)
            .WithOperator(OperatorType.GreaterThan)
            .WithField(FieldType.DailyVolume) // Günlük hacim ile karşılaştır
            .WithAction(ActionType.BUY)
            .WithOrder(3)
            .WithDescription("1 dakikalık hacim günlük hacmin üzerindeyse al")
            .Build();
        rules.Add(volumeRule);

        return rules;
    }

    /// <summary>
    /// Basit fiyat takip stratejisi
    /// </summary>
    public static List<Rule> CreateSimplePriceStrategy(int strategyId, decimal buyPrice, decimal sellPrice)
    {
        var rules = new List<Rule>();

        // Alış kuralı
        var buyRule = RuleBuilder.CreatePriceRule()
            .ForStrategy(strategyId)
            .WithOperator(OperatorType.LessThanOrEqual)
            .WithValue(buyPrice)
            .WithAction(ActionType.BUY)
            .WithOrder(1)
            .WithDescription($"Fiyat {buyPrice} TL'nin altına düşerse al")
            .Build();
        rules.Add(buyRule);

        // Satış kuralı
        var sellRule = RuleBuilder.CreatePriceRule()
            .ForStrategy(strategyId)
            .WithOperator(OperatorType.GreaterThanOrEqual)
            .WithValue(sellPrice)
            .WithAction(ActionType.SELL)
            .WithOrder(2)
            .WithDescription($"Fiyat {sellPrice} TL'nin üstüne çıkarsa sat")
            .Build();
        rules.Add(sellRule);

        return rules;
    }

    /// <summary>
    /// Hacim bazlı strateji
    /// </summary>
    public static List<Rule> CreateVolumeBasedStrategy(int strategyId, decimal volumeThreshold)
    {
        var rules = new List<Rule>();

        // Hacim patlaması kontrolü
        var volumeRule = RuleBuilder.CreateMinuteVolumeRule()
            .ForStrategy(strategyId)
            .WithOperator(OperatorType.GreaterThan)
            .WithValue(volumeThreshold)
            .WithAction(ActionType.BUY)
            .WithOrder(1)
            .WithDescription($"1 dakikalık hacim {volumeThreshold} üzerine çıkarsa al")
            .Build();
        rules.Add(volumeRule);

        return rules;
    }

    /// <summary>
    /// Açılış fiyatı bazlı strateji
    /// </summary>
    public static List<Rule> CreateOpeningPriceStrategy(int strategyId)
    {
        var rules = new List<Rule>();

        // Açılış fiyatından yüksekse al
        var buyRule = RuleBuilder.CreatePriceRule()
            .ForStrategy(strategyId)
            .WithOperator(OperatorType.GreaterThan)
            .WithField(FieldType.OpeningPrice)
            .WithAction(ActionType.BUY)
            .WithOrder(1)
            .WithDescription("Fiyat açılış fiyatının üzerine çıkarsa al")
            .Build();
        rules.Add(buyRule);

        // Açılış fiyatından düşükse sat
        var sellRule = RuleBuilder.CreatePriceRule()
            .ForStrategy(strategyId)
            .WithOperator(OperatorType.LessThan)
            .WithField(FieldType.OpeningPrice)
            .WithAction(ActionType.SELL)
            .WithOrder(2)
            .WithDescription("Fiyat açılış fiyatının altına düşerse sat")
            .Build();
        rules.Add(sellRule);

        return rules;
    }

    /// <summary>
    /// Son kapanış fiyatı bazlı strateji
    /// </summary>
    public static List<Rule> CreateLastClosingPriceStrategy(int strategyId)
    {
        var rules = new List<Rule>();

        // Son kapanış fiyatından yüksekse al
        var buyRule = RuleBuilder.CreatePriceRule()
            .ForStrategy(strategyId)
            .WithOperator(OperatorType.GreaterThan)
            .WithField(FieldType.LastClosingPrice)
            .WithAction(ActionType.BUY)
            .WithOrder(1)
            .WithDescription("Fiyat son kapanış fiyatının üzerine çıkarsa al")
            .Build();
        rules.Add(buyRule);

        return rules;
    }
}
