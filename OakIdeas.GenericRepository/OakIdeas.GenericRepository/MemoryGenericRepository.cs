using OakIdeas.GenericRepository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository
{
	public class MemoryGenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : EntityBase
	{
		readonly Dictionary<int, TEntity> _data;

		public MemoryGenericRepository()
		{
			_data = new Dictionary<int, TEntity>();
		}

		public async Task<bool> Delete(TEntity entityToDelete)
		{
			return await Task.Run(() =>
			{
				if (_data.TryGetValue(entityToDelete.ID, out TEntity exiting))
				{
					_data.Remove(entityToDelete.ID);
				}

				return true;
			});
		}

		public async Task<bool> Delete(object id)
		{
			return await Task.Run(() =>
			{
				if (id is int @int && _data.TryGetValue(@int, out TEntity exiting))
				{
					_data.Remove(@int);
				}

				return true;
			});
		}

		public async Task<IEnumerable<TEntity>> Get(Expression<Func<TEntity, bool>> filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null, string includeProperties = "")
		{
			return await Task.Run(() =>
			{
				try
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
						return orderBy(query).ToList();
					}
					else
					{
						return query.ToList();
					}
				}
				catch (Exception ex)
				{
					var msg = ex.Message;
					return null;
				}
			});
		}

		public async Task<TEntity> Get(object id)
		{
			return await Task.Run(() =>
			{
				if (id is int @int && _data.TryGetValue(@int, out TEntity exiting))
				{
					return exiting;
				}

				return null;
			});
		}

		public async Task<TEntity> Insert(TEntity entity)
		{
			if (entity.ID == 0)
			{
				entity.ID = await GetNextID();
			}

			if (_data.TryGetValue(entity.ID, out TEntity exiting))
			{
				return exiting;
			}
			else
			{
				_data.Add(entity.ID, entity);
				return entity;
			}
		}

		public async Task<TEntity> Update(TEntity entityToUpdate)
		{
			return await Task.Run(() =>
			{
				if (_data.TryGetValue(entityToUpdate.ID, out TEntity exiting))
				{
					_data[entityToUpdate.ID] = entityToUpdate;
				}

				return entityToUpdate;
			});
		}

		protected async Task<int> GetNextID()
		{
			return await Task.Run(() =>
			{
				int next = 1;
				if (_data.Count() > 0)
				{
					next = _data.Keys.Max(k => k) + 1;
				}
				return next;
			});
		}
	}
}
