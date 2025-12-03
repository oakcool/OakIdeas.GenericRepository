# Improvement Proposals

This document outlines proposed improvements to OakIdeas.GenericRepository. Each proposal includes a clear description, justification, and acceptance criteria.

## Status Legend

- 🟢 **Recommended**: High value, relatively low complexity
- 🟡 **Consideration**: Medium value or complexity
- 🔴 **Future**: High complexity or breaking change

---

## Proposal 1: Generic Key Types Support 🟢

### Description
Replace the hardcoded `int` ID in `EntityBase` with a generic type parameter to support different primary key types (Guid, long, string, etc.).

### Current Limitation
```csharp
public abstract class EntityBase
{
    public int ID { get; set; } // Fixed to int
}
```

### Proposed Solution
```csharp
public abstract class EntityBase<TKey>
{
    public TKey ID { get; set; }
}

// For backward compatibility
public abstract class EntityBase : EntityBase<int>
{
}

// Interface changes
public interface IGenericRepository<TEntity, TKey> where TEntity : class
{
    Task<TEntity> Get(TKey id);
    Task<bool> Delete(TKey id);
    // ... other methods
}

// Backward compatible interface
public interface IGenericRepository<TEntity> : IGenericRepository<TEntity, int> 
    where TEntity : class
{
}
```

### Justification
- **Flexibility**: Support Guids (common in distributed systems), longs (for large datasets), or composite keys
- **Modern patterns**: Align with common Entity Framework patterns
- **No vendor lock-in**: Don't force int keys when database uses different type

### Status
✅ **IMPLEMENTED** (Version 0.0.5.1-alpha)

### Acceptance Criteria

#### Documentation
- [x] Update API reference with generic key examples
- [x] Add migration guide for existing users
- [x] Document how to use Guid, long, and string keys
- [x] Update all code examples

#### Testing
- [x] Unit tests for each key type (int, Guid, long, string)
- [x] Backward compatibility tests (all existing tests pass)
- [ ] Integration tests with EF Core using different key types
- [ ] Performance tests comparing key types

#### Implementation
- [x] Create EntityBase<TKey>
- [x] Create backward-compatible EntityBase
- [x] Update IGenericRepository with generic key
- [x] Update MemoryGenericRepository
- [x] Update EntityFrameworkCoreRepository
- [x] Ensure backward compatibility

### Breaking Change Assessment
- **Breaking**: No - fully backward compatible
- **Implementation**: EntityBase and IGenericRepository<TEntity> maintained as aliases
- **Migration path**: Existing code continues to work with int keys, new code can use generic keys
- **Validation**: All 33 existing tests pass unchanged, 24 new tests added for generic key types

### Implementation Notes
- Changed EntityFrameworkCore project reference from PackageReference to ProjectReference
- Added `notnull` constraint on TKey type parameter
- Integer key auto-generation maintained for backward compatibility
- For non-integer keys, IDs must be provided by caller

### Estimated Effort
Medium (2-3 days) - **COMPLETED**

---

## Proposal 2: Specification Pattern Support 🟢

### Description
Implement the Specification pattern to encapsulate complex query logic and make it reusable and testable.

### Current Limitation
```csharp
// Complex filter logic scattered in service layer
var customers = await repository.Get(
    filter: c => c.IsActive && 
                 c.Orders.Any(o => o.TotalAmount > 1000) &&
                 c.CreatedDate >= DateTime.UtcNow.AddYears(-1)
);
```

### Proposed Solution
```csharp
// Specification interface
public interface ISpecification<T>
{
    Expression<Func<T, bool>> ToExpression();
    bool IsSatisfiedBy(T entity);
}

// Base specification
public abstract class Specification<T> : ISpecification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();
    
    public bool IsSatisfiedBy(T entity)
    {
        var predicate = ToExpression().Compile();
        return predicate(entity);
    }
    
    public Specification<T> And(Specification<T> specification)
    {
        return new AndSpecification<T>(this, specification);
    }
    
    public Specification<T> Or(Specification<T> specification)
    {
        return new OrSpecification<T>(this, specification);
    }
    
    public Specification<T> Not()
    {
        return new NotSpecification<T>(this);
    }
}

// Example specifications
public class ActiveCustomerSpecification : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return c => c.IsActive;
    }
}

public class HighValueCustomerSpecification : Specification<Customer>
{
    private readonly decimal _minimumValue;
    
    public HighValueCustomerSpecification(decimal minimumValue)
    {
        _minimumValue = minimumValue;
    }
    
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return c => c.Orders.Any(o => o.TotalAmount > _minimumValue);
    }
}

// Usage
var spec = new ActiveCustomerSpecification()
    .And(new HighValueCustomerSpecification(1000));

var customers = await repository.Get(filter: spec.ToExpression());
```

