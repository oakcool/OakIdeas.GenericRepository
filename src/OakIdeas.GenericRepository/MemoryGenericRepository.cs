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
		readonly Dictionary<int, TEntity> _addData;
		readonly Dictionary<int, TEntity> _deleteData;
		readonly Dictionary<int, TEntity> _updateData;

		public MemoryGenericRepository()
		{
			_data = new Dictionary<int, TEntity>();
			_addData = new Dictionary<int, TEntity>();
			_deleteData = new Dictionary<int, TEntity>();
			_updateData = new Dictionary<int, TEntity>();
		}

		public async Task<bool> Delete(TEntity entityToDelete)
		{
			return await Task.Run(() =>
			{
				if (_data.TryGetValue(entityToDelete.ID, out TEntity exiting))
				{
					_deleteData.Add(entityToDelete.ID, entityToDelete);
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
					_deleteData.Add(@int, exiting);
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
				_addData.Add(entity.ID, entity);
				return entity;
			}
		}

		public async Task<TEntity> Update(TEntity entityToUpdate)
		{
			return await Task.Run(() =>
			{
				if (_data.TryGetValue(entityToUpdate.ID, out TEntity exiting))
				{

					if (_updateData.TryGetValue(entityToUpdate.ID, out TEntity alreadyUpdated))
					{
						_updateData[entityToUpdate.ID] = exiting;
					}
					else
					{
						_updateData.Add(entityToUpdate.ID, entityToUpdate);
					}
				}

				return entityToUpdate;
			});
		}

		public async Task<int> Save()
		{
			int total = 0;
			await Task.Factory.StartNew(() =>
			{				
				foreach (var entity in _addData.Values)
				{
					_data.Add(entity.ID, entity);
					total++;
				}
				Task.Factory.StartNew( () =>
				{
					foreach (var entity in _updateData.Values)
					{
						_data[entity.ID] = entity;
						total++;
					}

					Task.Factory.StartNew(() =>
					{
						foreach (var entity in _deleteData.Values)
						{
							_data.Remove(entity.ID);
							total++;
						}
					},  TaskCreationOptions.AttachedToParent);
				}, TaskCreationOptions.AttachedToParent);
			});
			return total;
		}

		protected async Task<int> GetNextID()
		{
			return await Task.Run(() =>
			{
				int next = 1;
				if (_addData.Count() > 0)
				{
					next = _addData.Keys.Max(k => k) + 1;
				} else  if (_data.Count() > 0)
				{
					next = _data.Keys.Max(k => k) + 1;
				}				
				return next;
			});
		}
	}
}
