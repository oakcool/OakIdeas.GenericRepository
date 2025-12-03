# Type-Safe Include Properties

## Overview

Type-safe include properties provide compile-time checking and IntelliSense support when loading navigation properties in your queries. This feature eliminates typos and makes refactoring safer compared to string-based includes.

## Why Type-Safe Includes?

### Problems with String-Based Includes

```csharp
// ❌ String-based includes (old approach)
var orders = await repository.Get(
    filter: o => o.IsActive,
    includeProperties: "Customer,Items" // Typo would cause runtime error
);
```

**Issues:**
- **No compile-time checking**: Typos go undetected until runtime
- **No IntelliSense**: Manual typing without autocomplete
- **Refactoring issues**: Rename operations don't update strings
- **No type safety**: String properties could reference non-existent properties

### Benefits of Type-Safe Includes

```csharp
// ✅ Type-safe includes (new approach)
var orders = await repository.Get(
    filter: o => o.IsActive,
    includeExpressions: 
        o => o.Customer,  // Compile-time checked
        o => o.Items      // IntelliSense supported
);
```

**Benefits:**
- **Compile-time checking**: Typos cause build errors
- **IntelliSense support**: Full autocomplete when writing code
- **Refactoring support**: Rename operations automatically update includes
- **Type safety**: Only valid navigation properties can be referenced

## Basic Usage

### Single Navigation Property

```csharp
// Load customers with their orders
var customers = await repository.Get(
    includeExpressions: c => c.Orders
);
```

### Multiple Navigation Properties

```csharp
// Load orders with customer and items
var orders = await repository.Get(
    includeExpressions:
        o => o.Customer,
        o => o.Items
);
```

### With Filter

```csharp
// Get active orders with related data
var activeOrders = await repository.Get(
    filter: o => o.Status == OrderStatus.Active,
    includeExpressions:
        o => o.Customer,
        o => o.Items
);
```

### With Ordering

```csharp
// Get orders sorted by date with related data
var recentOrders = await repository.Get(
    filter: o => o.OrderDate >= DateTime.UtcNow.AddDays(-30),
    orderBy: q => q.OrderByDescending(o => o.OrderDate),
    includeExpressions:
        o => o.Customer,
        o => o.Items
);
```

### With Cancellation Token

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

var orders = await repository.Get(
    filter: o => o.IsActive,
    cancellationToken: cts.Token,
    includeExpressions:
        o => o.Customer,
        o => o.Items
);
```

## Advanced Scenarios

### Nested Includes (ThenInclude)

For nested navigation properties, use LINQ's `Select` method:

```csharp
// Load orders with items and their products
var orders = await repository.Get(
    includeExpressions:
        o => o.Items.Select(i => i.Product)
);
```

### Complex Example

```csharp
public class Order : EntityBase
{
    public string OrderNumber { get; set; }
    public Customer Customer { get; set; }
    public List<OrderItem> Items { get; set; }
    public ShippingAddress ShippingAddress { get; set; }
}

// Load orders with all related data
var orders = await repository.Get(
    filter: o => o.OrderDate >= startDate && o.OrderDate <= endDate,
    orderBy: q => q.OrderByDescending(o => o.OrderDate)
                   .ThenBy(o => o.OrderNumber),
    includeExpressions:
        o => o.Customer,
        o => o.Items,
        o => o.ShippingAddress,
        o => o.Items.Select(i => i.Product)
);
```

## Backward Compatibility

The string-based include method is still available and fully supported:

```csharp
// Old approach still works
var orders = await repository.Get(
    includeProperties: "Customer,Items"
);

// New type-safe approach
var orders = await repository.Get(
    includeExpressions:
        o => o.Customer,
        o => o.Items
);
```

Both methods can coexist in your codebase, allowing gradual migration to type-safe includes.

## Migration Guide

### From String-Based to Type-Safe

**Before:**
```csharp
var result = await repository.Get(
    filter: o => o.Status == OrderStatus.Shipped,
    includeProperties: "Customer,Items,ShippingAddress"
);
```

**After:**
```csharp
var result = await repository.Get(
    filter: o => o.Status == OrderStatus.Shipped,
    includeExpressions:
        o => o.Customer,
        o => o.Items,
        o => o.ShippingAddress
);
```

### Nested Properties

**Before:**
```csharp
var result = await repository.Get(
    includeProperties: "Items.Product"
);
```

**After:**
```csharp
var result = await repository.Get(
    includeExpressions:
        o => o.Items.Select(i => i.Product)
);
```

## Repository Implementation Notes

### Entity Framework Core Repository

The `EntityFrameworkCoreRepository` uses EF Core's `Include()` method with expressions, providing efficient database queries:

```csharp
// Translates to efficient SQL JOIN
var customers = await repository.Get(
    includeExpressions: c => c.Orders
);
```

### Memory Repository

The `MemoryGenericRepository` accepts type-safe includes for API consistency but ignores them since in-memory relationships are automatically loaded:

```csharp
// Works but includes are no-op in memory
var customers = await memoryRepository.Get(
    includeExpressions: c => c.Orders
);
```

## Performance Considerations

### Eager Loading vs Lazy Loading

Type-safe includes enable **eager loading**, which loads related entities in a single database query:

```csharp
// Single query with JOINs
var orders = await repository.Get(
    includeExpressions:
        o => o.Customer,
        o => o.Items
);
```

### Be Selective

Only include what you need to avoid over-fetching:

```csharp
// ❌ Bad - loading unnecessary data
var orders = await repository.Get(
    includeExpressions:
        o => o.Customer,
        o => o.Items,
        o => o.ShippingAddress,
        o => o.BillingAddress,
        o => o.PaymentMethod
);

