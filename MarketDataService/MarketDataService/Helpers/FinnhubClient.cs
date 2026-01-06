using MarketDataService.Dtos;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace MarketDataService.Helpers;

public class FinnhubClient
{
    private readonly string[] _apiKeys;
    private readonly HttpClient _httpClient;
    private int _requestCounter = 0;

    public FinnhubClient(string apiKey, string apiKey2 = null)
    {
        if (!string.IsNullOrEmpty(apiKey2))
        {
            _apiKeys = new[] { apiKey, apiKey2 };
        }
        else
        {
            _apiKeys = new[] { apiKey };
        }
        _httpClient = new HttpClient();
    }

    private string GetNextApiKey()
    {
        int index = Interlocked.Increment(ref _requestCounter) % _apiKeys.Length;
        return _apiKeys[index];
    }

    public async Task<StockQuoteDto> GetQuoteAsync(string ticker)
    {
        var apiKey = GetNextApiKey();
        var url = $"https://finnhub.io/api/v1/quote?symbol={ticker}&token={apiKey}";
        
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Finnhub API isteği başarısız: {response.StatusCode} - {errorContent}. " +
                $"API Key doğru yapılandırılmış mı kontrol edin. URL: {url.Replace(apiKey, "***")}");
        }
        
        var json = await response.Content.ReadAsStringAsync();
        var quoteData = JsonSerializer.Deserialize<JsonElement>(json);

        if (!quoteData.TryGetProperty("c", out var currentPriceElement) || 
            !quoteData.TryGetProperty("pc", out var previousCloseElement))
        {
            throw new InvalidOperationException(
                $"Finnhub API'den eksik veri döndü. Ticker: {ticker}, Response: {json}");
        }

        var currentPrice = currentPriceElement.GetDecimal();
        var previousClosePrice = previousCloseElement.GetDecimal();
        var change = currentPrice - previousClosePrice;
        
        decimal percentChange = 0;
        if (previousClosePrice != 0)
        {
            percentChange = (change / previousClosePrice) * 100;
        }
        else if (currentPrice != 0)
        {
            percentChange = 100;
        }

        var openPrice = quoteData.TryGetProperty("o", out var openElement) ? openElement.GetDecimal() : currentPrice;
        var highPrice = quoteData.TryGetProperty("h", out var highElement) ? highElement.GetDecimal() : currentPrice;
        var lowPrice = quoteData.TryGetProperty("l", out var lowElement) ? lowElement.GetDecimal() : currentPrice;
        var timestamp = quoteData.TryGetProperty("t", out var timestampElement) ? timestampElement.GetInt64() : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return new StockQuoteDto
        {
            Ticker = ticker,
            CurrentPrice = currentPrice,
            OpenPrice = openPrice,
            HighPrice = highPrice,
            LowPrice = lowPrice,
            PreviousClosePrice = previousClosePrice,
            Change = change,
            PercentChange = percentChange,
            Timestamp = timestamp
        };
    }

    public async Task<CompanyProfileDto> GetCompanyProfileAsync(string ticker)
    {
        var apiKey = GetNextApiKey();
        var url = $"https://finnhub.io/api/v1/stock/profile2?symbol={ticker}&token={apiKey}";
        
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Finnhub Company Profile API isteği başarısız: {response.StatusCode} - {errorContent}. " +
                $"Ticker: {ticker}");
        }
        
        var json = await response.Content.ReadAsStringAsync();
        var profileData = JsonSerializer.Deserialize<JsonElement>(json);

        return new CompanyProfileDto
        {
            Ticker = profileData.TryGetProperty("ticker", out var tickerElement) ? tickerElement.GetString() ?? ticker : ticker,
            Name = profileData.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty,
            Exchange = profileData.TryGetProperty("exchange", out var exchangeElement) ? exchangeElement.GetString() ?? string.Empty : string.Empty,
            Industry = profileData.TryGetProperty("finnhubIndustry", out var industryElement) ? industryElement.GetString() ?? string.Empty : string.Empty,
            Ipo = profileData.TryGetProperty("ipo", out var ipoElement) ? ipoElement.GetString() ?? string.Empty : string.Empty,
            Currency = profileData.TryGetProperty("currency", out var currencyElement) ? currencyElement.GetString() ?? "USD" : "USD"
        };
    }

    public async Task<FinancialMetricsDto> GetFinancialMetricsAsync(string ticker)
    {
        var apiKey = GetNextApiKey();
        var url = $"https://finnhub.io/api/v1/stock/metric?symbol={ticker}&metric=all&token={apiKey}";
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
        var apiKey = GetNextApiKey();
        var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri($"wss://ws.finnhub.io?token={apiKey}"), CancellationToken.None);
        return socket;
    }

    public async Task SubscribeAsync(WebSocket socket, string ticker)
    {
        var msg = JsonSerializer.Serialize(new { type = "subscribe", symbol = ticker });
        var buffer = Encoding.UTF8.GetBytes(msg);
        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
