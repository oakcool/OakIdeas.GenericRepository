# Async Enumerable Support

## Overview

The `GetAsyncEnumerable` method provides memory-efficient streaming of large result sets from the repository. Instead of loading all entities into memory at once, this method streams entities one at a time using C# 8.0's `IAsyncEnumerable<T>` feature.

## Why Use Async Enumerable?

### Memory Efficiency
When working with large datasets, traditional methods load all results into memory:

```csharp
// ❌ Loads ALL products into memory at once (could be millions!)
var allProducts = await repository.Get();
foreach (var product in allProducts)
{
    await ProcessProduct(product);
}
```

With async enumerable, entities are streamed on-demand:

```csharp
// ✅ Streams products one at a time - memory efficient
await foreach (var product in repository.GetAsyncEnumerable())
{
    await ProcessProduct(product);
}
```

### Benefits

1. **Memory Efficient**: Process millions of records without running out of memory
2. **Start Processing Immediately**: Begin processing as soon as the first result arrives
3. **Cancellation Support**: Cancel long-running operations gracefully
4. **Scalability**: Handle large datasets in production environments
5. **Modern C#**: Leverage async streams introduced in C# 8.0

## Basic Usage

### Simple Streaming

```csharp
public class ProductService
{
    private readonly IGenericRepository<Product> _repository;

    public async Task ExportAllProducts()
    {
        var count = 0;
        await foreach (var product in _repository.GetAsyncEnumerable())
        {
            // Process each product as it arrives
            await ExportToFile(product);
            count++;
            
            if (count % 1000 == 0)
            {
                Console.WriteLine($"Exported {count} products...");
            }
        }
        
        Console.WriteLine($"Export complete. Total: {count} products");
    }
}
```

### With Filtering

```csharp
public async Task ProcessActiveProducts()
{
    // Stream only active products
    await foreach (var product in _repository.GetAsyncEnumerable(
        filter: p => p.IsActive && p.Stock > 0))
    {
        await UpdatePricing(product);
    }
}
```

### With Ordering

```csharp
public async Task ProcessProductsByPriority()
{
    // Stream products ordered by priority
    await foreach (var product in _repository.GetAsyncEnumerable(
        orderBy: q => q.OrderByDescending(p => p.Priority)
                       .ThenBy(p => p.Name)))
    {
        await ProcessProduct(product);
    }
}
```

### With Cancellation

```csharp
public async Task ProcessWithTimeout(CancellationToken cancellationToken)
{
    try
    {
        await foreach (var product in _repository.GetAsyncEnumerable(
            cancellationToken: cancellationToken))
        {
            await ProcessProduct(product);
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Processing cancelled");
    }
}

// Usage with timeout
var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
await ProcessWithTimeout(cts.Token);
```

## Real-World Examples

### Data Export

```csharp
public class CsvExporter
{
    private readonly IGenericRepository<Customer> _repository;

    public async Task ExportToCSV(string filePath)
    {
        using var writer = new StreamWriter(filePath);
        await writer.WriteLineAsync("ID,Name,Email,CreatedDate");

        await foreach (var customer in _repository.GetAsyncEnumerable(
            orderBy: q => q.OrderBy(c => c.CreatedDate)))
        {
            var line = $"{customer.ID},{customer.Name},{customer.Email},{customer.CreatedDate}";
            await writer.WriteLineAsync(line);
        }
    }
}
```

### Batch Processing with Progress Reporting

```csharp
public class EmailNotificationService
{
    private readonly IGenericRepository<User> _repository;
    private readonly IEmailService _emailService;
    private readonly ILogger _logger;

    public async Task SendNewsletterToActiveUsers(
        IProgress<int> progress, 
        CancellationToken cancellationToken)
    {
        var count = 0;
        var successCount = 0;

        await foreach (var user in _repository.GetAsyncEnumerable(
            filter: u => u.IsActive && u.SubscribedToNewsletter,
            cancellationToken: cancellationToken))
        {
            try
            {
                await _emailService.SendNewsletter(user.Email);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {user.Email}");
            }

            count++;
            if (count % 100 == 0)
            {
                progress.Report(count);
            }
        }

        _logger.LogInformation($"Newsletter sent to {successCount} of {count} users");
    }
}
```

### Data Migration

