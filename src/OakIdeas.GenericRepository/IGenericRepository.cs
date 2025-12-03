using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository;

/// <summary>
/// Generic repository interface for CRUD operations on entities with generic primary key support.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public interface IGenericRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// Deletes an entity from the repository.
    /// </summary>
    /// <param name="entityToDelete">The entity to delete</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<bool> Delete(TEntity entityToDelete, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by its primary key.
    /// </summary>
    /// <param name="id">The primary key value</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<bool> Delete(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with optional filtering, ordering, and eager loading.
    /// </summary>
    /// <param name="filter">Optional LINQ filter expression</param>
    /// <param name="orderBy">Optional ordering function</param>
    /// <param name="includeProperties">Comma-separated list of navigation properties to include</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Collection of entities matching the criteria</returns>
    Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with optional filtering, ordering, and type-safe eager loading of navigation properties.
    /// </summary>
    /// <param name="filter">Optional LINQ filter expression</param>
    /// <param name="orderBy">Optional ordering function</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <param name="includeExpressions">Type-safe expressions for navigation properties to include</param>
    /// <returns>Collection of entities matching the criteria</returns>
    Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeExpressions);

    /// <summary>
    /// Gets an entity by its primary key.
    /// </summary>
    /// <param name="id">The primary key value</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The entity if found, null otherwise</returns>
    Task<TEntity?> Get(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new entity into the repository.
    /// </summary>
    /// <param name="entity">The entity to insert</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The inserted entity</returns>
    Task<TEntity> Insert(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entityToUpdate">The entity to update</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The updated entity</returns>
    Task<TEntity> Update(TEntity entityToUpdate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts multiple entities into the repository in a single operation.
    /// </summary>
    /// <param name="entities">The collection of entities to insert</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The collection of inserted entities</returns>
    /// <exception cref="ArgumentNullException">Thrown when entities is null</exception>
    Task<IEnumerable<TEntity>> InsertRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities in the repository in a single operation.
    /// </summary>
    /// <param name="entities">The collection of entities to update</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The collection of updated entities</returns>
    /// <exception cref="ArgumentNullException">Thrown when entities is null</exception>
    Task<IEnumerable<TEntity>> UpdateRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple entities from the repository in a single operation.
    /// </summary>
    /// <param name="entities">The collection of entities to delete</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The number of entities deleted</returns>
    /// <exception cref="ArgumentNullException">Thrown when entities is null</exception>
    Task<int> DeleteRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all entities matching the specified filter in a single operation.
    /// </summary>
    /// <param name="filter">LINQ filter expression to identify entities to delete</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The number of entities deleted</returns>
    /// <exception cref="ArgumentNullException">Thrown when filter is null</exception>
    Task<int> DeleteRange(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams entities asynchronously without loading all results into memory at once.
    /// Use this method for processing large datasets to avoid memory issues.
    /// </summary>
    /// <param name="filter">Optional LINQ filter expression</param>
    /// <param name="orderBy">Optional ordering function</param>
    /// <param name="includeProperties">Comma-separated list of navigation properties to include</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>An async enumerable stream of entities matching the criteria</returns>
    IAsyncEnumerable<TEntity> GetAsyncEnumerable(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities using a query object that encapsulates all query parameters.
    /// This method provides a fluent, reusable way to build complex queries.
    /// </summary>
    /// <param name="query">The query object containing filter, ordering, includes, pagination, and tracking configuration</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Collection of entities matching the query criteria</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null</exception>
    Task<IEnumerable<TEntity>> Get(Query<TEntity> query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic repository interface for CRUD operations on entities with integer primary keys.
/// Provided for backward compatibility with existing code.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
public interface IGenericRepository<TEntity> : IGenericRepository<TEntity, int> where TEntity : class
{
}
