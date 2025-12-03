using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.Tests.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Tests
{
    [TestClass]
    public class AsyncEnumerableTests
    {
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
            var repository = new MemoryGenericRepository<Customer>();
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
            var repository = new MemoryGenericRepository<Customer>();
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
            var repository = new MemoryGenericRepository<Customer>();
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
            var repository = new MemoryGenericRepository<Customer>();
            
            // Insert 1000 entities
            var entities = new List<Customer>();
            for (int i = 0; i < 1000; i++)
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

            Assert.AreEqual(1000, count);
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_WithCancellationToken_RespectsToken()
        {
            var repository = new MemoryGenericRepository<Customer>();
            
            // Insert multiple entities
            for (int i = 0; i < 100; i++)
            {
                await repository.Insert(new Customer { Name = $"Customer {i}" });
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
                Assert.IsTrue(count == 5, $"Expected count to be 5, but was {count}");
            }
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_WithPreCancelledToken_ThrowsImmediately()
        {
            var repository = new MemoryGenericRepository<Customer>();
            await repository.Insert(new Customer { Name = "Customer 1" });

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
            var repository = new MemoryGenericRepository<Customer>();
            var processedIds = new List<int>();

            for (int i = 0; i < 10; i++)
            {
                await repository.Insert(new Customer { Name = $"Customer {i}" });
            }

            await foreach (var customer in repository.GetAsyncEnumerable())
            {
                // Process each item
                processedIds.Add(customer.ID);
                await Task.Delay(1); // Simulate some async processing
            }

            Assert.AreEqual(10, processedIds.Count);
            Assert.AreEqual(10, processedIds.Distinct().Count()); // All IDs should be unique
        }
    }
}
