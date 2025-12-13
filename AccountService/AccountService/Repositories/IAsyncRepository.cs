using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace AccountService.Repositories;

public interface IAsyncRepository<TEntity> where TEntity : class
{
    Task<TEntity> GetAsync(
       Expression<Func<TEntity, bool>> predicate,
       Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
       CancellationToken cancellationToken = default);

    Task<ICollection<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
