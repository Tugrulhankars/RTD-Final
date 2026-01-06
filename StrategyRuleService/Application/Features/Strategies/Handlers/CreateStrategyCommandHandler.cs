using Application.Features.Strategies.Commands.Create;
using Application.Features.Strategies.Dtos;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Services.Grpc.Services;
using MediatR;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
namespace Application.Features.Strategies.Handlers;
public class CreateStrategyCommandHandler : IRequestHandler<CreateStrategyCommand, CreateStrategyResponse>
{
    private readonly INRulesService _nRulesService;
    private readonly IStrategyRepository _strategyRepository;
    private readonly IStrategyEventRepository _strategyEventRepository;
    private readonly IMarketDataService? _marketDataService;
    public CreateStrategyCommandHandler(
        INRulesService nRulesService, 
        IStrategyRepository strategyRepository,
        IStrategyEventRepository strategyEventRepository,
        IMarketDataService? marketDataService = null)
    {
        _nRulesService = nRulesService;
        _strategyRepository = strategyRepository;
        _strategyEventRepository = strategyEventRepository;
        _marketDataService = marketDataService;
    }
    public async Task<CreateStrategyResponse> Handle(CreateStrategyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.DurationHours.HasValue && request.DurationHours.Value < 0.0167)
            {
                throw new ArgumentException("İzleme süresi minimum 1 dakika (0.0167 saat) olmalıdır.");
            }
        var strategy = new Strategy
        {
            UserId = request.UserId,
            StrategyName = request.StrategyName,
            Description = request.Description ?? string.Empty,
            StockSymbol = request.StockSymbol,
            TransactionAmount = 0,
            TransactionPercentage = 100m,
            BuyThresholdPercent = request.EntryThresholdPercentage ?? -5.0m,
            ProfitTargetPercent = request.TakeProfitPercentage ?? 5.0m,
            StopLossPercent = request.StopLossPercentage ?? 2.0m,
            EntryThresholdPercentage = request.EntryThresholdPercentage ?? -5.0m,
            MaxTotalLoss = request.MaxLossLimitPercentage ?? (decimal)(request.TotalPercentLoss ?? 5.0m),
            Status = StrategyStatus.Waiting,
            StartDate = DateTime.Now,
            IsPositionOpen = false,
            RuleCount = 5,
            CurrentStep = 0,
            DurationHours = request.DurationHours.HasValue ? (int?)Math.Round(request.DurationHours.Value * 60) : null,
            ExpiryDate = request.DurationHours.HasValue 
                ? DateTime.Now.AddHours(request.DurationHours.Value)
                : (DateTime?)null,
            IsActive = true,
            AccountId = request.AccountId,
            PortfolioId = request.PortfolioId
        };
        decimal openingPrice = 0;
        decimal currentPrice = 0;
        decimal highPrice = 0;
        decimal lowPrice = 0;
        decimal previousClosePrice = 0;
        decimal change = 0;
        decimal percentChange = 0;
        try
        {
            if (_marketDataService != null && !string.IsNullOrEmpty(request.StockSymbol))
            {
                var stockInfo = await _marketDataService.GetStockInfo(request.StockSymbol);
                if (stockInfo?.Quote != null)
                {
                    openingPrice = stockInfo.Quote.OpenPrice;
                    currentPrice = stockInfo.Quote.CurrentPrice;
                    highPrice = stockInfo.Quote.HighPrice;
                    lowPrice = stockInfo.Quote.LowPrice;
                    previousClosePrice = stockInfo.Quote.PreviousClosePrice;
                    change = stockInfo.Quote.Change;
                    percentChange = stockInfo.Quote.PercentChange;
                }
                else
                {
                    openingPrice = (decimal)await _marketDataService.GetStockOpeningPrice(request.StockSymbol);
                    currentPrice = (decimal)await _marketDataService.GetStockCurrentPrice(request.StockSymbol);
                }
            }
        }
        catch (Exception ex)
        {
        }
        decimal transactionAmount = request.TransactionAmount ?? 0;
        if (transactionAmount <= 0)
        {
            if (request.Lot > 0 && currentPrice > 0)
                transactionAmount = request.Lot * currentPrice;
            else if (request.Lot > 0)
                transactionAmount = request.Lot;
        }
        strategy.TransactionAmount = transactionAmount;
        var savedStrategy = await _strategyRepository.AddAsync(strategy, cancellationToken);
        var strategyContext = new StockWorkflow
        {
            StrategyId = savedStrategy.Id,
            UserId = request.UserId,
            Symbol = request.StockSymbol.ToUpper().Trim(),
            OpeningPrice = openingPrice,
            CurrentPrice = currentPrice,
            HighPrice = highPrice,
            LowPrice = lowPrice,
            PreviousClosePrice = previousClosePrice,
            Change = change,
            PercentChange = percentChange,
            InPortfolio = false,
            TotalLossPercent = savedStrategy.MaxTotalLoss,
            StopLossPercent = savedStrategy.StopLossPercent,
            ProfitTargetPercent = savedStrategy.ProfitTargetPercent,
            EntryThresholdPercent = savedStrategy.EntryThresholdPercentage,
            MaxTotalLoss = savedStrategy.MaxTotalLoss,
            Now = DateTime.Now,
            TransactionAmount = transactionAmount,
            AccountId = request.AccountId ?? 0,
            PortfolioId = request.PortfolioId ?? 0,
            Step = 0
        };
        var strategyCreatedEvent = new Domain.Entities.StrategyEvent
        {
            StrategyId = savedStrategy.Id,
            Step = 0,
            RuleName = "StrategyCreated",
            Action = "CREATED",
            Reason = $"Strateji oluşturuldu: {request.StrategyName} - Symbol: {request.StockSymbol}",
            Price = strategyContext.CurrentPrice,
            Timestamp = DateTime.Now
        };
        await _strategyEventRepository.AddAsync(strategyCreatedEvent, cancellationToken);
        var strategyKey = $"Strategy_{savedStrategy.Id}";
        await _nRulesService.AddStrategyAsync(strategyKey, strategyContext);
            return new CreateStrategyResponse
            {
                Message = "Strateji oluşturuldu ve işleme alındı. Detay sayfasından ilerlemeyi takip edebilirsiniz.",
                Success = true,
                StrategyName = request.StrategyName,
                StockSymbol = request.StockSymbol,
                Status = "Waiting",
                StrategyId = savedStrategy.Id
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Strateji oluşturulurken hata oluştu: {ex.Message}", ex);
        }
    }
}
