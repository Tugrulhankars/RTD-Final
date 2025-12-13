using AccountService.Configuration;
using AccountService.Dtos.Request;
using AccountService.Dtos.Response;
using AccountService.Events;
using AccountService.Models;
using AccountService.Protos;
using AccountService.Repositories;
using Grpc.Core;
using System.Transactions;
using static AccountService.Protos.AccountService;
using UpdateBalanceRequest = AccountService.Dtos.Request.UpdateBalanceRequest;
using UpdateBalanceResponse = AccountService.Dtos.Response.UpdateBalanceResponse;

namespace AccountService.Services;

public class AccountService : AccountServiceBase,IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly KafkaConsumerService<UserCreatedEvent> _kafkaConsumerService;
    private readonly IRabbitMQPublisher _rabbitMQPublisher;
    public AccountService(IAccountRepository accountRepository,KafkaConsumerService<UserCreatedEvent> kafkaConsumerService, IRabbitMQPublisher rabbitMQPublisher)
    {
        _accountRepository = accountRepository;
        _kafkaConsumerService = kafkaConsumerService;
        _rabbitMQPublisher = rabbitMQPublisher;
    }
    public async Task CreateAccount()
    {
        UserCreatedEvent userCreatedEvent = await _kafkaConsumerService.Consume("user-created");
        Account account = new();
        account.Balance = 0;
        account.UserId = userCreatedEvent.UserId;
        account.FirstName = userCreatedEvent.FirstName;
        account.LastName = userCreatedEvent.LastName;
        account.Email = userCreatedEvent.Email;
        account.AccountStatus = AccountStatus.ACTIVE;

        await _accountRepository.AddAsync(account);
    }

    public async Task<CreateAccountResponse> CreateAccount(CreateAccountRequest request)
    {
        try
        {
            var existingAccount = await _accountRepository.GetAsync(a => a.UserId == request.UserId);
            if (existingAccount != null)
            {
                return new CreateAccountResponse
                {
                    IsSuccess = false,
                    Message = "Bu kullanıcının zaten bir hesabı bulunmaktadır.",
                    AccountId = 0
                };
            }

            Account account = new()
            {
                UserId = request.UserId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Balance = 0,
                AccountStatus = AccountStatus.ACTIVE
            };

            var createdAccount = await _accountRepository.AddAsync(account);
            
            return new CreateAccountResponse
            {
                IsSuccess = true,
                Message = "Hesap başarıyla oluşturuldu.",
                AccountId = createdAccount.Id
            };
        }
        catch (Exception ex)
        {
            return new CreateAccountResponse
            {
                IsSuccess = false,
                Message = $"Hesap oluşturulurken hata oluştu: {ex.Message}",
                AccountId = 0
            };
        }
    }

    public async Task<GetAccountByUserResponse> GetAccountByUser(int userId)
    {
        Account account = await _accountRepository.GetAsync(u => u.UserId == userId);
        if (account == null)
        {
            return null;
        }
        
        GetAccountByUserResponse response = new();
        response.AccountId = account.Id;
        response.AccountStatus = account.AccountStatus;
        response.Balance = account.Balance;
        response.FirstName = account.FirstName;
        response.LastName = account.LastName;
        return response;
    }

    public async Task<UpdateBalanceResponse> UpdateBalance(Dtos.Request.UpdateBalanceRequest request)
    {
        try
        {
            Account account = await _accountRepository.GetAsync(a => a.Id == request.AccountId && a.UserId == request.UserId);
            if (account == null)
            {
                return new UpdateBalanceResponse
                {
                    IsSuccess = false,
                    Message = "Hesap bulunamadı.",
                    NewBalance = 0
                };
            }

            if (request.Amount < 0 && account.Balance < Math.Abs(request.Amount))
            {
                return new UpdateBalanceResponse
                {
                    IsSuccess = false,
                    Message = "Yetersiz bakiye.",
                    NewBalance = account.Balance
                };
            }

            account.Balance += request.Amount;
            await _accountRepository.UpdateAsync(account);

            AccountBalanceUpdatedEvent balanceUpdatedEvent = new()
            {
                Email = account.Email,
                Amount = request.Amount,
                Date = DateTime.Now
            };
            await _rabbitMQPublisher.Producer(balanceUpdatedEvent, "account-balance-updated");

            return new UpdateBalanceResponse
            {
                IsSuccess = true,
                Message = "Bakiye başarıyla güncellendi.",
                NewBalance = account.Balance
            };
        }
        catch (Exception ex)
        {
            return new UpdateBalanceResponse
            {
                IsSuccess = false,
                Message = $"Bakiye güncellenirken hata oluştu: {ex.Message}",
                NewBalance = 0
            };
        }
    }
}
