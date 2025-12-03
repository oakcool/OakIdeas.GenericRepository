# GenericRepository
![OakIdeas.GenericRepository - Deploy](https://github.com/oakcool/OakIdeas.GenericRepository/workflows/OakIdeas.GenericRepository%20-%20Deploy/badge.svg)

A versatile and extensible implementation of the repository pattern for CRUD operations with support for middleware pipelines, multiple storage backends, and advanced querying.

## Features

- **Generic Repository Interface** - Type-safe repository operations with support for custom key types
- **Multiple Implementations**:
  - In-Memory Repository (Dictionary-based, perfect for testing)
  - Entity Framework Core Repository
- **Middleware Pipeline** - Extensible middleware architecture for cross-cutting concerns
- **Soft Delete Support** - Built-in soft delete functionality
- **Query Object Pattern** - Fluent API for building complex queries
- **Specification Pattern** - Reusable business rules and query logic
- **Async Enumerable Support** - Stream large datasets without loading everything into memory
- **Type-Safe Navigation** - Type-safe eager loading of related entities

## Packages

- **OakIdeas.GenericRepository** - Core interfaces and base functionality
- **OakIdeas.GenericRepository.Memory** - In-memory repository implementation
- **OakIdeas.GenericRepository.EntityFrameworkCore** - EF Core repository implementation
- **OakIdeas.GenericRepository.Middleware** - Middleware infrastructure and standard middleware

## Basic Usage

```csharp
using OakIdeas.GenericRepository;
using OakIdeas.GenericRepository.Models;

public class Customer : EntityBase
{
    public string Name { get; set; }
    public string Email { get; set; }
}

// Create a repository instance
var repository = new MemoryGenericRepository<Customer>();

// Create (C)
var customer = await repository.Insert(new Customer 
{ 
    Name = "John Doe",
    Email = "john@example.com"
});

// Retrieve (R)
var foundCustomer = await repository.Get(customer.ID);

// Or retrieve with filtering
var customers = await repository.Get(
    filter: c => c.Name.Contains("John"),
    orderBy: q => q.OrderBy(c => c.Name)
);

// Update (U)
customer.Name = "Jane Doe";
await repository.Update(customer);

// Delete (D)
await repository.Delete(customer.ID);
```

## Middleware Architecture

The middleware system provides a powerful way to add cross-cutting concerns to your repository operations.

### Configuring Options

```csharp
using OakIdeas.GenericRepository.Core;
using OakIdeas.GenericRepository.Middleware.Extensions;

// Create options with middleware support
var options = new RepositoryOptions()
    .WithLogging(enabled: true)
    .WithPerformanceTracking(enabled: true)
    .WithValidation(enabled: true);

var repository = new MemoryGenericRepository<Customer>(options);
```

### Standard Middleware

#### Logging Middleware

Automatically logs repository operations:

```csharp
using OakIdeas.GenericRepository.Middleware;
using OakIdeas.GenericRepository.Middleware.Standard;

var pipeline = new MiddlewarePipeline<Customer, int>();

pipeline.Use(new LoggingMiddleware<Customer, int>(
    logger: message => Console.WriteLine(message),
    includeTimings: true
));
```

#### Validation Middleware

Validates entities before operations:

```csharp
var validationMiddleware = new ValidationMiddleware<Customer, int>(
    validator: customer => 
    {
        if (string.IsNullOrEmpty(customer.Email))
            return (false, "Email is required");
        
        if (!customer.Email.Contains("@"))
            return (false, "Invalid email format");
        
        return (true, null);
    }
);

pipeline.Use(validationMiddleware);
```

#### Performance Middleware

Tracks operation performance:

```csharp
var performanceMiddleware = new PerformanceMiddleware<Customer, int>(
    metricCollector: (operationName, elapsedMs) => 
    {
        Console.WriteLine($"{operationName} took {elapsedMs}ms");
    },
    warningThresholdMs: 1000 // Warn if operation takes more than 1 second
);

pipeline.Use(performanceMiddleware);
```

### Custom Middleware

Create your own middleware by implementing `IRepositoryMiddleware<TEntity, TKey>`:

```csharp
public class AuditMiddleware<TEntity, TKey> : IRepositoryMiddleware<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    private readonly IAuditLogger _auditLogger;

    public AuditMiddleware(IAuditLogger auditLogger)
    {
        _auditLogger = auditLogger;
    }

    public async Task InvokeAsync(
        RepositoryContext<TEntity, TKey> context,
        RepositoryMiddlewareDelegate<TEntity, TKey> next)
    {
        // Before operation
        _auditLogger.LogBefore(context.Operation, context.Entity);

        await next(context);

        // After operation
        _auditLogger.LogAfter(context.Operation, context.Result, context.Success);
    }
}
```

## Advanced Querying

### Query Object Pattern

```csharp
using OakIdeas.GenericRepository;

var query = new Query<Customer>()
    .Where(c => c.Name.StartsWith("J"))
    .Sort(q => q.OrderByDescending(c => c.ID))
    .Include(c => c.Orders)
    .Paged(page: 1, pageSize: 10)
    .WithNoTracking();

var results = await repository.Get(query);
```

### Specification Pattern

```csharp
using OakIdeas.GenericRepository.Specifications;

public class ActiveCustomerSpecification : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return customer => customer.IsActive && !customer.IsDeleted;
    }
}

public class CustomerByNameSpecification : Specification<Customer>
{
    private readonly string _name;

    public CustomerByNameSpecification(string name)
    {
        _name = name;
    }

    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return customer => customer.Name.Contains(_name);
    }
}

// Use specifications
var activeCustomers = await repository.Get(
    filter: new ActiveCustomerSpecification()
);

// Combine specifications
var activeJohns = new ActiveCustomerSpecification()
    .And(new CustomerByNameSpecification("John"));

var results = await repository.Get(filter: activeJohns);
```

## Soft Delete Support

```csharp
using OakIdeas.GenericRepository;
using OakIdeas.GenericRepository.Models;

public class Customer : SoftDeletableEntity
{
    public string Name { get; set; }
}

var repository = new SoftDeleteMemoryRepository<Customer>();

// Soft delete - marks as deleted but doesn't remove from storage
await repository.Delete(customer);

// Normal queries automatically exclude soft-deleted entities
var activeCustomers = await repository.Get();

// Include soft-deleted entities
var allCustomers = await repository.GetIncludingDeleted();

// Restore a soft-deleted entity
await repository.Restore(customer);

// Permanently delete
await repository.PermanentlyDelete(customer);
```

## Entity Framework Core Usage

```csharp
using Microsoft.EntityFrameworkCore;
using OakIdeas.GenericRepository.EntityFrameworkCore;

public class MyDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }
}

var context = new MyDbContext(options);
var repository = new EntityFrameworkCoreRepository<Customer, MyDbContext>(context);

// Use the same repository interface
var customers = await repository.Get(
    filter: c => c.IsActive,
    orderBy: q => q.OrderBy(c => c.Name),
    includeProperties: "Orders,Address"
);
```

## Batch Operations

```csharp
// Insert multiple entities
var newCustomers = new List<Customer>
{
    new Customer { Name = "Alice" },
    new Customer { Name = "Bob" },
    new Customer { Name = "Charlie" }
};
var inserted = await repository.InsertRange(newCustomers);

// Update multiple entities
var updated = await repository.UpdateRange(newCustomers);

// Delete multiple entities
var deletedCount = await repository.DeleteRange(newCustomers);

// Delete by filter
var deletedByFilter = await repository.DeleteRange(c => c.IsActive == false);
```

## Async Enumerable for Large Datasets

```csharp
// Stream results without loading everything into memory
await foreach (var customer in repository.GetAsyncEnumerable(
    filter: c => c.CreatedDate > DateTime.UtcNow.AddYears(-1),
    orderBy: q => q.OrderBy(c => c.Name)))
{
    // Process each customer one at a time
    await ProcessCustomerAsync(customer);
}
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

For detailed contribution guidelines, see [docs/contributing.md](docs/contributing.md).

## Documentation

- **[Getting Started](docs/getting-started.md)** - Quick start guide
- **[Architecture](docs/architecture.md)** - System architecture overview
- **[Best Practices](docs/best-practices.md)** - Recommended patterns and practices
- **[API Reference](docs/api-reference.md)** - Detailed API documentation

### For AI Assistants & Developers

- **[AI Context](ai-context.md)** - Overview for AI assistants working with this codebase
- **[Copilot Instructions](copilot-instructions.md)** - GitHub Copilot coding guidelines
- **[AGENTS.MD](AGENTS.MD)** - Comprehensive agent capabilities and workflows
- **[GitHub Copilot Context](.github/copilot-context.md)** - Quick reference for Copilot

## License

This project is licensed under the terms specified in the LICENSE file.
