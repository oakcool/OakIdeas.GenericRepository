# Query Object Pattern

The Query Object pattern encapsulates all query parameters (filter, sort, pagination, includes, and tracking configuration) in a single, reusable object with a fluent API.

## Overview

Instead of passing multiple parameters to repository methods, you can create a `Query<TEntity>` object that clearly expresses your query intent and can be reused across multiple calls.

## Benefits

- **Extensibility**: Easy to add new query parameters without changing method signatures
- **Reusability**: Define queries once, use multiple times with modifications
- **Testability**: Query objects are easy to test independently
- **Clarity**: Self-documenting query intent through fluent API
- **Composability**: Build complex queries step by step

## Basic Usage

### Creating a Simple Query

```csharp
using OakIdeas.GenericRepository;

// Create a basic filter query
var query = new Query<Product>()
    .Where(p => p.IsActive);

var products = await repository.Get(query);
```

### Fluent API

The Query object provides a fluent API that allows method chaining:

```csharp
var query = new Query<Product>()
    .Where(p => p.Price > 100)
    .Sort(q => q.OrderBy(p => p.Name))
    .Paged(1, 20)
    .Include(p => p.Category)
    .WithNoTracking();

var results = await repository.Get(query);
```

## Query Options

### Filter (Where)

Add filter expressions to narrow down results:

```csharp
var query = new Query<Customer>()
    .Where(c => c.IsActive && c.TotalOrders > 10);
```

### Sorting (Sort)

Define ordering for the results:

```csharp
// Single sort
var query = new Query<Product>()
    .Sort(q => q.OrderBy(p => p.Name));

// Multiple sorts
var query = new Query<Product>()
    .Sort(q => q.OrderBy(p => p.Category)
                .ThenByDescending(p => p.Price));
```

### Pagination (Paged)

Configure pagination with page number and page size:

```csharp
var query = new Query<Product>()
    .Paged(page: 2, pageSize: 25);

var results = await repository.Get(query);
// Returns items 26-50
```

### Include Navigation Properties (Include)

Eagerly load related entities using type-safe expressions:

```csharp
// Single include
var query = new Query<Order>()
    .Include(o => o.Customer);

// Multiple includes
var query = new Query<Order>()
    .Include(o => o.Customer)
    .Include(o => o.Items);
```

### No Tracking (WithNoTracking)

For read-only scenarios, use no-tracking queries for better performance:

```csharp
var query = new Query<Product>()
    .Where(p => p.IsActive)
    .WithNoTracking();

var products = await repository.Get(query);
// Entities are not tracked by EF Core
```

## Real-World Examples

### Example 1: Active Products with Pagination

```csharp
var activeProductsQuery = new Query<Product>()
    .Where(p => p.IsActive)
    .Sort(q => q.OrderBy(p => p.Name))
    .Paged(1, 20);

var products = await productRepository.Get(activeProductsQuery);
```

### Example 2: Recent High-Value Orders

```csharp
var recentHighValueOrders = new Query<Order>()
    .Where(o => o.TotalAmount > 1000 && 
                o.OrderDate >= DateTime.UtcNow.AddDays(-30))
    .Sort(q => q.OrderByDescending(o => o.OrderDate))
    .Include(o => o.Customer)
    .Include(o => o.Items)
    .Paged(1, 10);

var orders = await orderRepository.Get(recentHighValueOrders);
```

### Example 3: Complex Product Search

```csharp
var productSearch = new Query<Product>()
    .Where(p => p.IsActive && 
                p.Price >= minPrice && 
                p.Price <= maxPrice &&
                p.Category.Name == categoryName)
    .Sort(q => q.OrderBy(p => p.Name))
    .Include(p => p.Category)
    .Include(p => p.Reviews)
    .Paged(currentPage, pageSize)
    .WithNoTracking();

var searchResults = await productRepository.Get(productSearch);
```

### Example 4: Export Query (No Pagination)

```csharp
var exportQuery = new Query<Customer>()
    .Where(c => c.CreatedDate >= startDate && 
                c.CreatedDate <= endDate)
    .Sort(q => q.OrderBy(c => c.Name))
    .WithNoTracking();

var customersToExport = await customerRepository.Get(exportQuery);
```

## Query Reusability

One of the most powerful features of the Query Object pattern is reusability. You can define a query once and use it multiple times, modifying it as needed:

```csharp
// Define a base query for active products
var activeProductsQuery = new Query<Product>()
    .Where(p => p.IsActive)
    .Sort(q => q.OrderBy(p => p.Name));

// Get all active products
var allActive = await repository.Get(activeProductsQuery);

// Modify for first page
activeProductsQuery.Paged(1, 20);
var firstPage = await repository.Get(activeProductsQuery);

// Modify for second page
activeProductsQuery.Paged(2, 20);
var secondPage = await repository.Get(activeProductsQuery);

// Add includes for detailed view
activeProductsQuery.Include(p => p.Category)
                   .Include(p => p.Reviews);
var detailedPage = await repository.Get(activeProductsQuery);
```

## Building Queries Conditionally

You can build queries conditionally based on business logic:

```csharp
public async Task<IEnumerable<Product>> SearchProducts(
    string? searchTerm,
    decimal? minPrice,
    decimal? maxPrice,
    string? category,
    int page,
    int pageSize)
{
    var query = new Query<Product>()
        .Where(p => p.IsActive)
        .Sort(q => q.OrderBy(p => p.Name))
        .Paged(page, pageSize);

    // Add optional filters
    if (!string.IsNullOrEmpty(searchTerm))
    {
        query.Where(p => p.Name.Contains(searchTerm) || 
                         p.Description.Contains(searchTerm));
    }

    if (minPrice.HasValue)
    {
        query.Where(p => p.Price >= minPrice.Value);
    }

    if (maxPrice.HasValue)
    {
        query.Where(p => p.Price <= maxPrice.Value);
    }

    if (!string.IsNullOrEmpty(category))
    {
        query.Where(p => p.Category.Name == category)
             .Include(p => p.Category);
    }

    return await _productRepository.Get(query);
}
```

