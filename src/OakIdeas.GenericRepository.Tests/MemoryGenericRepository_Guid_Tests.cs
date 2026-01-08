using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.Memory;
using OakIdeas.GenericRepository.Tests.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Tests
{
    [TestClass]
	public class MemoryGenericRepository_Guid_Tests
	{
		private readonly string _entityDefaultName = "Default Customer";
		private readonly string _entityNewName = "New Name";

        public TestContext TestContext { get; set; }

		[TestMethod]
		public async Task Insert_Entity()
		{
			var repository = new MemoryGenericRepository<CustomerGuid, Guid>();
			var id = Guid.NewGuid();
			var newEntity = await repository.Insert(new() { ID = id, Name = _entityDefaultName }, TestContext.CancellationToken);
			Assert.AreEqual(id, newEntity.ID);
		}

		[TestMethod]
		public async Task GetByID_Entity()
		{
			var repository = new MemoryGenericRepository<CustomerGuid, Guid>();
			var id = Guid.NewGuid();
			var newEntity = await repository.Insert(new() { ID = id, Name = _entityDefaultName }, TestContext.CancellationToken);
			var existing = await repository.Get(newEntity.ID, TestContext.CancellationToken);
			Assert.IsNotNull(existing);
			Assert.AreEqual(id, existing.ID);
		}

		[TestMethod]
		public async Task GetByName_Entity()
		{
			var repository = new MemoryGenericRepository<CustomerGuid, Guid>();
			var id = Guid.NewGuid();
			var newEntity = await repository.Insert(new() { ID = id, Name = _entityDefaultName }, TestContext.CancellationToken);
			var existing = await repository.Get(x => x.Name == _entityDefaultName, cancellationToken: TestContext.CancellationToken);
			Assert.IsNotNull(existing);
			Assert.AreEqual(1, existing.Count());
		}

		[TestMethod]
		public async Task Update_Entity()
		{
			var repository = new MemoryGenericRepository<CustomerGuid, Guid>();
			var id = Guid.NewGuid();
			var newEntity = await repository.Insert(new() { ID = id, Name = _entityDefaultName }, TestContext.CancellationToken);
			var existing = await repository.Get(newEntity.ID, TestContext.CancellationToken);
			if (existing != null)
			{
				existing.Name = _entityNewName;
				await repository.Update(existing, TestContext.CancellationToken);
			}
			var updated = await repository.Get(newEntity.ID, TestContext.CancellationToken);
			Assert.IsNotNull(updated);
			Assert.AreEqual(_entityNewName, updated!.Name);
		}

		[TestMethod]
		public async Task Delete_Entity()
		{
			var repository = new MemoryGenericRepository<CustomerGuid, Guid>();
			var id = Guid.NewGuid();
			var newEntity = await repository.Insert(new() { ID = id, Name = _entityDefaultName }, TestContext.CancellationToken);
			await repository.Delete(newEntity, TestContext.CancellationToken);
			var existing = await repository.Get(newEntity.ID, TestContext.CancellationToken);

			Assert.IsNull(existing);
		}

		[TestMethod]
		public async Task DeleteByID_Entity()
		{
			var repository = new MemoryGenericRepository<CustomerGuid, Guid>();
			var id = Guid.NewGuid();
			var newEntity = await repository.Insert(new() { ID = id, Name = _entityDefaultName }, TestContext.CancellationToken);
			await repository.Delete(newEntity.ID, TestContext.CancellationToken);
			var existing = await repository.Get(newEntity.ID, TestContext.CancellationToken);

			Assert.IsNull(existing);
		}

		[TestMethod]
		public async Task GetByID_NonExistentID_ReturnsNull()
		{
			var repository = new MemoryGenericRepository<CustomerGuid, Guid>();
			var result = await repository.Get(Guid.NewGuid(), TestContext.CancellationToken);
			Assert.IsNull(result);
		}

		[TestMethod]
		public async Task Get_MultipleEntities_ReturnsAll()
		{
			var repository = new MemoryGenericRepository<CustomerGuid, Guid>();
			await repository.Insert(new() { ID = Guid.NewGuid(), Name = _entityDefaultName }, TestContext.CancellationToken);
			await repository.Insert(new() { ID = Guid.NewGuid(), Name = _entityNewName }, TestContext.CancellationToken);
			await repository.Insert(new() { ID = Guid.NewGuid(), Name = "Third Customer" }, TestContext.CancellationToken);
			var result = await repository.Get(cancellationToken: TestContext.CancellationToken);
			Assert.AreEqual(3, result.Count());
		}
	}
}
