using System;
using System.Linq;
using System.Threading.Tasks;

namespace OakIdeas.GenericRepository.Middleware.Standard;

/// <summary>
/// Middleware that validates entities before operations are performed.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public class ValidationMiddleware<TEntity, TKey> : IRepositoryMiddleware<TEntity, TKey> 
    where TEntity : class 
    where TKey : notnull
{
    private readonly Func<TEntity, (bool IsValid, string? ErrorMessage)>? _validator;

    /// <summary>
    /// Initializes a new instance of the ValidationMiddleware class.
    /// </summary>
    /// <param name="validator">The validation function that returns validation result and optional error message</param>
    public ValidationMiddleware(Func<TEntity, (bool IsValid, string? ErrorMessage)>? validator = null)
    {
        _validator = validator;
    }

    /// <summary>
    /// Invokes the middleware with the specified context and next delegate.
    /// </summary>
    public async Task InvokeAsync(RepositoryContext<TEntity, TKey> context, RepositoryMiddlewareDelegate<TEntity, TKey> next)
    {
        // Only validate for Insert, Update, and related operations
        if (context.Operation == RepositoryOperation.Insert ||
            context.Operation == RepositoryOperation.Update ||
            context.Operation == RepositoryOperation.InsertRange ||
            context.Operation == RepositoryOperation.UpdateRange)
        {
            if (_validator != null)
            {
                if (context.Entity != null)
                {
                    var (isValid, errorMessage) = _validator(context.Entity);
                    if (!isValid)
                    {
                        context.Success = false;
                        context.Error = new InvalidOperationException(errorMessage ?? "Entity validation failed");
                        context.ShortCircuit = true;
                        return;
                    }
                }

                if (context.Entities != null)
                {
                    foreach (var entity in context.Entities)
                    {
                        var (isValid, errorMessage) = _validator(entity);
                        if (!isValid)
                        {
                            context.Success = false;
                            context.Error = new InvalidOperationException(
                                errorMessage ?? $"Entity validation failed for one or more entities");
                            context.ShortCircuit = true;
                            return;
                        }
                    }
                }
            }

            // Basic null validation
            if (context.Entity == null && context.Entities == null)
            {
                context.Success = false;
                context.Error = new ArgumentNullException("entity", "Entity cannot be null for this operation");
                context.ShortCircuit = true;
                return;
            }
        }

        await next(context);
    }
}