### Justification
- **Reusability**: Define query logic once, use everywhere
- **Testability**: Test specifications independently
- **Composability**: Combine specifications with And/Or/Not
- **Maintainability**: Centralize business rules
- **Readability**: Self-documenting query logic

### Status
✅ **IMPLEMENTED** (Version 0.0.6-alpha)

### Acceptance Criteria

#### Documentation
- [x] Add specification pattern guide
- [x] Provide 10+ real-world specification examples
- [x] Document combination strategies (And, Or, Not)
- [x] Show integration with existing repository

#### Testing
- [x] Unit tests for each specification operator
- [x] Tests for complex specification combinations
- [x] Tests for IsSatisfiedBy in-memory evaluation
- [x] Integration tests with repository

#### Implementation
- [x] Create ISpecification<T> interface
- [x] Create Specification<T> base class
- [x] Implement And/Or/Not combinators
- [x] Create example specifications
- [x] Add implicit conversion to Expression<Func<T, bool>>

### Breaking Change Assessment
- **Breaking**: No
- **Additive**: Pure addition, doesn't modify existing APIs
- **Implementation**: All specifications work seamlessly with existing repository methods

### Implementation Notes
- Added comprehensive specification pattern guide with 15+ real-world examples
- Implemented using expression tree manipulation for proper database query translation
- 29 unit and integration tests ensuring correctness
- Specifications work with both MemoryGenericRepository and EntityFrameworkCoreRepository
- Supports implicit conversion for cleaner syntax

### Estimated Effort
Medium (2-3 days) - **COMPLETED**

---

## Proposal 3: Pagination Support 🟢

### Description
Add built-in pagination support with efficient queries and metadata.

### Current Limitation
```csharp
// Manual pagination - not efficient
var all = await repository.Get();
var page = all.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
// No total count, no metadata
```

### Proposed Solution
```csharp
// Pagination request
public class PagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    public int Skip => (Page - 1) * PageSize;
}

// Pagination result
public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

// Extended interface
public interface IGenericRepository<TEntity> where TEntity : class
{
    // Existing methods...
    
    Task<PagedResult<TEntity>> GetPaged(
        PagedRequest request,
        Expression<Func<TEntity, bool>> filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        string includeProperties = "");
}

// Usage
var request = new PagedRequest { Page = 2, PageSize = 20 };
var result = await repository.GetPaged(
    request,
    filter: c => c.IsActive,
    orderBy: q => q.OrderBy(c => c.Name)
);

Console.WriteLine($"Showing {result.Items.Count} of {result.TotalCount} items");
Console.WriteLine($"Page {result.Page} of {result.TotalPages}");
```

### Justification
- **Performance**: Avoid loading all data for large datasets
- **User experience**: Standard pagination metadata
- **Efficiency**: Single query for data + count
- **Scalability**: Essential for production applications

### Acceptance Criteria

#### Documentation
- [ ] Add pagination guide with examples
- [ ] Document performance characteristics
- [ ] Show ASP.NET Core integration
- [ ] Provide client-side pagination examples

#### Testing
- [ ] Tests with small, medium, and large datasets
- [ ] Test edge cases (page beyond total, page 0, negative pageSize)
- [ ] Performance tests vs non-paged queries
- [ ] Test with filters and ordering

#### Implementation
- [ ] Create PagedRequest class
- [ ] Create PagedResult<T> class
- [ ] Add GetPaged to IGenericRepository
- [ ] Implement in MemoryGenericRepository
- [ ] Implement in EntityFrameworkCoreRepository (efficient SQL)
- [ ] Add validation for page and pageSize

### Breaking Change Assessment
- **Breaking**: No
- **Additive**: New method, existing methods unchanged

### Estimated Effort
Small (1-2 days)

---

## Proposal 4: Cancellation Token Support 🟢

### Description
Add CancellationToken parameters to all async methods for proper cancellation support.

### Current Limitation
```csharp
// Cannot cancel long-running queries
var results = await repository.Get(filter: c => c.ComplexCondition());
```

