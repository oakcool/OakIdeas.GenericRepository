using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository;

/// <summary>
/// Generic repository interface for CRUD operations on entities.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
public interface IGenericRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Deletes an entity from the repository.
    /// </summary>
    /// <param name="entityToDelete">The entity to delete</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<bool> Delete(TEntity entityToDelete);

    /// <summary>
    /// Deletes an entity by its primary key.
    /// </summary>
    /// <param name="id">The primary key value</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<bool> Delete(object id);

    /// <summary>
    /// Gets entities with optional filtering, ordering, and eager loading.
    /// </summary>
    /// <param name="filter">Optional LINQ filter expression</param>
    /// <param name="orderBy">Optional ordering function</param>
    /// <param name="includeProperties">Comma-separated list of navigation properties to include</param>
    /// <returns>Collection of entities matching the criteria</returns>
    Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "");

    /// <summary>
    /// Gets an entity by its primary key.
    /// </summary>
    /// <param name="id">The primary key value</param>
    /// <returns>The entity if found, null otherwise</returns>
    Task<TEntity?> Get(object id);

    /// <summary>
    /// Inserts a new entity into the repository.
    /// </summary>
    /// <param name="entity">The entity to insert</param>
    /// <returns>The inserted entity</returns>
    Task<TEntity> Insert(TEntity entity);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entityToUpdate">The entity to update</param>
    /// <returns>The updated entity</returns>
    Task<TEntity> Update(TEntity entityToUpdate);
}
