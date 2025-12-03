using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using OakIdeas.GenericRepository.Middleware.Standard;
using OakIdeas.GenericRepository.Models;
using Xunit;

namespace OakIdeas.GenericRepository.Middleware.Tests;

public class ValidatedEntity : EntityBase
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(50, ErrorMessage = "Name must be at most 50 characters")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Value must be between 1 and 100")]
    public int Value { get; set; }
}

public class ValidationMiddlewareTests
{
    [Fact]
    public async Task ValidationMiddleware_ValidEntity_AllowsInsert()
    {
        // Arrange
        var innerRepository = new MemoryGenericRepository<ValidatedEntity>();
        var validationMiddleware = new ValidationMiddleware<ValidatedEntity, int>();
        var repository = new MiddlewareRepository<ValidatedEntity>(
            innerRepository,
            validationMiddleware);

        // Act
        var entity = new ValidatedEntity { Name = "Valid", Value = 50 };
        var result = await repository.Insert(entity);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Valid", result.Name);
    }

    [Fact]
    public async Task ValidationMiddleware_InvalidEntity_ThrowsValidationException()
    {
        // Arrange
        var innerRepository = new MemoryGenericRepository<ValidatedEntity>();
        var validationMiddleware = new ValidationMiddleware<ValidatedEntity, int>();
        var repository = new MiddlewareRepository<ValidatedEntity>(
            innerRepository,
            validationMiddleware);

        // Act & Assert - Missing required name
        var entity1 = new ValidatedEntity { Name = "", Value = 50 };
        await Assert.ThrowsAsync<ValidationException>(() => repository.Insert(entity1));

        // Act & Assert - Value out of range
        var entity2 = new ValidatedEntity { Name = "Test", Value = 150 };
        await Assert.ThrowsAsync<ValidationException>(() => repository.Insert(entity2));
    }

    [Fact]
    public async Task ValidationMiddleware_ValidEntity_AllowsUpdate()
    {
        // Arrange
        var innerRepository = new MemoryGenericRepository<ValidatedEntity>();
        var validationMiddleware = new ValidationMiddleware<ValidatedEntity, int>();
        var repository = new MiddlewareRepository<ValidatedEntity>(
            innerRepository,
            validationMiddleware);

        // Act
        var entity = new ValidatedEntity { Name = "Valid", Value = 50 };
        var inserted = await repository.Insert(entity);
        
        inserted.Value = 75;
        var updated = await repository.Update(inserted);

        // Assert
        Assert.Equal(75, updated.Value);
    }

    [Fact]
    public async Task ValidationMiddleware_InvalidEntity_ThrowsOnUpdate()
    {
        // Arrange
        var innerRepository = new MemoryGenericRepository<ValidatedEntity>();
        var validationMiddleware = new ValidationMiddleware<ValidatedEntity, int>();
        var repository = new MiddlewareRepository<ValidatedEntity>(
            innerRepository,
            validationMiddleware);

        // Act
        var entity = new ValidatedEntity { Name = "Valid", Value = 50 };
        var inserted = await repository.Insert(entity);
        
        inserted.Value = 200; // Out of range

        // Assert
        await Assert.ThrowsAsync<ValidationException>(() => repository.Update(inserted));
    }

    [Fact]
    public async Task ValidationMiddleware_InsertRange_ValidatesAllEntities()
    {
        // Arrange
        var innerRepository = new MemoryGenericRepository<ValidatedEntity>();
        var validationMiddleware = new ValidationMiddleware<ValidatedEntity, int>();
        var repository = new MiddlewareRepository<ValidatedEntity>(
            innerRepository,
            validationMiddleware);

        // Act - All valid
        var validEntities = new[]
        {
            new ValidatedEntity { Name = "Valid1", Value = 10 },
            new ValidatedEntity { Name = "Valid2", Value = 20 }
        };
        var results = await repository.InsertRange(validEntities);

        // Assert
        Assert.Equal(2, results.Count());

        // Act & Assert - One invalid
        var mixedEntities = new[]
        {
            new ValidatedEntity { Name = "Valid", Value = 10 },
            new ValidatedEntity { Name = "", Value = 20 } // Invalid
        };
        await Assert.ThrowsAsync<ValidationException>(() => repository.InsertRange(mixedEntities));
    }

    [Fact]
    public async Task ValidationMiddleware_UpdateRange_ValidatesAllEntities()
    {
        // Arrange
        var innerRepository = new MemoryGenericRepository<ValidatedEntity>();
        var validationMiddleware = new ValidationMiddleware<ValidatedEntity, int>();
        var repository = new MiddlewareRepository<ValidatedEntity>(
            innerRepository,
            validationMiddleware);

        var entities = new[]
        {
            new ValidatedEntity { Name = "Entity1", Value = 10 },
            new ValidatedEntity { Name = "Entity2", Value = 20 }
        };
        var inserted = await repository.InsertRange(entities);

        // Act - All valid updates
        var toUpdate = inserted.ToList();
        toUpdate[0].Value = 30;
        toUpdate[1].Value = 40;
        var updated = await repository.UpdateRange(toUpdate);

        // Assert
        Assert.Equal(2, updated.Count());

        // Act & Assert - One invalid update
        toUpdate[0].Value = 200; // Out of range
        await Assert.ThrowsAsync<ValidationException>(() => repository.UpdateRange(toUpdate));
    }
}
