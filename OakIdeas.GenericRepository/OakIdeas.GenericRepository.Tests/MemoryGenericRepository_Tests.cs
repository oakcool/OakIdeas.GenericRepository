using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.Tests.Models;
using System.Linq;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Tests
{
	[TestClass]
	public class MemoryGenericRepository_Tests
	{
		private readonly string _entityDefaultName = "Default Customer";
		private readonly string _entityNewName = "New Name";

		[TestMethod]
		public async Task Insert_Entity()
		{
			MemoryGenericRepository<Customer> repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			Assert.IsTrue(newEntity.ID > 0);
		}

		[TestMethod]
		public async Task InsertExisting_Entity()
		{
			MemoryGenericRepository<Customer> repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			var existing = await repository.Insert(newEntity);
			Assert.IsTrue(newEntity == existing);
		}

		[TestMethod]
		public async Task GetNothing_Entity()
		{
			MemoryGenericRepository<Customer> repository = new MemoryGenericRepository<Customer>();			
			var existing = await repository.Get();
			Assert.IsTrue(existing.Count() == 0);
		}

		[TestMethod]
		public async Task GetByID_Entity()
		{
			MemoryGenericRepository<Customer> repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			var existing = await repository.Get(newEntity.ID);
			Assert.IsNotNull(existing);
		}

		[TestMethod]
		public async Task GetByName_Entity()
		{
			MemoryGenericRepository<Customer> repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			var existing = await repository.Get(x => x.Name == _entityDefaultName);
			Assert.IsNotNull(existing);
		}

		[TestMethod]
		public async Task GetOrdered_Entity()
		{
			MemoryGenericRepository<Customer> repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new Customer() { Name = _entityNewName });
			var defaultEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });			
			var ordered = await repository.Get(orderBy: (x => x.OrderBy(c => c.Name)));
			Assert.IsNotNull(ordered.First(c => c.Name == _entityDefaultName));
		}

		[TestMethod]
		public async Task Update_Entity()
		{
			
			MemoryGenericRepository<Customer> repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			var existing = await repository.Get(newEntity.ID);
			existing.Name = _entityNewName;
			await repository.Update(existing);
			var updated = await repository.Get(newEntity.ID);
			Assert.IsNotNull(updated);
		}

		[TestMethod]
		public async Task Delete_Entity()
		{			
			MemoryGenericRepository<Customer> repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			await repository.Delete(newEntity);
			var existing = await repository.Get(newEntity.ID);

			Assert.IsNull(existing);
		}

		[TestMethod]
		public async Task DeleteByID_Entity()
		{
			MemoryGenericRepository<Customer> repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			await repository.Delete(newEntity.ID);
			var existing = await repository.Get(newEntity.ID);

			Assert.IsNull(existing);
		}
	}
}
