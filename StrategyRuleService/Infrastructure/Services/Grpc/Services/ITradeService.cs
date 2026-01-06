using StrategyRuleService.Protos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Infrastructure.Services.Grpc.Services;
public interface ITradeService
{
    public Task<CreateTradeResponse> CreateTrade(CreateTradeRequest request);
}
