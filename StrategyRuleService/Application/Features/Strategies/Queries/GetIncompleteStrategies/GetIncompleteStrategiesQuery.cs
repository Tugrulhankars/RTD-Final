using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Strategies.Queries.GetIncompleteStrategies;

public class GetIncompleteStrategiesQuery : IRequest<GetIncompleteStrategiesResponse>
{
    public int UserId { get; set; }

    public GetIncompleteStrategiesQuery(int userId)
    {
        UserId = userId;
    }
}

