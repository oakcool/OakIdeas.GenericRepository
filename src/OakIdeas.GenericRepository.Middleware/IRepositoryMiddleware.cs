using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Middleware;

/// <summary>
/// Defines the contract for repository middleware components.
/// Middleware can intercept and process repository operations before and after execution.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public interface IRepositoryMiddleware<TEntity, TKey> where TEntity : class where TKey : notnull
{
    /// <summary>
    /// Invokes the middleware with the specified context and next delegate.
    /// </summary>
    /// <param name="context">The repository operation context</param>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task InvokeAsync(RepositoryContext<TEntity, TKey> context, RepositoryMiddlewareDelegate<TEntity, TKey> next);
}

/// <summary>
/// Represents a delegate for the next middleware in the pipeline.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
/// <param name="context">The repository operation context</param>
/// <returns>A task representing the asynchronous operation</returns>
public delegate Task RepositoryMiddlewareDelegate<TEntity, TKey>(RepositoryContext<TEntity, TKey> context) 
    where TEntity : class 
    where TKey : notnull;
