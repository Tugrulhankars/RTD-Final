using Application.Features.Strategies.Commands.UpdatePreferences;
using Application.Features.Strategies.Dtos;
using Application.Services;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Application.Features.Strategies.Handlers;
public class UpdateStrategyPreferencesCommandHandler : IRequestHandler<UpdateStrategyPreferencesCommand, UpdateStrategyPreferencesResponse>
{
    private readonly INRulesService _nRulesService;
    private readonly IStrategyRepository _strategyRepository;
    private readonly ILogger<UpdateStrategyPreferencesCommandHandler> _logger;
    public UpdateStrategyPreferencesCommandHandler(
        INRulesService nRulesService,
        IStrategyRepository strategyRepository,
        ILogger<UpdateStrategyPreferencesCommandHandler> logger)
    {
        _nRulesService = nRulesService;
        _strategyRepository = strategyRepository;
        _logger = logger;
    }
    public async Task<UpdateStrategyPreferencesResponse> Handle(UpdateStrategyPreferencesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var strategy = await _strategyRepository.GetAsync(s => s.Id == request.StrategyId, cancellationToken: cancellationToken);
            if (strategy == null)
            {
                return new UpdateStrategyPreferencesResponse
                {
                    Success = false,
                    Message = "Strateji bulunamadı",
                    StrategyId = request.StrategyId
                };
            }
            if (strategy.UserId != request.UserId)
            {
                return new UpdateStrategyPreferencesResponse
                {
                    Success = false,
                    Message = "Bu stratejiye erişim yetkiniz yok",
                    StrategyId = request.StrategyId
                };
            }
            strategy.StopLossPercent = request.StopLossPercentage;
            strategy.ProfitTargetPercent = request.TakeProfitPercentage;
            strategy.EntryThresholdPercentage = request.EntryThresholdPercentage;
            strategy.MaxTotalLoss = request.MaxLossLimitPercentage;
            await _strategyRepository.UpdateAsync(strategy, cancellationToken);
            var userPreference = new UserPreference
            {
                StrategyId = request.StrategyId,
                UserId = request.UserId,
                Ticker = request.Ticker.ToUpper().Trim(),
                StopLossPercentage = request.StopLossPercentage,
                TakeProfitPercentage = request.TakeProfitPercentage,
                EntryThresholdPercentage = request.EntryThresholdPercentage,
                MaxLossLimitPercentage = request.MaxLossLimitPercentage
            };
            await _nRulesService.UpdateStrategyPreferencesAsync(request.StrategyId, userPreference);
            _logger.LogInformation("Strateji tercihleri güncellendi: StrategyId={StrategyId}, UserId={UserId}", 
                request.StrategyId, request.UserId);
            return new UpdateStrategyPreferencesResponse
            {
                Success = true,
                Message = "Strateji tercihleri başarıyla güncellendi",
                StrategyId = request.StrategyId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Strateji tercihleri güncellenirken hata oluştu: StrategyId={StrategyId}", request.StrategyId);
            return new UpdateStrategyPreferencesResponse
            {
                Success = false,
                Message = $"Strateji tercihleri güncellenirken hata oluştu: {ex.Message}",
                StrategyId = request.StrategyId
            };
        }
    }
}
