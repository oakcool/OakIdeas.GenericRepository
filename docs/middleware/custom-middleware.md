# Creating Custom Middleware

This guide walks you through creating custom middleware for the OakIdeas.GenericRepository.

## Prerequisites

- Understanding of async/await in C#
- Familiarity with the repository pattern
- Basic knowledge of the OakIdeas.GenericRepository library

## Step-by-Step Guide

### Step 1: Create the Middleware Class

Start by inheriting from `RepositoryMiddlewareBase<TEntity, TKey>`:

```csharp
using OakIdeas.GenericRepository.Middleware;

public class MyCustomMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class
{
    // Your implementation here
}
```

### Step 2: Add Dependencies

If your middleware needs external dependencies, add them via constructor:

```csharp
public class MyCustomMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class
{
    private readonly ILogger _logger;
    private readonly IConfiguration _config;

    public MyCustomMiddleware(ILogger logger, IConfiguration config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }
}
```

### Step 3: Override Methods

Override only the methods you need to intercept:

```csharp
public class MyCustomMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class
{
    private readonly ILogger _logger;

    public MyCustomMiddleware(ILogger logger)
    {
        _logger = logger;
    }

    public override async Task<TEntity> Insert(
        Func<Task<TEntity>> next,
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        // Before logic
        _logger.LogInformation($"Inserting {typeof(TEntity).Name}");

        // Call the next middleware or repository
        var result = await next();

        // After logic
        _logger.LogInformation($"Inserted {typeof(TEntity).Name} with ID");

        return result;
    }
}
```

## Real-World Examples

### Example 1: Retry Middleware

Automatically retries failed operations:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using OakIdeas.GenericRepository.Middleware;

public class RetryMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class
{
    private readonly int _maxRetries;
    private readonly TimeSpan _retryDelay;

    public RetryMiddleware(int maxRetries = 3, TimeSpan? retryDelay = null)
    {
        _maxRetries = maxRetries;
        _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(500);
    }

    public override async Task<TEntity> Insert(
        Func<Task<TEntity>> next,
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetry(next);
    }

    public override async Task<TEntity> Update(
        Func<Task<TEntity>> next,
        TEntity entityToUpdate,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetry(next);
    }

    private async Task<T> ExecuteWithRetry<T>(Func<Task<T>> operation)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt < _maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                lastException = ex;
                if (attempt < _maxRetries - 1)
                {
                    await Task.Delay(_retryDelay);
                }
            }
        }

        throw new InvalidOperationException(
            $"Operation failed after {_maxRetries} attempts", lastException);
    }

    private bool IsTransient(Exception ex)
    {
        // Define which exceptions are transient and should be retried
        return ex is TimeoutException || 
               ex is System.Net.Http.HttpRequestException;
    }
}
```

### Example 2: Authorization Middleware

Checks permissions before allowing operations:

```csharp
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using OakIdeas.GenericRepository.Middleware;

public class AuthorizationMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class
{
    private readonly ClaimsPrincipal _user;
    private readonly string _requiredPermission;

    public AuthorizationMiddleware(ClaimsPrincipal user, string requiredPermission)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _requiredPermission = requiredPermission;
    }

    public override async Task<TEntity> Insert(
        Func<Task<TEntity>> next,
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        CheckPermission("Create");
        return await next();
    }

    public override async Task<TEntity> Update(
        Func<Task<TEntity>> next,
        TEntity entityToUpdate,
        CancellationToken cancellationToken = default)
    {
        CheckPermission("Update");
        return await next();
    }

    public override async Task<bool> Delete(
        Func<Task<bool>> next,
        TEntity entityToDelete,
        CancellationToken cancellationToken = default)
    {
        CheckPermission("Delete");
        return await next();
    }

    private void CheckPermission(string operation)
    {
        var requiredClaim = $"{_requiredPermission}:{operation}";
        
        if (!_user.HasClaim("Permission", requiredClaim))
        {
            throw new UnauthorizedAccessException(
                $"User does not have permission: {requiredClaim}");
        }
    }
}
```

### Example 3: Timestamp Middleware

Automatically sets timestamps on entities:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using OakIdeas.GenericRepository.Middleware;

public interface ITimestamped
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}

public class TimestampMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class, ITimestamped
{
    public override async Task<TEntity> Insert(
        Func<Task<TEntity>> next,
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = null;
        return await next();
    }

    public override async Task<TEntity> Update(
        Func<Task<TEntity>> next,
        TEntity entityToUpdate,
        CancellationToken cancellationToken = default)
    {
        entityToUpdate.UpdatedAt = DateTime.UtcNow;
        return await next();
    }

    public override async Task<IEnumerable<TEntity>> InsertRange(
        Func<Task<IEnumerable<TEntity>>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entity in entities)
        {
            entity.CreatedAt = now;
            entity.UpdatedAt = null;
        }
        return await next();
    }

    public override async Task<IEnumerable<TEntity>> UpdateRange(
        Func<Task<IEnumerable<TEntity>>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entity in entities)
        {
            entity.UpdatedAt = now;
        }
        return await next();
    }
}
```

### Example 4: Soft Delete Middleware

Implements soft delete pattern using middleware:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using OakIdeas.GenericRepository.Middleware;
using OakIdeas.GenericRepository.Models;

public class SoftDeleteMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class, ISoftDeletable
{
    private string? _deletedBy;

    public void SetDeletedBy(string deletedBy)
    {
        _deletedBy = deletedBy;
    }

    // Filter out deleted entities in Get operations
    public override async Task<IEnumerable<TEntity>> Get(
        Func<Task<IEnumerable<TEntity>>> next,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default)
    {
        var results = await next();
        return results.Where(e => !e.IsDeleted);
    }

