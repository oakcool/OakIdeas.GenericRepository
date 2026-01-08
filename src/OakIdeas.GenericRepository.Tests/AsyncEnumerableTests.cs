using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.Memory;
using OakIdeas.GenericRepository.Tests.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class AsyncEnumerableTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task GetAsyncEnumerable_EmptyRepository_ReturnsNoItems()
        {
            var repository = new MemoryGenericRepository<Customer>();
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
            var repository = new MemoryGenericRepository<Customer>();
            await repository.Insert(new() { Name = "Customer 1" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Customer 2" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Customer 3" }, TestContext.CancellationToken);

            var count = 0;
            var names = new List<string>();

            await foreach (var customer in repository.GetAsyncEnumerable())
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
            var repository = new MemoryGenericRepository<Customer>();
            await repository.Insert(new() { Name = "Active Customer" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Inactive Customer" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Active User" }, TestContext.CancellationToken);

            var count = 0;
            await foreach (var customer in repository.GetAsyncEnumerable(
                filter: c => c.Name.Contains("Active")))
            {
                count++;
                StringAssert.Contains(customer.Name, "Active");
            }

            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_WithOrdering_ReturnsOrderedItems()
        {
            var repository = new MemoryGenericRepository<Customer>();
            await repository.Insert(new() { Name = "Charlie" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Alice" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Bob" }, TestContext.CancellationToken);

            var names = new List<string>();
            await foreach (var customer in repository.GetAsyncEnumerable(
                orderBy: q => q.OrderBy(c => c.Name)))
            {
                names.Add(customer.Name);
            }

            CollectionAssert.AllItemsAreNotNull(names);
            Assert.HasCount(3, names);
            Assert.AreEqual("Alice", names[0]);
            Assert.AreEqual("Bob", names[1]);
            Assert.AreEqual("Charlie", names[2]);
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_WithFilterAndOrdering_ReturnsSortedFilteredItems()
        {
            var repository = new MemoryGenericRepository<Customer>();
            await repository.Insert(new() { Name = "Active Z" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Inactive A" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Active A" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Active M" }, TestContext.CancellationToken);

            var names = new List<string>();
            await foreach (var customer in repository.GetAsyncEnumerable(
                filter: c => c.Name.Contains("Active"),
                orderBy: q => q.OrderBy(c => c.Name)))
            {
                names.Add(customer.Name);
            }

            CollectionAssert.AllItemsAreNotNull(names);
            Assert.HasCount(3, names);
            Assert.AreEqual("Active A", names[0]);
            Assert.AreEqual("Active M", names[1]);
            Assert.AreEqual("Active Z", names[2]);
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_LargeDataset_StreamsEfficiently()
        {
            var repository = new MemoryGenericRepository<Customer>();
            
            // Insert 1000 entities
            var entities = new List<Customer>();
            for (int i = 0; i < 1000; i++)
            {
                entities.Add(new() { Name = $"Customer {i}" });
            }
            await repository.InsertRange(entities, TestContext.CancellationToken);

            var count = 0;
            await foreach (var customer in repository.GetAsyncEnumerable())
            {
                count++;
                Assert.IsNotNull(customer.Name);
            }

            Assert.AreEqual(1000, count);
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_WithCancellationToken_RespectsToken()
        {
            var repository = new MemoryGenericRepository<Customer>();
            
            // Insert multiple entities
            for (int i = 0; i < 100; i++)
            {
                await repository.Insert(new() { Name = $"Customer {i}" }, TestContext.CancellationToken);
            }

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
        public async Task GetAsyncEnumerable_WithPreCancelledToken_ThrowsImmediately()
        {
            var repository = new MemoryGenericRepository<Customer>();
            await repository.Insert(new() { Name = "Customer 1" }, TestContext.CancellationToken);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            try
            {
                await foreach (var customer in repository.GetAsyncEnumerable(cancellationToken: cts.Token))
                {
                    Assert.Fail("Should not enumerate any items");
                }
                Assert.Fail("Expected OperationCanceledException");
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_CanBeEnumeratedMultipleTimes()
        {
            var repository = new MemoryGenericRepository<Customer>();
            await repository.Insert(new() { Name = "Customer 1" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Customer 2" }, TestContext.CancellationToken );

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
            var repository = new MemoryGenericRepository<Customer>();
            var processedIds = new List<int>();

            for (int i = 0; i < 10; i++)
            {
                await repository.Insert(new() { Name = $"Customer {i}" }, TestContext.CancellationToken);
            }

            await foreach (var customer in repository.GetAsyncEnumerable())
            {
                // Process each item
                processedIds.Add(customer.ID);
                await Task.Delay(1); // Simulate some async processing
            }

            CollectionAssert.AllItemsAreNotNull(processedIds);
            Assert.HasCount(10, processedIds);
            Assert.AreEqual(10, processedIds.Distinct().Count()); // All IDs should be unique
        }
    }
}
