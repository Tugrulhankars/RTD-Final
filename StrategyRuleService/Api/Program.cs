using Application;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistance;
using Persistance.DatabaseContext;
using Api.Middleware;
using Domain.Entities;
using Domain.Enums;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddPersistanceServices(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddHttpContextAccessor();
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
app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<Context>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Veritabanı migration'ları uygulanıyor...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Veritabanı migration'ları başarıyla uygulandı.");
        var testUserId = 26;
        var existingStrategy = await context.Strategies
            .FirstOrDefaultAsync(s => s.UserId == testUserId && s.StrategyName == "Test Strategy - NRules Integration");
        if (existingStrategy == null)
        {
            var defaultStrategy = new Strategy
            {
                UserId = testUserId,
                StrategyName = "Test Strategy - NRules Integration",
                Description = "NRules entegrasyonu için test stratejisi - 5% Stop-Loss ve 10% Take-Profit",
                StockSymbol = "THYAD",
                TransactionAmount = 10000m,
                TransactionPercentage = 100m,
                BuyThresholdPercent = -5.0m,
                ProfitTargetPercent = 5.0m,
                StopLossPercent = 2.0m,
                StopLossPercentage = 5.0m,
                TakeProfitPercentage = 10.0m,
                EntryThresholdPercentage = -5.0m,
                IsActive = true,
                Status = StrategyStatus.Active,
                MaxTotalLoss = 5.0m,
                TotalProfit = 0m,
                TotalLoss = 0m,
                TotalTransactions = 0,
                SuccessfulTransactions = 0,
                RuleCount = 0
            };
            context.Strategies.Add(defaultStrategy);
            await context.SaveChangesAsync();
            logger.LogInformation("Test kullanıcısı (UserId: {UserId}) için default strategy seed data eklendi.", testUserId);
        }
        else
        {
            logger.LogInformation("Test kullanıcısı (UserId: {UserId}) için strategy zaten mevcut.", testUserId);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı migration'ları veya seed data uygulanırken bir hata oluştu.");
        throw;
    }
}
app.Run();
