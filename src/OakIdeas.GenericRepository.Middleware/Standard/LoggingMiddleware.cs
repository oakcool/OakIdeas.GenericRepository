using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Middleware.Standard;

/// <summary>
/// Middleware that logs repository operations with timing information.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public class LoggingMiddleware<TEntity, TKey> : IRepositoryMiddleware<TEntity, TKey> 
    where TEntity : class 
    where TKey : notnull
{
    private readonly Action<string> _logger;
    private readonly bool _includeTimings;

    /// <summary>
    /// Initializes a new instance of the LoggingMiddleware class.
    /// </summary>
    /// <param name="logger">The logging action</param>
    /// <param name="includeTimings">Whether to include timing information</param>
    public LoggingMiddleware(Action<string> logger, bool includeTimings = true)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _includeTimings = includeTimings;
    }

    /// <summary>
    /// Invokes the middleware with the specified context and next delegate.
    /// </summary>
    public async Task InvokeAsync(RepositoryContext<TEntity, TKey> context, RepositoryMiddlewareDelegate<TEntity, TKey> next)
    {
        var entityName = typeof(TEntity).Name;
        var operation = context.Operation;

        _logger($"[Repository] Starting {operation} operation on {entityName}");

        Stopwatch? stopwatch = null;
        if (_includeTimings)
        {
            stopwatch = Stopwatch.StartNew();
        }

        try
        {
            await next(context);

            if (stopwatch != null)
            {
                stopwatch.Stop();
                _logger($"[Repository] Completed {operation} operation on {entityName} in {stopwatch.ElapsedMilliseconds}ms - Success: {context.Success}");
            }
            else
            {
                _logger($"[Repository] Completed {operation} operation on {entityName} - Success: {context.Success}");
            }
        }
        catch (Exception ex)
        {
            if (stopwatch != null)
            {
                stopwatch.Stop();
                _logger($"[Repository] Failed {operation} operation on {entityName} in {stopwatch.ElapsedMilliseconds}ms - Error: {ex.Message}");
            }
            else
            {
                _logger($"[Repository] Failed {operation} operation on {entityName} - Error: {ex.Message}");
            }
            throw;
        }
    }
}
