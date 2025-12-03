# Usage Examples

This document provides comprehensive examples of using OakIdeas.GenericRepository in various scenarios.

## Table of Contents

- [Working with Different Key Types](#working-with-different-key-types)
- [Basic CRUD Operations](#basic-crud-operations)
- [Filtering and Querying](#filtering-and-querying)
- [Ordering Results](#ordering-results)
- [Eager Loading (Entity Framework Core)](#eager-loading-entity-framework-core)
- [Cancellation Token Support](#cancellation-token-support)
- [Batch Operations](#batch-operations)
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

## Cancellation Token Support

All async methods support cancellation tokens to allow cancellation of long-running operations. This is essential for responsive applications and proper resource management.

### Basic Cancellation Token Usage

```csharp
using System.Threading;

var repository = new MemoryGenericRepository<Customer>();
var cts = new CancellationTokenSource();

// Pass cancellation token to any async method
var customer = await repository.Get(1, cts.Token);
var customers = await repository.Get(
    filter: c => c.IsActive,
    cancellationToken: cts.Token
);
var inserted = await repository.Insert(new Customer { Name = "John" }, cts.Token);
var updated = await repository.Update(customer, cts.Token);
var deleted = await repository.Delete(1, cts.Token);
```

### Timeout Scenario

```csharp
// Set a 5-second timeout for the operation
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    var customers = await repository.Get(
        filter: c => c.Orders.Any(o => o.Total > 1000),
        orderBy: q => q.OrderBy(c => c.Name),
        cancellationToken: cts.Token
    );
    
    Console.WriteLine($"Found {customers.Count()} high-value customers");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Query took too long and was cancelled");
}
```

### User-Initiated Cancellation

```csharp
// In a console application or desktop app
var cts = new CancellationTokenSource();

// Start long-running operation
var task = Task.Run(async () =>
{
    var customers = await repository.Get(cancellationToken: cts.Token);
    foreach (var customer in customers)
    {
        // Process customer
        await ProcessCustomerAsync(customer, cts.Token);
    }
});

// User presses Ctrl+C or clicks Cancel button
Console.WriteLine("Press any key to cancel...");
Console.ReadKey();
cts.Cancel();

try
{
    await task;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation cancelled by user");
}
```

### ASP.NET Core Integration

ASP.NET Core provides automatic cancellation tokens when clients disconnect:

```csharp
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IGenericRepository<Customer> _repository;

    public CustomersController(IGenericRepository<Customer> repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken) // Automatically provided by ASP.NET Core
    {
        var customers = await _repository.Get(
            filter: isActive.HasValue ? c => c.IsActive == isActive.Value : null,
            orderBy: q => q.OrderBy(c => c.Name),
            cancellationToken: cancellationToken // Cancels if client disconnects
        );
        
        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetCustomer(
        int id, 
        CancellationToken cancellationToken)
    {
        var customer = await _repository.Get(id, cancellationToken);
        
        if (customer == null)
            return NotFound();
        
        return Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> CreateCustomer(
        [FromBody] Customer customer,
        CancellationToken cancellationToken)
    {
        var created = await _repository.Insert(customer, cancellationToken);
        return CreatedAtAction(nameof(GetCustomer), new { id = created.ID }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Customer>> UpdateCustomer(
        int id,
        [FromBody] Customer customer,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.Get(id, cancellationToken);
        if (existing == null)
            return NotFound();
        
        customer.ID = id;
        var updated = await _repository.Update(customer, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCustomer(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _repository.Delete(id, cancellationToken);
        
        if (!deleted)
            return NotFound();
        
        return NoContent();
    }
}
```

### Chaining Operations with Same Token

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

try
{
    // Multiple operations sharing the same cancellation token
    var customer = await repository.Get(1, cts.Token);
    
    customer.LastModified = DateTime.UtcNow;
    var updated = await repository.Update(customer, cts.Token);
    
    var allCustomers = await repository.Get(
        filter: c => c.IsActive,
        cancellationToken: cts.Token
    );
    
    Console.WriteLine($"Updated customer and retrieved {allCustomers.Count()} active customers");
}
catch (OperationCanceledException)
{
    Console.WriteLine("One or more operations were cancelled");
}
finally
{
    cts.Dispose();
}
```

### Conditional Cancellation

```csharp
var cts = new CancellationTokenSource();

// Cancel after processing 1000 items
int processedCount = 0;
var customers = await repository.Get(cancellationToken: cts.Token);

foreach (var customer in customers)
{
    if (processedCount >= 1000)
    {
        cts.Cancel();
        break;
    }
    
    // Process customer
    await ProcessCustomerAsync(customer);
    processedCount++;
}
```

### Backward Compatibility

All existing code continues to work without modification:

```csharp
// No cancellation token - works exactly as before
var customer = await repository.Get(1);
var customers = await repository.Get(filter: c => c.IsActive);
await repository.Insert(new Customer { Name = "John" });
await repository.Update(customer);
await repository.Delete(1);
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

## Batch Operations

Batch operations allow you to perform multiple insert, update, or delete operations efficiently in a single call. This is significantly more efficient than performing individual operations, especially with Entity Framework Core where it reduces the number of database round-trips.

### Inserting Multiple Entities

```csharp
using OakIdeas.GenericRepository;

public class Product : EntityBase
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// Create a list of products
var products = new List<Product>
{
    new Product { Name = "Laptop", Price = 999.99m },
    new Product { Name = "Mouse", Price = 24.99m },
    new Product { Name = "Keyboard", Price = 79.99m },
    new Product { Name = "Monitor", Price = 299.99m }
};

// Insert all products in a single operation
var repository = new MemoryGenericRepository<Product>();
var insertedProducts = await repository.InsertRange(products);

Console.WriteLine($"Inserted {insertedProducts.Count()} products");
// Output: Inserted 4 products
```

### Updating Multiple Entities

```csharp
// Get products to update
var productsToUpdate = await repository.Get(filter: p => p.Price < 100);

// Apply a discount to all products
foreach (var product in productsToUpdate)
{
    product.Price *= 0.9m; // 10% discount
}

// Update all products in a single operation
var updatedProducts = await repository.UpdateRange(productsToUpdate);

Console.WriteLine($"Applied discount to {updatedProducts.Count()} products");
```

### Deleting Multiple Entities

```csharp
// Get products to delete
var productsToDelete = await repository.Get(filter: p => p.Price < 10);

// Delete all products in a single operation
var deletedCount = await repository.DeleteRange(productsToDelete);

Console.WriteLine($"Deleted {deletedCount} products");
```

### Deleting by Filter

The most efficient way to delete multiple entities is by using a filter expression:

```csharp
// Delete all products with price less than 10
var deletedCount = await repository.DeleteRange(p => p.Price < 10);

Console.WriteLine($"Deleted {deletedCount} low-priced products");

// Delete all products from a specific category
var deletedCount2 = await repository.DeleteRange(p => p.Name.StartsWith("Obsolete"));

Console.WriteLine($"Deleted {deletedCount2} obsolete products");
```

### Data Import Scenario

```csharp
public class ProductImporter
{
    private readonly IGenericRepository<Product> _repository;
    
    public ProductImporter(IGenericRepository<Product> repository)
    {
        _repository = repository;
    }
    
    public async Task<ImportResult> ImportProductsFromCsv(string filePath)
    {
        var products = new List<Product>();
        
        // Read products from CSV (simplified example)
        foreach (var line in File.ReadLines(filePath).Skip(1)) // Skip header
        {
            var parts = line.Split(',');
            products.Add(new Product
            {
                Name = parts[0],
                Price = decimal.Parse(parts[1])
            });
        }
        
        // Insert all products in a single batch operation
        var startTime = DateTime.UtcNow;
        var inserted = await _repository.InsertRange(products);
        var duration = DateTime.UtcNow - startTime;
        
        return new ImportResult
        {
            TotalRecords = inserted.Count(),
            Duration = duration,
            RecordsPerSecond = inserted.Count() / duration.TotalSeconds
        };
    }
}
```

### Bulk Update Scenario

```csharp
public class PriceUpdater
{
    private readonly IGenericRepository<Product> _repository;
    
    public async Task ApplySeasonalDiscounts(string category, decimal discountPercent)
    {
        // Get all products in the category
        var products = await _repository.Get(
            filter: p => p.Category == category && p.IsActive
        );
        
        // Apply discount
        var productsToUpdate = products.ToList();
        foreach (var product in productsToUpdate)
        {
            product.Price *= (1 - discountPercent / 100);
            product.LastModified = DateTime.UtcNow;
        }
        
        // Update all products in a single batch
        var updated = await _repository.UpdateRange(productsToUpdate);
        
        Console.WriteLine($"Applied {discountPercent}% discount to {updated.Count()} {category} products");
    }
}
```

### Batch Operations with Cancellation

```csharp
public async Task ImportLargeDataset(List<Product> products, CancellationToken cancellationToken)
{
    try
    {
        // Process in batches of 1000
        const int batchSize = 1000;
        var totalInserted = 0;
        
        for (int i = 0; i < products.Count; i += batchSize)
        {
            var batch = products.Skip(i).Take(batchSize);
            var inserted = await _repository.InsertRange(batch, cancellationToken);
            totalInserted += inserted.Count();
            
            Console.WriteLine($"Progress: {totalInserted}/{products.Count} products imported");
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Import was cancelled by user");
        throw;
    }
}
```

### Batch Delete with Validation

```csharp
public async Task<int> DeleteInactiveProducts(DateTime olderThan)
{
    // Delete all products that haven't been modified in over a year
    var deletedCount = await _repository.DeleteRange(
        p => p.LastModified < olderThan && !p.IsActive
    );
    
    if (deletedCount > 0)
    {
        Console.WriteLine($"Cleaned up {deletedCount} inactive products");
    }
    
    return deletedCount;
}
```

### Performance Comparison

```csharp
public async Task PerformanceComparison()
{
    var products = Enumerable.Range(1, 1000)
        .Select(i => new Product { Name = $"Product {i}", Price = i * 10 })
        .ToList();
    
    // Method 1: Individual inserts
    var repository1 = new MemoryGenericRepository<Product>();
    var startTime1 = DateTime.UtcNow;
    
    foreach (var product in products)
    {
        await repository1.Insert(product);
    }
    
    var duration1 = DateTime.UtcNow - startTime1;
    Console.WriteLine($"Individual inserts: {duration1.TotalMilliseconds}ms");
    
    // Method 2: Batch insert
    var repository2 = new MemoryGenericRepository<Product>();
    var startTime2 = DateTime.UtcNow;
    
    await repository2.InsertRange(products);
    
    var duration2 = DateTime.UtcNow - startTime2;
    Console.WriteLine($"Batch insert: {duration2.TotalMilliseconds}ms");
    Console.WriteLine($"Performance improvement: {duration1.TotalMilliseconds / duration2.TotalMilliseconds:F2}x faster");
}
```

### Entity Framework Core Batch Operations

When using Entity Framework Core, batch operations are highly optimized and translated to efficient SQL:

```csharp
using OakIdeas.GenericRepository.EntityFrameworkCore;

public class ProductService
{
    private readonly EntityFrameworkCoreRepository<Product, MyDbContext> _repository;
    
    public async Task BulkOperationsExample()
    {
        // InsertRange uses AddRangeAsync - single database round-trip
        var newProducts = new List<Product> { /* ... */ };
        await _repository.InsertRange(newProducts);
        
        // UpdateRange uses UpdateRange - efficient batch update
        var productsToUpdate = await _repository.Get(filter: p => p.Category == "Electronics");
        foreach (var product in productsToUpdate)
        {
            product.Price *= 1.1m; // 10% price increase
        }
        await _repository.UpdateRange(productsToUpdate);
        
        // DeleteRange with filter - efficient bulk delete
        await _repository.DeleteRange(p => p.IsDiscontinued && p.Stock == 0);
    }
}
```

### Best Practices for Batch Operations

1. **Use batch operations for multiple entities** - Always prefer `InsertRange`, `UpdateRange`, or `DeleteRange` over loops with individual operations
2. **Consider batch size** - For very large datasets (10,000+ records), consider processing in smaller batches (e.g., 1,000 at a time)
3. **Use filter-based deletion** - `DeleteRange(filter)` is more efficient than retrieving entities first
4. **Handle empty collections** - All batch operations safely handle empty collections and return appropriate defaults
5. **Validate before batch operations** - Validate all entities before calling batch operations to avoid partial failures
6. **Use cancellation tokens** - For long-running batch operations, pass cancellation tokens to allow user cancellation
7. **Transaction considerations** - Batch operations execute in a single transaction, so all succeed or all fail
8. **Monitor performance** - For Entity Framework Core, use logging to verify that batch operations are generating efficient SQL

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
