using Application.Services;
using Core.Repositories;
using Domain.Entities;
using Persistance.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Repositories;

public class StrategyRepository : EfRepositoryBase<int, Strategy>, IStrategyRepository
{
    public StrategyRepository(Context dbContext) : base(dbContext)
    {
    }
}
