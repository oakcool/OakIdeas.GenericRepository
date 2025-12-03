using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OakIdeas.GenericRepository.Models;

namespace OakIdeas.GenericRepository.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core repository implementation with soft delete support.
/// Automatically filters out soft-deleted entities in all queries.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements ISoftDeletable</typeparam>
/// <typeparam name="TContext">The DbContext type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public class SoftDeleteEntityFrameworkCoreRepository<TEntity, TContext, TKey> : EntityFrameworkCoreRepository<TEntity, TContext, TKey>
    where TEntity : class, ISoftDeletable
    where TContext : DbContext
    where TKey : notnull
{
    /// <summary>
    /// Initializes a new instance of the SoftDeleteEntityFrameworkCoreRepository class.
    /// </summary>
    /// <param name="context">The DbContext instance</param>
    public SoftDeleteEntityFrameworkCoreRepository(TContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets entities with optional filtering, ordering, and eager loading.
    /// Automatically excludes soft-deleted entities.
    /// </summary>
    public override async Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default)
    {
        var combinedFilter = CombineFilters(filter, e => !e.IsDeleted);
        return await base.Get(combinedFilter, orderBy, includeProperties, cancellationToken);
    }

    /// <summary>
    /// Gets entities with optional filtering, ordering, and type-safe eager loading of navigation properties.
    /// Automatically excludes soft-deleted entities.
    /// </summary>
    public override async Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeExpressions)
    {
        var combinedFilter = CombineFilters(filter, e => !e.IsDeleted);
        return await base.Get(combinedFilter, orderBy, cancellationToken, includeExpressions);
    }

    /// <summary>
    /// Gets entities using a query object.
    /// Automatically excludes soft-deleted entities.
    /// </summary>
    public override async Task<IEnumerable<TEntity>> Get(Query<TEntity> query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        var originalFilter = query.Filter;
        query.Filter = CombineFilters(originalFilter, e => !e.IsDeleted);

        try
        {
            return await base.Get(query, cancellationToken);
        }
        finally
        {
            query.Filter = originalFilter;
        }
    }

    /// <summary>
    /// Gets an entity by its primary key.
    /// Returns null if the entity is soft-deleted.
    /// </summary>
    public override async Task<TEntity?> Get(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await base.Get(id, cancellationToken);
        
        if (entity != null && entity.IsDeleted)
            return null;

        return entity;
    }

    /// <summary>
    /// Streams entities with optional filtering and ordering using IAsyncEnumerable.
    /// Automatically excludes soft-deleted entities.
    /// </summary>
    public override IAsyncEnumerable<TEntity> GetAsyncEnumerable(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default)
    {
        var combinedFilter = CombineFilters(filter, e => !e.IsDeleted);
        return base.GetAsyncEnumerable(combinedFilter, orderBy, includeProperties, cancellationToken);
    }

    /// <summary>
    /// Soft deletes an entity by marking it as deleted without removing it from the database.
    /// </summary>
    public override async Task<bool> Delete(TEntity entityToDelete, CancellationToken cancellationToken = default)
    {
        if (entityToDelete == null)
            throw new ArgumentNullException(nameof(entityToDelete));

        entityToDelete.IsDeleted = true;
        entityToDelete.DeletedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(_deletedBy))
        {
            entityToDelete.DeletedBy = _deletedBy;
            _deletedBy = null; // Clear after use to avoid unintended reuse
        }
        
        var updated = await Update(entityToDelete, cancellationToken);
        return updated != null;
    }

    /// <summary>
    /// Soft deletes an entity by its primary key.
    /// </summary>
    public override async Task<bool> Delete(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await base.Get(id, cancellationToken);
        if (entity == null)
            return false;

        return await Delete(entity, cancellationToken);
    }

    /// <summary>
    /// Soft deletes a range of entities.
    /// </summary>
    public override async Task<int> DeleteRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));

        var entityList = entities.ToList();
        var now = DateTime.UtcNow;
        var deletedBy = _deletedBy;
        _deletedBy = null; // Clear after capturing to avoid unintended reuse

        foreach (var entity in entityList)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = now;
            if (!string.IsNullOrEmpty(deletedBy))
            {
                entity.DeletedBy = deletedBy;
            }
        }

        await UpdateRange(entityList, cancellationToken);
        return entityList.Count;
    }

    /// <summary>
    /// Soft deletes entities matching the specified filter.
    /// </summary>
    public override async Task<int> DeleteRange(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));

        var entities = await base.Get(filter, null, "", cancellationToken);
        var entityList = entities.Where(e => !e.IsDeleted).ToList();

        if (entityList.Count == 0)
            return 0;

        return await DeleteRange(entityList, cancellationToken);
    }

    /// <summary>
    /// Gets entities including soft-deleted ones with optional filtering, ordering, and eager loading.
    /// </summary>
    public async Task<IEnumerable<TEntity>> GetIncludingDeleted(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default)
    {
        return await base.Get(filter, orderBy, includeProperties, cancellationToken);
    }

    /// <summary>
    /// Gets entities including soft-deleted ones with optional filtering, ordering, and type-safe eager loading.
    /// </summary>
    public async Task<IEnumerable<TEntity>> GetIncludingDeleted(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeExpressions)
    {
        return await base.Get(filter, orderBy, cancellationToken, includeExpressions);
    }

    /// <summary>
    /// Permanently deletes a soft-deleted entity from the database.
    /// </summary>
    public async Task<bool> PermanentlyDelete(TEntity entityToDelete, CancellationToken cancellationToken = default)
    {
        if (entityToDelete == null)
            throw new ArgumentNullException(nameof(entityToDelete));

        return await base.Delete(entityToDelete, cancellationToken);
    }

    /// <summary>
    /// Permanently deletes a soft-deleted entity by its primary key.
    /// </summary>
    public async Task<bool> PermanentlyDelete(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await base.Get(id, cancellationToken);
        if (entity == null)
            return false;

        return await PermanentlyDelete(entity, cancellationToken);
    }

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    public async Task<TEntity?> Restore(TEntity entityToRestore, CancellationToken cancellationToken = default)
    {
        if (entityToRestore == null)
            throw new ArgumentNullException(nameof(entityToRestore));

        if (!entityToRestore.IsDeleted)
            return entityToRestore;

        entityToRestore.IsDeleted = false;
        entityToRestore.DeletedAt = null;
        entityToRestore.DeletedBy = null;

        return await Update(entityToRestore, cancellationToken);
    }

    /// <summary>
    /// Restores a soft-deleted entity by its primary key.
    /// </summary>
    public async Task<TEntity?> Restore(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await base.Get(id, cancellationToken);
        if (entity == null)
            return null;

        return await Restore(entity, cancellationToken);
    }

    /// <summary>
    /// Sets the DeletedBy field for soft delete operations.
    /// Call this before Delete() to track who deleted the entity.
    /// </summary>
    public void SetDeletedBy(string deletedBy)
    {
        _deletedBy = deletedBy;
    }

    private string? _deletedBy;

    /// <summary>
    /// Combines two filter expressions using AND logic.
    /// </summary>
    private Expression<Func<TEntity, bool>>? CombineFilters(
        Expression<Func<TEntity, bool>>? first,
        Expression<Func<TEntity, bool>> second)
    {
        if (first == null)
            return second;

        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var combined = Expression.AndAlso(
            Expression.Invoke(first, parameter),
            Expression.Invoke(second, parameter)
        );
        return Expression.Lambda<Func<TEntity, bool>>(combined, parameter);
    }
}

/// <summary>
/// Entity Framework Core repository implementation with soft delete support for entities with integer primary keys.
/// Provided for backward compatibility with existing code.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements ISoftDeletable</typeparam>
/// <typeparam name="TContext">The DbContext type</typeparam>
public class SoftDeleteEntityFrameworkCoreRepository<TEntity, TContext> : SoftDeleteEntityFrameworkCoreRepository<TEntity, TContext, int>
    where TEntity : class, ISoftDeletable
    where TContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the SoftDeleteEntityFrameworkCoreRepository class.
    /// </summary>
    /// <param name="context">The DbContext instance</param>
    public SoftDeleteEntityFrameworkCoreRepository(TContext context) : base(context)
    {
    }
}
