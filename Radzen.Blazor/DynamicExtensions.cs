using Radzen;

namespace System.Linq.Dynamic.Core
{
    /// <summary>
    /// Class DynamicExtensions used to replace System.Linq.Dynamic.Core library.
    /// </summary>
    public static class DynamicExtensions
    {
        static Func<string, Type> typeLocator = type => AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.FullName.Replace("+",".") == type);

        /// <summary>
        /// Filters using the specified filter descriptors.
        /// </summary>
        public static IQueryable<T> Where<T>(
            this IQueryable<T> source,
            string selector,
            object[] parameters = null, object[] otherParameters = null)
        {
            try
            {
                if (parameters != null)
                {
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var value = object.Equals(parameters[i], string.Empty) ? @"""""" :
                            parameters[i] == null ? @"null" :
                                parameters[i] is string ? @$"""{parameters[i].ToString().Replace("\"", "\\\"")}"""  : 
                                    parameters[i] is bool ? $"{parameters[i]}".ToLower() : parameters[i];

                        selector = selector.Replace($"@{i}", $"{value}");
                    }
                }

                selector = (selector == "true" ? "i => true" : selector)
                    .Replace("DateTime(", "DateTime.Parse(")
                    .Replace("DateTimeOffset(", "DateTimeOffset.Parse(")
                    .Replace("DateOnly(", "DateOnly.Parse(")
                    .Replace("Guid(", "Guid.Parse(")
                    .Replace(" = ", " == ");

                return !string.IsNullOrEmpty(selector) ? 
                    source.Where(ExpressionParser.Parse<T>(selector, typeLocator)) : source;
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException($"Invalid Where selector");
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
            return Radzen.QueryableExtension.OrderBy(source, selector);
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
                    .Select(s => (s.Contains(" as ") ? s.Split(" as ").LastOrDefault().Trim() : s.Trim()) + " = " + $"it.{s.Split(" as ").FirstOrDefault().Replace(".", ".").Trim()}"));

                return !string.IsNullOrEmpty(selector) ?
                   source.Select(ExpressionParser.ParseProjection<T>($"it => new {{ {selector} }}")) : source;
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException($"Invalid selector");
            }
        }
    }
}
