using MarketDataService.Helpers;
using MarketDataService.Services;
using MarketDataService.Services.Impl;

var builder = WebApplication.CreateBuilder(args);
string apiKey = builder.Configuration["Finnhub:ApiKey"] 
    ?? throw new InvalidOperationException("Finnhub:ApiKey configuration değeri bulunamadı. Lütfen appsettings.json dosyasına Finnhub:ApiKey ekleyin.");
string apiKey2 = builder.Configuration["Finnhub:ApiKey2"];
builder.Services.AddSingleton<FinnhubClient>(sp => new FinnhubClient(apiKey, apiKey2));
builder.Services.AddSingleton<IStockQuoteService, StockQuoteService>();
builder.Services.AddSingleton<ICompanyProfileService, CompanyProfileService>();
builder.Services.AddSingleton<IFinancialMetricsService, FinancialMetricsService>();
builder.Services.AddSingleton<IMarketDataService, MarketDataService.Services.Impl.MarketDataService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:5286")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();
app.UseCors();
app.UseWebSockets();
app.MapControllers();
app.Run();
