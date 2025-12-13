using Core.Entities;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repositories;

public interface IAsyncRepository<TId,TEntity>
    where TEntity: BaseEntity<TId>
{
    Task<TEntity> GetAsync(
        Expression<Func<TEntity,bool>> predicate,
        Func<IQueryable<TEntity>,IIncludableQueryable<TEntity,object>>? include=null,
        CancellationToken cancellationToken=default);

    Task<ICollection<TEntity>> GetAllAsync(
        Expression<Func<TEntity,bool>>? predicate=null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
