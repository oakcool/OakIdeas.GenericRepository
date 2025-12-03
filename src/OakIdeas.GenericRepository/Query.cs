using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace OakIdeas.GenericRepository;

/// <summary>
/// Query object that encapsulates all query parameters in a single, reusable object.
/// Provides a fluent API for building complex queries.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
public class Query<TEntity> where TEntity : class
{
    /// <summary>
    /// Gets or sets the filter expression to apply to the query.
    /// </summary>
    public Expression<Func<TEntity, bool>>? Filter { get; set; }

    /// <summary>
    /// Gets or sets the ordering function to apply to the query.
    /// </summary>
    public Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? OrderBy { get; set; }

    /// <summary>
    /// Gets the list of navigation properties to eagerly load using type-safe expressions.
    /// </summary>
    public List<Expression<Func<TEntity, object>>>? Includes { get; private set; }

    /// <summary>
    /// Gets or sets the page number for pagination (1-based).
    /// </summary>
    public int? Page { get; set; }

    /// <summary>
    /// Gets or sets the page size for pagination.
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use no-tracking queries.
    /// No-tracking queries are more efficient for read-only scenarios.
    /// </summary>
    public bool AsNoTracking { get; set; }

    /// <summary>
    /// Adds a filter expression to the query.
    /// Note: Calling this method multiple times will replace the previous filter.
    /// To combine multiple filters, use && in a single expression or use the Specification pattern.
    /// </summary>
    /// <param name="filter">The filter expression</param>
    /// <returns>This query instance for fluent chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when filter is null</exception>
    public Query<TEntity> Where(Expression<Func<TEntity, bool>> filter)
    {
        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        Filter = filter;
        return this;
    }

    /// <summary>
    /// Adds an ordering function to the query.
    /// </summary>
    /// <param name="orderBy">The ordering function</param>
    /// <returns>This query instance for fluent chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when orderBy is null</exception>
    public Query<TEntity> Sort(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy)
    {
        if (orderBy == null)
        {
            throw new ArgumentNullException(nameof(orderBy));
        }

        OrderBy = orderBy;
        return this;
    }

    /// <summary>
    /// Adds a navigation property to eagerly load.
    /// </summary>
    /// <param name="include">The navigation property expression</param>
    /// <returns>This query instance for fluent chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when include is null</exception>
    public Query<TEntity> Include(Expression<Func<TEntity, object>> include)
    {
        if (include == null)
        {
            throw new ArgumentNullException(nameof(include));
        }

        Includes ??= new List<Expression<Func<TEntity, object>>>();
        Includes.Add(include);
        return this;
    }

    /// <summary>
    /// Configures pagination for the query.
    /// </summary>
    /// <param name="page">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <returns>This query instance for fluent chaining</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when page is less than 1 or pageSize is less than 1
    /// </exception>
    public Query<TEntity> Paged(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than or equal to 1.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be greater than or equal to 1.");
        }

        Page = page;
        PageSize = pageSize;
        return this;
    }

    /// <summary>
    /// Configures the query to use no-tracking for better read-only performance.
    /// </summary>
    /// <param name="asNoTracking">Whether to use no-tracking</param>
    /// <returns>This query instance for fluent chaining</returns>
    public Query<TEntity> WithNoTracking(bool asNoTracking = true)
    {
        AsNoTracking = asNoTracking;
        return this;
    }

    /// <summary>
    /// Gets the number of items to skip based on Page and PageSize.
    /// </summary>
    public int? Skip => Page.HasValue && PageSize.HasValue 
        ? (Page.Value - 1) * PageSize.Value 
        : null;

    /// <summary>
    /// Gets the number of items to take based on PageSize.
    /// </summary>
    public int? Take => PageSize;
}
