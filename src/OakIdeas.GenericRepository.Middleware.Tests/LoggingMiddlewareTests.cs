using Microsoft.VisualStudio.TestTools.UnitTesting;
using OakIdeas.GenericRepository.Middleware;
using OakIdeas.GenericRepository.Middleware.Standard;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Middleware.Tests;

[TestClass]
public class LoggingMiddlewareTests
{
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [TestMethod]
    public async Task LoggingMiddleware_LogsStartAndCompletion()
    {
        // Arrange
        var logs = new List<string>();
        var middleware = new LoggingMiddleware<TestEntity, int>(msg => logs.Add(msg), includeTimings: false);

        var context = new RepositoryContext<TestEntity, int>
        {
            Operation = RepositoryOperation.Insert,
            Success = true
        };

        // Act
        await middleware.InvokeAsync(context, async ctx => await Task.CompletedTask);

        // Assert
        Assert.AreEqual(2, logs.Count);
        Assert.IsTrue(logs[0].Contains("Starting Insert operation on TestEntity"));
        Assert.IsTrue(logs[1].Contains("Completed Insert operation on TestEntity"));
        Assert.IsTrue(logs[1].Contains("Success: True"));
    }

    [TestMethod]
    public async Task LoggingMiddleware_LogsWithTimings()
    {
        // Arrange
        var logs = new List<string>();
        var middleware = new LoggingMiddleware<TestEntity, int>(msg => logs.Add(msg), includeTimings: true);

        var context = new RepositoryContext<TestEntity, int>
        {
            Operation = RepositoryOperation.Get
        };

        // Act
        await middleware.InvokeAsync(context, async ctx =>
        {
            await Task.Delay(10); // Small delay to ensure timing is captured
        });

        // Assert
        Assert.AreEqual(2, logs.Count);
        Assert.IsTrue(logs[1].Contains("ms"));
    }

    [TestMethod]
    public async Task LoggingMiddleware_LogsErrors()
    {
        // Arrange
        var logs = new List<string>();
        var middleware = new LoggingMiddleware<TestEntity, int>(msg => logs.Add(msg), includeTimings: true);

        var context = new RepositoryContext<TestEntity, int>
        {
            Operation = RepositoryOperation.Delete
        };

        // Act & Assert
        try
        {
            await middleware.InvokeAsync(context, async ctx =>
            {
                await Task.CompletedTask;
                throw new InvalidOperationException("Test error");
            });
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException ex) when (ex.Message == "Test error")
        {
            // Expected
        }

        Assert.AreEqual(2, logs.Count);
        Assert.IsTrue(logs[1].Contains("Failed Delete operation"));
        Assert.IsTrue(logs[1].Contains("Test error"));
    }

    [TestMethod]
    public void LoggingMiddleware_ThrowsOnNullLogger()
    {
        // Act & Assert
        try
        {
            new LoggingMiddleware<TestEntity, int>(null!);
            Assert.Fail("Expected ArgumentNullException was not thrown");
        }
        catch (ArgumentNullException)
        {
            // Expected
        }
    }
}
