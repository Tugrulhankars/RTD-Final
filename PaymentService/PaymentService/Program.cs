using PaymentService.Repositories;
using PaymentService.Services;
using PaymentService.Services.Impl;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<Iyzipay.Options>(options =>
{
    options.ApiKey = builder.Configuration["Iyzico:ApiKey"] ?? "sandbox-jJ9iwVPKmLVPhHy9quhLMsdqvDLQY0J9";
    options.SecretKey = builder.Configuration["Iyzico:SecretKey"] ?? "sandbox-q4dk0SrgBiNf9mr2zCCU5PuHQwMYGxKv";
    options.BaseUrl = builder.Configuration["Iyzico:BaseUrl"] ?? "https://sandbox-api.iyzipay.com";
});

builder.Services.AddDbContext<DatabaseContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=localhost;Database=PaymentServiceDb;Trusted_Connection=True;TrustServerCertificate=True;";
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

builder.Services.AddScoped<IIyzicoPaymentService, IyzicoPaymentServiceImpl>();
builder.Services.AddScoped<IRabbitMQPublisher, RabbitMQPublisher>();

builder.Services.AddHttpClient("AccountService", client =>
{
    var accountServiceUrl = builder.Configuration["AccountService:BaseUrl"] ?? "https://localhost:5001";
    client.BaseAddress = new Uri(accountServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IAccountService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<AccountServiceClient>>();
    var accountServiceGrpcAddress = builder.Configuration["AccountService:GrpcAddress"] ?? "https://localhost:5001";
    return new AccountServiceClient(accountServiceGrpcAddress, logger);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
