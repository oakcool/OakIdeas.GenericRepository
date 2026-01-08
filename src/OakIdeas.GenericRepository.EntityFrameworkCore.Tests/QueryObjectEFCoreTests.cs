using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Contexts;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Helpers;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.EntityFrameworkCore.Tests
{
    [TestClass]
    public class QueryObjectEFCoreTests
    {
        public TestContext TestContext { get; set; }

        private static readonly string[] expected = ["Alice", "Bob", "Charlie"];

        private static InMemoryDataContext CreateContext([System.Runtime.CompilerServices.CallerMemberName] string testName = "")
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
            await repository.Insert(new() { Name = "Active Customer" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Inactive Customer" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Active Person" }, TestContext.CancellationToken);

            var query = new Query<Customer>()
                .Where(c => c.Name.StartsWith("Active"));

            // Act
            var results = await repository.Get(query, cancellationToken: TestContext.CancellationToken);

            // Assert
            CollectionAssertEx.HasCount(results, 2);
            Assert.IsTrue(results.All(c => c.Name.StartsWith("Active")));
        }

        [TestMethod]
        public async Task GetWithQuery_WithOrdering_ReturnsOrdered()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            await repository.Insert(new() { Name = "Charlie" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Alice" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Bob" }, TestContext.CancellationToken);

            var query = new Query<Customer>()
                .Sort(q => q.OrderBy(c => c.Name));

            // Act
            var results = await repository.Get(query, cancellationToken: TestContext.CancellationToken);

            // Assert
            var names = results.Select(c => c.Name).ToList();
            CollectionAssert.AreEqual(expected, names);
        }

        [TestMethod]
        public async Task GetWithQuery_WithPagination_ReturnsPaginatedResults()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            
            for (int i = 1; i <= 25; i++)
            {
                await repository.Insert(new() { Name = $"Customer{i:D2}" }, TestContext.CancellationToken);
            }

            var query = new Query<Customer>()
                .Sort(q => q.OrderBy(c => c.Name))
                .Paged(2, 10);

            // Act
            var results = await repository.Get(query, cancellationToken: TestContext.CancellationToken);

            // Assert
            CollectionAssertEx.HasCount(results, 10);
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

            var customer = await repository.Insert(new() { Name = "Customer1" }, TestContext.CancellationToken);
            var product = await productRepository.Insert(new Product { Name = "Product1" }, TestContext.CancellationToken);
            customer.Products.Add(product);
            await context.SaveChangesAsync(TestContext.CancellationToken);

            var query = new Query<Customer>()
                .Include(c => c.Products);

            // Act
            var results = await repository.Get(query, cancellationToken: TestContext.CancellationToken);
            var customerWithProducts = results.First();

            // Assert
            Assert.IsNotNull(customerWithProducts.Products, "Products should not be null.");
            Assert.HasCount(1, customerWithProducts.Products, "There should be one product associated with the customer.");
            Assert.AreEqual("Product1", customerWithProducts.Products.First().Name, "The product name should be Product1.");
        }

        [TestMethod]
        public async Task GetWithQuery_WithNoTracking_DoesNotTrackEntities()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            await repository.Insert(new() { Name = "Test Customer" }, TestContext.CancellationToken);

            var query = new Query<Customer>()
                .WithNoTracking();

            // Act
            var results = await repository.Get(query, cancellationToken: TestContext.CancellationToken);
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
                await repository.Insert(new()
                {
                    Name = $"Customer{i:D2}"
                }, TestContext.CancellationToken);
            }

            var query = new Query<Customer>()
                .Where(c => c.ID > 25)
                .Sort(q => q.OrderByDescending(c => c.Name))
                .Paged(2, 5)
                .WithNoTracking();

            // Act
            var results = await repository.Get(query, cancellationToken: TestContext.CancellationToken);

            // Assert
            CollectionAssertEx.HasCount(results, 5);
            Assert.IsTrue(results.All(c => c.ID > 25));
            
            // Check descending order
            var names = results.Select(c => c.Name).ToList();
            CollectionAssert.AreEqual(names.OrderByDescending(n => n).ToList(), names);

            // Verify entities are not tracked
            Assert.IsTrue(results.All(c => 
                context.Entry(c).State == Microsoft.EntityFrameworkCore.EntityState.Detached));
        }

        [TestMethod]
        public async Task GetWithQuery_NullQuery_ThrowsException()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);

            // Act
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await repository.Get((Query<Customer>)null!, TestContext.CancellationToken));
        }

        [TestMethod]
        public async Task GetWithQuery_EmptyQuery_ReturnsAll()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            await repository.Insert(new() { Name = "Customer1" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Customer2" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Customer3" }, TestContext.CancellationToken);

            var query = new Query<Customer>();

            // Act
            var results = await repository.Get(query, cancellationToken: TestContext.CancellationToken);

            // Assert
            CollectionAssertEx.HasCount(results, 3, "The query should return all customers.");
        }

        [TestMethod]
        public async Task GetWithQuery_QueryReusability_Works()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);

            for (int i = 1; i <= 20; i++)
            {
                await repository.Insert(new()
                {
                    Name = $"Customer{i:D2}"
                }, TestContext.CancellationToken);
            }

            // Create a reusable query - filter customers with ID > 10
            var filteredCustomersQuery = new Query<Customer>()
                .Where(c => c.ID > 10)
                .Sort(q => q.OrderBy(c => c.Name));

            // Act - Use the query multiple times
            var allFiltered = await repository.Get(filteredCustomersQuery, cancellationToken: TestContext.CancellationToken);

            // Modify for pagination - first page
            filteredCustomersQuery.Paged(1, 5);
            var firstPage = await repository.Get(filteredCustomersQuery, cancellationToken: TestContext.CancellationToken);

            // Second page
            filteredCustomersQuery.Paged(2, 5);
            var secondPage = await repository.Get(filteredCustomersQuery, cancellationToken: TestContext.CancellationToken);

            // Assert
            Assert.AreEqual(10, allFiltered.Count(), "There should be 10 filtered customers."); // Customers 11-20
            Assert.AreEqual(5, firstPage.Count(), "The first page should contain 5 customers."); // First 5 filtered customers
            Assert.AreEqual(5, secondPage.Count(), "The second page should contain 5 customers."); // Next 5 filtered customers
            Assert.AreNotEqual(firstPage.First().Name, secondPage.First().Name, "The first customer on the first page should not be the same as the first customer on the second page.");
        }

        [TestMethod]
        public async Task GetWithQuery_MultipleIncludes_LoadsAllNavigationProperties()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var productRepository = new EntityFrameworkCoreRepository<Product, InMemoryDataContext>(context);

            var customer = await repository.Insert(new() { Name = "Customer1" }, TestContext.CancellationToken);
            var product1 = await productRepository.Insert(new Product { Name = "Product1" }, TestContext.CancellationToken);
            var product2 = await productRepository.Insert(new Product { Name = "Product2" }, TestContext.CancellationToken);
            customer.Products.Add(product1);
            customer.Products.Add(product2);
            await context.SaveChangesAsync(TestContext.CancellationToken);

            var query = new Query<Customer>()
                .Include(c => c.Products)
                .Where(c => c.Name == "Customer1");

            // Act
            var results = await repository.Get(query, cancellationToken: TestContext.CancellationToken);
            var customerWithProducts = results.First();

            // Assert
            Assert.IsNotNull(customerWithProducts.Products, "Products should not be null.");
            Assert.HasCount(2, customerWithProducts.Products, "There should be two products associated with the customer.");
        }

        [TestMethod]
        public async Task GetWithQuery_WithFilterAndPagination_AppliesFilterBeforePagination()
        {
            // Arrange
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);

            for (int i = 1; i <= 30; i++)
            {
                await repository.Insert(new()
                {
                    Name = $"Customer{i:D2}"
                }, TestContext.CancellationToken);
            }

            var query = new Query<Customer>()
                .Where(c => c.ID > 10) // Only customers 11-30
                .Sort(q => q.OrderBy(c => c.Name))
                .Paged(1, 10);

            // Act
            var results = await repository.Get(query, cancellationToken: TestContext.CancellationToken);

            // Assert
            CollectionAssertEx.HasCount(results, 10, "The results should contain 10 customers.");
            // All should have ID > 10 (filtered)
            Assert.IsTrue(results.All(c => c.ID > 10), "All customers should have an ID greater than 10.");
            // First page of filtered customers should start at Customer11
            Assert.AreEqual("Customer11", results.First().Name, "The first customer should be Customer11.");
        }
    }
}
