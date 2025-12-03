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
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            
            Assert.IsTrue(customer.ID > 0);
            Assert.IsFalse(customer.IsDeleted);
            Assert.IsNull(customer.DeletedAt);
            Assert.IsNull(customer.DeletedBy);
        }

        [TestMethod]
        public async Task Delete_Entity_MarksAsDeleted()
        {
            var context = CreateContext(nameof(Delete_Entity_MarksAsDeleted));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            
            var result = await repository.Delete(customer);
            
            Assert.IsTrue(result);
            Assert.IsTrue(customer.IsDeleted);
            Assert.IsNotNull(customer.DeletedAt);
            Assert.IsTrue((DateTime.UtcNow - customer.DeletedAt.Value).TotalSeconds < 2);
        }

        [TestMethod]
        public async Task Delete_ByID_MarksAsDeleted()
        {
            var context = CreateContext(nameof(Delete_ByID_MarksAsDeleted));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            var id = customer.ID;
            
            var result = await repository.Delete(id);
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task Get_ExcludesSoftDeletedEntities()
        {
            var context = CreateContext(nameof(Get_ExcludesSoftDeletedEntities));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer1 = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            var customer2 = await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" });
            
            await repository.Delete(customer1);
            
            var results = await repository.Get();
            
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("Jane Doe", results.First().Name);
        }

        [TestMethod]
        public async Task Get_WithFilter_ExcludesSoftDeletedEntities()
        {
            var context = CreateContext(nameof(Get_WithFilter_ExcludesSoftDeletedEntities));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer1 = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            var customer2 = await repository.Insert(new SoftDeletableCustomer { Name = "John Smith" });
            var customer3 = await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" });
            
            await repository.Delete(customer1);
            
            var results = await repository.Get(filter: c => c.Name.StartsWith("John"));
            
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("John Smith", results.First().Name);
        }

        [TestMethod]
        public async Task Get_ByID_ReturnNullForSoftDeletedEntity()
        {
            var context = CreateContext(nameof(Get_ByID_ReturnNullForSoftDeletedEntity));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            var id = customer.ID;
            
            await repository.Delete(customer);
            
            var result = await repository.Get(id);
            
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetIncludingDeleted_IncludesSoftDeletedEntities()
        {
            var context = CreateContext(nameof(GetIncludingDeleted_IncludesSoftDeletedEntities));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer1 = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            var customer2 = await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" });
            
            await repository.Delete(customer1);
            
            var results = await repository.GetIncludingDeleted();
            
            Assert.AreEqual(2, results.Count());
        }

        [TestMethod]
        public async Task GetIncludingDeleted_WithFilter_IncludesSoftDeletedEntities()
        {
            var context = CreateContext(nameof(GetIncludingDeleted_WithFilter_IncludesSoftDeletedEntities));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer1 = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            var customer2 = await repository.Insert(new SoftDeletableCustomer { Name = "John Smith" });
            
            await repository.Delete(customer1);
            
            var results = await repository.GetIncludingDeleted(filter: c => c.Name.StartsWith("John"));
            
            Assert.AreEqual(2, results.Count());
        }

        [TestMethod]
        public async Task Restore_RestoresSoftDeletedEntity()
        {
            var context = CreateContext(nameof(Restore_RestoresSoftDeletedEntity));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            
            await repository.Delete(customer);
            var restored = await repository.Restore(customer);
            
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
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            var id = customer.ID;
            
            await repository.Delete(customer);
            var restored = await repository.Restore(id);
            
            Assert.IsNotNull(restored);
            Assert.IsFalse(restored.IsDeleted);
        }

        [TestMethod]
        public async Task Restore_NonDeletedEntity_ReturnsEntityUnchanged()
        {
            var context = CreateContext(nameof(Restore_NonDeletedEntity_ReturnsEntityUnchanged));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            
            var restored = await repository.Restore(customer);
            
            Assert.IsNotNull(restored);
            Assert.IsFalse(restored.IsDeleted);
        }

        [TestMethod]
        public async Task PermanentlyDelete_RemovesEntityFromDatabase()
        {
            var context = CreateContext(nameof(PermanentlyDelete_RemovesEntityFromDatabase));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            
            await repository.Delete(customer);
            var result = await repository.PermanentlyDelete(customer);
            
            Assert.IsTrue(result);
            var all = await repository.GetIncludingDeleted();
            Assert.AreEqual(0, all.Count());
        }

        [TestMethod]
        public async Task PermanentlyDelete_ByID_RemovesEntityFromDatabase()
        {
            var context = CreateContext(nameof(PermanentlyDelete_ByID_RemovesEntityFromDatabase));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            var id = customer.ID;
            
            await repository.Delete(customer);
            var result = await repository.PermanentlyDelete(id);
            
            Assert.IsTrue(result);
            var all = await repository.GetIncludingDeleted();
            Assert.AreEqual(0, all.Count());
        }

        [TestMethod]
        public async Task DeleteRange_Entities_MarksAllAsDeleted()
        {
            var context = CreateContext(nameof(DeleteRange_Entities_MarksAllAsDeleted));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer1 = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            var customer2 = await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" });
            var customer3 = await repository.Insert(new SoftDeletableCustomer { Name = "Bob Smith" });
            
            var deletedCount = await repository.DeleteRange(new[] { customer1, customer2 });
            
            Assert.AreEqual(2, deletedCount);
            var remaining = await repository.Get();
            Assert.AreEqual(1, remaining.Count());
            Assert.AreEqual("Bob Smith", remaining.First().Name);
        }

        [TestMethod]
        public async Task DeleteRange_WithFilter_MarksMatchingEntitiesAsDeleted()
        {
            var context = CreateContext(nameof(DeleteRange_WithFilter_MarksMatchingEntitiesAsDeleted));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            await repository.Insert(new SoftDeletableCustomer { Name = "John Smith" });
            await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" });
            
            var deletedCount = await repository.DeleteRange(c => c.Name.StartsWith("John"));
            
            Assert.AreEqual(2, deletedCount);
            var remaining = await repository.Get();
            Assert.AreEqual(1, remaining.Count());
            Assert.AreEqual("Jane Doe", remaining.First().Name);
        }

        [TestMethod]
        public async Task GetAsyncEnumerable_ExcludesSoftDeletedEntities()
        {
            var context = CreateContext(nameof(GetAsyncEnumerable_ExcludesSoftDeletedEntities));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer1 = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            var customer2 = await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" });
            var customer3 = await repository.Insert(new SoftDeletableCustomer { Name = "Bob Smith" });
            
            await repository.Delete(customer1);
            
            var results = repository.GetAsyncEnumerable();
            var count = 0;
            await foreach (var customer in results)
            {
                count++;
                Assert.AreNotEqual("John Doe", customer.Name);
            }
            
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public async Task GetWithQuery_ExcludesSoftDeletedEntities()
        {
            var context = CreateContext(nameof(GetWithQuery_ExcludesSoftDeletedEntities));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer1 = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            var customer2 = await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" });
            
            await repository.Delete(customer1);
            
            var query = new Query<SoftDeletableCustomer>();
            var results = await repository.Get(query);
            
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("Jane Doe", results.First().Name);
        }

        [TestMethod]
        public async Task GetWithQuery_WithFilter_ExcludesSoftDeletedEntities()
        {
            var context = CreateContext(nameof(GetWithQuery_WithFilter_ExcludesSoftDeletedEntities));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            await repository.Insert(new SoftDeletableCustomer { Name = "John Smith" });
            var customer3 = await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" });
            
            await repository.Delete(customer3);
            
            var query = new Query<SoftDeletableCustomer>()
                .Where(c => c.Name.StartsWith("John"));
            var results = await repository.Get(query);
            
            Assert.AreEqual(2, results.Count());
        }

        [TestMethod]
        public async Task SetDeletedBy_RecordsWhoDeleted()
        {
            var context = CreateContext(nameof(SetDeletedBy_RecordsWhoDeleted));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            
            repository.SetDeletedBy("admin@example.com");
            await repository.Delete(customer);
            
            Assert.AreEqual("admin@example.com", customer.DeletedBy);
        }

        [TestMethod]
        public async Task Delete_WithCancellationToken_RespectsToken()
        {
            var context = CreateContext(nameof(Delete_WithCancellationToken_RespectsToken));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            
            var cts = new CancellationTokenSource();
            cts.Cancel();
            
            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
                await repository.Delete(customer, cts.Token));
        }

        [TestMethod]
        public async Task Get_TypeSafeInclude_ExcludesSoftDeletedEntities()
        {
            var context = CreateContext(nameof(Get_TypeSafeInclude_ExcludesSoftDeletedEntities));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            var customer1 = await repository.Insert(new SoftDeletableCustomer { Name = "John Doe" });
            var customer2 = await repository.Insert(new SoftDeletableCustomer { Name = "Jane Doe" });
            
            await repository.Delete(customer1);
            
            var results = await repository.Get(
                filter: null,
                orderBy: null,
                cancellationToken: default);
            
            Assert.AreEqual(1, results.Count());
        }

        [TestMethod]
        public async Task DeleteRange_NullCollection_ThrowsException()
        {
            var context = CreateContext(nameof(DeleteRange_NullCollection_ThrowsException));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await repository.DeleteRange((System.Collections.Generic.IEnumerable<SoftDeletableCustomer>)null!));
        }

        [TestMethod]
        public async Task DeleteRange_NullFilter_ThrowsException()
        {
            var context = CreateContext(nameof(DeleteRange_NullFilter_ThrowsException));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await repository.DeleteRange((System.Linq.Expressions.Expression<System.Func<SoftDeletableCustomer, bool>>)null!));
        }

        [TestMethod]
        public async Task Delete_NullEntity_ThrowsException()
        {
            var context = CreateContext(nameof(Delete_NullEntity_ThrowsException));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await repository.Delete((SoftDeletableCustomer)null!));
        }

        [TestMethod]
        public async Task PermanentlyDelete_NullEntity_ThrowsException()
        {
            var context = CreateContext(nameof(PermanentlyDelete_NullEntity_ThrowsException));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await repository.PermanentlyDelete((SoftDeletableCustomer)null!));
        }

        [TestMethod]
        public async Task Restore_NullEntity_ThrowsException()
        {
            var context = CreateContext(nameof(Restore_NullEntity_ThrowsException));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await repository.Restore((SoftDeletableCustomer)null!));
        }

        [TestMethod]
        public async Task Restore_NonExistentID_ReturnsNull()
        {
            var context = CreateContext(nameof(Restore_NonExistentID_ReturnsNull));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            var result = await repository.Restore(999);
            
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task PermanentlyDelete_NonExistentID_ReturnsFalse()
        {
            var context = CreateContext(nameof(PermanentlyDelete_NonExistentID_ReturnsFalse));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            var result = await repository.PermanentlyDelete(999);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task Delete_NonExistentID_ReturnsFalse()
        {
            var context = CreateContext(nameof(Delete_NonExistentID_ReturnsFalse));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            var result = await repository.Delete(999);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task GetWithQuery_NullQuery_ThrowsException()
        {
            var context = CreateContext(nameof(GetWithQuery_NullQuery_ThrowsException));
            var repository = new SoftDeleteEntityFrameworkCoreRepository<SoftDeletableCustomer, InMemoryDataContext>(context);
            
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await repository.Get((Query<SoftDeletableCustomer>)null!));
        }
    }
}
