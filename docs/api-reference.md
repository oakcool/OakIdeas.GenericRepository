# API Reference

Complete reference documentation for all public APIs in OakIdeas.GenericRepository.

## Interfaces

### IGenericRepository<TEntity>

Generic repository interface for CRUD operations.

**Namespace**: `OakIdeas.GenericRepository`

**Type Constraints**: `where TEntity : class`

#### Methods

##### Insert

```csharp
Task<TEntity> Insert(TEntity entity)
```

Inserts a new entity into the repository.

**Parameters:**
- `entity` (TEntity): The entity to insert. Cannot be null.

**Returns:** Task<TEntity> - The inserted entity with generated ID.

**Exceptions:**
- `ArgumentNullException`: Thrown when entity is null.

**Example:**
```csharp
var customer = new Customer { Name = "John Doe" };
var inserted = await repository.Insert(customer);
Console.WriteLine($"Created with ID: {inserted.ID}");
```

---

##### Get (by ID)

```csharp
Task<TEntity> Get(object id)
```

Gets an entity by its primary key.

**Parameters:**
- `id` (object): The primary key value. Cannot be null.

**Returns:** Task<TEntity> - The entity if found, null otherwise.

**Exceptions:**
- `ArgumentNullException`: Thrown when id is null.

**Example:**
```csharp
var customer = await repository.Get(1);
if (customer != null)
{
    Console.WriteLine(customer.Name);
}
```

---

##### Get (with filter)

```csharp
Task<IEnumerable<TEntity>> Get(
    Expression<Func<TEntity, bool>> filter = null,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
    string includeProperties = "")
```

Gets entities with optional filtering, ordering, and eager loading.

**Parameters:**
- `filter` (Expression<Func<TEntity, bool>>, optional): LINQ filter expression to apply.
- `orderBy` (Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>, optional): Function to order results.
- `includeProperties` (string, optional): Comma-separated list of navigation properties to include (EF Core only).

**Returns:** Task<IEnumerable<TEntity>> - Collection of entities matching the criteria.

**Example:**
```csharp
// Get all active customers ordered by name
var customers = await repository.Get(
    filter: c => c.IsActive,
    orderBy: q => q.OrderBy(c => c.Name),
    includeProperties: "Orders,Address"
);
```

---

##### Update

```csharp
Task<TEntity> Update(TEntity entityToUpdate)
```

Updates an existing entity in the repository.

**Parameters:**
- `entityToUpdate` (TEntity): The entity with updated values. Cannot be null.

**Returns:** Task<TEntity> - The updated entity.

**Exceptions:**
- `ArgumentNullException`: Thrown when entityToUpdate is null.

**Notes:**
- For EntityFrameworkCore: Entity is attached and marked as modified.
- For MemoryRepository: Entity is replaced if ID exists.

**Example:**
```csharp
var customer = await repository.Get(1);
customer.Email = "newemail@example.com";
var updated = await repository.Update(customer);
```

---

##### Delete (by entity)

```csharp
Task<bool> Delete(TEntity entityToDelete)
```

Deletes an entity from the repository.

**Parameters:**
- `entityToDelete` (TEntity): The entity to delete. Cannot be null.

**Returns:** Task<bool> - True if deletion was successful, false otherwise.

**Exceptions:**
- `ArgumentNullException`: Thrown when entityToDelete is null.

**Example:**
```csharp
var customer = await repository.Get(1);
var deleted = await repository.Delete(customer);
```

---

##### Delete (by ID)

```csharp
Task<bool> Delete(object id)
```

Deletes an entity by its primary key.

**Parameters:**
- `id` (object): The primary key value. Cannot be null.

**Returns:** Task<bool> - True if deletion was successful, false otherwise.

**Exceptions:**
- `ArgumentNullException`: Thrown when id is null.

**Example:**
```csharp
var deleted = await repository.Delete(1);
```

---

## Classes

### EntityBase

Base class for entities with an integer primary key.

**Namespace**: `OakIdeas.GenericRepository.Models`

**Properties:**

```csharp
public int ID { get; set; }
```

The primary key identifier. Auto-generated on insert if value is 0.

**Example:**
```csharp
public class Customer : EntityBase
{
    public string Name { get; set; }
    // ID property inherited from EntityBase
}
```

---

### MemoryGenericRepository<TEntity>

In-memory implementation of the generic repository pattern using a concurrent dictionary.

**Namespace**: `OakIdeas.GenericRepository`

**Type Constraints**: `where TEntity : EntityBase`

