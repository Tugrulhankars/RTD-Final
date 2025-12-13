using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace PortfolioService.Repositories.Abstracts;

public interface IAsyncRepository<TEntity>
{
    Task<TEntity> GetAsync(Expression<Func<TEntity,bool>> predicate,
        Func<IQueryable<TEntity>,IIncludableQueryable<TEntity,object>>? include=null,
        CancellationToken cancellationToken=default);
    Task<ICollection<TEntity>> GetAllAsync(
       Expression<Func<TEntity, bool>>? predicate = null,
       Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
       Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
       CancellationToken cancellationToken = default);

    Task<TEntity> UpdateAsync(TEntity entity,CancellationToken cancellationToken= default);
    Task DeleteAsync(TEntity entity,CancellationToken cancellationToken= default);
    Task AddAsync(TEntity entity,CancellationToken cancellationToken= default);
}
