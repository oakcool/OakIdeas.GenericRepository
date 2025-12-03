using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.Memory;
using OakIdeas.GenericRepository.Tests.Models;
using System;
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
			var repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			Assert.IsTrue(newEntity.ID > 0);
		}

		[TestMethod]
		public async Task InsertExisting_Entity()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			var existing = await repository.Insert(newEntity);
			Assert.IsTrue(newEntity == existing);
		}

		[TestMethod]
		public async Task GetNothing_Entity()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var existing = await repository.Get();
			Assert.IsTrue(existing.Count() == 0);
		}

		[TestMethod]
		public async Task GetByID_Entity()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			var existing = await repository.Get(newEntity.ID);
			Assert.IsNotNull(existing);
		}

		[TestMethod]
		public async Task GetByName_Entity()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			var existing = await repository.Get(x => x.Name == _entityDefaultName);
			Assert.IsNotNull(existing);
		}

		[TestMethod]
		public async Task GetOrdered_Entity()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new Customer() { Name = _entityNewName });
			var defaultEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			var ordered = await repository.Get(orderBy: (x => x.OrderBy(c => c.Name)));
			Assert.IsNotNull(ordered.First(c => c.Name == _entityDefaultName));
		}

		[TestMethod]
		public async Task Update_Entity()
		{

			var repository = new MemoryGenericRepository<Customer>();
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
			var repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			await repository.Delete(newEntity);
			var existing = await repository.Get(newEntity.ID);

			Assert.IsNull(existing);
		}

		[TestMethod]
		public async Task DeleteByID_Entity()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			await repository.Delete(newEntity.ID);
			var existing = await repository.Get(newEntity.ID);

			Assert.IsNull(existing);
		}

		// Error handling tests
		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task Insert_NullEntity_ThrowsException()
		{
			var repository = new MemoryGenericRepository<Customer>();
			await repository.Insert(null);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task Update_NullEntity_ThrowsException()
		{
			var repository = new MemoryGenericRepository<Customer>();
			await repository.Update(null);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task Delete_NullEntity_ThrowsException()
		{
			var repository = new MemoryGenericRepository<Customer>();
			await repository.Delete((Customer)null);
		}

		// Edge case tests
		[TestMethod]
		public async Task Update_NonExistentEntity_ReturnsEntity()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var entity = new Customer() { ID = 999, Name = _entityDefaultName };
			var result = await repository.Update(entity);
			Assert.AreEqual(entity, result);
		}

		[TestMethod]
		public async Task GetByID_NonExistentID_ReturnsNull()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var result = await repository.Get(999);
			Assert.IsNull(result);
		}

		[TestMethod]
		public async Task Delete_NonExistentEntity_ReturnsTrue()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var entity = new Customer() { ID = 999, Name = _entityDefaultName };
			var result = await repository.Delete(entity);
			Assert.IsTrue(result);
		}

		[TestMethod]
		public async Task DeleteByID_NonExistentID_ReturnsTrue()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var result = await repository.Delete(999);
			Assert.IsTrue(result);
		}

		[TestMethod]
		public async Task Get_WithFilter_NoMatches_ReturnsEmpty()
		{
			var repository = new MemoryGenericRepository<Customer>();
			await repository.Insert(new Customer() { Name = _entityDefaultName });
			var result = await repository.Get(x => x.Name == "NonExistent");
			Assert.AreEqual(0, result.Count());
		}

		[TestMethod]
		public async Task Get_MultipleEntities_ReturnsAll()
		{
			var repository = new MemoryGenericRepository<Customer>();
			await repository.Insert(new Customer() { Name = _entityDefaultName });
			await repository.Insert(new Customer() { Name = _entityNewName });
			await repository.Insert(new Customer() { Name = "Third Customer" });
			var result = await repository.Get();
			Assert.AreEqual(3, result.Count());
		}
	}
}
