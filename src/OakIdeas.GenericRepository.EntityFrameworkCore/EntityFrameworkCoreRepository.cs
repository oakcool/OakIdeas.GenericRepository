using Microsoft.EntityFrameworkCore;
using OakIdeas.GenericRepository;
using OakIdeas.GenericRepository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core implementation of the generic repository pattern.
/// Provides CRUD operations with support for eager loading and LINQ queries.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TDataContext">The DbContext type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public class EntityFrameworkCoreRepository<TEntity, TDataContext, TKey>(TDataContext dataContext) : IGenericRepository<TEntity, TKey>
    where TEntity : class 
    where TDataContext : DbContext
    where TKey : notnull
{
    protected readonly TDataContext context = dataContext;
    internal DbSet<TEntity> dbSet = dataContext.Set<TEntity>();

    /// <summary>
    /// Generic Get method with LINQ filter support, ordering, and eager loading of navigation properties.
    /// </summary>
    /// <param name="filter">Optional LINQ filter expression</param>
    /// <param name="orderBy">Optional ordering function</param>
    /// <param name="includeProperties">Comma-separated list of navigation properties to include</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Collection of entities matching the criteria</returns>
    public virtual async Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default)
    {
        // Get the dbSet from the Entity passed in                
        IQueryable<TEntity> query = dbSet;

        // Apply the filter
        if (filter is not null)
        {
            query = query.Where(filter);
        }

        // Include the specified properties
        foreach (var includeProperty in includeProperties.Split([','], StringSplitOptions.RemoveEmptyEntries))
        {
            query = query.Include(includeProperty);
        }

        // Sort and return
        if (orderBy is not null)
        {
            return await orderBy(query).ToListAsync(cancellationToken);
        }
        
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Generic Get method with LINQ filter support, ordering, and type-safe eager loading of navigation properties.
    /// </summary>
    /// <param name="filter">Optional LINQ filter expression</param>
    /// <param name="orderBy">Optional ordering function</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <param name="includeExpressions">Type-safe expressions for navigation properties to include</param>
    /// <returns>Collection of entities matching the criteria</returns>
    public virtual async Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeExpressions)
    {
        // Get the dbSet from the Entity passed in                
        IQueryable<TEntity> query = dbSet;

        // Apply the filter
        if (filter is not null)
        {
            query = query.Where(filter);
        }

        // Include the specified properties using type-safe expressions
        if (includeExpressions is not null)
        {
            foreach (var includeExpression in includeExpressions)
            {
                query = query.Include(includeExpression);
            }
        }

        // Sort and return
        if (orderBy is not null)
        {
            return await orderBy(query).ToListAsync(cancellationToken);
        }
        
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets an entity by its primary key.
    /// </summary>
    /// <param name="id">The primary key value</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The entity if found, null otherwise</returns>
    public virtual async Task<TEntity?> Get(TKey id, CancellationToken cancellationToken = default)
    {
        ThrowIfNull(id);

        return await dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <summary>
    /// Inserts a new entity into the database.
    /// </summary>
    /// <param name="entity">The entity to insert</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The inserted entity</returns>
    public virtual async Task<TEntity> Insert(TEntity entity, CancellationToken cancellationToken = default)
    {
        ThrowIfNull(entity);

        await dbSet.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <summary>
    /// Deletes an entity by its primary key.
    /// </summary>
    /// <param name="id">The primary key value</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    public virtual async Task<bool> Delete(TKey id, CancellationToken cancellationToken = default)
    {
        ThrowIfNull(id);

        var entityToDelete = await dbSet.FindAsync(new object[] { id }, cancellationToken);
        return await Delete(entityToDelete, cancellationToken);
    }

    /// <summary>
    /// Deletes an entity from the database.
    /// </summary>
    /// <param name="entityToDelete">The entity to delete</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    public virtual async Task<bool> Delete(TEntity entityToDelete, CancellationToken cancellationToken = default)
    {
        ThrowIfNull(entityToDelete);

        if (context.Entry(entityToDelete).State == EntityState.Detached)
        {
            dbSet.Attach(entityToDelete);
        }
        dbSet.Remove(entityToDelete);
        return await context.SaveChangesAsync(cancellationToken) >= 1;
    }

    /// <summary>
    /// Updates an existing entity in the database.
    /// </summary>
    /// <param name="entityToUpdate">The entity to update</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The updated entity</returns>
    public virtual async Task<TEntity> Update(TEntity entityToUpdate, CancellationToken cancellationToken = default)
    {
        ThrowIfNull(entityToUpdate);

        var entityDbSet = context.Set<TEntity>();
        entityDbSet.Attach(entityToUpdate);
        context.Entry(entityToUpdate).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
        return entityToUpdate;
    }

    /// <summary>
    /// Inserts multiple entities into the database in a single operation.
    /// </summary>
    /// <param name="entities">The collection of entities to insert</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The collection of inserted entities</returns>
    /// <exception cref="ArgumentNullException">Thrown when entities is null</exception>
    public virtual async Task<IEnumerable<TEntity>> InsertRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ThrowIfNull(entities);

        var entityList = entities.ToList();
        if (entityList.Count == 0)
        {
            return entityList;
        }

        await dbSet.AddRangeAsync(entityList, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entityList;
    }

    /// <summary>
    /// Updates multiple entities in the database in a single operation.
    /// </summary>
    /// <param name="entities">The collection of entities to update</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The collection of updated entities</returns>
    /// <exception cref="ArgumentNullException">Thrown when entities is null</exception>
    public virtual async Task<IEnumerable<TEntity>> UpdateRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ThrowIfNull(entities);

        var entityList = entities.ToList();
        if (entityList.Count == 0)
        {
            return entityList;
        }

        dbSet.UpdateRange(entityList);
        await context.SaveChangesAsync(cancellationToken);
        return entityList;
    }

    /// <summary>
    /// Deletes multiple entities from the database in a single operation.
    /// </summary>
    /// <param name="entities">The collection of entities to delete</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The number of entities deleted</returns>
    /// <exception cref="ArgumentNullException">Thrown when entities is null</exception>
    public virtual async Task<int> DeleteRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ThrowIfNull(entities);

        var entityList = entities.ToList();
        if (entityList.Count == 0)
        {
            return 0;
        }

        foreach (var entity in entityList)
        {
            if (context.Entry(entity).State == EntityState.Detached)
            {
                dbSet.Attach(entity);
            }
        }

        dbSet.RemoveRange(entityList);
        return await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes all entities matching the specified filter in a single operation.
    /// </summary>
    /// <param name="filter">LINQ filter expression to identify entities to delete</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The number of entities deleted</returns>
    /// <exception cref="ArgumentNullException">Thrown when filter is null</exception>
    public virtual async Task<int> DeleteRange(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
    {
        ThrowIfNull(filter);

        var entitiesToDelete = await dbSet.Where(filter).ToListAsync(cancellationToken);
        if (entitiesToDelete.Count == 0)
        {
            return 0;
        }

        dbSet.RemoveRange(entitiesToDelete);
        return await context.SaveChangesAsync(cancellationToken);
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
/// Entity Framework Core implementation of the generic repository pattern.
/// Provides CRUD operations with support for eager loading and LINQ queries. Uses integer primary keys.
/// Provided for backward compatibility with existing code.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TDataContext">The DbContext type</typeparam>
public class EntityFrameworkCoreRepository<TEntity, TDataContext>(TDataContext dataContext) : EntityFrameworkCoreRepository<TEntity, TDataContext, int>(dataContext)
    where TEntity : class 
    where TDataContext : DbContext
{
}
