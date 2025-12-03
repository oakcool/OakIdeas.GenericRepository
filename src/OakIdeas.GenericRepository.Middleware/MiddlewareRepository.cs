using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Middleware;

/// <summary>
/// Repository implementation that wraps another repository and applies a middleware pipeline.
/// This enables cross-cutting concerns like logging, caching, validation, etc.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public class MiddlewareRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    private readonly IGenericRepository<TEntity, TKey> _innerRepository;
    private readonly IReadOnlyList<IRepositoryMiddleware<TEntity, TKey>> _middlewares;

    /// <summary>
    /// Initializes a new instance of the MiddlewareRepository class.
    /// </summary>
    /// <param name="innerRepository">The repository to wrap</param>
    /// <param name="middlewares">The middleware components to apply in order</param>
    public MiddlewareRepository(
        IGenericRepository<TEntity, TKey> innerRepository,
        params IRepositoryMiddleware<TEntity, TKey>[] middlewares)
    {
        _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        _middlewares = middlewares ?? Array.Empty<IRepositoryMiddleware<TEntity, TKey>>();
    }

    /// <summary>
    /// Gets entities with optional filtering, ordering, and eager loading.
    /// </summary>
    public Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default)
    {
        Func<Task<IEnumerable<TEntity>>> operation = () =>
            _innerRepository.Get(filter, orderBy, includeProperties, cancellationToken);

        // Apply middleware in reverse order so first middleware wraps the operation
        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var currentOperation = operation;
            operation = () => middleware.Get(
                currentOperation,
                filter,
                orderBy,
                includeProperties,
                cancellationToken);
        }

        return operation();
    }

    /// <summary>
    /// Gets entities with optional filtering, ordering, and type-safe eager loading.
    /// </summary>
    public Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeExpressions)
    {
        Func<Task<IEnumerable<TEntity>>> operation = () =>
            _innerRepository.Get(filter, orderBy, cancellationToken, includeExpressions);

        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var currentOperation = operation;
            operation = () => middleware.Get(
                currentOperation,
                filter,
                orderBy,
                cancellationToken,
                includeExpressions);
        }

        return operation();
    }

    /// <summary>
    /// Gets an entity by its primary key.
    /// </summary>
    public Task<TEntity?> Get(TKey id, CancellationToken cancellationToken = default)
    {
        Func<Task<TEntity?>> operation = () => _innerRepository.Get(id, cancellationToken);

        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var currentOperation = operation;
            operation = () => middleware.GetById(currentOperation, id, cancellationToken);
        }

        return operation();
    }

    /// <summary>
    /// Gets entities using a query object.
    /// </summary>
    public Task<IEnumerable<TEntity>> Get(Query<TEntity> query, CancellationToken cancellationToken = default)
    {
        Func<Task<IEnumerable<TEntity>>> operation = () =>
            _innerRepository.Get(query, cancellationToken);

        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var currentOperation = operation;
            operation = () => middleware.GetWithQuery(currentOperation, query, cancellationToken);
        }

        return operation();
    }

    /// <summary>
    /// Inserts a new entity.
    /// </summary>
    public Task<TEntity> Insert(TEntity entity, CancellationToken cancellationToken = default)
    {
        Func<Task<TEntity>> operation = () => _innerRepository.Insert(entity, cancellationToken);

        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var currentOperation = operation;
            operation = () => middleware.Insert(currentOperation, entity, cancellationToken);
        }

        return operation();
    }

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    public Task<TEntity> Update(TEntity entityToUpdate, CancellationToken cancellationToken = default)
    {
        Func<Task<TEntity>> operation = () =>
            _innerRepository.Update(entityToUpdate, cancellationToken);

        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var currentOperation = operation;
            operation = () => middleware.Update(currentOperation, entityToUpdate, cancellationToken);
        }

        return operation();
    }

    /// <summary>
    /// Deletes an entity.
    /// </summary>
    public Task<bool> Delete(TEntity entityToDelete, CancellationToken cancellationToken = default)
    {
        Func<Task<bool>> operation = () =>
            _innerRepository.Delete(entityToDelete, cancellationToken);

        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var currentOperation = operation;
            operation = () => middleware.Delete(currentOperation, entityToDelete, cancellationToken);
        }

        return operation();
    }

    /// <summary>
    /// Deletes an entity by its primary key.
    /// </summary>
    public Task<bool> Delete(TKey id, CancellationToken cancellationToken = default)
    {
        Func<Task<bool>> operation = () => _innerRepository.Delete(id, cancellationToken);

        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var currentOperation = operation;
            operation = () => middleware.DeleteById(currentOperation, id, cancellationToken);
        }

        return operation();
    }

    /// <summary>
    /// Inserts multiple entities.
    /// </summary>
    public Task<IEnumerable<TEntity>> InsertRange(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        Func<Task<IEnumerable<TEntity>>> operation = () =>
            _innerRepository.InsertRange(entities, cancellationToken);

        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var currentOperation = operation;
            operation = () => middleware.InsertRange(currentOperation, entities, cancellationToken);
        }

        return operation();
    }

    /// <summary>
    /// Updates multiple entities.
    /// </summary>
    public Task<IEnumerable<TEntity>> UpdateRange(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        Func<Task<IEnumerable<TEntity>>> operation = () =>
            _innerRepository.UpdateRange(entities, cancellationToken);

        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var currentOperation = operation;
            operation = () => middleware.UpdateRange(currentOperation, entities, cancellationToken);
        }

        return operation();
    }

    /// <summary>
    /// Deletes multiple entities.
    /// </summary>
    public Task<int> DeleteRange(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        Func<Task<int>> operation = () =>
            _innerRepository.DeleteRange(entities, cancellationToken);

        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var currentOperation = operation;
            operation = () => middleware.DeleteRange(currentOperation, entities, cancellationToken);
        }

        return operation();
    }

    /// <summary>
    /// Deletes entities matching the specified filter.
    /// </summary>
    public Task<int> DeleteRange(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        Func<Task<int>> operation = () =>
            _innerRepository.DeleteRange(filter, cancellationToken);

        for (int i = _middlewares.Count - 1; i >= 0; i--)
        {
            var middleware = _middlewares[i];
            var currentOperation = operation;
            operation = () => middleware.DeleteRangeWithFilter(currentOperation, filter, cancellationToken);
        }

        return operation();
    }

    /// <summary>
    /// Streams entities asynchronously.
    /// Note: GetAsyncEnumerable does not support middleware interception in this implementation.
    /// </summary>
    public IAsyncEnumerable<TEntity> GetAsyncEnumerable(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default)
    {
        // For streaming operations, we delegate directly to the inner repository
        // as middleware interception of streaming is complex and may not be desired
        return _innerRepository.GetAsyncEnumerable(filter, orderBy, includeProperties, cancellationToken);
    }
}

/// <summary>
/// Repository implementation that wraps another repository and applies a middleware pipeline.
/// Uses integer primary keys for backward compatibility.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
public class MiddlewareRepository<TEntity> : MiddlewareRepository<TEntity, int>
    where TEntity : class
{
    /// <summary>
    /// Initializes a new instance of the MiddlewareRepository class.
    /// </summary>
    /// <param name="innerRepository">The repository to wrap</param>
    /// <param name="middlewares">The middleware components to apply in order</param>
    public MiddlewareRepository(
        IGenericRepository<TEntity, int> innerRepository,
        params IRepositoryMiddleware<TEntity, int>[] middlewares)
        : base(innerRepository, middlewares)
    {
    }
}
