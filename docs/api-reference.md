# API Reference

Complete reference documentation for all public APIs in OakIdeas.GenericRepository.

## Interfaces

### IGenericRepository<TEntity, TKey>

Generic repository interface for CRUD operations with support for different primary key types.

**Namespace**: `OakIdeas.GenericRepository`

**Type Constraints**: 
- `where TEntity : class`

**Type Parameters:**
- `TEntity`: The entity type
- `TKey`: The type of the primary key (int, Guid, long, string, etc.)

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
// With Guid keys
var repository = new MemoryGenericRepository<Customer, Guid>();
var customer = new Customer { ID = Guid.NewGuid(), Name = "John Doe" };
var inserted = await repository.Insert(customer);

// With int keys (backward compatible)
var intRepository = new MemoryGenericRepository<Customer>();
var customer2 = new Customer { Name = "Jane Doe" };
var inserted2 = await intRepository.Insert(customer2);
Console.WriteLine($"Created with ID: {inserted2.ID}");
```

---

##### Get (by ID)

```csharp
Task<TEntity?> Get(TKey id)
```

Gets an entity by its primary key.

**Parameters:**
- `id` (TKey): The primary key value. Cannot be null.

**Returns:** Task<TEntity?> - The entity if found, null otherwise.

**Exceptions:**
- `ArgumentNullException`: Thrown when id is null.

**Example:**
```csharp
// With Guid keys
var customer = await repository.Get(Guid.Parse("..."));

// With string keys
var repository = new MemoryGenericRepository<Product, string>();
var product = await repository.Get("PROD-001");

// With int keys (backward compatible)
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
    Expression<Func<TEntity, bool>>? filter = null,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
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
Task<bool> Delete(TKey id)
```

Deletes an entity by its primary key.

**Parameters:**
- `id` (TKey): The primary key value. Cannot be null.

**Returns:** Task<bool> - True if deletion was successful, false otherwise.

**Exceptions:**
- `ArgumentNullException`: Thrown when id is null.

**Example:**
```csharp
// With int keys
var deleted = await repository.Delete(1);

// With Guid keys
var deleted = await repository.Delete(Guid.Parse("..."));

// With string keys
var deleted = await repository.Delete("CUST-001");
```

---

### IGenericRepository<TEntity>

Backward-compatible generic repository interface for CRUD operations with integer primary keys.

**Namespace**: `OakIdeas.GenericRepository`

**Type Constraints**: `where TEntity : class`

**Inheritance**: Inherits from `IGenericRepository<TEntity, int>`

This interface is provided for backward compatibility with existing code. All methods are inherited from `IGenericRepository<TEntity, int>` with `int` as the key type.

**Example:**
```csharp
// This continues to work as before
IGenericRepository<Customer> repository = new MemoryGenericRepository<Customer>();
var customer = await repository.Get(1);
```

---

## Classes

### EntityBase<TKey>

Base class for entities with a generic primary key type.

**Namespace**: `OakIdeas.GenericRepository.Models`

**Type Parameters:**
- `TKey`: The type of the primary key

**Properties:**

```csharp
public TKey ID { get; set; }
```

The primary key identifier.

**Example:**
```csharp
// Entity with Guid primary key
public class Customer : EntityBase<Guid>
{
    public string Name { get; set; }
    public string Email { get; set; }
}

// Entity with string primary key
public class Product : EntityBase<string>
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// Entity with long primary key
public class Transaction : EntityBase<long>
{
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}
```

---

### EntityBase

Base class for entities with an integer primary key.

**Namespace**: `OakIdeas.GenericRepository.Models`

**Inheritance**: Inherits from `EntityBase<int>`

**Properties:**

```csharp
public int ID { get; set; }
```

The primary key identifier. Auto-generated on insert if value is 0.

**Example:**
```csharp
// Backward-compatible usage
public class Customer : EntityBase
{
    public string Name { get; set; }
    // ID property inherited from EntityBase (int type)
}
```

---

### MemoryGenericRepository<TEntity, TKey>

In-memory implementation of the generic repository pattern using a concurrent dictionary with support for generic key types.

**Namespace**: `OakIdeas.GenericRepository`

**Type Constraints**: 
- `where TEntity : EntityBase<TKey>`
- `where TKey : notnull`

**Thread Safety**: ✅ Thread-safe (uses ConcurrentDictionary)

**Implements**: `IGenericRepository<TEntity, TKey>`

#### Constructor

```csharp
public MemoryGenericRepository()
```

Initializes a new instance with an empty concurrent dictionary.

#### Behavior Notes

- **ID Generation**: For integer keys, if entity.ID is 0, assigns next available ID (incrementing counter)
- **ID Generation**: For non-integer keys, ID must be provided by the caller
- **Insert with existing ID**: Returns existing entity if ID already exists
- **Update non-existent entity**: Returns the entity without error
- **Delete non-existent entity**: Returns true without error
- **Storage**: Data stored in memory, lost on application restart
- **includeProperties parameter**: Ignored (no navigation properties in memory)

#### Example

```csharp
// With Guid keys
var guidRepo = new MemoryGenericRepository<CustomerGuid, Guid>();
var customer = await guidRepo.Insert(new CustomerGuid { ID = Guid.NewGuid(), Name = "Test" });

