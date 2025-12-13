using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using PortfolioService.Repositories.Abstracts;
using PortfolioService.Repositories.Context;
using System.Linq.Expressions;

namespace PortfolioService.Repositories;

public class EfRepositoryBase<TEntity> : IAsyncRepository<TEntity>
    where TEntity : class
{
    private readonly DatabaseContext _context;
    public EfRepositoryBase(DatabaseContext context)
    {
        _context = context;
    }
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _context.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _context.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ICollection<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _context.Set<TEntity>();

        if (predicate != null)
            query = query.Where(predicate);

        if (include != null)
            query = include(query);

        if (orderBy != null)
            query = orderBy(query);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<TEntity> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> query = _context.Set<TEntity>();

        if (include != null)
            query = include(query);

        return await query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {

        TEntity entity1 = _context.Update(entity).Entity;
        await _context.SaveChangesAsync(cancellationToken);
        return entity1;
    }
}
