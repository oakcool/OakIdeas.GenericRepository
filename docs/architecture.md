# Architecture Overview

## Design Pattern

OakIdeas.GenericRepository implements the **Repository Pattern**, which provides an abstraction layer between the domain and data mapping layers. This pattern offers several benefits:

- **Separation of Concerns**: Business logic is isolated from data access logic
- **Testability**: Easy to mock repositories for unit testing
- **Flexibility**: Can swap implementations without changing business logic
- **Maintainability**: Centralized data access logic

## Component Structure

```
OakIdeas.GenericRepository (Core)
├── IGenericRepository<TEntity>      # Generic repository interface
├── MemoryGenericRepository<TEntity> # In-memory implementation
└── Models
    └── EntityBase                   # Base entity class

OakIdeas.GenericRepository.EntityFrameworkCore
└── EntityFrameworkCoreRepository<TEntity, TDataContext> # EF Core implementation
```

## Core Components

### IGenericRepository<TEntity>

The central interface that defines CRUD operations:

```csharp
public interface IGenericRepository<TEntity> where TEntity : class
{
    Task<TEntity> Insert(TEntity entity);
    Task<TEntity> Get(object id);
    Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>> filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        string includeProperties = "");
    Task<TEntity> Update(TEntity entityToUpdate);
    Task<bool> Delete(TEntity entityToDelete);
    Task<bool> Delete(object id);
}
```

### EntityBase

Base class for entities with a simple integer primary key:

```csharp
public abstract class EntityBase
{
    public int ID { get; set; }
}
```

**Note**: While the current implementation uses `int` for the primary key, future versions may support generic key types for greater flexibility.

## Implementation Details

### MemoryGenericRepository

- **Storage**: Uses `ConcurrentDictionary<int, TEntity>` for thread-safe operations
- **ID Generation**: Auto-increments from the maximum existing ID
- **Use Cases**: Testing, development, POC scenarios
- **Performance**: O(1) for Get by ID, O(n) for filtered queries

Key characteristics:
- Thread-safe for concurrent access
- Data is lost when application restarts
- No transaction support (not needed for in-memory)
- Lightweight and fast

### EntityFrameworkCoreRepository

- **Storage**: Delegates to Entity Framework Core DbContext
- **ID Generation**: Handled by the database
- **Use Cases**: Production scenarios with relational databases
- **Performance**: Depends on underlying database and indexes

Key characteristics:
- Full EF Core feature support
- Automatic change tracking
- Transaction support through DbContext
- Eager loading support via `includeProperties`
- Database persistence

## Data Flow

### Insert Operation

```
Client Code
    ↓
IGenericRepository.Insert()
    ↓
Implementation (Memory or EF Core)
    ↓
Validation (null check)
    ↓
ID Assignment (if needed)
    ↓
Storage/Database
    ↓
Return inserted entity
```

### Query Operation with Filter

```
Client Code
    ↓
IGenericRepository.Get(filter, orderBy)
    ↓
Build IQueryable
    ↓
Apply filter (Where clause)
    ↓
Apply ordering (OrderBy)
    ↓
Execute query
    ↓
Return IEnumerable<TEntity>
```

## Design Decisions

### Why Async/Await Throughout?

All methods are async to:
- Support scalable applications
- Prevent blocking I/O operations
- Align with modern .NET best practices
- Allow future optimization for I/O-bound operations

Even the in-memory implementation uses `Task` return types to maintain interface compatibility, though it uses `Task.FromResult()` rather than actual async operations.

### Why ConcurrentDictionary for Memory Implementation?

- Thread-safe without explicit locking
- Good performance for concurrent scenarios
- Commonly used pattern in .NET

### Why String-Based includeProperties?

The current implementation uses comma-separated strings for navigation properties:
- **Pros**: Simple, works with EF Core's Include method
- **Cons**: Not type-safe, prone to typos, no compile-time checking

Future versions could use expression-based includes for type safety.

## Thread Safety

### MemoryGenericRepository
✅ Thread-safe - uses ConcurrentDictionary

### EntityFrameworkCoreRepository
⚠️ **Not thread-safe by default** - DbContext is not thread-safe. Use one of these patterns:
- Create a new DbContext per operation
- Use dependency injection with scoped lifetime
- Implement proper locking if sharing DbContext

## Extensibility

The architecture is designed for extensibility:

1. **Custom Implementations**: Implement `IGenericRepository<TEntity>` for other data stores (MongoDB, Redis, etc.)
2. **Decorators**: Wrap implementations to add caching, logging, etc.
3. **Base Classes**: Inherit from existing implementations and override methods
4. **Generic Constraints**: Add additional constraints to your entity types

Example custom implementation:

```csharp
public class CachedRepository<TEntity> : IGenericRepository<TEntity> 
    where TEntity : EntityBase
{
    private readonly IGenericRepository<TEntity> _innerRepository;
    private readonly IMemoryCache _cache;

    public CachedRepository(IGenericRepository<TEntity> innerRepository, IMemoryCache cache)
    {
        _innerRepository = innerRepository;
        _cache = cache;
    }

    // Implement caching logic...
}
```

## Future Architecture Considerations

See [improvement-proposals.md](./improvement-proposals.md) for detailed proposals on:
- Generic key types
- Specification pattern
- Pagination support
- Batch operations
- Cancellation token support
