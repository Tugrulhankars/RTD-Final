using StrategyRuleService.Protos;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
namespace Infrastructure.Services.Grpc.Services;
public class TradeManager : ITradeService
{
    private readonly TradeService.TradeServiceClient _tradeServiceClient;
    private readonly ILogger<TradeManager>? _logger;
    
    public TradeManager(TradeService.TradeServiceClient tradeServiceClient, ILogger<TradeManager>? logger = null)
    {
        _tradeServiceClient = tradeServiceClient;
        _logger = logger;
    }
    
    public async Task<CreateTradeResponse> CreateTrade(CreateTradeRequest request)
    {
        try
        {
            _logger?.LogInformation("TradeService'e gRPC çağrısı yapılıyor: AccountId={AccountId}, Symbol={Symbol}, Quantity={Quantity}, Price={Price}, Type={Type}", 
                request.AccountId, request.Symbol, request.Quantity, request.Price, request.Type);
            
            var response = await _tradeServiceClient.CreateTradeAsync(request);
            
            _logger?.LogInformation("TradeService'den başarılı yanıt alındı: TradeId={TradeId}, Message={Message}", 
                response.TradeId, response.Message);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "TradeService gRPC hatası: AccountId={AccountId}, Symbol={Symbol}, Error={Error}", 
                request.AccountId, request.Symbol, ex.Message);
            throw new Exception($"TradeService gRPC hatası: {ex.Message}", ex);
        }
    }
}
