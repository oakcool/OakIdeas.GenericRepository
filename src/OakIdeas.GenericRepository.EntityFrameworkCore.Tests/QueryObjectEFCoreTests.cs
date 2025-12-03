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
    public class QueryObjectEFCoreTests
    {
        private InMemoryDataContext CreateContext([System.Runtime.CompilerServices.CallerMemberName] string testName = "")
        {
            var options = new DbContextOptionsBuilder<InMemoryDataContext>()
                .UseInMemoryDatabase(databaseName: $"QueryObjectTests_{testName}_{Guid.NewGuid()}")
                .Options;

            return new InMemoryDataContext(options);
        }

        [TestMethod]
        public async Task GetWithQuery_WithFilter_ReturnsFiltered()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            await repository.Insert(new Customer { Name = "Active Customer" });
            await repository.Insert(new Customer { Name = "Inactive Customer" });
            await repository.Insert(new Customer { Name = "Active Person" });

            var query = new Query<Customer>()
                .Where(c => c.Name.StartsWith("Active"));

            // Act
            var results = await repository.Get(query);

            // Assert
            Assert.AreEqual(2, results.Count());
            Assert.IsTrue(results.All(c => c.Name.StartsWith("Active")));
        }

        [TestMethod]
        public async Task GetWithQuery_WithOrdering_ReturnsOrdered()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            await repository.Insert(new Customer { Name = "Charlie" });
            await repository.Insert(new Customer { Name = "Alice" });
            await repository.Insert(new Customer { Name = "Bob" });

            var query = new Query<Customer>()
                .Sort(q => q.OrderBy(c => c.Name));

            // Act
            var results = await repository.Get(query);

            // Assert
            var names = results.Select(c => c.Name).ToList();
            CollectionAssert.AreEqual(new[] { "Alice", "Bob", "Charlie" }, names);
        }

        [TestMethod]
        public async Task GetWithQuery_WithPagination_ReturnsPaginatedResults()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            
            for (int i = 1; i <= 25; i++)
            {
                await repository.Insert(new Customer { Name = $"Customer{i:D2}" });
            }

            var query = new Query<Customer>()
                .Sort(q => q.OrderBy(c => c.Name))
                .Paged(2, 10);

            // Act
            var results = await repository.Get(query);

            // Assert
            Assert.AreEqual(10, results.Count());
            // Second page should start at Customer11
            Assert.AreEqual("Customer11", results.First().Name);
            Assert.AreEqual("Customer20", results.Last().Name);
        }

        [TestMethod]
        public async Task GetWithQuery_WithInclude_LoadsNavigationProperties()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

            var customer = await repository.Insert(new Customer { Name = "Customer1" });
            var product = await productRepository.Insert(new Product { Name = "Product1" });
            customer.Products.Add(product);
            await context.SaveChangesAsync();

            var query = new Query<Customer>()
                .Include(c => c.Products);

            // Act
            var results = await repository.Get(query);
            var customerWithProducts = results.First();

            // Assert
            Assert.IsNotNull(customerWithProducts.Products);
            Assert.AreEqual(1, customerWithProducts.Products.Count);
            Assert.AreEqual("Product1", customerWithProducts.Products.First().Name);
        }

        [TestMethod]
        public async Task GetWithQuery_WithNoTracking_DoesNotTrackEntities()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            await repository.Insert(new Customer { Name = "Test Customer" });

            var query = new Query<Customer>()
                .WithNoTracking();

            // Act
            var results = await repository.Get(query);
            var customer = results.First();

            // Assert
            Assert.AreEqual(Microsoft.EntityFrameworkCore.EntityState.Detached, 
                context.Entry(customer).State);
        }

        [TestMethod]
        public async Task GetWithQuery_ComplexQuery_Works()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);

            for (int i = 1; i <= 50; i++)
            {
                await repository.Insert(new Customer
                {
                    Name = $"Customer{i:D2}"
                });
            }

            var query = new Query<Customer>()
                .Where(c => c.ID > 25)
                .Sort(q => q.OrderByDescending(c => c.Name))
                .Paged(2, 5)
                .WithNoTracking();

            // Act
            var results = await repository.Get(query);

            // Assert
            Assert.AreEqual(5, results.Count());
            Assert.IsTrue(results.All(c => c.ID > 25));
            
            // Check descending order
            var names = results.Select(c => c.Name).ToList();
            CollectionAssert.AreEqual(names.OrderByDescending(n => n).ToList(), names);

            // Verify entities are not tracked
            Assert.IsTrue(results.All(c => 
                context.Entry(c).State == Microsoft.EntityFrameworkCore.EntityState.Detached));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task GetWithQuery_NullQuery_ThrowsException()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);

            // Act
            await repository.Get((Query<Customer>)null!);
        }

        [TestMethod]
        public async Task GetWithQuery_EmptyQuery_ReturnsAll()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            await repository.Insert(new Customer { Name = "Customer1" });
            await repository.Insert(new Customer { Name = "Customer2" });
            await repository.Insert(new Customer { Name = "Customer3" });

            var query = new Query<Customer>();

            // Act
            var results = await repository.Get(query);

            // Assert
            Assert.AreEqual(3, results.Count());
        }

        [TestMethod]
        public async Task GetWithQuery_QueryReusability_Works()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);

            for (int i = 1; i <= 20; i++)
            {
                await repository.Insert(new Customer
                {
                    Name = $"Customer{i:D2}"
                });
            }

            // Create a reusable query - filter customers with ID > 10
            var filteredCustomersQuery = new Query<Customer>()
                .Where(c => c.ID > 10)
                .Sort(q => q.OrderBy(c => c.Name));

            // Act - Use the query multiple times
            var allFiltered = await repository.Get(filteredCustomersQuery);

            // Modify for pagination - first page
            filteredCustomersQuery.Paged(1, 5);
            var firstPage = await repository.Get(filteredCustomersQuery);

            // Second page
            filteredCustomersQuery.Paged(2, 5);
            var secondPage = await repository.Get(filteredCustomersQuery);

            // Assert
            Assert.AreEqual(10, allFiltered.Count()); // Customers 11-20
            Assert.AreEqual(5, firstPage.Count()); // First 5 filtered customers
            Assert.AreEqual(5, secondPage.Count()); // Next 5 filtered customers
            Assert.AreNotEqual(firstPage.First().Name, secondPage.First().Name);
        }

        [TestMethod]
        public async Task GetWithQuery_MultipleIncludes_LoadsAllNavigationProperties()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

            var customer = await repository.Insert(new Customer { Name = "Customer1" });
            var product1 = await productRepository.Insert(new Product { Name = "Product1" });
            var product2 = await productRepository.Insert(new Product { Name = "Product2" });
            customer.Products.Add(product1);
            customer.Products.Add(product2);
            await context.SaveChangesAsync();

            var query = new Query<Customer>()
                .Include(c => c.Products)
                .Where(c => c.Name == "Customer1");

            // Act
            var results = await repository.Get(query);
            var customerWithProducts = results.First();

            // Assert
            Assert.IsNotNull(customerWithProducts.Products);
            Assert.AreEqual(2, customerWithProducts.Products.Count);
        }

        [TestMethod]
        public async Task GetWithQuery_WithFilterAndPagination_AppliesFilterBeforePagination()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);

            for (int i = 1; i <= 30; i++)
            {
                await repository.Insert(new Customer
                {
                    Name = $"Customer{i:D2}"
                });
            }

            var query = new Query<Customer>()
                .Where(c => c.ID > 10) // Only customers 11-30
                .Sort(q => q.OrderBy(c => c.Name))
                .Paged(1, 10);

            // Act
            var results = await repository.Get(query);

            // Assert
            Assert.AreEqual(10, results.Count());
            // All should have ID > 10 (filtered)
            Assert.IsTrue(results.All(c => c.ID > 10));
            // First page of filtered customers should start at Customer11
            Assert.AreEqual("Customer11", results.First().Name);
        }
    }
}
