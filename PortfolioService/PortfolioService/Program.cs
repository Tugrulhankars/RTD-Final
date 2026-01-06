using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using PortfolioService.Repositories.Abstracts;
using PortfolioService.Repositories.Context;
using PortfolioService.Repositories;
using PortfolioService.Services.Abstracts;
using PortfolioService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

builder.Services.AddGrpc();

builder.Services.AddDbContext<DatabaseContext>(opt =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
        ?? "Server=localhost;Database=RtdPortfolio-Service;Integrated Security=True;TrustServerCertificate=True;Encrypt=false;";
    opt.UseSqlServer(connectionString);
});

builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<IStockCertificateRepository, StockCertificateRepository>();

builder.Services.AddScoped<IPortfolioService, PortfolioService.Services.PortfolioService>();
builder.Services.AddSingleton<IKafkaService, KafkaProducerService>();

builder.Services.AddSingleton(typeof(PortfolioService.Configuration.KafkaConsumerService<>));

builder.Services.AddHttpClient("AccountService", client =>
{
    var accountServiceUrl = builder.Configuration["AccountService:BaseUrl"] ?? "http://localhost:5239";
    client.BaseAddress = new Uri(accountServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHostedService<PortfolioService.Services.UserCreatedEventConsumerService>();

var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:19092";
builder.Services.AddSingleton(new ProducerConfig 
{ 
    BootstrapServers = kafkaBootstrapServers,
    RequestTimeoutMs = 10000,
    MessageTimeoutMs = 30000,
    RetryBackoffMs = 100,
    EnableIdempotence = false
});

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseRouting();
app.UseAuthorization();

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.MapGrpcService<PortfolioService.Services.PortfolioService>();

app.Run();