## Integration with Specifications

The Query Object pattern works seamlessly with the Specification pattern:

```csharp
// Define a specification
var activeProductsSpec = new ActiveProductsSpecification();

// Use it in a query
var query = new Query<Product>()
    .Where(activeProductsSpec.ToExpression())
    .Sort(q => q.OrderBy(p => p.Name))
    .Paged(1, 20);

var products = await repository.Get(query);
```

## Advanced Scenarios

### Repository Extension Methods

You can create extension methods for common queries:

```csharp
public static class ProductRepositoryExtensions
{
    public static async Task<IEnumerable<Product>> GetActiveProducts(
        this IGenericRepository<Product> repository,
        int page,
        int pageSize)
    {
        var query = new Query<Product>()
            .Where(p => p.IsActive)
            .Sort(q => q.OrderBy(p => p.Name))
            .Paged(page, pageSize);

        return await repository.Get(query);
    }

    public static async Task<IEnumerable<Product>> GetProductsByCategory(
        this IGenericRepository<Product> repository,
        string categoryName,
        bool includeInactive = false)
    {
        var query = new Query<Product>()
            .Where(p => p.Category.Name == categoryName)
            .Include(p => p.Category)
            .Sort(q => q.OrderBy(p => p.Name));

        if (!includeInactive)
        {
            query.Where(p => p.IsActive);
        }

        return await repository.Get(query);
    }
}
```

### Service Layer Integration

Use Query objects in your service layer:

```csharp
public class ProductService
{
    private readonly IGenericRepository<Product> _productRepository;

    public ProductService(IGenericRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<PagedResult<Product>> GetProductPage(
        int page, 
        int pageSize,
        bool includeRelated = false)
    {
        var query = new Query<Product>()
            .Where(p => p.IsActive)
            .Sort(q => q.OrderBy(p => p.Name))
            .Paged(page, pageSize);

        if (includeRelated)
        {
            query.Include(p => p.Category)
                 .Include(p => p.Reviews);
        }

        var products = await _productRepository.Get(query);

        return new PagedResult<Product>
        {
            Items = products.ToList(),
            Page = page,
            PageSize = pageSize
        };
    }
}
```

## Testing

Query objects are easy to test:

```csharp
[TestMethod]
public void Query_WithAllOptions_ConfiguresCorrectly()
{
    // Arrange & Act
    var query = new Query<Product>()
        .Where(p => p.IsActive)
        .Sort(q => q.OrderBy(p => p.Name))
        .Include(p => p.Category)
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
    Assert.AreEqual(20, query.Skip); // (page - 1) * pageSize = 1 * 20
    Assert.AreEqual(20, query.Take);
}
```

## Performance Considerations

### Use WithNoTracking for Read-Only Queries

```csharp
// For read-only scenarios
var query = new Query<Product>()
    .Where(p => p.IsActive)
    .WithNoTracking(); // Better performance

var products = await repository.Get(query);
```

### Pagination for Large Result Sets

```csharp
// Always paginate large result sets
var query = new Query<Product>()
    .Where(p => p.Category.Name == "Electronics")
    .Paged(1, 50); // Avoid loading all products

var products = await repository.Get(query);
```

### Include Only Necessary Navigation Properties

```csharp
// Only include what you need
var query = new Query<Order>()
    .Include(o => o.Customer) // Only include Customer
    .WithNoTracking();

// Avoid including unnecessary data
// .Include(o => o.Items)        // Not needed for this view
// .Include(o => o.ShippingInfo) // Not needed for this view
```

## API Reference

### Query<TEntity> Class

#### Properties

- `Filter`: Optional LINQ filter expression
- `OrderBy`: Optional ordering function
- `Includes`: List of navigation properties to include
- `Page`: Page number for pagination (1-based)
- `PageSize`: Number of items per page
- `AsNoTracking`: Whether to use no-tracking queries
- `Skip`: Calculated number of items to skip
- `Take`: Calculated number of items to take

#### Methods

- `Where(Expression<Func<TEntity, bool>> filter)`: Adds a filter expression
- `Sort(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy)`: Adds ordering
- `Include(Expression<Func<TEntity, object>> include)`: Adds a navigation property to include
- `Paged(int page, int pageSize)`: Configures pagination
- `WithNoTracking(bool asNoTracking = true)`: Configures no-tracking

## Migration from Traditional Queries

### Before (Traditional Approach)

```csharp
var products = await repository.Get(
    filter: p => p.IsActive && p.Price > 100,
    orderBy: q => q.OrderBy(p => p.Name),
    includeProperties: "Category,Reviews"
);
```

### After (Query Object Pattern)

```csharp
var query = new Query<Product>()
    .Where(p => p.IsActive && p.Price > 100)
    .Sort(q => q.OrderBy(p => p.Name))
    .Include(p => p.Category)
    .Include(p => p.Reviews);

var products = await repository.Get(query);
```

## See Also

- [Specification Pattern](specification-pattern.md) - Combine with Query objects for even more reusability
- [Type-Safe Includes](type-safe-includes.md) - Learn more about type-safe navigation property loading
- [API Reference](api-reference.md) - Complete API documentation
- [Best Practices](best-practices.md) - General best practices
