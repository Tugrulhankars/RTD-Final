using MediatR;
namespace Application.Features.Strategies.Queries.GetStrategyDetail;
public class GetStrategyDetailQuery : IRequest<GetStrategyDetailResponse>
{
    public int StrategyId { get; set; }
    public int UserId { get; set; }
    public GetStrategyDetailQuery(int strategyId, int userId)
    {
        StrategyId = strategyId;
        UserId = userId;
    }
}
