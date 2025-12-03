using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OakIdeas.GenericRepository.Middleware.Standard;
using Xunit;

namespace OakIdeas.GenericRepository.Middleware.Tests;

public class FluentMiddlewareExtensionTests
{
    [Fact]
    public async Task Repository_WithLogging_WorksCorrectly()
    {
        // Arrange
        var logs = new List<string>();
        var repository = new MemoryGenericRepository<TestEntity>()
            .WithLogging(log => logs.Add(log), logPerformance: false);

        // Act
        var entity = new TestEntity { Name = "Test", Value = 42 };
        await repository.Insert(entity);

        // Assert
        Assert.Contains(logs, l => l.Contains("Starting Insert"));
        Assert.Contains(logs, l => l.Contains("Completed Insert"));
    }

    [Fact]
    public async Task Repository_WithValidation_RejectsInvalidEntity()
    {
        // Arrange
        var repository = new MemoryGenericRepository<ValidatedEntity>()
            .WithValidation();

        // Act & Assert
        var invalidEntity = new ValidatedEntity { Name = "", Value = 50 }; // Invalid: empty name
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
            () => repository.Insert(invalidEntity));
    }

    [Fact]
    public async Task Repository_WithMultipleMiddlewares_ExecutesInOrder()
    {
        // Arrange
        var logs = new List<string>();
        var auditEntries = new List<AuditEntry>();
        
        var repository = new MemoryGenericRepository<TestEntity>()
            .WithValidation()
            .WithAuditing(entry => auditEntries.Add(entry), () => "TestUser")
            .WithLogging(log => logs.Add(log), logPerformance: false);

        // Act
        var entity = new TestEntity { Name = "Test", Value = 42 };
        await repository.Insert(entity);

        // Assert
        Assert.NotEmpty(logs);
        Assert.Single(auditEntries);
        Assert.Equal("Insert", auditEntries[0].Operation);
        Assert.Equal("TestUser", auditEntries[0].User);
    }

    [Fact]
    public async Task Repository_WithMiddlewareOptions_WorksCorrectly()
    {
        // Arrange
        var logs = new List<string>();
        var repository = new MemoryGenericRepository<TestEntity>()
            .WithMiddleware(options => options
                .UseMiddleware(new LoggingMiddleware<TestEntity, int>(log => logs.Add(log))));

        // Act
        var entity = new TestEntity { Name = "Test", Value = 42 };
        await repository.Insert(entity);

        // Assert
        Assert.NotEmpty(logs);
    }

    [Fact]
    public async Task Repository_WithPerformanceMonitoring_ReportsMetrics()
    {
        // Arrange
        var metrics = new Dictionary<string, long>();
        var repository = new MemoryGenericRepository<TestEntity>()
            .WithPerformanceMonitoring(
                (operation, duration) => metrics[operation] = duration,
                slowOperationThresholdMs: 1000);

        // Act
        var entity = new TestEntity { Name = "Test", Value = 42 };
        await repository.Insert(entity);

        // Assert
        Assert.True(metrics.ContainsKey("TestEntity.Insert"));
        Assert.True(metrics["TestEntity.Insert"] >= 0);
    }

    [Fact]
    public async Task Repository_ChainedMiddleware_MaintainsCorrectOrder()
    {
        // Arrange
        var executionOrder = new List<string>();
        var middleware1 = new TestMiddleware<TestEntity, int>("MW1", executionOrder);
        var middleware2 = new TestMiddleware<TestEntity, int>("MW2", executionOrder);
        
        var repository = new MemoryGenericRepository<TestEntity>()
            .WithMiddleware(middleware1)
            .WithMiddleware(middleware2);

        // Act
        var entity = new TestEntity { Name = "Test", Value = 42 };
        await repository.Insert(entity);

        // Assert - MW2 wraps MW1, so MW2 executes outer layer
        Assert.Equal(4, executionOrder.Count);
        Assert.Equal("MW2-Before", executionOrder[0]);
        Assert.Equal("MW1-Before", executionOrder[1]);
        Assert.Equal("MW1-After", executionOrder[2]);
        Assert.Equal("MW2-After", executionOrder[3]);
    }

    [Fact]
    public async Task Repository_WithoutMiddleware_WorksNormally()
    {
        // Arrange
        var repository = new MemoryGenericRepository<TestEntity>();

        // Act
        var entity = new TestEntity { Name = "Test", Value = 42 };
        var inserted = await repository.Insert(entity);
        var retrieved = await repository.Get(inserted.ID);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Test", retrieved.Name);
    }
}
