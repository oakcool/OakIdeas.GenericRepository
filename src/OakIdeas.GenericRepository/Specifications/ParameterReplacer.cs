using System.Linq.Expressions;

namespace OakIdeas.GenericRepository.Specifications;

/// <summary>
/// Expression visitor that replaces parameters in an expression tree.
/// Used internally by specification combinators to merge expressions.
/// </summary>
internal class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _parameter;

    /// <summary>
    /// Initializes a new instance of the ParameterReplacer class.
    /// </summary>
    /// <param name="parameter">The parameter to use as replacement</param>
    public ParameterReplacer(ParameterExpression parameter)
    {
        _parameter = parameter;
    }

    /// <summary>
    /// Visits a parameter expression and replaces it with the specified parameter.
    /// </summary>
    /// <param name="node">The parameter expression to visit</param>
    /// <returns>The replacement parameter</returns>
    protected override Expression VisitParameter(ParameterExpression node)
    {
        return _parameter;
    }
}
