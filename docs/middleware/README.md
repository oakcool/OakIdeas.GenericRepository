# Middleware Pattern

## Overview

The middleware pattern provides a powerful and flexible way to add cross-cutting concerns to your repository operations. Instead of using inheritance or tightly coupled decorators, middleware allows you to compose functionality in a clean, reusable pipeline.

## What is Middleware?

Middleware in the context of OakIdeas.GenericRepository is a component that intercepts repository operations, performs some logic before and/or after the operation, and optionally modifies the behavior. Middleware components can be chained together to create a processing pipeline.

### Benefits

- **Separation of Concerns**: Each middleware focuses on a single responsibility
- **Reusability**: Middleware can be reused across different repositories
- **Composability**: Mix and match middleware components as needed
- **Testability**: Each middleware can be tested independently
- **Flexibility**: Add or remove middleware at runtime
- **Order Control**: Define the exact order of execution

## Architecture

```
Client Code
    ↓
MiddlewareRepository
    ↓
Middleware 1 (Before)
    ↓
Middleware 2 (Before)
    ↓
Middleware N (Before)
    ↓
Actual Repository Operation
    ↓
Middleware N (After)
    ↓
Middleware 2 (After)
    ↓
Middleware 1 (After)
    ↓
Return to Client
```

## Core Components

### IRepositoryMiddleware<TEntity, TKey>

The core interface that all middleware must implement. It defines methods for intercepting each repository operation.

```csharp
public interface IRepositoryMiddleware<TEntity, TKey> where TEntity : class
{
    Task<TEntity> Insert(
        Func<Task<TEntity>> next,
        TEntity entity,
        CancellationToken cancellationToken = default);

    Task<TEntity> Update(
        Func<Task<TEntity>> next,
        TEntity entityToUpdate,
        CancellationToken cancellationToken = default);

    // ... other methods
}
```

### RepositoryMiddlewareBase<TEntity, TKey>

A base implementation that provides pass-through behavior for all operations. Inherit from this to create custom middleware:

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
        
        // Call next middleware or repository
        var result = await next();
        
        // After logic
        Console.WriteLine("After insert");
        
        return result;
    }
}
```

### ComposableRepository<TEntity, TKey>

A repository decorator that applies middleware to repository operations.

**Using Extension Methods (Recommended):**

```csharp
var repository = new MemoryGenericRepository<Customer>()
    .WithLogging(logger)
    .WithValidation()
    .WithAuditing(auditLog);
```

**Direct Instantiation (Advanced):**

```csharp
var repository = new ComposableRepository<Customer, int>(
    new MemoryGenericRepository<Customer>(),
    middleware1,
    middleware2,
    middleware3
);
```

**Note:** `MiddlewareRepository` is deprecated in favor of `ComposableRepository`. Existing code using `MiddlewareRepository` will continue to work.


## Basic Usage

### Creating a Simple Middleware

```csharp
using OakIdeas.GenericRepository.Middleware;

public class SimpleLoggingMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class
{
    public override async Task<TEntity> Insert(
        Func<Task<TEntity>> next,
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Inserting {typeof(TEntity).Name}");
        var result = await next();
        Console.WriteLine($"Inserted {typeof(TEntity).Name}");
        return result;
    }
}
```

### Using Middleware

```csharp
// Create the base repository
var innerRepository = new MemoryGenericRepository<Customer>();

// Create middleware instances
var loggingMiddleware = new LoggingMiddleware<Customer, int>(
    log => Console.WriteLine(log));

var validationMiddleware = new ValidationMiddleware<Customer, int>();

// Wrap with MiddlewareRepository
var repository = new MiddlewareRepository<Customer>(
    innerRepository,
    loggingMiddleware,
    validationMiddleware
);

// Use normally
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

**Features:**
- Logs operation start and completion
- Optional performance timing
- Exception logging
- Entity type in log messages

### ValidationMiddleware

Validates entities using DataAnnotations before insert/update operations.

```csharp
public class Customer : EntityBase
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Range(0, 150)]
    public int Age { get; set; }
}

var validationMiddleware = new ValidationMiddleware<Customer, int>(
    throwOnValidationError: true
);
```

**Features:**
- Automatic DataAnnotations validation
- Validates on Insert, Update, and Range operations
- Configurable error handling
- Detailed error messages

### PerformanceMiddleware

Monitors and reports performance metrics for all operations.

```csharp
var performanceMiddleware = new PerformanceMiddleware<Customer, int>(
    (operation, durationMs) => _metrics.Record(operation, durationMs),
    slowOperationThresholdMs: 1000
);
```

**Features:**
- Records operation duration
- Reports slow operations
- Entity-specific metrics
- Customizable thresholds

### AuditMiddleware

Creates audit trails for data modification operations.

```csharp
var auditMiddleware = new AuditMiddleware<Customer, int>(
    entry => _auditLog.Add(entry),
    userProvider: () => _httpContext.User.Identity.Name
);
```

**Features:**
- Tracks Insert, Update, Delete operations
- Records timestamp and user
- Batch operation support
- Customizable audit storage

**AuditEntry Structure:**
```csharp
public class AuditEntry
{
    public DateTime Timestamp { get; set; }
    public string EntityType { get; set; }
    public string Operation { get; set; }
    public string? User { get; set; }
    public string? Details { get; set; }
}
```

## Advanced Scenarios

### Multiple Middleware in Order

Middleware executes in the order specified, with each wrapping the next:

```csharp
var repository = new MiddlewareRepository<Customer>(
    innerRepository,
    validationMiddleware,  // Executes first
    auditMiddleware,       // Executes second
    loggingMiddleware      // Executes last
);
```

