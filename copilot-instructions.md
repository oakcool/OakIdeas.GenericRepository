# GitHub Copilot Instructions for OakIdeas.GenericRepository

## Project Overview

This is a .NET library implementing a versatile Generic Repository pattern with support for CRUD operations, middleware pipelines, multiple storage backends, and advanced querying capabilities.

### Key Components
- **Core Library**: Generic repository interfaces and base functionality (netstandard2.0)
- **Memory Implementation**: Dictionary-based in-memory repository (netstandard2.0)
- **Entity Framework Core Implementation**: EF Core-backed repository (netstandard2.0)
- **Middleware System**: Extensible middleware architecture for cross-cutting concerns (netstandard2.0)
- **Test Projects**: Comprehensive test suites (net10.0)

## Technology Stack

### Target Frameworks
- **Libraries**: `netstandard2.0` for maximum compatibility across .NET Framework, .NET Core, and .NET 5+
- **Test Projects**: `net10.0` (latest .NET version)
- **Language Version**: C# 13.0

### Key Dependencies
- Microsoft.Bcl.AsyncInterfaces (9.0.0) - for async enumerable support
- Entity Framework Core (for EF implementation)
- MSTest (for testing)

## Coding Standards

### C# Language Features
- **Use modern C# features**: Pattern matching, switch expressions, record types where appropriate
- **Nullable Reference Types**: ALWAYS enabled - use nullable annotations (`?`) appropriately
- **Async/Await**: Use async methods consistently with `Task` or `ValueTask` return types
- **Expression-bodied members**: Use for simple property getters and one-line methods
- **File-scoped namespaces**: Use `namespace OakIdeas.GenericRepository;` format (no braces)
- **Target-typed new**: Use `new()` when type is obvious from context

### Design Patterns
This project implements several important patterns:
- **Repository Pattern**: Core abstraction for data access
- **Specification Pattern**: Reusable business rules and query logic
- **Query Object Pattern**: Fluent API for building complex queries
- **Middleware Pattern**: Pipeline-based cross-cutting concerns
- **Soft Delete Pattern**: Logical deletion with restoration capabilities

### Naming Conventions
- **Interfaces**: Prefix with `I` (e.g., `IGenericRepository`)
- **Type Parameters**: Use descriptive names like `TEntity`, `TKey` (not single letters)
- **Async Methods**: Suffix with `Async` (e.g., `GetAsync`, `InsertAsync`)
- **Private Fields**: Use `_camelCase` with underscore prefix
- **Constants**: Use `PascalCase` for public, `_PascalCase` for private

### Code Organization
- **One class per file**: Keep file names matching class names
- **Namespace alignment**: Match folder structure
- **Separation of concerns**: Keep interfaces, implementations, and tests separate
- **Dependency injection ready**: Design for constructor injection

## Key Architectural Principles

### Generic Constraints
Always use appropriate generic constraints for type safety:
```csharp
where TEntity : class
where TKey : notnull
```

### Async/Await Best Practices
- Use `ConfigureAwait(false)` in library code (not needed in test code)
- Return `Task` or `ValueTask` for async methods
- Use `IAsyncEnumerable<T>` for streaming large datasets
- Avoid async void (except for event handlers)

### Error Handling
- Throw meaningful exceptions with clear messages
- Use `ArgumentNullException`, `InvalidOperationException`, etc. appropriately
- Document exceptions in XML comments with `<exception>` tags

### Performance Considerations
- Use `ValueTask` for hot paths that may complete synchronously
- Implement `IAsyncEnumerable` for large result sets to avoid loading everything into memory
- Consider memory pooling for frequently allocated objects
- Use spans and memory for high-performance scenarios when needed

## Testing Guidelines

### Test Framework
- Use MSTest with modern analyzers enabled
- Follow AAA pattern: Arrange, Act, Assert
- Use `Assert.HasCount` instead of `Assert.AreEqual` for collection counts
- Use `Assert.Contains` instead of `Assert.IsTrue` with string Contains

