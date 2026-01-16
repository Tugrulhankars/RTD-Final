using Infrastructure.Services.Grpc.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using StrategyRuleService.Protos;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
namespace Infrastructure.Services.Grpc;
public static class GrpcServiceRegistration
{
    public static IServiceCollection AddGrpcServices(this IServiceCollection services, IConfiguration configuration)
    {
        var accountServiceAddress = configuration["GrpcServices:AccountServiceAddress"] ?? "https://localhost:5001";
        var portfolioServiceAddress = configuration["GrpcServices:PortfolioServiceAddress"] ?? "http://localhost:5002";
        var tradeServiceAddress = configuration["GrpcServices:TradeServiceAddress"] ?? "http://localhost:5003";
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
            var loggerFactory = provider.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("GrpcServiceRegistration");
            
            // HTTP/2 protokol hatası için özel yapılandırma (development için)
            var httpHandler = new HttpClientHandler();
            httpHandler.ServerCertificateCustomValidationCallback = 
                (message, cert, chain, errors) => true;
            
            var channelOptions = new GrpcChannelOptions
            {
                HttpHandler = httpHandler,
                MaxReceiveMessageSize = 4 * 1024 * 1024, // 4 MB
                MaxSendMessageSize = 4 * 1024 * 1024 // 4 MB
            };
            
            logger?.LogInformation("AccountService gRPC channel oluşturuluyor: Address={Address}", accountServiceAddress);
            
            try
            {
                var channel = GrpcChannel.ForAddress(accountServiceAddress, channelOptions);
                var accountServiceLogger = provider.GetService<Microsoft.Extensions.Logging.ILogger<Infrastructure.Services.Grpc.Services.AccountService>>();
                var accountService = new Infrastructure.Services.Grpc.Services.AccountService(accountServiceAddress, accountServiceLogger);
                logger?.LogInformation("AccountService gRPC client başarıyla oluşturuldu: Address={Address}", accountServiceAddress);
                return accountService;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "AccountService gRPC channel oluşturulurken hata: Address={Address}, Error={Error}", accountServiceAddress, ex.Message);
                throw;
            }
        });
        // HTTP/2 desteği için AppContext ayarı (uygulama başlangıcında bir kez)
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        
        services.AddScoped<IPortfolioService>(provider =>
        {
            var loggerFactory = provider.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("GrpcServiceRegistration");
            
            // HTTP/2 protokol hatası için özel yapılandırma (development için)
            var httpHandler = new HttpClientHandler();
            httpHandler.ServerCertificateCustomValidationCallback = 
                (message, cert, chain, errors) => true;
            
            var channelOptions = new GrpcChannelOptions
            {
                HttpHandler = httpHandler,
                MaxReceiveMessageSize = 4 * 1024 * 1024, // 4 MB
                MaxSendMessageSize = 4 * 1024 * 1024 // 4 MB
            };
            
            logger?.LogInformation("PortfolioService gRPC channel oluşturuluyor: Address={Address}", portfolioServiceAddress);
            
            try
            {
                var channel = GrpcChannel.ForAddress(portfolioServiceAddress, channelOptions);
                var client = new PortfolioService.PortfolioServiceClient(channel);
                var portfolioManagerLogger = provider.GetService<Microsoft.Extensions.Logging.ILogger<PortfolioManager>>();
                logger?.LogInformation("PortfolioService gRPC client başarıyla oluşturuldu: Address={Address}", portfolioServiceAddress);
                return new PortfolioManager(client, portfolioManagerLogger);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "PortfolioService gRPC channel oluşturulurken hata: Address={Address}, Error={Error}", portfolioServiceAddress, ex.Message);
                throw;
            }
        });
        
        services.AddScoped<ITradeService>(provider =>
        {
            // HTTP/2 protokol hatası için özel yapılandırma (development için)
            var httpHandler = new HttpClientHandler();
            httpHandler.ServerCertificateCustomValidationCallback = 
                (message, cert, chain, errors) => true;
            
            var channelOptions = new GrpcChannelOptions
            {
                HttpHandler = httpHandler,
                MaxReceiveMessageSize = 4 * 1024 * 1024, // 4 MB
                MaxSendMessageSize = 4 * 1024 * 1024 // 4 MB
            };
            
            var loggerFactory = provider.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("GrpcServiceRegistration");
            logger?.LogInformation("TradeService gRPC channel oluşturuluyor: Address={Address}", tradeServiceAddress);
            
            try
            {
                var channel = GrpcChannel.ForAddress(tradeServiceAddress, channelOptions);
                var client = new TradeService.TradeServiceClient(channel);
                var tradeManagerLogger = provider.GetService<Microsoft.Extensions.Logging.ILogger<TradeManager>>();
                logger?.LogInformation("TradeService gRPC client başarıyla oluşturuldu: Address={Address}", tradeServiceAddress);
                return new TradeManager(client, tradeManagerLogger);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "TradeService gRPC channel oluşturulurken hata: Address={Address}, Error={Error}", tradeServiceAddress, ex.Message);
                throw;
            }
        });
        services.AddScoped<IMarketDataService, MarketDataManager>();
        var authUserServiceBaseUrl = configuration["AuthUserService:BaseUrl"] ?? "http://localhost:8081";
        services.AddHttpClient<IUserService, UserService>(client =>
        {
            client.BaseAddress = new Uri(authUserServiceBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        return services;
    }
}
