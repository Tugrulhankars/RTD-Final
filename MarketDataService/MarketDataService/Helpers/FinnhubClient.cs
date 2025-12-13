using MarketDataService.Dtos;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace MarketDataService.Helpers;

public class FinnhubClient
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    public FinnhubClient(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
    }

    public async Task<StockQuoteDto> GetQuoteAsync(string ticker)
    {
        var url = $"https://finnhub.io/api/v1/quote?symbol={ticker}&token={_apiKey}";
        var json = await _httpClient.GetStringAsync(url);
        var quoteData = JsonSerializer.Deserialize<JsonElement>(json);

        var currentPrice = quoteData.GetProperty("c").GetDecimal();
        var previousClosePrice = quoteData.GetProperty("pc").GetDecimal();
        var change = currentPrice - previousClosePrice;
        
        // Sıfıra bölme hatasını önlemek için kontrol
        decimal percentChange = 0;
        if (previousClosePrice != 0)
        {
            percentChange = (change / previousClosePrice) * 100;
        }
        else if (currentPrice != 0)
        {
            // Eğer previous close 0 ise ama current price varsa, %100 değişim olarak kabul et
            percentChange = 100;
        }

        return new StockQuoteDto
        {
            Ticker = ticker,
            CurrentPrice = currentPrice,
            OpenPrice = quoteData.GetProperty("o").GetDecimal(),
            HighPrice = quoteData.GetProperty("h").GetDecimal(),
            LowPrice = quoteData.GetProperty("l").GetDecimal(),
            PreviousClosePrice = previousClosePrice,
            Change = change,
            PercentChange = percentChange,
            Timestamp = quoteData.GetProperty("t").GetInt64()
        };
    }

    public async Task<CompanyProfileDto> GetCompanyProfileAsync(string ticker)
    {
        var url = $"https://finnhub.io/api/v1/stock/profile2?symbol={ticker}&token={_apiKey}";
        var json = await _httpClient.GetStringAsync(url);
        var profileData = JsonSerializer.Deserialize<JsonElement>(json);

        return new CompanyProfileDto
        {
            Ticker = profileData.GetProperty("ticker").GetString(),
            Name = profileData.GetProperty("name").GetString(),
            Exchange = profileData.GetProperty("exchange").GetString(),
            Industry = profileData.GetProperty("finnhubIndustry").GetString(),
            Ipo = profileData.GetProperty("ipo").GetString(),
            Currency = profileData.GetProperty("currency").GetString()
        };
    }

    public async Task<FinancialMetricsDto> GetFinancialMetricsAsync(string ticker)
    {
        var url = $"https://finnhub.io/api/v1/stock/metric?symbol={ticker}&metric=all&token={_apiKey}";
        var json = await _httpClient.GetStringAsync(url);
        var root = JsonSerializer.Deserialize<JsonElement>(json);
        var metricsData = root.GetProperty("metric");

        return new FinancialMetricsDto
        {
            Pe = metricsData.TryGetProperty("peNormalizedAnnual", out var pe) ? pe.GetDecimal() : (decimal?)null,
            Pb = metricsData.TryGetProperty("pbAnnual", out var pb) ? pb.GetDecimal() : (decimal?)null,
            Roe = metricsData.TryGetProperty("roeQuarterly", out var roe) ? roe.GetDecimal() : (decimal?)null,
            NetMargin = metricsData.TryGetProperty("netMarginAnnual", out var nm) ? nm.GetDecimal() : (decimal?)null,
            DebtEquity = metricsData.TryGetProperty("debtEquityAnnual", out var de) ? de.GetDecimal() : (decimal?)null
        };
    }

    public async Task<WebSocket> ConnectWebSocketAsync()
    {
        var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri($"wss://ws.finnhub.io?token={_apiKey}"), CancellationToken.None);
        return socket;
    }

    public async Task SubscribeAsync(WebSocket socket, string ticker)
    {
        var msg = JsonSerializer.Serialize(new { type = "subscribe", symbol = ticker });
        var buffer = Encoding.UTF8.GetBytes(msg);
        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
