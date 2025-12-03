using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Contexts;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.EntityFrameworkCore.Tests
{
	[TestClass]
	public class EntityFrameworkCoreRepository
	{
		private readonly string _entityDefaultName = "Default Customer";
		private readonly string _entityNewName = "New Name";
		private readonly string _productName = "Some Product";

		[TestMethod]
		public async Task Insert_Entity()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			Assert.IsTrue(newEntity.ID > 0);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public async Task InsertExisting_Entity()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			var existing = await repository.Insert(newEntity);			
		}

		[TestMethod]
		public async Task GetByID_Entity()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			var existing = await repository.Get(newEntity.ID);
			Assert.IsNotNull(existing);
		}

		[TestMethod]
		public async Task GetByName_Entity()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			var existing = await repository.Get(x => x.Name == _entityDefaultName);
			Assert.IsNotNull(existing);
		}

		[TestMethod]
		public async Task GetWithProperties_Entity()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);
			var newProductEntity = await productRepository.Insert(new Product() { Name = _productName });
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			newEntity.Products.Add(newProductEntity);
			var updatedEntity = await repository.Update(newEntity);
			var existing = await repository.Get(includeProperties: "Products");
			Assert.IsNotNull(existing);
		}

		[TestMethod]
		public async Task GetOrdered_Entity()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var newEntity = await repository.Insert(new Customer() { Name = _entityNewName });
			var defaultEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			var ordered = await repository.Get(orderBy: (x => x.OrderBy(c => c.Name)));
			Assert.IsNotNull(ordered.First(c => c.Name == _entityDefaultName));
		}

		[TestMethod]
		public async Task Update_Entity()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
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
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var newEntity = await repository.Insert(new Customer() { Name = _entityDefaultName });
			await repository.Delete(newEntity);
			var existing = await repository.Get(newEntity.ID);

			Assert.IsNull(existing);
		}

		[TestMethod]
		public async Task DeleteByID_Entity()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
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
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			await repository.Insert(null);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task Update_NullEntity_ThrowsException()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			await repository.Update(null);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task Delete_NullEntity_ThrowsException()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			await repository.Delete((Customer)null);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task DeleteByID_NullID_ThrowsException()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			await repository.Delete((object)null);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task GetByID_NullID_ThrowsException()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			await repository.Get((object)null);
		}

		// Edge case tests
		[TestMethod]
		public async Task GetByID_NonExistentID_ReturnsNull()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var result = await repository.Get(999);
			Assert.IsNull(result);
		}

		[TestMethod]
		public async Task Get_WithFilter_NoMatches_ReturnsEmpty()
		{
			var context = new InMemoryDataContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			await repository.Insert(new Customer() { Name = _entityDefaultName });
			var result = await repository.Get(x => x.Name == "NonExistent");
			Assert.AreEqual(0, result.Count());
		}

		[TestMethod]
		public async Task Get_MultipleEntities_ReturnsAll()
		{
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;
			var context = new InMemoryDataContext(options);
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			await repository.Insert(new Customer() { Name = _entityDefaultName });
			await repository.Insert(new Customer() { Name = _entityNewName });
			await repository.Insert(new Customer() { Name = "Third Customer" });
			var result = await repository.Get();
			Assert.AreEqual(3, result.Count());
		}
	}
}
