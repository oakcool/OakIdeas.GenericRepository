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
	public class BatchOperationsTests
	{
        public TestContext TestContext { get; set; }

		[TestMethod]
		public async Task InsertRange_MultipleEntities_InsertsAll()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			var customers = new List<Customer>
			{
				new() { Name = "Customer 1" },
				new() { Name = "Customer 2" },
				new() { Name = "Customer 3" }
			};

			// Act
			var result = await repository.InsertRange(customers, TestContext.CancellationToken);

			// Assert
			Assert.AreEqual(3, result.Count());
			var allCustomers = await repository.Get(cancellationToken: TestContext.CancellationToken);
			Assert.AreEqual(3, allCustomers.Count());
		}

		[TestMethod]
		public async Task InsertRange_EmptyCollection_ReturnsEmpty()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			var customers = new List<Customer>();

			// Act
			var result = await repository.InsertRange(customers, TestContext.CancellationToken);

			// Assert
			Assert.AreEqual(0, result.Count());
		}

		[TestMethod]
		public async Task InsertRange_NullCollection_ThrowsException()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();

            // Act
            try
            {
                await repository.InsertRange(null!, TestContext.CancellationToken);
                Assert.Fail("Expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
		}

		[TestMethod]
		public async Task InsertRange_WithCancellationToken_RespectsToken()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			var customers = new List<Customer>
			{
				new() { Name = "Customer 1" }
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			// Act & Assert
			await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await repository.InsertRange(customers, cts.Token));
		}

		[TestMethod]
		public async Task UpdateRange_MultipleEntities_UpdatesAll()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			var customer1 = await repository.Insert(new() { Name = "Customer 1" }, TestContext.CancellationToken);
			var customer2 = await repository.Insert(new() { Name = "Customer 2" }, TestContext.CancellationToken);
			var customer3 = await repository.Insert(new() { Name = "Customer 3" }, TestContext.CancellationToken);

			customer1.Name = "Updated 1";
			customer2.Name = "Updated 2";
			customer3.Name = "Updated 3";

			var toUpdate = new List<Customer> { customer1, customer2, customer3 };

			// Act
			var result = await repository.UpdateRange(toUpdate, TestContext.CancellationToken);

			// Assert
			Assert.AreEqual(3, result.Count());
			var updated1 = await repository.Get(customer1.ID, TestContext.CancellationToken);
			var updated2 = await repository.Get(customer2.ID, TestContext.CancellationToken);
			var updated3 = await repository.Get(customer3.ID, TestContext.CancellationToken);
            Assert.AreEqual("Updated 1", updated1?.Name);
            Assert.AreEqual("Updated 2", updated2?.Name);
            Assert.AreEqual("Updated 3", updated3?.Name);
		}

		[TestMethod]
		public async Task UpdateRange_EmptyCollection_ReturnsEmpty()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			var customers = new List<Customer>();

			// Act
			var result = await repository.UpdateRange(customers, TestContext.CancellationToken);

			// Assert
			Assert.AreEqual(0, result.Count());
		}

		[TestMethod]
		public async Task UpdateRange_NullCollection_ThrowsException()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();

            // Act
            try
            {
                await repository.UpdateRange(null!, TestContext.CancellationToken);
                Assert.Fail("Expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
		}

		[TestMethod]
		public async Task DeleteRange_MultipleEntities_DeletesAll()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			var customer1 = await repository.Insert(new() { Name = "Customer 1" }, TestContext.CancellationToken);
			var customer2 = await repository.Insert(new() { Name = "Customer 2" }, TestContext.CancellationToken);
			var customer3 = await repository.Insert(new() { Name = "Customer 3" }, TestContext.CancellationToken );

			var toDelete = new List<Customer> { customer1, customer2, customer3 };

			// Act
			var deletedCount = await repository.DeleteRange(toDelete, TestContext.CancellationToken);

			// Assert
			Assert.AreEqual(3, deletedCount);
			var remaining = await repository.Get(cancellationToken: TestContext.CancellationToken);
			Assert.AreEqual(0, remaining.Count());
		}

		[TestMethod]
		public async Task DeleteRange_EmptyCollection_ReturnsZero()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			var customers = new List<Customer>();

			// Act
			var deletedCount = await repository.DeleteRange(customers, TestContext.CancellationToken);

			// Assert
			Assert.AreEqual(0, deletedCount);
		}

		[TestMethod]
		public async Task DeleteRange_NullCollection_ThrowsException()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();

            // Act
            try
            {
                await repository.DeleteRange((IEnumerable<Customer>)null!, TestContext.CancellationToken);
                Assert.Fail("Expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
		}

		[TestMethod]
		public async Task DeleteRange_WithFilter_DeletesMatchingEntities()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			await repository.Insert(new() { Name = "Active Customer 1" }, TestContext.CancellationToken);
			await repository.Insert(new() { Name = "Active Customer 2" }, TestContext.CancellationToken);
			await repository.Insert(new() { Name = "Inactive Customer 1" }, TestContext.CancellationToken);
			await repository.Insert(new() { Name = "Inactive Customer 2" }, TestContext.CancellationToken );

			// Act
			var deletedCount = await repository.DeleteRange(c => c.Name.StartsWith("Active"), TestContext.CancellationToken);

			// Assert
			Assert.AreEqual(2, deletedCount);
			var remaining = await repository.Get(cancellationToken: TestContext.CancellationToken);
			Assert.AreEqual(2, remaining.Count());
			Assert.IsTrue(remaining.All(c => c.Name.StartsWith("Inactive")));
		}

		[TestMethod]
		public async Task DeleteRange_WithFilter_NoMatches_ReturnsZero()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			await repository.Insert(new() { Name = "Customer 1" }, TestContext.CancellationToken);
            await repository.Insert(new() { Name = "Customer 2" }, TestContext.CancellationToken );

			// Act
			var deletedCount = await repository.DeleteRange(c => c.Name.StartsWith("NonExistent"), TestContext.CancellationToken);

			// Assert
			Assert.AreEqual(0, deletedCount);
			var remaining = await repository.Get(cancellationToken: TestContext.CancellationToken);
			Assert.AreEqual(2, remaining.Count());
		}

		[TestMethod]
		public async Task DeleteRange_NullFilter_ThrowsException()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();

            // Act
            try
            {
                await repository.DeleteRange((System.Linq.Expressions.Expression<Func<Customer, bool>>)null!, TestContext.CancellationToken);
                Assert.Fail("Expected ArgumentNullException");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
		}

		[TestMethod]
		public async Task InsertRange_LargeCollection_InsertsAllEfficiently()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			var customers = Enumerable.Range(1, 100)
				.Select(i => new Customer() { Name = $"Customer {i}" })
				.ToList();

			// Act
			var result = await repository.InsertRange(customers, TestContext.CancellationToken);

			// Assert
			Assert.AreEqual(100, result.Count());
			var allCustomers = await repository.Get(cancellationToken: TestContext.CancellationToken);
			Assert.AreEqual(100, allCustomers.Count());
		}

		[TestMethod]
		public async Task UpdateRange_NonExistentEntities_ReturnsUpdatedList()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			var nonExistent = new List<Customer>
			{
				new() { ID = 999, Name = "NonExistent 1" },
				new() { ID = 998, Name = "NonExistent 2" }
			};

			// Act
			var result = await repository.UpdateRange(nonExistent, TestContext.CancellationToken);

			// Assert - MemoryGenericRepository doesn't add non-existent entities on update
			Assert.AreEqual(2, result.Count());
		}

		[TestMethod]
		public async Task DeleteRange_PartialExistence_DeletesOnlyExisting()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			var customer1 = await repository.Insert(new() { Name = "Customer 1" }, TestContext.CancellationToken);
			var nonExistent = new Customer() { ID = 999, Name = "NonExistent" };

			var toDelete = new List<Customer> { customer1, nonExistent };

			// Act
			var deletedCount = await repository.DeleteRange(toDelete, TestContext.CancellationToken);

			// Assert
			Assert.AreEqual(1, deletedCount);
			var remaining = await repository.Get(cancellationToken: TestContext.CancellationToken);
			Assert.AreEqual(0, remaining.Count());
		}

		[TestMethod]
		public async Task BatchOperations_CombinedScenario_WorksCorrectly()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();

			// Insert initial batch
			var initialCustomers = new List<Customer>
			{
				new() { Name = "Customer 1" },
				new() { Name = "Customer 2" },
				new() { Name = "Customer 3" },
				new() { Name = "Customer 4" },
				new() { Name = "Customer 5" }
			};
			await repository.InsertRange(initialCustomers, TestContext.CancellationToken);

			// Update some
			var toUpdate = (await repository.Get(filter: c => c.Name == "Customer 1" || c.Name == "Customer 2", cancellationToken: TestContext.CancellationToken)).ToList();
			foreach (var customer in toUpdate)
			{
				customer.Name += " Updated";
			}
			await repository.UpdateRange(toUpdate, TestContext.CancellationToken);

			// Delete some by filter
			await repository.DeleteRange(c => c.Name == "Customer 3", TestContext.CancellationToken);

			// Act - verify final state
			var remaining = await repository.Get(cancellationToken: TestContext.CancellationToken);

			// Assert
			Assert.AreEqual(4, remaining.Count());
			Assert.IsTrue(remaining.Any(c => c.Name == "Customer 1 Updated"));
			Assert.IsTrue(remaining.Any(c => c.Name == "Customer 2 Updated"));
			Assert.IsFalse(remaining.Any(c => c.Name == "Customer 3"));
		}
	}
}