```csharp
public class DataMigrationService
{
    private readonly IGenericRepository<OldProduct> _oldRepository;
    private readonly IGenericRepository<NewProduct> _newRepository;

    public async Task MigrateProducts()
    {
        var batchSize = 100;
        var batch = new List<NewProduct>();

        await foreach (var oldProduct in _oldRepository.GetAsyncEnumerable())
        {
            // Transform old product to new format
            var newProduct = new NewProduct
            {
                Name = oldProduct.ProductName,
                Price = oldProduct.UnitPrice,
                Category = oldProduct.CategoryName
            };

            batch.Add(newProduct);

            if (batch.Count >= batchSize)
            {
                // Insert in batches for better performance
                await _newRepository.InsertRange(batch);
                batch.Clear();
            }
        }

        // Insert remaining items
        if (batch.Count > 0)
        {
            await _newRepository.InsertRange(batch);
        }
    }
}
```

### ETL Pipeline

```csharp
public class EtlPipeline
{
    private readonly IGenericRepository<RawData> _sourceRepository;
    private readonly IGenericRepository<ProcessedData> _targetRepository;

    public async Task ProcessData(CancellationToken cancellationToken)
    {
        var buffer = new List<ProcessedData>();
        const int bufferSize = 1000;

        await foreach (var rawData in _sourceRepository.GetAsyncEnumerable(
            filter: r => !r.IsProcessed,
            orderBy: q => q.OrderBy(r => r.CreatedDate),
            cancellationToken: cancellationToken))
        {
            // Extract
            var extracted = Extract(rawData);
            
            // Transform
            var transformed = Transform(extracted);
            
            // Load (buffered)
            buffer.Add(transformed);

            if (buffer.Count >= bufferSize)
            {
                await _targetRepository.InsertRange(buffer);
                buffer.Clear();
            }
        }

        if (buffer.Count > 0)
        {
            await _targetRepository.InsertRange(buffer);
        }
    }
}
```

### Search Results Pagination UI

```csharp
public class SearchController : Controller
{
    private readonly IGenericRepository<Product> _repository;

    [HttpGet]
    public IAsyncEnumerable<ProductDto> SearchProducts(
        string searchTerm,
        CancellationToken cancellationToken)
    {
        return GetProductsAsync(searchTerm, cancellationToken);
    }

    private async IAsyncEnumerable<ProductDto> GetProductsAsync(
        string searchTerm,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var product in _repository.GetAsyncEnumerable(
            filter: p => p.Name.Contains(searchTerm),
            orderBy: q => q.OrderBy(p => p.Name),
            cancellationToken: cancellationToken))
        {
            yield return new ProductDto
            {
                Id = product.ID,
                Name = product.Name,
                Price = product.Price
            };
        }
    }
}
```

## ASP.NET Core Integration

### Controller with Cancellation Token

