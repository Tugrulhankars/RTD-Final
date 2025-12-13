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
            //ActivitySource activitySource = new ActivitySource("CreateStrategyCommandHandler");
           // using var activity=activitySource.StartActivity("Handle CreateStrategyCommand");
           
            // Stratejiyi oluştur
        var strategy = new Strategy
        {
            UserId = request.UserId,
            StrategyName = request.StrategyName,
            Description = request.Description ?? string.Empty,
            StockSymbol = request.StockSymbol,
            TransactionAmount = 0,
            TransactionPercentage = 100m,
            BuyThresholdPercent = -5.0m,
            ProfitTargetPercent = 5.0m,
            StopLossPercent = 2.0m,
            MaxTotalLoss = (decimal)(request.TotalPercentLoss ?? 5.0m),
            Status = StrategyStatus.Active,
            StartDate = DateTime.Now,
            IsPositionOpen = false,
            RuleCount = 5 // TimeCheck, PortfolioCheck, Buy, Sell, Cancel
        };

        // MarketDataService'den gerçek fiyat bilgilerini al
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
                // Tüm market verilerini tek seferde al
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
                    // Fallback: Eski metodlar
                    openingPrice = (decimal)await _marketDataService.GetStockOpeningPrice(request.StockSymbol);
                    currentPrice = (decimal)await _marketDataService.GetStockCurrentPrice(request.StockSymbol);
                }
            }
        }
        catch (Exception ex)
        {
            // MarketDataService'den veri alınamazsa varsayılan değerler kullanılacak
            // UpdateContextAsync içinde tekrar denenecek
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

        // Stratejiyi veritabanına kaydet
        var savedStrategy = await _strategyRepository.AddAsync(strategy, cancellationToken);

        // Strateji context'i oluştur
        var strategyContext = new StockWorkflow
        {
            StrategyId = savedStrategy.Id,
            UserId = request.UserId,
            Symbol = request.StockSymbol,
            OpeningPrice = openingPrice, // MarketDataService'den alınan veya 0 (UpdateContextAsync'te tekrar alınacak)
            CurrentPrice = currentPrice,  // MarketDataService'den alınan veya 0 (UpdateContextAsync'te tekrar alınacak)
            HighPrice = highPrice,
            LowPrice = lowPrice,
            PreviousClosePrice = previousClosePrice,
            Change = change,
            PercentChange = percentChange,
            InPortfolio = false,
            TotalLossPercent = (decimal)(request.TotalPercentLoss ?? 5.0m),
            StopLossPercent = savedStrategy.StopLossPercent,
            ProfitTargetPercent = savedStrategy.ProfitTargetPercent,
            MaxTotalLoss = savedStrategy.MaxTotalLoss,
            Now = DateTime.Now,
            TransactionAmount = transactionAmount,
            AccountId = request.AccountId ?? 0,
            PortfolioId = request.PortfolioId ?? 0,
            Step = 0 // Başlangıç adımı
        };
        //activity.SetTag(request.UserId.ToString(),"Strateji Başladı" );

        // Strateji oluşturuldu event'ini oluştur
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
        
        // Event'i veritabanına kaydet
        await _strategyEventRepository.AddAsync(strategyCreatedEvent, cancellationToken);

        // NRules'a strateji ekle (Worker Service sürekli işleyecek)
        // Unique key için StrategyId kullan (StrategyName tekrar edebilir)
        var strategyKey = $"Strategy_{savedStrategy.Id}";
        await _nRulesService.AddStrategyAsync(strategyKey, strategyContext);
        
        // İlk kuralı tetiklemek için ProcessRulesAsync çağır
        await _nRulesService.ProcessRulesAsync();

        //activity.AddEvent(new("Starteji kurallara eklendi"));

            return new CreateStrategyResponse
            {
                Message = "Strateji oluşturuldu ve NRules'a eklendi",
                Success = true,
                StrategyName = request.StrategyName,
                StockSymbol = request.StockSymbol,
                Status = "Active",
                StrategyId = savedStrategy.Id
            };
        }
        catch (Exception ex)
        {
            // Log hatayı
            // Exception handling middleware'e iletmek için throw et
            throw new Exception($"Strateji oluşturulurken hata oluştu: {ex.Message}", ex);
        }
    }
}