    public override async Task<TEntity?> GetById(
        Func<Task<TEntity?>> next,
        TKey id,
        CancellationToken cancellationToken = default)
    {
        var entity = await next();
        return (entity != null && !entity.IsDeleted) ? entity : null;
    }

    // Mark as deleted instead of actually deleting
    public override async Task<bool> Delete(
        Func<Task<bool>> next,
        TEntity entityToDelete,
        CancellationToken cancellationToken = default)
    {
        entityToDelete.IsDeleted = true;
        entityToDelete.DeletedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(_deletedBy))
        {
            entityToDelete.DeletedBy = _deletedBy;
            _deletedBy = null;
        }
        
        // Don't call next (actual delete), return true
        return true;
    }

    public override async Task<int> DeleteRange(
        Func<Task<int>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        var now = DateTime.UtcNow;
        var deletedBy = _deletedBy;
        _deletedBy = null;

        foreach (var entity in entityList)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = now;
            if (!string.IsNullOrEmpty(deletedBy))
            {
                entity.DeletedBy = deletedBy;
            }
        }

        return entityList.Count;
    }
}
```

### Example 5: Notification Middleware

Sends notifications when entities are modified:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OakIdeas.GenericRepository.Middleware;

public class NotificationMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class
{
    private readonly INotificationService _notificationService;

    public NotificationMiddleware(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public override async Task<TEntity> Insert(
        Func<Task<TEntity>> next,
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        var result = await next();
        
        await _notificationService.SendAsync(new Notification
        {
            Type = "EntityCreated",
            EntityType = typeof(TEntity).Name,
            Message = $"New {typeof(TEntity).Name} created"
        });

        return result;
    }

    public override async Task<bool> Delete(
        Func<Task<bool>> next,
        TEntity entityToDelete,
        CancellationToken cancellationToken = default)
    {
        var result = await next();

        if (result)
        {
            await _notificationService.SendAsync(new Notification
            {
                Type = "EntityDeleted",
                EntityType = typeof(TEntity).Name,
                Message = $"{typeof(TEntity).Name} deleted"
            });
        }

        return result;
    }
}
```

## Common Patterns

### Pattern 1: Filtering Results

```csharp
public override async Task<IEnumerable<TEntity>> Get(
    Func<Task<IEnumerable<TEntity>>> next,
    Expression<Func<TEntity, bool>>? filter = null,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
    string includeProperties = "",
    CancellationToken cancellationToken = default)
{
    var results = await next();
    
    // Apply additional filtering
    return results.Where(/* your condition */);
}
```

### Pattern 2: Modifying Entities Before Operation

```csharp
public override async Task<TEntity> Insert(
    Func<Task<TEntity>> next,
    TEntity entity,
    CancellationToken cancellationToken = default)
{
    // Modify entity before insert
    entity.SomeProperty = ComputeValue();
    
    return await next();
}
```

### Pattern 3: Post-Processing Results

```csharp
public override async Task<TEntity> Insert(
    Func<Task<TEntity>> next,
    TEntity entity,
    CancellationToken cancellationToken = default)
{
    var result = await next();
    
    // Post-process result
    DoSomethingWith(result);
    
    return result;
}
```

### Pattern 4: Conditional Execution

```csharp
public override async Task<TEntity> Insert(
    Func<Task<TEntity>> next,
    TEntity entity,
    CancellationToken cancellationToken = default)
{
    if (ShouldProcess(entity))
    {
        // Do special processing
        ProcessEntity(entity);
    }
    
    return await next();
}
```

## Testing Your Middleware

### Unit Test Template

```csharp
using Xunit;

public class MyCustomMiddlewareTests
{
    [Fact]
    public async Task MyMiddleware_DoesExpectedBehavior()
    {
        // Arrange
        var middleware = new MyCustomMiddleware<TestEntity, int>(/* dependencies */);
        var testEntity = new TestEntity();
        var nextCalled = false;
        
        Func<Task<TestEntity>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult(testEntity);
        };

        // Act
        var result = await middleware.Insert(next, testEntity);

        // Assert
        Assert.True(nextCalled);
        // Add your specific assertions
    }
}
```

## Best Practices

1. **Always await next()**: Don't block with `.Result`
2. **Handle cancellation tokens**: Respect cancellation requests
3. **Don't swallow exceptions**: Let exceptions propagate unless you handle them specifically
4. **Keep it focused**: One middleware = one responsibility
5. **Document behavior**: Clearly document what your middleware does
6. **Consider performance**: Middleware adds overhead
7. **Test thoroughly**: Test both success and failure paths

## Common Pitfalls

### ❌ Blocking the thread

```csharp
// Wrong
var result = next().Result; // Blocks!
```

```csharp
// Correct
var result = await next();
```

### ❌ Forgetting to call next

```csharp
// Wrong - operation never executes
public override async Task<TEntity> Insert(...)
{
    DoSomething();
    return entity; // Forgot to call next()!
}
```

```csharp
// Correct
public override async Task<TEntity> Insert(...)
{
    DoSomething();
    return await next();
}
```

### ❌ Modifying entities in Get operations

```csharp
// Wrong - side effects in read operations
public override async Task<IEnumerable<TEntity>> Get(...)
{
    var results = await next();
    foreach (var entity in results)
    {
        entity.SomeProperty = "Modified"; // Don't do this!
    }
    return results;
}
```

## See Also

- [Middleware Overview](./README.md)
- [Middleware Examples](./middleware-examples.md)
- [Standard Middleware](./standard-middleware.md)
