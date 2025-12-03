using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.Specifications;
using OakIdeas.GenericRepository.Tests.Models;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Tests;

/// <summary>
/// Integration tests demonstrating how specifications work with repositories.
/// </summary>
[TestClass]
public class SpecificationIntegrationTests
{
    // Example specifications for testing
    private class NameStartsWithSpecification : Specification<Customer>
    {
        private readonly string _prefix;

        public NameStartsWithSpecification(string prefix)
        {
            _prefix = prefix;
        }

        public override Expression<Func<Customer, bool>> ToExpression()
        {
            return c => c.Name.StartsWith(_prefix);
        }
    }

    private class ActiveCustomerSpecification : Specification<Customer>
    {
        public override Expression<Func<Customer, bool>> ToExpression()
        {
            return c => c.ID > 0;
        }
    }

    private class NameContainsSpecification : Specification<Customer>
    {
        private readonly string _substring;

        public NameContainsSpecification(string substring)
        {
            _substring = substring;
        }

        public override Expression<Func<Customer, bool>> ToExpression()
        {
            return c => c.Name.Contains(_substring);
        }
    }

    [TestMethod]
    public async Task Repository_WithBasicSpecification_ReturnsMatchingEntities()
    {
        // Arrange
        var repository = new MemoryGenericRepository<Customer>();
        await repository.Insert(new Customer { Name = "John Doe" });
        await repository.Insert(new Customer { Name = "Jane Doe" });
        await repository.Insert(new Customer { Name = "Bob Smith" });

        var spec = new NameStartsWithSpecification("John");

        // Act
        var results = await repository.Get(filter: spec.ToExpression());

        // Assert
        Assert.AreEqual(1, results.Count());
        Assert.AreEqual("John Doe", results.First().Name);
    }

    [TestMethod]
    public async Task Repository_WithAndSpecification_ReturnsMatchingEntities()
    {
        // Arrange
        var repository = new MemoryGenericRepository<Customer>();
        await repository.Insert(new Customer { Name = "John Doe" });
        await repository.Insert(new Customer { Name = "John Smith" });
        await repository.Insert(new Customer { Name = "Jane Doe" });

        var spec = new NameStartsWithSpecification("John")
            .And(new NameContainsSpecification("Doe"));

        // Act
        var results = await repository.Get(filter: spec.ToExpression());

        // Assert
        Assert.AreEqual(1, results.Count());
        Assert.AreEqual("John Doe", results.First().Name);
    }

    [TestMethod]
    public async Task Repository_WithOrSpecification_ReturnsMatchingEntities()
    {
        // Arrange
        var repository = new MemoryGenericRepository<Customer>();
        await repository.Insert(new Customer { Name = "John Doe" });
        await repository.Insert(new Customer { Name = "Jane Smith" });
        await repository.Insert(new Customer { Name = "Bob Johnson" });

        var spec = new NameStartsWithSpecification("John")
            .Or(new NameStartsWithSpecification("Jane"));

        // Act
        var results = await repository.Get(filter: spec.ToExpression());

        // Assert
        Assert.AreEqual(2, results.Count());
        Assert.IsTrue(results.Any(c => c.Name == "John Doe"));
        Assert.IsTrue(results.Any(c => c.Name == "Jane Smith"));
    }

    [TestMethod]
    public async Task Repository_WithNotSpecification_ReturnsNonMatchingEntities()
    {
        // Arrange
        var repository = new MemoryGenericRepository<Customer>();
        await repository.Insert(new Customer { Name = "John Doe" });
        await repository.Insert(new Customer { Name = "Jane Doe" });
        await repository.Insert(new Customer { Name = "Bob Smith" });

        var spec = new NameStartsWithSpecification("John").Not();

        // Act
        var results = await repository.Get(filter: spec.ToExpression());

        // Assert
        Assert.AreEqual(2, results.Count());
        Assert.IsFalse(results.Any(c => c.Name == "John Doe"));
    }

