using Application.Services;
using Core.Repositories;
using Domain.Entities;
using Persistance.DatabaseContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Persistance.Repositories;
public class StrategyEventRepository : EfRepositoryBase<int, StrategyEvent>, IStrategyEventRepository
{
    public StrategyEventRepository(Context dbContext) : base(dbContext)
    {
    }
}