Execution flow for Insert:
1. ValidationMiddleware validates entity
2. AuditMiddleware records operation start
3. LoggingMiddleware logs operation
4. Actual insert happens
5. LoggingMiddleware logs completion
6. AuditMiddleware records operation completion
7. ValidationMiddleware passes result through

### Conditional Middleware

Create middleware that executes conditionally:

```csharp
public class ConditionalMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class
{
    private readonly Func<bool> _condition;

    public ConditionalMiddleware(Func<bool> condition)
    {
        _condition = condition;
    }

    public override async Task<TEntity> Insert(
        Func<Task<TEntity>> next,
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        if (_condition())
        {
            // Execute special logic
            Console.WriteLine("Condition met, executing special logic");
        }
        
        return await next();
    }
}
```

### Error Handling Middleware

```csharp
public class ErrorHandlingMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class
{
    private readonly Action<Exception> _errorHandler;

    public ErrorHandlingMiddleware(Action<Exception> errorHandler)
    {
        _errorHandler = errorHandler;
    }

    public override async Task<TEntity> Insert(
        Func<Task<TEntity>> next,
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            _errorHandler(ex);
            throw; // Re-throw or handle as needed
        }
    }
}
```

### Caching Middleware

```csharp
public class CachingMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class, IHasId<TKey>
    where TKey : notnull
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration;

    public CachingMiddleware(IMemoryCache cache, TimeSpan cacheDuration)
    {
        _cache = cache;
        _cacheDuration = cacheDuration;
    }

    public override async Task<TEntity?> GetById(
        Func<Task<TEntity?>> next,
        TKey id,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{typeof(TEntity).Name}:{id}";
        
        if (_cache.TryGetValue(cacheKey, out TEntity? cached))
        {
            return cached;
        }

        var entity = await next();
        
        if (entity != null)
        {
            _cache.Set(cacheKey, entity, _cacheDuration);
        }

        return entity;
    }

    public override async Task<TEntity> Update(
        Func<Task<TEntity>> next,
        TEntity entityToUpdate,
        CancellationToken cancellationToken = default)
    {
        var result = await next();
        
        // Invalidate cache on update
        var cacheKey = $"{typeof(TEntity).Name}:{entityToUpdate.ID}";
        _cache.Remove(cacheKey);
        
        return result;
    }
}
```

## Best Practices

### 1. Keep Middleware Focused

Each middleware should have a single, well-defined responsibility.

✅ **Good:**
```csharp
public class LoggingMiddleware // Only logs
public class ValidationMiddleware // Only validates
```

❌ **Bad:**
```csharp
public class LoggingAndValidationMiddleware // Does too much
```

### 2. Order Matters

Think carefully about the order of your middleware:

```csharp
// Validate BEFORE logging (don't log invalid operations)
var repository = new MiddlewareRepository<Customer>(
    innerRepository,
    validationMiddleware,
    loggingMiddleware
);
```

### 3. Handle Exceptions Appropriately

Decide whether middleware should catch and handle exceptions or let them propagate:

```csharp
public override async Task<TEntity> Insert(...)
{
    try
    {
        return await next();
    }
    catch (ValidationException)
    {
        // Handle specifically
    }
    catch (Exception ex)
    {
        // Log and re-throw
        _logger.LogError(ex, "Operation failed");
        throw;
    }
}
```

### 4. Use Async/Await Properly

Always await the next delegate and return the result:

```csharp
// ✅ Correct
var result = await next();
return result;

// ❌ Wrong - blocks thread
return next().Result;
```

### 5. Don't Modify Entities in Read Middleware

Middleware on read operations (Get) should not modify entities unless that's explicitly their purpose.

### 6. Consider Performance

Middleware adds overhead. Keep operations lightweight, especially for high-frequency operations.

## Comparison with Other Patterns

### Middleware vs. Inheritance

**Inheritance (SoftDeleteRepository):**
- Tight coupling
- Difficult to combine features
- Fixed at compile time

**Middleware:**
- Loose coupling
- Easy to combine features
- Configurable at runtime

### Middleware vs. Decorator Pattern

**Traditional Decorator:**
- Each decorator wraps the entire interface
- Verbose implementation
- Limited composition

**Middleware:**
- Focused intercept points
- Clean composition
- Pipeline-based

## Testing Middleware

### Unit Testing Individual Middleware

```csharp
[Fact]
public async Task LoggingMiddleware_LogsInsertOperation()
{
    // Arrange
    var logs = new List<string>();
    var middleware = new LoggingMiddleware<Customer, int>(
        log => logs.Add(log));
    
    var called = false;
    Func<Task<Customer>> next = () =>
    {
        called = true;
        return Task.FromResult(new Customer { ID = 1 });
    };

    // Act
    await middleware.Insert(next, new Customer());

    // Assert
    Assert.True(called);
    Assert.Contains(logs, l => l.Contains("Insert"));
}
```

### Integration Testing with Repository

```csharp
[Fact]
public async Task MiddlewareRepository_WithValidation_RejectsInvalidEntity()
{
    // Arrange
    var repository = new MiddlewareRepository<Customer>(
        new MemoryGenericRepository<Customer>(),
        new ValidationMiddleware<Customer, int>());

    // Act & Assert
    await Assert.ThrowsAsync<ValidationException>(
        () => repository.Insert(new Customer { Name = "" }));
}
```

## See Also

- [Creating Custom Middleware](./custom-middleware.md)
- [Middleware Examples](./middleware-examples.md)
- [API Reference](./middleware-api-reference.md)
