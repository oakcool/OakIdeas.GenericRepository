using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository
{
	public abstract class GenericUnitOfWork<TStore> : IDisposable where TStore : IDisposable
	{
		protected TStore _store;
		private bool _disposed = false;
		protected Dictionary<Type, object> _repository = new Dictionary<Type, object>();

		public GenericUnitOfWork(TStore store)
		{
			_store = store;
		}

		public async Task<IGenericRepository<TEntity>> Repository<TEntity>() where TEntity : class
		{
			return await Task.Run(() =>
			{
				if (_repository.ContainsKey(typeof(TEntity)))
				{
					return (IGenericRepository<TEntity>)_repository[typeof(TEntity)];
				}

				return null;
			});
		}
		public async Task Register<TEntity>(IGenericRepository<TEntity> repository) where TEntity : class
		{
			await Task.Run(() =>
			{
				
				if (!_repository.ContainsKey(typeof(TEntity)))
				{
					_repository.Add(typeof(IGenericRepository<TEntity>),repository);
				}

				return null;
			});
		}
		public abstract Task Save();		

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_store.Dispose();
				}
			}
			_disposed = true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
