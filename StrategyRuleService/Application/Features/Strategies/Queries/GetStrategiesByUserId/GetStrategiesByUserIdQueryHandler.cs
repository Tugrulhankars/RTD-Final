using Application.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Strategies.Queries.GetStrategiesByUserId;

public class GetStrategiesByUserIdQueryHandler : IRequestHandler<GetStrategiesByUserIdQuery, GetStrategiesByUserIdResponse>
{
    private readonly IStrategyRepository _strategyRepository;
    private readonly IStrategyEventRepository _strategyEventRepository;

    public GetStrategiesByUserIdQueryHandler(
        IStrategyRepository strategyRepository,
        IStrategyEventRepository strategyEventRepository)
    {
        _strategyRepository = strategyRepository;
        _strategyEventRepository = strategyEventRepository;
    }

    public async Task<GetStrategiesByUserIdResponse> Handle(GetStrategiesByUserIdQuery request, CancellationToken cancellationToken)
    {
        // Kullanıcının tüm stratejilerini getir
        var strategies = await _strategyRepository.GetAllAsync(
            predicate: s => s.UserId == request.UserId,
            orderBy: q => q.OrderByDescending(s => s.StartDate),
            cancellationToken: cancellationToken);

        var response = new GetStrategiesByUserIdResponse();

        foreach (var strategy in strategies)
        {
            // Her strateji için event'leri getir
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

            response.Strategies.Add(strategyDto);
        }

        return response;
    }
}

