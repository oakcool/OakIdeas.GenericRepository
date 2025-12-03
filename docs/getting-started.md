# Getting Started

This guide will help you get up and running with OakIdeas.GenericRepository.

## Prerequisites

- .NET Standard 2.0 or higher
- C# 7.3 or higher
- For Entity Framework Core implementation: EF Core 2.0 or higher

## Installation

### Option 1: Using .NET CLI

For the core library (includes in-memory repository):
```bash
dotnet add package OakIdeas.GenericRepository
```

For Entity Framework Core support:
```bash
dotnet add package OakIdeas.GenericRepository.EntityFrameworkCore
```

### Option 2: Using Package Manager Console

```powershell
Install-Package OakIdeas.GenericRepository
Install-Package OakIdeas.GenericRepository.EntityFrameworkCore
```

### Option 3: Using PackageReference in .csproj

```xml
<ItemGroup>
  <PackageReference Include="OakIdeas.GenericRepository" Version="0.0.5.1-alpha" />
  <PackageReference Include="OakIdeas.GenericRepository.EntityFrameworkCore" Version="0.0.5.2-alpha" />
</ItemGroup>
```

## Basic Setup

### Step 1: Define Your Entity

All entities must inherit from `EntityBase`:

```csharp
using OakIdeas.GenericRepository.Models;

public class Customer : EntityBase
{
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

The `EntityBase` class provides an `ID` property of type `int` which serves as the primary key.

### Step 2: Choose an Implementation

#### Option A: In-Memory Repository (for testing/development)

```csharp
using OakIdeas.GenericRepository;

var customerRepository = new MemoryGenericRepository<Customer>();
```

#### Option B: Entity Framework Core Repository (for production)

First, create your DbContext:

```csharp
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options)
    {
    }
}
```

Then create the repository:

```csharp
using OakIdeas.GenericRepository.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer("your-connection-string")
    .Options;

var context = new ApplicationDbContext(options);
var customerRepository = new EntityFrameworkCoreRepository<Customer, ApplicationDbContext>(context);
```

### Step 3: Perform CRUD Operations

```csharp
// Create
var customer = new Customer 
{ 
    Name = "John Doe",
    Email = "john@example.com",
    CreatedAt = DateTime.UtcNow
};
var insertedCustomer = await customerRepository.Insert(customer);
Console.WriteLine($"Created customer with ID: {insertedCustomer.ID}");

// Read by ID
var retrievedCustomer = await customerRepository.Get(insertedCustomer.ID);
Console.WriteLine($"Retrieved: {retrievedCustomer.Name}");

// Read with filter
var customers = await customerRepository.Get(
    filter: c => c.Name.Contains("John")
);
foreach (var c in customers)
{
    Console.WriteLine($"Found: {c.Name}");
}

// Update
retrievedCustomer.Email = "newemail@example.com";
var updatedCustomer = await customerRepository.Update(retrievedCustomer);
Console.WriteLine($"Updated email to: {updatedCustomer.Email}");

// Delete
var deleted = await customerRepository.Delete(updatedCustomer);
Console.WriteLine($"Deleted: {deleted}");
```

## Dependency Injection Setup

### ASP.NET Core Integration

In `Program.cs` or `Startup.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using OakIdeas.GenericRepository;
using OakIdeas.GenericRepository.EntityFrameworkCore;

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
builder.Services.AddScoped<IGenericRepository<Customer>, 
    EntityFrameworkCoreRepository<Customer, ApplicationDbContext>>();
```

Then inject in your controllers or services:

```csharp
public class CustomerService
{
    private readonly IGenericRepository<Customer> _customerRepository;

    public CustomerService(IGenericRepository<Customer> customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Customer> CreateCustomerAsync(string name, string email)
    {
        var customer = new Customer { Name = name, Email = email };
        return await _customerRepository.Insert(customer);
    }
}
```

## Testing Setup

For unit tests, use the in-memory repository:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository;

[TestClass]
public class CustomerServiceTests
{
    private IGenericRepository<Customer> _repository;
    private CustomerService _service;

    [TestInitialize]
    public void Setup()
    {
        _repository = new MemoryGenericRepository<Customer>();
        _service = new CustomerService(_repository);
    }

    [TestMethod]
    public async Task CreateCustomer_ValidData_ReturnsCustomerWithId()
    {
        // Arrange
        var name = "Test Customer";
        var email = "test@example.com";

        // Act
        var result = await _service.CreateCustomerAsync(name, email);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ID > 0);
        Assert.AreEqual(name, result.Name);
    }
}
```

## Common Patterns

### Repository Factory

For applications with many entity types:

```csharp
public interface IRepositoryFactory
{
    IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : EntityBase;
}

public class RepositoryFactory : IRepositoryFactory
{
    private readonly ApplicationDbContext _context;

    public RepositoryFactory(ApplicationDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : EntityBase
    {
        return new EntityFrameworkCoreRepository<TEntity, ApplicationDbContext>(_context);
    }
}
```

### Unit of Work Pattern

Combine multiple repositories in a transaction:

```csharp
public class UnitOfWork : IDisposable
{
    private readonly ApplicationDbContext _context;
    private IGenericRepository<Customer> _customers;
    private IGenericRepository<Order> _orders;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<Customer> Customers => 
        _customers ??= new EntityFrameworkCoreRepository<Customer, ApplicationDbContext>(_context);

    public IGenericRepository<Order> Orders => 
        _orders ??= new EntityFrameworkCoreRepository<Order, ApplicationDbContext>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
```

## Next Steps

- Read [Usage Examples](./usage-examples.md) for more detailed scenarios
- Check [Best Practices](./best-practices.md) for recommendations
- Review [API Reference](./api-reference.md) for complete method documentation
- See [Contributing Guidelines](./contributing.md) if you want to contribute

## Troubleshooting

### Issue: Entity doesn't have an ID after insert
**Solution**: Ensure your entity inherits from `EntityBase` and the ID property is not manually set to a value that already exists.

### Issue: Concurrent modification exceptions with EntityFrameworkCoreRepository
**Solution**: Ensure you're using a scoped DbContext per request/operation, not a singleton.

### Issue: Navigation properties not loaded
**Solution**: Use the `includeProperties` parameter: `await repository.Get(includeProperties: "Orders,Address")`

### Issue: Null reference exception when updating
**Solution**: Ensure the entity exists and has a valid ID before calling Update.
