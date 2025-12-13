using Infrastructure.Services.Grpc.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Grpc.Services;

public interface IMarketDataService
{
    Task<float> GetStockCurrentPrice(string stockSymbol);
    Task<float> GetStockOpeningPrice(string stockSymbol);
    Task<StockInfoDto> GetStockInfo(string stockSymbol);
}
