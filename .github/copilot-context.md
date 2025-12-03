# GitHub Copilot Context

> Quick reference for GitHub Copilot when working with this repository.

## Quick Facts

- **Language**: C# 13.0
- **Library Target**: netstandard2.0
- **Test Target**: net10.0
- **Test Framework**: MSTest
- **Nullable**: Enabled

## Key Commands

```bash
# Build
dotnet build src/OakIdeas.GenericRepository.sln -c Release

# Test
dotnet test src/OakIdeas.GenericRepository.sln -c Release

# Pack
dotnet pack src/OakIdeas.GenericRepository.sln -c Release
```

## Project Layout

- `src/OakIdeas.GenericRepository/` - Core library
- `src/OakIdeas.GenericRepository.Memory/` - In-memory implementation
- `src/OakIdeas.GenericRepository.EntityFrameworkCore/` - EF Core implementation
- `src/OakIdeas.GenericRepository.Middleware/` - Middleware system
- `src/*.Tests/` - Test projects
- `docs/` - Comprehensive documentation

## Common Patterns

### Repository Method Signature
```csharp
Task<TEntity?> GetAsync<TEntity>(TKey id) where TEntity : class;
Task<IEnumerable<TEntity>> GetAsync<TEntity>(
    Expression<Func<TEntity, bool>>? filter = null,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null)
    where TEntity : class;
```

### Middleware Implementation
```csharp
public class MyMiddleware<TEntity, TKey> : IRepositoryMiddleware<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    public async Task InvokeAsync(
        RepositoryContext<TEntity, TKey> context,
        RepositoryMiddlewareDelegate<TEntity, TKey> next)
    {
        // Before
        await next(context);
        // After
    }
}
```

### Test Structure
```csharp
[TestClass]
public class MyTests
{
    [TestMethod]
    public async Task Method_Scenario_Expected()
    {
        // Arrange
        // Act
        // Assert
    }
}
```

## Important Notes

1. **Backward Compatibility**: This is a NuGet library - don't break existing APIs
2. **XML Docs**: All public APIs must have XML documentation
3. **Async Pattern**: Use async/await consistently, suffix methods with `Async`
4. **Nullable**: Use nullable reference types properly (`?` for nullable)
5. **Generic Constraints**: Always use `where TEntity : class` and `where TKey : notnull`

## More Information

See:
- `copilot-instructions.md` - Comprehensive coding guidelines
- `AGENTS.MD` - Full agent capabilities and workflow
- `docs/` - Detailed documentation
