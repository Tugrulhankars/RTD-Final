using AccountService.Configuration;
using AccountService.Repositories;
using AccountService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddGrpc();
builder.Services.AddScoped<IAccountService, AccountService.Services.AccountService>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IRabbitMQPublisher,RabbitMqPublisher>();
builder.Services.AddScoped(typeof(KafkaConsumerService<>));

builder.Services.AddHostedService<UserCreatedEventConsumerService>();
builder.Services.AddDbContext<DatabaseContext>(op =>
{
    op.UseSqlServer("server=MetropolTilkisi;database=RTD-AccountService;integrated security=SSPI;persist security info=False;Trusted_Connection=True;Encrypt=false");
});
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

var grpcPort = builder.Configuration["Grpc:Port"] ?? "5001";
app.MapGrpcService<AccountService.Services.AccountService>();

app.Run();

