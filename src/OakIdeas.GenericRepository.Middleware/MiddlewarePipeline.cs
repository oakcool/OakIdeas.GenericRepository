using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Middleware;

/// <summary>
/// Manages and executes a pipeline of middleware components for repository operations.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public class MiddlewarePipeline<TEntity, TKey> where TEntity : class where TKey : notnull
{
    private readonly List<IRepositoryMiddleware<TEntity, TKey>> _middlewares = new();

    /// <summary>
    /// Adds a middleware component to the end of the pipeline.
    /// </summary>
    /// <param name="middleware">The middleware to add</param>
    public void Use(IRepositoryMiddleware<TEntity, TKey> middleware)
    {
        if (middleware == null)
            throw new ArgumentNullException(nameof(middleware));

        _middlewares.Add(middleware);
    }

    /// <summary>
    /// Executes the middleware pipeline with the specified context.
    /// </summary>
    /// <param name="context">The repository operation context</param>
    /// <param name="finalOperation">The final operation to execute after all middleware</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task ExecuteAsync(
        RepositoryContext<TEntity, TKey> context,
        Func<RepositoryContext<TEntity, TKey>, Task> finalOperation)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (finalOperation == null)
            throw new ArgumentNullException(nameof(finalOperation));

        // Build the pipeline in reverse order
        RepositoryMiddlewareDelegate<TEntity, TKey> pipeline = async ctx =>
        {
            if (!ctx.ShortCircuit)
            {
                await finalOperation(ctx);
            }
        };

        // Chain middleware in reverse order so they execute in the order they were added
        foreach (var middleware in _middlewares.AsEnumerable().Reverse())
        {
            var next = pipeline;
            var current = middleware;
            pipeline = async ctx =>
            {
                if (!ctx.ShortCircuit)
                {
                    await current.InvokeAsync(ctx, next);
                }
                else
                {
                    await next(ctx);
                }
            };
        }

        // Execute the pipeline
        await pipeline(context);
    }

    /// <summary>
    /// Gets the count of middleware components in the pipeline.
    /// </summary>
    public int Count => _middlewares.Count;

    /// <summary>
    /// Clears all middleware from the pipeline.
    /// </summary>
    public void Clear()
    {
        _middlewares.Clear();
    }
}
