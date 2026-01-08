using Microsoft.EntityFrameworkCore;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Contexts;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Helpers;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Models;

namespace OakIdeas.GenericRepository.EntityFrameworkCore.Tests
{
    [TestClass]
	public class TypeSafeIncludeTests
	{
		private readonly string _customerName = "Test Customer";
		private readonly string _product1Name = "Product 1";
		private readonly string _product2Name = "Product 2";

        public TestContext TestContext { get; set; }

		[TestMethod]
		public async Task Get_WithSingleTypeSafeInclude_LoadsNavigationProperty()
		{
			// Arrange
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

            using var context = new InMemoryDataContext(options);
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

            // Create products
            var product1 = await productRepository.Insert(new Product { Name = _product1Name }, TestContext.CancellationToken);
            var product2 = await productRepository.Insert(new Product { Name = _product2Name }, TestContext.CancellationToken);

            // Create customer with products
            var customer = new Customer() { Name = _customerName };
            customer.Products.Add(product1);
            customer.Products.Add(product2);
            await repository.Insert(customer, TestContext.CancellationToken);

            // Clear the context to ensure navigation properties aren't auto-loaded
            context.ChangeTracker.Clear();

            // Act - Use type-safe include
            var customers = await repository.Get(
                filter: c => c.Name == _customerName,
                includeExpressions: c => c.Products,
                cancellationToken: TestContext.CancellationToken
            );

            // Assert
            var retrievedCustomer = customers.FirstOrDefault();
            Assert.IsNotNull(retrievedCustomer);
            Assert.AreEqual(_customerName, retrievedCustomer.Name);
            Assert.IsNotNull(retrievedCustomer.Products);
            Assert.HasCount(2, retrievedCustomer.Products);
            CollectionAssert.Contains(retrievedCustomer.Products.Select(p => p.Name).ToList(), _product1Name);
            CollectionAssert.Contains(retrievedCustomer.Products.Select(p => p.Name).ToList(), _product2Name);
        }

		[TestMethod]
		public async Task Get_WithoutTypeSafeInclude_DoesNotLoadNavigationProperty()
		{
			// Arrange
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

            using var context = new InMemoryDataContext(options);
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

            // Create products
            var product = await productRepository.Insert(new Product { Name = _product1Name }, TestContext.CancellationToken);

            // Create customer with product
            var customer = new Customer() { Name = _customerName };
            customer.Products.Add(product);
            await repository.Insert(customer, TestContext.CancellationToken);

            // Clear the context to ensure navigation properties aren't auto-loaded
            context.ChangeTracker.Clear();

            // Act - Don't use include
            var customers = await repository.Get(filter: c => c.Name == _customerName, cancellationToken: TestContext.CancellationToken);

            // Assert
            var retrievedCustomer = customers.FirstOrDefault();
            Assert.IsNotNull(retrievedCustomer);
            Assert.AreEqual(_customerName, retrievedCustomer.Name);
            // Navigation property should be empty/not loaded
            CollectionAssertEx.IsEmpty(retrievedCustomer.Products);
        }

		[TestMethod]
		public async Task Get_WithTypeSafeIncludeAndFilter_ReturnsFilteredWithIncludes()
		{
			// Arrange
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

            using var context = new InMemoryDataContext(options);
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

            // Create products
            var product = await productRepository.Insert(new Product { Name = _product1Name }, TestContext.CancellationToken);

            // Create two customers
            var customer1 = new Customer() { Name = "Customer 1" };
            customer1.Products.Add(product);
            await repository.Insert(customer1, TestContext.CancellationToken);

            var customer2 = new Customer() { Name = "Customer 2" };
            await repository.Insert(customer2, TestContext.CancellationToken);

            // Clear the context
            context.ChangeTracker.Clear();

            // Act - Filter and include
            var customers = await repository.Get(
                filter: c => c.Name == "Customer 1",
                includeExpressions: c => c.Products,
                cancellationToken: TestContext.CancellationToken
            );

            // Assert
            Assert.HasCount(1, customers.ToList());
            var retrievedCustomer = customers.FirstOrDefault();
            Assert.IsNotNull(retrievedCustomer);
            Assert.AreEqual("Customer 1", retrievedCustomer.Name);
            Assert.HasCount(1, retrievedCustomer.Products);
        }

