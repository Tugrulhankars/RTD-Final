using StrategyRuleService.Protos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Infrastructure.Services.Grpc.Services;
public class PortfolioManager : IPortfolioService
{
    private readonly PortfolioService.PortfolioServiceClient _portfolioServiceClient;
    public PortfolioManager(PortfolioService.PortfolioServiceClient portfolioServiceClient)
    {
        _portfolioServiceClient = portfolioServiceClient;
    }
    public async Task<bool> IsInPortfolio(int portfolioId, string symbol)
    {
        HasStockInPortfolioRequest request = new HasStockInPortfolioRequest
        {
            PortfolioId = portfolioId,
            Symbol = symbol
        };
         HasStockInPortfolioResponse response=await _portfolioServiceClient.HasStockInPortfolioAsync(request);
        return response.Result;
    }
}
