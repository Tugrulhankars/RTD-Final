using Application.Services;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
namespace Examples;
public class StrategyExamples
{
    public static Strategy CreateSimpleBuySellStrategy(string stockSymbol, decimal transactionAmount)
    {
        var strategy = new Strategy
        {
            StrategyName = $"Basit Strateji - {stockSymbol}",
            Description = $"Hisse senedi {stockSymbol} için basit alım-satım stratejisi",
            StockSymbol = stockSymbol,
            TransactionAmount = transactionAmount,
            BuyThresholdPercent = -5.0m,
            ProfitTargetPercent = 5.0m,
            StopLossPercent = 2.0m,
            Status = StrategyStatus.Active,
            StartDate = DateTime.Now,
            IsPositionOpen = false
        };
        return strategy;
    }
    public static List<Rule> CreateTHYADStrategy(int strategyId)
    {
        var rules = new List<Rule>();
        var totalLossRule = RuleBuilder.CreateTotalLossRule()
            .ForStrategy(strategyId)
            .WithOperator(OperatorType.GreaterThanOrEqual)
            .WithValue(5.0m)
            .WithAction(ActionType.CLOSE)
            .WithOrder(1)
            .WithDescription("Toplam zarar %5'i geçerse stratejiyi kapat")
            .Build();
        rules.Add(totalLossRule);
        var priceCheckRule = RuleBuilder.CreatePriceRule()
            .ForStrategy(strategyId)
            .WithOperator(OperatorType.GreaterThan)
            .WithField(FieldType.OpeningPrice)
            .WithAction(ActionType.WAIT)
            .WithOrder(2)
            .WithDescription("Mevcut fiyat açılış fiyatından yüksekse StopLoss/TakeProfit kontrol et")
            .Build();
        rules.Add(priceCheckRule);
        var volumeRule = RuleBuilder.CreateMinuteVolumeRule()
            .ForStrategy(strategyId)
            .WithOperator(OperatorType.GreaterThan)
            .WithField(FieldType.DailyVolume)
            .WithAction(ActionType.BUY)
            .WithOrder(3)
            .WithDescription("1 dakikalık hacim günlük hacmin üzerindeyse al")
            .Build();
        rules.Add(volumeRule);
        return rules;
    }
    public static List<Rule> CreateSimplePriceStrategy(int strategyId, decimal buyPrice, decimal sellPrice)
    {
        var rules = new List<Rule>();
        var buyRule = RuleBuilder.CreatePriceRule()
            .ForStrategy(strategyId)
            .WithOperator(OperatorType.LessThanOrEqual)
            .WithValue(buyPrice)
            .WithAction(ActionType.BUY)
            .WithOrder(1)
            .WithDescription($"Fiyat {buyPrice} TL'nin altına düşerse al")
            .Build();
        rules.Add(buyRule);
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
    public static List<Rule> CreateVolumeBasedStrategy(int strategyId, decimal volumeThreshold)
    {
        var rules = new List<Rule>();
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
    public static List<Rule> CreateOpeningPriceStrategy(int strategyId)
    {
        var rules = new List<Rule>();
        var buyRule = RuleBuilder.CreatePriceRule()
            .ForStrategy(strategyId)
            .WithOperator(OperatorType.GreaterThan)
            .WithField(FieldType.OpeningPrice)
            .WithAction(ActionType.BUY)
            .WithOrder(1)
            .WithDescription("Fiyat açılış fiyatının üzerine çıkarsa al")
            .Build();
        rules.Add(buyRule);
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
    public static List<Rule> CreateLastClosingPriceStrategy(int strategyId)
    {
        var rules = new List<Rule>();
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
