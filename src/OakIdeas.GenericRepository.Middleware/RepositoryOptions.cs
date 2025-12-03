using System;
using System.Collections.Generic;

namespace OakIdeas.GenericRepository.Middleware;

/// <summary>
/// Configuration options for repositories with middleware support.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public class RepositoryOptions<TEntity, TKey> 
    where TEntity : class
    where TKey : notnull
{
    private readonly List<IRepositoryMiddleware<TEntity, TKey>> _middlewares = new();

    /// <summary>
    /// Gets the list of middleware components to apply to repository operations.
    /// </summary>
    public IReadOnlyList<IRepositoryMiddleware<TEntity, TKey>> Middlewares => _middlewares;

    /// <summary>
    /// Adds a middleware component to the pipeline.
    /// </summary>
    /// <param name="middleware">The middleware to add</param>
    /// <returns>This options instance for fluent chaining</returns>
    public RepositoryOptions<TEntity, TKey> UseMiddleware(IRepositoryMiddleware<TEntity, TKey> middleware)
    {
        if (middleware == null)
            throw new ArgumentNullException(nameof(middleware));
            
        _middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// Adds multiple middleware components to the pipeline.
    /// </summary>
    /// <param name="middlewares">The middlewares to add</param>
    /// <returns>This options instance for fluent chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when middlewares array is null or contains null items</exception>
    public RepositoryOptions<TEntity, TKey> UseMiddlewares(params IRepositoryMiddleware<TEntity, TKey>[] middlewares)
    {
        if (middlewares == null)
            throw new ArgumentNullException(nameof(middlewares));
            
        foreach (var middleware in middlewares)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware), "Middleware array contains null item");
            }
            _middlewares.Add(middleware);
        }
        return this;
    }

    /// <summary>
    /// Clears all middleware from the pipeline.
    /// </summary>
    /// <returns>This options instance for fluent chaining</returns>
    public RepositoryOptions<TEntity, TKey> ClearMiddlewares()
    {
        _middlewares.Clear();
        return this;
    }

    /// <summary>
    /// Converts the middleware list to an array.
    /// </summary>
    /// <returns>Array of middleware components</returns>
    internal IRepositoryMiddleware<TEntity, TKey>[] ToArray()
    {
        return _middlewares.ToArray();
    }
}
