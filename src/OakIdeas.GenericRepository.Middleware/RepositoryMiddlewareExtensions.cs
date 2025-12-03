using System;
using System.Linq;

namespace OakIdeas.GenericRepository.Middleware;

/// <summary>
/// Extension methods for registering middleware with repositories.
/// </summary>
public static class RepositoryMiddlewareExtensions
{
    /// <summary>
    /// Wraps a repository with middleware support.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The type of the primary key</typeparam>
    /// <param name="repository">The repository to wrap</param>
    /// <param name="middlewares">The middleware components to apply</param>
    /// <returns>A repository with middleware applied</returns>
    public static IGenericRepository<TEntity, TKey> WithMiddleware<TEntity, TKey>(
        this IGenericRepository<TEntity, TKey> repository,
        params IRepositoryMiddleware<TEntity, TKey>[] middlewares)
        where TEntity : class
        where TKey : notnull
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));
            
        if (middlewares == null || middlewares.Length == 0)
            return repository;
            
        return new ComposableRepository<TEntity, TKey>(repository, middlewares);
    }

    /// <summary>
    /// Wraps a repository with middleware configured through options.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The type of the primary key</typeparam>
    /// <param name="repository">The repository to wrap</param>
    /// <param name="configure">Action to configure middleware options</param>
    /// <returns>A repository with middleware applied</returns>
    public static IGenericRepository<TEntity, TKey> WithMiddleware<TEntity, TKey>(
        this IGenericRepository<TEntity, TKey> repository,
        Action<RepositoryOptions<TEntity, TKey>> configure)
        where TEntity : class
        where TKey : notnull
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));
            
        var options = new RepositoryOptions<TEntity, TKey>();
        configure(options);
        
        if (options.Middlewares.Count == 0)
            return repository;
            
        var middlewares = options.Middlewares
            .OfType<IRepositoryMiddleware<TEntity, TKey>>()
            .ToArray();
            
        return new ComposableRepository<TEntity, TKey>(repository, middlewares);
    }
}
