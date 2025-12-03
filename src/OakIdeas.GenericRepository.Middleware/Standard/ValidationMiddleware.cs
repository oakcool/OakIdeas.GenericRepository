using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Middleware.Standard;

/// <summary>
/// Middleware that validates entities before insert or update operations.
/// Uses DataAnnotations validation attributes.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public class ValidationMiddleware<TEntity, TKey> : RepositoryMiddlewareBase<TEntity, TKey>
    where TEntity : class
{
    private readonly bool _throwOnValidationError;

    /// <summary>
    /// Initializes a new instance of the ValidationMiddleware class.
    /// </summary>
    /// <param name="throwOnValidationError">Whether to throw an exception on validation errors</param>
    public ValidationMiddleware(bool throwOnValidationError = true)
    {
        _throwOnValidationError = throwOnValidationError;
    }

    public override async Task<TEntity> Insert(
        Func<Task<TEntity>> next,
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ValidateEntity(entity);
        return await next();
    }

    public override async Task<TEntity> Update(
        Func<Task<TEntity>> next,
        TEntity entityToUpdate,
        CancellationToken cancellationToken = default)
    {
        ValidateEntity(entityToUpdate);
        return await next();
    }

    public override async Task<IEnumerable<TEntity>> InsertRange(
        Func<Task<IEnumerable<TEntity>>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var entityList = entities?.ToList() ?? new List<TEntity>();
        foreach (var entity in entityList)
        {
            ValidateEntity(entity);
        }
        return await next();
    }

    public override async Task<IEnumerable<TEntity>> UpdateRange(
        Func<Task<IEnumerable<TEntity>>> next,
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        var entityList = entities?.ToList() ?? new List<TEntity>();
        foreach (var entity in entityList)
        {
            ValidateEntity(entity);
        }
        return await next();
    }

    private void ValidateEntity(TEntity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        var validationContext = new ValidationContext(entity);
        var validationResults = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(entity, validationContext, validationResults, true);

        if (!isValid && _throwOnValidationError)
        {
            var errors = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
            throw new ValidationException($"Entity validation failed: {errors}");
        }
    }
}
