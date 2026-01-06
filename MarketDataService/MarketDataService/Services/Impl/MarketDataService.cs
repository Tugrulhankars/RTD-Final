using MarketDataService.Dtos;
using MarketDataService.Helpers;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace MarketDataService.Services.Impl;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;

public class MarketDataService : IMarketDataService
{
    private readonly ICompanyProfileService _profileService;
    private readonly IFinancialMetricsService _metricsService;
    private readonly FinnhubClient _finnhubClient;

    private readonly ConcurrentDictionary<string, List<WebSocket>> _tickerSubscriptions = new();

    public MarketDataService(
        ICompanyProfileService profileService,
        IFinancialMetricsService metricsService,
        FinnhubClient finnhubClient)
    {
        _profileService = profileService;
        _metricsService = metricsService;
        _finnhubClient = finnhubClient;

        _ = StartFinnhubStreamAsync();
    }

    public void RegisterSocket(WebSocket socket, string ticker)
    {
        _tickerSubscriptions.AddOrUpdate(
            ticker,
            new List<WebSocket> { socket },
            (key, list) => { list.Add(socket); return list; }
        );
    }

    public async Task BroadcastStockInfoAsync(string ticker, StockInfoDto stockInfo)
    {
        if (!_tickerSubscriptions.ContainsKey(ticker)) return;

        var json = JsonSerializer.Serialize(stockInfo);
        var buffer = Encoding.UTF8.GetBytes(json);

        foreach (var socket in _tickerSubscriptions[ticker])
        {
            if (socket.State == WebSocketState.Open)
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    private async Task StartFinnhubStreamAsync()
    {
        var socket = await _finnhubClient.ConnectWebSocketAsync();

        foreach (var ticker in _tickerSubscriptions.Keys)
        {
            await _finnhubClient.SubscribeAsync(socket, ticker);
        }

        var buffer = new byte[4096];

        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

                using var doc = JsonDocument.Parse(json);
                var ticker = doc.RootElement.GetProperty("s").GetString();
                var price = doc.RootElement.GetProperty("p").GetDecimal();
                var timestamp = doc.RootElement.GetProperty("t").GetInt64();

                if (ticker != null)
                {
                    var stockQuote = new StockQuoteDto
                    {
                        Ticker = ticker,
                        CurrentPrice = price,
                        OpenPrice = price,
                        HighPrice = price,
                        LowPrice = price,
                        PreviousClosePrice = price,
                        Change = 0,
                        PercentChange = 0,
                        Timestamp = timestamp
                    };

                    var stockInfo = new StockInfoDto
                    {
                        Quote = stockQuote,
                        Profile = null,
                        Metrics = null
                    };

                    await BroadcastStockInfoAsync(ticker, stockInfo);
                }
            }
        }
    }
}
