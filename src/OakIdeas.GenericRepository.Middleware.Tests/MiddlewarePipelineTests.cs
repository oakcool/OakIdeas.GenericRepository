using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.Middleware;
using System;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Middleware.Tests;

[TestClass]
public class MiddlewarePipelineTests
{
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestMiddleware : IRepositoryMiddleware<TestEntity, int>
    {
        public string Name { get; set; } = string.Empty;
        public Action<RepositoryContext<TestEntity, int>>? OnInvoke { get; set; }

        public async Task InvokeAsync(RepositoryContext<TestEntity, int> context, RepositoryMiddlewareDelegate<TestEntity, int> next)
        {
            OnInvoke?.Invoke(context);
            await next(context);
        }
    }

    [TestMethod]
    public async Task Pipeline_ExecutesMiddlewareInOrder()
    {
        // Arrange
        var pipeline = new MiddlewarePipeline<TestEntity, int>();
        var executionOrder = new List<string>();

        var middleware1 = new TestMiddleware 
        { 
            Name = "First",
            OnInvoke = ctx => executionOrder.Add("First-Before")
        };

        var middleware2 = new TestMiddleware 
        { 
            Name = "Second",
            OnInvoke = ctx => executionOrder.Add("Second-Before")
        };

        pipeline.Use(middleware1);
        pipeline.Use(middleware2);

        var context = new RepositoryContext<TestEntity, int>
        {
            Operation = RepositoryOperation.Get
        };

        // Act
        await pipeline.ExecuteAsync(context, async ctx =>
        {
            executionOrder.Add("Final");
            await Task.CompletedTask;
        });

        // Assert
        Assert.AreEqual(3, executionOrder.Count);
        Assert.AreEqual("First-Before", executionOrder[0]);
        Assert.AreEqual("Second-Before", executionOrder[1]);
        Assert.AreEqual("Final", executionOrder[2]);
    }

    [TestMethod]
    public async Task Pipeline_SupportsShortCircuit()
    {
        // Arrange
        var pipeline = new MiddlewarePipeline<TestEntity, int>();
        var executed = new List<string>();

        var middleware1 = new TestMiddleware 
        { 
            Name = "First",
            OnInvoke = ctx => 
            {
                executed.Add("First");
                ctx.ShortCircuit = true;
            }
        };

        var middleware2 = new TestMiddleware 
        { 
            Name = "Second",
            OnInvoke = ctx => executed.Add("Second")
        };

        pipeline.Use(middleware1);
        pipeline.Use(middleware2);

        var context = new RepositoryContext<TestEntity, int>
        {
            Operation = RepositoryOperation.Get
        };

        // Act
        await pipeline.ExecuteAsync(context, async ctx =>
        {
            executed.Add("Final");
            await Task.CompletedTask;
        });

        // Assert
        Assert.AreEqual(1, executed.Count);
        Assert.AreEqual("First", executed[0]);
        Assert.IsTrue(context.ShortCircuit);
    }

    [TestMethod]
    public async Task Pipeline_CanStoreDataInContext()
    {
        // Arrange
        var pipeline = new MiddlewarePipeline<TestEntity, int>();

        var middleware1 = new TestMiddleware 
        { 
            OnInvoke = ctx => ctx.Items["TestKey"] = "TestValue"
        };

        pipeline.Use(middleware1);

        var context = new RepositoryContext<TestEntity, int>
        {
            Operation = RepositoryOperation.Insert
        };

        // Act
        await pipeline.ExecuteAsync(context, async ctx =>
        {
            await Task.CompletedTask;
        });

        // Assert
        Assert.IsTrue(context.Items.ContainsKey("TestKey"));
        Assert.AreEqual("TestValue", context.Items["TestKey"]);
    }

    [TestMethod]
    public void Pipeline_CountReflectsMiddlewareCount()
    {
        // Arrange
        var pipeline = new MiddlewarePipeline<TestEntity, int>();

        // Act & Assert
        Assert.AreEqual(0, pipeline.Count);

        pipeline.Use(new TestMiddleware());
        Assert.AreEqual(1, pipeline.Count);

        pipeline.Use(new TestMiddleware());
        Assert.AreEqual(2, pipeline.Count);

        pipeline.Clear();
        Assert.AreEqual(0, pipeline.Count);
    }

    [TestMethod]
    public void Pipeline_UseThrowsOnNullMiddleware()
    {
        // Arrange
        var pipeline = new MiddlewarePipeline<TestEntity, int>();

        // Act & Assert
        try
        {
            pipeline.Use(null!);
            Assert.Fail("Expected ArgumentNullException was not thrown");
        }
        catch (ArgumentNullException)
        {
            // Expected
        }
    }

    [TestMethod]
    public async Task Pipeline_ExecuteThrowsOnNullContext()
    {
        // Arrange
        var pipeline = new MiddlewarePipeline<TestEntity, int>();

        // Act & Assert
        try
        {
            await pipeline.ExecuteAsync(null!, async ctx => await Task.CompletedTask);
            Assert.Fail("Expected ArgumentNullException was not thrown");
        }
        catch (ArgumentNullException)
        {
            // Expected
        }
    }

    [TestMethod]
    public async Task Pipeline_ExecuteThrowsOnNullFinalOperation()
    {
        // Arrange
        var pipeline = new MiddlewarePipeline<TestEntity, int>();
        var context = new RepositoryContext<TestEntity, int>();

        // Act & Assert
        try
        {
            await pipeline.ExecuteAsync(context, null!);
            Assert.Fail("Expected ArgumentNullException was not thrown");
        }
        catch (ArgumentNullException)
        {
            // Expected
        }
    }
}
