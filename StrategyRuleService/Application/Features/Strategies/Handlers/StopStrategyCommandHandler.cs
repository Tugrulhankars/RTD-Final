using Application.Features.Strategies.Commands.Stop;
using Application.Services;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
namespace Application.Features.Strategies.Handlers;
public class StopStrategyCommandHandler : IRequestHandler<StopStrategyCommand, StopStrategyResponse>
{
    private readonly IStrategyRepository _strategyRepository;
    private readonly IStrategyEventRepository _strategyEventRepository;
    private readonly INRulesService _nRulesService;
    private readonly ILogger<StopStrategyCommandHandler> _logger;
    public StopStrategyCommandHandler(
        IStrategyRepository strategyRepository,
        IStrategyEventRepository strategyEventRepository,
        INRulesService nRulesService,
        ILogger<StopStrategyCommandHandler> logger)
    {
        _strategyRepository = strategyRepository;
        _strategyEventRepository = strategyEventRepository;
        _nRulesService = nRulesService;
        _logger = logger;
    }
    public async Task<StopStrategyResponse> Handle(StopStrategyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var strategy = await _strategyRepository.GetAsync(
                s => s.Id == request.StrategyId && s.UserId == request.UserId,
                cancellationToken: cancellationToken);
            if (strategy == null)
            {
                _logger.LogWarning("Strateji bulunamadı: StrategyId={StrategyId}, UserId={UserId}", 
                    request.StrategyId, request.UserId);
                return new StopStrategyResponse
                {
                    Success = false,
                    Message = "Strateji bulunamadı veya bu kullanıcıya ait değil.",
                    StrategyId = request.StrategyId
                };
            }
            if (strategy.Status == StrategyStatus.Inactive || strategy.Status == StrategyStatus.Completed)
            {
                _logger.LogInformation("Strateji zaten durdurulmuş: StrategyId={StrategyId}, Status={Status}", 
                    request.StrategyId, strategy.Status);
                return new StopStrategyResponse
                {
                    Success = false,
                    Message = $"Strateji zaten {strategy.Status} durumunda.",
                    StrategyId = request.StrategyId
                };
            }
            strategy.Status = StrategyStatus.Inactive;
            strategy.FinishTime = DateTime.Now;
            await _strategyRepository.UpdateAsync(strategy, cancellationToken);
            var strategyKey = $"Strategy_{strategy.Id}";
            await _nRulesService.RemoveStrategyAsync(strategyKey);
            var stopEvent = new Domain.Entities.StrategyEvent
            {
                StrategyId = strategy.Id,
                Step = -1,
                RuleName = "StopStrategy",
                Action = "STOPPED",
                Reason = request.Reason ?? "Kullanıcı tarafından durduruldu",
                Price = 0,
                Timestamp = DateTime.Now
            };
            await _strategyEventRepository.AddAsync(stopEvent, cancellationToken);
            _logger.LogInformation("Strateji başarıyla durduruldu: StrategyId={StrategyId}, StrategyName={StrategyName}", 
                strategy.Id, strategy.StrategyName);
            return new StopStrategyResponse
            {
                Success = true,
                Message = "Strateji başarıyla durduruldu.",
                StrategyId = strategy.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Strateji durdurulurken hata oluştu: StrategyId={StrategyId}", 
                request.StrategyId);
            throw;
        }
    }
}
