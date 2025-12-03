using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.Tests.Models;
using System.Linq;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Tests
{
	[TestClass]
	public class MemoryGenericRepository_Long_Tests
	{
		private readonly string _entityDefaultName = "Default Customer";
		private readonly string _entityNewName = "New Name";

		[TestMethod]
		public async Task Insert_Entity()
		{
			var repository = new MemoryGenericRepository<CustomerLong, long>();
			var id = 1000000000000L;
			var newEntity = await repository.Insert(new CustomerLong() { ID = id, Name = _entityDefaultName });
			Assert.AreEqual(id, newEntity.ID);
		}

		[TestMethod]
		public async Task GetByID_Entity()
		{
			var repository = new MemoryGenericRepository<CustomerLong, long>();
			var id = 1000000000000L;
			var newEntity = await repository.Insert(new CustomerLong() { ID = id, Name = _entityDefaultName });
			var existing = await repository.Get(newEntity.ID);
			Assert.IsNotNull(existing);
			Assert.AreEqual(id, existing.ID);
		}

		[TestMethod]
		public async Task GetByName_Entity()
		{
			var repository = new MemoryGenericRepository<CustomerLong, long>();
			var id = 1000000000000L;
			var newEntity = await repository.Insert(new CustomerLong() { ID = id, Name = _entityDefaultName });
			var existing = await repository.Get(x => x.Name == _entityDefaultName);
			Assert.IsNotNull(existing);
			Assert.AreEqual(1, existing.Count());
		}

		[TestMethod]
		public async Task Update_Entity()
		{
			var repository = new MemoryGenericRepository<CustomerLong, long>();
			var id = 1000000000000L;
			var newEntity = await repository.Insert(new CustomerLong() { ID = id, Name = _entityDefaultName });
			var existing = await repository.Get(newEntity.ID);
			existing.Name = _entityNewName;
			await repository.Update(existing);
			var updated = await repository.Get(newEntity.ID);
			Assert.IsNotNull(updated);
			Assert.AreEqual(_entityNewName, updated.Name);
		}

		[TestMethod]
		public async Task Delete_Entity()
		{
			var repository = new MemoryGenericRepository<CustomerLong, long>();
			var id = 1000000000000L;
			var newEntity = await repository.Insert(new CustomerLong() { ID = id, Name = _entityDefaultName });
			await repository.Delete(newEntity);
			var existing = await repository.Get(newEntity.ID);

			Assert.IsNull(existing);
		}

		[TestMethod]
		public async Task DeleteByID_Entity()
		{
			var repository = new MemoryGenericRepository<CustomerLong, long>();
			var id = 1000000000000L;
			var newEntity = await repository.Insert(new CustomerLong() { ID = id, Name = _entityDefaultName });
			await repository.Delete(newEntity.ID);
			var existing = await repository.Get(newEntity.ID);

			Assert.IsNull(existing);
		}

		[TestMethod]
		public async Task GetByID_NonExistentID_ReturnsNull()
		{
			var repository = new MemoryGenericRepository<CustomerLong, long>();
			var result = await repository.Get(999999999L);
			Assert.IsNull(result);
		}

		[TestMethod]
		public async Task Get_MultipleEntities_ReturnsAll()
		{
			var repository = new MemoryGenericRepository<CustomerLong, long>();
			await repository.Insert(new CustomerLong() { ID = 1000000000000L, Name = _entityDefaultName });
			await repository.Insert(new CustomerLong() { ID = 2000000000000L, Name = _entityNewName });
			await repository.Insert(new CustomerLong() { ID = 3000000000000L, Name = "Third Customer" });
			var result = await repository.Get();
			Assert.AreEqual(3, result.Count());
		}
	}
}
