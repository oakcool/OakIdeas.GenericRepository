using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Middleware.Standard;

/// <summary>
/// Middleware that monitors and reports performance metrics for repository operations.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public class PerformanceMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class
{
    private readonly Action<string, long> _performanceReporter;
    private readonly long _slowOperationThresholdMs;

    /// <summary>
    /// Initializes a new instance of the PerformanceMiddleware class.
    /// </summary>
    /// <param name="performanceReporter">Action to report performance metrics (operation name, duration in ms)</param>
    /// <param name="slowOperationThresholdMs">Threshold in milliseconds for reporting slow operations</param>
    public PerformanceMiddleware(
        Action<string, long> performanceReporter,
        long slowOperationThresholdMs = 1000)
    {
        _performanceReporter = performanceReporter ?? throw new ArgumentNullException(nameof(performanceReporter));
        _slowOperationThresholdMs = slowOperationThresholdMs;
    }

    public override async Task<IEnumerable<TEntity>> Get(
        Func<Task<IEnumerable<TEntity>>> next,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default)
    {
        return await MonitorPerformance("Get", next);
    }

    public override async Task<TEntity?> GetById(
        Func<Task<TEntity?>> next,
        TKey id,
        CancellationToken cancellationToken = default)
    {
        return await MonitorPerformance("GetById", next);
    }

    public override async Task<IEnumerable<TEntity>> GetWithQuery(
        Func<Task<IEnumerable<TEntity>>> next,
        Query<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        return await MonitorPerformance("GetWithQuery", next);
    }

    public override async Task<TEntity> Insert(
        Func<Task<TEntity>> next,
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await MonitorPerformance("Insert", next);
    }

    public override async Task<TEntity> Update(
        Func<Task<TEntity>> next,
        TEntity entityToUpdate,
        CancellationToken cancellationToken = default)
    {
        return await MonitorPerformance("Update", next);
    }

    public override async Task<bool> Delete(
        Func<Task<bool>> next,
        TEntity entityToDelete,
        CancellationToken cancellationToken = default)
    {
        return await MonitorPerformance("Delete", next);
    }

    public override async Task<bool> DeleteById(
        Func<Task<bool>> next,
        TKey id,
        CancellationToken cancellationToken = default)
    {
        return await MonitorPerformance("DeleteById", next);
    }

    public override async Task<IEnumerable<TEntity>> InsertRange(
        Func<Task<IEnumerable<TEntity>>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        return await MonitorPerformance("InsertRange", next);
    }

    public override async Task<IEnumerable<TEntity>> UpdateRange(
        Func<Task<IEnumerable<TEntity>>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        return await MonitorPerformance("UpdateRange", next);
    }

    public override async Task<int> DeleteRange(
        Func<Task<int>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        return await MonitorPerformance("DeleteRange", next);
    }

    public override async Task<int> DeleteRangeWithFilter(
        Func<Task<int>> next,
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        return await MonitorPerformance("DeleteRangeWithFilter", next);
    }

    private async Task<T> MonitorPerformance<T>(string operationName, Func<Task<T>> operation)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await operation();
            sw.Stop();
            
            var entityName = typeof(TEntity).Name;
            var fullOperationName = $"{entityName}.{operationName}";
            
            _performanceReporter(fullOperationName, sw.ElapsedMilliseconds);
            
            if (sw.ElapsedMilliseconds >= _slowOperationThresholdMs)
            {
                _performanceReporter($"{fullOperationName} (SLOW)", sw.ElapsedMilliseconds);
            }
            
            return result;
        }
        catch
        {
            sw.Stop();
            throw;
        }
    }
}
