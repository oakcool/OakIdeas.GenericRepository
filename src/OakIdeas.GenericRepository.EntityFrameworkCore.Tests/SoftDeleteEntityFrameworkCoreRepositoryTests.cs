using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Contexts;
using OakIdeas.GenericRepository.EntityFrameworkCore.Tests.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.EntityFrameworkCore.Tests
{
    [TestClass]
    public class SoftDeleteEntityFrameworkCoreRepositoryTests
    {
        private InMemoryDataContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<InMemoryDataContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new InMemoryDataContext(options);
        }

        [TestMethod]
        public async Task Insert_Entity_IsNotMarkedAsDeleted()
        {
            var context = CreateContext(nameof(Insert_Entity_IsNotMarkedAsDeleted));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);            
            Assert.IsGreaterThan(0,customer.ID);
            Assert.IsFalse(customer.IsDeleted);
            Assert.IsNull(customer.DeletedAt);
            Assert.IsNull(customer.DeletedBy);
        }

        [TestMethod]
        public async Task Delete_Entity_MarksAsDeleted()
        {
            var context = CreateContext(nameof(Delete_Entity_MarksAsDeleted));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            
            var result = await repository.Delete(customer, TestContext.CancellationToken);
            
            Assert.IsTrue(result);
            Assert.IsTrue(customer.IsDeleted);
            Assert.IsNotNull(customer.DeletedAt);
            Assert.IsLessThan(5, (DateTime.UtcNow - customer.DeletedAt!.Value).TotalSeconds, "DeletedAt should be set within 5 seconds of deletion.");
        }

        [TestMethod]
        public async Task Delete_ByID_MarksAsDeleted()
        {
            var context = CreateContext(nameof(Delete_ByID_MarksAsDeleted));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            var id = customer.ID;
            
            var result = await repository.Delete(id, TestContext.CancellationToken);
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task Get_ExcludesSoftDeletedEntities()
        {
            var context = CreateContext(nameof(Get_ExcludesSoftDeletedEntities));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer1 = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            var customer2 = await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" }, TestContext.CancellationToken);
            
            await repository.Delete(customer1, TestContext.CancellationToken);
            
            var results = await repository.Get(cancellationToken: TestContext.CancellationToken);
            
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("Jane Doe", results.First().Name);
        }

        [TestMethod]
        public async Task Get_WithFilter_ExcludesSoftDeletedEntities()
        {
            var context = CreateContext(nameof(Get_WithFilter_ExcludesSoftDeletedEntities));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer1 = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            var customer2 = await repository.Insert(new SoftDeletableCustomer { Name = "John Smith" }, TestContext.CancellationToken);
            var customer3 = await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" }, TestContext.CancellationToken);
            
            await repository.Delete(customer1, TestContext.CancellationToken);
            
            var results = await repository.Get(filter: c => c.Name.StartsWith("John"), cancellationToken: TestContext.CancellationToken);
            
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("John Smith", results.First().Name);
        }

        [TestMethod]
        public async Task Get_ByID_ReturnNullForSoftDeletedEntity()
        {
            var context = CreateContext(nameof(Get_ByID_ReturnNullForSoftDeletedEntity));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            var id = customer.ID;
            
            await repository.Delete(customer, TestContext.CancellationToken);
            
            var result = await repository.Get(id, TestContext.CancellationToken);
            
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetIncludingDeleted_IncludesSoftDeletedEntities()
        {
            var context = CreateContext(nameof(GetIncludingDeleted_IncludesSoftDeletedEntities));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer1 = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            var customer2 = await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" }, TestContext.CancellationToken);
            
            await repository.Delete(customer1, TestContext.CancellationToken);
            
            var results = await repository.GetIncludingDeleted(cancellationToken: TestContext.CancellationToken);
            
            Assert.AreEqual(2, results.Count());
        }

        [TestMethod]
        public async Task GetIncludingDeleted_WithFilter_IncludesSoftDeletedEntities()
        {
            var context = CreateContext(nameof(GetIncludingDeleted_WithFilter_IncludesSoftDeletedEntities));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer1 = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            var customer2 = await repository.Insert(new SoftDeletableCustomer { Name = "John Smith" }, TestContext.CancellationToken);
            
            await repository.Delete(customer1, TestContext.CancellationToken);
            
            var results = await repository.GetIncludingDeleted(filter: c => c.Name.StartsWith("John"), cancellationToken: TestContext.CancellationToken);
            
            Assert.AreEqual(2, results.Count());
        }

        [TestMethod]
        public async Task Restore_RestoresSoftDeletedEntity()
        {
            var context = CreateContext(nameof(Restore_RestoresSoftDeletedEntity));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            
            await repository.Delete(customer, TestContext.CancellationToken);
            var restored = await repository.Restore(customer, TestContext.CancellationToken);
            
            Assert.IsNotNull(restored);
            Assert.IsFalse(restored.IsDeleted);
            Assert.IsNull(restored.DeletedAt);
            Assert.IsNull(restored.DeletedBy);
        }

        [TestMethod]
        public async Task Restore_ByID_RestoresSoftDeletedEntity()
        {
            var context = CreateContext(nameof(Restore_ByID_RestoresSoftDeletedEntity));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            var id = customer.ID;
            
            await repository.Delete(customer, TestContext.CancellationToken);
            var restored = await repository.Restore(id, TestContext.CancellationToken);
            
            Assert.IsNotNull(restored);
            Assert.IsFalse(restored.IsDeleted);
        }

        [TestMethod]
        public async Task Restore_NonDeletedEntity_ReturnsEntityUnchanged()
        {
            var context = CreateContext(nameof(Restore_NonDeletedEntity_ReturnsEntityUnchanged));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            
            var restored = await repository.Restore(customer, TestContext.CancellationToken);
            
            Assert.IsNotNull(restored);
            Assert.IsFalse(restored.IsDeleted);
        }

        [TestMethod]
        public async Task PermanentlyDelete_RemovesEntityFromDatabase()
        {
            var context = CreateContext(nameof(PermanentlyDelete_RemovesEntityFromDatabase));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            
            await repository.Delete(customer, TestContext.CancellationToken);
            var result = await repository.PermanentlyDelete(customer, TestContext.CancellationToken);
            
            Assert.IsTrue(result);
            var all = await repository.GetIncludingDeleted(cancellationToken: TestContext.CancellationToken);
            Assert.AreEqual(0, all.Count());
        }

        [TestMethod]
        public async Task PermanentlyDelete_ByID_RemovesEntityFromDatabase()
        {
            var context = CreateContext(nameof(PermanentlyDelete_ByID_RemovesEntityFromDatabase));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            var id = customer.ID;
            
            await repository.Delete(customer, TestContext.CancellationToken);
            var result = await repository.PermanentlyDelete(id, TestContext.CancellationToken);
            
            Assert.IsTrue(result);
            var all = await repository.GetIncludingDeleted(cancellationToken: TestContext.CancellationToken);
            Assert.AreEqual(0, all.Count());
        }

        [TestMethod]
        public async Task DeleteRange_Entities_MarksAllAsDeleted()
        {
            var context = CreateContext(nameof(DeleteRange_Entities_MarksAllAsDeleted));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer1 = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            var customer2 = await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" }, TestContext.CancellationToken);
            var customer3 = await repository.Insert(new SoftDeletableCustomer { Name = "Bob Smith" }, TestContext.CancellationToken);
            
            var deletedCount = await repository.DeleteRange([customer1, customer2], TestContext.CancellationToken);
            
            Assert.AreEqual(2, deletedCount);
            var remaining = await repository.Get(cancellationToken: TestContext.CancellationToken);
            Assert.AreEqual(1, remaining.Count());
            Assert.AreEqual("Bob Smith", remaining.First().Name);
        }

        [TestMethod]
        public async Task DeleteRange_WithFilter_MarksMatchingEntitiesAsDeleted()
        {
            var context = CreateContext(nameof(DeleteRange_WithFilter_MarksMatchingEntitiesAsDeleted));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            await repository.Insert(new SoftDeletableCustomer { Name = "John Smith" }, TestContext.CancellationToken);
            await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" }, TestContext.CancellationToken);
            
            var deletedCount = await repository.DeleteRange(c => c.Name.StartsWith("John"), TestContext.CancellationToken);
            
            Assert.AreEqual(2, deletedCount, "Two customers should be marked as deleted.");
            var remaining = await repository.Get(cancellationToken: TestContext.CancellationToken);
            Assert.AreEqual(1, remaining.Count(), "Only one customer should remain.");
            Assert.AreEqual("Jane Doe", remaining.First().Name, "The remaining customer should be Jane Doe.");
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_ExcludesSoftDeletedEntities()
        {
            var context = CreateContext(nameof(GetAsyncEnumerable_ExcludesSoftDeletedEntities));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer1 = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            var customer2 = await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" }, TestContext.CancellationToken);
            var customer3 = await repository.Insert(new SoftDeletableCustomer { Name = "Bob Smith" }, TestContext.CancellationToken);
            
            await repository.Delete(customer1, TestContext.CancellationToken);
            
            var results = repository.GetAsyncEnumerable(cancellationToken: TestContext.CancellationToken);
            var count = 0;
            await foreach (var customer in results)
            {
                count++;
            }
            
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public async Task GetWithQuery_ExcludesSoftDeletedEntities()
        {
            var context = CreateContext(nameof(GetWithQuery_ExcludesSoftDeletedEntities));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer1 = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            var customer2 = await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" }, TestContext.CancellationToken);
            
            await repository.Delete(customer1, TestContext.CancellationToken);
            
            var query = new Query<SoftDeletableCustomer>();
            var results = await repository.Get(query, TestContext.CancellationToken);
            
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("Jane Doe", results.First().Name);
        }

        [TestMethod]
        public async Task GetWithQuery_WithFilter_ExcludesSoftDeletedEntities()
        {
            var context = CreateContext(nameof(GetWithQuery_WithFilter_ExcludesSoftDeletedEntities));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            await repository.Insert(new SoftDeletableCustomer { Name = "John Smith" }, TestContext.CancellationToken);
            var customer3 = await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" }, TestContext.CancellationToken);
            
            await repository.Delete(customer3, TestContext.CancellationToken);
            
            var query = new Query<SoftDeletableCustomer>()
                .Where(c => c.Name.StartsWith("John"));
            var results = await repository.Get(query, TestContext.CancellationToken);
            
            Assert.AreEqual(2, results.Count());
        }

        [TestMethod]
        public async Task SetDeletedBy_RecordsWhoDeleted()
        {
            var context = CreateContext(nameof(SetDeletedBy_RecordsWhoDeleted));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            
            repository.SetDeletedBy("admin@example.com");
            await repository.Delete(customer, TestContext.CancellationToken);
            
            Assert.AreEqual("admin@example.com", customer.DeletedBy);
        }

        [TestMethod]
        public async Task Delete_WithCancellationToken_RespectsToken()
        {
            var context = CreateContext(nameof(Delete_WithCancellationToken_RespectsToken));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            
            var cts = new CancellationTokenSource();
            cts.Cancel();
            
            await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
                await repository.Delete(customer, cts.Token));
        }

        [TestMethod]
        public async Task Get_TypeSafeInclude_ExcludesSoftDeletedEntities()
        {
            var context = CreateContext(nameof(Get_TypeSafeInclude_ExcludesSoftDeletedEntities));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer1 = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" }, TestContext.CancellationToken);
            var customer2 = await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" }, TestContext.CancellationToken);
            
            await repository.Delete(customer1, TestContext.CancellationToken);
            
            var results = await repository.Get(
                filter: null,
                orderBy: null,
                cancellationToken: TestContext.CancellationToken);
            
            Assert.AreEqual(1, results.Count());
        }

        [TestMethod]
        public async Task DeleteRange_NullCollection_ThrowsException()
        {
            var context = CreateContext(nameof(DeleteRange_NullCollection_ThrowsException));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
                await repository.DeleteRange((System.Collections.Generic.IEnumerable<SoftDeletableCustomer>)null!, TestContext.CancellationToken));
        }

        [TestMethod]
        public async Task DeleteRange_NullFilter_ThrowsException()
        {
            var context = CreateContext(nameof(DeleteRange_NullFilter_ThrowsException));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
                await repository.DeleteRange((System.Linq.Expressions.Expression<System.Func<SoftDeletableCustomer, bool>>)null!, TestContext.CancellationToken));
        }

        [TestMethod]
        public async Task Delete_NullEntity_ThrowsException()
        {
            var context = CreateContext(nameof(Delete_NullEntity_ThrowsException));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
                await repository.Delete((SoftDeletableCustomer)null!, TestContext.CancellationToken));
        }

        [TestMethod]
        public async Task PermanentlyDelete_NullEntity_ThrowsException()
        {
            var context = CreateContext(nameof(PermanentlyDelete_NullEntity_ThrowsException));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
                await repository.PermanentlyDelete((SoftDeletableCustomer)null!, TestContext.CancellationToken));
        }

        [TestMethod]
        public async Task Restore_NullEntity_ThrowsException()
        {
            var context = CreateContext(nameof(Restore_NullEntity_ThrowsException));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
                await repository.Restore((SoftDeletableCustomer)null!, TestContext.CancellationToken));
        }

        [TestMethod]
        public async Task Restore_NonExistentID_ReturnsNull()
        {
            var context = CreateContext(nameof(Restore_NonExistentID_ReturnsNull));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            var result = await repository.Restore(999, TestContext.CancellationToken);
            
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task PermanentlyDelete_NonExistentID_ReturnsFalse()
        {
            var context = CreateContext(nameof(PermanentlyDelete_NonExistentID_ReturnsFalse));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            var result = await repository.PermanentlyDelete(999, TestContext.CancellationToken);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task Delete_NonExistentID_ReturnsFalse()
        {
            var context = CreateContext(nameof(Delete_NonExistentID_ReturnsFalse));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            var result = await repository.Delete(999, TestContext.CancellationToken);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task GetWithQuery_NullQuery_ThrowsException()
        {
            var context = CreateContext(nameof(GetWithQuery_NullQuery_ThrowsException));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
                await repository.Get((Query<SoftDeletableCustomer>)null!, TestContext.CancellationToken));
        }

        public TestContext TestContext { get; set; }
    }
}
