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

        public TestContext TestContext { get; set; }

		[TestMethod]
		public async Task Insert_Entity()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
            Assert.IsGreaterThan(0, newEntity.ID, "ID should be greater than 0");
		}

		[TestMethod]
		public async Task InsertExisting_Entity()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
			var existing = await repository.Insert(newEntity, TestContext.CancellationToken);
            Assert.AreSame(newEntity, existing);
		}

		[TestMethod]
		public async Task GetNothing_Entity()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var existing = await repository.Get(cancellationToken: TestContext.CancellationToken);
            Assert.AreEqual(0, existing.Count());
		}

		[TestMethod]
		public async Task GetByID_Entity()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
			var existing = await repository.Get(newEntity.ID, TestContext.CancellationToken);
			Assert.IsNotNull(existing);
		}

		[TestMethod]
		public async Task GetByName_Entity()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
			var existing = await repository.Get(filter: x => x.Name == _entityDefaultName, cancellationToken: TestContext.CancellationToken);
			Assert.IsNotNull(existing);
		}

		[TestMethod]
		public async Task GetOrdered_Entity()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new() { Name = _entityNewName }, TestContext.CancellationToken);
            var defaultEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
            var ordered = await repository.Get(orderBy: x => x.OrderBy(c => c.Name), cancellationToken: TestContext.CancellationToken);
            Assert.IsNotNull(ordered.FirstOrDefault(c => c.Name == _entityDefaultName));
		}

		[TestMethod]
		public async Task Update_Entity()
		{

            var repository = new MemoryGenericRepository<Customer>();
            var newEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
            var existing = await repository.Get(newEntity.ID, TestContext.CancellationToken);
            Assert.IsNotNull(existing);
            if (existing != null)
            {
                existing.Name = _entityNewName;
                await repository.Update(existing, TestContext.CancellationToken);
            }
            var updated = await repository.Get(newEntity.ID, TestContext.CancellationToken);
            Assert.IsNotNull(updated);
		}

		[TestMethod]
		public async Task Delete_Entity()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
			await repository.Delete(newEntity, TestContext.CancellationToken);
			var existing = await repository.Get(newEntity.ID, TestContext.CancellationToken);

			Assert.IsNull(existing);
		}

		[TestMethod]
		public async Task DeleteByID_Entity()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var newEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
			await repository.Delete(newEntity.ID, TestContext.CancellationToken);
			var existing = await repository.Get(newEntity.ID, TestContext.CancellationToken);

			Assert.IsNull(existing);
		}

		// Error handling tests
		[TestMethod]
		public async Task Insert_NullEntity_ThrowsException()
		{
			var repository = new MemoryGenericRepository<Customer>();
            try
            {
                await repository.Insert(null!, TestContext.CancellationToken);
                Assert.Fail("Expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
		}

		[TestMethod]
		public async Task Update_NullEntity_ThrowsException()
		{
			var repository = new MemoryGenericRepository<Customer>();
            try
            {
                await repository.Update(null!, TestContext.CancellationToken);
                Assert.Fail("Expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
		}

		[TestMethod]
		public async Task Delete_NullEntity_ThrowsException()
		{
			var repository = new MemoryGenericRepository<Customer>();
            try
            {
                await repository.Delete((Customer)null!, TestContext.CancellationToken);
                Assert.Fail("Expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
		}

		// Edge case tests
		[TestMethod]
		public async Task Update_NonExistentEntity_ReturnsEntity()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var entity = new Customer() { ID = 999, Name = _entityDefaultName };
			var result = await repository.Update(entity, TestContext.CancellationToken);
			Assert.AreEqual(entity, result);
		}

		[TestMethod]
		public async Task GetByID_NonExistentID_ReturnsNull()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var result = await repository.Get(999, TestContext.CancellationToken);
			Assert.IsNull(result);
		}

		[TestMethod]
		public async Task Delete_NonExistentEntity_ReturnsTrue()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var entity = new Customer() { ID = 999, Name = _entityDefaultName };
			var result = await repository.Delete(entity, TestContext.CancellationToken);
			Assert.IsTrue(result);
		}

		[TestMethod]
		public async Task DeleteByID_NonExistentID_ReturnsTrue()
		{
			var repository = new MemoryGenericRepository<Customer>();
			var result = await repository.Delete(999, TestContext.CancellationToken);
			Assert.IsTrue(result);
		}

		[TestMethod]
		public async Task Get_WithFilter_NoMatches_ReturnsEmpty()
		{
			var repository = new MemoryGenericRepository<Customer>();
			await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
			var result = await repository.Get(filter: x => x.Name == "NonExistent", cancellationToken: TestContext.CancellationToken);
			Assert.AreEqual(0, result.Count());
		}

		[TestMethod]
		public async Task Get_MultipleEntities_ReturnsAll()
		{
			var repository = new MemoryGenericRepository<Customer>();
			await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
			await repository.Insert(new() { Name = _entityNewName }, TestContext.CancellationToken);
			await repository.Insert(new() { Name = "Third Customer" }, TestContext.CancellationToken);
			var result = await repository.Get(cancellationToken: TestContext.CancellationToken);
			Assert.AreEqual(3, result.Count());
		}
	}
}
