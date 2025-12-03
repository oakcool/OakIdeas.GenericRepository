using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.Memory;
using OakIdeas.GenericRepository.Tests.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Tests
{
    [TestClass]
    public class CancellationTokenTests
    {
        private readonly string _entityDefaultName = "Test Customer";

        [TestMethod]
        public async Task Insert_WithCancellationToken_ShouldSucceed()
        {
            // Arrange
            var repository = new MemoryGenericRepository<Customer>();
            var cts = new CancellationTokenSource();
            var customer = new Customer { Name = _entityDefaultName };

            // Act
            var result = await repository.Insert(customer, cts.Token);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.ID > 0);
            Assert.AreEqual(_entityDefaultName, result.Name);
        }

        [TestMethod]
        public async Task Insert_WithCancelledToken_ShouldThrow()
        {
            // Arrange
            var repository = new MemoryGenericRepository<Customer>();
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var customer = new Customer { Name = _entityDefaultName };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                async () => await repository.Insert(customer, cts.Token)
            );
        }

        [TestMethod]
        public async Task Get_WithCancellationToken_ShouldSucceed()
        {
            // Arrange
            var repository = new MemoryGenericRepository<Customer>();
            var cts = new CancellationTokenSource();
            var customer = await repository.Insert(new Customer { Name = _entityDefaultName });

            // Act
            var result = await repository.Get(customer.ID, cts.Token);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_entityDefaultName, result.Name);
        }

        [TestMethod]
        public async Task Get_WithCancelledToken_ShouldThrow()
        {
            // Arrange
            var repository = new MemoryGenericRepository<Customer>();
            var customer = await repository.Insert(new Customer { Name = _entityDefaultName });
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                async () => await repository.Get(customer.ID, cts.Token)
            );
        }

        [TestMethod]
        public async Task GetAll_WithCancellationToken_ShouldSucceed()
        {
            // Arrange
            var repository = new MemoryGenericRepository<Customer>();
            var cts = new CancellationTokenSource();
            await repository.Insert(new Customer { Name = _entityDefaultName });

            // Act
            var result = await repository.Get(cancellationToken: cts.Token);

            // Assert
            Assert.IsTrue(result.Count() > 0);
        }

        [TestMethod]
        public async Task GetAll_WithCancelledToken_ShouldThrow()
        {
            // Arrange
            var repository = new MemoryGenericRepository<Customer>();
            await repository.Insert(new Customer { Name = _entityDefaultName });
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                async () => await repository.Get(cancellationToken: cts.Token)
            );
        }

        [TestMethod]
        public async Task Update_WithCancellationToken_ShouldSucceed()
        {
            // Arrange
            var repository = new MemoryGenericRepository<Customer>();
            var cts = new CancellationTokenSource();
            var customer = await repository.Insert(new Customer { Name = _entityDefaultName });
            customer.Name = "Updated Name";

            // Act
            var result = await repository.Update(customer, cts.Token);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Updated Name", result.Name);
        }

        [TestMethod]
        public async Task Update_WithCancelledToken_ShouldThrow()
        {
            // Arrange
            var repository = new MemoryGenericRepository<Customer>();
            var customer = await repository.Insert(new Customer { Name = _entityDefaultName });
            customer.Name = "Updated Name";
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                async () => await repository.Update(customer, cts.Token)
            );
        }

        [TestMethod]
        public async Task Delete_WithCancellationToken_ShouldSucceed()
        {
            // Arrange
            var repository = new MemoryGenericRepository<Customer>();
            var cts = new CancellationTokenSource();
            var customer = await repository.Insert(new Customer { Name = _entityDefaultName });

            // Act
            var result = await repository.Delete(customer, cts.Token);

            // Assert
            Assert.IsTrue(result);
            var deletedCustomer = await repository.Get(customer.ID);
            Assert.IsNull(deletedCustomer);
        }

        [TestMethod]
        public async Task Delete_WithCancelledToken_ShouldThrow()
        {
            // Arrange
            var repository = new MemoryGenericRepository<Customer>();
            var customer = await repository.Insert(new Customer { Name = _entityDefaultName });
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                async () => await repository.Delete(customer, cts.Token)
            );
        }

        [TestMethod]
        public async Task DeleteById_WithCancellationToken_ShouldSucceed()
        {
            // Arrange
            var repository = new MemoryGenericRepository<Customer>();
            var cts = new CancellationTokenSource();
            var customer = await repository.Insert(new Customer { Name = _entityDefaultName });

            // Act
            var result = await repository.Delete(customer.ID, cts.Token);

            // Assert
            Assert.IsTrue(result);
            var deletedCustomer = await repository.Get(customer.ID);
            Assert.IsNull(deletedCustomer);
        }

        [TestMethod]
        public async Task DeleteById_WithCancelledToken_ShouldThrow()
        {
            // Arrange
            var repository = new MemoryGenericRepository<Customer>();
            var customer = await repository.Insert(new Customer { Name = _entityDefaultName });
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                async () => await repository.Delete(customer.ID, cts.Token)
            );
        }

        [TestMethod]
        public async Task GetFiltered_WithCancellationToken_ShouldSucceed()
        {
            // Arrange
            var repository = new MemoryGenericRepository<Customer>();
            var cts = new CancellationTokenSource();
            await repository.Insert(new Customer { Name = "Customer A" });
            await repository.Insert(new Customer { Name = "Customer B" });

            // Act
            var result = await repository.Get(
                filter: c => c.Name == "Customer A",
                cancellationToken: cts.Token);

            // Assert
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("Customer A", result.First().Name);
        }

        [TestMethod]
        public async Task GetFiltered_WithCancelledToken_ShouldThrow()
        {
            // Arrange
            var repository = new MemoryGenericRepository<Customer>();
            await repository.Insert(new Customer { Name = "Customer A" });
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                async () => await repository.Get(
                    filter: c => c.Name == "Customer A",
                    cancellationToken: cts.Token)
            );
        }

        [TestMethod]
        public async Task MultipleOperations_WithSameCancellationToken_ShouldSucceed()
        {
            // Arrange
            var repository = new MemoryGenericRepository<Customer>();
            var cts = new CancellationTokenSource();

            // Act
            var customer1 = await repository.Insert(new Customer { Name = "Customer 1" }, cts.Token);
            var customer2 = await repository.Insert(new Customer { Name = "Customer 2" }, cts.Token);
            var retrieved = await repository.Get(customer1.ID, cts.Token);
            customer1.Name = "Updated Customer 1";
            await repository.Update(customer1, cts.Token);
            var allCustomers = await repository.Get(cancellationToken: cts.Token);

            // Assert
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(2, allCustomers.Count());
            Assert.IsTrue(allCustomers.Any(c => c.Name == "Updated Customer 1"));
        }

        [TestMethod]
        public async Task DefaultCancellationToken_ShouldWorkAsExpected()
        {
            // Arrange
            var repository = new MemoryGenericRepository<Customer>();

            // Act - Use default CancellationToken (none)
            var customer = await repository.Insert(new Customer { Name = _entityDefaultName });
            var retrieved = await repository.Get(customer.ID);
            customer.Name = "Updated";
            await repository.Update(customer);
            var all = await repository.Get();
            await repository.Delete(customer.ID);

            // Assert - All operations should complete successfully
            Assert.IsNotNull(customer);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(1, all.Count());
        }
    }
}
