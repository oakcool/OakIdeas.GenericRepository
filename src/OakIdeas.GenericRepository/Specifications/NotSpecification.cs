using System;
using System.Linq.Expressions;

namespace OakIdeas.GenericRepository.Specifications;

/// <summary>
/// Specification that represents the logical negation of another specification.
/// </summary>
/// <typeparam name="T">The entity type to which this specification applies</typeparam>
public class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _specification;

    /// <summary>
    /// Initializes a new instance of the NotSpecification class.
    /// </summary>
    /// <param name="specification">The specification to negate</param>
    public NotSpecification(Specification<T> specification)
    {
        _specification = specification ?? throw new ArgumentNullException(nameof(specification));
    }

    /// <summary>
    /// Converts the NOT specification to a LINQ expression.
    /// </summary>
    /// <returns>An expression tree representing the negation of the specification</returns>
    public override Expression<Func<T, bool>> ToExpression()
    {
        var expression = _specification.ToExpression();
        var parameter = Expression.Parameter(typeof(T));
        
        var body = new ParameterReplacer(parameter).Visit(expression.Body);
        var notBody = Expression.Not(body!);
        
        return Expression.Lambda<Func<T, bool>>(notBody, parameter);
    }
}
