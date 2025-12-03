using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OakIdeas.GenericRepository.Middleware.Standard;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete - testing backward compatibility

namespace OakIdeas.GenericRepository.Middleware.Tests;

public class MiddlewareRepositoryTests
{
    [Fact]
    public async Task MiddlewareRepository_WithLoggingMiddleware_LogsOperations()
    {
        // Arrange
        var logs = new List<string>();
        var innerRepository = new MemoryGenericRepository<TestEntity>();
        var loggingMiddleware = new LoggingMiddleware<TestEntity, int>(
            log => logs.Add(log),
            logPerformance: false);
        
        var repository = new MiddlewareRepository<TestEntity, int>(
            innerRepository,
            loggingMiddleware);

        // Act
        var entity = new TestEntity { Name = "Test", Value = 42 };
        await repository.Insert(entity);
        await repository.Get(entity.ID);
        await repository.Update(entity);
        await repository.Delete(entity);

        // Assert
        Assert.Contains(logs, l => l.Contains("Starting Insert"));
        Assert.Contains(logs, l => l.Contains("Completed Insert"));
        Assert.Contains(logs, l => l.Contains("Starting GetById"));
        Assert.Contains(logs, l => l.Contains("Completed GetById"));
        Assert.Contains(logs, l => l.Contains("Starting Update"));
        Assert.Contains(logs, l => l.Contains("Completed Update"));
        Assert.Contains(logs, l => l.Contains("Starting Delete"));
        Assert.Contains(logs, l => l.Contains("Completed Delete"));
    }

    [Fact]
    public async Task MiddlewareRepository_WithPerformanceMiddleware_ReportsMetrics()
    {
        // Arrange
        var metrics = new Dictionary<string, long>();
        var innerRepository = new MemoryGenericRepository<TestEntity>();
        var performanceMiddleware = new PerformanceMiddleware<TestEntity, int>(
            (operation, duration) => metrics[operation] = duration);
        
        var repository = new MiddlewareRepository<TestEntity, int>(
            innerRepository,
            performanceMiddleware);

        // Act
        var entity = new TestEntity { Name = "Test", Value = 42 };
        await repository.Insert(entity);
        await repository.Get(entity.ID);

        // Assert
        Assert.True(metrics.ContainsKey("TestEntity.Insert"));
        Assert.True(metrics.ContainsKey("TestEntity.GetById"));
        Assert.True(metrics["TestEntity.Insert"] >= 0);
        Assert.True(metrics["TestEntity.GetById"] >= 0);
    }

    [Fact]
    public async Task MiddlewareRepository_WithAuditMiddleware_CreatesAuditTrail()
    {
        // Arrange
        var auditEntries = new List<AuditEntry>();
        var innerRepository = new MemoryGenericRepository<TestEntity>();
        var auditMiddleware = new AuditMiddleware<TestEntity, int>(
            entry => auditEntries.Add(entry),
            () => "TestUser");
        
        var repository = new MiddlewareRepository<TestEntity, int>(
            innerRepository,
            auditMiddleware);

        // Act
        var entity = new TestEntity { Name = "Test", Value = 42 };
        await repository.Insert(entity);
        entity.Value = 100;
        await repository.Update(entity);
        await repository.Delete(entity);

        // Assert
        Assert.Equal(3, auditEntries.Count);
        
        var insertAudit = auditEntries[0];
        Assert.Equal("TestEntity", insertAudit.EntityType);
        Assert.Equal("Insert", insertAudit.Operation);
        Assert.Equal("TestUser", insertAudit.User);
        
        var updateAudit = auditEntries[1];
        Assert.Equal("Update", updateAudit.Operation);
        
        var deleteAudit = auditEntries[2];
        Assert.Equal("Delete", deleteAudit.Operation);
    }

