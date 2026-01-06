using Application.Features.Strategies.Queries.GetStrategiesByUserId;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Application.Features.Strategies.Queries.GetIncompleteStrategies;
public class GetIncompleteStrategiesResponse
{
    public List<StrategyDto> IncompleteStrategies { get; set; } = new List<StrategyDto>();
}
