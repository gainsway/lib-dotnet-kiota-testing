using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Kiota.Abstractions;

namespace Gainsway.Kiota.Testing;

/// <summary>
/// Provides extension methods for combining RequestInformation predicates and accessing path parameters.
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

    /// <summary>
    /// Tries to get a path parameter value from the RequestInformation, attempting multiple naming conventions.
    /// Tries: original name, kebab-case, URL-encoded kebab-case, PascalCase.
    /// </summary>
    /// <param name="requestInfo">The request information object.</param>
    /// <param name="parameterName">The parameter name to search for (e.g., "fundId").</param>
    /// <param name="value">The parameter value if found.</param>
    /// <returns>True if the parameter was found; otherwise, false.</returns>
    /// <example>
    /// <code>
    /// // Works with any of these Kiota-generated keys: fundId, fund-id, fund%2Did
    /// if (req.TryGetPathParameter("fundId", out var id))
    /// {
    ///     return id.ToString() == expectedFundId.ToString();
    /// }
    /// </code>
    /// </example>
    public static bool TryGetPathParameter(
        this RequestInformation requestInfo,
        string parameterName,
        out object? value
    )
    {
        value = null;

        if (requestInfo?.PathParameters == null)
        {
            return false;
        }

        // Try original name
        if (requestInfo.PathParameters.TryGetValue(parameterName, out value))
        {
            return true;
        }

        // Try kebab-case
        var kebabCase = ToKebabCase(parameterName);
        if (requestInfo.PathParameters.TryGetValue(kebabCase, out value))
        {
            return true;
        }

        // Try URL-encoded kebab-case
        var encodedKebabCase = Uri.EscapeDataString(kebabCase);
        if (requestInfo.PathParameters.TryGetValue(encodedKebabCase, out value))
        {
            return true;
        }

        // Try PascalCase
        var pascalCase = ToPascalCase(parameterName);
        if (requestInfo.PathParameters.TryGetValue(pascalCase, out value))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a path parameter value from the RequestInformation, attempting multiple naming conventions.
    /// Throws a descriptive exception if the parameter is not found.
    /// </summary>
    /// <param name="requestInfo">The request information object.</param>
    /// <param name="parameterName">The parameter name to search for (e.g., "fundId").</param>
    /// <returns>The parameter value.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the parameter is not found. The exception message includes the actual parameter keys
    /// available in the request and the URL template from Kiota.
    /// </exception>
    /// <example>
    /// <code>
    /// // Works with any of these Kiota-generated keys: fundId, fund-id, fund%2Did
    /// var id = req.GetPathParameter("fundId");
    /// return id.ToString() == expectedFundId.ToString();
    /// </code>
    /// </example>
    public static object GetPathParameter(this RequestInformation requestInfo, string parameterName)
    {
        if (TryGetPathParameter(requestInfo, parameterName, out var value))
        {
            return value!;
        }

        // Build a helpful error message
        var availableKeys = requestInfo?.PathParameters?.Keys.ToList() ?? new List<string>();
        var actualUrlTemplate = requestInfo?.UrlTemplate ?? "<unknown>";

        var errorMessage = new StringBuilder();
        errorMessage.AppendLine(
            $"Path parameter '{parameterName}' not found in RequestInformation.PathParameters."
        );
        errorMessage.AppendLine();
        errorMessage.AppendLine("Tried the following naming variations:");
        errorMessage.AppendLine($"  - {parameterName} (original)");
        errorMessage.AppendLine($"  - {ToKebabCase(parameterName)} (kebab-case)");
        errorMessage.AppendLine(
            $"  - {Uri.EscapeDataString(ToKebabCase(parameterName))} (URL-encoded kebab-case)"
        );
        errorMessage.AppendLine($"  - {ToPascalCase(parameterName)} (PascalCase)");
        errorMessage.AppendLine();
        errorMessage.AppendLine($"Kiota's actual URL template: {actualUrlTemplate}");
        errorMessage.AppendLine();
        errorMessage.AppendLine("Available path parameter keys:");
        if (availableKeys.Any())
        {
            foreach (var key in availableKeys)
            {
                var decodedKey = Uri.UnescapeDataString(key);
                if (decodedKey != key)
                {
                    errorMessage.AppendLine($"  - {key} (decoded: {decodedKey})");
                }
                else
                {
                    errorMessage.AppendLine($"  - {key}");
                }
            }
        }
        else
        {
            errorMessage.AppendLine("  (none)");
        }
        errorMessage.AppendLine();
        errorMessage.AppendLine(
            "To fix this, check Kiota's generated code (e.g., *RequestBuilder.cs files) to find the exact parameter name used in the URL template."
        );

        throw new KeyNotFoundException(errorMessage.ToString());
    }

    /// <summary>
    /// Converts a camelCase or PascalCase string to kebab-case.
    /// </summary>
    /// <param name="value">The input string.</param>
    /// <returns>The kebab-case string.</returns>
    /// <example>
    /// fundId -> fund-id
    /// FundId -> fund-id
    /// activityId -> activity-id
    /// </example>
    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Insert hyphen before uppercase letters and convert to lowercase
        return Regex.Replace(value, "(?<!^)([A-Z])", "-$1", RegexOptions.Compiled).ToLower();
    }

    /// <summary>
    /// Converts a camelCase string to PascalCase.
    /// </summary>
    /// <param name="value">The input string.</param>
    /// <returns>The PascalCase string.</returns>
    /// <example>
    /// fundId -> FundId
    /// activityId -> ActivityId
    /// </example>
    private static string ToPascalCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return char.ToUpper(value[0]) + value.Substring(1);
    }
}
