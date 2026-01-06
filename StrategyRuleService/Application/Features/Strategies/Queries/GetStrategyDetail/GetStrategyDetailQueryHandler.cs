using Application.Services;
using Infrastructure.Services.Grpc.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Application.Features.Strategies.Queries.GetStrategyDetail;
public class GetStrategyDetailQueryHandler : IRequestHandler<GetStrategyDetailQuery, GetStrategyDetailResponse>
{
    private readonly IStrategyRepository _strategyRepository;
    private readonly IStrategyEventRepository _strategyEventRepository;
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<GetStrategyDetailQueryHandler> _logger;
    public GetStrategyDetailQueryHandler(
        IStrategyRepository strategyRepository,
        IStrategyEventRepository strategyEventRepository,
        IMarketDataService marketDataService,
        ILogger<GetStrategyDetailQueryHandler> logger)
    {
        _strategyRepository = strategyRepository;
        _strategyEventRepository = strategyEventRepository;
        _marketDataService = marketDataService;
        _logger = logger;
    }
    public async Task<GetStrategyDetailResponse> Handle(GetStrategyDetailQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Strateji detayı isteniyor: StrategyId={StrategyId}, UserId={UserId}", 
                request.StrategyId, request.UserId);
            var strategy = await _strategyRepository.GetAsync(
                s => s.Id == request.StrategyId && s.UserId == request.UserId,
                cancellationToken: cancellationToken);
            if (strategy == null)
            {
                _logger.LogWarning("Strateji bulunamadı: StrategyId={StrategyId}, UserId={UserId}", 
                    request.StrategyId, request.UserId);
                throw new KeyNotFoundException($"Strateji bulunamadı (StrategyId: {request.StrategyId}, UserId: {request.UserId}). Strateji mevcut değil veya bu kullanıcıya ait değil.");
            }
            var now = DateTime.Now;
            if (strategy.ExpiryDate.HasValue && 
                strategy.ExpiryDate.Value < now && 
                strategy.Status == Domain.Enums.StrategyStatus.Active)
            {
                _logger.LogInformation(
                    "Strateji süresi doldu, pasif yapılıyor: StrategyId={StrategyId}, ExpiryDate={ExpiryDate}",
                    strategy.Id, strategy.ExpiryDate);
                strategy.Status = Domain.Enums.StrategyStatus.Inactive;
                strategy.IsActive = false;
                await _strategyRepository.UpdateAsync(strategy, cancellationToken);
                _logger.LogInformation("Strateji süresi doldu ve pasif yapıldı: StrategyId={StrategyId}, IsActive=false", strategy.Id);
            }
            var events = await _strategyEventRepository.GetAllAsync(
                predicate: e => e.StrategyId == strategy.Id,
                orderBy: q => q.OrderBy(e => e.Timestamp),
                cancellationToken: cancellationToken) ?? new List<Domain.Entities.StrategyEvent>();
            var strategyDto = new StrategyDetailDto
            {
                Id = strategy.Id,
                UserId = strategy.UserId,
                StrategyName = strategy.StrategyName ?? string.Empty,
                Description = strategy.Description ?? string.Empty,
                StockSymbol = strategy.StockSymbol ?? string.Empty,
                Status = strategy.Status.ToString(),
                StartDate = strategy.StartDate,
                FinishTime = strategy.FinishTime,
                BuyPrice = strategy.BuyPrice,
                SellPrice = strategy.SellPrice,
                ProfitLoss = strategy.ProfitLoss,
                IsPositionOpen = strategy.IsPositionOpen,
                TotalProfit = strategy.TotalProfit,
                TotalLoss = strategy.TotalLoss,
                TotalTransactions = strategy.TotalTransactions,
                SuccessfulTransactions = strategy.SuccessfulTransactions,
                DurationHours = strategy.DurationHours,
                ExpiryDate = strategy.ExpiryDate,
                IsActive = strategy.IsActive,
                Events = events?.Select(e => new GetStrategiesByUserId.StrategyEventDto
                {
                    Id = e.Id,
                    StrategyId = e.StrategyId,
                    Step = e.Step,
                    RuleName = e.RuleName ?? string.Empty,
                    Action = e.Action ?? string.Empty,
                    Reason = e.Reason ?? string.Empty,
                    Price = e.Price,
                    Timestamp = e.Timestamp
                }).ToList() ?? new List<GetStrategiesByUserId.StrategyEventDto>()
            };
            try
            {
                if (_marketDataService != null && !string.IsNullOrEmpty(strategy.StockSymbol))
                {
                    var stockInfo = await _marketDataService.GetStockInfo(strategy.StockSymbol);
                    if (stockInfo?.Quote != null)
                    {
                        strategyDto.CurrentPrice = stockInfo.Quote.CurrentPrice;
                        strategyDto.OpeningPrice = stockInfo.Quote.OpenPrice;
                        strategyDto.HighPrice = stockInfo.Quote.HighPrice;
                        strategyDto.LowPrice = stockInfo.Quote.LowPrice;
                        strategyDto.PreviousClosePrice = stockInfo.Quote.PreviousClosePrice;
                        strategyDto.Change = stockInfo.Quote.Change;
                        strategyDto.PercentChange = stockInfo.Quote.PercentChange;
                        strategyDto.LastPriceUpdate = DateTime.Now;
                        if (stockInfo.Quote.OpenPrice > 0)
                        {
                            strategyDto.PriceChangePercent = ((stockInfo.Quote.CurrentPrice - stockInfo.Quote.OpenPrice) / stockInfo.Quote.OpenPrice) * 100;
                        }
                    }
                    if (stockInfo?.Profile != null)
                    {
                        strategyDto.CompanyName = stockInfo.Profile.Name;
                        strategyDto.Exchange = stockInfo.Profile.Exchange;
                        strategyDto.Industry = stockInfo.Profile.Industry;
                        strategyDto.Currency = stockInfo.Profile.Currency;
                        strategyDto.Ipo = stockInfo.Profile.Ipo;
                    }
                    if (stockInfo?.Metrics != null)
                    {
                        strategyDto.Pe = stockInfo.Metrics.Pe;
                        strategyDto.Pb = stockInfo.Metrics.Pb;
                        strategyDto.Roe = stockInfo.Metrics.Roe;
                        strategyDto.NetMargin = stockInfo.Metrics.NetMargin;
                        strategyDto.DebtEquity = stockInfo.Metrics.DebtEquity;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MarketDataService'den veri alınamadı: {Symbol}", strategy.StockSymbol);
            }
            strategyDto.IsMarketOpen = now.TimeOfDay >= new TimeSpan(10, 0, 0) && 
                                       now.TimeOfDay <= new TimeSpan(17, 59, 0);
            if (strategy.CurrentStep.HasValue)
            {
                var lastEvent = events?.OrderByDescending(e => e.Timestamp).FirstOrDefault();
                if (lastEvent != null)
                {
                    strategyDto.CurrentStep = $"Step {strategy.CurrentStep.Value}: {lastEvent.RuleName} - {lastEvent.Action}";
                }
                else
                {
                    strategyDto.CurrentStep = $"Step {strategy.CurrentStep.Value}";
                }
            }
            else if (events != null && events.Any())
            {
                var lastEvent = events.OrderByDescending(e => e.Timestamp).FirstOrDefault();
                if (lastEvent != null)
                {
                    strategyDto.CurrentStep = $"Step {lastEvent.Step}: {lastEvent.RuleName} - {lastEvent.Action}";
                }
            }
            else
            {
                strategyDto.CurrentStep = "Step 0: Başlatılıyor...";
            }
            _logger.LogInformation("Strateji detayı başarıyla alındı: StrategyId={StrategyId}", strategy.Id);
            return new GetStrategyDetailResponse
            {
                Strategy = strategyDto,
                Events = strategyDto.Events
            };
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Strateji detayı alınırken beklenmeyen hata: StrategyId={StrategyId}, UserId={UserId}", 
                request.StrategyId, request.UserId);
            throw new Exception($"Strateji detayı alınırken hata oluştu: {ex.Message}", ex);
        }
    }
}
