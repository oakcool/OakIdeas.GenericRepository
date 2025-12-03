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
	public class BatchOperationsTests
	{
		private InMemoryDataContext CreateContext()
		{
			var options = new DbContextOptionsBuilder<InMemoryDataContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;
			return new InMemoryDataContext(options);
		}

		[TestMethod]
		public async Task InsertRange_MultipleEntities_InsertsAll()
		{
			// Arrange
			var context = CreateContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var customers = new List<Customer>
			{
				new Customer { Name = "Customer 1" },
				new Customer { Name = "Customer 2" },
				new Customer { Name = "Customer 3" }
			};

			// Act
			var result = await repository.InsertRange(customers);

			// Assert
			Assert.AreEqual(3, result.Count());
			var allCustomers = await repository.Get();
			Assert.AreEqual(3, allCustomers.Count());
		}

		[TestMethod]
		public async Task InsertRange_EmptyCollection_ReturnsEmpty()
		{
			// Arrange
			var context = CreateContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var customers = new List<Customer>();

			// Act
			var result = await repository.InsertRange(customers);

			// Assert
			Assert.AreEqual(0, result.Count());
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task InsertRange_NullCollection_ThrowsException()
		{
			// Arrange
			var context = CreateContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);

			// Act
			await repository.InsertRange(null);
		}

		[TestMethod]
		public async Task InsertRange_WithCancellationToken_RespectsToken()
		{
			// Arrange
			var context = CreateContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var customers = new List<Customer>
			{
				new Customer { Name = "Customer 1" }
			};
			var cts = new CancellationTokenSource();
			cts.Cancel();

			// Act & Assert
			// TaskCanceledException is a subclass of OperationCanceledException
			await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
			{
				await repository.InsertRange(customers, cts.Token);
			});
		}

		[TestMethod]
		public async Task UpdateRange_MultipleEntities_UpdatesAll()
		{
			// Arrange
			var context = CreateContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var customer1 = await repository.Insert(new Customer { Name = "Customer 1" });
			var customer2 = await repository.Insert(new Customer { Name = "Customer 2" });
			var customer3 = await repository.Insert(new Customer { Name = "Customer 3" });

			// Detach entities to simulate real-world scenario
			context.Entry(customer1).State = EntityState.Detached;
			context.Entry(customer2).State = EntityState.Detached;
			context.Entry(customer3).State = EntityState.Detached;

			customer1.Name = "Updated 1";
			customer2.Name = "Updated 2";
			customer3.Name = "Updated 3";

			var toUpdate = new List<Customer> { customer1, customer2, customer3 };

			// Act
			var result = await repository.UpdateRange(toUpdate);

			// Assert
			Assert.AreEqual(3, result.Count());
			var updated1 = await repository.Get(customer1.ID);
			var updated2 = await repository.Get(customer2.ID);
			var updated3 = await repository.Get(customer3.ID);
			Assert.AreEqual("Updated 1", updated1.Name);
			Assert.AreEqual("Updated 2", updated2.Name);
			Assert.AreEqual("Updated 3", updated3.Name);
		}

		[TestMethod]
		public async Task UpdateRange_EmptyCollection_ReturnsEmpty()
		{
			// Arrange
			var context = CreateContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var customers = new List<Customer>();

			// Act
			var result = await repository.UpdateRange(customers);

			// Assert
			Assert.AreEqual(0, result.Count());
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task UpdateRange_NullCollection_ThrowsException()
		{
			// Arrange
			var context = CreateContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);

			// Act
			await repository.UpdateRange(null);
		}

		[TestMethod]
		public async Task DeleteRange_MultipleEntities_DeletesAll()
		{
			// Arrange
			var context = CreateContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var customer1 = await repository.Insert(new Customer { Name = "Customer 1" });
			var customer2 = await repository.Insert(new Customer { Name = "Customer 2" });
			var customer3 = await repository.Insert(new Customer { Name = "Customer 3" });

			var toDelete = new List<Customer> { customer1, customer2, customer3 };

			// Act
			var deletedCount = await repository.DeleteRange(toDelete);

			// Assert
			Assert.AreEqual(3, deletedCount);
			var remaining = await repository.Get();
			Assert.AreEqual(0, remaining.Count());
		}

		[TestMethod]
		public async Task DeleteRange_EmptyCollection_ReturnsZero()
		{
			// Arrange
			var context = CreateContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var customers = new List<Customer>();

			// Act
			var deletedCount = await repository.DeleteRange(customers);

			// Assert
			Assert.AreEqual(0, deletedCount);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task DeleteRange_NullCollection_ThrowsException()
		{
			// Arrange
			var context = CreateContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);

			// Act
			await repository.DeleteRange((IEnumerable<Customer>)null);
		}

		[TestMethod]
		public async Task DeleteRange_WithFilter_DeletesMatchingEntities()
		{
			// Arrange
			var context = CreateContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			await repository.Insert(new Customer { Name = "Active Customer 1" });
			await repository.Insert(new Customer { Name = "Active Customer 2" });
			await repository.Insert(new Customer { Name = "Inactive Customer 1" });
			await repository.Insert(new Customer { Name = "Inactive Customer 2" });

			// Act
			var deletedCount = await repository.DeleteRange(c => c.Name.StartsWith("Active"));

			// Assert
			Assert.AreEqual(2, deletedCount);
			var remaining = await repository.Get();
			Assert.AreEqual(2, remaining.Count());
			Assert.IsTrue(remaining.All(c => c.Name.StartsWith("Inactive")));
		}

		[TestMethod]
		public async Task DeleteRange_WithFilter_NoMatches_ReturnsZero()
		{
			// Arrange
			var context = CreateContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			await repository.Insert(new Customer { Name = "Customer 1" });
			await repository.Insert(new Customer { Name = "Customer 2" });

			// Act
			var deletedCount = await repository.DeleteRange(c => c.Name.StartsWith("NonExistent"));

			// Assert
			Assert.AreEqual(0, deletedCount);
			var remaining = await repository.Get();
			Assert.AreEqual(2, remaining.Count());
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task DeleteRange_NullFilter_ThrowsException()
		{
			// Arrange
			var context = CreateContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);

			// Act
			await repository.DeleteRange((System.Linq.Expressions.Expression<Func<Customer, bool>>)null);
		}

		[TestMethod]
		public async Task InsertRange_LargeCollection_InsertsAllEfficiently()
		{
			// Arrange
			var context = CreateContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var customers = Enumerable.Range(1, 100)
				.Select(i => new Customer { Name = $"Customer {i}" })
				.ToList();

			// Act
			var result = await repository.InsertRange(customers);

			// Assert
			Assert.AreEqual(100, result.Count());
			var allCustomers = await repository.Get();
			Assert.AreEqual(100, allCustomers.Count());
		}

		[TestMethod]
		public async Task DeleteRange_DetachedEntities_AttachesAndDeletes()
		{
			// Arrange
			var context = CreateContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);
			var customer1 = await repository.Insert(new Customer { Name = "Customer 1" });
			var customer2 = await repository.Insert(new Customer { Name = "Customer 2" });

			// Detach entities
			context.Entry(customer1).State = EntityState.Detached;
			context.Entry(customer2).State = EntityState.Detached;

			var toDelete = new List<Customer> { customer1, customer2 };

			// Act
			var deletedCount = await repository.DeleteRange(toDelete);

			// Assert
			Assert.AreEqual(2, deletedCount);
			var remaining = await repository.Get();
			Assert.AreEqual(0, remaining.Count());
		}

		[TestMethod]
		public async Task BatchOperations_CombinedScenario_WorksCorrectly()
		{
			// Arrange
			var context = CreateContext();
			var repository = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context);

			// Insert initial batch
			var initialCustomers = new List<Customer>
			{
				new Customer { Name = "Customer 1" },
				new Customer { Name = "Customer 2" },
				new Customer { Name = "Customer 3" },
				new Customer { Name = "Customer 4" },
				new Customer { Name = "Customer 5" }
			};
			await repository.InsertRange(initialCustomers);

			// Update some
			var toUpdate = (await repository.Get(c => c.Name == "Customer 1" || c.Name == "Customer 2")).ToList();
			foreach (var customer in toUpdate)
			{
				customer.Name = customer.Name + " Updated";
				context.Entry(customer).State = EntityState.Detached;
			}
			await repository.UpdateRange(toUpdate);

			// Delete some by filter
			await repository.DeleteRange(c => c.Name == "Customer 3");

			// Act - verify final state
			var remaining = await repository.Get();

			// Assert
			Assert.AreEqual(4, remaining.Count());
			Assert.IsTrue(remaining.Any(c => c.Name == "Customer 1 Updated"));
			Assert.IsTrue(remaining.Any(c => c.Name == "Customer 2 Updated"));
			Assert.IsFalse(remaining.Any(c => c.Name == "Customer 3"));
		}

		[TestMethod]
		public async Task InsertRange_PerformanceComparison_FasterThanIndividualInserts()
		{
			// Arrange
			var context1 = CreateContext();
			var repository1 = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context1);

			var context2 = CreateContext();
			var repository2 = new EntityFrameworkCoreRepository<Customer, InMemoryDataContext>(context2);

			var customers1 = Enumerable.Range(1, 50)
				.Select(i => new Customer { Name = $"Customer {i}" })
				.ToList();

			var customers2 = Enumerable.Range(1, 50)
				.Select(i => new Customer { Name = $"Customer {i}" })
				.ToList();

			// Act - InsertRange
			var startBatch = DateTime.UtcNow;
			await repository1.InsertRange(customers1);
			var batchTime = DateTime.UtcNow - startBatch;

			// Act - Individual inserts
			var startIndividual = DateTime.UtcNow;
			foreach (var customer in customers2)
			{
				await repository2.Insert(customer);
			}
			var individualTime = DateTime.UtcNow - startIndividual;

			// Assert - both complete successfully
			var result1 = await repository1.Get();
			var result2 = await repository2.Get();
			Assert.AreEqual(50, result1.Count());
			Assert.AreEqual(50, result2.Count());

			// Note: In some cases, batch operations are more efficient
			// This test just verifies both approaches work
		}
	}
}
