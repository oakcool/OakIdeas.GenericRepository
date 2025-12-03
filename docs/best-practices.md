# Best Practices

This guide provides recommendations for using OakIdeas.GenericRepository effectively and efficiently.

## General Practices

### 1. Use Async/Await Consistently

✅ **DO:**
```csharp
public async Task<Customer> GetCustomerAsync(int id)
{
    return await _repository.Get(id);
}
```

❌ **DON'T:**
```csharp
public Customer GetCustomer(int id)
{
    return _repository.Get(id).Result; // Blocks thread, can cause deadlocks
}
```

### 2. Always Check for Null Results

✅ **DO:**
```csharp
var customer = await repository.Get(id);
if (customer == null)
{
    throw new NotFoundException($"Customer {id} not found");
}
// Use customer safely
```

❌ **DON'T:**
```csharp
var customer = await repository.Get(id);
Console.WriteLine(customer.Name); // NullReferenceException if not found
```

### 3. Validate Input Before Repository Operations

✅ **DO:**
```csharp
public async Task<Customer> CreateCustomer(Customer customer)
{
    if (customer == null)
        throw new ArgumentNullException(nameof(customer));
    
    if (string.IsNullOrWhiteSpace(customer.Name))
        throw new ValidationException("Customer name is required");
    
    return await _repository.Insert(customer);
}
```

### 4. Use Dependency Injection

✅ **DO:**
```csharp
public class CustomerService
{
    private readonly IGenericRepository<Customer> _repository;
    
    public CustomerService(IGenericRepository<Customer> repository)
    {
        _repository = repository;
    }
}
```

❌ **DON'T:**
```csharp
public class CustomerService
{
    public async Task DoSomething()
    {
        var repository = new MemoryGenericRepository<Customer>(); // Hard-coded dependency
    }
}
```

## Entity Framework Core Specific

### 1. Use Scoped Lifetime for DbContext

✅ **DO:**
```csharp
// In ASP.NET Core
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString), 
    ServiceLifetime.Scoped); // Default and correct
```

❌ **DON'T:**
```csharp
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString),
    ServiceLifetime.Singleton); // Not thread-safe!
```

### 2. Load Only What You Need

✅ **DO:**
```csharp
// Only include navigation properties you'll use
var orders = await repository.Get(
    filter: o => o.CustomerId == customerId,
    includeProperties: "Items"
);
```

❌ **DON'T:**
```csharp
// Loading everything unnecessarily
var orders = await repository.Get(
    includeProperties: "Customer,Items,Items.Product,Customer.Address,Customer.Orders"
);
```

### 3. Be Mindful of Connection Lifetime

✅ **DO:**
```csharp
// In ASP.NET Core - one DbContext per request (scoped)
public class OrderController : ControllerBase
{
    private readonly IGenericRepository<Order> _repository;
    
    public OrderController(IGenericRepository<Order> repository)
    {
        _repository = repository;
    }
    
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _repository.Get(id);
        return Ok(order);
    } // DbContext disposed at end of request
}
```

### 4. Handle Concurrency

✅ **DO:**
```csharp
// Add rowversion/timestamp column to entity
public class Product : EntityBase
{
    public string Name { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; }
}

// Handle concurrency exceptions
try
{
    await _repository.Update(product);
}
catch (DbUpdateConcurrencyException)
{
    // Handle conflict - refresh, retry, or notify user
}
```

## Performance Best Practices

### 1. Use Filtering at Database Level

✅ **DO:**
```csharp
// Filter in database
var activeCustomers = await repository.Get(
    filter: c => c.IsActive
);
```

❌ **DON'T:**
```csharp
// Get all, then filter in memory
var allCustomers = await repository.Get();
var activeCustomers = allCustomers.Where(c => c.IsActive).ToList();
```

### 2. Implement Pagination for Large Result Sets

✅ **DO:**
```csharp
public async Task<PagedResult<Product>> GetProducts(int page, int pageSize)
{
    var allProducts = await repository.Get(
        orderBy: q => q.OrderBy(p => p.Name)
    );
    
    var items = allProducts
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();
    
    return new PagedResult<Product>
    {
        Items = items,
        TotalCount = allProducts.Count(),
        Page = page,
        PageSize = pageSize
    };
}
```

### 3. Consider Caching for Frequently Accessed Data

✅ **DO:**
```csharp
public class CachedProductRepository
{
    private readonly IGenericRepository<Product> _repository;
    private readonly IMemoryCache _cache;
    
    public async Task<Product> GetProduct(int id)
    {
        return await _cache.GetOrCreateAsync($"product_{id}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await _repository.Get(id);
        });
    }
}
```

