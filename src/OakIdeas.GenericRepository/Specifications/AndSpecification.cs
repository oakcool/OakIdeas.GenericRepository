using System;
using System.Linq.Expressions;

namespace OakIdeas.GenericRepository.Specifications;

/// <summary>
/// Specification that combines two specifications using a logical AND operation.
/// </summary>
/// <typeparam name="T">The entity type to which this specification applies</typeparam>
public class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    /// <summary>
    /// Initializes a new instance of the AndSpecification class.
    /// </summary>
    /// <param name="left">The left specification</param>
    /// <param name="right">The right specification</param>
    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
    }

    /// <summary>
    /// Converts the AND specification to a LINQ expression.
    /// </summary>
    /// <returns>An expression tree representing both specifications combined with AND</returns>
    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpression = _left.ToExpression();
        var rightExpression = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));
        
        var leftBody = new ParameterReplacer(parameter).Visit(leftExpression.Body);
        var rightBody = new ParameterReplacer(parameter).Visit(rightExpression.Body);

        var body = Expression.AndAlso(leftBody!, rightBody!);
        
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