// With string keys
var stringRepo = new MemoryGenericRepository<ProductString, string>();
var product = await stringRepo.Insert(new ProductString { ID = "PROD-001", Name = "Widget" });

// With long keys
var longRepo = new MemoryGenericRepository<TransactionLong, long>();
var transaction = await longRepo.Insert(new TransactionLong { ID = 1000000000000L, Amount = 99.99m });
```

---

### MemoryGenericRepository<TEntity>

Backward-compatible in-memory implementation with integer primary keys.

**Namespace**: `OakIdeas.GenericRepository`

**Type Constraints**: `where TEntity : EntityBase`

**Inheritance**: Inherits from `MemoryGenericRepository<TEntity, int>`

**Example:**
```csharp
// This continues to work as before
var repository = new MemoryGenericRepository<Customer>();
var customer = await repository.Insert(new Customer { Name = "Test" });
```

---

### EntityFrameworkCoreRepository<TEntity, TDataContext, TKey>

Entity Framework Core implementation of the generic repository pattern with support for generic key types.

**Namespace**: `OakIdeas.GenericRepository.EntityFrameworkCore`

**Type Constraints**: 
- `where TEntity : class`
- `where TDataContext : DbContext`
- `where TKey : notnull`

**Thread Safety**: ⚠️ Not thread-safe (DbContext is not thread-safe)

**Implements**: `IGenericRepository<TEntity, TKey>`

#### Constructor

```csharp
public EntityFrameworkCoreRepository(TDataContext dataContext)
```

Initializes a new instance with the specified DbContext.

**Parameters:**
- `dataContext` (TDataContext): The Entity Framework Core DbContext instance.

#### Behavior Notes

- **ID Generation**: Handled by database (identity columns, sequences, or client-generated)
- **Change Tracking**: Automatic via EF Core
- **SaveChanges**: Called automatically in Insert, Update, and Delete
- **Eager Loading**: Supported via `includeProperties` parameter
- **Detached Entities**: Automatically attached when needed

#### Example

```csharp
public class MyDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Product> Products { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Guid primary key
        modelBuilder.Entity<Customer>()
            .HasKey(c => c.ID);
        
        // Configure string primary key
        modelBuilder.Entity<Product>()
            .HasKey(p => p.ID);
    }
}

// With Guid keys
var context = new MyDbContext();
var customerRepo = new EntityFrameworkCoreRepository<Customer, MyDbContext, Guid>(context);
var customer = await customerRepo.Insert(new Customer { ID = Guid.NewGuid(), Name = "Test" });

// With string keys
var productRepo = new EntityFrameworkCoreRepository<Product, MyDbContext, string>(context);
var product = await productRepo.Insert(new Product { ID = "PROD-001", Name = "Widget" });
```

---

### EntityFrameworkCoreRepository<TEntity, TDataContext>

Backward-compatible Entity Framework Core implementation with integer primary keys.

**Namespace**: `OakIdeas.GenericRepository.EntityFrameworkCore`

**Type Constraints**: 
- `where TEntity : class`
- `where TDataContext : DbContext`

**Inheritance**: Inherits from `EntityFrameworkCoreRepository<TEntity, TDataContext, int>`

**Example:**
```csharp
public class MyDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }
}

// This continues to work as before
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
    public static async Task<bool> ExistsAsync<TEntity, TKey>(
        this IGenericRepository<TEntity, TKey> repository,
        Expression<Func<TEntity, bool>> filter) where TEntity : class
    {
        var results = await repository.Get(filter);
        return results.Any();
    }

    public static async Task<int> CountAsync<TEntity, TKey>(
        this IGenericRepository<TEntity, TKey> repository,
        Expression<Func<TEntity, bool>>? filter = null) where TEntity : class
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
- For MemoryGenericRepository: Must inherit from EntityBase<TKey>
- For EntityFrameworkCoreRepository: Any class with proper EF configuration

