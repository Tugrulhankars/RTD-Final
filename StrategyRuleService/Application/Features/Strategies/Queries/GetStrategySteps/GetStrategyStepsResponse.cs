using System;
using System.Collections.Generic;
namespace Application.Features.Strategies.Queries.GetStrategySteps;
public class GetStrategyStepsResponse
{
    public List<StrategyStepDto> Steps { get; set; } = new List<StrategyStepDto>();
}
public class StrategyStepDto
{
    public int Id { get; set; }
    public int StrategyId { get; set; }
    public int Step { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
}
