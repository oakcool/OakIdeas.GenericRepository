using OakIdeas.GenericRepository.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository;

/// <summary>
/// In-memory implementation of the generic repository pattern using a concurrent dictionary.
/// Suitable for testing and development scenarios.
/// </summary>
/// <typeparam name="TEntity">The entity type, must inherit from EntityBase</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public class MemoryGenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : EntityBase<TKey>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TEntity> _data = new();
    private int _nextId = 1;
    private static readonly bool _isIntKey = typeof(TKey) == typeof(int);

    /// <summary>
    /// Deletes an entity from the repository.
    /// </summary>
    /// <param name="entityToDelete">The entity to delete</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when entityToDelete is null</exception>
    public Task<bool> Delete(TEntity entityToDelete)
    {
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
    /// <returns>True if deletion was successful, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when id is null</exception>
    public Task<bool> Delete(TKey id)
    {
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
    /// <returns>Collection of entities matching the criteria</returns>
    public Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>>? filter = null, 
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, 
        string includeProperties = "")
    {
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
    /// Gets an entity by its primary key.
    /// </summary>
    /// <param name="id">The primary key value</param>
    /// <returns>The entity if found, null otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when id is null</exception>
    public Task<TEntity?> Get(TKey id)
    {
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
    /// <returns>The inserted entity, or the existing entity if one with the same ID already exists</returns>
    /// <exception cref="ArgumentNullException">Thrown when entity is null</exception>
    public async Task<TEntity> Insert(TEntity entity)
    {
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
    /// <returns>The updated entity</returns>
    /// <exception cref="ArgumentNullException">Thrown when entityToUpdate is null</exception>
    public Task<TEntity> Update(TEntity entityToUpdate)
    {
        ThrowIfNull(entityToUpdate);

        if (_data.TryGetValue(entityToUpdate.ID, out _))
        {
            _data[entityToUpdate.ID] = entityToUpdate;
        }

        return Task.FromResult(entityToUpdate);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowIfNull(object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
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
