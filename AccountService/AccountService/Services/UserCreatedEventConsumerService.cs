using AccountService.Configuration;
using AccountService.Events;
using AccountService.Repositories;
using AccountService.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AccountService.Services;

public class UserCreatedEventConsumerService : BackgroundService
{
    private readonly ILogger<UserCreatedEventConsumerService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public UserCreatedEventConsumerService(
        ILogger<UserCreatedEventConsumerService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UserCreatedEventConsumerService başlatıldı");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var kafkaConsumer = scope.ServiceProvider.GetRequiredService<KafkaConsumerService<UserCreatedEvent>>();
                var accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

                _logger.LogInformation("Kafka'dan UserCreatedEvent bekleniyor...");

                // Kafka'dan event al
                // Not: Consume metodu blocking bir while(true) döngüsü içinde
                // Production'da bu yapıyı async/await pattern'e çevirmek gerekir
                var userCreatedEvent = await kafkaConsumer.Consume("user-created");

                if (userCreatedEvent != null)
                {
                    _logger.LogInformation("UserCreatedEvent alındı: UserId={UserId}, Email={Email}",
                        userCreatedEvent.UserId, userCreatedEvent.Email);

                    // Kullanıcının zaten hesabı var mı kontrol et
                    var existingAccount = await accountRepository.GetAsync(a => a.UserId == userCreatedEvent.UserId);
                    if (existingAccount != null)
                    {
                        _logger.LogWarning("Kullanıcının zaten bir hesabı var: UserId={UserId}", userCreatedEvent.UserId);
                        continue;
                    }

                    // Yeni hesap oluştur
                    Account account = new()
                    {
                        Balance = 0,
                        UserId = userCreatedEvent.UserId,
                        FirstName = userCreatedEvent.FirstName,
                        LastName = userCreatedEvent.LastName,
                        Email = userCreatedEvent.Email,
                        AccountStatus = AccountStatus.ACTIVE
                    };

                    await accountRepository.AddAsync(account);
                    _logger.LogInformation("Hesap başarıyla oluşturuldu: AccountId={AccountId}, UserId={UserId}",
                        account.Id, account.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserCreatedEvent işlenirken hata oluştu");
                // Hata durumunda kısa bir bekleme yap ve tekrar dene
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("UserCreatedEventConsumerService durduruldu");
    }
}

