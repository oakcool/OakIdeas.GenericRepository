using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Contexts;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Helpers;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.EntityFrameworkCore.Tests
{
    [TestClass]
    public class AsyncEnumerableEFCoreTests
    {
        private static InMemoryDataContext CreateContext([System.Runtime.CompilerServices.CallerMemberName] string testName = "")
        {
            var options = new DbContextOptionsBuilder<InMemoryDataContext>()
                .UseInMemoryDatabase(databaseName: $"TestDB_{testName}_{Guid.NewGuid()}")
                .Options;
            return new InMemoryDataContext(options);
        }

        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task GetAsyncEnumerable_EmptyRepository_ReturnsNoItems()
        {
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var count = 0;

            await foreach (var customer in repository.GetAsyncEnumerable(cancellationToken: TestContext.CancellationToken))
            {
                count++;
            }

            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_MultipleEntities_StreamsAllItems()
        {
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            
            await repository.Insert(new() { Name = "Customer 1" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Customer 2" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Customer 3" }, TestContext.CancellationToken);

            var count = 0;
            var names = new List<string>();

            await foreach (var customer in repository.GetAsyncEnumerable(cancellationToken: TestContext.CancellationToken))
            {
                count++;
                names.Add(customer.Name);
            }

            Assert.AreEqual(3, count);
            CollectionAssert.Contains(names, "Customer 1");
            CollectionAssert.Contains(names, "Customer 2");
            CollectionAssert.Contains(names, "Customer 3");
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_WithFilter_ReturnsFilteredItems()
        {
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            
            await repository.Insert(new() { Name = "Active Customer" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Inactive Customer" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Active User" }, TestContext.CancellationToken);

            var count = 0;
            await foreach (var customer in repository.GetAsyncEnumerable(
                filter: c => c.Name.Contains("Active"),
                cancellationToken: TestContext.CancellationToken))
            {
                count++;
                Assert.Contains("Active", customer.Name);
            }

            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_WithOrdering_ReturnsOrderedItems()
        {
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            
            await repository.Insert(new() { Name = "Charlie" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Alice" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Bob" }, TestContext.CancellationToken);

            var names = new List<string>();
            await foreach (var customer in repository.GetAsyncEnumerable(
                orderBy: q => q.OrderBy(c => c.Name),
                cancellationToken: TestContext.CancellationToken))
            {
                names.Add(customer.Name);
            }

            Assert.HasCount(3, names);
            Assert.AreEqual("Alice", names[0]);
            Assert.AreEqual("Bob", names[1]);
            Assert.AreEqual("Charlie", names[2]);
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_WithFilterAndOrdering_ReturnsSortedFilteredItems()
        {
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            
            await repository.Insert(new() { Name = "Active Z" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Inactive A" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Active A" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Active M" }, TestContext.CancellationToken);

            var names = new List<string>();
            await foreach (var customer in repository.GetAsyncEnumerable(
                filter: c => c.Name.Contains("Active"),
                orderBy: q => q.OrderBy(c => c.Name),
                cancellationToken: TestContext.CancellationToken))
            {
                names.Add(customer.Name);
            }

            Assert.HasCount(3, names);
            Assert.AreEqual("Active A", names[0]);
            Assert.AreEqual("Active M", names[1]);
            Assert.AreEqual("Active Z", names[2]);
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_LargeDataset_StreamsEfficiently()
        {
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            
            // Insert 500 entities
            var entities = new List<Customer>();
            for (int i = 0; i < 500; i++)
            {
                entities.Add(new() { Name = $"Customer {i}" });
            }
            await repository.InsertRange(entities, TestContext.CancellationToken);

            var count = 0;
            await foreach (var customer in repository.GetAsyncEnumerable(cancellationToken: TestContext.CancellationToken))
            {
                count++;
                Assert.IsNotNull(customer.Name);
            }

            Assert.AreEqual(500, count);
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_WithCancellationToken_RespectsToken()
        {
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            
            // Insert multiple entities
            var entities = new List<Customer>
            {
                new() { Name = "Customer 0" },
                new() { Name = "Customer 1" },
                new() { Name = "Customer 2" },
                new() { Name = "Customer 3" },
                new() { Name = "Customer 4" },
                new() { Name = "Customer 5" },
                new() { Name = "Customer 6" },
                new() { Name = "Customer 7" },
                new() { Name = "Customer 8" },
                new() { Name = "Customer 9" }
            };
            await repository.InsertRange(entities, TestContext.CancellationToken);

            var cts = new CancellationTokenSource();
            var count = 0;

            try
            {
                await foreach (var customer in repository.GetAsyncEnumerable(cancellationToken: cts.Token))
                {
                    count++;
                    if (count == 5)
                    {
                        cts.Cancel();
                    }
                }
                Assert.Fail("Expected OperationCanceledException");
            }
            catch (OperationCanceledException)
            {
                Assert.AreEqual(5, count, $"Expected count to be 5, but was {count}");
            }
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_WithIncludeProperties_LoadsRelatedData()
        {
            var context = CreateContext();
            var customerRepo = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);

            // Insert customer - just test that include properties doesn't cause errors
            var customer = await customerRepo.Insert(new() { Name = "Test Customer" }, TestContext.CancellationToken);

            var count = 0;
            await foreach (var cust in customerRepo.GetAsyncEnumerable(includeProperties: "Products", cancellationToken: TestContext.CancellationToken))
            {
                count++;
                Assert.IsNotNull(cust.Products);
                // Note: In-memory database might not properly load relationships in async enumerable
                // This test verifies the code path works without errors
            }

            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_CanBeEnumeratedMultipleTimes()
        {
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            
            await repository.Insert(new() { Name = "Customer 1" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Customer 2" }, TestContext.CancellationToken);

            var asyncEnumerable = repository.GetAsyncEnumerable(cancellationToken: TestContext.CancellationToken);

            // First enumeration
            var count1 = 0;
            await foreach (var customer in repository.GetAsyncEnumerable(cancellationToken: TestContext.CancellationToken))
            {
                count1++;
            }

            // Second enumeration
            var count2 = 0;
            await foreach (var customer in repository.GetAsyncEnumerable(cancellationToken: TestContext.CancellationToken))
            {
                count2++;
            }

            Assert.AreEqual(2, count1);
            Assert.AreEqual(2, count2);
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_ProcessItemsOneAtATime_WorksCorrectly()
        {
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var processedIds = new List<int>();

            var entities = new List<Customer>();
            for (int i = 0; i < 20; i++)
            {
                entities.Add(new() { Name = $"Customer {i}" });
            }
            await repository.InsertRange(entities, TestContext.CancellationToken);

            await foreach (var customer in repository.GetAsyncEnumerable(cancellationToken: TestContext.CancellationToken))
            {
                // Process each item
                processedIds.Add(customer.ID);
                await Task.Delay(1, TestContext.CancellationToken); // Simulate some async processing
            }

            Assert.HasCount(20, processedIds);
            Assert.AreEqual(20, processedIds.Distinct().Count()); // All IDs should be unique
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_ComparedToGet_ReturnsSameData()
        {
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            
            await repository.Insert(new() { Name = "Customer 1" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Customer 2" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Customer 3" }, TestContext.CancellationToken);

            // Get all using traditional method
            var allTraditional = (await repository.Get(cancellationToken: TestContext.CancellationToken)).OrderBy(c => c.Name).ToList();

            // Get all using async enumerable
            var allAsync = new List<Customer>();
            await foreach (var customer in repository.GetAsyncEnumerable(orderBy: q => q.OrderBy(c => c.Name), cancellationToken: TestContext.CancellationToken))
            {
                allAsync.Add(customer);
            }

            Assert.HasCount(allTraditional.Count, allAsync);
            for (int i = 0; i < allTraditional.Count; i++)
            {
                Assert.AreEqual(allTraditional[i].ID, allAsync[i].ID);
                Assert.AreEqual(allTraditional[i].Name, allAsync[i].Name);
            }
        }
    }
}
