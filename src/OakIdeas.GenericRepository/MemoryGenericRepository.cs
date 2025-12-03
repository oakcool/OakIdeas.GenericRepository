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
public class MemoryGenericRepository<TEntity> : IGenericRepository<TEntity>
    where TEntity : EntityBase
{
    private readonly ConcurrentDictionary<int, TEntity> _data = [];
    private int _nextId = 1;

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
    public Task<bool> Delete(object id)
    {
        ThrowIfNull(id);

        if (id is int intId && _data.TryGetValue(intId, out _))
        {
            return Task.FromResult(_data.TryRemove(intId, out _));
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
    public Task<TEntity?> Get(object id)
    {
        ThrowIfNull(id);

        return Task.FromResult(
            id is int intId && _data.TryGetValue(intId, out var existing) 
                ? existing 
                : null
        );
    }

    /// <summary>
    /// Inserts a new entity into the repository. If the entity ID is 0, a new ID is generated.
    /// </summary>
    /// <param name="entity">The entity to insert</param>
    /// <returns>The inserted entity, or the existing entity if one with the same ID already exists</returns>
    /// <exception cref="ArgumentNullException">Thrown when entity is null</exception>
    public async Task<TEntity> Insert(TEntity entity)
    {
        ThrowIfNull(entity);

        if (entity.ID == 0)
        {
            entity.ID = await GetNextID();
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
