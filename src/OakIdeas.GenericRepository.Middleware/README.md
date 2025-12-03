# OakIdeas.GenericRepository.Middleware

Middleware infrastructure for OakIdeas.GenericRepository providing extensible cross-cutting concerns.

## Features

- **Pipeline-based architecture** for composable functionality
- **Fluent API** for clean, readable middleware configuration
- **Standard middleware** for common scenarios (logging, validation, auditing, performance monitoring)
- **Easy to extend** with custom middleware
- **Type-safe** and fully async
- **Minimal overhead** with efficient execution

## Installation

```bash
dotnet add package OakIdeas.GenericRepository.Middleware
```

## Quick Start

### Fluent API (Recommended)

The simplest way to use middleware is through extension methods:

```csharp
using OakIdeas.GenericRepository;
using OakIdeas.GenericRepository.Middleware;

// Create repository with middleware
var repository = new MemoryGenericRepository<Customer>()
    .WithLogging(log => Console.WriteLine(log))
    .WithValidation()
    .WithAuditing(entry => SaveAudit(entry), () => currentUser);

// Use normally - middleware automatically applied
var customer = new Customer { Name = "John Doe" };
await repository.Insert(customer);
```

### Options-Based Configuration

For more complex scenarios:

```csharp
var repository = new MemoryGenericRepository<Customer>()
    .WithMiddleware(options => options
        .UseMiddleware(new LoggingMiddleware<Customer, int>(logger))
        .UseMiddleware(new ValidationMiddleware<Customer, int>())
        .UseMiddleware(new AuditMiddleware<Customer, int>(auditLog)));
```

### ComposableRepository (Advanced)

For advanced scenarios where you need direct control:

```csharp
var innerRepository = new MemoryGenericRepository<Customer>();
var repository = new ComposableRepository<Customer, int>(
    innerRepository,
    new LoggingMiddleware<Customer, int>(logger),
    new ValidationMiddleware<Customer, int>(),
    new AuditMiddleware<Customer, int>(auditLog));
```

### Dependency Injection

```csharp
services.AddScoped<IGenericRepository<Customer, int>>(sp => 
{
    var logger = sp.GetRequiredService<ILogger<Customer>>();
    var auditService = sp.GetRequiredService<IAuditService>();
    
    return new MemoryGenericRepository<Customer>()
        .WithLogging(log => logger.LogInformation(log))
        .WithValidation()
        .WithAuditing(entry => auditService.Log(entry));
});
```

## Standard Middleware

All standard middleware can be added using convenient extension methods:

### LoggingMiddleware

```csharp
var repository = new MemoryGenericRepository<Customer>()
    .WithLogging(log => _logger.LogInformation(log), logPerformance: true);
```

### ValidationMiddleware

```csharp
public class Customer : EntityBase
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
}

var repository = new MemoryGenericRepository<Customer>()
    .WithValidation();
```

### PerformanceMiddleware

```csharp
var repository = new MemoryGenericRepository<Customer>()
    .WithPerformanceMonitoring(
        (operation, durationMs) => _metrics.Record(operation, durationMs),
        slowOperationThresholdMs: 1000);
```

### AuditMiddleware

```csharp
var repository = new MemoryGenericRepository<Customer>()
    .WithAuditing(
        entry => _auditLog.Add(entry),
        userProvider: () => _httpContext.User.Identity.Name);
```

## Chaining Middleware

Middleware can be chained for complex scenarios:

```csharp
var repository = new MemoryGenericRepository<Customer>()
    .WithValidation()                    // Validates first
    .WithAuditing(SaveAudit, GetUser)    // Then audits
    .WithLogging(Log)                    // Then logs
    .WithPerformanceMonitoring(Track);   // Finally tracks performance
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
