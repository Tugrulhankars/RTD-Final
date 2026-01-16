using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Grpc.Core;
using StrategyRuleService.Protos;
using System.Net.Http;
using Microsoft.Extensions.Logging;
namespace Infrastructure.Services.Grpc.Services;
public class AccountService : IAccountService
{
    private readonly GrpcChannel _channel;
    private readonly string _serverAddress;
    private readonly ILogger<AccountService>? _logger;
    
    public AccountService(string serverAddress, ILogger<AccountService>? logger = null)
    {
        _serverAddress = serverAddress;
        _logger = logger;
        
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
        
        Console.WriteLine($"[AccountService Client Constructor] gRPC channel oluşturuluyor - Address={serverAddress}");
        _channel = GrpcChannel.ForAddress(serverAddress, channelOptions);
        Console.WriteLine($"[AccountService Client Constructor] gRPC channel oluşturuldu - Address={serverAddress}");
    }
    public async Task<double> GetAccountBalanceAsync(int accountId)
    {
        try
        {
            _logger?.LogInformation("[AccountService Client] GetAccountBalanceAsync çağrılıyor - AccountId={AccountId}, ServerAddress={ServerAddress}", accountId, _serverAddress);
            Console.WriteLine($"[AccountService Client] GetAccountBalanceAsync çağrılıyor - AccountId={accountId}, ServerAddress={_serverAddress}");
            
            var client = new AccountServiceClient(_channel);
            var request = new StrategyRuleService.Protos.GetAccountBalanceRequest
            {
                AccountId = accountId
            };
            
            _logger?.LogDebug("[AccountService Client] gRPC request gönderiliyor - AccountId={AccountId}", accountId);
            Console.WriteLine($"[AccountService Client] gRPC request gönderiliyor - AccountId={accountId}");
            
            var response = await client.GetAccountBalanceAsync(request);
            
            _logger?.LogInformation("[AccountService Client] gRPC response alındı - Success={Success}, Balance={Balance}, Message={Message}", response.Success, response.Balance, response.Message);
            Console.WriteLine($"[AccountService Client] gRPC response alındı - Success={response.Success}, Balance={response.Balance}, Message={response.Message}");
            
            if (response.Success)
            {
                _logger?.LogWarning("[AccountService Client] ✅✅✅ BAKİYE BAŞARILI - AccountId={AccountId}, Balance={Balance} TL", accountId, response.Balance);
                Console.WriteLine($"[AccountService Client] ✅✅✅ BAKİYE BAŞARILI - AccountId={accountId}, Balance={response.Balance} TL");
                return response.Balance;
            }
            
            _logger?.LogError("[AccountService Client] ❌ Bakiye alınamadı - AccountId={AccountId}, Message={Message}", accountId, response.Message);
            Console.WriteLine($"[AccountService Client] ❌ Bakiye alınamadı - AccountId={accountId}, Message={response.Message}");
            throw new Exception($"Hesap bakiyesi alınamadı: {response.Message}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[AccountService Client] ❌❌❌ EXCEPTION - AccountId={AccountId}, Error={Error}, InnerException={InnerException}", accountId, ex.Message, ex.InnerException?.Message);
            Console.WriteLine($"[AccountService Client] ❌❌❌ EXCEPTION - AccountId={accountId}, Error={ex.Message}, InnerException={ex.InnerException?.Message}, StackTrace={ex.StackTrace}");
            throw new Exception($"GRPC AccountService GetAccountBalanceAsync hatası: {ex.Message}", ex);
        }
    }
    public async Task<bool> UpdateAccountBalanceAsync(int accountId, double newBalance)
    {
        try
        {
            var client = new AccountServiceClient(_channel);
            var request = new StrategyRuleService.Protos.UpdateAccountBalanceRequest
            {
                AccountId = accountId,
                NewBalance = newBalance
            };
            var response = await client.UpdateAccountBalanceAsync(request);
            if (!response.Success)
            {
                throw new Exception($"Hesap bakiyesi güncellenemedi: {response.Message}");
            }
            return response.Success;
        }
        catch (Exception ex)
        {
            throw new Exception($"GRPC AccountService UpdateAccountBalanceAsync hatası: {ex.Message}", ex);
        }
    }
    public void Dispose()
    {
        _channel?.Dispose();
    }
}
public class AccountServiceClient
{
    private readonly GrpcChannel _channel;
    private readonly StrategyRuleService.Protos.AccountService.AccountServiceClient _client;
    
    public AccountServiceClient(GrpcChannel channel)
    {
        _channel = channel;
        _client = new StrategyRuleService.Protos.AccountService.AccountServiceClient(channel);
    }
    
    public async Task<StrategyRuleService.Protos.GetAccountBalanceResponse> GetAccountBalanceAsync(StrategyRuleService.Protos.GetAccountBalanceRequest request)
    {
        try
        {
            Console.WriteLine($"[AccountServiceClient] gRPC GetAccountBalance çağrılıyor - AccountId={request.AccountId}");
            var response = await _client.GetAccountBalanceAsync(request);
            Console.WriteLine($"[AccountServiceClient] gRPC response alındı - Success={response.Success}, Balance={response.Balance}, Message={response.Message}");
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AccountServiceClient] ❌❌❌ gRPC EXCEPTION - AccountId={request.AccountId}, Error={ex.Message}, InnerException={ex.InnerException?.Message}");
            throw new Exception($"gRPC GetAccountBalanceAsync hatası: {ex.Message}", ex);
        }
    }
    
    public async Task<StrategyRuleService.Protos.UpdateAccountBalanceResponse> UpdateAccountBalanceAsync(StrategyRuleService.Protos.UpdateAccountBalanceRequest request)
    {
        try
        {
            var response = await _client.UpdateAccountBalanceAsync(request);
            return response;
        }
        catch (Exception ex)
        {
            throw new Exception($"gRPC UpdateAccountBalanceAsync hatası: {ex.Message}", ex);
        }
    }
}