    [TestMethod]
    public async Task Repository_WithComplexSpecification_ReturnsMatchingEntities()
    {
        // Arrange
        var repository = new MemoryGenericRepository<Customer>();
        await repository.Insert(new Customer { Name = "John Doe" });
        await repository.Insert(new Customer { Name = "John Smith" });
        await repository.Insert(new Customer { Name = "Jane Doe" });
        await repository.Insert(new Customer { Name = "Bob Johnson" });

        // (StartsWith "John" AND Contains "Doe") OR StartsWith "Jane"
        var spec = new NameStartsWithSpecification("John")
            .And(new NameContainsSpecification("Doe"))
            .Or(new NameStartsWithSpecification("Jane"));

        // Act
        var results = await repository.Get(filter: spec.ToExpression());

        // Assert
        Assert.AreEqual(2, results.Count());
        Assert.IsTrue(results.Any(c => c.Name == "John Doe"));
        Assert.IsTrue(results.Any(c => c.Name == "Jane Doe"));
    }

    [TestMethod]
    public async Task Repository_WithSpecificationAndOrdering_ReturnsOrderedResults()
    {
        // Arrange
        var repository = new MemoryGenericRepository<Customer>();
        await repository.Insert(new Customer { Name = "John Smith" });
        await repository.Insert(new Customer { Name = "John Doe" });
        await repository.Insert(new Customer { Name = "John Adams" });
        await repository.Insert(new Customer { Name = "Jane Doe" });

        var spec = new NameStartsWithSpecification("John");

        // Act
        var results = await repository.Get(
            filter: spec.ToExpression(),
            orderBy: q => q.OrderBy(c => c.Name));

        // Assert
        Assert.AreEqual(3, results.Count());
        var resultList = results.ToList();
        Assert.AreEqual("John Adams", resultList[0].Name);
        Assert.AreEqual("John Doe", resultList[1].Name);
        Assert.AreEqual("John Smith", resultList[2].Name);
    }

    [TestMethod]
    public async Task Repository_WithImplicitConversion_Works()
    {
        // Arrange
        var repository = new MemoryGenericRepository<Customer>();
        await repository.Insert(new Customer { Name = "John Doe" });
        await repository.Insert(new Customer { Name = "Jane Doe" });

        var spec = new NameStartsWithSpecification("John");

        // Act - implicit conversion from Specification<Customer> to Expression<Func<Customer, bool>>
        var results = await repository.Get(filter: spec);

        // Assert
        Assert.AreEqual(1, results.Count());
        Assert.AreEqual("John Doe", results.First().Name);
    }

    [TestMethod]
    public async Task Repository_ReuseSpecification_ConsistentResults()
    {
        // Arrange
        var repository = new MemoryGenericRepository<Customer>();
        await repository.Insert(new Customer { Name = "John Doe" });
        await repository.Insert(new Customer { Name = "Jane Doe" });
        await repository.Insert(new Customer { Name = "John Smith" });

        var spec = new NameStartsWithSpecification("John");

        // Act - use the same specification multiple times
        var results1 = await repository.Get(filter: spec.ToExpression());
        var results2 = await repository.Get(filter: spec.ToExpression());

        // Assert
        Assert.AreEqual(2, results1.Count());
        Assert.AreEqual(2, results2.Count());
        Assert.AreEqual(results1.Count(), results2.Count());
    }

    [TestMethod]
    public async Task Repository_CombineMultipleSpecifications_Dynamically()
    {
        // Arrange
        var repository = new MemoryGenericRepository<Customer>();
        await repository.Insert(new Customer { Name = "John Doe" });
        await repository.Insert(new Customer { Name = "Jane Doe" });
        await repository.Insert(new Customer { Name = "Bob Smith" });

        // Act - dynamically build specification based on conditions
        Specification<Customer> spec = new NameContainsSpecification("Doe");
        
        bool filterByJohn = true;
        if (filterByJohn)
        {
            spec = spec.And(new NameStartsWithSpecification("John"));
        }

        var results = await repository.Get(filter: spec.ToExpression());

        // Assert
        Assert.AreEqual(1, results.Count());
        Assert.AreEqual("John Doe", results.First().Name);
    }
}
