using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Middleware;

/// <summary>
/// Defines the contract for repository middleware that can intercept and process repository operations.
/// Middleware components can be chained together to form a processing pipeline.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public interface IRepositoryMiddleware<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// Intercepts and processes the Get operation with filtering and ordering.
    /// </summary>
    Task<IEnumerable<TEntity>> Get(
        Func<Task<IEnumerable<TEntity>>> next,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Intercepts and processes the Get operation with type-safe includes.
    /// </summary>
    Task<IEnumerable<TEntity>> Get(
        Func<Task<IEnumerable<TEntity>>> next,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeExpressions);

    /// <summary>
    /// Intercepts and processes the Get operation by primary key.
    /// </summary>
    Task<TEntity?> GetById(
        Func<Task<TEntity?>> next,
        TKey id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Intercepts and processes the Get operation with a query object.
    /// </summary>
    Task<IEnumerable<TEntity>> GetWithQuery(
        Func<Task<IEnumerable<TEntity>>> next,
        Query<TEntity> query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Intercepts and processes the Insert operation.
    /// </summary>
    Task<TEntity> Insert(
        Func<Task<TEntity>> next,
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Intercepts and processes the Update operation.
    /// </summary>
    Task<TEntity> Update(
        Func<Task<TEntity>> next,
        TEntity entityToUpdate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Intercepts and processes the Delete operation for an entity.
    /// </summary>
    Task<bool> Delete(
        Func<Task<bool>> next,
        TEntity entityToDelete,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Intercepts and processes the Delete operation by primary key.
    /// </summary>
    Task<bool> DeleteById(
        Func<Task<bool>> next,
        TKey id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Intercepts and processes the InsertRange operation.
    /// </summary>
    Task<IEnumerable<TEntity>> InsertRange(
        Func<Task<IEnumerable<TEntity>>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Intercepts and processes the UpdateRange operation.
    /// </summary>
    Task<IEnumerable<TEntity>> UpdateRange(
        Func<Task<IEnumerable<TEntity>>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Intercepts and processes the DeleteRange operation for entities.
    /// </summary>
    Task<int> DeleteRange(
        Func<Task<int>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Intercepts and processes the DeleteRange operation with filter.
    /// </summary>
    Task<int> DeleteRangeWithFilter(
        Func<Task<int>> next,
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default);
}
