using MediatR;
namespace Application.Features.Strategies.Queries.GetStrategySteps;
public class GetStrategyStepsQuery : IRequest<GetStrategyStepsResponse>
{
    public int StrategyId { get; set; }
    public int UserId { get; set; }
    public GetStrategyStepsQuery(int strategyId, int userId)
    {
        StrategyId = strategyId;
        UserId = userId;
    }
}