// ✅ Good - only load what's needed
var orders = await repository.Get(
    includeExpressions:
        o => o.Customer,
        o => o.Items
);
```

## Common Patterns

### Service Layer Pattern

```csharp
public class OrderService
{
    private readonly IGenericRepository<Order> _orderRepository;

    public async Task<OrderDetailDto> GetOrderDetails(int orderId)
    {
        var orders = await _orderRepository.Get(
            filter: o => o.ID == orderId,
            includeExpressions:
                o => o.Customer,
                o => o.Items.Select(i => i.Product),
                o => o.ShippingAddress
        );

        var order = orders.FirstOrDefault();
        return order == null ? null : MapToDto(order);
    }
}
```

### ASP.NET Core API

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IGenericRepository<Order> _repository;

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(
        int id, 
        CancellationToken cancellationToken)
    {
        var orders = await _repository.Get(
            filter: o => o.ID == id,
            cancellationToken: cancellationToken,
            includeExpressions:
                o => o.Customer,
                o => o.Items
        );

        var order = orders.FirstOrDefault();
        if (order == null)
            return NotFound();

        return Ok(MapToDto(order));
    }
}
```

### Repository Wrapper Pattern

For complex includes, create wrapper methods:

```csharp
public class OrderRepository : EntityFrameworkCoreRepository<Order, DbContext>
{
    public OrderRepository(DbContext context) : base(context) { }

    public async Task<IEnumerable<Order>> GetOrdersWithFullDetails(
        Expression<Func<Order, bool>> filter = null,
        CancellationToken cancellationToken = default)
    {
        return await Get(
            filter: filter,
            cancellationToken: cancellationToken,
            includeExpressions:
                o => o.Customer,
                o => o.Items.Select(i => i.Product),
                o => o.ShippingAddress,
                o => o.PaymentMethod
        );
    }

    public async Task<IEnumerable<Order>> GetOrdersWithCustomer(
        Expression<Func<Order, bool>> filter = null,
        CancellationToken cancellationToken = default)
    {
        return await Get(
            filter: filter,
            cancellationToken: cancellationToken,
            includeExpressions: o => o.Customer
        );
    }
}
```

## Best Practices

1. **Use type-safe includes for new code**: They provide better developer experience and catch errors at compile time.

2. **Keep includes minimal**: Only include navigation properties you actually need.

3. **Consider projections for DTOs**: For read-only scenarios, consider using LINQ projections instead of includes:
   ```csharp
   var orderDtos = context.Orders
       .Where(o => o.IsActive)
       .Select(o => new OrderDto
       {
           OrderNumber = o.OrderNumber,
           CustomerName = o.Customer.Name
       })
       .ToListAsync();
   ```

4. **Use wrapper methods for common includes**: Encapsulate complex include patterns in repository methods.

5. **Migrate gradually**: You can use both string-based and type-safe includes in the same codebase during migration.

## Troubleshooting

### Include Not Loading Data

**Problem**: Navigation property is null or empty after include.

**Solution**: Ensure the relationship is properly configured in your DbContext:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>()
        .HasOne(o => o.Customer)
        .WithMany(c => c.Orders);
        
    modelBuilder.Entity<Order>()
        .HasMany(o => o.Items)
        .WithOne(i => i.Order);
}
```

### Ambiguous Method Call

**Problem**: Compiler can't determine which `Get` method to use.

**Solution**: Explicitly specify parameter names:

```csharp
// If ambiguous
var orders = await repository.Get(
    filter: o => o.IsActive,
    orderBy: null,
    cancellationToken: default,
    includeExpressions: o => o.Customer
);
```

### Circular References

**Problem**: Serialization errors when returning entities with includes.

**Solution**: Use DTOs to break circular references:

```csharp
public class OrderDto
{
    public int ID { get; set; }
    public string OrderNumber { get; set; }
    public CustomerDto Customer { get; set; }
    // Don't include back-references
}
```

## See Also

- [API Reference](api-reference.md)
- [Usage Examples](usage-examples.md)
- [Best Practices](best-practices.md)
- [Entity Framework Core Documentation](https://docs.microsoft.com/ef/core/)
