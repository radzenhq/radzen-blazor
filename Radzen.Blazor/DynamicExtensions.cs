using Radzen;
using System.Linq.Expressions;

namespace System.Linq.Dynamic.Core
{
    /// <summary>
    /// Class DynamicExtensions used to replace System.Linq.Dynamic.Core library.
    /// </summary>
    public static class DynamicExtensions
    {
        static readonly Func<string, Type> typeLocator = type => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.FullName.Replace("+", ".") == type);

        /// <summary>
        /// Filters using the specified filter descriptors.
        /// </summary>
        public static IQueryable<T> Where<T>(
            this IQueryable<T> source,
            string predicate,
            object[] parameters = null, object[] otherParameters = null)
        {
            try
            {
                if (parameters != null && !string.IsNullOrEmpty(predicate))
                {
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        object param = parameters[i];
                        string value = param switch
                        {
                            string s when s == string.Empty => @"""""",
                            null => "null",
                            string s => @$"""{s.Replace("\"", "\\\"")}""",
                            bool b => b.ToString().ToLower(),
                            Guid g => $"Guid.Parse(\"{g}\")",
                            DateTime dt => $"DateTime.Parse(\"{dt:yyyy-MM-ddTHH:mm:ss.fffZ}\")",
                            DateTimeOffset dto => $"DateTime.Parse(\"{dto.UtcDateTime:yyyy-MM-ddTHH:mm:ss.fffZ}\")",
                            DateOnly d => $"DateOnly.Parse(\"{d:yyy-MM-dd}\")",
                            TimeOnly t => $"TimeOnly.Parse(\"{t:HH:mm:ss}\")",
                            _ => param.ToString()
                        };

                        predicate = predicate.Replace($"@{i}", $"{value}");
                    }
                }

                predicate = (predicate == "true" ? "" : predicate)
                    .Replace("DateTime(", "DateTime.Parse(")
                    .Replace("DateTimeOffset(", "DateTimeOffset.Parse(")
                    .Replace("DateOnly(", "DateOnly.Parse(")
                    .Replace("Guid(", "Guid.Parse(")
                    .Replace(" = ", " == ");

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
            object[] parameters = null)
        {
            try
            {
                selector = $"{selector}";

                if (selector.Contains("=>"))
                {
                    var identifierName = selector.Split("=>")[0];

                    selector = selector.Replace($"{identifierName}=>", "").Trim();

                    string methodAsc = "OrderBy";
                    string methodDesc = "OrderByDescending";

                    Expression expression = source.Expression;

                    foreach (var part in selector.Split(","))
                    {
                        var lambda = ExpressionParser.ParseLambda<T>($"{identifierName.Trim()} => {part}");

                        expression = Expression.Call(
                            typeof(Queryable), part.Trim().ToLower().Contains(" desc") ? methodDesc : methodAsc,
                            new Type[] { source.ElementType, lambda.ReturnType },
                            expression, Expression.Quote(lambda));

                        methodAsc = "ThenBy";
                        methodDesc = "ThenByDescending";
                    }

                    return (IOrderedQueryable<T>)source.Provider.CreateQuery(expression);
                }

                return (IOrderedQueryable<T>)QueryableExtension.OrderBy((IQueryable)source, selector);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid selector: {selector}.", ex);
            }
        }

        /// <summary>
        /// Projects each element of a sequence into a collection of property values.
        /// </summary>
        public static IQueryable Select<T>(this IQueryable<T> source, string selector, object[] parameters = null)
        {
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
        public static IQueryable Select(this IQueryable source, string selector, object[] parameters = null)
        {
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

                if (!selector.Contains("=>"))
                {
                    var properties = selector
                        .Replace("new (", "").Replace(")", "").Replace("new {", "").Replace("}", "").Trim()
                        .Split(",", StringSplitOptions.RemoveEmptyEntries);

                    selector = string.Join(", ", properties
                        .Select(s => (s.Contains(" as ") ? s.Split(" as ").LastOrDefault().Trim().Replace(".", "_") : s.Trim().Replace(".", "_")) +
                            " = " + $"it.{s.Split(" as ").FirstOrDefault().Replace(".", "?.").Trim()}"));
                }

                var lambda = lambdaCreator(selector.Contains("=>") ? selector : $"it => new {{ {selector} }}");

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
