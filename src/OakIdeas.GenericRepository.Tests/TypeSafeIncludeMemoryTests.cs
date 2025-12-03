using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.Memory;
using OakIdeas.GenericRepository.Tests.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Tests
{
	[TestClass]
	public class TypeSafeIncludeMemoryTests
	{
		private readonly string _customerName = "Test Customer";

		[TestMethod]
		public async Task Get_WithTypeSafeInclude_WorksWithMemoryRepository()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			var customer = new Customer { Name = _customerName };
			await repository.Insert(customer);

			// Act - Use type-safe include (should be ignored in memory repository)
			var customers = await repository.Get(
				filter: c => c.Name == _customerName,
				includeExpressions: c => c.Name // In memory, navigation properties work differently
			);

			// Assert
			var retrievedCustomer = customers.FirstOrDefault();
			Assert.IsNotNull(retrievedCustomer);
			Assert.AreEqual(_customerName, retrievedCustomer.Name);
		}

		[TestMethod]
		public async Task Get_WithEmptyTypeSafeIncludeArray_ReturnsResults()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			var customer = new Customer { Name = _customerName };
			await repository.Insert(customer);

			// Act - Empty include array
			var customers = await repository.Get();

			// Assert
			var retrievedCustomer = customers.FirstOrDefault();
			Assert.IsNotNull(retrievedCustomer);
			Assert.AreEqual(_customerName, retrievedCustomer.Name);
		}

		[TestMethod]
		public async Task Get_TypeSafeIncludeWithFilter_ReturnsFilteredResults()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			await repository.Insert(new Customer { Name = "Customer 1" });
			await repository.Insert(new Customer { Name = "Customer 2" });
			await repository.Insert(new Customer { Name = "Customer 3" });

			// Act - Filter with type-safe include
			var customers = await repository.Get(
				filter: c => c.Name.Contains("2"),
				includeExpressions: c => c.Name
			);

			// Assert
			Assert.AreEqual(1, customers.Count());
			Assert.AreEqual("Customer 2", customers.First().Name);
		}

		[TestMethod]
		public async Task Get_TypeSafeIncludeWithOrdering_ReturnsOrderedResults()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			await repository.Insert(new Customer { Name = "C Customer" });
			await repository.Insert(new Customer { Name = "A Customer" });
			await repository.Insert(new Customer { Name = "B Customer" });

			// Act - Order with type-safe include
			var customers = await repository.Get(
				orderBy: q => q.OrderBy(c => c.Name),
				includeExpressions: c => c.Name
			);

			// Assert
			var customerList = customers.ToList();
			Assert.AreEqual(3, customerList.Count);
			Assert.AreEqual("A Customer", customerList[0].Name);
			Assert.AreEqual("B Customer", customerList[1].Name);
			Assert.AreEqual("C Customer", customerList[2].Name);
		}

		[TestMethod]
		public async Task Get_TypeSafeIncludeWithCancellationToken_RespectsToken()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			var customer = new Customer { Name = _customerName };
			await repository.Insert(customer);

			var cancellationToken = new System.Threading.CancellationToken(canceled: false);

			// Act & Assert - Should not throw
			var customers = await repository.Get(
				cancellationToken: cancellationToken,
				includeExpressions: c => c.Name
			);

			Assert.IsNotNull(customers);
			Assert.AreEqual(1, customers.Count());
		}

		[TestMethod]
		public async Task Get_TypeSafeIncludeWithFilterAndOrdering_WorksCorrectly()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			await repository.Insert(new Customer { Name = "Active Customer 3" });
			await repository.Insert(new Customer { Name = "Active Customer 1" });
			await repository.Insert(new Customer { Name = "Inactive Customer" });
			await repository.Insert(new Customer { Name = "Active Customer 2" });

			// Act - Filter, order, and include
			var customers = await repository.Get(
				filter: c => c.Name.StartsWith("Active"),
				orderBy: q => q.OrderBy(c => c.Name),
				includeExpressions: c => c.Name
			);

			// Assert
			var customerList = customers.ToList();
			Assert.AreEqual(3, customerList.Count);
			Assert.AreEqual("Active Customer 1", customerList[0].Name);
			Assert.AreEqual("Active Customer 2", customerList[1].Name);
			Assert.AreEqual("Active Customer 3", customerList[2].Name);
		}

		[TestMethod]
		public async Task Get_StringIncludeStillWorks_BackwardCompatibility()
		{
			// Arrange
			var repository = new MemoryGenericRepository<Customer>();
			var customer = new Customer { Name = _customerName };
			await repository.Insert(customer);

			// Act - Use old string-based include (backward compatibility)
			var customers = await repository.Get(
				filter: c => c.Name == _customerName,
				includeProperties: "Name"
			);

			// Assert
			var retrievedCustomer = customers.FirstOrDefault();
			Assert.IsNotNull(retrievedCustomer);
			Assert.AreEqual(_customerName, retrievedCustomer.Name);
		}
	}
}
