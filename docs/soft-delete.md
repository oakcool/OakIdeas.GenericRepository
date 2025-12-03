# Soft Delete Pattern Guide

The Soft Delete Pattern is a data deletion strategy that marks records as deleted without physically removing them from the database. OakIdeas.GenericRepository provides built-in support for this pattern through specialized repository implementations.

## Table of Contents

- [Overview](#overview)
- [Basic Usage](#basic-usage)
- [Implementation](#implementation)
- [Key Features](#key-features)
- [Real-World Examples](#real-world-examples)
- [Advanced Scenarios](#advanced-scenarios)
- [Best Practices](#best-practices)
- [Migration Guide](#migration-guide)

## Overview

The Soft Delete Pattern solves several important problems:

1. **Data Recovery**: Restore accidentally deleted records
2. **Audit Trail**: Maintain history of deleted records for compliance
3. **Referential Integrity**: Preserve relationships when deleting parent records
4. **Business Rules**: Support "undelete" functionality in user interfaces
5. **Data Analytics**: Include deleted records in historical reports

### When to Use Soft Delete

✅ **Use soft delete when:**
- You need to maintain audit trails
- Users should be able to restore deleted records
- Regulatory compliance requires data retention
- You need to preserve referential integrity
- Analytics require historical deleted data

❌ **Avoid soft delete when:**
- Data privacy laws require permanent deletion (e.g., GDPR "right to be forgotten")
- Storage costs are a primary concern
- Deleted records have no business value
- You need to truly remove sensitive information

## Basic Usage

### 1. Define a Soft Deletable Entity

```csharp
using OakIdeas.GenericRepository.Models;

// Using integer primary key
public class Product : SoftDeletableEntity
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
}

// Using Guid primary key
public class Order : SoftDeletableEntity<Guid>
{
    public Guid CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
}
```

### 2. Create a Soft Delete Repository

```csharp
using OakIdeas.GenericRepository;

// In-memory repository (for testing)
var repository = new SoftDeleteMemoryRepository<Product>();

// Entity Framework Core repository (for production)
var dbContext = new MyDbContext();
var efRepository = new SoftDeleteEntityFrameworkCoreRepository<Product, MyDbContext>(dbContext);
```

### 3. Basic Operations

```csharp
// Insert - works the same as regular repository
var product = await repository.Insert(new Product 
{ 
    Name = "Laptop", 
    Price = 999.99m 
});

// Soft delete - marks as deleted but keeps in database
await repository.Delete(product);
// OR
await repository.Delete(product.ID);

// Get - automatically excludes soft-deleted entities
var activeProducts = await repository.Get();
// product is NOT included

// Get including deleted - retrieves all records
var allProducts = await repository.GetIncludingDeleted();
// product IS included

// Restore - undelete a soft-deleted entity
var restored = await repository.Restore(product);
// OR
var restored = await repository.Restore(product.ID);

// Permanently delete - physically removes from database
await repository.PermanentlyDelete(product);
```

## Implementation

### ISoftDeletable Interface

All soft-deletable entities implement this interface:

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
```

### SoftDeletableEntity Base Classes

```csharp
// Generic key type
public abstract class SoftDeletableEntity<TKey> : EntityBase<TKey>, ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

// Integer key (backward compatible)
public abstract class SoftDeletableEntity : SoftDeletableEntity<int>
{
}
```

## Key Features

### 1. Automatic Filtering

All query methods automatically exclude soft-deleted entities:

```csharp
// These all exclude soft-deleted entities automatically
var all = await repository.Get();
var filtered = await repository.Get(filter: p => p.Price > 100);
var ordered = await repository.Get(orderBy: q => q.OrderBy(p => p.Name));
var byId = await repository.Get(productId);

// Async enumerable also filters
await foreach (var product in repository.GetAsyncEnumerable())
{
    // Only active products
}
```

### 2. Query Object Integration

Works seamlessly with the Query Object pattern:

```csharp
var query = new Query<Product>()
    .Where(p => p.Price > 100)
    .Sort(q => q.OrderBy(p => p.Name))
    .Paged(1, 20);

// Automatically excludes soft-deleted
var products = await repository.Get(query);
```

### 3. Tracking Who Deleted

Track which user deleted an entity:

```csharp
var repository = new SoftDeleteEntityFrameworkCoreRepository<Product, MyDbContext>(dbContext);

// Set the user who is performing the deletion
repository.SetDeletedBy("john.doe@example.com");

// This will record the user in DeletedBy field
await repository.Delete(product);

Console.WriteLine($"Deleted by: {product.DeletedBy}");
// Output: Deleted by: john.doe@example.com
```

### 4. Batch Operations

Soft delete supports batch operations:

```csharp
// Delete range of entities
var obsoleteProducts = new[] { product1, product2, product3 };
var deletedCount = await repository.DeleteRange(obsoleteProducts);

// Delete by filter
var deletedCount = await repository.DeleteRange(p => p.Price < 10);

Console.WriteLine($"Soft-deleted {deletedCount} products");
```

## Real-World Examples

### Example 1: E-Commerce Product Catalog

```csharp
public class Product : SoftDeletableEntity
{
    public string SKU { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}

public class ProductService
{
    private readonly SoftDeleteEntityFrameworkCoreRepository<Product, AppDbContext> _repository;
    private readonly ICurrentUserService _currentUserService;

    public ProductService(
        AppDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _repository = new SoftDeleteEntityFrameworkCoreRepository<Product, AppDbContext>(dbContext);
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<Product>> GetActiveProductsAsync()
    {
        // Only returns active (not deleted) products
        return await _repository.Get(
            filter: p => p.StockQuantity > 0,
            orderBy: q => q.OrderBy(p => p.Name)
        );
    }

    public async Task<bool> DiscontinueProductAsync(int productId)
    {
        // Track who discontinued the product
        _repository.SetDeletedBy(_currentUserService.GetUsername());
        
        // Soft delete
        return await _repository.Delete(productId);
    }

    public async Task<IEnumerable<Product>> GetDiscontinuedProductsAsync()
    {
        // Get only soft-deleted products
        var all = await _repository.GetIncludingDeleted();
        return all.Where(p => p.IsDeleted);
    }

    public async Task<Product?> RestoreProductAsync(int productId)
    {
        // Restore a discontinued product
        return await _repository.Restore(productId);
    }
}
```

### Example 2: Customer Management with Compliance

```csharp
public class Customer : SoftDeletableEntity<Guid>
{
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime RegistrationDate { get; set; }
}

public class CustomerService
{
    private readonly SoftDeleteEntityFrameworkCoreRepository<Customer, AppDbContext, Guid> _repository;

    public async Task<bool> DeactivateCustomerAsync(Guid customerId, string reason)
    {
        _repository.SetDeletedBy($"System - {reason}");
        return await _repository.Delete(customerId);
    }

    public async Task<IEnumerable<Customer>> GetInactiveCustomersAsync(DateTime since)
    {
        var all = await _repository.GetIncludingDeleted(
            filter: c => c.DeletedAt >= since
        );
        return all.Where(c => c.IsDeleted);
    }

    public async Task<bool> PermanentlyRemoveCustomerAsync(Guid customerId)
    {
        // GDPR: Right to be forgotten
        // This physically deletes the customer
        return await _repository.PermanentlyDelete(customerId);
    }

    public async Task<Customer?> ReactivateCustomerAsync(Guid customerId)
    {
        return await _repository.Restore(customerId);
    }
}
```

### Example 3: Document Management System

```csharp
public class Document : SoftDeletableEntity
{
    public string Title { get; set; }
    public string Content { get; set; }
    public int AuthorId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}

public class DocumentService
{
    private readonly SoftDeleteEntityFrameworkCoreRepository<Document, AppDbContext> _repository;

    public async Task<IEnumerable<Document>> SearchDocumentsAsync(string searchTerm)
    {
        // Search only active documents
        return await _repository.Get(
            filter: d => d.Title.Contains(searchTerm) || d.Content.Contains(searchTerm)
        );
    }

    public async Task<bool> MoveToTrashAsync(int documentId, string userId)
    {
        _repository.SetDeletedBy(userId);
        return await _repository.Delete(documentId);
    }

    public async Task<IEnumerable<Document>> GetTrashAsync(string userId)
    {
        // Get user's deleted documents from the last 30 days
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var all = await _repository.GetIncludingDeleted(
            filter: d => d.DeletedBy == userId && d.DeletedAt >= cutoffDate
        );
        return all.Where(d => d.IsDeleted);
    }

    public async Task<Document?> RestoreFromTrashAsync(int documentId)
    {
        return await _repository.Restore(documentId);
    }

    public async Task EmptyTrashAsync(string userId)
    {
        // Permanently delete documents older than 30 days
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var oldDeleted = await _repository.GetIncludingDeleted(
            filter: d => d.DeletedBy == userId && d.DeletedAt < cutoffDate
        );
        
        foreach (var doc in oldDeleted.Where(d => d.IsDeleted))
        {
            await _repository.PermanentlyDelete(doc);
        }
    }
}
```

### Example 4: Task Management with Team Collaboration

```csharp
public class Task : SoftDeletableEntity
{
    public string Title { get; set; }
    public string Description { get; set; }
    public int AssignedToUserId { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
}

public class TaskService
{
    private readonly SoftDeleteEntityFrameworkCoreRepository<Task, AppDbContext> _repository;

    public async Task<IEnumerable<Task>> GetActiveTasksAsync(int userId)
    {
        return await _repository.Get(
            filter: t => t.AssignedToUserId == userId && !t.IsCompleted,
            orderBy: q => q.OrderBy(t => t.DueDate)
        );
    }

    public async Task<bool> ArchiveTaskAsync(int taskId, string archivedBy)
    {
        _repository.SetDeletedBy(archivedBy);
        return await _repository.Delete(taskId);
    }

    public async Task<IEnumerable<Task>> GetArchivedTasksAsync()
    {
        var all = await _repository.GetIncludingDeleted();
        return all.Where(t => t.IsDeleted)
                 .OrderByDescending(t => t.DeletedAt);
    }

    public async Task<int> BulkArchiveCompletedTasksAsync(DateTime before, string userId)
    {
        return await _repository.DeleteRange(
            t => t.IsCompleted && t.DueDate < before
        );
    }
}
```

## Advanced Scenarios

### Combining with Specification Pattern

```csharp
public class ActiveProductSpecification : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.StockQuantity > 0 && p.Price > 0;
    }
}

public class ExpensiveProductSpecification : Specification<Product>
{
    private readonly decimal _minPrice;

    public ExpensiveProductSpecification(decimal minPrice)
    {
        _minPrice = minPrice;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.Price >= _minPrice;
    }
}

// Usage
var spec = new ActiveProductSpecification()
    .And(new ExpensiveProductSpecification(1000));

// Soft delete repository automatically excludes deleted products
var products = await repository.Get(filter: spec.ToExpression());
```

### Using with Async Enumerable

```csharp
// Process large dataset without loading all into memory
await foreach (var product in repository.GetAsyncEnumerable(
    filter: p => p.Price > 100,
    orderBy: q => q.OrderBy(p => p.Name)))
{
    // Only active (non-deleted) products are returned
    await ProcessProductAsync(product);
}
```

### Integration with ASP.NET Core

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly SoftDeleteEntityFrameworkCoreRepository<Product, AppDbContext> _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProductsController(
        AppDbContext dbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = new SoftDeleteEntityFrameworkCoreRepository<Product, AppDbContext>(dbContext);
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        var products = await _repository.Get();
        return Ok(products);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        var username = _httpContextAccessor.HttpContext.User.Identity?.Name 
            ?? "Anonymous";
        
        _repository.SetDeletedBy(username);
        var result = await _repository.Delete(id);
        
        if (!result)
            return NotFound();
            
        return NoContent();
    }

    [HttpPost("{id}/restore")]
    public async Task<ActionResult<Product>> RestoreProduct(int id)
    {
        var product = await _repository.Restore(id);
        
        if (product == null)
            return NotFound();
            
        return Ok(product);
    }

    [HttpGet("trash")]
    public async Task<ActionResult<IEnumerable<Product>>> GetDeletedProducts()
    {
        var all = await _repository.GetIncludingDeleted();
        var deleted = all.Where(p => p.IsDeleted);
        return Ok(deleted);
    }
}
```

## Best Practices

### 1. Database Indexes

Add indexes on soft delete columns for better query performance:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>(entity =>
    {
        entity.HasIndex(e => e.IsDeleted);
        entity.HasIndex(e => new { e.IsDeleted, e.DeletedAt });
    });
}
```

### 2. Automatic Timestamps

Entity Framework Core can automatically set timestamps:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var entries = ChangeTracker.Entries<ISoftDeletable>();
    
    foreach (var entry in entries)
    {
        if (entry.State == EntityState.Modified && entry.Entity.IsDeleted)
        {
            if (entry.Entity.DeletedAt == null)
            {
                entry.Entity.DeletedAt = DateTime.UtcNow;
            }
        }
    }
    
    return await base.SaveChangesAsync(cancellationToken);
}
```

### 3. Cascading Soft Deletes

Handle related entities when soft deleting:

```csharp
public async Task<bool> DeleteOrderWithItemsAsync(int orderId)
{
    var order = await _orderRepository.Get(orderId);
    if (order == null) return false;

    // Soft delete all order items first
    var items = await _orderItemRepository.Get(filter: i => i.OrderId == orderId);
    await _orderItemRepository.DeleteRange(items);

    // Then soft delete the order
    return await _orderRepository.Delete(order);
}
```

### 4. Validation Before Permanent Delete

```csharp
public async Task<bool> PermanentlyDeleteProductAsync(int productId)
{
    var product = await _repository.Get(productId);
    
    if (product == null || !product.IsDeleted)
    {
        throw new InvalidOperationException(
            "Only soft-deleted products can be permanently deleted");
    }

    // Check if enough time has passed (e.g., 30 days grace period)
    var gracePeriod = TimeSpan.FromDays(30);
    if (DateTime.UtcNow - product.DeletedAt < gracePeriod)
    {
        throw new InvalidOperationException(
            $"Product can only be permanently deleted after {gracePeriod.TotalDays} days");
    }

    return await _repository.PermanentlyDelete(product);
}
```

### 5. Audit Logging

```csharp
public async Task<bool> DeleteWithAuditAsync(Product product, string reason)
{
    _repository.SetDeletedBy($"{_currentUser.Username} - {reason}");
    
    var result = await _repository.Delete(product);
    
    if (result)
    {
        await _auditLogger.LogAsync(new AuditEntry
        {
            Action = "SoftDelete",
            EntityType = typeof(Product).Name,
            EntityId = product.ID,
            User = _currentUser.Username,
            Timestamp = DateTime.UtcNow,
            Details = reason
        });
    }
    
    return result;
}
```

## Migration Guide

### From Regular Repository to Soft Delete Repository

#### Step 1: Update Entity Model

```csharp
// Before
public class Product : EntityBase
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// After
public class Product : SoftDeletableEntity
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

#### Step 2: Create Database Migration

```bash
# Entity Framework Core
dotnet ef migrations add AddSoftDeleteToProducts
dotnet ef database update
```

The migration will add three columns:
- `IsDeleted` (bit/boolean)
- `DeletedAt` (datetime, nullable)
- `DeletedBy` (nvarchar, nullable)

#### Step 3: Update Repository Instantiation

```csharp
// Before
var repository = new EntityFrameworkCoreRepository<Product, AppDbContext>(dbContext);

// After
var repository = new SoftDeleteEntityFrameworkCoreRepository<Product, AppDbContext>(dbContext);
```

#### Step 4: Handle Existing Data

```csharp
// Mark all existing records as not deleted
await dbContext.Database.ExecuteSqlRawAsync(
    "UPDATE Products SET IsDeleted = 0 WHERE IsDeleted IS NULL");
```

### Considerations

- **Breaking Changes**: Existing queries will now exclude soft-deleted entities
- **Data Volume**: Soft delete increases database size over time
- **Cleanup**: Implement a periodic cleanup process for old soft-deleted records
- **Performance**: Add appropriate indexes on `IsDeleted` column

## Summary

The Soft Delete Pattern in OakIdeas.GenericRepository provides:

✅ **Automatic filtering** of deleted entities  
✅ **Easy restoration** of deleted records  
✅ **Audit trail** with deletion timestamps and user tracking  
✅ **Batch operations** for efficient bulk soft deletes  
✅ **Seamless integration** with Query Objects and Specifications  
✅ **Permanent deletion** option when needed  
✅ **Full backward compatibility** with existing code  

Use soft delete when you need data recovery, audit trails, or regulatory compliance while maintaining the simplicity of the repository pattern.
