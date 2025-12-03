using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Middleware.Standard;

/// <summary>
/// Middleware that logs repository operations for debugging and monitoring.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public class LoggingMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class
{
    private readonly Action<string> _logger;
    private readonly bool _logPerformance;

    /// <summary>
    /// Initializes a new instance of the LoggingMiddleware class.
    /// </summary>
    /// <param name="logger">Action to log messages</param>
    /// <param name="logPerformance">Whether to log operation duration</param>
    public LoggingMiddleware(Action<string> logger, bool logPerformance = true)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logPerformance = logPerformance;
    }

    public override async Task<IEnumerable<TEntity>> Get(
        Func<Task<IEnumerable<TEntity>>> next,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default)
    {
        return await LogOperation("Get", next);
    }

    public override async Task<TEntity?> GetById(
        Func<Task<TEntity?>> next,
        TKey id,
        CancellationToken cancellationToken = default)
    {
        return await LogOperation($"GetById({id})", next);
    }

    public override async Task<IEnumerable<TEntity>> GetWithQuery(
        Func<Task<IEnumerable<TEntity>>> next,
        Query<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        return await LogOperation("GetWithQuery", next);
    }

    public override async Task<TEntity> Insert(
        Func<Task<TEntity>> next,
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await LogOperation("Insert", next);
    }

    public override async Task<TEntity> Update(
        Func<Task<TEntity>> next,
        TEntity entityToUpdate,
        CancellationToken cancellationToken = default)
    {
        return await LogOperation("Update", next);
    }

    public override async Task<bool> Delete(
        Func<Task<bool>> next,
        TEntity entityToDelete,
        CancellationToken cancellationToken = default)
    {
        return await LogOperation("Delete", next);
    }

    public override async Task<bool> DeleteById(
        Func<Task<bool>> next,
        TKey id,
        CancellationToken cancellationToken = default)
    {
        return await LogOperation($"DeleteById({id})", next);
    }

    public override async Task<IEnumerable<TEntity>> InsertRange(
        Func<Task<IEnumerable<TEntity>>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var entityList = entities?.ToList() ?? new List<TEntity>();
        return await LogOperation($"InsertRange(count={entityList.Count})", next);
    }

    public override async Task<IEnumerable<TEntity>> UpdateRange(
        Func<Task<IEnumerable<TEntity>>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var entityList = entities?.ToList() ?? new List<TEntity>();
        return await LogOperation($"UpdateRange(count={entityList.Count})", next);
    }

    public override async Task<int> DeleteRange(
        Func<Task<int>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var entityList = entities?.ToList() ?? new List<TEntity>();
        return await LogOperation($"DeleteRange(count={entityList.Count})", next);
    }

    public override async Task<int> DeleteRangeWithFilter(
        Func<Task<int>> next,
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        return await LogOperation("DeleteRangeWithFilter", next);
    }

    private async Task<T> LogOperation<T>(string operationName, Func<Task<T>> operation)
    {
        var entityName = typeof(TEntity).Name;
        
        if (_logPerformance)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger($"[{entityName}] Starting {operationName}");
                var result = await operation();
                sw.Stop();
                _logger($"[{entityName}] Completed {operationName} in {sw.ElapsedMilliseconds}ms");
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger($"[{entityName}] Failed {operationName} in {sw.ElapsedMilliseconds}ms: {ex.Message}");
                throw;
            }
        }
        else
        {
            try
            {
                _logger($"[{entityName}] Starting {operationName}");
                var result = await operation();
                _logger($"[{entityName}] Completed {operationName}");
                return result;
            }
            catch (Exception ex)
            {
                _logger($"[{entityName}] Failed {operationName}: {ex.Message}");
                throw;
            }
        }
    }
}
