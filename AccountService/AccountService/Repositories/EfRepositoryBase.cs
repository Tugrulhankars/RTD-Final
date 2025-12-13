using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace AccountService.Repositories;

public class EfRepositoryBase<TEntity>:IAsyncRepository<TEntity>
    where TEntity : class
{
    private readonly DatabaseContext _dbContext;
    public EfRepositoryBase(DatabaseContext dbContext)
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
        if (predicate == null)
        {
            return await _dbContext.Set<TEntity>().AnyAsync(cancellationToken);
        }
        return await _dbContext.Set<TEntity>().AnyAsync(predicate, cancellationToken);
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _dbContext.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ICollection<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? predicate = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, CancellationToken cancellationToken = default)
    {
        ICollection<TEntity> entities = await _dbContext.Set<TEntity>().ToListAsync(cancellationToken);
        return entities;
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
        TEntity entity1 = _dbContext.Update(entity).Entity;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity1;
    }
}