### Supported Key Types

The following key types are commonly used and fully supported:

| Key Type | Use Case | Example |
|----------|----------|---------|
| `int` | Traditional auto-incrementing IDs | `1, 2, 3, ...` |
| `long` | Large datasets, distributed systems | `1000000000000L` |
| `Guid` | Distributed systems, no collisions | `Guid.NewGuid()` |
| `string` | Business keys, external IDs | `"CUST-001"`, `"ORD-2024-001"` |

**Notes:**
- Key types must be non-nullable (`notnull` constraint)
- For MemoryGenericRepository with int keys, IDs are auto-generated starting from 1
- For other key types, IDs must be provided by the caller
- For EntityFrameworkCore, ID generation depends on database configuration

---

## Migration Guide

### Migrating from Single Key Type to Generic Keys

If you have existing code using `EntityBase` (with int keys), you can migrate gradually:

**Step 1: No changes needed for existing int-based code**
```csharp
// This continues to work exactly as before
public class Customer : EntityBase
{
    public string Name { get; set; }
}

var repository = new MemoryGenericRepository<Customer>();
```

**Step 2: For new entities with different key types**
```csharp
// Define entity with desired key type
public class Order : EntityBase<Guid>
{
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
}

// Use generic repository with explicit key type
var repository = new MemoryGenericRepository<Order, Guid>();
```

**Step 3: Update existing entities when ready**
```csharp
// Before
public class Customer : EntityBase
{
    public string Name { get; set; }
}

// After
public class Customer : EntityBase<Guid>
{
    public string Name { get; set; }
}

// Update repository instantiation
var repository = new MemoryGenericRepository<Customer, Guid>();
```

---

## Performance Considerations

### MemoryGenericRepository

| Operation | Time Complexity | Notes |
|-----------|----------------|-------|
| Insert | O(1) | O(1) for all key types with pre-assigned IDs |
| Get by ID | O(1) | Dictionary lookup |
| Get with filter | O(n) | Iterates all values |
| Update | O(1) | Dictionary update |
| Delete | O(1) | Dictionary removal |

**Key Type Performance:**
- `int`, `long`: Fastest (value type, direct comparison)
- `Guid`: Fast (value type, but larger memory footprint)
- `string`: Slightly slower (reference type, string comparison)

### EntityFrameworkCoreRepository

Performance depends on:
- Database type and configuration
- Indexes on queried columns
- Network latency
- Query complexity
- Number of included navigation properties
- Key type and index structure

**Key Type Recommendations:**
- `int`: Best for sequential access, smallest storage
- `Guid`: Best for distributed systems, no sequential benefit
- `long`: Balance of range and performance
- `string`: Best for business keys, ensure proper indexing

**Recommendations:**
- Add indexes on primary key columns (automatic for most databases)
- Use clustered indexes for sequential keys (int, long)
- Use non-clustered indexes for non-sequential keys (Guid, string)
- Use projection (Select) for large result sets
- Avoid including unnecessary navigation properties
- Use pagination for large datasets

---

## Version History

### 0.0.5.1-alpha (OakIdeas.GenericRepository)
- **NEW**: Generic key type support (Guid, long, string, etc.)
- **NEW**: EntityBase<TKey> generic base class
- **NEW**: IGenericRepository<TEntity, TKey> interface
- Backward-compatible EntityBase and IGenericRepository<TEntity> maintained
- Updated to .NET 8.0 test projects
- Added XML documentation
- Added null parameter validation
- Removed Task.Run wrappers
- Fixed typos in variable names
- Improved error handling

### 0.0.5.2-alpha (OakIdeas.GenericRepository.EntityFrameworkCore)
- **NEW**: Generic key type support (Guid, long, string, etc.)
- **NEW**: EntityFrameworkCoreRepository<TEntity, TDataContext, TKey> with three type parameters
- Backward-compatible EntityFrameworkCoreRepository<TEntity, TDataContext> maintained
- Changed from PackageReference to ProjectReference for main library
- Updated to EF Core 8.0
- Added XML documentation
- Added null parameter validation
- Improved error handling

---

## See Also

- [Architecture Documentation](./architecture.md)
- [Usage Examples](./usage-examples.md)
- [Best Practices](./best-practices.md)