ASP.NET Core automatically cancels the request when the client disconnects:

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IGenericRepository<Product> _repository;

    public ProductsController(IGenericRepository<Product> repository)
    {
        _repository = repository;
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportProducts(CancellationToken cancellationToken)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);

        try
        {
            await foreach (var product in _repository.GetAsyncEnumerable(
                cancellationToken: cancellationToken))
            {
                await writer.WriteLineAsync($"{product.ID},{product.Name},{product.Price}");
            }

            await writer.FlushAsync();
            stream.Position = 0;

            return File(stream, "text/csv", "products.csv");
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, "Client disconnected");
        }
    }
}
```

### Background Service

```csharp
public class DataProcessingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataProcessingService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider
                .GetRequiredService<IGenericRepository<DataQueue>>();

            await foreach (var item in repository.GetAsyncEnumerable(
                filter: i => !i.IsProcessed,
                cancellationToken: stoppingToken))
            {
                try
                {
                    await ProcessItem(item);
                    item.IsProcessed = true;
                    await repository.Update(item);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing item {item.ID}");
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

## Performance Comparison

### Memory Usage

```csharp
// Traditional approach - loads everything into memory
var stopwatch = Stopwatch.StartNew();
var allProducts = await repository.Get(); // 1 million products
var memoryBefore = GC.GetTotalMemory(false);
foreach (var product in allProducts)
{
    await ProcessProduct(product);
}
var memoryAfter = GC.GetTotalMemory(false);
stopwatch.Stop();
// Memory usage: ~500 MB, Time: 30 seconds

// Async enumerable - streams one at a time
stopwatch.Restart();
var memoryBefore = GC.GetTotalMemory(false);
await foreach (var product in repository.GetAsyncEnumerable())
{
    await ProcessProduct(product);
}
var memoryAfter = GC.GetTotalMemory(false);
stopwatch.Stop();
// Memory usage: ~50 MB, Time: 32 seconds
```

## Best Practices

### 1. Use for Large Datasets

✅ **Good**: Processing millions of records
```csharp
await foreach (var order in repository.GetAsyncEnumerable())
{
    await ProcessOrder(order);
}
```

❌ **Avoid**: Small result sets where materialization is fine
```csharp
// Overkill for 10 items
await foreach (var config in configRepository.GetAsyncEnumerable())
{
    // ...
}
```

### 2. Combine with Filtering

Always filter at the database level:

```csharp
// ✅ Efficient - database filters before streaming
await foreach (var product in repository.GetAsyncEnumerable(
    filter: p => p.Category == "Electronics" && p.Price > 100))
{
    await ProcessProduct(product);
}

// ❌ Inefficient - streams everything then filters in memory
await foreach (var product in repository.GetAsyncEnumerable())
{
    if (product.Category == "Electronics" && product.Price > 100)
    {
        await ProcessProduct(product);
    }
}
```

### 3. Handle Cancellation

Always support cancellation for long-running operations:

```csharp
public async Task ProcessData(CancellationToken cancellationToken)
{
    await foreach (var item in repository.GetAsyncEnumerable(
        cancellationToken: cancellationToken))
    {
        // Work with item
    }
}
```

### 4. Batch When Appropriate

For operations that benefit from batching (like database inserts):

```csharp
var batch = new List<ProcessedItem>();
const int batchSize = 100;

await foreach (var item in repository.GetAsyncEnumerable())
{
    var processed = ProcessItem(item);
    batch.Add(processed);

    if (batch.Count >= batchSize)
    {
        await targetRepository.InsertRange(batch);
        batch.Clear();
    }
}

if (batch.Count > 0)
{
    await targetRepository.InsertRange(batch);
}
```

### 5. Monitor Progress

For long-running operations, provide feedback:

```csharp
var processed = 0;
var startTime = DateTime.UtcNow;

await foreach (var item in repository.GetAsyncEnumerable())
{
    await ProcessItem(item);
    processed++;

    if (processed % 1000 == 0)
    {
        var elapsed = DateTime.UtcNow - startTime;
        var rate = processed / elapsed.TotalSeconds;
        _logger.LogInformation($"Processed {processed} items at {rate:F2} items/sec");
    }
}
```

## When NOT to Use Async Enumerable

1. **Small Result Sets**: If you're working with a small number of items (< 100), the overhead isn't worth it
2. **Random Access Needed**: If you need to access items by index or iterate multiple times
3. **Aggregations**: Use database aggregations instead of streaming all data
4. **In-Memory Operations**: If the data is already in memory, use regular enumerables

## Implementation Details

### Memory Repository

The in-memory repository implementation uses synchronous iteration internally but exposes it as an async enumerable:

```csharp
public async IAsyncEnumerable<TEntity> GetAsyncEnumerable(
    Expression<Func<TEntity, bool>>? filter = null,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
    string includeProperties = "",
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    IQueryable<TEntity> query = _data.Values.AsQueryable();

    if (filter is not null)
    {
        query = query.Where(filter);
    }

    var orderedQuery = orderBy is not null ? orderBy(query) : query;

    foreach (var entity in orderedQuery)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return entity;
    }
}
```

### Entity Framework Core Repository

The EF Core implementation uses native async enumerable support:

```csharp
public virtual async IAsyncEnumerable<TEntity> GetAsyncEnumerable(
    Expression<Func<TEntity, bool>>? filter = null,
    Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
    string includeProperties = "",
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    IQueryable<TEntity> query = dbSet;

    if (filter is not null)
    {
        query = query.Where(filter);
    }

    foreach (var includeProperty in includeProperties.Split([','], StringSplitOptions.RemoveEmptyEntries))
    {
        query = query.Include(includeProperty);
    }

    if (orderBy is not null)
    {
        query = orderBy(query);
    }

    await foreach (var entity in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
    {
        yield return entity;
    }
}
```

## Requirements

- C# 8.0 or later
- .NET Standard 2.0+ (with Microsoft.Bcl.AsyncInterfaces package)
- For optimal database streaming: Entity Framework Core 3.0+

## See Also

- [API Reference](api-reference.md)
- [Best Practices](best-practices.md)
- [Batch Operations](usage-examples.md#batch-operations)
- [Cancellation Token Support](usage-examples.md#cancellation-tokens)
