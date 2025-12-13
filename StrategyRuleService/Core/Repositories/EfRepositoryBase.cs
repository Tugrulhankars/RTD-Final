using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repositories;

public class EfRepositoryBase<TId, TEntity> : IAsyncRepository<TId, TEntity>
    where TEntity : BaseEntity<TId>
{
    private readonly DbContext _dbContext;

    public EfRepositoryBase(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _dbContext.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
         TEntity entity=await _dbContext.FindAsync<TEntity>(predicate);
        if (entity ==null)
        {
            return false;
        }
        return true;
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Remove(entity);
        _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ICollection<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? predicate = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbContext.Set<TEntity>();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (include != null)
        {
            query = include(query);
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _dbContext.Set<TEntity>();

        if (include != null)
        {
            query = include(query);
        }

        return await query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        TEntity entity1= _dbContext.Update(entity).Entity;
       await  _dbContext.SaveChangesAsync(cancellationToken);
        return entity1;
    }
}