    [Fact]
    public async Task MiddlewareRepository_WithMultipleMiddlewares_ExecutesInOrder()
    {
        // Arrange
        var executionOrder = new List<string>();
        var innerRepository = new MemoryGenericRepository<TestEntity>();
        
        var middleware1 = new TestMiddleware<TestEntity, int>("MW1", executionOrder);
        var middleware2 = new TestMiddleware<TestEntity, int>("MW2", executionOrder);
        
        var repository = new MiddlewareRepository<TestEntity, int>(
            innerRepository,
            middleware1,
            middleware2);

        // Act
        var entity = new TestEntity { Name = "Test", Value = 42 };
        await repository.Insert(entity);

        // Assert
        Assert.Equal(4, executionOrder.Count);
        Assert.Equal("MW1-Before", executionOrder[0]);
        Assert.Equal("MW2-Before", executionOrder[1]);
        Assert.Equal("MW2-After", executionOrder[2]);
        Assert.Equal("MW1-After", executionOrder[3]);
    }

    [Fact]
    public async Task MiddlewareRepository_WithoutMiddleware_WorksCorrectly()
    {
        // Arrange
        var innerRepository = new MemoryGenericRepository<TestEntity>();
        var repository = new MiddlewareRepository<TestEntity, int>(innerRepository);

        // Act
        var entity = new TestEntity { Name = "Test", Value = 42 };
        var inserted = await repository.Insert(entity);
        var retrieved = await repository.Get(inserted.ID);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Test", retrieved.Name);
        Assert.Equal(42, retrieved.Value);
    }

    [Fact]
    public async Task MiddlewareRepository_GetOperations_WorkThroughMiddleware()
    {
        // Arrange
        var logs = new List<string>();
        var innerRepository = new MemoryGenericRepository<TestEntity>();
        var loggingMiddleware = new LoggingMiddleware<TestEntity, int>(
            log => logs.Add(log),
            logPerformance: false);
        
        var repository = new MiddlewareRepository<TestEntity, int>(
            innerRepository,
            loggingMiddleware);

        // Act
        await repository.Insert(new TestEntity { Name = "Test1", Value = 1 });
        await repository.Insert(new TestEntity { Name = "Test2", Value = 2 });
        
        var results = await repository.Get(filter: e => e.Value > 0);

        // Assert
        Assert.Equal(2, results.Count());
        Assert.Contains(logs, l => l.Contains("Starting Get"));
        Assert.Contains(logs, l => l.Contains("Completed Get"));
    }

    [Fact]
    public async Task MiddlewareRepository_BatchOperations_WorkThroughMiddleware()
    {
        // Arrange
        var auditEntries = new List<AuditEntry>();
        var innerRepository = new MemoryGenericRepository<TestEntity>();
        var auditMiddleware = new AuditMiddleware<TestEntity, int>(
            entry => auditEntries.Add(entry));
        
        var repository = new MiddlewareRepository<TestEntity, int>(
            innerRepository,
            auditMiddleware);

        // Act
        var entities = new[]
        {
            new TestEntity { Name = "Test1", Value = 1 },
            new TestEntity { Name = "Test2", Value = 2 },
            new TestEntity { Name = "Test3", Value = 3 }
        };
        
        await repository.InsertRange(entities);
        await repository.UpdateRange(entities);
        await repository.DeleteRange(entities);

        // Assert
        Assert.Equal(3, auditEntries.Count);
        Assert.Contains(auditEntries, e => e.Operation == "InsertRange" && e.Details!.Contains("3"));
        Assert.Contains(auditEntries, e => e.Operation == "UpdateRange" && e.Details!.Contains("3"));
        Assert.Contains(auditEntries, e => e.Operation == "DeleteRange" && e.Details!.Contains("3"));
    }
}

// Helper middleware for testing execution order
public class TestMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class
{
    private readonly string _name;
    private readonly List<string> _executionOrder;

    public TestMiddleware(string name, List<string> executionOrder)
    {
        _name = name;
        _executionOrder = executionOrder;
    }

    public override async Task<TEntity> Insert(
        System.Func<Task<TEntity>> next,
        TEntity entity,
        System.Threading.CancellationToken cancellationToken = default)
    {
        _executionOrder.Add($"{_name}-Before");
        var result = await next();
        _executionOrder.Add($"{_name}-After");
        return result;
    }
}
