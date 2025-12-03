using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Middleware;

/// <summary>
/// Base implementation of repository middleware that provides pass-through behavior.
/// Derived classes can override specific methods to add custom behavior.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public abstract class RepositoryMiddlewareBase<TEntity, TKey> : IRepositoryMiddleware<TEntity, TKey>
    where TEntity : class
{
    /// <summary>
    /// Intercepts and processes the Get operation with filtering and ordering.
    /// Default implementation calls next delegate without modification.
    /// </summary>
    public virtual Task<IEnumerable<TEntity>> Get(
        Func<Task<IEnumerable<TEntity>>> next,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default)
    {
        return next();
    }

    /// <summary>
    /// Intercepts and processes the Get operation with type-safe includes.
    /// Default implementation calls next delegate without modification.
    /// </summary>
    public virtual Task<IEnumerable<TEntity>> Get(
        Func<Task<IEnumerable<TEntity>>> next,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeExpressions)
    {
        return next();
    }

    /// <summary>
    /// Intercepts and processes the Get operation by primary key.
    /// Default implementation calls next delegate without modification.
    /// </summary>
    public virtual Task<TEntity?> GetById(
        Func<Task<TEntity?>> next,
        TKey id,
        CancellationToken cancellationToken = default)
    {
        return next();
    }

    /// <summary>
    /// Intercepts and processes the Get operation with a query object.
    /// Default implementation calls next delegate without modification.
    /// </summary>
    public virtual Task<IEnumerable<TEntity>> GetWithQuery(
        Func<Task<IEnumerable<TEntity>>> next,
        Query<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        return next();
    }

    /// <summary>
    /// Intercepts and processes the Insert operation.
    /// Default implementation calls next delegate without modification.
    /// </summary>
    public virtual Task<TEntity> Insert(
        Func<Task<TEntity>> next,
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return next();
    }

    /// <summary>
    /// Intercepts and processes the Update operation.
    /// Default implementation calls next delegate without modification.
    /// </summary>
    public virtual Task<TEntity> Update(
        Func<Task<TEntity>> next,
        TEntity entityToUpdate,
        CancellationToken cancellationToken = default)
    {
        return next();
    }

    /// <summary>
    /// Intercepts and processes the Delete operation for an entity.
    /// Default implementation calls next delegate without modification.
    /// </summary>
    public virtual Task<bool> Delete(
        Func<Task<bool>> next,
        TEntity entityToDelete,
        CancellationToken cancellationToken = default)
    {
        return next();
    }

    /// <summary>
    /// Intercepts and processes the Delete operation by primary key.
    /// Default implementation calls next delegate without modification.
    /// </summary>
    public virtual Task<bool> DeleteById(
        Func<Task<bool>> next,
        TKey id,
        CancellationToken cancellationToken = default)
    {
        return next();
    }

    /// <summary>
    /// Intercepts and processes the InsertRange operation.
    /// Default implementation calls next delegate without modification.
    /// </summary>
    public virtual Task<IEnumerable<TEntity>> InsertRange(
        Func<Task<IEnumerable<TEntity>>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        return next();
    }

    /// <summary>
    /// Intercepts and processes the UpdateRange operation.
    /// Default implementation calls next delegate without modification.
    /// </summary>
    public virtual Task<IEnumerable<TEntity>> UpdateRange(
        Func<Task<IEnumerable<TEntity>>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        return next();
    }

    /// <summary>
    /// Intercepts and processes the DeleteRange operation for entities.
    /// Default implementation calls next delegate without modification.
    /// </summary>
    public virtual Task<int> DeleteRange(
        Func<Task<int>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        return next();
    }

    /// <summary>
    /// Intercepts and processes the DeleteRange operation with filter.
    /// Default implementation calls next delegate without modification.
    /// </summary>
    public virtual Task<int> DeleteRangeWithFilter(
        Func<Task<int>> next,
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        return next();
    }
}
