using Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Application.Features.Strategies.Queries.GetStrategySteps;
public class GetStrategyStepsQueryHandler : IRequestHandler<GetStrategyStepsQuery, GetStrategyStepsResponse>
{
    private readonly IStrategyRepository _strategyRepository;
    private readonly IStrategyEventRepository _strategyEventRepository;
    private readonly ILogger<GetStrategyStepsQueryHandler> _logger;
    public GetStrategyStepsQueryHandler(
        IStrategyRepository strategyRepository,
        IStrategyEventRepository strategyEventRepository,
        ILogger<GetStrategyStepsQueryHandler> logger)
    {
        _strategyRepository = strategyRepository;
        _strategyEventRepository = strategyEventRepository;
        _logger = logger;
    }
    public async Task<GetStrategyStepsResponse> Handle(GetStrategyStepsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Strateji adımları isteniyor: StrategyId={StrategyId}, UserId={UserId}", 
                request.StrategyId, request.UserId);
            var strategy = await _strategyRepository.GetAsync(
                s => s.Id == request.StrategyId && s.UserId == request.UserId,
                cancellationToken: cancellationToken);
            if (strategy == null)
            {
                _logger.LogWarning("Strateji bulunamadı: StrategyId={StrategyId}, UserId={UserId}", 
                    request.StrategyId, request.UserId);
                throw new KeyNotFoundException($"Strateji bulunamadı (StrategyId: {request.StrategyId}, UserId: {request.UserId}). Strateji mevcut değil veya bu kullanıcıya ait değil.");
            }
            var events = await _strategyEventRepository.GetAllAsync(
                predicate: e => e.StrategyId == request.StrategyId,
                orderBy: q => q.OrderBy(e => e.Timestamp),
                cancellationToken: cancellationToken) ?? new List<Domain.Entities.StrategyEvent>();
            var response = new GetStrategyStepsResponse();
            response.Steps = events.Select(e => new StrategyStepDto
            {
                Id = e.Id,
                StrategyId = e.StrategyId,
                Step = e.Step,
                StepName = e.RuleName ?? string.Empty,
                Action = e.Action ?? string.Empty,
                Reason = e.Reason ?? string.Empty,
                Price = e.Price,
                Timestamp = e.Timestamp
            }).ToList();
            _logger.LogInformation("Strateji adımları başarıyla alındı: StrategyId={StrategyId}, StepCount={StepCount}", 
                request.StrategyId, response.Steps.Count);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Strateji adımları alınırken hata oluştu: StrategyId={StrategyId}, UserId={UserId}", 
                request.StrategyId, request.UserId);
            throw;
        }
    }
}
