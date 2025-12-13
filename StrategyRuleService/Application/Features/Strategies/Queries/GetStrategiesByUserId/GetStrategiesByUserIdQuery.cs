using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Strategies.Queries.GetStrategiesByUserId;

public class GetStrategiesByUserIdQuery : IRequest<GetStrategiesByUserIdResponse>
{
    public int UserId { get; set; }

    public GetStrategiesByUserIdQuery(int userId)
    {
        UserId = userId;
    }
}