### Proposed Solution
```csharp
public interface IGenericRepository<TEntity> where TEntity : class
{
    Task<TEntity> Insert(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> Get(object id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>> filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default);
    Task<TEntity> Update(TEntity entityToUpdate, CancellationToken cancellationToken = default);
    Task<bool> Delete(TEntity entityToDelete, CancellationToken cancellationToken = default);
    Task<bool> Delete(object id, CancellationToken cancellationToken = default);
}

// Usage
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    var results = await repository.Get(
        filter: c => c.ComplexCondition(),
        cancellationToken: cts.Token
    );
}
catch (OperationCanceledException)
{
    // Handle cancellation
}
```

### Justification
- **Responsiveness**: Cancel operations when user navigates away
- **Resource efficiency**: Stop unnecessary database operations
- **ASP.NET Core best practice**: Request cancellation when client disconnects
- **Timeouts**: Implement query timeouts easily

### Status
✅ **IMPLEMENTED** (Version 0.0.7-alpha)

### Acceptance Criteria

#### Documentation
- [x] Document cancellation token usage
- [x] Show ASP.NET Core integration examples
- [x] Explain timeout scenarios
- [x] Document best practices

#### Testing
- [x] Test cancellation during operations
- [x] Test default parameter behavior
- [x] Test with already-cancelled tokens
- [x] Test backward compatibility

#### Implementation
- [x] Add CancellationToken to interface
- [x] Implement in MemoryGenericRepository
- [x] Implement in EntityFrameworkCoreRepository
- [x] Pass token through to EF Core methods
- [x] Verify all existing tests pass

### Breaking Change Assessment
- **Breaking**: No - fully backward compatible
- **Implementation**: Default parameters maintain compatibility
- **Migration path**: Existing code continues to work without modification, new code can pass cancellation tokens
- **Validation**: All 71 existing tests pass unchanged, 16 new tests added for cancellation token functionality

### Implementation Notes
- All async methods now accept an optional `CancellationToken` parameter with default value
- MemoryGenericRepository checks cancellation at the start of each operation
- EntityFrameworkCoreRepository passes cancellation token through to EF Core methods
- Comprehensive ASP.NET Core integration examples provided
- Full backward compatibility maintained

### Estimated Effort
Small (1 day) - **COMPLETED**

---

## Proposal 5: Batch Operations 🟡

### Description
Add methods for efficient batch insert, update, and delete operations.

### Current Limitation
```csharp
// Inefficient for bulk operations
foreach (var product in products)
{
    await repository.Insert(product); // N database calls
}
```

### Proposed Solution
```csharp
public interface IGenericRepository<TEntity> where TEntity : class
{
    // Existing methods...
    
    Task<IEnumerable<TEntity>> InsertRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> UpdateRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task<int> DeleteRange(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task<int> DeleteRange(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);
}

// Usage
var products = new List<Product>
{
    new Product { Name = "Product 1", Price = 10 },
    new Product { Name = "Product 2", Price = 20 },
    new Product { Name = "Product 3", Price = 30 }
};

var inserted = await repository.InsertRange(products);
Console.WriteLine($"Inserted {inserted.Count()} products");

// Bulk delete
var deletedCount = await repository.DeleteRange(p => p.Price < 15);
Console.WriteLine($"Deleted {deletedCount} products");
```

### Justification
- **Performance**: Single database round-trip vs multiple
- **Transactions**: All-or-nothing for batch operations
- **Common use case**: Data imports, bulk updates
- **EF Core support**: Already optimized in EF Core

### Status
✅ **IMPLEMENTED** (Version 0.0.8-alpha)

### Acceptance Criteria

#### Documentation
- [x] Document batch operation usage
- [x] Show performance comparisons
- [x] Document transaction behavior
- [x] Provide data import examples

#### Testing
- [x] Test with small and large batches
- [x] Test transaction rollback scenarios
- [x] Performance tests vs individual operations
- [x] Test with validation failures

#### Implementation
- [x] Add batch methods to interface
- [x] Implement in MemoryGenericRepository
- [x] Implement in EntityFrameworkCoreRepository
- [x] Add validation for null/empty collections
- [x] Optimize EF Core implementation (AddRange, etc.)

### Breaking Change Assessment
- **Breaking**: No
- **Additive**: New methods only
- **Implementation**: All batch operations work seamlessly with existing repository methods
- **Validation**: All 104 existing tests pass unchanged, 41 new tests added for batch operations

