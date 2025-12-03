using System;
using System.Linq.Expressions;

namespace OakIdeas.GenericRepository.Specifications;

/// <summary>
/// Base class for specifications that provides common functionality and logical operators.
/// </summary>
/// <typeparam name="T">The entity type to which this specification applies</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    /// <summary>
    /// Converts the specification to a LINQ expression for use in database queries.
    /// Must be implemented by derived classes.
    /// </summary>
    /// <returns>An expression tree representing the specification criteria</returns>
    public abstract Expression<Func<T, bool>> ToExpression();

    /// <summary>
    /// Evaluates whether an entity satisfies this specification by compiling and executing the expression.
    /// </summary>
    /// <param name="entity">The entity to evaluate</param>
    /// <returns>True if the entity satisfies the specification, false otherwise</returns>
    public virtual bool IsSatisfiedBy(T entity)
    {
        var predicate = ToExpression().Compile();
        return predicate(entity);
    }

    /// <summary>
    /// Combines this specification with another using a logical AND operation.
    /// </summary>
    /// <param name="specification">The specification to combine with</param>
    /// <returns>A new specification representing the AND operation</returns>
    public Specification<T> And(Specification<T> specification)
    {
        return new AndSpecification<T>(this, specification);
    }

    /// <summary>
    /// Combines this specification with another using a logical OR operation.
    /// </summary>
    /// <param name="specification">The specification to combine with</param>
    /// <returns>A new specification representing the OR operation</returns>
    public Specification<T> Or(Specification<T> specification)
    {
        return new OrSpecification<T>(this, specification);
    }

    /// <summary>
    /// Creates a specification that represents the logical negation of this specification.
    /// </summary>
    /// <returns>A new specification representing the NOT operation</returns>
    public Specification<T> Not()
    {
        return new NotSpecification<T>(this);
    }

    /// <summary>
    /// Implicitly converts a specification to an expression for convenient usage with repository methods.
    /// </summary>
    /// <param name="specification">The specification to convert</param>
    public static implicit operator Expression<Func<T, bool>>(Specification<T> specification)
    {
        return specification.ToExpression();
    }
}
