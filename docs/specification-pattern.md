# Specification Pattern Guide

The Specification Pattern is a powerful design pattern that encapsulates query logic and business rules in reusable, testable, and composable objects. OakIdeas.GenericRepository provides a comprehensive implementation of this pattern.

## Table of Contents

- [Overview](#overview)
- [Basic Usage](#basic-usage)
- [Creating Specifications](#creating-specifications)
- [Combining Specifications](#combining-specifications)
- [Integration with Repository](#integration-with-repository)
- [Real-World Examples](#real-world-examples)
- [Best Practices](#best-practices)
- [Performance Considerations](#performance-considerations)

## Overview

The Specification Pattern solves several problems:

1. **Scattered Query Logic**: Business rules scattered across service layer
2. **Code Duplication**: Same filters repeated in multiple places
3. **Difficult Testing**: Complex LINQ expressions hard to test
4. **Poor Maintainability**: Changes to business rules require updates in many places
5. **Lack of Reusability**: Query logic can't be easily shared

### Key Interfaces

```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>> ToExpression();
    bool IsSatisfiedBy(T entity);
}

public abstract class Specification<T> : ISpecification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();
    public virtual bool IsSatisfiedBy(T entity);
    
    public Specification<T> And(Specification<T> specification);
    public Specification<T> Or(Specification<T> specification);
    public Specification<T> Not();
}
```

## Basic Usage

### Creating a Simple Specification

```csharp
using OakIdeas.GenericRepository.Specifications;
using System;
using System.Linq.Expressions;

public class ActiveCustomerSpecification : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return c => c.IsActive;
    }
}

// Usage
var spec = new ActiveCustomerSpecification();
var activeCustomers = await repository.Get(filter: spec.ToExpression());
```

### Parameterized Specifications

```csharp
public class CustomerByNameSpecification : Specification<Customer>
{
    private readonly string _name;

    public CustomerByNameSpecification(string name)
    {
        _name = name;
    }

    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return c => c.Name == _name;
    }
}

// Usage
var spec = new CustomerByNameSpecification("John Doe");
var customer = await repository.Get(filter: spec.ToExpression());
```

### In-Memory Validation

```csharp
var spec = new ActiveCustomerSpecification();
var customer = new Customer { ID = 1, IsActive = true };

if (spec.IsSatisfiedBy(customer))
{
    Console.WriteLine("Customer is active!");
}
```

## Combining Specifications

### AND Operations

Combine multiple specifications that must all be true:

```csharp
var spec1 = new ActiveCustomerSpecification();
var spec2 = new PremiumCustomerSpecification();

var combinedSpec = spec1.And(spec2);
var premiumActiveCustomers = await repository.Get(filter: combinedSpec.ToExpression());
```

### OR Operations

Combine specifications where any can be true:

```csharp
var spec1 = new PremiumCustomerSpecification();
var spec2 = new VIPCustomerSpecification();

var combinedSpec = spec1.Or(spec2);
var specialCustomers = await repository.Get(filter: combinedSpec.ToExpression());
```

### NOT Operations

Negate a specification:

```csharp
var spec = new InactiveCustomerSpecification();

var activeSpec = spec.Not();
var activeCustomers = await repository.Get(filter: activeSpec.ToExpression());
```

### Complex Combinations

Chain multiple operators for complex logic:

```csharp
// (Active AND Premium) OR VIP
var spec = new ActiveCustomerSpecification()
    .And(new PremiumCustomerSpecification())
    .Or(new VIPCustomerSpecification());

var results = await repository.Get(filter: spec.ToExpression());
```

## Integration with Repository

### Direct Usage

```csharp
var spec = new ActiveCustomerSpecification();
var customers = await repository.Get(filter: spec.ToExpression());
```

### With Implicit Conversion

The Specification class provides implicit conversion to `Expression<Func<T, bool>>`:

```csharp
var spec = new ActiveCustomerSpecification();
var customers = await repository.Get(filter: spec);  // Implicit conversion
```

### With Ordering

```csharp
var spec = new ActiveCustomerSpecification();
var customers = await repository.Get(
    filter: spec.ToExpression(),
    orderBy: q => q.OrderBy(c => c.Name)
);
```

### With Include Properties

```csharp
var spec = new ActiveCustomerSpecification();
var customers = await repository.Get(
    filter: spec.ToExpression(),
    includeProperties: "Orders,Address"
);
```

## Real-World Examples

### E-Commerce: Product Filtering

```csharp
// Specification for products in stock
public class InStockSpecification : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.StockQuantity > 0;
    }
}

// Specification for products in price range
public class PriceRangeSpecification : Specification<Product>
{
    private readonly decimal _minPrice;
    private readonly decimal _maxPrice;

    public PriceRangeSpecification(decimal minPrice, decimal maxPrice)
    {
        _minPrice = minPrice;
        _maxPrice = maxPrice;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.Price >= _minPrice && p.Price <= _maxPrice;
    }
}

// Specification for products by category
public class CategorySpecification : Specification<Product>
{
    private readonly string _category;

    public CategorySpecification(string category)
    {
        _category = category;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.Category == _category;
    }
}

// Usage: Find affordable electronics in stock
var spec = new InStockSpecification()
    .And(new PriceRangeSpecification(0, 500))
    .And(new CategorySpecification("Electronics"));

var products = await repository.Get(filter: spec.ToExpression());
```

### Customer Relationship Management

```csharp
// High-value customer specification
public class HighValueCustomerSpecification : Specification<Customer>
{
    private readonly decimal _minimumLifetimeValue;

    public HighValueCustomerSpecification(decimal minimumLifetimeValue = 10000)
    {
        _minimumLifetimeValue = minimumLifetimeValue;
    }

    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return c => c.Orders.Sum(o => o.TotalAmount) >= _minimumLifetimeValue;
    }
}

// Recent customer specification
public class RecentCustomerSpecification : Specification<Customer>
{
    private readonly int _daysAgo;

    public RecentCustomerSpecification(int daysAgo = 90)
    {
        _daysAgo = daysAgo;
    }

    public override Expression<Func<Customer, bool>> ToExpression()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_daysAgo);
        return c => c.Orders.Any(o => o.OrderDate >= cutoffDate);
    }
}

// At-risk customer specification
public class AtRiskCustomerSpecification : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        return c => c.IsActive && 
                    !c.Orders.Any(o => o.OrderDate >= sixMonthsAgo);
    }
}

// Usage: Find high-value customers who are at risk
var spec = new HighValueCustomerSpecification(5000)
    .And(new AtRiskCustomerSpecification());

var atRiskVIPs = await repository.Get(filter: spec.ToExpression());
```

### Order Management

```csharp
// Pending orders specification
public class PendingOrdersSpecification : Specification<Order>
{
    public override Expression<Func<Order, bool>> ToExpression()
    {
        return o => o.Status == OrderStatus.Pending;
    }
}

// Overdue orders specification
public class OverdueOrdersSpecification : Specification<Order>
{
    public override Expression<Func<Order, bool>> ToExpression()
    {
        var today = DateTime.UtcNow.Date;
        return o => o.ExpectedDeliveryDate < today && 
                    o.Status != OrderStatus.Delivered;
    }
}

// Large orders specification
public class LargeOrderSpecification : Specification<Order>
{
    private readonly decimal _threshold;

    public LargeOrderSpecification(decimal threshold = 1000)
    {
        _threshold = threshold;
    }

    public override Expression<Func<Order, bool>> ToExpression()
    {
        return o => o.TotalAmount >= _threshold;
    }
}

// Usage: Find large pending or overdue orders
var spec = new LargeOrderSpecification()
    .And(new PendingOrdersSpecification()
        .Or(new OverdueOrdersSpecification()));

var criticalOrders = await repository.Get(
    filter: spec.ToExpression(),
    orderBy: q => q.OrderByDescending(o => o.TotalAmount)
);
```

### Employee Management

```csharp
// Senior employee specification
public class SeniorEmployeeSpecification : Specification<Employee>
{
    private readonly int _yearsOfService;

    public SeniorEmployeeSpecification(int yearsOfService = 5)
    {
        _yearsOfService = yearsOfService;
    }

    public override Expression<Func<Employee, bool>> ToExpression()
    {
        var cutoffDate = DateTime.UtcNow.AddYears(-_yearsOfService);
        return e => e.HireDate <= cutoffDate;
    }
}

// Department specification
public class DepartmentSpecification : Specification<Employee>
{
    private readonly string _department;

    public DepartmentSpecification(string department)
    {
        _department = department;
    }

    public override Expression<Func<Employee, bool>> ToExpression()
    {
        return e => e.Department == _department;
    }
}

// Eligible for bonus specification
public class EligibleForBonusSpecification : Specification<Employee>
{
    public override Expression<Func<Employee, bool>> ToExpression()
    {
        return e => e.IsActive && 
                    e.PerformanceRating >= 4 && 
                    e.YearsOfService >= 1;
    }
}

// Usage: Find senior IT employees eligible for bonus
var spec = new SeniorEmployeeSpecification(3)
    .And(new DepartmentSpecification("IT"))
    .And(new EligibleForBonusSpecification());

var bonusEligibleEmployees = await repository.Get(filter: spec.ToExpression());
```

### Content Management

```csharp
// Published content specification
public class PublishedContentSpecification : Specification<Article>
{
    public override Expression<Func<Article, bool>> ToExpression()
    {
        var now = DateTime.UtcNow;
        return a => a.Status == ArticleStatus.Published &&
                    a.PublishDate <= now &&
                    (a.ExpiryDate == null || a.ExpiryDate > now);
    }
}

// Featured content specification
public class FeaturedContentSpecification : Specification<Article>
{
    public override Expression<Func<Article, bool>> ToExpression()
    {
        return a => a.IsFeatured;
    }
}

// Popular content specification
public class PopularContentSpecification : Specification<Article>
{
    private readonly int _minimumViews;

    public PopularContentSpecification(int minimumViews = 1000)
    {
        _minimumViews = minimumViews;
    }

    public override Expression<Func<Article, bool>> ToExpression()
    {
        return a => a.ViewCount >= _minimumViews;
    }
}

// Author specification
public class AuthorSpecification : Specification<Article>
{
    private readonly string _authorId;

    public AuthorSpecification(string authorId)
    {
        _authorId = authorId;
    }

    public override Expression<Func<Article, bool>> ToExpression()
    {
        return a => a.AuthorId == _authorId;
    }
}

// Usage: Find featured or popular published articles
var spec = new PublishedContentSpecification()
    .And(new FeaturedContentSpecification()
        .Or(new PopularContentSpecification(500)));

var articles = await repository.Get(
    filter: spec.ToExpression(),
    orderBy: q => q.OrderByDescending(a => a.PublishDate)
);
```

## Best Practices

### 1. Keep Specifications Focused

Each specification should represent a single business rule or concept:

```csharp
// Good: Single responsibility
public class ActiveCustomerSpecification : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return c => c.IsActive;
    }
}

// Bad: Multiple responsibilities
public class ActivePremiumCustomerWithOrdersSpecification : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return c => c.IsActive && c.IsPremium && c.Orders.Any();
    }
}
```

### 2. Use Composition Over Complex Specifications

Build complex logic by combining simple specifications:

```csharp
// Good: Composed from simple specifications
var spec = new ActiveCustomerSpecification()
    .And(new PremiumCustomerSpecification())
    .And(new HasOrdersSpecification());

// Instead of one complex specification
```

### 3. Make Specifications Immutable

Specifications should not have mutable state:

```csharp
// Good: Immutable
public class PriceRangeSpecification : Specification<Product>
{
    private readonly decimal _minPrice;
    private readonly decimal _maxPrice;

    public PriceRangeSpecification(decimal minPrice, decimal maxPrice)
    {
        _minPrice = minPrice;
        _maxPrice = maxPrice;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.Price >= _minPrice && p.Price <= _maxPrice;
    }
}
```

### 4. Name Specifications Clearly

Use descriptive names that reflect the business rule:

```csharp
// Good names
public class ActiveCustomerSpecification
public class HighValueOrderSpecification
public class OverdueInvoiceSpecification

// Poor names
public class CustomerSpec
public class OrderFilter
public class InvoiceChecker
```

### 5. Test Specifications Independently

Test each specification in isolation:

```csharp
[TestMethod]
public void ActiveCustomerSpecification_ActiveCustomer_ReturnsTrue()
{
    // Arrange
    var spec = new ActiveCustomerSpecification();
    var customer = new Customer { IsActive = true };

    // Act
    var result = spec.IsSatisfiedBy(customer);

    // Assert
    Assert.IsTrue(result);
}
```

### 6. Reuse Specifications

Define specifications once and reuse them across your application:

```csharp
// In a specifications library
public static class CustomerSpecifications
{
    public static Specification<Customer> Active()
        => new ActiveCustomerSpecification();
    
    public static Specification<Customer> Premium()
        => new PremiumCustomerSpecification();
    
    public static Specification<Customer> HighValue(decimal threshold)
        => new HighValueCustomerSpecification(threshold);
}

// Usage
var spec = CustomerSpecifications.Active()
    .And(CustomerSpecifications.Premium());
```

### 7. Document Business Rules

Add XML documentation explaining the business rule:

```csharp
/// <summary>
/// Specification that identifies high-value customers.
/// A customer is considered high-value if their lifetime order total
/// exceeds the specified threshold (default $10,000).
/// </summary>
public class HighValueCustomerSpecification : Specification<Customer>
{
    // Implementation...
}
```

## Performance Considerations

### Expression Trees vs Compiled Predicates

- **ToExpression()**: Returns an expression tree that can be translated to SQL by Entity Framework
- **IsSatisfiedBy()**: Compiles the expression and evaluates in memory

```csharp
// Efficient: Runs in database
var customers = await repository.Get(filter: spec.ToExpression());

// Less efficient: Loads all customers into memory first
var allCustomers = await repository.Get();
var filtered = allCustomers.Where(c => spec.IsSatisfiedBy(c));
```

### Avoid Complex Nested Specifications

Very deep nesting can impact query performance:

```csharp
// Potentially inefficient
var spec = spec1.And(spec2).And(spec3).And(spec4).And(spec5);

// Better: Combine when possible
public class CombinedSpecification : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return c => c.Condition1 && c.Condition2 && c.Condition3;
    }
}
```

### Test Query Performance

Always test the SQL generated by your specifications:

```csharp
// Enable EF Core query logging
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.LogTo(Console.WriteLine);
}

// Run query and inspect SQL
var spec = new ComplexSpecification();
var results = await repository.Get(filter: spec.ToExpression());
```

### Cache Specifications

Reuse specification instances when possible:

```csharp
// Good: Reuse instance
private static readonly Specification<Customer> _activeSpec = 
    new ActiveCustomerSpecification();

public async Task<IEnumerable<Customer>> GetActiveCustomers()
{
    return await repository.Get(filter: _activeSpec.ToExpression());
}
```

## Summary

The Specification Pattern in OakIdeas.GenericRepository provides:

- ✅ **Reusable** query logic
- ✅ **Testable** business rules
- ✅ **Composable** specifications with And/Or/Not
- ✅ **Readable** code that expresses intent
- ✅ **Maintainable** centralized business rules
- ✅ **Type-safe** query construction

By following this guide and best practices, you can build robust, maintainable query logic for your applications.

## Next Steps

- Review the [API Reference](api-reference.md) for complete specification API documentation
- Check [Usage Examples](usage-examples.md) for more practical scenarios
- See [Best Practices](best-practices.md) for general repository patterns
