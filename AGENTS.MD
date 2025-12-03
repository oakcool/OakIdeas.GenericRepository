# AGENTS.MD

> **Agent Configuration and Capabilities for OakIdeas.GenericRepository**
>
> This document defines how AI coding agents should interact with this repository,
> including capabilities, constraints, and context needed for effective assistance.

## Overview

**OakIdeas.GenericRepository** is a .NET library providing a versatile Generic Repository pattern implementation with middleware support, multiple storage backends, and advanced querying capabilities.

### Repository Information
- **Language**: C# 13.0
- **Target Framework**: netstandard2.0 (libraries), net10.0 (tests)
- **License**: See LICENSE file
- **Maintainer**: OakIdeas (Mateus Carvalho)
- **Repository**: https://github.com/oakcool/OakIdeas.GenericRepository

## Agent Capabilities

### What Agents CAN Do

#### Code Development
- ✅ Implement new repository features following the established patterns
- ✅ Add new middleware implementations
- ✅ Create unit tests using MSTest framework
- ✅ Refactor code while maintaining backward compatibility
- ✅ Add XML documentation to public APIs
- ✅ Update markdown documentation in `/docs` folder
- ✅ Fix bugs in existing code
- ✅ Optimize performance while maintaining readability
- ✅ Add support for new storage backends (following existing patterns)

#### Testing & Validation
- ✅ Write comprehensive unit tests with good coverage
- ✅ Build the solution using `dotnet build`
- ✅ Run tests using `dotnet test`
- ✅ Validate nullable reference type annotations
- ✅ Check for analyzer warnings and address them

#### Documentation
- ✅ Update README.md with new features
- ✅ Create or update documentation in `/docs` folder
- ✅ Add code examples to documentation
- ✅ Improve XML documentation comments
- ✅ Update API reference documentation

### What Agents SHOULD NOT Do

#### Breaking Changes
- ❌ Change public API signatures without explicit approval
- ❌ Remove existing public methods or properties
- ❌ Modify behavior that would break existing consumers
- ❌ Change target framework without discussion
- ❌ Remove or significantly alter existing middleware

#### Quality & Standards
- ❌ Skip writing tests for new functionality
- ❌ Ignore compiler warnings
- ❌ Disable nullable reference types
- ❌ Add dependencies without justification
- ❌ Use deprecated APIs or patterns

#### Project Structure
- ❌ Reorganize folder structure without approval
- ❌ Rename namespaces
- ❌ Move files between projects arbitrarily
- ❌ Change project file settings without reason

## Repository Structure

```
OakIdeas.GenericRepository/
├── .github/
│   └── workflows/           # CI/CD workflows
├── docs/                    # Comprehensive documentation
│   ├── README.md
│   ├── getting-started.md
│   ├── architecture.md
│   ├── best-practices.md
│   └── ...
├── Images/                  # Logo and images for package
├── src/
│   ├── OakIdeas.GenericRepository/
│   │   ├── Core/           # Core interfaces and base classes
│   │   ├── Models/         # Entity base classes
│   │   ├── Specifications/ # Specification pattern implementation
│   │   └── Query/          # Query object pattern
│   ├── OakIdeas.GenericRepository.Memory/
│   │   └── Memory/         # In-memory repository implementation
│   ├── OakIdeas.GenericRepository.EntityFrameworkCore/
│   │   └── EntityFramework/# EF Core repository implementation
│   ├── OakIdeas.GenericRepository.Middleware/
│   │   ├── Core/           # Middleware infrastructure
│   │   └── Standard/       # Built-in middleware (logging, validation, etc.)
│   ├── OakIdeas.GenericRepository.Tests/
│   ├── OakIdeas.GenericRepository.Middleware.Tests/
│   └── OakIdeas.GenericRepository.EntityFrameworkCore.Tests/
├── copilot-instructions.md  # GitHub Copilot specific instructions
├── AGENTS.MD                # This file
├── README.md                # Main project README
└── LICENSE
```

## Development Workflow

### Prerequisites
- .NET 10 SDK or later
- Understanding of Repository Pattern
- Familiarity with C# async/await patterns
- Knowledge of Entity Framework Core (for EF implementation)

### Standard Development Flow

1. **Before Starting**
   ```bash
   # Build to ensure everything compiles
   dotnet build src/OakIdeas.GenericRepository.sln -c Release
   
   # Run tests to ensure baseline functionality
   dotnet test src/OakIdeas.GenericRepository.sln -c Release
   ```

