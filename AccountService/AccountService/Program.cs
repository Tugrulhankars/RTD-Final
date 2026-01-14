using AccountService.Configuration;
using AccountService.Repositories;
using AccountService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddGrpc();
builder.Services.AddScoped<IAccountService, AccountService.Services.AccountService>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IRabbitMQPublisher,RabbitMqPublisher>();

builder.Services.AddHttpClient("PortfolioService", client =>
{
    var portfolioServiceUrl = builder.Configuration["PortfolioService:BaseUrl"] ?? "http://localhost:5242";
    client.BaseAddress = new Uri(portfolioServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton(typeof(KafkaConsumerService<>));
builder.Services.AddSingleton<KafkaTopicInitializer>();

builder.Services.AddHostedService<UserCreatedEventConsumerService>();
builder.Services.AddHostedService<PaymentSuccessEventConsumerService>();

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
builder.Services.AddDbContext<DatabaseContext>(op =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=MetropolTilkisi;Database=RtdAccount-Service;Integrated Security=SSPI;Persist Security Info=False;Trusted_Connection=True;Encrypt=false;TrustServerCertificate=True;";
    
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Database connection string is null or empty. Please check appsettings.json ConnectionStrings:DefaultConnection");
    }
    
    op.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MaxBatchSize(100);
        sqlOptions.CommandTimeout(60);
        
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        );
    });
});
var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Account-Service başlatılıyor...");

_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(2000);
        using var scope = app.Services.CreateScope();
        var topicInitializer = scope.ServiceProvider.GetRequiredService<KafkaTopicInitializer>();
        await topicInitializer.InitializeTopicsAsync();
        logger.LogInformation("Kafka topic initialization tamamlandı.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Kafka topic initialization başarısız (non-critical): {Error}", ex.Message);
    }
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.MapControllers();

var grpcPort = builder.Configuration["Grpc:Port"] ?? "5001";
app.MapGrpcService<AccountService.Services.AccountService>();

logger.LogInformation("Account-Service başlatıldı, dinleniyor...");
app.Run();
