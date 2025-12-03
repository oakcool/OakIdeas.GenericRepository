using Microsoft.EntityFrameworkCore;
using OakIdeas.GenericRepository;
using OakIdeas.GenericRepository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
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
    /// <returns>Collection of entities matching the criteria</returns>
    public virtual async Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "")
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

        // Sort
        return orderBy is not null 
            ? orderBy(query).ToList() 
            : await query.ToListAsync();
    }

    /// <summary>
    /// Gets an entity by its primary key.
    /// </summary>
    /// <param name="id">The primary key value</param>
    /// <returns>The entity if found, null otherwise</returns>
    public virtual async Task<TEntity?> Get(TKey id)
    {
        ThrowIfNull(id);

        return await dbSet.FindAsync(id);
    }

    /// <summary>
    /// Inserts a new entity into the database.
    /// </summary>
    /// <param name="entity">The entity to insert</param>
    /// <returns>The inserted entity</returns>
    public virtual async Task<TEntity> Insert(TEntity entity)
    {
        ThrowIfNull(entity);

        await dbSet.AddAsync(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Deletes an entity by its primary key.
    /// </summary>
    /// <param name="id">The primary key value</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    public virtual async Task<bool> Delete(TKey id)
    {
        ThrowIfNull(id);

        var entityToDelete = await dbSet.FindAsync(id);
        return await Delete(entityToDelete);
    }

    /// <summary>
    /// Deletes an entity from the database.
    /// </summary>
    /// <param name="entityToDelete">The entity to delete</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    public virtual async Task<bool> Delete(TEntity entityToDelete)
    {
        ThrowIfNull(entityToDelete);

        if (context.Entry(entityToDelete).State == EntityState.Detached)
        {
            dbSet.Attach(entityToDelete);
        }
        dbSet.Remove(entityToDelete);
        return await context.SaveChangesAsync() >= 1;
    }

    /// <summary>
    /// Updates an existing entity in the database.
    /// </summary>
    /// <param name="entityToUpdate">The entity to update</param>
    /// <returns>The updated entity</returns>
    public virtual async Task<TEntity> Update(TEntity entityToUpdate)
    {
        ThrowIfNull(entityToUpdate);

        var entityDbSet = context.Set<TEntity>();
        entityDbSet.Attach(entityToUpdate);
        context.Entry(entityToUpdate).State = EntityState.Modified;
        await context.SaveChangesAsync();
        return entityToUpdate;
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