### Implementation Notes
- Added comprehensive batch operations documentation with real-world examples
- Implemented using EF Core's optimized batch methods (AddRangeAsync, UpdateRange, RemoveRange)
- 41 unit and integration tests ensuring correctness for both implementations
- Batch operations work efficiently with both MemoryGenericRepository and EntityFrameworkCoreRepository
- All methods support cancellation tokens for long-running operations
- Empty collections are handled safely
- DeleteRange with filter provides most efficient bulk delete

### Estimated Effort
Medium (2 days) - **COMPLETED**

---

## Proposal 6: Type-Safe Include Properties 🟡

### Description
Replace string-based navigation property includes with type-safe expression-based includes.

### Current Limitation
```csharp
// Typo-prone, no IntelliSense, no compile-time checking
var orders = await repository.Get(
    includeProperties: "Custmer,Items" // Typo! Runtime error
);
```

### Proposed Solution
```csharp
public interface IGenericRepository<TEntity> where TEntity : class
{
    // Existing string-based method for backward compatibility
    Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>> filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        string includeProperties = "");
    
    // New type-safe method
    Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>> filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        params Expression<Func<TEntity, object>>[] includeExpressions);
}

// Usage
var orders = await repository.Get(
    filter: o => o.OrderDate >= DateTime.UtcNow.AddDays(-30),
    includeExpressions: 
        o => o.Customer,
        o => o.Items
);

// Nested includes
var orders = await repository.Get(
    includeExpressions:
        o => o.Customer,
        o => o.Items.Select(i => i.Product)
);
```

### Justification
- **Type safety**: Compile-time checking
- **IntelliSense**: Better development experience
- **Refactoring**: Rename refactoring works correctly
- **Modern pattern**: Aligns with EF Core conventions

### Status
✅ **IMPLEMENTED** (Version 0.0.9-alpha)

### Acceptance Criteria

#### Documentation
- [x] Document both include methods
- [x] Show migration path from string-based
- [x] Document nested include syntax
- [x] Provide comparison examples

#### Testing
- [x] Test single includes
- [x] Test multiple includes
- [x] Test nested includes (with Select)
- [x] Test backward compatibility with strings

#### Implementation
- [x] Add overload with expression parameters
- [x] Implement in EntityFrameworkCoreRepository
- [x] Handle nested includes properly (via Select)
- [x] Keep string-based method for compatibility
- [x] Document that MemoryRepository ignores includes

### Breaking Change Assessment
- **Breaking**: No
- **Additive**: New overload, existing method remains
- **Implementation**: All type-safe includes work seamlessly with existing repository methods
- **Validation**: All 136 existing tests pass unchanged, 15 new tests added for type-safe includes

### Implementation Notes
- Added comprehensive type-safe includes documentation with real-world examples
- Implemented using EF Core's Include() method with expressions for proper database query translation
- 15 unit and integration tests ensuring correctness for both implementations
- Type-safe includes work with both MemoryGenericRepository and EntityFrameworkCoreRepository
- Supports single and multiple navigation properties
- Nested includes supported via LINQ Select expressions
- Full backward compatibility with string-based includes maintained

### Estimated Effort
Small (1-2 days) - **COMPLETED**

---

## Proposal 7: Async Enumerable Support 🔴

### Description
Return IAsyncEnumerable<T> for streaming large result sets without loading all into memory.

### Current Limitation
```csharp
// Loads all results into memory
var allProducts = await repository.Get(); // Could be millions of rows
foreach (var product in allProducts)
{
    // Process
}
```

### Proposed Solution
```csharp
public interface IGenericRepository<TEntity> where TEntity : class
{
    // New streaming method
    IAsyncEnumerable<TEntity> GetAsyncEnumerable(
        Expression<Func<TEntity, bool>> filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        string includeProperties = "");
}

// Usage
await foreach (var product in repository.GetAsyncEnumerable(filter: p => p.IsActive))
{
    // Process one at a time, memory efficient
    await ProcessProduct(product);
}
```

### Justification
- **Memory efficiency**: Don't load entire result set
- **Scalability**: Handle millions of records
- **Streaming**: Start processing immediately
- **Modern C#**: Leverage async streams (C# 8.0+)

### Status
✅ **IMPLEMENTED** (Version 0.0.10-alpha)

### Acceptance Criteria

#### Documentation
- [x] Document when to use async enumerable
- [x] Show memory usage comparisons
- [x] Document performance characteristics
- [x] Provide real-world examples

#### Testing
- [x] Test with large datasets
- [x] Memory usage tests
- [x] Test cancellation during enumeration
- [x] Performance benchmarks

