using StrategyRuleService.Protos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
namespace Infrastructure.Services.Grpc.Services;
public class PortfolioManager : IPortfolioService
{
    private readonly PortfolioService.PortfolioServiceClient _portfolioServiceClient;
    private readonly ILogger<PortfolioManager>? _logger;
    
    public PortfolioManager(PortfolioService.PortfolioServiceClient portfolioServiceClient, ILogger<PortfolioManager>? logger = null)
    {
        _portfolioServiceClient = portfolioServiceClient;
        _logger = logger;
    }
    
    public async Task<bool> IsInPortfolio(int portfolioId, string symbol)
    {
        try
        {
            Console.WriteLine($"[PortfolioManager] IsInPortfolio çağrılıyor - PortfolioId={portfolioId}, Symbol={symbol}");
            _logger?.LogInformation("[PortfolioManager] IsInPortfolio çağrılıyor - PortfolioId={PortfolioId}, Symbol={Symbol}", portfolioId, symbol);
            
            HasStockInPortfolioRequest request = new HasStockInPortfolioRequest
            {
                PortfolioId = portfolioId,
                Symbol = symbol
            };
            
            Console.WriteLine($"[PortfolioManager] gRPC request gönderiliyor - PortfolioId={portfolioId}, Symbol={symbol}");
            _logger?.LogDebug("[PortfolioManager] gRPC request gönderiliyor - PortfolioId={PortfolioId}, Symbol={Symbol}", portfolioId, symbol);
            
            HasStockInPortfolioResponse response = await _portfolioServiceClient.HasStockInPortfolioAsync(request);
            
            Console.WriteLine($"[PortfolioManager] ✅ gRPC response alındı - PortfolioId={portfolioId}, Symbol={symbol}, Result={response.Result}");
            _logger?.LogInformation("[PortfolioManager] ✅ gRPC response alındı - PortfolioId={PortfolioId}, Symbol={Symbol}, Result={Result}", portfolioId, symbol, response.Result);
            
            return response.Result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PortfolioManager] ❌❌❌ EXCEPTION - PortfolioId={portfolioId}, Symbol={symbol}, Error={ex.Message}, InnerException={ex.InnerException?.Message}");
            _logger?.LogError(ex, "[PortfolioManager] ❌❌❌ EXCEPTION - PortfolioId={PortfolioId}, Symbol={Symbol}, Error={Error}", portfolioId, symbol, ex.Message);
            throw new Exception($"PortfolioService gRPC hatası: {ex.Message}", ex);
        }
    }
}
