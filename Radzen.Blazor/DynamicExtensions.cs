using Radzen;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace System.Linq.Dynamic.Core
{
    /// <summary>
    /// Class DynamicExtensions used to replace System.Linq.Dynamic.Core library.
    /// </summary>
    public static class DynamicExtensions
    {
        static readonly Func<string, Type?> typeLocator = type => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t =>
            {
                var fullName = t.FullName;
                return fullName != null && fullName.Replace("+", ".", StringComparison.Ordinal) == type;
            });

        /// <summary>
        /// Filters using the specified filter descriptors.
        /// </summary>
        public static IQueryable<T> Where<T>(
            this IQueryable<T> source,
            string predicate,
            object[]? parameters = null, object[]? otherParameters = null)
        {
            ArgumentNullException.ThrowIfNull(source);

            try
            {
                if (parameters != null && !string.IsNullOrEmpty(predicate))
                {
                    predicate = Regex.Replace(predicate, @"@(\d+)", match =>
                    {
                        int index = int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                        if (index >= parameters.Length)
                            throw new InvalidOperationException($"No parameter provided for {match.Value}");

                        return ExpressionSerializer.FormatValue(parameters[index]) ?? string.Empty;
                    });
                }

                predicate = (predicate == "true" ? "" : predicate ?? string.Empty)
                    .Replace("DateTime(", "DateTime.Parse(", StringComparison.Ordinal)
                    .Replace("DateTimeOffset(", "DateTimeOffset.Parse(", StringComparison.Ordinal)
                    .Replace("DateOnly(", "DateOnly.Parse(", StringComparison.Ordinal)
                    .Replace("Guid(", "Guid.Parse(", StringComparison.Ordinal)
                    .Replace(" = ", " == ", StringComparison.Ordinal);

                return !string.IsNullOrEmpty(predicate) ?
                    source.Where(ExpressionParser.ParsePredicate<T>(predicate, typeLocator)) : source;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid predicate: {predicate}", ex);
            }
        }

        /// <summary>
        /// Sorts the elements of a sequence in ascending or descending order according to a key.
        /// </summary>
        public static IOrderedQueryable<T> OrderBy<T>(
            this IQueryable<T> source,
            string selector,
            object[]? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(source);

            try
            {
                return QueryableExtension.OrderBy(source, selector);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid selector: {selector}.", ex);
            }
        }

        /// <summary>
        /// Projects each element of a sequence into a collection of property values.
        /// </summary>
        public static IQueryable Select<T>(this IQueryable<T> source, string selector, object[]? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (source.ElementType == typeof(object))
            {
                var elementType = source.ElementType;

                if (source.Expression is MethodCallExpression methodCall && methodCall.Method.Name == "Cast")
                {
                    elementType = methodCall.Arguments[0].Type.GetGenericArguments().FirstOrDefault() ?? typeof(object);
                }
                else if (typeof(EnumerableQuery).IsAssignableFrom(source.GetType()))
                {
                    elementType = source.FirstOrDefault()?.GetType() ?? typeof(object);
                }

                return source.Cast(elementType).Select(selector, expression => ExpressionParser.ParseLambda(expression, elementType));
            }

            return source.Select(selector, expression => ExpressionParser.ParseLambda<T>(expression));
        }

        /// <summary>
        /// Projects each element of a sequence into a collection of property values.
        /// </summary>
        public static IQueryable Select(this IQueryable source, string selector, object[]? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(selector);
            return source.Select(selector, expression => ExpressionParser.ParseLambda(expression, source.ElementType));
        }

        private static IQueryable Select(this IQueryable source, string selector, Func<string, LambdaExpression> lambdaCreator)
        {
            try
            {
                if (string.IsNullOrEmpty(selector))
                {
                    return source;
                }

                if (!selector.Contains("=>", StringComparison.Ordinal))
                {
                    var properties = selector
                        .Replace("new (", "", StringComparison.Ordinal).Replace(")", "", StringComparison.Ordinal).Replace("new {", "", StringComparison.Ordinal).Replace("}", "", StringComparison.Ordinal).Trim()
                        .Split(",", StringSplitOptions.RemoveEmptyEntries);

                    selector = string.Join(", ", properties
                        .Select(s =>
                        {
                            var parts = s.Split(" as ", StringSplitOptions.RemoveEmptyEntries);
                            var sourcePart = (parts.FirstOrDefault() ?? s).Trim();
                            var targetPart = (parts.Length > 1 ? parts.Last() : sourcePart).Trim();

                            var safeTarget = targetPart.Replace(".", "_", StringComparison.Ordinal);
                            var safeSource = sourcePart.Replace(".", "?.", StringComparison.Ordinal);

                            return $"{safeTarget} = it.{safeSource}";
                        }));
                }

                var lambda = lambdaCreator(selector.Contains("=>", StringComparison.Ordinal) ? selector : $"it => new {{ {selector} }}");

                return source.Provider.CreateQuery(Expression.Call(typeof(Queryable), nameof(Queryable.Select),
                          [source.ElementType, lambda.Body.Type], source.Expression, Expression.Quote(lambda)));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid selector: {selector}.", ex);
            }
        }
    }
}