		[TestMethod]
		public async Task Get_WithTypeSafeIncludeAndOrdering_ReturnsOrderedWithIncludes()
		{
			// Arrange
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

            using var context = new InMemoryDataContext(options);
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

            // Create products
            var product1 = await productRepository.Insert(new Product { Name = _product1Name }, TestContext.CancellationToken);
            var product2 = await productRepository.Insert(new Product { Name = _product2Name }, TestContext.CancellationToken);

            // Create customers
            var customerB = new Customer() { Name = "B Customer" };
            customerB.Products.Add(product1);
            await repository.Insert(customerB, TestContext.CancellationToken);

            var customerA = new Customer() { Name = "A Customer" };
            customerA.Products.Add(product2);
            await repository.Insert(customerA, TestContext.CancellationToken);

            // Clear the context
            context.ChangeTracker.Clear();

            // Act - Order and include
            var customers = await repository.Get(
                orderBy: q => q.OrderBy(c => c.Name),
                includeExpressions: c => c.Products,
                cancellationToken: TestContext.CancellationToken
            );

            // Assert
            var customerList = customers.ToList();
            Assert.HasCount(2, customerList);
            Assert.AreEqual("A Customer", customerList[0].Name);
            Assert.AreEqual("B Customer", customerList[1].Name);
            Assert.IsNotEmpty(customerList[0].Products);
            Assert.IsNotEmpty(customerList[1].Products);
        }

		[TestMethod]
		public async Task Get_WithEmptyTypeSafeIncludeArray_ReturnsWithoutIncludes()
		{
			// Arrange
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

            using var context = new InMemoryDataContext(options);
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

            // Create product and customer
            var product = await productRepository.Insert(new Product { Name = _product1Name }, TestContext.CancellationToken);
            var customer = new Customer() { Name = _customerName };
            customer.Products.Add(product);
            await repository.Insert(customer, TestContext.CancellationToken);

            // Clear the context
            context.ChangeTracker.Clear();

            // Act - Empty include array
            var customers = await repository.Get(cancellationToken: TestContext.CancellationToken);

            // Assert
            var retrievedCustomer = customers.FirstOrDefault();
            Assert.IsNotNull(retrievedCustomer);
            CollectionAssertEx.IsEmpty(retrievedCustomer.Products);
        }

		[TestMethod]
		public async Task Get_TypeSafeIncludeWithCancellationToken_RespectsToken()
		{
			// Arrange
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

            using var context = new InMemoryDataContext(options);
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var customer = new Customer() { Name = _customerName };
            await repository.Insert(customer, TestContext.CancellationToken);

            var cancellationToken = new System.Threading.CancellationToken(canceled: false);

            // Act & Assert - Should not throw
            var customers = await repository.Get(
               cancellationToken: TestContext.CancellationToken,
               includeExpressions: c => c.Products
            );

            Assert.IsNotNull(customers);
        }

		[TestMethod]
		public async Task Get_StringIncludeStillWorks_BackwardCompatibility()
		{
			// Arrange
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

            using var context = new InMemoryDataContext(options);
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

            // Create products
            var product1 = await productRepository.Insert(new Product { Name = _product1Name }, TestContext.CancellationToken);
            var product2 = await productRepository.Insert(new Product { Name = _product2Name }, TestContext.CancellationToken);

            // Create customer with products
            var customer = new Customer() { Name = _customerName };
            customer.Products.Add(product1);
            customer.Products.Add(product2);
            await repository.Insert(customer, TestContext.CancellationToken);

            // Clear the context
            context.ChangeTracker.Clear();

            // Act - Use old string-based include (backward compatibility)
            var customers = await repository.Get(
                filter: c => c.Name == _customerName,
                includeProperties: "Products",
                cancellationToken: TestContext.CancellationToken
            );

            // Assert
            var retrievedCustomer = customers.FirstOrDefault();
            Assert.IsNotNull(retrievedCustomer);
            Assert.AreEqual(_customerName, retrievedCustomer.Name);
            Assert.IsNotNull(retrievedCustomer.Products);
            Assert.HasCount(2, retrievedCustomer.Products);
        }

		[TestMethod]
		public async Task Get_TypeSafeIncludeWithNoFilter_LoadsAllWithIncludes()
		{
			// Arrange
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

            using var context = new InMemoryDataContext(options);
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

            // Create products
            var product = await productRepository.Insert(new Product { Name = _product1Name }, TestContext.CancellationToken);

            // Create customers
            var customer1 = new Customer() { Name = "Customer 1" };
            customer1.Products.Add(product);
            await repository.Insert(customer1, TestContext.CancellationToken);

            var customer2 = new Customer() { Name = "Customer 2" };
            await repository.Insert(customer2, TestContext.CancellationToken);

            // Clear the context
            context.ChangeTracker.Clear();

            // Act - No filter, just include
            var customers = await repository.Get(includeExpressions: c => c.Products, cancellationToken: TestContext.CancellationToken);

            // Assert
            Assert.AreEqual(2, customers.Count());
            var customerWithProduct = customers.FirstOrDefault(c => c.Name == "Customer 1");
            Assert.IsNotNull(customerWithProduct);
            Assert.HasCount(1, customerWithProduct.Products);
        }
	}
}
