using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Strategies.Commands.Create;

public class CreateStrategyResponse
{
    public int Id { get; set; }
    public int StrategyId { get; set; }
    public string StrategyName { get; set; }
    public string StockSymbol { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
    public bool Success { get; set; }

    public CreateStrategyResponse(string message, bool success)
    {
        Message = message;
        Success = success;
    }

    public CreateStrategyResponse()
    {
        
    }
}
