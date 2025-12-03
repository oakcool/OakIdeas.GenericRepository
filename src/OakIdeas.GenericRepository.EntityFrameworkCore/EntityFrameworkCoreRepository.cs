using Microsoft.EntityFrameworkCore;
using OakIdeas.GenericRepository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.EntityFrameworkCore
{
    /// <summary>
    /// Entity Framework Core implementation of the generic repository pattern.
    /// Provides CRUD operations with support for eager loading and LINQ queries.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TDataContext">The DbContext type</typeparam>
	public class EntityFrameworkCoreRepository<TEntity, TDataContext> : IGenericRepository<TEntity> where TEntity : class where TDataContext : DbContext
    {
        protected readonly TDataContext context;
        internal DbSet<TEntity> dbSet;

        public EntityFrameworkCoreRepository(TDataContext dataContext)
        {
            context = dataContext;
            dbSet = context.Set<TEntity>();
        }


        /// <summary>
        /// Generic Get method with LINQ filter support, ordering, and eager loading of navigation properties.
        /// </summary>
        /// <param name="filter">Optional LINQ filter expression</param>
        /// <param name="orderBy">Optional ordering function</param>
        /// <param name="includeProperties">Comma-separated list of navigation properties to include</param>
        /// <returns>Collection of entities matching the criteria</returns>
        public virtual async Task<IEnumerable<TEntity>> Get(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "")
        {
            // Get the dbSet from the Entity passed in                
            IQueryable<TEntity> query = dbSet;

            // Apply the filter
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // Include the specified properties
            foreach (var includeProperty in includeProperties.Split
                (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            // Sort
            if (orderBy != null)
            {
                return orderBy(query).ToList();
            }
            else
            {
                return await query.ToListAsync();
            }
        }

        /// <summary>
        /// Gets an entity by its primary key.
        /// </summary>
        /// <param name="id">The primary key value</param>
        /// <returns>The entity if found, null otherwise</returns>
        public virtual async Task<TEntity> Get(object id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            return await dbSet.FindAsync(id);
        }

        /// <summary>
        /// Inserts a new entity into the database.
        /// </summary>
        /// <param name="entity">The entity to insert</param>
        /// <returns>The inserted entity</returns>
        public virtual async Task<TEntity> Insert(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await dbSet.AddAsync(entity);
            await context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// Deletes an entity by its primary key.
        /// </summary>
        /// <param name="id">The primary key value</param>
        /// <returns>True if deletion was successful, false otherwise</returns>
        public virtual async Task<bool> Delete(object id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            TEntity entityToDelete = await dbSet.FindAsync(id);
            return await Delete(entityToDelete);
        }

        /// <summary>
        /// Deletes an entity from the database.
        /// </summary>
        /// <param name="entityToDelete">The entity to delete</param>
        /// <returns>True if deletion was successful, false otherwise</returns>
        public virtual async Task<bool> Delete(TEntity entityToDelete)
        {
            if (entityToDelete == null)
                throw new ArgumentNullException(nameof(entityToDelete));

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
            if (entityToUpdate == null)
                throw new ArgumentNullException(nameof(entityToUpdate));

            var dbSet = context.Set<TEntity>();
            dbSet.Attach(entityToUpdate);
            context.Entry(entityToUpdate).State = EntityState.Modified;
            await context.SaveChangesAsync();
            return entityToUpdate;
        }
    }
}
