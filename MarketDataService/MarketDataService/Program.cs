using MarketDataService.Helpers;
using MarketDataService.Services;
using MarketDataService.Services.Impl;

var builder = WebApplication.CreateBuilder(args);
string apiKey = "d2r0tthr01qluccqf96gd2r0tthr01qluccqf970";
builder.Services.AddSingleton<FinnhubClient>(sp => new FinnhubClient(apiKey));
builder.Services.AddSingleton<IStockQuoteService, StockQuoteService>();
builder.Services.AddSingleton<ICompanyProfileService, CompanyProfileService>();
builder.Services.AddSingleton<IFinancialMetricsService, FinancialMetricsService>();
builder.Services.AddSingleton<IMarketDataService, MarketDataService.Services.Impl.MarketDataService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

var app = builder.Build();
app.UseWebSockets();
app.MapControllers();
app.Run();
