using MediatR;

namespace Application.Features.Strategies.Commands.Stop;

public class StopStrategyCommand : IRequest<StopStrategyResponse>
{
    public int StrategyId { get; set; }
    public int UserId { get; set; }
    public string? Reason { get; set; }
}

