using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.Middleware;
using OakIdeas.GenericRepository.Middleware.Standard;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Middleware.Tests;

[TestClass]
public class ValidationMiddlewareTests
{
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [TestMethod]
    public async Task ValidationMiddleware_ValidEntity_AllowsOperation()
    {
        // Arrange
        var validator = (TestEntity e) => (e.Name != null, "Name is required");
        var middleware = new ValidationMiddleware<TestEntity, int>(validator);

        var context = new RepositoryContext<TestEntity, int>
        {
            Operation = RepositoryOperation.Insert,
            Entity = new TestEntity { Name = "Valid Name" }
        };

        var nextCalled = false;

        // Act
        await middleware.InvokeAsync(context, async ctx =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        // Assert
        Assert.IsTrue(nextCalled);
        Assert.IsTrue(context.Success);
        Assert.IsNull(context.Error);
        Assert.IsFalse(context.ShortCircuit);
    }

    [TestMethod]
    public async Task ValidationMiddleware_InvalidEntity_ShortCircuits()
    {
        // Arrange
        var validator = (TestEntity e) => (e.Name != null && e.Name != "", "Name is required");
        var middleware = new ValidationMiddleware<TestEntity, int>(validator);

        var context = new RepositoryContext<TestEntity, int>
        {
            Operation = RepositoryOperation.Insert,
            Entity = new TestEntity { Name = "" }
        };

        var nextCalled = false;

        // Act
        await middleware.InvokeAsync(context, async ctx =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        // Assert
        Assert.IsFalse(nextCalled);
        Assert.IsFalse(context.Success);
        Assert.IsNotNull(context.Error);
        Assert.IsTrue(context.ShortCircuit);
        Assert.IsInstanceOfType(context.Error, typeof(InvalidOperationException));
    }

    [TestMethod]
    public async Task ValidationMiddleware_ValidatesMultipleEntities()
    {
        // Arrange
        var validator = (TestEntity e) => (e.Name != "", "Name is required");
        var middleware = new ValidationMiddleware<TestEntity, int>(validator);

        var entities = new List<TestEntity>
        {
            new TestEntity { Name = "Valid1" },
            new TestEntity { Name = "Valid2" }
        };

        var context = new RepositoryContext<TestEntity, int>
        {
            Operation = RepositoryOperation.InsertRange,
            Entities = entities
        };

        var nextCalled = false;

        // Act
        await middleware.InvokeAsync(context, async ctx =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        // Assert
        Assert.IsTrue(nextCalled);
        Assert.IsTrue(context.Success);
    }

    [TestMethod]
    public async Task ValidationMiddleware_StopsOnFirstInvalidEntity()
    {
        // Arrange
        var validator = (TestEntity e) => (e.Name != "", "Name is required");
        var middleware = new ValidationMiddleware<TestEntity, int>(validator);

        var entities = new List<TestEntity>
        {
            new TestEntity { Name = "Valid" },
            new TestEntity { Name = "" }, // Invalid
            new TestEntity { Name = "Another Valid" }
        };

        var context = new RepositoryContext<TestEntity, int>
        {
            Operation = RepositoryOperation.UpdateRange,
            Entities = entities
        };

        var nextCalled = false;

        // Act
        await middleware.InvokeAsync(context, async ctx =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        // Assert
        Assert.IsFalse(nextCalled);
        Assert.IsFalse(context.Success);
        Assert.IsTrue(context.ShortCircuit);
    }

    [TestMethod]
    public async Task ValidationMiddleware_SkipsValidationForGetOperations()
    {
        // Arrange
        var validatorCalled = false;
        var validator = (TestEntity e) =>
        {
            validatorCalled = true;
            return (false, "Should not be called");
        };
        var middleware = new ValidationMiddleware<TestEntity, int>(validator);

        var context = new RepositoryContext<TestEntity, int>
        {
            Operation = RepositoryOperation.Get
        };

        var nextCalled = false;

        // Act
        await middleware.InvokeAsync(context, async ctx =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        // Assert
        Assert.IsTrue(nextCalled);
        Assert.IsFalse(validatorCalled);
    }
}
