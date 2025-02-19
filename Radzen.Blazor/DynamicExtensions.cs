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
                selector = selector.Contains("=>") ? RemoveVariableReference(selector) : selector;

                return QueryableExtension.OrderBy(source, selector.Trim());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid selector: {selector}.", ex);
            }
        }

        /// <summary>
        /// Projects each element of a sequence into a collection of property values.
        /// </summary>
        public static IQueryable Select<T>(
            this IQueryable<T> source,
            string selector,
            object[] parameters = null)
        {
            try
            {
                var properties = selector
                    .Replace("new (", "").Replace(")", "").Replace("new {", "").Replace("}", "").Trim()
                    .Split(",", StringSplitOptions.RemoveEmptyEntries);

                selector = string.Join(", ", properties
                    .Select(s => (s.Contains(" as ") ? s.Split(" as ").LastOrDefault().Trim().Replace(".", "_") : s.Trim().Replace(".", "_")) +
                        " = " + $"it.{s.Split(" as ").FirstOrDefault().Replace(".", "?.").Trim()}"));

                if (string.IsNullOrEmpty(selector))
                {
                    return source;
                }

                var lambda = ExpressionParser.ParseLambda<T>($"it => new {{ {selector} }}");

                return source.Provider.CreateQuery(Expression.Call(typeof(Queryable), nameof(Queryable.Select),
                          [source.ElementType, lambda.Body.Type], source.Expression, Expression.Quote(lambda)));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid selector: {selector}.", ex);
            }
        }
        static string RemoveVariableReference(string expression)
        {
            // Regex pattern to match any variable reference in a lambda expression
            string pattern = @"^\s*\b\w+\b\s*=>\s*";  // Matches "it => " or similar

            // Remove the variable reference from the start
            expression = Regex.Replace(expression, pattern, "").Trim();

            // Remove remaining instances of the variable reference prefix (e.g., "it.")
            pattern = @"\b\w+\."; // Matches "it.", "x.", etc.
            expression = Regex.Replace(expression, pattern, "");

            return expression.Trim();
        }
    }
}
