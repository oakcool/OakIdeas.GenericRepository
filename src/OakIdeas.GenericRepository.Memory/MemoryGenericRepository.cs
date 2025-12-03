using OakIdeas.GenericRepository.Core;
using OakIdeas.GenericRepository.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Memory;

/// <summary>
/// In-memory implementation of the generic repository pattern using a concurrent dictionary.
/// Suitable for testing and development scenarios.
/// </summary>
/// <typeparam name="TEntity">The entity type, must inherit from EntityBase</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public class MemoryGenericRepository<TEntity, TKey> : CoreRepository<TEntity, TKey>, IGenericRepository<TEntity, TKey>
    where TEntity : EntityBase<TKey>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TEntity> _data = new();
    private int _nextId = 1;
    private static readonly bool _isIntKey = typeof(TKey) == typeof(int);

    /// <summary>
    /// Initializes a new instance of the MemoryGenericRepository class.
    /// </summary>
    /// <param name="options">Optional repository options</param>
    public MemoryGenericRepository(RepositoryOptions? options = null) : base(options)
    {
    }

    /// <summary>
    /// Deletes an entity from the repository.
    /// </summary>
    /// <param name="entityToDelete">The entity to delete</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when entityToDelete is null</exception>
    public Task<bool> Delete(TEntity entityToDelete, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfNull(entityToDelete);

        if (_data.TryGetValue(entityToDelete.ID, out _))
        {
            return Task.FromResult(_data.TryRemove(entityToDelete.ID, out _));
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// Deletes an entity by its primary key.
    /// </summary>
    /// <param name="id">The primary key value</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when id is null</exception>
    public Task<bool> Delete(TKey id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfNull(id);

        if (_data.TryGetValue(id, out _))
        {
            return Task.FromResult(_data.TryRemove(id, out _));
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// Gets entities with optional filtering and ordering.
    /// </summary>
    /// <param name="filter">Optional LINQ filter expression</param>
    /// <param name="orderBy">Optional ordering function</param>
    /// <param name="includeProperties">Not used in memory repository implementation</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Collection of entities matching the criteria</returns>
    public Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>>? filter = null, 
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, 
        string includeProperties = "",
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IQueryable<TEntity> query = _data.Values.AsQueryable();

        // Apply the filter
        if (filter is not null)
        {
            query = query.Where(filter);
        }

        // Sort
        return Task.FromResult<IEnumerable<TEntity>>(
            orderBy is not null ? orderBy(query).ToList() : query.ToList()
        );
    }

    /// <summary>
    /// Gets entities with optional filtering, ordering, and type-safe eager loading of navigation properties.
    /// Note: Include expressions are not used in memory repository implementation as relationships are loaded in-memory.
    /// </summary>
    /// <param name="filter">Optional LINQ filter expression</param>
    /// <param name="orderBy">Optional ordering function</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <param name="includeExpressions">Not used in memory repository implementation</param>
    /// <returns>Collection of entities matching the criteria</returns>
    public Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeExpressions)
    {
        // In memory repository doesn't use includes, so delegate to the main Get method
        return Get(filter, orderBy, string.Empty, cancellationToken);
    }

    /// <summary>
    /// Gets an entity by its primary key.
    /// </summary>
    /// <param name="id">The primary key value</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The entity if found, null otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when id is null</exception>
    public Task<TEntity?> Get(TKey id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfNull(id);

        return Task.FromResult(
            _data.TryGetValue(id, out var existing) 
                ? existing 
                : null
        );
    }

    /// <summary>
    /// Inserts a new entity into the repository. If the entity ID is the default value for its type, a new ID is generated (for integer keys only).
    /// </summary>
    /// <param name="entity">The entity to insert</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The inserted entity, or the existing entity if one with the same ID already exists</returns>
    /// <exception cref="ArgumentNullException">Thrown when entity is null</exception>
    public async Task<TEntity> Insert(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfNull(entity);

        if (_isIntKey && EqualityComparer<TKey>.Default.Equals(entity.ID, default!))
        {
            entity.ID = (TKey)(object)await GetNextID();
        }

        if (_data.TryGetValue(entity.ID, out var existing))
        {
            return existing;
        }

        _data.AddOrUpdate(entity.ID, _ => entity, (_, _) => entity);
        return entity;
    }

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entityToUpdate">The entity to update</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The updated entity</returns>
    /// <exception cref="ArgumentNullException">Thrown when entityToUpdate is null</exception>
    public Task<TEntity> Update(TEntity entityToUpdate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfNull(entityToUpdate);

        if (_data.TryGetValue(entityToUpdate.ID, out _))
        {
            _data[entityToUpdate.ID] = entityToUpdate;
        }

        return Task.FromResult(entityToUpdate);
    }

    /// <summary>
    /// Inserts multiple entities into the repository in a single operation.
    /// </summary>
    /// <param name="entities">The collection of entities to insert</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The collection of inserted entities</returns>
    /// <exception cref="ArgumentNullException">Thrown when entities is null</exception>
    public async Task<IEnumerable<TEntity>> InsertRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfNull(entities);

        var entityList = entities.ToList();
        if (entityList.Count == 0)
        {
            return entityList;
        }

        var insertedEntities = new List<TEntity>();
        foreach (var entity in entityList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var inserted = await Insert(entity, cancellationToken);
            insertedEntities.Add(inserted);
        }

        return insertedEntities;
    }

    /// <summary>
    /// Updates multiple entities in the repository in a single operation.
    /// </summary>
    /// <param name="entities">The collection of entities to update</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The collection of updated entities</returns>
    /// <exception cref="ArgumentNullException">Thrown when entities is null</exception>
    public Task<IEnumerable<TEntity>> UpdateRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfNull(entities);

        var entityList = entities.ToList();
        if (entityList.Count == 0)
        {
            return Task.FromResult<IEnumerable<TEntity>>(entityList);
        }

        var updatedEntities = new List<TEntity>();
        foreach (var entity in entityList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfNull(entity);

            if (_data.TryGetValue(entity.ID, out _))
            {
                _data[entity.ID] = entity;
            }
            updatedEntities.Add(entity);
        }

        return Task.FromResult<IEnumerable<TEntity>>(updatedEntities);
    }

    /// <summary>
    /// Deletes multiple entities from the repository in a single operation.
    /// </summary>
    /// <param name="entities">The collection of entities to delete</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The number of entities deleted</returns>
    /// <exception cref="ArgumentNullException">Thrown when entities is null</exception>
    public Task<int> DeleteRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfNull(entities);

        var entityList = entities.ToList();
        if (entityList.Count == 0)
        {
            return Task.FromResult(0);
        }

        int deletedCount = 0;
        foreach (var entity in entityList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfNull(entity);

            if (_data.TryRemove(entity.ID, out _))
            {
                deletedCount++;
            }
        }

        return Task.FromResult(deletedCount);
    }

    /// <summary>
    /// Deletes all entities matching the specified filter in a single operation.
    /// </summary>
    /// <param name="filter">LINQ filter expression to identify entities to delete</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The number of entities deleted</returns>
    /// <exception cref="ArgumentNullException">Thrown when filter is null</exception>
    public async Task<int> DeleteRange(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfNull(filter);

        var entitiesToDelete = await Get(filter: filter, cancellationToken: cancellationToken);
        return await DeleteRange(entitiesToDelete, cancellationToken);
    }

    /// <summary>
    /// Streams entities asynchronously without loading all results into memory at once.
    /// Use this method for processing large datasets to avoid memory issues.
    /// </summary>
    /// <param name="filter">Optional LINQ filter expression</param>
    /// <param name="orderBy">Optional ordering function</param>
    /// <param name="includeProperties">Not used in memory repository implementation</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>An async enumerable stream of entities matching the criteria</returns>
    public async IAsyncEnumerable<TEntity> GetAsyncEnumerable(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IQueryable<TEntity> query = _data.Values.AsQueryable();

        // Apply the filter
        if (filter is not null)
        {
            query = query.Where(filter);
        }

        // Sort
        var orderedQuery = orderBy is not null ? orderBy(query) : query;

        // Stream results one at a time
        foreach (var entity in orderedQuery)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return entity;
        }
    }

    /// <summary>
    /// Gets entities using a query object that encapsulates all query parameters.
    /// This method provides a fluent, reusable way to build complex queries.
    /// Note: AsNoTracking is ignored in the memory repository as it doesn't use entity tracking.
    /// </summary>
    /// <param name="query">The query object containing filter, ordering, includes, and pagination configuration</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Collection of entities matching the query criteria</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null</exception>
    public Task<IEnumerable<TEntity>> Get(Query<TEntity> query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfNull(query);

        IQueryable<TEntity> queryable = _data.Values.AsQueryable();

        // Apply filter
        if (query.Filter is not null)
        {
            queryable = queryable.Where(query.Filter);
        }

        // Apply ordering
        if (query.OrderBy is not null)
        {
            queryable = query.OrderBy(queryable);
        }

        // Apply pagination
        if (query.Skip.HasValue)
        {
            queryable = queryable.Skip(query.Skip.Value);
        }

        if (query.Take.HasValue)
        {
            queryable = queryable.Take(query.Take.Value);
        }

        return Task.FromResult<IEnumerable<TEntity>>(queryable.ToList());
    }

    /// <summary>
    /// Generates the next available ID for a new entity.
    /// Uses atomic increment for O(1) performance and thread safety.
    /// Only applicable for integer keys.
    /// </summary>
    /// <returns>The next available ID</returns>
    protected Task<int> GetNextID()
    {
        int next = Interlocked.Increment(ref _nextId);
        return Task.FromResult(next);
    }
}

/// <summary>
/// In-memory implementation of the generic repository pattern using a concurrent dictionary.
/// Suitable for testing and development scenarios. Uses integer primary keys.
/// Provided for backward compatibility with existing code.
/// </summary>
/// <typeparam name="TEntity">The entity type, must inherit from EntityBase</typeparam>
public class MemoryGenericRepository<TEntity> : MemoryGenericRepository<TEntity, int>
    where TEntity : EntityBase
{
}
