using Infrastructure.Services.Grpc.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using StrategyRuleService.Protos;
using Grpc.Net.Client;
namespace Infrastructure.Services.Grpc;
public static class GrpcServiceRegistration
{
    public static IServiceCollection AddGrpcServices(this IServiceCollection services, IConfiguration configuration)
    {
        var accountServiceAddress = configuration["GrpcServices:AccountServiceAddress"] ?? "https://localhost:5001";
        var portfolioServiceAddress = configuration["GrpcServices:PortfolioServiceAddress"] ?? "https://localhost:5002";
        var tradeServiceAddress = configuration["GrpcServices:TradeServiceAddress"] ?? "https://localhost:5003";
        var marketDataServiceAddress = configuration["GrpcServices:MarketDataServiceAddress"] ?? "https://localhost:5004";
        var marketDataServiceBaseUrl = configuration["MarketDataService:BaseUrl"] ?? "http://localhost:5275";
        services.AddHttpClient<IMarketDataService, MarketDataManager>(client =>
        {
            client.BaseAddress = new Uri(marketDataServiceBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(
                int.Parse(configuration["MarketDataService:TimeoutSeconds"] ?? "30")
            );
        });
        services.AddScoped<IAccountService>(provider =>
        {
            return new Infrastructure.Services.Grpc.Services.AccountService(accountServiceAddress);
        });
        services.AddScoped<IPortfolioService>(provider =>
        {
            var channel = GrpcChannel.ForAddress(portfolioServiceAddress);
            var client = new PortfolioService.PortfolioServiceClient(channel);
            return new PortfolioManager(client);
        });
        services.AddScoped<ITradeService>(provider =>
        {
            var channel = GrpcChannel.ForAddress(tradeServiceAddress);
            var client = new TradeService.TradeServiceClient(channel);
            return new TradeManager(client);
        });
        services.AddScoped<IMarketDataService, MarketDataManager>();
        var authUserServiceBaseUrl = configuration["AuthUserService:BaseUrl"] ?? "http://localhost:8080";
        services.AddHttpClient<IUserService, UserService>(client =>
        {
            client.BaseAddress = new Uri(authUserServiceBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        return services;
    }
}