### 4. Use AsNoTracking for Read-Only Operations (EF Core)

While the current repository doesn't expose this, you can extend it:

```csharp
public class ReadOnlyRepository<TEntity, TContext> : EntityFrameworkCoreRepository<TEntity, TContext>
    where TEntity : class
    where TContext : DbContext
{
    public ReadOnlyRepository(TContext context) : base(context) { }
    
    public override async Task<IEnumerable<TEntity>> Get(
        Expression<Func<TEntity, bool>> filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        string includeProperties = "")
    {
        IQueryable<TEntity> query = dbSet.AsNoTracking(); // More efficient for reads
        
        // ... rest of implementation
    }
}
```

## Testing Best Practices

### 1. Use In-Memory Repository for Unit Tests

✅ **DO:**
```csharp
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
    public async Task GetCustomer_ExistingId_ReturnsCustomer()
    {
        // Arrange
        var customer = await _repository.Insert(new Customer { Name = "Test" });
        
        // Act
        var result = await _service.GetCustomer(customer.ID);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test", result.Name);
    }
}
```

### 2. Use EF Core In-Memory Database for Integration Tests

✅ **DO:**
```csharp
[TestClass]
public class OrderRepositoryIntegrationTests
{
    private ApplicationDbContext _context;
    private IGenericRepository<Order> _repository;
    
    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        
        _context = new ApplicationDbContext(options);
        _repository = new EntityFrameworkCoreRepository<Order, ApplicationDbContext>(_context);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        _context?.Dispose();
    }
}
```

### 3. Test Error Scenarios

✅ **DO:**
```csharp
[TestMethod]
[ExpectedException(typeof(ArgumentNullException))]
public async Task Insert_NullEntity_ThrowsException()
{
    await _repository.Insert(null);
}

[TestMethod]
public async Task Get_NonExistentId_ReturnsNull()
{
    var result = await _repository.Get(999);
    Assert.IsNull(result);
}
```

## Security Best Practices

### 1. Validate and Sanitize User Input

✅ **DO:**
```csharp
public async Task<IEnumerable<Product>> SearchProducts(string userInput)
{
    // Validate
    if (string.IsNullOrWhiteSpace(userInput))
        return Enumerable.Empty<Product>();
    
    // Sanitize
    var sanitized = userInput.Trim().ToLower();
    
    // Use parameterized query (LINQ does this automatically)
    return await _repository.Get(
        filter: p => p.Name.ToLower().Contains(sanitized)
    );
}
```

❌ **DON'T:**
```csharp
// Don't build raw SQL from user input
public async Task SearchProducts(string userInput)
{
    // This is just an example - the repository doesn't support raw SQL
    // but this pattern should be avoided in general
    await _context.Database.ExecuteSqlRawAsync(
        $"SELECT * FROM Products WHERE Name LIKE '%{userInput}%'" // SQL injection risk!
    );
}
```

### 2. Implement Authorization Checks

✅ **DO:**
```csharp
public async Task<Order> GetOrder(int orderId, int currentUserId)
{
    var order = await _repository.Get(orderId);
    
    if (order == null)
        throw new NotFoundException("Order not found");
    
    if (order.UserId != currentUserId)
        throw new UnauthorizedException("You don't have access to this order");
    
    return order;
}
```

### 3. Never Expose Repository Directly to Clients

✅ **DO:**
```csharp
// Service layer with business logic
public class CustomerService
{
    private readonly IGenericRepository<Customer> _repository;
    
    public async Task<CustomerDto> GetCustomer(int id, int requestingUserId)
    {
        // Authorization
        if (!await CanAccessCustomer(id, requestingUserId))
            throw new UnauthorizedException();
        
        // Business logic
        var customer = await _repository.Get(id);
        
        // Transform to DTO
        return MapToDto(customer);
    }
}
```

❌ **DON'T:**
```csharp
// Controller directly using repository
public class CustomerController : ControllerBase
{
    private readonly IGenericRepository<Customer> _repository;
    
    [HttpGet("{id}")]
    public async Task<Customer> Get(int id)
    {
        return await _repository.Get(id); // No business logic, no authorization
    }
}
```

## Architecture Best Practices

### 1. Use Service Layer

✅ **DO:**
```
Controller/API -> Service Layer -> Repository -> Database
```