2. **During Development**
   - Follow the patterns established in existing code
   - Write tests alongside implementation
   - Add XML documentation for public APIs
   - Build frequently to catch issues early
   - Run tests to validate changes

3. **Before Completing**
   - Ensure all tests pass
   - Verify no new warnings introduced
   - Update documentation if API changed
   - Review changes for backward compatibility
   - Validate nullable reference type usage

### Testing Strategy

#### Unit Tests
- Located in `*.Tests` projects
- Use MSTest framework with `[TestClass]` and `[TestMethod]`
- Follow AAA pattern (Arrange, Act, Assert)
- Test both success and failure scenarios
- Verify async behavior properly

#### Test Naming Convention
```csharp
[TestMethod]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var repository = new MemoryGenericRepository<Customer>();
    var customer = new Customer { Name = "Test" };
    
    // Act
    var result = await repository.Insert(customer);
    
    // Assert
    Assert.IsNotNull(result);
    Assert.AreEqual("Test", result.Name);
}
```

## Key Patterns and Concepts

### 1. Repository Pattern
Core abstraction providing CRUD operations with consistent interface across different storage backends.

**Key Interface**: `IGenericRepository<TEntity, TKey>`

### 2. Specification Pattern
Encapsulates query logic into reusable, composable specifications.

**Key Classes**: `Specification<T>`, combinators like `And`, `Or`, `Not`

### 3. Query Object Pattern
Fluent API for building complex queries with filtering, sorting, paging, and eager loading.

**Key Class**: `Query<T>`

### 4. Middleware Pattern
Pipeline-based architecture for adding cross-cutting concerns like logging, validation, and performance tracking.

**Key Interface**: `IRepositoryMiddleware<TEntity, TKey>`

### 5. Soft Delete Pattern
Logical deletion allowing entities to be "deleted" without physical removal, with restoration capability.

**Key Classes**: `SoftDeletableEntity`, `ISoftDeleteRepository<TEntity, TKey>`

## Common Tasks for Agents

### Adding a New Repository Implementation

```csharp
// 1. Implement IGenericRepository<TEntity, TKey>
public class MyStorageRepository<TEntity, TKey> 
    : IGenericRepository<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    // Implement all interface methods
}

// 2. Add tests
[TestClass]
public class MyStorageRepositoryTests
{
    // Test all CRUD operations
}

// 3. Update documentation
// - Add to README.md under "Multiple Implementations"
// - Create usage guide in docs/
```

### Adding New Middleware

```csharp
// 1. Implement IRepositoryMiddleware<TEntity, TKey>
public class MyMiddleware<TEntity, TKey> 
    : IRepositoryMiddleware<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    public async Task InvokeAsync(
        RepositoryContext<TEntity, TKey> context,
        RepositoryMiddlewareDelegate<TEntity, TKey> next)
    {
        // Pre-processing
        await next(context);
        // Post-processing
    }
}

// 2. Add extension method for easy configuration
public static RepositoryOptions WithMyMiddleware(
    this RepositoryOptions options, bool enabled = true)
{
    // Configure middleware
    return options;
}

// 3. Add tests
// 4. Update documentation
```

### Extending Entity Models

```csharp
// For basic entities, inherit from EntityBase
public class MyEntity : EntityBase
{
    public string Name { get; set; } = string.Empty;
}

// For soft-deletable entities, inherit from SoftDeletableEntity
public class MySoftDeleteEntity : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
}

// For custom key types, use generic base
public class MyCustomKeyEntity : EntityBase<Guid>
{
    public string Name { get; set; } = string.Empty;
}
```

## Code Quality Standards

### Must Have
- ✅ XML documentation on all public APIs
- ✅ Unit tests for all new functionality
- ✅ Nullable reference type annotations
- ✅ Async/await patterns for I/O operations
- ✅ Proper exception handling
- ✅ Follow existing code style and conventions

### Should Have
- ✅ Performance tests for critical paths
- ✅ Example code in documentation
- ✅ Integration tests for complex scenarios
- ✅ Benchmark tests for performance-critical code
- ✅ Clear commit messages

### Should Avoid
- ❌ Synchronous I/O in async methods
- ❌ Catching and swallowing exceptions
- ❌ Large methods (>50 lines)
- ❌ Complex nested conditionals
- ❌ Magic numbers or strings
- ❌ Unused using directives

## Build and Test Commands

### Build Commands
```bash
# Build entire solution
dotnet build src/OakIdeas.GenericRepository.sln -c Release

# Build specific project
dotnet build src/OakIdeas.GenericRepository/OakIdeas.GenericRepository.csproj -c Release

# Clean and rebuild
dotnet clean src/OakIdeas.GenericRepository.sln
dotnet build src/OakIdeas.GenericRepository.sln -c Release
```

