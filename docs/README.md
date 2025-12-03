# OakIdeas.GenericRepository Documentation

Welcome to the OakIdeas.GenericRepository documentation. This library provides a simple and generic implementation of the Repository pattern for CRUD operations.

## Table of Contents

- [Architecture Overview](./architecture.md)
- [Getting Started](./getting-started.md)
- [Usage Examples](./usage-examples.md)
- [Specification Pattern](./specification-pattern.md)
- [API Reference](./api-reference.md)
- [Best Practices](./best-practices.md)
- [Contributing Guidelines](./contributing.md)
- [Future Improvements](./improvement-proposals.md)

## Quick Links

- [GitHub Repository](https://github.com/oakcool/OakIdeas.GenericRepository)
- [NuGet Package - Core](https://www.nuget.org/packages/OakIdeas.GenericRepository/)
- [NuGet Package - EntityFrameworkCore](https://www.nuget.org/packages/OakIdeas.GenericRepository.EntityFrameworkCore/)

## Overview

OakIdeas.GenericRepository provides two main implementations:

1. **MemoryGenericRepository**: In-memory implementation using a concurrent dictionary, ideal for testing and development
2. **EntityFrameworkCoreRepository**: Entity Framework Core implementation for production database scenarios

Both implementations share the same `IGenericRepository<TEntity>` interface, making it easy to swap between implementations or create your own custom repository.

## Key Features

- Generic interface for CRUD operations
- Support for LINQ filtering and ordering
- Async/await support throughout
- Thread-safe in-memory implementation
- Entity Framework Core integration with eager loading support
- **Specification Pattern for reusable query logic**
- Comprehensive XML documentation
- Full test coverage

## Installation

### Core Library

```bash
dotnet add package OakIdeas.GenericRepository
```

### Entity Framework Core Integration

```bash
dotnet add package OakIdeas.GenericRepository.EntityFrameworkCore
```

## Quick Start

```csharp
using OakIdeas.GenericRepository;
using OakIdeas.GenericRepository.Models;

// Define your entity
public class Customer : EntityBase
{
    public string Name { get; set; }
}

// Use the repository
var repository = new MemoryGenericRepository<Customer>();
var customer = await repository.Insert(new Customer { Name = "John Doe" });
```

For more detailed examples, see the [Usage Examples](./usage-examples.md) documentation.
