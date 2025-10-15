using System.Linq.Expressions;
using Microsoft.Kiota.Abstractions;

namespace Gainsway.Kiota.Testing;

/// <summary>
/// Provides extension methods for combining RequestInformation predicates.
/// </summary>
public static class RequestInformationExtensions
{
    private class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParam;
        private readonly ParameterExpression _newParam;

        public ReplaceParameterVisitor(ParameterExpression oldParam, ParameterExpression newParam)
        {
            _oldParam = oldParam;
            _newParam = newParam;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParam ? _newParam : base.VisitParameter(node);
        }
    }

    /// <summary>
    /// Combines two RequestInformation predicates using logical AND.
    /// </summary>
    /// <param name="expr1">The first predicate expression.</param>
    /// <param name="expr2">The second predicate expression.</param>
    /// <returns>A combined predicate expression that evaluates both conditions.</returns>
    public static Expression<Predicate<RequestInformation>> And(
        this Expression<Predicate<RequestInformation>> expr1,
        Expression<Predicate<RequestInformation>> expr2
    )
    {
        var parameter = Expression.Parameter(typeof(RequestInformation));

        var leftVisitor = new ReplaceParameterVisitor(expr1.Parameters[0], parameter);
        var left = leftVisitor.Visit(expr1.Body);

        var rightVisitor = new ReplaceParameterVisitor(expr2.Parameters[0], parameter);
        var right = rightVisitor.Visit(expr2.Body);

        var body = Expression.AndAlso(left!, right!);
        return Expression.Lambda<Predicate<RequestInformation>>(body, parameter);
    }
}
