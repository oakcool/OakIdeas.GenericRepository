# Contributing Guidelines

Thank you for your interest in contributing to OakIdeas.GenericRepository! This document provides guidelines and instructions for contributing.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Making Changes](#making-changes)
- [Testing](#testing)
- [Submitting Changes](#submitting-changes)
- [Coding Standards](#coding-standards)
- [Documentation](#documentation)

## Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inspiring community for all. Please be respectful and constructive in all interactions.

### Expected Behavior

- Be respectful of differing viewpoints
- Accept constructive criticism gracefully
- Focus on what is best for the community
- Show empathy towards other community members

## Getting Started

### Prerequisites

- .NET SDK 8.0 or higher
- Git
- A code editor (Visual Studio, VS Code, or Rider recommended)
- Basic knowledge of C# and Entity Framework Core

### Finding Work

1. **Check existing issues**: Look for issues labeled `good-first-issue` or `help-wanted`
2. **Review improvement proposals**: See [improvement-proposals.md](./improvement-proposals.md)
3. **Propose new features**: Open an issue for discussion before starting work

## Development Setup

### 1. Fork and Clone

```bash
# Fork the repository on GitHub, then:
git clone https://github.com/YOUR-USERNAME/OakIdeas.GenericRepository.git
cd OakIdeas.GenericRepository
```

### 2. Create a Branch

```bash
git checkout -b feature/your-feature-name
# or
git checkout -b fix/issue-number-description
```

### 3. Build the Solution

```bash
cd src
dotnet restore
dotnet build
```

### 4. Run Tests

```bash
dotnet test
```

All tests should pass before you start making changes.

## Making Changes

### Branch Naming Convention

- Features: `feature/feature-name`
- Bug fixes: `fix/issue-number-description`
- Documentation: `docs/description`
- Refactoring: `refactor/description`

### Commit Messages

Follow the conventional commits format:

```
type(scope): subject

body (optional)

footer (optional)
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `test`: Adding or updating tests
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `chore`: Maintenance tasks

**Examples:**

```
feat(repository): add pagination support

Implements GetPaged method with PagedResult<T> return type.
Includes support for filtering and ordering.

Closes #123
```

```
fix(memory-repo): correct null handling in Delete method

Previously threw NullReferenceException when deleting non-existent entity.
Now returns true without error as per specification.

Fixes #45
```

### Code Changes Checklist

Before submitting your changes, ensure:

- [ ] Code follows the project's coding standards
- [ ] All existing tests pass
- [ ] New tests are added for new functionality
- [ ] XML documentation is added for public APIs
- [ ] Documentation is updated if needed
- [ ] No compiler warnings are introduced
- [ ] Code is properly formatted

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test src/OakIdeas.GenericRepository.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Writing Tests

#### Test Structure

```csharp
[TestClass]
public class FeatureName_Tests
{
    private IGenericRepository<TestEntity> _repository;

    [TestInitialize]
    public void Setup()
    {
        _repository = new MemoryGenericRepository<TestEntity>();
    }

    [TestMethod]
    public async Task MethodName_Scenario_ExpectedResult()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test" };

        // Act
        var result = await _repository.Insert(entity);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ID > 0);
    }
}
```

#### Test Coverage Requirements

- **New features**: Minimum 80% code coverage
- **Bug fixes**: Test that reproduces the bug + test that verifies fix
- **Public APIs**: Test all public methods and properties
- **Error cases**: Test null inputs, invalid arguments, edge cases

#### Test Categories

1. **Unit Tests**: Test individual methods in isolation
2. **Integration Tests**: Test repository with actual EF Core context
3. **Error Tests**: Test exception handling and validation

### Example Test Cases

```csharp
// Happy path
[TestMethod]
public async Task Insert_ValidEntity_ReturnsEntityWithId() { }

// Error handling
[TestMethod]
[ExpectedException(typeof(ArgumentNullException))]
public async Task Insert_NullEntity_ThrowsException() { }

// Edge cases
[TestMethod]
public async Task Get_NonExistentId_ReturnsNull() { }

// Complex scenarios
[TestMethod]
public async Task Get_WithFilterAndOrderBy_ReturnsCorrectResults() { }
```

## Submitting Changes

### 1. Push Your Changes

```bash
git push origin feature/your-feature-name
```

### 2. Create Pull Request

1. Go to the repository on GitHub
2. Click "New Pull Request"
3. Select your branch
4. Fill in the PR template

### Pull Request Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Related Issue
Closes #issue-number

## Testing
- [ ] All existing tests pass
- [ ] New tests added and passing
- [ ] Manual testing completed

## Checklist
- [ ] Code follows style guidelines
- [ ] XML documentation added
- [ ] Documentation updated
- [ ] No compiler warnings
```

### 3. Code Review Process

1. Automated checks will run (build, tests)
2. Maintainers will review your code
3. Address feedback by pushing additional commits
4. Once approved, maintainers will merge

### What Reviewers Look For

- **Correctness**: Does the code work as intended?
- **Tests**: Are there adequate tests?
- **Design**: Does it fit the architecture?
- **Documentation**: Is it well-documented?
- **Performance**: Are there performance implications?
- **Security**: Are there security concerns?

## Coding Standards

### C# Style Guide

Follow standard C# conventions:

```csharp
// ✅ Good
public async Task<Customer> GetCustomerAsync(int id)
{
    if (id <= 0)
        throw new ArgumentException("ID must be positive", nameof(id));

    return await _repository.Get(id);
}

// ❌ Bad
public async Task<Customer> getcustomer(int ID)
{
    return await _repository.Get(ID);
}
```

### Naming Conventions

- **Classes/Interfaces**: PascalCase (`CustomerRepository`)
- **Methods**: PascalCase (`GetCustomer`)
- **Parameters/Variables**: camelCase (`customerId`)
- **Private Fields**: _camelCase (`_repository`)
- **Constants**: PascalCase (`MaxRetryCount`)

### Code Formatting

- Use 4 spaces for indentation (no tabs)
- Opening braces on new line for classes and methods
- Single line for short statements
- Max line length: 120 characters

### XML Documentation

All public APIs must have XML documentation:

```csharp
/// <summary>
/// Gets an entity by its primary key.
/// </summary>
/// <param name="id">The primary key value</param>
/// <returns>The entity if found, null otherwise</returns>
/// <exception cref="ArgumentNullException">Thrown when id is null</exception>
public async Task<TEntity> Get(object id)
{
    // Implementation
}
```

### Error Handling

```csharp
// ✅ Validate input
public async Task<TEntity> Insert(TEntity entity)
{
    if (entity == null)
        throw new ArgumentNullException(nameof(entity));
    
    // Implementation
}

// ❌ Don't swallow exceptions
public async Task<TEntity> Get(int id)
{
    try
    {
        return await _repository.Get(id);
    }
    catch
    {
        return null; // Bad: loses error information
    }
}
```

### Async/Await Guidelines

```csharp
// ✅ Good
public async Task<Customer> GetCustomerAsync(int id)
{
    return await _repository.Get(id);
}

// ❌ Bad - avoid async void
public async void ProcessCustomer(int id)
{
    await _repository.Get(id);
}

// ❌ Bad - don't block async
public Customer GetCustomer(int id)
{
    return _repository.Get(id).Result;
}
```

## Documentation

### When to Update Documentation

Update documentation when:
- Adding new public APIs
- Changing existing behavior
- Adding new features
- Fixing bugs that affect usage

### Documentation Files to Update

1. **API Reference** (`docs/api-reference.md`): For API changes
2. **Usage Examples** (`docs/usage-examples.md`): For new features
3. **Best Practices** (`docs/best-practices.md`): For recommended patterns
4. **README.md**: For major changes or new sections
5. **Improvement Proposals** (`docs/improvement-proposals.md`): For new proposals

### Documentation Style

- Use clear, concise language
- Include code examples
- Show both correct and incorrect usage
- Document exceptions and edge cases
- Keep examples realistic and practical

### Example Documentation Pattern

```markdown
### MethodName

Brief description of what the method does.

**Parameters:**
- `param1` (Type): Description
- `param2` (Type): Description

**Returns:** Description of return value

**Exceptions:**
- `ExceptionType`: When it's thrown

**Example:**
\`\`\`csharp
var result = await repository.MethodName(param1, param2);
\`\`\`

**See Also:**
- [Related Method](#related-method)
```

## Release Process

Releases are handled by maintainers:

1. Version bump (SemVer)
2. Update CHANGELOG.md
3. Create GitHub release
4. Publish NuGet packages

### Versioning

We follow Semantic Versioning (SemVer):

- **Major** (X.0.0): Breaking changes
- **Minor** (0.X.0): New features, backward compatible
- **Patch** (0.0.X): Bug fixes

## Getting Help

- **Questions**: Open a discussion on GitHub
- **Bugs**: Open an issue with "bug" label
- **Features**: Open an issue for discussion
- **Security**: Email maintainers directly (see SECURITY.md if available)

## Recognition

Contributors are recognized in:
- README.md contributors section
- GitHub contributors page
- Release notes for significant contributions

## License

By contributing, you agree that your contributions will be licensed under the same license as the project (see LICENSE file).

---

Thank you for contributing to OakIdeas.GenericRepository! Your efforts help make this library better for everyone.