### Test Commands
```bash
# Run all tests
dotnet test src/OakIdeas.GenericRepository.sln -c Release

# Run tests with coverage
dotnet test src/OakIdeas.GenericRepository.sln --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test src/OakIdeas.GenericRepository.Tests/OakIdeas.GenericRepository.Tests.csproj

# Run tests with detailed output
dotnet test src/OakIdeas.GenericRepository.sln -c Release --logger "console;verbosity=detailed"
```

### Package Commands
```bash
# Create NuGet packages
dotnet pack src/OakIdeas.GenericRepository.sln -c Release

# Pack with version override
dotnet pack src/OakIdeas.GenericRepository.sln -c Release /p:Version=0.0.6.0
```

## API Compatibility Guidelines

This is a library package distributed via NuGet. **Backward compatibility is critical.**

### Versioning Strategy
- **Major**: Breaking changes (change major version number)
- **Minor**: New features, backward compatible (increment minor version)
- **Patch**: Bug fixes, backward compatible (increment patch version)
- **Alpha/Beta**: Pre-release versions for testing

### When Making Changes
1. **Adding new methods/properties**: ✅ Safe (minor version bump)
2. **Adding optional parameters**: ✅ Safe (minor version bump)
3. **Changing method signatures**: ❌ Breaking (requires major version bump)
4. **Removing public members**: ❌ Breaking (requires major version bump)
5. **Changing return types**: ❌ Usually breaking (requires major version bump)
6. **Adding interfaces to existing classes**: ⚠️ May be breaking (evaluate case by case)

## Documentation Requirements

### For New Features
1. XML documentation on all public APIs
2. README.md update with basic usage example
3. Detailed guide in `/docs` folder
4. Update `docs/api-reference.md` if applicable
5. Add to appropriate existing documentation files

### Documentation Style
- Use clear, concise language
- Provide practical code examples
- Include both simple and advanced scenarios
- Document exceptions and edge cases
- Keep examples up-to-date with current API

## Performance Considerations

### For Repository Implementations
- Use async I/O for database/storage operations
- Support `IAsyncEnumerable<T>` for streaming large result sets
- Consider connection pooling and resource management
- Implement proper disposal patterns
- Use efficient data structures (avoid O(n²) operations)

### For Middleware
- Keep middleware logic lightweight
- Avoid blocking operations
- Consider using `ValueTask` for hot paths
- Minimize allocations in frequently-called code
- Profile performance-critical paths

## Security Considerations

### Input Validation
- Validate all input parameters
- Use parameterized queries (prevent SQL injection)
- Sanitize user input before using in dynamic queries
- Check for null references appropriately

### Data Access
- Follow principle of least privilege
- Implement proper authentication/authorization hooks
- Don't log sensitive data
- Properly dispose of resources

## Agent Communication Guidelines

### When Asking Questions
Agents should specify:
1. What you're trying to accomplish
2. What you've already tried
3. Specific error messages or unexpected behavior
4. Relevant code context

### When Reporting Issues
Include:
1. Steps to reproduce
2. Expected vs actual behavior
3. Environment details (.NET version, OS)
4. Relevant logs or error messages

### When Proposing Changes
Explain:
1. The problem being solved
2. Why this approach was chosen
3. Impact on existing functionality
4. Testing strategy

## Additional Resources

### Internal Documentation
- `/docs/getting-started.md` - Quick start guide
- `/docs/architecture.md` - Architectural overview
- `/docs/best-practices.md` - Best practices and patterns
- `/docs/contributing.md` - Contribution guidelines
- `/docs/api-reference.md` - Detailed API documentation

### External References
- [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html)
- [Specification Pattern](https://deviq.com/design-patterns/specification-pattern)
- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [C# 13 What's New](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-13)
- [MSTest Documentation](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest)

## Maintenance and Updates

### Regular Maintenance Tasks
- Keep dependencies up to date
- Update documentation as features evolve
- Review and address GitHub issues
- Monitor for security vulnerabilities
- Update examples to reflect current best practices

### Agent Responsibilities
- Ensure changes align with project goals
- Maintain code quality standards
- Preserve backward compatibility
- Update documentation alongside code changes
- Write comprehensive tests

---

**Last Updated**: 2025-12-03  
**Document Version**: 1.0.0  
**Maintained By**: OakIdeas Team

For questions or suggestions about this document, please open an issue on GitHub.