#### Implementation
- [x] Add GetAsyncEnumerable to interface
- [x] Implement in EntityFrameworkCoreRepository
- [x] Implement in MemoryGenericRepository
- [x] Add proper cancellation support

### Breaking Change Assessment
- **Breaking**: No
- **Additive**: New method only
- **Requirements**: C# 8.0+ for consumers
- **Implementation**: All async enumerable methods work seamlessly with existing repository methods
- **Validation**: All 151 existing tests pass unchanged, 21 new tests added for async enumerable functionality

### Implementation Notes
- Added comprehensive async enumerable documentation with 15+ real-world examples
- Implemented using `IAsyncEnumerable<T>` with proper cancellation support
- 21 unit and integration tests ensuring correctness for both implementations
- Async enumerable works efficiently with both MemoryGenericRepository and EntityFrameworkCoreRepository
- Supports filtering, ordering, and include properties
- Memory efficient streaming for large datasets
- Full backward compatibility maintained
- Added Microsoft.Bcl.AsyncInterfaces package for .NET Standard 2.0 support

### Estimated Effort
Medium (2 days) - **COMPLETED**

---

## Proposal 8: Query Object Pattern 🔴

### Description
Introduce a query object to encapsulate all query parameters (filter, sort, page, include) in a single, reusable object.

### Current Limitation
```csharp
// Many parameters, hard to extend
public async Task<IEnumerable<Product>> GetProducts(
    Expression<Func<Product, bool>> filter,
    Func<IQueryable<Product>, IOrderedQueryable<Product>> orderBy,
    int page,
    int pageSize,
    string includes)
{
    // Repeated parameter passing
}
```

### Proposed Solution
```csharp
public class Query<TEntity> where TEntity : class
{
    public Expression<Func<TEntity, bool>> Filter { get; set; }
    public Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> OrderBy { get; set; }
    public List<Expression<Func<TEntity, object>>> Includes { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public bool AsNoTracking { get; set; }
    
    // Fluent API
    public Query<TEntity> Where(Expression<Func<TEntity, bool>> filter)
    {
        Filter = filter;
        return this;
    }
    
    public Query<TEntity> Include(Expression<Func<TEntity, object>> include)
    {
        Includes ??= new List<Expression<Func<TEntity, object>>>();
        Includes.Add(include);
        return this;
    }
    
    public Query<TEntity> Paged(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
        return this;
    }
}

// Repository method
Task<IEnumerable<TEntity>> Get(Query<TEntity> query);

// Usage
var query = new Query<Product>()
    .Where(p => p.IsActive)
    .Include(p => p.Category)
    .Paged(1, 20);

var products = await repository.Get(query);
```

### Justification
- **Extensibility**: Easy to add new query parameters
- **Reusability**: Define queries once, use multiple times
- **Testability**: Query objects are easy to test
- **Clarity**: Self-documenting query intent

### Acceptance Criteria

#### Documentation
- [ ] Complete query object guide
- [ ] Document fluent API
- [ ] Show query reuse patterns
- [ ] Provide migration guide

#### Testing
- [ ] Test all query options
- [ ] Test query combinations
- [ ] Test query reusability
- [ ] Performance tests

#### Implementation
- [ ] Create Query<TEntity> class
- [ ] Add fluent API methods
- [ ] Add repository method
- [ ] Keep existing methods for compatibility
- [ ] Implement in both repositories

### Breaking Change Assessment
- **Breaking**: No if existing methods remain
- **Recommended**: Deprecate individual parameter methods eventually

### Estimated Effort
Large (3-5 days)

---

## Proposal 9: Soft Delete Support 🟢

### Description
Built-in support for soft delete pattern with automatic filtering.

### Current Limitation
```csharp
// Manual soft delete implementation
public class Product : EntityBase
{
    public bool IsDeleted { get; set; }
}

// Must remember to filter everywhere
var products = await repository.Get(filter: p => !p.IsDeleted);
```

