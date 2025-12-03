using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace OakIdeas.GenericRepository.Core;

/// <summary>
/// Base repository class that provides common functionality for all repository implementations.
/// Contains shared utilities, validation logic, and middleware pipeline support.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The type of the primary key</typeparam>
public abstract class CoreRepository<TEntity, TKey> where TEntity : class where TKey : notnull
{
    /// <summary>
    /// Gets the repository options.
    /// </summary>
    protected RepositoryOptions Options { get; }

    /// <summary>
    /// Initializes a new instance of the CoreRepository class.
    /// </summary>
    /// <param name="options">Optional repository options</param>
    protected CoreRepository(RepositoryOptions? options = null)
    {
        Options = options ?? new RepositoryOptions();
    }

    /// <summary>
    /// Throws ArgumentNullException if the argument is null.
    /// Provides a centralized null checking mechanism with caller argument expression.
    /// </summary>
    /// <param name="argument">The argument to check</param>
    /// <param name="paramName">The parameter name (automatically captured)</param>
    /// <exception cref="ArgumentNullException">Thrown when argument is null</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void ThrowIfNull(object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    /// <summary>
    /// Validates an entity before performing an operation.
    /// Can be overridden by derived classes to provide custom validation logic.
    /// </summary>
    /// <param name="entity">The entity to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when entity is null</exception>
    protected virtual void ValidateEntity(TEntity entity)
    {
        ThrowIfNull(entity);
    }

    /// <summary>
    /// Combines two filter expressions using AND logic.
    /// Useful for combining user filters with system filters (e.g., soft delete).
    /// </summary>
    /// <param name="first">The first filter expression</param>
    /// <param name="second">The second filter expression</param>
    /// <returns>A combined filter expression, or null if both are null</returns>
    protected Expression<Func<TEntity, bool>>? CombineFilters(
        Expression<Func<TEntity, bool>>? first,
        Expression<Func<TEntity, bool>>? second)
    {
        if (first == null)
            return second;

        if (second == null)
            return first;

        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var combined = Expression.AndAlso(
            Expression.Invoke(first, parameter),
            Expression.Invoke(second, parameter)
        );
        return Expression.Lambda<Func<TEntity, bool>>(combined, parameter);
    }

    /// <summary>
    /// Determines if the key type is an integer type.
    /// </summary>
    protected static bool IsIntKey => typeof(TKey) == typeof(int);

    /// <summary>
    /// Checks if a key has the default value for its type.
    /// </summary>
    /// <param name="key">The key to check</param>
    /// <returns>True if the key equals default value, false otherwise</returns>
    protected static bool IsDefaultKey(TKey key)
    {
        return EqualityComparer<TKey>.Default.Equals(key, default!);
    }
}