```csharp
// Controller
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    
    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderDto dto)
    {
        var order = await _orderService.CreateOrder(dto);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }
}

// Service
public class OrderService : IOrderService
{
    private readonly IGenericRepository<Order> _orderRepository;
    private readonly IGenericRepository<Product> _productRepository;
    
    public async Task<Order> CreateOrder(CreateOrderDto dto)
    {
        // Validation
        await ValidateOrder(dto);
        
        // Business logic
        var order = MapToEntity(dto);
        order.CreatedAt = DateTime.UtcNow;
        
        // Repository
        return await _orderRepository.Insert(order);
    }
}
```

### 2. Use Unit of Work Pattern for Transactions

✅ **DO:**
```csharp
public class UnitOfWork : IDisposable
{
    private readonly ApplicationDbContext _context;
    
    public IGenericRepository<Customer> Customers { get; }
    public IGenericRepository<Order> Orders { get; }
    
    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Customers = new EntityFrameworkCoreRepository<Customer, ApplicationDbContext>(context);
        Orders = new EntityFrameworkCoreRepository<Order, ApplicationDbContext>(context);
    }
    
    public async Task<int> CommitAsync()
    {
        return await _context.SaveChangesAsync();
    }
    
    public void Dispose()
    {
        _context?.Dispose();
    }
}

// Usage
public async Task TransferOrder(int orderId, int newCustomerId)
{
    using var uow = new UnitOfWork(_contextFactory.CreateDbContext());
    
    var order = await uow.Orders.Get(orderId);
    var customer = await uow.Customers.Get(newCustomerId);
    
    order.CustomerId = newCustomerId;
    await uow.Orders.Update(order);
    
    customer.TotalOrders++;
    await uow.Customers.Update(customer);
    
    await uow.CommitAsync(); // Single transaction
}
```

### 3. Separate Read and Write Models (CQRS Lite)

For complex applications:

```csharp
public interface ICustomerReadRepository
{
    Task<CustomerSummary> GetSummary(int id);
    Task<IEnumerable<CustomerListItem>> GetList(CustomerFilter filter);
}

public interface ICustomerWriteRepository : IGenericRepository<Customer>
{
    // Additional write-specific methods
}
```

## Error Handling Best Practices

### 1. Use Specific Exception Types

✅ **DO:**
```csharp
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

public async Task<Customer> GetCustomer(int id)
{
    var customer = await _repository.Get(id);
    if (customer == null)
        throw new NotFoundException($"Customer {id} not found");
    return customer;
}
```

### 2. Log Exceptions

✅ **DO:**
```csharp
public async Task<Customer> GetCustomer(int id)
{
    try
    {
        _logger.LogInformation("Getting customer {CustomerId}", id);
        var customer = await _repository.Get(id);
        
        if (customer == null)
        {
            _logger.LogWarning("Customer {CustomerId} not found", id);
            throw new NotFoundException($"Customer {id} not found");
        }
        
        return customer;
    }
    catch (Exception ex) when (!(ex is NotFoundException))
    {
        _logger.LogError(ex, "Error getting customer {CustomerId}", id);
        throw;
    }
}
```

## Maintenance Best Practices

### 1. Keep Entities Simple

✅ **DO:**
```csharp
public class Customer : EntityBase
{
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public List<Order> Orders { get; set; }
}
```

❌ **DON'T:**
```csharp
public class Customer : EntityBase
{
    public string Name { get; set; }
    
    // Business logic in entity
    public decimal CalculateLifetimeValue() { ... }
    public void SendWelcomeEmail() { ... }
}
```

### 2. Document Complex Queries

✅ **DO:**
```csharp
/// <summary>
/// Gets high-value customers with recent orders for marketing campaign.
/// High-value defined as: total order value > $10,000 in last year.
/// Recent defined as: at least one order in last 30 days.
/// </summary>
public async Task<IEnumerable<Customer>> GetHighValueRecentCustomers()
{
    var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
    var oneYearAgo = DateTime.UtcNow.AddYears(-1);
    
    return await _repository.Get(
        filter: c => c.Orders.Any(o => o.OrderDate >= thirtyDaysAgo) &&
                     c.Orders.Where(o => o.OrderDate >= oneYearAgo)
                               .Sum(o => o.TotalAmount) > 10000,
        includeProperties: "Orders"
    );
}
```

### 3. Version Your Entities

For evolving schemas:

```csharp
public class Customer : EntityBase
{
    public string Name { get; set; }
    public string Email { get; set; }
    
    // New properties with defaults for backward compatibility
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsVerified { get; set; } = false;
}
```

## See Also

- [Usage Examples](./usage-examples.md)
- [Architecture Documentation](./architecture.md)
- [API Reference](./api-reference.md)
