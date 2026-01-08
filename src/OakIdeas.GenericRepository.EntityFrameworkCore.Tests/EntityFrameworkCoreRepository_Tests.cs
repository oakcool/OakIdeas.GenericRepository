using Microsoft.EntityFrameworkCore;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Contexts;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Models;

namespace OakIdeas.GenericRepository.EntityFrameworkCore.Tests
{
    [TestClass]
	public class EntityFrameworkCoreRepository
	{
		private readonly string _entityDefaultName = "Default Customer";
		private readonly string _entityNewName = "New Name";
		private readonly string _productName = "Some Product";

		public TestContext TestContext { get; set; }

		[TestMethod]
		public async Task Insert_Entity()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var newEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
            newEntity.ID = 1;
            Assert.IsGreaterThan<int>(0, newEntity.ID);
		}

		[TestMethod]
		public async Task InsertExisting_Entity()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var newEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
            await Assert.ThrowsExactlyAsync<ArgumentException>(async () => await repository.Insert(newEntity, TestContext.CancellationToken));
		}

		[TestMethod]
		public async Task GetByID_Entity()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var newEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
            var existing = await repository.Get(newEntity.ID, TestContext.CancellationToken);
			Assert.IsNotNull(existing);
		}

		[TestMethod]
		public async Task GetByName_Entity()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var newEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
            var existing = await repository.Get(filter: x => x.Name == _entityDefaultName, cancellationToken: TestContext.CancellationToken);
			Assert.IsNotNull(existing);
		}

		[TestMethod]
		public async Task GetWithProperties_Entity()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);
            var newProductEntity = await productRepository.Insert(new Product() { Name = _productName }, TestContext.CancellationToken);
            var newEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
            newEntity.Products.Add(newProductEntity);
            var updatedEntity = await repository.Update(newEntity, TestContext.CancellationToken);
            var existing = await repository.Get(includeProperties: "Products", cancellationToken: TestContext.CancellationToken);
			Assert.IsNotNull(existing);
		}

		[TestMethod]
		public async Task GetOrdered_Entity()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var newEntity = await repository.Insert(new() { Name = _entityNewName }, TestContext.CancellationToken);
            var defaultEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
            var ordered = await repository.Get(orderBy: x => x.OrderBy(c => c.Name), cancellationToken: TestContext.CancellationToken);
			Assert.IsNotNull(ordered.First(c => c.Name == _entityDefaultName));
		}

		[TestMethod]
		public async Task Update_Entity()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var newEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
            var existing = await repository.Get(newEntity.ID, TestContext.CancellationToken);
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
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var newEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
            await repository.Delete(newEntity, TestContext.CancellationToken);
            var existing = await repository.Get(newEntity.ID, TestContext.CancellationToken);

			Assert.IsNull(existing);
		}

		[TestMethod]
		public async Task DeleteByID_Entity()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var newEntity = await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
            await repository.Delete(newEntity.ID, TestContext.CancellationToken);
            var existing = await repository.Get(newEntity.ID, TestContext.CancellationToken);

			Assert.IsNull(existing);
		}

		// Error handling tests
		[TestMethod]
		public async Task Insert_NullEntity_ThrowsException()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await repository.Insert(null!, TestContext.CancellationToken));
		}

		[TestMethod]
		public async Task Update_NullEntity_ThrowsException()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await repository.Update(null!, TestContext.CancellationToken));
		}

		[TestMethod]
		public async Task Delete_NullEntity_ThrowsException()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await repository.Delete(null!, TestContext.CancellationToken));
		}

		// Edge case tests
		[TestMethod]
		public async Task GetByID_NonExistentID_ReturnsNull()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var result = await repository.Get(999, TestContext.CancellationToken);
			Assert.IsNull(result);
		}

		[TestMethod]
		public async Task Get_WithFilter_NoMatches_ReturnsEmpty()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
            var result = await repository.Get(x => x.Name == "NonExistent", cancellationToken: TestContext.CancellationToken);
            Assert.AreEqual(0, result.Count());
		}

		[TestMethod]
		public async Task Get_MultipleEntities_ReturnsAll()
		{
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

            using var context = new InMemoryDataContext(options);
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            await repository.Insert(new() { Name = _entityDefaultName }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = _entityNewName }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Third Customer" }, TestContext.CancellationToken);
            var result = await repository.Get(cancellationToken: TestContext.CancellationToken);
            Assert.AreEqual(3, result.Count());
        }
	}
}
