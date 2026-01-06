using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Grpc.Core;
using StrategyRuleService.Protos;
namespace Infrastructure.Services.Grpc.Services;
public class AccountService : IAccountService
{
    private readonly GrpcChannel _channel;
    private readonly string _serverAddress;
    public AccountService(string serverAddress)
    {
        _serverAddress = serverAddress;
        _channel = GrpcChannel.ForAddress(serverAddress);
    }
    public async Task<double> GetAccountBalanceAsync(int accountId)
    {
        try
        {
            var client = new AccountServiceClient(_channel);
            var request = new GetAccountBalanceRequest
            {
                AccountId = accountId
            };
            var response = await client.GetAccountBalanceAsync(request);
            if (response.Success)
            {
                return response.Balance;
            }
            throw new Exception($"Hesap bakiyesi alınamadı: {response.Message}");
        }
        catch (Exception ex)
        {
            throw new Exception($"GRPC AccountService GetAccountBalanceAsync hatası: {ex.Message}", ex);
        }
    }
    public async Task<bool> UpdateAccountBalanceAsync(int accountId, double newBalance)
    {
        try
        {
            var client = new AccountServiceClient(_channel);
            var request = new UpdateAccountBalanceRequest
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
    public AccountServiceClient(GrpcChannel channel)
    {
        _channel = channel;
    }
    public async Task<GetAccountBalanceResponse> GetAccountBalanceAsync(GetAccountBalanceRequest request)
    {
        await Task.Delay(1);
        return new GetAccountBalanceResponse
        {
            Success = true,
            Balance = 1000.0,
            Message = "Success"
        };
    }
    public async Task<UpdateAccountBalanceResponse> UpdateAccountBalanceAsync(UpdateAccountBalanceRequest request)
    {
        await Task.Delay(1);
        return new UpdateAccountBalanceResponse
        {
            Success = true,
            Message = "Updated successfully"
        };
    }
}
public class GetAccountBalanceRequest
{
    public int AccountId { get; set; }
}
public class GetAccountBalanceResponse
{
    public double Balance { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
public class UpdateAccountBalanceRequest
{
    public int AccountId { get; set; }
    public double NewBalance { get; set; }
}
public class UpdateAccountBalanceResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
