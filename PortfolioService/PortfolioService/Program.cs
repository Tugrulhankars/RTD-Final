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
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<IStockCertificateRepository, StockCertificateRepository>();

builder.Services.AddScoped<IPortfolioService, PortfolioService.Services.PortfolioService>();
builder.Services.AddSingleton<IKafkaService, KafkaProducerService>();

builder.Services.AddSingleton(new ProducerConfig { BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();
app.MapControllers();

app.MapGrpcService<PortfolioService.Services.PortfolioService>();

app.Run();
