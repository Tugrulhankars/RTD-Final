using Grpc.Net.Client;
using Grpc.Core;
using PaymentService.Protos;
using Microsoft.Extensions.Logging;
using Google.Protobuf.WellKnownTypes;

namespace PaymentService.Services;

public class AccountServiceClient : IAccountService, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly AccountService.AccountServiceClient _client;
    private readonly ILogger<AccountServiceClient> _logger;

    public AccountServiceClient(string serverAddress, ILogger<AccountServiceClient> logger)
    {
        _logger = logger;
        _channel = GrpcChannel.ForAddress(serverAddress);
        _client = new AccountService.AccountServiceClient(_channel);
    }

    public async Task<bool> UpdateAccountBalanceAsync(int accountId, int userId, string firstName, string lastName, double amount)
    {
        try
        {
            _logger.LogInformation("gRPC UpdateAccountBalance çağrılıyor: AccountId={AccountId}, UserId={UserId}, Amount={Amount}", 
                accountId, userId, amount);

            var request = new UpdateBalanceRequest
            {
                AccountId = accountId,
                UserId = userId,
                FirstName = firstName ?? "",
                LastName = lastName ?? "",
                Amount = amount,
                TransactionTime = Timestamp.FromDateTime(DateTime.UtcNow)
            };

            var response = await _client.UpdateAccountBalanceAsync(request);

            if (!response.IsSuccess)
            {
                _logger.LogWarning("gRPC UpdateAccountBalance başarısız: AccountId={AccountId}, Error={Error}, Message={Message}", 
                    accountId, response.Error, response.Message);
                throw new Exception($"Hesap bakiyesi güncellenemedi: {response.Message} (Error: {response.Error})");
            }

            _logger.LogInformation("gRPC UpdateAccountBalance başarılı: AccountId={AccountId}, NewBalance={NewBalance}", 
                accountId, response.NewBalance);

            return response.IsSuccess;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC UpdateAccountBalance RPC hatası: AccountId={AccountId}, Status={Status}, Detail={Detail}", 
                accountId, ex.StatusCode, ex.Status.Detail);
            throw new Exception($"gRPC AccountService UpdateAccountBalance hatası: {ex.Status.Detail}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC UpdateAccountBalance genel hatası: AccountId={AccountId}", accountId);
            throw new Exception($"gRPC AccountService UpdateAccountBalance hatası: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}

