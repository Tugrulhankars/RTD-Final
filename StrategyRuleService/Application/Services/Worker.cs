using Application.Services;
using Application.Features.Strategies.Rules;
using Application.Features.Strategies.Dtos;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace StrategyRuleService.Worker
{
    public class Worker :BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly INRulesService _nRulesService;
        private readonly List<Strategy> _activeStrategies;
        private readonly SemaphoreSlim _semaphore;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, INRulesService nRulesService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _nRulesService = nRulesService;
            _activeStrategies = new List<Strategy>();
            _semaphore = new SemaphoreSlim(1, 1);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NRules Worker başlatıldı - Kurallar sürekli çalışacak");

            // Test amaçlı bir strateji ekle - resimdeki stratejiye uygun
            var strategy = new StockWorkflow
            {
                Symbol = "THYAO", // THY hissesi
                OpeningPrice = 100,
                CurrentPrice = 95, // Açılışın altında
                InPortfolio = false,
                TotalLossPercent = -5, // %5 zarar
                Now = DateTime.Now,
                Step = 0 // Başlangıç adımı
            };
            await _nRulesService.AddStrategyAsync("THYAO_Strategy", strategy);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _semaphore.WaitAsync(stoppingToken);
                    await _nRulesService.ProcessRulesAsync();
                    _logger.LogDebug("Kurallar işlendi - {Time}", DateTime.Now);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker döngüsünde hata oluştu");
                }
                finally
                {
                    _semaphore.Release();
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // 1 dakikalık takip periyodu
            }

            _logger.LogInformation("NRules Worker durduruldu");
        }

        private async Task InitializeDefaultStrategiesAsync()
        {
            //try
            //{
            //    // Örnek strateji context'leri oluştur
            //    var strategy1 = new StockWorkflow
            //    {
            //        Symbol = "AAPL",
            //        OpeningPrice = 100,
            //        CurrentPrice = 95,
            //        InPortfolio = false,
            //        TotalLossPercent = -5, // %5 zarar
            //        Step = 0, // Başlangıç adımı
            //        Now = DateTime.Now
            //    };

            //    var strategy2 = new StockWorkflow
            //    {
            //        Symbol = "MSFT",
            //        OpeningPrice = 50,
            //        CurrentPrice = 52,
            //        InPortfolio = true,
            //        TotalLossPercent = 4, // %4 kar
            //        Step = 0, // Başlangıç adımı
            //        Now = DateTime.Now
            //    };

            //    await _nRulesService.AddStrategyAsync("Strategy1", strategy1);
            //    await _nRulesService.AddStrategyAsync("Strategy2", strategy2);

            //    _logger.LogInformation("Varsayılan stratejiler yüklendi - Strategy1: {S1}, Strategy2: {S2}", 
            //        $"Fiyat={strategy1.CurrentPrice}, Zarar={strategy1.TotalLossPercent}%", 
            //        $"Fiyat={strategy2.CurrentPrice}, Kar={strategy2.TotalLossPercent}%");
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Varsayılan stratejiler yüklenirken hata oluştu");
            //}
        }



      


        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Strateji Worker durduruluyor...");

            await _semaphore.WaitAsync();
            try
            {
                _activeStrategies.Clear();
            }
            finally
            {
                _semaphore.Release();
            }

            _semaphore.Dispose();
            await base.StopAsync(cancellationToken);
        }
    }
}
