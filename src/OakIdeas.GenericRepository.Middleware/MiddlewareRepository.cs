using System;

namespace OakIdeas.GenericRepository.Middleware;

/// <summary>
/// Repository implementation that wraps another repository and applies a middleware pipeline.
/// This enables cross-cutting concerns like logging, caching, validation, etc.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
[Obsolete("Use ComposableRepository instead. MiddlewareRepository will be removed in a future version.")]
public class MiddlewareRepository<TEntity, TKey> : ComposableRepository<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    /// <summary>
    /// Initializes a new instance of the MiddlewareRepository class.
    /// </summary>
    /// <param name="innerRepository">The repository to wrap</param>
    /// <param name="middlewares">The middleware components to apply in order</param>
    public MiddlewareRepository(
        IGenericRepository<TEntity, TKey> innerRepository,
        params IRepositoryMiddleware<TEntity, TKey>[] middlewares)
        : base(innerRepository, middlewares)
    {
    }
}
