using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Middleware;

/// <summary>
/// Defines the context for a repository operation, containing all relevant data for middleware processing.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public class RepositoryContext<TEntity, TKey> where TEntity : class where TKey : notnull
{
    /// <summary>
    /// Gets or sets the operation type being performed.
    /// </summary>
    public RepositoryOperation Operation { get; set; }

    /// <summary>
    /// Gets or sets the entity being operated on (for single entity operations).
    /// </summary>
    public TEntity? Entity { get; set; }

    /// <summary>
    /// Gets or sets the entities being operated on (for batch operations).
    /// </summary>
    public IEnumerable<TEntity>? Entities { get; set; }

    /// <summary>
    /// Gets or sets the primary key value (for key-based operations).
    /// </summary>
    public TKey? Key { get; set; }

    /// <summary>
    /// Gets or sets the result of the operation.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets any error that occurred during the operation.
    /// </summary>
    public Exception? Error { get; set; }

    /// <summary>
    /// Gets the cancellation token for the operation.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Gets a dictionary for storing custom data between middleware components.
    /// </summary>
    public Dictionary<string, object> Items { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the operation should be short-circuited.
    /// When true, subsequent middleware and the actual operation will be skipped.
    /// </summary>
    public bool ShortCircuit { get; set; }
}

/// <summary>
/// Defines the types of repository operations.
/// </summary>
public enum RepositoryOperation
{
    /// <summary>Get operation - retrieving data</summary>
    Get,
    /// <summary>Insert operation - adding new data</summary>
    Insert,
    /// <summary>Update operation - modifying existing data</summary>
    Update,
    /// <summary>Delete operation - removing data</summary>
    Delete,
    /// <summary>InsertRange operation - adding multiple items</summary>
    InsertRange,
    /// <summary>UpdateRange operation - modifying multiple items</summary>
    UpdateRange,
    /// <summary>DeleteRange operation - removing multiple items</summary>
    DeleteRange
}
