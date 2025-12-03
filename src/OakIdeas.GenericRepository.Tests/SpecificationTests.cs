using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.Specifications;
using OakIdeas.GenericRepository.Tests.Models;
using System;
using System.Linq.Expressions;

namespace OakIdeas.GenericRepository.Tests;

/// <summary>
/// Tests for the Specification pattern implementation.
/// </summary>
[TestClass]
public class SpecificationTests
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
    public void ToExpression_ReturnsValidExpression()
    {
        // Arrange
        var spec = new NameStartsWithSpecification("John");

        // Act
        var expression = spec.ToExpression();

        // Assert
        Assert.IsNotNull(expression);
        Assert.IsInstanceOfType(expression, typeof(Expression<Func<Customer, bool>>));
    }

    [TestMethod]
    public void IsSatisfiedBy_EntityMatchesSpecification_ReturnsTrue()
    {
        // Arrange
        var spec = new NameStartsWithSpecification("John");
        var customer = new Customer { ID = 1, Name = "John Doe" };

        // Act
        var result = spec.IsSatisfiedBy(customer);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsSatisfiedBy_EntityDoesNotMatchSpecification_ReturnsFalse()
    {
        // Arrange
        var spec = new NameStartsWithSpecification("John");
        var customer = new Customer { ID = 1, Name = "Jane Doe" };

        // Act
        var result = spec.IsSatisfiedBy(customer);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void And_CombinesTwoSpecifications()
    {
        // Arrange
        var spec1 = new NameStartsWithSpecification("John");
        var spec2 = new ActiveCustomerSpecification();
        var customer = new Customer { ID = 1, Name = "John Doe" };

        // Act
        var combinedSpec = spec1.And(spec2);
        var result = combinedSpec.IsSatisfiedBy(customer);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void And_FirstSpecificationFails_ReturnsFalse()
    {
        // Arrange
        var spec1 = new NameStartsWithSpecification("Jane");
        var spec2 = new ActiveCustomerSpecification();
        var customer = new Customer { ID = 1, Name = "John Doe" };

        // Act
        var combinedSpec = spec1.And(spec2);
        var result = combinedSpec.IsSatisfiedBy(customer);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void And_SecondSpecificationFails_ReturnsFalse()
    {
        // Arrange
        var spec1 = new NameStartsWithSpecification("John");
        var spec2 = new ActiveCustomerSpecification();
        var customer = new Customer { ID = 0, Name = "John Doe" };

        // Act
        var combinedSpec = spec1.And(spec2);
        var result = combinedSpec.IsSatisfiedBy(customer);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Or_BothSpecificationsMatch_ReturnsTrue()
    {
        // Arrange
        var spec1 = new NameStartsWithSpecification("John");
        var spec2 = new NameContainsSpecification("Doe");
        var customer = new Customer { ID = 1, Name = "John Doe" };

        // Act
        var combinedSpec = spec1.Or(spec2);
        var result = combinedSpec.IsSatisfiedBy(customer);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Or_FirstSpecificationMatches_ReturnsTrue()
    {
        // Arrange
        var spec1 = new NameStartsWithSpecification("John");
        var spec2 = new NameContainsSpecification("Smith");
        var customer = new Customer { ID = 1, Name = "John Doe" };

        // Act
        var combinedSpec = spec1.Or(spec2);
        var result = combinedSpec.IsSatisfiedBy(customer);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Or_SecondSpecificationMatches_ReturnsTrue()
    {
        // Arrange
        var spec1 = new NameStartsWithSpecification("Jane");
        var spec2 = new NameContainsSpecification("Doe");
        var customer = new Customer { ID = 1, Name = "John Doe" };

        // Act
        var combinedSpec = spec1.Or(spec2);
        var result = combinedSpec.IsSatisfiedBy(customer);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Or_NeitherSpecificationMatches_ReturnsFalse()
    {
        // Arrange
        var spec1 = new NameStartsWithSpecification("Jane");
        var spec2 = new NameContainsSpecification("Smith");
        var customer = new Customer { ID = 1, Name = "John Doe" };

        // Act
        var combinedSpec = spec1.Or(spec2);
        var result = combinedSpec.IsSatisfiedBy(customer);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Not_SpecificationMatches_ReturnsFalse()
    {
        // Arrange
        var spec = new NameStartsWithSpecification("John");
        var customer = new Customer { ID = 1, Name = "John Doe" };

        // Act
        var notSpec = spec.Not();
        var result = notSpec.IsSatisfiedBy(customer);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Not_SpecificationDoesNotMatch_ReturnsTrue()
    {
        // Arrange
        var spec = new NameStartsWithSpecification("Jane");
        var customer = new Customer { ID = 1, Name = "John Doe" };

        // Act
        var notSpec = spec.Not();
        var result = notSpec.IsSatisfiedBy(customer);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ComplexSpecification_AndOrCombination_WorksCorrectly()
    {
        // Arrange
        // (StartsWith "John" AND Contains "Doe") OR (StartsWith "Jane")
        var spec1 = new NameStartsWithSpecification("John");
        var spec2 = new NameContainsSpecification("Doe");
        var spec3 = new NameStartsWithSpecification("Jane");
        
        var customer1 = new Customer { ID = 1, Name = "John Doe" };
        var customer2 = new Customer { ID = 2, Name = "Jane Smith" };
        var customer3 = new Customer { ID = 3, Name = "John Smith" };
        var customer4 = new Customer { ID = 4, Name = "Bob Johnson" };

        // Act
        var combinedSpec = spec1.And(spec2).Or(spec3);

        // Assert
        Assert.IsTrue(combinedSpec.IsSatisfiedBy(customer1));  // Matches first part
        Assert.IsTrue(combinedSpec.IsSatisfiedBy(customer2));  // Matches second part
        Assert.IsFalse(combinedSpec.IsSatisfiedBy(customer3)); // Matches neither
        Assert.IsFalse(combinedSpec.IsSatisfiedBy(customer4)); // Matches neither
    }

    [TestMethod]
    public void ComplexSpecification_NotAndOr_WorksCorrectly()
    {
        // Arrange
        // NOT(StartsWith "John") OR Contains "Doe"
        var spec1 = new NameStartsWithSpecification("John");
        var spec2 = new NameContainsSpecification("Doe");
        
        var customer1 = new Customer { ID = 1, Name = "John Doe" };
        var customer2 = new Customer { ID = 2, Name = "Jane Doe" };
        var customer3 = new Customer { ID = 3, Name = "John Smith" };
        var customer4 = new Customer { ID = 4, Name = "Bob Johnson" };

        // Act
        var combinedSpec = spec1.Not().Or(spec2);

        // Assert
        Assert.IsTrue(combinedSpec.IsSatisfiedBy(customer1));  // Matches second part (Contains "Doe")
        Assert.IsTrue(combinedSpec.IsSatisfiedBy(customer2));  // Matches both parts
        Assert.IsFalse(combinedSpec.IsSatisfiedBy(customer3)); // Matches neither (starts with John, no Doe)
        Assert.IsTrue(combinedSpec.IsSatisfiedBy(customer4));  // Matches first part (NOT starts with John)
    }

    [TestMethod]
    public void ImplicitConversion_SpecificationToExpression_Works()
    {
        // Arrange
        var spec = new NameStartsWithSpecification("John");

        // Act
        Expression<Func<Customer, bool>> expression = spec;

        // Assert
        Assert.IsNotNull(expression);
        Assert.IsInstanceOfType(expression, typeof(Expression<Func<Customer, bool>>));
    }

    [TestMethod]
    public void AndSpecification_NullLeftSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new NameStartsWithSpecification("John");

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new AndSpecification<Customer>(null!, spec));
    }

    [TestMethod]
    public void AndSpecification_NullRightSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new NameStartsWithSpecification("John");

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new AndSpecification<Customer>(spec, null!));
    }

    [TestMethod]
    public void OrSpecification_NullLeftSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new NameStartsWithSpecification("John");

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new OrSpecification<Customer>(null!, spec));
    }

    [TestMethod]
    public void OrSpecification_NullRightSpecification_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new NameStartsWithSpecification("John");

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new OrSpecification<Customer>(spec, null!));
    }

    [TestMethod]
    public void NotSpecification_NullSpecification_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new NotSpecification<Customer>(null!));
    }
}