### Proposed Solution
```csharp
// Interface for soft-deletable entities
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string DeletedBy { get; set; }
}

// Base class
public abstract class SoftDeletableEntity : EntityBase, ISoftDeletable
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; }
}

// Repository automatically filters soft-deleted entities
public class SoftDeleteRepository<TEntity> : EntityFrameworkCoreRepository<TEntity, DbContext>
    where TEntity : class, ISoftDeletable
{
    public override async Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>> filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        string includeProperties = "")
    {
        // Automatically add IsDeleted filter
        Expression<Func<TEntity, bool>> notDeletedFilter = e => !e.IsDeleted;
        
        if (filter != null)
        {
            filter = CombineExpressions(filter, notDeletedFilter);
        }
        else
        {
            filter = notDeletedFilter;
        }
        
        return await base.Get(filter, orderBy, includeProperties);
    }
    
    public override async Task<bool> Delete(TEntity entity)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        return await Update(entity) != null;
    }
    
    // Method to get including deleted
    public async Task<IEnumerable<TEntity>> GetIncludingDeleted(
        Expression<Func<TEntity, bool>> filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        string includeProperties = "")
    {
        return await base.Get(filter, orderBy, includeProperties);
    }
}

// Usage
var repository = new SoftDeleteRepository<Product>(context);
await repository.Delete(product); // Soft delete
var products = await repository.Get(); // Automatically excludes deleted
var all = await repository.GetIncludingDeleted(); // Include deleted if needed
```

### Justification
- **Audit trail**: Keep history of deleted records
- **Data recovery**: Undelete functionality
- **Compliance**: Required for many applications
- **Common pattern**: Avoid reimplementing everywhere

### Acceptance Criteria

#### Documentation
- [ ] Document soft delete pattern
- [ ] Show how to permanently delete
- [ ] Document GetIncludingDeleted usage
- [ ] Provide audit trail examples

#### Testing
- [ ] Test soft delete behavior
- [ ] Test automatic filtering
- [ ] Test GetIncludingDeleted
- [ ] Test with includes and ordering

#### Implementation
- [ ] Create ISoftDeletable interface
- [ ] Create SoftDeletableEntity base class
- [ ] Create SoftDeleteRepository
- [ ] Implement automatic filtering
- [ ] Add configuration options

### Breaking Change Assessment
- **Breaking**: No
- **Additive**: New repository implementation, opt-in

### Estimated Effort
Medium (2-3 days)

---

## Proposal 10: Event/Notification Support 🔴

### Description
Raise events/notifications on entity changes for cross-cutting concerns (logging, caching, notifications).

### Proposed Solution
```csharp
public interface IRepositoryEvent<TEntity>
{
    TEntity Entity { get; }
    DateTime OccurredAt { get; }
}

public class EntityInsertedEvent<TEntity> : IRepositoryEvent<TEntity>
{
    public TEntity Entity { get; }
    public DateTime OccurredAt { get; }
}

public interface IGenericRepository<TEntity> where TEntity : class
{
    event EventHandler<EntityInsertedEvent<TEntity>> EntityInserted;
    event EventHandler<EntityUpdatedEvent<TEntity>> EntityUpdated;
    event EventHandler<EntityDeletedEvent<TEntity>> EntityDeleted;
}

// Usage
repository.EntityInserted += async (sender, e) =>
{
    await _cache.InvalidateAsync($"entity_{e.Entity.ID}");
    await _logger.LogAsync($"Entity {e.Entity.ID} created");
};
```

### Justification
- **Decoupling**: Separate cross-cutting concerns
- **Extensibility**: Add functionality without modifying repository
- **Observability**: Track all data changes

### Acceptance Criteria
- [ ] Event interfaces and implementations
- [ ] Thread-safe event raising
- [ ] Async event handler support
- [ ] Documentation and examples

### Estimated Effort
Large (3-4 days)

---

## Implementation Priority

### Phase 1: High Value, Low Complexity (1-2 weeks)
1. ✅ Update to .NET 8.0
2. ✅ Add XML documentation
3. ✅ Add comprehensive tests
4. ✅ Generic Key Types (Proposal 1)
5. ✅ Specification Pattern Support (Proposal 2)
6. ✅ Cancellation Token Support (Proposal 4)
7. ✅ Batch Operations (Proposal 5)
8. ✅ Type-Safe Include Properties (Proposal 6)
9. ✅ Async Enumerable Support (Proposal 7)
10. 🟢 Pagination Support (Proposal 3)
11. 🟢 Soft Delete Support (Proposal 9)

### Phase 2: Advanced Features (4-6 weeks)
1. 🔴 Query Object Pattern (Proposal 8)
2. 🔴 Event/Notification Support (Proposal 10)

## Contributing

To propose a new improvement:
1. Create an issue with the "enhancement" label
2. Use the proposal template in this document
3. Include description, justification, and acceptance criteria
4. Discuss with maintainers before implementation

## Feedback

We welcome feedback on these proposals. Please comment on the related GitHub issues or start a discussion.