### Test Naming
- Use descriptive method names: `MethodName_Scenario_ExpectedResult`
- Example: `Insert_ValidEntity_ReturnsEntityWithId`

### Test Organization
- Group related tests in the same test class
- Use `[TestClass]` and `[TestMethod]` attributes
- Consider `[DataRow]` for parameterized tests

## Documentation Standards

### XML Documentation
Always provide XML documentation for:
- All public types (classes, interfaces, enums)
- All public members (methods, properties, fields)
- Type parameters and constraints
- Exceptions that may be thrown

Example:
```csharp
/// <summary>
/// Retrieves entities matching the specified filter criteria.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <param name="filter">Optional filter expression.</param>
/// <param name="orderBy">Optional ordering function.</param>
/// <returns>A task containing the collection of matching entities.</returns>
/// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
Task<IEnumerable<TEntity>> GetAsync<TEntity>(
    Expression<Func<TEntity, bool>>? filter = null,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null)
    where TEntity : class;
```

### Markdown Documentation
- Keep documentation in the `/docs` folder
- Use clear headings and code examples
- Include usage examples for all major features
- Keep README.md focused on overview and quick start

## Project-Specific Guidelines

### Repository Interface Design
- Methods should accept optional parameters with sensible defaults
- Support both synchronous and asynchronous operations where appropriate
- Return `IEnumerable<T>` or `IAsyncEnumerable<T>` for collections
- Use expressions for filters: `Expression<Func<TEntity, bool>>`

### Middleware Implementation
- Middleware should follow the pipeline pattern
- Each middleware has access to context and calls next delegate
- Support both pre-processing and post-processing logic
- Make middleware composable and reusable

### Entity Base Classes
- `EntityBase`: Provides `ID` property for primary key
- `SoftDeletableEntity`: Adds `IsDeleted` and `DeletedAt` for soft delete support
- Support custom key types through generic `TKey` parameter

### Soft Delete Behavior
- Regular queries should automatically exclude soft-deleted entities
- Provide explicit methods like `GetIncludingDeleted()` when needed
- Support restoration with `Restore()` method
- Provide `PermanentlyDelete()` for hard deletion

## Build and Test Commands

```bash
# Build the solution
dotnet build src/OakIdeas.GenericRepository.sln -c Release

# Run all tests
dotnet test src/OakIdeas.GenericRepository.sln -c Release

# Build specific project
dotnet build src/OakIdeas.GenericRepository/OakIdeas.GenericRepository.csproj

# Pack NuGet packages
dotnet pack src/OakIdeas.GenericRepository.sln -c Release
```

## Common Pitfalls to Avoid

1. **Don't use `.Result` or `.Wait()`** - Always use `await` for async operations
2. **Don't modify collections during enumeration** - Create copies if needed
3. **Don't catch and ignore exceptions** - Log or rethrow with context
4. **Don't use string concatenation in tight loops** - Use `StringBuilder` or interpolation
5. **Don't create unnecessary allocations** - Consider object pooling for hot paths
6. **Don't forget null checks** - Leverage nullable reference types
7. **Don't mix sync and async code** - Be consistent in API design

## When Making Changes

### Before Modifying Code
1. Understand the existing pattern and design
2. Check if similar functionality already exists
3. Review related tests to understand expected behavior
4. Consider backward compatibility (this is a library)

### After Making Changes
1. Build the solution: `dotnet build`
2. Run all tests: `dotnet test`
3. Update XML documentation
4. Update relevant markdown documentation
5. Add/update tests for new functionality
6. Check for any new warnings and address them

## Additional Resources

- **Repository**: https://github.com/oakcool/OakIdeas.GenericRepository
- **Documentation**: See `/docs` folder for detailed guides
- **Issues**: Report bugs and feature requests on GitHub Issues
- **Contributing**: See `docs/contributing.md` for contribution guidelines

## Version Information

- **Current Version**: 0.0.5.1-alpha
- **Target Framework**: netstandard2.0 (libraries), net10.0 (tests)
- **Language Version**: C# 13.0
