using Application;
using Application.Services;
using Infrastructure;
using Persistance;
using StrategyRuleService.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddPersistanceServices();

builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
