# AI Context for OakIdeas.GenericRepository

> This file provides context for AI assistants working with this codebase.

## What This Project Does

OakIdeas.GenericRepository is a .NET library that implements the Repository Pattern for data access with:
- Generic CRUD operations for any entity type
- Multiple storage backends (Memory, Entity Framework Core)
- Middleware pipeline for cross-cutting concerns (logging, validation, performance tracking)
- Advanced querying with Specification and Query Object patterns
- Soft delete support for logical deletion
- Async/await throughout for scalability

## Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│           IGenericRepository<TEntity, TKey>         │
│  (Core interface for CRUD operations)               │
└────────────────┬────────────────────────────────────┘
                 │
       ┌─────────┴──────────┬──────────────────────────┐
       │                    │                          │
┌──────▼──────┐    ┌────────▼─────────┐    ┌──────────▼────────┐
│   Memory    │    │  Entity          │    │  Future           │
│ Repository  │    │  Framework       │    │  Implementations  │
│             │    │  Repository      │    │  (MongoDB, etc)   │
└─────────────┘    └──────────────────┘    └───────────────────┘

                 ┌──────────────────────┐
                 │  Middleware Pipeline │
                 │  (Logging, Validation,│
                 │   Performance, etc)   │
                 └──────────────────────┘
```

## Key Design Decisions

### 1. Target Framework: netstandard2.0
**Why**: Maximum compatibility across .NET Framework 4.6.1+, .NET Core 2.0+, and all modern .NET versions.

### 2. Generic Key Type (`TKey`)
**Why**: Allows flexibility for different ID types (int, Guid, string, etc.) while maintaining type safety.

### 3. Async-First API
**Why**: Modern applications need scalability. All I/O operations are async.

### 4. Expression Trees for Queries
**Why**: Type-safe queries that can be translated by different providers (EF Core, etc.).

### 5. Middleware Pattern
**Why**: Separates cross-cutting concerns, makes code testable, allows composition.

## Common Scenarios

### Scenario 1: Adding a New Storage Backend
1. Implement `IGenericRepository<TEntity, TKey>`
2. Handle all CRUD operations for your storage
3. Consider implementing `IAsyncEnumerable` for streaming
4. Add comprehensive tests
5. Document usage patterns

### Scenario 2: Creating Custom Middleware
1. Implement `IRepositoryMiddleware<TEntity, TKey>`
2. Call `await next(context)` to continue pipeline
3. Add logic before/after the next call
4. Create extension method for easy configuration
5. Add tests for middleware behavior

### Scenario 3: Defining Entity Models
```csharp
// Simple entity
public class Product : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

// Soft-deletable entity
public class Customer : SoftDeletableEntity
{
    public string Email { get; set; } = string.Empty;
}

// Custom key type
public class Order : EntityBase<Guid>
{
    public DateTime OrderDate { get; set; }
}
```

## Code Style Quick Reference

### ✅ Do This
```csharp
// Async all the way
public async Task<Customer?> GetCustomerAsync(int id)
{
    return await repository.GetAsync(id);
}

// Nullable reference types
public string? OptionalValue { get; set; }
public string RequiredValue { get; set; } = string.Empty;

// Expression-bodied members for simple cases
public bool IsValid => !string.IsNullOrEmpty(Name);

// File-scoped namespaces
namespace OakIdeas.GenericRepository.Memory;

public class MemoryRepository { }
```

### ❌ Don't Do This
```csharp
// Don't use .Result or .Wait()
var customer = repository.GetAsync(id).Result; // ❌

// Don't ignore nullability
public string Name { get; set; } // ❌ Should be string? or initialized

// Don't use old-style namespaces unnecessarily
namespace OakIdeas.GenericRepository.Memory // ❌ (missing semicolon)
{
    public class MemoryRepository { }
}
```

## Understanding the Codebase

### Entry Points
1. **Core Interfaces**: `src/OakIdeas.GenericRepository/Core/IGenericRepository.cs`
2. **Memory Implementation**: `src/OakIdeas.GenericRepository.Memory/Memory/MemoryGenericRepository.cs`
3. **Middleware System**: `src/OakIdeas.GenericRepository.Middleware/Core/MiddlewarePipeline.cs`

### Important Base Classes
- `EntityBase` / `EntityBase<TKey>`: Base for entities with ID
- `SoftDeletableEntity`: Adds soft delete capability
- `Specification<T>`: Base for reusable query specifications

### Key Interfaces
- `IGenericRepository<TEntity, TKey>`: Main repository contract
- `ISoftDeleteRepository<TEntity, TKey>`: Soft delete operations
- `IRepositoryMiddleware<TEntity, TKey>`: Middleware contract

## Testing Philosophy

### Test Structure
- Each feature should have corresponding tests
- Use AAA pattern (Arrange, Act, Assert)
- Test both success and failure paths
- Mock external dependencies

### Test Coverage Goals
- All public methods should have tests
- Edge cases and error conditions covered
- Integration tests for complex scenarios
- Performance tests for critical paths

## When Proposing Changes

### Consider
1. **Backward Compatibility**: Will this break existing users?
2. **Performance**: Will this add overhead to hot paths?
3. **Testability**: Can this be easily tested?
4. **Documentation**: What docs need updating?
5. **Consistency**: Does this match existing patterns?

### Before Committing
- [ ] Build succeeds: `dotnet build`
- [ ] All tests pass: `dotnet test`
- [ ] No new warnings introduced
- [ ] XML docs added/updated for public APIs
- [ ] Relevant documentation files updated
- [ ] Nullable annotations correct

## Dependencies Management

### Current Dependencies (minimal by design)
- `Microsoft.Bcl.AsyncInterfaces` - For `IAsyncEnumerable<T>` support on netstandard2.0

### Adding New Dependencies
- ✅ Justify why it's needed
- ✅ Check license compatibility
- ✅ Consider target framework support
- ✅ Minimize dependency tree
- ❌ Avoid dependencies that pull in large trees

## Common Gotchas

1. **netstandard2.0 Limitations**: Some newer C# features require polyfills
2. **Async Enumerable**: Need `Microsoft.Bcl.AsyncInterfaces` package
3. **EF Core Version**: Check compatibility with target framework
4. **Memory Repository**: Not thread-safe by default (by design, for testing)
5. **Middleware Order**: Matters! Middleware executes in registration order

## Useful Patterns in This Codebase

### Fluent Configuration
```csharp
var options = new RepositoryOptions()
    .WithLogging(enabled: true)
    .WithValidation(enabled: true)
    .WithPerformanceTracking(enabled: true);
```

### Specification Composition
```csharp
var spec = new ActiveCustomerSpec()
    .And(new EmailVerifiedSpec())
    .Or(new AdminSpec());
```

### Query Building
```csharp
var query = new Query<Customer>()
    .Where(c => c.IsActive)
    .Sort(q => q.OrderBy(c => c.Name))
    .Paged(page: 1, pageSize: 20);
```

## References

- Main README: `/README.md`
- Documentation: `/docs/` folder
- Copilot Instructions: `/copilot-instructions.md`
- Agent Configuration: `/AGENTS.MD`
- GitHub Context: `/.github/copilot-context.md`

---

**Quick Start for AI Assistants**:
1. Read this file first for overview
2. Check `copilot-instructions.md` for detailed coding standards
3. Review `AGENTS.MD` for comprehensive workflow
4. Explore `/docs` for feature-specific details
5. Build and test before making changes

**Philosophy**: Keep it simple, type-safe, async-first, and well-tested.
