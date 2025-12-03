# OakIdeas.GenericRepository.Middleware

Middleware infrastructure for OakIdeas.GenericRepository providing extensible cross-cutting concerns.

## Features

- **Pipeline-based architecture** for composable functionality
- **Standard middleware** for common scenarios (logging, validation, auditing, performance monitoring)
- **Easy to extend** with custom middleware
- **Type-safe** and fully async
- **Minimal overhead** with efficient execution

## Installation

```bash
dotnet add package OakIdeas.GenericRepository.Middleware
```

## Quick Start

```csharp
using OakIdeas.GenericRepository;
using OakIdeas.GenericRepository.Middleware;
using OakIdeas.GenericRepository.Middleware.Standard;

// Create base repository
var innerRepository = new MemoryGenericRepository<Customer>();

// Add middleware
var repository = new MiddlewareRepository<Customer>(
    innerRepository,
    new LoggingMiddleware<Customer, int>(log => Console.WriteLine(log)),
    new ValidationMiddleware<Customer, int>(),
    new AuditMiddleware<Customer, int>(entry => SaveAudit(entry))
);

// Use normally - middleware automatically applied
var customer = new Customer { Name = "John Doe" };
await repository.Insert(customer);
```

## Standard Middleware

### LoggingMiddleware

Logs all repository operations with optional performance metrics.

```csharp
var loggingMiddleware = new LoggingMiddleware<Customer, int>(
    log => _logger.LogInformation(log),
    logPerformance: true
);
```

### ValidationMiddleware

Validates entities using DataAnnotations before insert/update.

```csharp
public class Customer : EntityBase
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
}

var validationMiddleware = new ValidationMiddleware<Customer, int>();
```

### PerformanceMiddleware

Monitors and reports performance metrics.

```csharp
var performanceMiddleware = new PerformanceMiddleware<Customer, int>(
    (operation, durationMs) => _metrics.Record(operation, durationMs),
    slowOperationThresholdMs: 1000
);
```

### AuditMiddleware

Creates audit trails for data modifications.

```csharp
var auditMiddleware = new AuditMiddleware<Customer, int>(
    entry => _auditLog.Add(entry),
    userProvider: () => _httpContext.User.Identity.Name
);
```

## Creating Custom Middleware

```csharp
public class MyMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class
{
    public override async Task<TEntity> Insert(
        Func<Task<TEntity>> next,
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        // Before logic
        Console.WriteLine("Before insert");
        
        // Execute operation
        var result = await next();
        
        // After logic
        Console.WriteLine("After insert");
        
        return result;
    }
}
```

## Documentation

For comprehensive documentation, see:

- [Middleware Pattern Overview](https://github.com/oakcool/OakIdeas.GenericRepository/blob/main/docs/middleware/README.md)
- [Creating Custom Middleware](https://github.com/oakcool/OakIdeas.GenericRepository/blob/main/docs/middleware/custom-middleware.md)

## Benefits Over Traditional Patterns

### vs. Inheritance
- ✅ Loose coupling
- ✅ Runtime composition
- ✅ Easy to combine features

### vs. Decorator Pattern
- ✅ Focused intercept points
- ✅ Pipeline-based
- ✅ Less boilerplate

## License

This project is licensed under the same license as OakIdeas.GenericRepository.

## Contributing

Contributions are welcome! Please see the [Contributing Guide](https://github.com/oakcool/OakIdeas.GenericRepository/blob/main/CONTRIBUTING.md).
