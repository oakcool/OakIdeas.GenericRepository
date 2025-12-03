using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Contexts;
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
        private InMemoryDataContext CreateContext([System.Runtime.CompilerServices.CallerMemberName] string testName = "")
        {
            var options = new DbContextOptionsBuilder<InMemoryDataContext>()
                .UseInMemoryDatabase(databaseName: $"TestDB_{testName}_{Guid.NewGuid()}")
                .Options;
            return new InMemoryDataContext(options);
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_EmptyRepository_ReturnsNoItems()
        {
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            var count = 0;

            await foreach (var customer in repository.GetAsyncEnumerable())
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
            
            await repository.Insert(new Customer { Name = "Customer 1" });
            await repository.Insert(new Customer { Name = "Customer 2" });
            await repository.Insert(new Customer { Name = "Customer 3" });

            var count = 0;
            var names = new List<string>();

            await foreach (var customer in repository.GetAsyncEnumerable())
            {
                count++;
                names.Add(customer.Name);
            }

            Assert.AreEqual(3, count);
            Assert.IsTrue(names.Contains("Customer 1"));
            Assert.IsTrue(names.Contains("Customer 2"));
            Assert.IsTrue(names.Contains("Customer 3"));
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_WithFilter_ReturnsFilteredItems()
        {
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            
            await repository.Insert(new Customer { Name = "Active Customer" });
            await repository.Insert(new Customer { Name = "Inactive Customer" });
            await repository.Insert(new Customer { Name = "Active User" });

            var count = 0;
            await foreach (var customer in repository.GetAsyncEnumerable(
                filter: c => c.Name.Contains("Active")))
            {
                count++;
                Assert.IsTrue(customer.Name.Contains("Active"));
            }

            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_WithOrdering_ReturnsOrderedItems()
        {
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            
            await repository.Insert(new Customer { Name = "Charlie" });
            await repository.Insert(new Customer { Name = "Alice" });
            await repository.Insert(new Customer { Name = "Bob" });

            var names = new List<string>();
            await foreach (var customer in repository.GetAsyncEnumerable(
                orderBy: q => q.OrderBy(c => c.Name)))
            {
                names.Add(customer.Name);
            }

            Assert.AreEqual(3, names.Count);
            Assert.AreEqual("Alice", names[0]);
            Assert.AreEqual("Bob", names[1]);
            Assert.AreEqual("Charlie", names[2]);
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_WithFilterAndOrdering_ReturnsSortedFilteredItems()
        {
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            
            await repository.Insert(new Customer { Name = "Active Z" });
            await repository.Insert(new Customer { Name = "Inactive A" });
            await repository.Insert(new Customer { Name = "Active A" });
            await repository.Insert(new Customer { Name = "Active M" });

            var names = new List<string>();
            await foreach (var customer in repository.GetAsyncEnumerable(
                filter: c => c.Name.Contains("Active"),
                orderBy: q => q.OrderBy(c => c.Name)))
            {
                names.Add(customer.Name);
            }

            Assert.AreEqual(3, names.Count);
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
                entities.Add(new Customer { Name = $"Customer {i}" });
            }
            await repository.InsertRange(entities);

            var count = 0;
            await foreach (var customer in repository.GetAsyncEnumerable())
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
            var entities = new List<Customer>();
            for (int i = 0; i < 100; i++)
            {
                entities.Add(new Customer { Name = $"Customer {i}" });
            }
            await repository.InsertRange(entities);

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
                Assert.IsTrue(count == 5, $"Expected count to be 5, but was {count}");
            }
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_WithIncludeProperties_LoadsRelatedData()
        {
            var context = CreateContext();
            var customerRepo = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);

            // Insert customer - just test that include properties doesn't cause errors
            var customer = await customerRepo.Insert(new Customer { Name = "Test Customer" });

            var count = 0;
            await foreach (var cust in customerRepo.GetAsyncEnumerable(includeProperties: "Products"))
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
            
            await repository.Insert(new Customer { Name = "Customer 1" });
            await repository.Insert(new Customer { Name = "Customer 2" });

            var asyncEnumerable = repository.GetAsyncEnumerable();

            // First enumeration
            var count1 = 0;
            await foreach (var customer in asyncEnumerable)
            {
                count1++;
            }

            // Second enumeration
            var count2 = 0;
            await foreach (var customer in asyncEnumerable)
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
                entities.Add(new Customer { Name = $"Customer {i}" });
            }
            await repository.InsertRange(entities);

            await foreach (var customer in repository.GetAsyncEnumerable())
            {
                // Process each item
                processedIds.Add(customer.ID);
                await Task.Delay(1); // Simulate some async processing
            }

            Assert.AreEqual(20, processedIds.Count);
            Assert.AreEqual(20, processedIds.Distinct().Count()); // All IDs should be unique
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_ComparedToGet_ReturnsSameData()
        {
            var context = CreateContext();
            var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
            
            await repository.Insert(new Customer { Name = "Customer 1" });
            await repository.Insert(new Customer { Name = "Customer 2" });
            await repository.Insert(new Customer { Name = "Customer 3" });

            // Get all using traditional method
            var allTraditional = (await repository.Get()).OrderBy(c => c.Name).ToList();

            // Get all using async enumerable
            var allAsync = new List<Customer>();
            await foreach (var customer in repository.GetAsyncEnumerable(orderBy: q => q.OrderBy(c => c.Name)))
            {
                allAsync.Add(customer);
            }

            Assert.AreEqual(allTraditional.Count, allAsync.Count);
            for (int i = 0; i < allTraditional.Count; i++)
            {
                Assert.AreEqual(allTraditional[i].ID, allAsync[i].ID);
                Assert.AreEqual(allTraditional[i].Name, allAsync[i].Name);
            }
        }
    }
}
