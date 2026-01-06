using Application.Features.Strategies.Queries.GetStrategiesByUserId;
using Application.Services;
using Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Application.Features.Strategies.Queries.GetIncompleteStrategies;
public class GetIncompleteStrategiesQueryHandler : IRequestHandler<GetIncompleteStrategiesQuery, GetIncompleteStrategiesResponse>
{
    private readonly IStrategyRepository _strategyRepository;
    private readonly IStrategyEventRepository _strategyEventRepository;
    public GetIncompleteStrategiesQueryHandler(
        IStrategyRepository strategyRepository,
        IStrategyEventRepository strategyEventRepository)
    {
        _strategyRepository = strategyRepository;
        _strategyEventRepository = strategyEventRepository;
    }
    public async Task<GetIncompleteStrategiesResponse> Handle(GetIncompleteStrategiesQuery request, CancellationToken cancellationToken)
    {
        var strategies = await _strategyRepository.GetAllAsync(
            predicate: s => s.UserId == request.UserId &&
                          s.Status != StrategyStatus.Completed &&
                          s.FinishTime == null,
            orderBy: q => q.OrderByDescending(s => s.StartDate),
            cancellationToken: cancellationToken);
        var response = new GetIncompleteStrategiesResponse();
        var now = DateTime.Now;
        foreach (var strategy in strategies)
        {
            if (strategy.ExpiryDate.HasValue && 
                strategy.ExpiryDate.Value < now && 
                strategy.Status == StrategyStatus.Active)
            {
                strategy.Status = StrategyStatus.Inactive;
                strategy.IsActive = false;
                await _strategyRepository.UpdateAsync(strategy, cancellationToken);
            }
            var events = await _strategyEventRepository.GetAllAsync(
                predicate: e => e.StrategyId == strategy.Id,
                orderBy: q => q.OrderBy(e => e.Timestamp),
                cancellationToken: cancellationToken);
            var strategyDto = new StrategyDto
            {
                Id = strategy.Id,
                UserId = strategy.UserId,
                StrategyName = strategy.StrategyName,
                Description = strategy.Description,
                StockSymbol = strategy.StockSymbol,
                Status = strategy.Status.ToString(),
                StartDate = strategy.StartDate,
                FinishTime = strategy.FinishTime,
                BuyPrice = strategy.BuyPrice,
                SellPrice = strategy.SellPrice,
                ProfitLoss = strategy.ProfitLoss,
                IsPositionOpen = strategy.IsPositionOpen,
                TotalProfit = strategy.TotalProfit,
                TotalLoss = strategy.TotalLoss,
                TotalTransactions = strategy.TotalTransactions,
                SuccessfulTransactions = strategy.SuccessfulTransactions,
                DurationHours = strategy.DurationHours,
                ExpiryDate = strategy.ExpiryDate,
                Events = events.Select(e => new StrategyEventDto
                {
                    Id = e.Id,
                    StrategyId = e.StrategyId,
                    Step = e.Step,
                    RuleName = e.RuleName,
                    Action = e.Action,
                    Reason = e.Reason,
                    Price = e.Price,
                    Timestamp = e.Timestamp
                }).ToList()
            };
            response.IncompleteStrategies.Add(strategyDto);
        }
        return response;
    }
}
