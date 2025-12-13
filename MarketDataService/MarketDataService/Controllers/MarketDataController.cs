using MarketDataService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

[ApiController]
[Route("ws/marketdata")]
public class MarketDataController : ControllerBase
{
    private readonly IMarketDataService _marketDataService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MarketDataController(IMarketDataService marketDataService, IHttpContextAccessor httpContextAccessor)
    {
        _marketDataService = marketDataService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet("{ticker}")]
    public async Task GetWebSocket(string ticker)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context.WebSockets.IsWebSocketRequest)
        {
            var socket = await context.WebSockets.AcceptWebSocketAsync();
            _marketDataService.RegisterSocket(socket, ticker);

            var buffer = new byte[1024 * 4];
            while (socket.State == WebSocketState.Open)
            {
                await Task.Delay(1000); // keep-alive
            }
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
}
