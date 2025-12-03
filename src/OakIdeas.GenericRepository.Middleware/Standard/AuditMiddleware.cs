using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Middleware.Standard;

/// <summary>
/// Represents an audit log entry.
/// </summary>
public class AuditEntry
{
    /// <summary>
    /// Gets or sets the timestamp of the operation.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the name of the entity type.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation type (Insert, Update, Delete, etc.).
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user who performed the operation.
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Gets or sets additional details about the operation.
    /// </summary>
    public string? Details { get; set; }
}

/// <summary>
/// Middleware that creates audit trail entries for data modification operations.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public class AuditMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class
{
    private readonly Action<AuditEntry> _auditLogger;
    private readonly Func<string>? _userProvider;

    /// <summary>
    /// Initializes a new instance of the AuditMiddleware class.
    /// </summary>
    /// <param name="auditLogger">Action to log audit entries</param>
    /// <param name="userProvider">Optional function to get the current user</param>
    public AuditMiddleware(Action<AuditEntry> auditLogger, Func<string>? userProvider = null)
    {
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _userProvider = userProvider;
    }

    public override async Task<TEntity> Insert(
        Func<Task<TEntity>> next,
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        var result = await next();
        LogAudit("Insert", "Entity inserted");
        return result;
    }

    public override async Task<TEntity> Update(
        Func<Task<TEntity>> next,
        TEntity entityToUpdate,
        CancellationToken cancellationToken = default)
    {
        var result = await next();
        LogAudit("Update", "Entity updated");
        return result;
    }

    public override async Task<bool> Delete(
        Func<Task<bool>> next,
        TEntity entityToDelete,
        CancellationToken cancellationToken = default)
    {
        var result = await next();
        if (result)
        {
            LogAudit("Delete", "Entity deleted");
        }
        return result;
    }

    public override async Task<bool> DeleteById(
        Func<Task<bool>> next,
        TKey id,
        CancellationToken cancellationToken = default)
    {
        var result = await next();
        if (result)
        {
            LogAudit("Delete", $"Entity with ID {id} deleted");
        }
        return result;
    }

    public override async Task<IEnumerable<TEntity>> InsertRange(
        Func<Task<IEnumerable<TEntity>>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var result = await next();
        var count = result?.Count() ?? 0;
        LogAudit("InsertRange", $"{count} entities inserted");
        return result ?? Enumerable.Empty<TEntity>();
    }

    public override async Task<IEnumerable<TEntity>> UpdateRange(
        Func<Task<IEnumerable<TEntity>>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var result = await next();
        var count = result?.Count() ?? 0;
        LogAudit("UpdateRange", $"{count} entities updated");
        return result ?? Enumerable.Empty<TEntity>();
    }

    public override async Task<int> DeleteRange(
        Func<Task<int>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var result = await next();
        LogAudit("DeleteRange", $"{result} entities deleted");
        return result;
    }

    public override async Task<int> DeleteRangeWithFilter(
        Func<Task<int>> next,
        System.Linq.Expressions.Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        var result = await next();
        LogAudit("DeleteRangeWithFilter", $"{result} entities deleted by filter");
        return result;
    }

    private void LogAudit(string operation, string details)
    {
        var entry = new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            EntityType = typeof(TEntity).Name,
            Operation = operation,
            User = _userProvider?.Invoke(),
            Details = details
        };

        _auditLogger(entry);
    }
}