**Thread Safety**: ✅ Thread-safe (uses ConcurrentDictionary)

**Implements**: `IGenericRepository<TEntity>`

#### Constructor

```csharp
public MemoryGenericRepository()
```

Initializes a new instance with an empty concurrent dictionary.

#### Behavior Notes

- **ID Generation**: If entity.ID is 0, assigns next available ID (max + 1)
- **Insert with existing ID**: Returns existing entity if ID already exists
- **Update non-existent entity**: Returns the entity without error
- **Delete non-existent entity**: Returns true without error
- **Storage**: Data stored in memory, lost on application restart
- **includeProperties parameter**: Ignored (no navigation properties in memory)

#### Example

```csharp
var repository = new MemoryGenericRepository<Customer>();
var customer = await repository.Insert(new Customer { Name = "Test" });
```

---

### EntityFrameworkCoreRepository<TEntity, TDataContext>

Entity Framework Core implementation of the generic repository pattern.

**Namespace**: `OakIdeas.GenericRepository.EntityFrameworkCore`

**Type Constraints**: 
- `where TEntity : class`
- `where TDataContext : DbContext`

**Thread Safety**: ⚠️ Not thread-safe (DbContext is not thread-safe)

**Implements**: `IGenericRepository<TEntity>`

#### Constructor

```csharp
public EntityFrameworkCoreRepository(TDataContext dataContext)
```

Initializes a new instance with the specified DbContext.

**Parameters:**
- `dataContext` (TDataContext): The Entity Framework Core DbContext instance.

#### Behavior Notes

- **ID Generation**: Handled by database (identity columns)
- **Change Tracking**: Automatic via EF Core
- **SaveChanges**: Called automatically in Insert, Update, and Delete
- **Eager Loading**: Supported via `includeProperties` parameter
- **Detached Entities**: Automatically attached when needed

#### Example

```csharp
public class MyDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }
}

var context = new MyDbContext();
var repository = new EntityFrameworkCoreRepository<Customer, MyDbContext>(context);
var customer = await repository.Insert(new Customer { Name = "Test" });
```

---

## Extension Methods

Currently, this library does not provide extension methods. Consider creating your own:

```csharp
public static class RepositoryExtensions
{
    public static async Task<bool> ExistsAsync<TEntity>(
        this IGenericRepository<TEntity> repository,
        Expression<Func<TEntity, bool>> filter) where TEntity : class
    {
        var results = await repository.Get(filter);
        return results.Any();
    }

    public static async Task<int> CountAsync<TEntity>(
        this IGenericRepository<TEntity> repository,
        Expression<Func<TEntity, bool>> filter = null) where TEntity : class
    {
        var results = await repository.Get(filter);
        return results.Count();
    }
}
```

---

## Type Compatibility

### Supported Entity Types

- Must be a class (reference type)
- For MemoryGenericRepository: Must inherit from EntityBase
- For EntityFrameworkCoreRepository: Any class with proper EF configuration

### ID Type

Current version supports `int` IDs only via EntityBase. For other ID types, use EntityFrameworkCoreRepository with entities that don't inherit from EntityBase.

---

## Performance Considerations

### MemoryGenericRepository

| Operation | Time Complexity | Notes |
|-----------|----------------|-------|
| Insert | O(1) | O(n) if ID=0 (needs to find max) |
| Get by ID | O(1) | Dictionary lookup |
| Get with filter | O(n) | Iterates all values |
| Update | O(1) | Dictionary update |
| Delete | O(1) | Dictionary removal |

### EntityFrameworkCoreRepository

Performance depends on:
- Database type and configuration
- Indexes on queried columns
- Network latency
- Query complexity
- Number of included navigation properties

**Recommendations:**
- Add indexes on frequently filtered columns
- Use projection (Select) for large result sets
- Avoid including unnecessary navigation properties
- Use pagination for large datasets

---

## Version History

### 0.0.5.1-alpha (OakIdeas.GenericRepository)
- Updated to .NET 8.0 test projects
- Added XML documentation
- Added null parameter validation
- Removed Task.Run wrappers
- Fixed typos in variable names
- Improved error handling

### 0.0.5.2-alpha (OakIdeas.GenericRepository.EntityFrameworkCore)
- Updated to EF Core 8.0
- Added XML documentation
- Added null parameter validation
- Improved error handling

---

## See Also

- [Architecture Documentation](./architecture.md)
- [Usage Examples](./usage-examples.md)
- [Best Practices](./best-practices.md)
