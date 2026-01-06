using Infrastructure.Services.Grpc.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
namespace Infrastructure.Services.Grpc.Services;
public class MarketDataManager : IMarketDataService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MarketDataManager> _logger;
    private readonly string _baseUrl;
    public MarketDataManager(HttpClient httpClient, IConfiguration configuration, ILogger<MarketDataManager> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _baseUrl = _configuration["MarketDataService:BaseUrl"] ?? "http://localhost:5275";
        if (string.IsNullOrEmpty(_httpClient.BaseAddress?.ToString()))
        {
            _httpClient.BaseAddress = new Uri(_baseUrl);
        }
        _logger.LogInformation("MarketDataManager initialized with BaseUrl: {BaseUrl}", _baseUrl);
    }
    public async Task<float> GetStockCurrentPrice(string stockSymbol)
    {
        try
        {
            var endpoint = $"/api/quotes/{stockSymbol}/current";
            _logger.LogDebug("Getting current price for {Symbol} from {Endpoint}", stockSymbol, endpoint);
            var price = await _httpClient.GetFromJsonAsync<decimal>(endpoint);
            _logger.LogInformation("Current price for {Symbol}: {Price}", stockSymbol, price);
            return (float)price;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current price for {Symbol}", stockSymbol);
            throw;
        }
    }
    public async Task<float> GetStockOpeningPrice(string stockSymbol)
    {
        try
        {
            var endpoint = $"/api/quotes/{stockSymbol}/open";
            _logger.LogDebug("Getting opening price for {Symbol} from {Endpoint}", stockSymbol, endpoint);
            var price = await _httpClient.GetFromJsonAsync<decimal>(endpoint);
            _logger.LogInformation("Opening price for {Symbol}: {Price}", stockSymbol, price);
            return (float)price;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting opening price for {Symbol}", stockSymbol);
            throw;
        }
    }
    public async Task<StockInfoDto> GetStockInfo(string stockSymbol)
    {
        try
        {
            var endpoint = $"/api/stockinfo/{stockSymbol}";
            _logger.LogDebug("Getting stock info for {Symbol} from {Endpoint}", stockSymbol, endpoint);
            var stockInfo = await _httpClient.GetFromJsonAsync<StockInfoDto>(endpoint);
            if (stockInfo == null)
            {
                _logger.LogWarning("Stock info is null for {Symbol}", stockSymbol);
                throw new Exception($"Stock info not found for symbol: {stockSymbol}");
            }
            _logger.LogInformation("Stock info retrieved for {Symbol}: CurrentPrice={CurrentPrice}, OpenPrice={OpenPrice}, PercentChange={PercentChange}%", 
                stockSymbol, stockInfo.Quote?.CurrentPrice, stockInfo.Quote?.OpenPrice, stockInfo.Quote?.PercentChange);
            return stockInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock info for {Symbol}", stockSymbol);
            throw;
        }
    }
}
