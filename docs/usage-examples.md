# Usage Examples

This document provides comprehensive examples of using OakIdeas.GenericRepository in various scenarios.

## Table of Contents

- [Working with Different Key Types](#working-with-different-key-types)
- [Basic CRUD Operations](#basic-crud-operations)
- [Filtering and Querying](#filtering-and-querying)
- [Ordering Results](#ordering-results)
- [Eager Loading (Entity Framework Core)](#eager-loading-entity-framework-core)
- [Complex Scenarios](#complex-scenarios)
- [Error Handling](#error-handling)

## Working with Different Key Types

The generic repository supports multiple key types including int, Guid, long, and string. Choose the appropriate key type for your use case.

### Integer Keys (Default, Backward Compatible)

```csharp
using OakIdeas.GenericRepository;
using OakIdeas.GenericRepository.Models;

// Define entity with int key (backward compatible)
public class Product : EntityBase
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// Use repository - IDs auto-generated starting from 1
var repository = new MemoryGenericRepository<Product>();
var product = new Product { Name = "Laptop", Price = 999.99m };
var inserted = await repository.Insert(product);
Console.WriteLine($"Created with ID: {inserted.ID}"); // e.g., "Created with ID: 1"
```

### Guid Keys (Recommended for Distributed Systems)

```csharp
// Define entity with Guid key
public class Order : EntityBase<Guid>
{
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public string CustomerId { get; set; }
}

// Use repository with Guid keys - must provide ID
var repository = new MemoryGenericRepository<Order, Guid>();
var order = new Order 
{ 
    ID = Guid.NewGuid(),  // Generate unique ID
    OrderDate = DateTime.UtcNow,
    Total = 1599.99m
};
var inserted = await repository.Insert(order);
Console.WriteLine($"Created with ID: {inserted.ID}");

// Retrieve by Guid
var retrieved = await repository.Get(inserted.ID);

// Delete by Guid
await repository.Delete(inserted.ID);
```

### Long Keys (For Large Datasets)

```csharp
// Define entity with long key
public class Transaction : EntityBase<long>
{
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public string AccountNumber { get; set; }
}

// Use repository with long keys
var repository = new MemoryGenericRepository<Transaction, long>();
var transaction = new Transaction 
{ 
    ID = 1000000000000L,  // Large ID for distributed/sharded systems
    Amount = 250.00m,
    Timestamp = DateTime.UtcNow
};
var inserted = await repository.Insert(transaction);

// Retrieve by long
var retrieved = await repository.Get(1000000000000L);
```

### String Keys (For Business/Natural Keys)

```csharp
// Define entity with string key
public class Customer : EntityBase<string>
{
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime JoinDate { get; set; }
}

// Use repository with string keys
var repository = new MemoryGenericRepository<Customer, string>();
var customer = new Customer 
{ 
    ID = "CUST-2024-001",  // Business-friendly ID
    Name = "Acme Corporation",
    Email = "contact@acme.com",
    JoinDate = DateTime.UtcNow
};
var inserted = await repository.Insert(customer);

// Retrieve by string
var retrieved = await repository.Get("CUST-2024-001");

// Filter works the same regardless of key type
var recentCustomers = await repository.Get(
    filter: c => c.JoinDate >= DateTime.UtcNow.AddDays(-30),
    orderBy: q => q.OrderBy(c => c.Name)
);
```

### Choosing the Right Key Type

| Key Type | Best For | Pros | Cons |
|----------|----------|------|------|
| `int` | Simple apps, small datasets | Auto-generated, smallest storage, fastest | Limited range, not suitable for distributed systems |
| `Guid` | Distributed systems, microservices | No collisions, can generate client-side | Larger storage, not sequential |
| `long` | Large datasets, future-proofing | Wide range, sequential possible | Larger than int |
| `string` | Business keys, external systems | Human-readable, flexible | Slower comparison, larger storage, no auto-generation |

## Basic CRUD Operations

### Insert (Create)

```csharp
using OakIdeas.GenericRepository;
using OakIdeas.GenericRepository.Models;

public class Product : EntityBase
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

var repository = new MemoryGenericRepository<Product>();

// Insert a new product
var product = new Product
{
    Name = "Laptop",
    Price = 999.99m,
    Stock = 10
};

var insertedProduct = await repository.Insert(product);
Console.WriteLine($"Product created with ID: {insertedProduct.ID}");
```

### Get by ID (Read)

```csharp
// Retrieve by ID
var productId = 1;
var product = await repository.Get(productId);

if (product != null)
{
    Console.WriteLine($"Found: {product.Name} - ${product.Price}");
}
else
{
    Console.WriteLine("Product not found");
}
```

### Update

```csharp
// Get the product first
var product = await repository.Get(1);

if (product != null)
{
    // Modify properties
    product.Price = 899.99m;
    product.Stock = 15;

    // Update in repository
    var updated = await repository.Update(product);
    Console.WriteLine($"Updated price to: ${updated.Price}");
}
```

### Delete

```csharp
// Delete by entity
var product = await repository.Get(1);
if (product != null)
{
    var deleted = await repository.Delete(product);
    Console.WriteLine($"Deletion successful: {deleted}");
}

// Or delete by ID directly
var deleted = await repository.Delete(1);
Console.WriteLine($"Deletion successful: {deleted}");
```

## Filtering and Querying

### Simple Filter

```csharp
// Find all products under $500
var affordableProducts = await repository.Get(
    filter: p => p.Price < 500
);

foreach (var product in affordableProducts)
{
    Console.WriteLine($"{product.Name}: ${product.Price}");
}
```

### Multiple Conditions

```csharp
// Find products that are in stock and under $1000
var availableProducts = await repository.Get(
    filter: p => p.Stock > 0 && p.Price < 1000
);
```

### String Operations

```csharp
// Find products with "Laptop" in the name
var laptops = await repository.Get(
    filter: p => p.Name.Contains("Laptop")
);

// Case-insensitive search (with EF Core)
var searchTerm = "laptop";
var results = await repository.Get(
    filter: p => p.Name.ToLower().Contains(searchTerm.ToLower())
);
```

### Get All Entities

```csharp
// Get all entities (no filter)
var allProducts = await repository.Get();

Console.WriteLine($"Total products: {allProducts.Count()}");
```

## Ordering Results

### Simple Ordering

```csharp
// Order by price ascending
var productsByPrice = await repository.Get(
    orderBy: q => q.OrderBy(p => p.Price)
);

foreach (var product in productsByPrice)
{
    Console.WriteLine($"{product.Name}: ${product.Price}");
}
```

### Descending Order

```csharp
// Order by price descending
var expensiveFirst = await repository.Get(
    orderBy: q => q.OrderByDescending(p => p.Price)
);
```

### Multiple Order Criteria

```csharp
// Order by stock (ascending), then by price (descending)
var orderedProducts = await repository.Get(
    orderBy: q => q.OrderBy(p => p.Stock)
                   .ThenByDescending(p => p.Price)
);
```

### Combined Filter and Order

```csharp
// Find in-stock products ordered by name
var inStockProducts = await repository.Get(
    filter: p => p.Stock > 0,
    orderBy: q => q.OrderBy(p => p.Name)
);
```

## Eager Loading (Entity Framework Core)

### Single Navigation Property

```csharp
public class Order : EntityBase
{
    public DateTime OrderDate { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; }
    public List<OrderItem> Items { get; set; }
}

// Load orders with customer information
var orders = await orderRepository.Get(
    includeProperties: "Customer"
);

foreach (var order in orders)
{
    Console.WriteLine($"Order for {order.Customer.Name}");
}
```

### Multiple Navigation Properties

```csharp
// Load orders with both customer and items
var ordersWithDetails = await orderRepository.Get(
    includeProperties: "Customer,Items"
);

foreach (var order in ordersWithDetails)
{
    Console.WriteLine($"Order by {order.Customer.Name}");
    Console.WriteLine($"Items: {order.Items.Count}");
}
```

### Nested Navigation Properties

```csharp
// Load orders with items and each item's product
var ordersWithProducts = await orderRepository.Get(
    includeProperties: "Items,Items.Product"
);
```

### Combined: Filter, Order, and Include

```csharp
// Recent orders for active customers with full details
var recentOrders = await orderRepository.Get(
    filter: o => o.OrderDate >= DateTime.UtcNow.AddDays(-30),
    orderBy: q => q.OrderByDescending(o => o.OrderDate),
    includeProperties: "Customer,Items"
);
```

## Complex Scenarios

### Bulk Insert

```csharp
public async Task<List<Product>> BulkInsertProducts(List<Product> products)
{
    var inserted = new List<Product>();
    
    foreach (var product in products)
    {
        var result = await repository.Insert(product);
        inserted.Add(result);
    }
    
    return inserted;
}
```

### Conditional Update

```csharp
public async Task<Product> UpdatePriceIfChanged(int productId, decimal newPrice)
{
    var product = await repository.Get(productId);
    
    if (product == null)
        throw new NotFoundException($"Product {productId} not found");
    
    // Only update if price actually changed
    if (product.Price != newPrice)
    {
        product.Price = newPrice;
        return await repository.Update(product);
    }
    
    return product;
}
```

### Search with Pagination Pattern

```csharp
public async Task<PagedResult<Product>> SearchProducts(
    string searchTerm,
    int page = 1,
    int pageSize = 10)
{
    var allResults = await repository.Get(
        filter: p => p.Name.Contains(searchTerm),
        orderBy: q => q.OrderBy(p => p.Name)
    );
    
    var total = allResults.Count();
    var items = allResults
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();
    
    return new PagedResult<Product>
    {
        Items = items,
        TotalCount = total,
        Page = page,
        PageSize = pageSize
    };
}

public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

### Exists Check

```csharp
public async Task<bool> ProductExists(int productId)
{
    var product = await repository.Get(productId);
    return product != null;
}

public async Task<bool> ProductExistsByName(string name)
{
    var products = await repository.Get(filter: p => p.Name == name);
    return products.Any();
}
```

### Soft Delete Pattern

```csharp
public class Product : EntityBase
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public async Task<Product> SoftDelete(int productId)
{
    var product = await repository.Get(productId);
    
    if (product == null)
        throw new NotFoundException($"Product {productId} not found");
    
    product.IsDeleted = true;
    product.DeletedAt = DateTime.UtcNow;
    
    return await repository.Update(product);
}

public async Task<IEnumerable<Product>> GetActiveProducts()
{
    return await repository.Get(filter: p => !p.IsDeleted);
}
```

### Caching Pattern

```csharp
using Microsoft.Extensions.Caching.Memory;

public class CachedProductRepository
{
    private readonly IGenericRepository<Product> _repository;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);

    public CachedProductRepository(
        IGenericRepository<Product> repository,
        IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<Product> Get(int id)
    {
        var cacheKey = $"product_{id}";
        
        if (!_cache.TryGetValue(cacheKey, out Product product))
        {
            product = await _repository.Get(id);
            
            if (product != null)
            {
                _cache.Set(cacheKey, product, _cacheExpiration);
            }
        }
        
        return product;
    }

    public async Task<Product> Update(Product product)
    {
        var result = await _repository.Update(product);
        
        // Invalidate cache
        var cacheKey = $"product_{product.ID}";
        _cache.Remove(cacheKey);
        
        return result;
    }
}
```

## Error Handling

### Null Checks

```csharp
public async Task<Product> GetProductSafely(int productId)
{
    try
    {
        var product = await repository.Get(productId);
        
        if (product == null)
        {
            throw new NotFoundException($"Product with ID {productId} not found");
        }
        
        return product;
    }
    catch (ArgumentNullException ex)
    {
        // Handle null ID
        throw new ValidationException("Product ID cannot be null", ex);
    }
}
```

### Try-Catch Pattern

```csharp
public async Task<(bool Success, Product Product, string Error)> TryInsertProduct(Product product)
{
    try
    {
        if (product == null)
        {
            return (false, null, "Product cannot be null");
        }

        var inserted = await repository.Insert(product);
        return (true, inserted, null);
    }
    catch (ArgumentNullException ex)
    {
        return (false, null, "Invalid product data");
    }
    catch (Exception ex)
    {
        return (false, null, $"Unexpected error: {ex.Message}");
    }
}
```

### Validation Before Operations

```csharp
public async Task<Product> InsertProductWithValidation(Product product)
{
    // Validate
    if (product == null)
        throw new ArgumentNullException(nameof(product));
    
    if (string.IsNullOrWhiteSpace(product.Name))
        throw new ValidationException("Product name is required");
    
    if (product.Price <= 0)
        throw new ValidationException("Product price must be greater than zero");
    
    // Check for duplicates
    var existing = await repository.Get(filter: p => p.Name == product.Name);
    if (existing.Any())
        throw new DuplicateException($"Product with name '{product.Name}' already exists");
    
    // Insert
    return await repository.Insert(product);
}
```

## Best Practices from Examples

1. **Always null-check results** from Get operations
2. **Use meaningful variable names** that reflect business domain
3. **Combine filter and orderBy** for efficient queries
4. **Handle exceptions appropriately** based on your application's needs
5. **Use async/await consistently** throughout your codebase
6. **Consider caching** for frequently accessed data
7. **Implement soft deletes** if you need audit trails
8. **Validate input** before repository operations

For more guidance, see [Best Practices](./best-practices.md).
