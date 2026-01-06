using StrategyRuleService.Protos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Infrastructure.Services.Grpc.Services;
public class TradeManager : ITradeService
{
    private readonly TradeService.TradeServiceClient _tradeServiceClient;
    public TradeManager(TradeService.TradeServiceClient tradeServiceClient)
    {
        _tradeServiceClient = tradeServiceClient;
    }
    public async Task<CreateTradeResponse> CreateTrade(CreateTradeRequest request)
    {
       CreateTradeResponse response=   await _tradeServiceClient.CreateTradeAsync(request);
       return response;
    }
}
