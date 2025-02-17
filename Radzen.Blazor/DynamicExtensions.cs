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
                        var value = object.Equals(parameters[i], string.Empty) ? @"""""" :
                            parameters[i] == null ? @"null" :
                                parameters[i] is string ? @$"""{parameters[i].ToString().Replace("\"", "\\\"")}""" :
                                    parameters[i] is bool ? $"{parameters[i]}".ToLower() : parameters[i];

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
            return QueryableExtension.OrderBy(source, selector);
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
                    .Select(s => (s.Contains(" as ") ? s.Split(" as ").LastOrDefault().Trim() : s.Trim()) +
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
    }
}
