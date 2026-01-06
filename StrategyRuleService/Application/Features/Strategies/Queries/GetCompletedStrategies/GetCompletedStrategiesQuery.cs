using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Application.Features.Strategies.Queries.GetCompletedStrategies;
public class GetCompletedStrategiesQuery : IRequest<GetCompletedStrategiesResponse>
{
    public int UserId { get; set; }
    public GetCompletedStrategiesQuery(int userId)
    {
        UserId = userId;
    }
}
