using OakIdeas.GenericRepository.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository
{
    /// <summary>
    /// In-memory implementation of the generic repository pattern using a concurrent dictionary.
    /// Suitable for testing and development scenarios.
    /// </summary>
    /// <typeparam name="TEntity">The entity type, must inherit from EntityBase</typeparam>
    public class MemoryGenericRepository<TEntity> : IGenericRepository<TEntity>
        where TEntity : EntityBase
    {
        readonly ConcurrentDictionary<int, TEntity> _data;

        public MemoryGenericRepository()
        {
            _data = new ConcurrentDictionary<int, TEntity>();
        }

        /// <summary>
        /// Deletes an entity from the repository.
        /// </summary>
        /// <param name="entityToDelete">The entity to delete</param>
        /// <returns>True if deletion was successful, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when entityToDelete is null</exception>
        public Task<bool> Delete(TEntity entityToDelete)
        {
            if (entityToDelete == null)
                throw new ArgumentNullException(nameof(entityToDelete));

            if (_data.TryGetValue(entityToDelete.ID, out TEntity existing))
            {
                return Task.FromResult(_data.TryRemove(entityToDelete.ID, out TEntity oldEntity));
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
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (id is int @int && _data.TryGetValue(@int, out TEntity existing))
            {
                return Task.FromResult(_data.TryRemove(@int, out TEntity entity));
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
        public Task<IEnumerable<TEntity>> Get(Expression<Func<TEntity, bool>> filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null, string includeProperties = "")
        {
            IQueryable<TEntity> query = _data.Values.AsQueryable<TEntity>();

            // Apply the filter
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // Sort
            if (orderBy != null)
            {
                return Task.FromResult<IEnumerable<TEntity>>(orderBy(query).ToList());
            }
            else
            {
                return Task.FromResult<IEnumerable<TEntity>>(query.ToList());
            }
        }

        /// <summary>
        /// Gets an entity by its primary key.
        /// </summary>
        /// <param name="id">The primary key value</param>
        /// <returns>The entity if found, null otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when id is null</exception>
        public Task<TEntity> Get(object id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (id is int @int && _data.TryGetValue(@int, out TEntity existing))
            {
                return Task.FromResult(existing);
            }

            return Task.FromResult<TEntity>(null);
        }

        /// <summary>
        /// Inserts a new entity into the repository. If the entity ID is 0, a new ID is generated.
        /// </summary>
        /// <param name="entity">The entity to insert</param>
        /// <returns>The inserted entity, or the existing entity if one with the same ID already exists</returns>
        /// <exception cref="ArgumentNullException">Thrown when entity is null</exception>
        public async Task<TEntity> Insert(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity.ID == 0)
            {
                entity.ID = await GetNextID();
            }

            if (_data.TryGetValue(entity.ID, out TEntity existing))
            {
                return existing;
            }
            else
            {
                _data.AddOrUpdate(entity.ID, id => entity, (id, oldEntity) => entity);

                return entity;
            }
        }

        /// <summary>
        /// Updates an existing entity in the repository.
        /// </summary>
        /// <param name="entityToUpdate">The entity to update</param>
        /// <returns>The updated entity</returns>
        /// <exception cref="ArgumentNullException">Thrown when entityToUpdate is null</exception>
        public Task<TEntity> Update(TEntity entityToUpdate)
        {
            if (entityToUpdate == null)
                throw new ArgumentNullException(nameof(entityToUpdate));

            if (_data.TryGetValue(entityToUpdate.ID, out TEntity existing))
            {
                _data[entityToUpdate.ID] = entityToUpdate;
            }

            return Task.FromResult(entityToUpdate);
        }

        /// <summary>
        /// Generates the next available ID for a new entity.
        /// </summary>
        /// <returns>The next available ID</returns>
        protected Task<int> GetNextID()
        {
            int next = 1;
            if (_data.Count > 0)
            {
                next = _data.Keys.Max(k => k) + 1;
            }
            return Task.FromResult(next);
        }
    }
}
