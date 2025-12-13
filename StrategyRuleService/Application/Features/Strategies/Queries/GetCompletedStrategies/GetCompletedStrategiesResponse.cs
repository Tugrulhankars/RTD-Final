using Application.Features.Strategies.Queries.GetStrategiesByUserId;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Strategies.Queries.GetCompletedStrategies;

public class GetCompletedStrategiesResponse
{
    public List<StrategyDto> CompletedStrategies { get; set; } = new List<StrategyDto>();
}

