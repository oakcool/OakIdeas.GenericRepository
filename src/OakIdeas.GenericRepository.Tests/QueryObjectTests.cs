using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository;
using OakIdeas.GenericRepository.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Tests;

/// <summary>
/// Tests for the Query object pattern implementation
/// </summary>
[TestClass]
public class QueryObjectTests
{
    private class TestEntity : EntityBase
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public bool IsActive { get; set; }
    }

    [TestMethod]
    public void Query_DefaultConstructor_InitializesCorrectly()
    {
        // Arrange & Act
        var query = new Query<TestEntity>();

        // Assert
        Assert.IsNull(query.Filter);
        Assert.IsNull(query.OrderBy);
        Assert.IsNull(query.Includes);
        Assert.IsNull(query.Page);
        Assert.IsNull(query.PageSize);
        Assert.IsFalse(query.AsNoTracking);
    }

    [TestMethod]
    public void Query_Where_SetsFilter()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Act
        var result = query.Where(e => e.IsActive);

        // Assert
        Assert.IsNotNull(query.Filter);
        Assert.AreSame(query, result); // Fluent API
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Query_Where_NullFilter_ThrowsException()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Act & Assert
        query.Where(null!);
    }

    [TestMethod]
    public void Query_Sort_SetsOrderBy()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Act
        var result = query.Sort(q => q.OrderBy(e => e.Name));

        // Assert
        Assert.IsNotNull(query.OrderBy);
        Assert.AreSame(query, result); // Fluent API
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Query_Sort_NullOrderBy_ThrowsException()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Act & Assert
        query.Sort(null!);
    }

    [TestMethod]
    public void Query_Include_AddsToIncludes()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Act
        var result = query.Include(e => e.Name);

        // Assert
        Assert.IsNotNull(query.Includes);
        Assert.AreEqual(1, query.Includes.Count);
        Assert.AreSame(query, result); // Fluent API
    }

    [TestMethod]
    public void Query_Include_MultipleIncludes_AddsAll()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Act
        query.Include(e => e.Name)
             .Include(e => e.Value);

        // Assert
        Assert.IsNotNull(query.Includes);
        Assert.AreEqual(2, query.Includes.Count);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Query_Include_NullExpression_ThrowsException()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Act & Assert
        query.Include(null!);
    }

    [TestMethod]
    public void Query_Paged_SetsPageAndPageSize()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Act
        var result = query.Paged(2, 10);

        // Assert
        Assert.AreEqual(2, query.Page);
        Assert.AreEqual(10, query.PageSize);
        Assert.AreSame(query, result); // Fluent API
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void Query_Paged_InvalidPage_ThrowsException()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Act & Assert
        query.Paged(0, 10);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void Query_Paged_InvalidPageNegative_ThrowsException()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Act & Assert
        query.Paged(-1, 10);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void Query_Paged_InvalidPageSize_ThrowsException()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Act & Assert
        query.Paged(1, 0);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void Query_Paged_InvalidPageSizeNegative_ThrowsException()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Act & Assert
        query.Paged(1, -1);
    }

    [TestMethod]
    public void Query_Skip_CalculatesCorrectly()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Act
        query.Paged(1, 10);

        // Assert
        Assert.AreEqual(0, query.Skip);

        // Act
        query.Paged(2, 10);

        // Assert
        Assert.AreEqual(10, query.Skip);

        // Act
        query.Paged(3, 20);

        // Assert
        Assert.AreEqual(40, query.Skip);
    }

    [TestMethod]
    public void Query_Skip_NoPagination_ReturnsNull()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Assert
        Assert.IsNull(query.Skip);
    }

    [TestMethod]
    public void Query_Take_ReturnsPageSize()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Act
        query.Paged(1, 10);

        // Assert
        Assert.AreEqual(10, query.Take);
    }

    [TestMethod]
    public void Query_Take_NoPagination_ReturnsNull()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Assert
        Assert.IsNull(query.Take);
    }

    [TestMethod]
    public void Query_WithNoTracking_SetsAsNoTracking()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Act
        var result = query.WithNoTracking();

        // Assert
        Assert.IsTrue(query.AsNoTracking);
        Assert.AreSame(query, result); // Fluent API
    }

    [TestMethod]
    public void Query_WithNoTracking_False_SetsAsNoTrackingFalse()
    {
        // Arrange
        var query = new Query<TestEntity>();

        // Act
        var result = query.WithNoTracking(false);

        // Assert
        Assert.IsFalse(query.AsNoTracking);
        Assert.AreSame(query, result); // Fluent API
    }

    [TestMethod]
    public void Query_FluentAPI_ChainsCorrectly()
    {
        // Arrange & Act
        var query = new Query<TestEntity>()
            .Where(e => e.IsActive)
            .Sort(q => q.OrderBy(e => e.Name))
            .Include(e => e.Value)
            .Paged(2, 20)
            .WithNoTracking();

        // Assert
        Assert.IsNotNull(query.Filter);
        Assert.IsNotNull(query.OrderBy);
        Assert.IsNotNull(query.Includes);
        Assert.AreEqual(1, query.Includes.Count);
        Assert.AreEqual(2, query.Page);
        Assert.AreEqual(20, query.PageSize);
        Assert.IsTrue(query.AsNoTracking);
        Assert.AreEqual(20, query.Skip);
        Assert.AreEqual(20, query.Take);
    }

    [TestMethod]
    public async Task MemoryRepository_GetWithQuery_WithFilter_ReturnsFiltered()
    {
        // Arrange
        var repository = new MemoryGenericRepository<TestEntity>();
        await repository.Insert(new TestEntity { Name = "Active1", IsActive = true });
        await repository.Insert(new TestEntity { Name = "Inactive1", IsActive = false });
        await repository.Insert(new TestEntity { Name = "Active2", IsActive = true });

        var query = new Query<TestEntity>()
            .Where(e => e.IsActive);

        // Act
        var results = await repository.Get(query);

        // Assert
        Assert.AreEqual(2, results.Count());
        Assert.IsTrue(results.All(e => e.IsActive));
    }

    [TestMethod]
    public async Task MemoryRepository_GetWithQuery_WithOrdering_ReturnsOrdered()
    {
        // Arrange
        var repository = new MemoryGenericRepository<TestEntity>();
        await repository.Insert(new TestEntity { Name = "Charlie" });
        await repository.Insert(new TestEntity { Name = "Alice" });
        await repository.Insert(new TestEntity { Name = "Bob" });

        var query = new Query<TestEntity>()
            .Sort(q => q.OrderBy(e => e.Name));

        // Act
        var results = await repository.Get(query);

        // Assert
        var names = results.Select(e => e.Name).ToArray();
        CollectionAssert.AreEqual(new[] { "Alice", "Bob", "Charlie" }, names);
    }

    [TestMethod]
    public async Task MemoryRepository_GetWithQuery_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        var repository = new MemoryGenericRepository<TestEntity>();
        for (int i = 1; i <= 25; i++)
        {
            await repository.Insert(new TestEntity { Name = $"Item{i}", Value = i });
        }

        var query = new Query<TestEntity>()
            .Sort(q => q.OrderBy(e => e.Value))
            .Paged(2, 10);

        // Act
        var results = await repository.Get(query);

        // Assert
        Assert.AreEqual(10, results.Count());
        Assert.AreEqual(11, results.First().Value); // Second page starts at item 11
        Assert.AreEqual(20, results.Last().Value);
    }

    [TestMethod]
    public async Task MemoryRepository_GetWithQuery_ComplexQuery_Works()
    {
        // Arrange
        var repository = new MemoryGenericRepository<TestEntity>();
        for (int i = 1; i <= 50; i++)
        {
            await repository.Insert(new TestEntity 
            { 
                Name = $"Item{i}", 
                Value = i,
                IsActive = i % 2 == 0
            });
        }

        var query = new Query<TestEntity>()
            .Where(e => e.IsActive && e.Value > 10)
            .Sort(q => q.OrderByDescending(e => e.Value))
            .Paged(1, 5);

        // Act
        var results = await repository.Get(query);

        // Assert
        Assert.AreEqual(5, results.Count());
        Assert.IsTrue(results.All(e => e.IsActive));
        Assert.IsTrue(results.All(e => e.Value > 10));
        // Check descending order
        var values = results.Select(e => e.Value).ToList();
        CollectionAssert.AreEqual(values.OrderByDescending(v => v).ToList(), values);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task MemoryRepository_GetWithQuery_NullQuery_ThrowsException()
    {
        // Arrange
        var repository = new MemoryGenericRepository<TestEntity>();

        // Act & Assert
        await repository.Get((Query<TestEntity>)null!);
    }

    [TestMethod]
    public async Task MemoryRepository_GetWithQuery_EmptyQuery_ReturnsAll()
    {
        // Arrange
        var repository = new MemoryGenericRepository<TestEntity>();
        await repository.Insert(new TestEntity { Name = "Item1" });
        await repository.Insert(new TestEntity { Name = "Item2" });
        await repository.Insert(new TestEntity { Name = "Item3" });

        var query = new Query<TestEntity>();

        // Act
        var results = await repository.Get(query);

        // Assert
        Assert.AreEqual(3, results.Count());
    }

    [TestMethod]
    public async Task MemoryRepository_GetWithQuery_QueryReusability_Works()
    {
        // Arrange
        var repository = new MemoryGenericRepository<TestEntity>();
        for (int i = 1; i <= 20; i++)
        {
            await repository.Insert(new TestEntity 
            { 
                Name = $"Item{i}", 
                Value = i,
                IsActive = i % 2 == 0
            });
        }

        // Create a reusable query
        var activeItemsQuery = new Query<TestEntity>()
            .Where(e => e.IsActive)
            .Sort(q => q.OrderBy(e => e.Value));

        // Act - Use the query multiple times
        var allActive = await repository.Get(activeItemsQuery);
        
        // Modify for pagination
        activeItemsQuery.Paged(1, 5);
        var firstPage = await repository.Get(activeItemsQuery);

        activeItemsQuery.Paged(2, 5);
        var secondPage = await repository.Get(activeItemsQuery);

        // Assert
        Assert.AreEqual(10, allActive.Count()); // All active items
        Assert.AreEqual(5, firstPage.Count()); // First 5 active items
        Assert.AreEqual(5, secondPage.Count()); // Next 5 active items
        Assert.AreNotEqual(firstPage.First().Value, secondPage.First().Value);
    }
}
