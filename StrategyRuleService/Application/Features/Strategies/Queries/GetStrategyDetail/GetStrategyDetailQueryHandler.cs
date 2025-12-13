using Application.Services;
using Infrastructure.Services.Grpc.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
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
        // Stratejiyi bul
        var strategy = await _strategyRepository.GetAsync(
            s => s.Id == request.StrategyId && s.UserId == request.UserId,
            cancellationToken: cancellationToken);

        if (strategy == null)
        {
            throw new Exception("Strateji bulunamadı veya bu kullanıcıya ait değil.");
        }

        // Event'leri getir
        var events = await _strategyEventRepository.GetAllAsync(
            predicate: e => e.StrategyId == strategy.Id,
            orderBy: q => q.OrderBy(e => e.Timestamp),
            cancellationToken: cancellationToken);

        // Base DTO oluştur
        var strategyDto = new StrategyDetailDto
        {
            Id = strategy.Id,
            UserId = strategy.UserId,
            StrategyName = strategy.StrategyName,
            Description = strategy.Description,
            StockSymbol = strategy.StockSymbol,
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
            Events = events.Select(e => new GetStrategiesByUserId.StrategyEventDto
            {
                Id = e.Id,
                StrategyId = e.StrategyId,
                Step = e.Step,
                RuleName = e.RuleName,
                Action = e.Action,
                Reason = e.Reason,
                Price = e.Price,
                Timestamp = e.Timestamp
            }).ToList()
        };

        // Anlık piyasa bilgilerini al
        try
        {
            if (_marketDataService != null && !string.IsNullOrEmpty(strategy.StockSymbol))
            {
                var currentPrice = await _marketDataService.GetStockCurrentPrice(strategy.StockSymbol);
                var openingPrice = await _marketDataService.GetStockOpeningPrice(strategy.StockSymbol);

                strategyDto.CurrentPrice = (decimal)currentPrice;
                strategyDto.OpeningPrice = (decimal)openingPrice;
                strategyDto.LastPriceUpdate = DateTime.Now;

                // Fiyat değişim yüzdesi hesapla
                if (openingPrice > 0)
                {
                    strategyDto.PriceChangePercent = ((currentPrice - openingPrice) / openingPrice) * 100;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MarketDataService'den fiyat bilgisi alınamadı: {Symbol}", strategy.StockSymbol);
        }

        // Strateji durumu detayları
        var now = DateTime.Now;
        strategyDto.IsMarketOpen = now.TimeOfDay >= new TimeSpan(10, 0, 0) && 
                                   now.TimeOfDay <= new TimeSpan(17, 59, 0);

        // Son event'ten step bilgisini al
        var lastEvent = events.OrderByDescending(e => e.Timestamp).FirstOrDefault();
        if (lastEvent != null)
        {
            strategyDto.CurrentStep = $"Step {lastEvent.Step}: {lastEvent.RuleName} - {lastEvent.Action}";
        }

        return new GetStrategyDetailResponse
        {
            Strategy = strategyDto
        };
    }
}

