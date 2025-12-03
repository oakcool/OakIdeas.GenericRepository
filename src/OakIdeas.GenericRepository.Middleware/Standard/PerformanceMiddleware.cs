using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Middleware.Standard;

/// <summary>
/// Middleware that tracks performance metrics for repository operations.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public class PerformanceMiddleware<TEntity, TKey> : IRepositoryMiddleware<TEntity, TKey> 
    where TEntity : class 
    where TKey : notnull
{
    private readonly Action<string, long> _metricCollector;
    private readonly long _warningThresholdMs;

    /// <summary>
    /// Initializes a new instance of the PerformanceMiddleware class.
    /// </summary>
    /// <param name="metricCollector">Action to collect metrics (operation name, elapsed milliseconds)</param>
    /// <param name="warningThresholdMs">Threshold in milliseconds for slow operation warnings (0 to disable)</param>
    public PerformanceMiddleware(Action<string, long> metricCollector, long warningThresholdMs = 1000)
    {
        _metricCollector = metricCollector ?? throw new ArgumentNullException(nameof(metricCollector));
        _warningThresholdMs = warningThresholdMs;
    }

    /// <summary>
    /// Invokes the middleware with the specified context and next delegate.
    /// </summary>
    public async Task InvokeAsync(RepositoryContext<TEntity, TKey> context, RepositoryMiddlewareDelegate<TEntity, TKey> next)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            var operationName = $"{typeof(TEntity).Name}.{context.Operation}";
            _metricCollector(operationName, stopwatch.ElapsedMilliseconds);

            // Store timing in context for other middleware
            context.Items["PerformanceMs"] = stopwatch.ElapsedMilliseconds;

            // Optional: Log warning for slow operations
            if (_warningThresholdMs > 0 && stopwatch.ElapsedMilliseconds > _warningThresholdMs)
            {
                context.Items["SlowOperation"] = true;
            }
        }
    }
}
