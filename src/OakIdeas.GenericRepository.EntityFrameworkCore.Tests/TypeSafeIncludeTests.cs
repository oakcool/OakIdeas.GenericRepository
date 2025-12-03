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
	public class TypeSafeIncludeTests
	{
		private readonly string _customerName = "Test Customer";
		private readonly string _product1Name = "Product 1";
		private readonly string _product2Name = "Product 2";

		[TestMethod]
		public async Task Get_WithSingleTypeSafeInclude_LoadsNavigationProperty()
		{
			// Arrange
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

			using (var context = new InMemoryDataContext(options))
			{
				var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
				var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

				// Create products
				var product1 = await productRepository.Insert(new Product { Name = _product1Name });
				var product2 = await productRepository.Insert(new Product { Name = _product2Name });

				// Create customer with products
				var customer = new Customer { Name = _customerName };
				customer.Products.Add(product1);
				customer.Products.Add(product2);
				await repository.Insert(customer);

				// Clear the context to ensure navigation properties aren't auto-loaded
				context.ChangeTracker.Clear();

				// Act - Use type-safe include
				var customers = await repository.Get(
					filter: c => c.Name == _customerName,
					includeExpressions: c => c.Products
				);

				// Assert
				var retrievedCustomer = customers.FirstOrDefault();
				Assert.IsNotNull(retrievedCustomer);
				Assert.AreEqual(_customerName, retrievedCustomer.Name);
				Assert.IsNotNull(retrievedCustomer.Products);
				Assert.AreEqual(2, retrievedCustomer.Products.Count);
				Assert.IsTrue(retrievedCustomer.Products.Any(p => p.Name == _product1Name));
				Assert.IsTrue(retrievedCustomer.Products.Any(p => p.Name == _product2Name));
			}
		}

		[TestMethod]
		public async Task Get_WithoutTypeSafeInclude_DoesNotLoadNavigationProperty()
		{
			// Arrange
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

			using (var context = new InMemoryDataContext(options))
			{
				var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
				var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

				// Create products
				var product = await productRepository.Insert(new Product { Name = _product1Name });

				// Create customer with product
				var customer = new Customer { Name = _customerName };
				customer.Products.Add(product);
				await repository.Insert(customer);

				// Clear the context to ensure navigation properties aren't auto-loaded
				context.ChangeTracker.Clear();

				// Act - Don't use include
				var customers = await repository.Get(filter: c => c.Name == _customerName);

				// Assert
				var retrievedCustomer = customers.FirstOrDefault();
				Assert.IsNotNull(retrievedCustomer);
				Assert.AreEqual(_customerName, retrievedCustomer.Name);
				// Navigation property should be empty/not loaded
				Assert.AreEqual(0, retrievedCustomer.Products.Count);
			}
		}

		[TestMethod]
		public async Task Get_WithTypeSafeIncludeAndFilter_ReturnsFilteredWithIncludes()
		{
			// Arrange
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

			using (var context = new InMemoryDataContext(options))
			{
				var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
				var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

				// Create products
				var product = await productRepository.Insert(new Product { Name = _product1Name });

				// Create two customers
				var customer1 = new Customer { Name = "Customer 1" };
				customer1.Products.Add(product);
				await repository.Insert(customer1);

				var customer2 = new Customer { Name = "Customer 2" };
				await repository.Insert(customer2);

				// Clear the context
				context.ChangeTracker.Clear();

				// Act - Filter and include
				var customers = await repository.Get(
					filter: c => c.Name == "Customer 1",
					includeExpressions: c => c.Products
				);

				// Assert
				Assert.AreEqual(1, customers.Count());
				var retrievedCustomer = customers.FirstOrDefault();
				Assert.IsNotNull(retrievedCustomer);
				Assert.AreEqual("Customer 1", retrievedCustomer.Name);
				Assert.AreEqual(1, retrievedCustomer.Products.Count);
			}
		}

		[TestMethod]
		public async Task Get_WithTypeSafeIncludeAndOrdering_ReturnsOrderedWithIncludes()
		{
			// Arrange
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

			using (var context = new InMemoryDataContext(options))
			{
				var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
				var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

				// Create products
				var product1 = await productRepository.Insert(new Product { Name = _product1Name });
				var product2 = await productRepository.Insert(new Product { Name = _product2Name });

				// Create customers
				var customerB = new Customer { Name = "B Customer" };
				customerB.Products.Add(product1);
				await repository.Insert(customerB);

				var customerA = new Customer { Name = "A Customer" };
				customerA.Products.Add(product2);
				await repository.Insert(customerA);

				// Clear the context
				context.ChangeTracker.Clear();

				// Act - Order and include
				var customers = await repository.Get(
					orderBy: q => q.OrderBy(c => c.Name),
					includeExpressions: c => c.Products
				);

				// Assert
				var customerList = customers.ToList();
				Assert.AreEqual(2, customerList.Count);
				Assert.AreEqual("A Customer", customerList[0].Name);
				Assert.AreEqual("B Customer", customerList[1].Name);
				Assert.IsTrue(customerList[0].Products.Count > 0);
				Assert.IsTrue(customerList[1].Products.Count > 0);
			}
		}

		[TestMethod]
		public async Task Get_WithEmptyTypeSafeIncludeArray_ReturnsWithoutIncludes()
		{
			// Arrange
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

			using (var context = new InMemoryDataContext(options))
			{
				var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
				var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

				// Create product and customer
				var product = await productRepository.Insert(new Product { Name = _product1Name });
				var customer = new Customer { Name = _customerName };
				customer.Products.Add(product);
				await repository.Insert(customer);

				// Clear the context
				context.ChangeTracker.Clear();

				// Act - Empty include array
				var customers = await repository.Get();

				// Assert
				var retrievedCustomer = customers.FirstOrDefault();
				Assert.IsNotNull(retrievedCustomer);
				Assert.AreEqual(0, retrievedCustomer.Products.Count);
			}
		}

		[TestMethod]
		public async Task Get_TypeSafeIncludeWithCancellationToken_RespectsToken()
		{
			// Arrange
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

			using (var context = new InMemoryDataContext(options))
			{
				var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
				var customer = new Customer { Name = _customerName };
				await repository.Insert(customer);

				var cancellationToken = new System.Threading.CancellationToken(canceled: false);

				// Act & Assert - Should not throw
				var customers = await repository.Get(
					cancellationToken: cancellationToken,
					includeExpressions: c => c.Products
				);

				Assert.IsNotNull(customers);
			}
		}

		[TestMethod]
		public async Task Get_StringIncludeStillWorks_BackwardCompatibility()
		{
			// Arrange
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

			using (var context = new InMemoryDataContext(options))
			{
				var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
				var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

				// Create products
				var product1 = await productRepository.Insert(new Product { Name = _product1Name });
				var product2 = await productRepository.Insert(new Product { Name = _product2Name });

				// Create customer with products
				var customer = new Customer { Name = _customerName };
				customer.Products.Add(product1);
				customer.Products.Add(product2);
				await repository.Insert(customer);

				// Clear the context
				context.ChangeTracker.Clear();

				// Act - Use old string-based include (backward compatibility)
				var customers = await repository.Get(
					filter: c => c.Name == _customerName,
					includeProperties: "Products"
				);

				// Assert
				var retrievedCustomer = customers.FirstOrDefault();
				Assert.IsNotNull(retrievedCustomer);
				Assert.AreEqual(_customerName, retrievedCustomer.Name);
				Assert.IsNotNull(retrievedCustomer.Products);
				Assert.AreEqual(2, retrievedCustomer.Products.Count);
			}
		}

		[TestMethod]
		public async Task Get_TypeSafeIncludeWithNoFilter_LoadsAllWithIncludes()
		{
			// Arrange
			var uniqueDbName = $"CustomerDB_{Guid.NewGuid()}";
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(uniqueDbName)
				.Options;

			using (var context = new InMemoryDataContext(options))
			{
				var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
				var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

				// Create products
				var product = await productRepository.Insert(new Product { Name = _product1Name });

				// Create customers
				var customer1 = new Customer { Name = "Customer 1" };
				customer1.Products.Add(product);
				await repository.Insert(customer1);

				var customer2 = new Customer { Name = "Customer 2" };
				await repository.Insert(customer2);

				// Clear the context
				context.ChangeTracker.Clear();

				// Act - No filter, just include
				var customers = await repository.Get(includeExpressions: c => c.Products);

				// Assert
				Assert.AreEqual(2, customers.Count());
				var customerWithProduct = customers.FirstOrDefault(c => c.Name == "Customer 1");
				Assert.IsNotNull(customerWithProduct);
				Assert.AreEqual(1, customerWithProduct.Products.Count);
			}
		}
	}
}
