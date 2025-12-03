using System;
using System.Linq.Expressions;

namespace OakIdeas.GenericRepository.Specifications;

/// <summary>
/// Represents a specification that encapsulates query logic and business rules.
/// </summary>
/// <typeparam name="T">The entity type to which this specification applies</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Converts the specification to a LINQ expression for use in database queries.
    /// </summary>
    /// <returns>An expression tree representing the specification criteria</returns>
    Expression<Func<T, bool>> ToExpression();

    /// <summary>
    /// Evaluates whether an entity satisfies this specification.
    /// Useful for in-memory validation and testing.
    /// </summary>
    /// <param name="entity">The entity to evaluate</param>
    /// <returns>True if the entity satisfies the specification, false otherwise</returns>
    bool IsSatisfiedBy(T entity);
}
