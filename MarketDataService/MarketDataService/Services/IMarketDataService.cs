using MarketDataService.Dtos;
using System.Net.WebSockets;

namespace MarketDataService.Services;

public interface IMarketDataService
{
    Task BroadcastStockInfoAsync(string ticker, StockInfoDto stockInfo);
    void RegisterSocket(WebSocket socket, string ticker);   // <-- burayı ekle


}
