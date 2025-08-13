using Radzen;
using Radzen.Blazor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Radzen
{
    /// <summary>
    /// Class QueryableExtension.
    /// </summary>
    public static class QueryableExtension
    {
        static Expression notNullCheck(Expression property) => Nullable.GetUnderlyingType(property.Type) != null || property.Type == typeof(string) ?
            Expression.Coalesce(property, property.Type == typeof(string) ? Expression.Constant(string.Empty) : Expression.Constant(null, property.Type)) : property;

        /// <summary>
        /// Projects each element of a sequence into a collection of property values.
        /// </summary>
        internal static IQueryable Select(this IQueryable source, string propertyName)
        {
            var parameter = Expression.Parameter(source.ElementType, "x");

            var property = GetNestedPropertyExpression(parameter, propertyName);

            var lambda = Expression.Lambda(notNullCheck(property), parameter);

            var selectExpression = Expression.Call(typeof(Queryable),
                nameof(Queryable.Select), [source.ElementType, property.Type], source.Expression,
                    Expression.Quote(lambda));

            return source.Provider.CreateQuery(selectExpression);
        }


        /// <summary>
        /// Projects each element of a sequence to an IEnumerable and flattens the resulting sequences into one sequence.
        /// </summary>
        public static IQueryable SelectMany(this IQueryable source, string propertyName)
        {
            var parameter = Expression.Parameter(source.ElementType, "x");

            var property = GetNestedPropertyExpression(parameter, propertyName);

            var lambda = Expression.Lambda(notNullCheck(property), parameter);

            var returnElementType = property.Type.GetElementType() ??
                (property.Type.IsGenericType ? property.Type.GetGenericArguments()[0] : typeof(object));

            var enumerableType = typeof(IEnumerable<>).MakeGenericType(returnElementType);
            var delegateType = typeof(Func<,>).MakeGenericType(source.ElementType, enumerableType);
            lambda = Expression.Lambda(delegateType, lambda.Body, lambda.Parameters);

            var selectManyExpression = Expression.Call(typeof(Queryable),
                nameof(Queryable.SelectMany), [source.ElementType, returnElementType], source.Expression,
                    Expression.Quote(lambda));

            return source.Provider.CreateQuery(selectManyExpression);
        }

        /// <summary>
        /// Projects each element of a sequence to an IEnumerable and flattens the resulting sequences into one sequence.
        /// </summary>
        public static IQueryable<GroupResult> GroupByMany<T>(this IQueryable<T> source, string[] properties)
        {
            var parameter = Expression.Parameter(source.ElementType, "x");

            return GroupByMany(source,
                properties.Select(p => Expression.Lambda<Func<T, object>>(Expression.Convert(GetNestedPropertyExpression(parameter, p), typeof(object)), parameter).Compile()).ToArray(),
                    0);
        }

        private static IQueryable<GroupResult> GroupByMany<T>(IEnumerable<T> source, Func<T, object>[] lambdas, int index)
        {
            return source.GroupBy(lambdas[index]).Select(
                g => new GroupResult
                {
                    Key = g.Key,
                    Count = g.Count(),
                    Items = g,
                    Subgroups = index < lambdas.Length - 1 ? GroupByMany(g, lambdas, index + 1) : null
                }).AsQueryable();
        }

        internal static string RemoveVariableReference(string expression)
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

        /// <summary>
        /// Sorts the elements of a sequence in ascending or descending order according to a key.
        /// </summary>
        /// <returns>A <see cref="IQueryable{T}"/> whose elements are sorted according to the specified <paramref name="selector"/>.</returns>
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string selector = null)
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

            return (IOrderedQueryable<T>)OrderBy((IQueryable)source, selector);
        }

        /// <summary>
        /// Sorts the elements of a sequence in ascending or descending order according to a key.
        /// </summary>
        /// <returns>A <see cref="IQueryable"/> whose elements are sorted according to the specified <paramref name="selector"/>.</returns>
        public static IQueryable OrderBy(this IQueryable source, string selector = null)
        {
            selector = selector.Contains("=>") ? RemoveVariableReference(selector) : selector;

            var parameters = new ParameterExpression[] { Expression.Parameter(source.ElementType, "x") };

            Expression expression = source.Expression;

            string methodAsc = "OrderBy";
            string methodDesc = "OrderByDescending";
            string[] sortStrings = new string[] { "asc", "desc" }; 

            foreach (var o in (selector ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var nameAndOrder = o.Trim();
                var name = string.Join(" ", nameAndOrder.Split(' ').Where(i => !sortStrings.Contains(i.Trim()))).Trim();
                var order = nameAndOrder.Split(' ').FirstOrDefault(i => sortStrings.Contains(i.Trim())) ?? sortStrings.First();

                Expression property = !string.IsNullOrEmpty(nameAndOrder) ?
                        GetNestedPropertyExpression(parameters.FirstOrDefault(), name) : parameters.FirstOrDefault();

                expression = Expression.Call(
                    typeof(Queryable), order.Equals(sortStrings.First(), StringComparison.OrdinalIgnoreCase) ? methodAsc : methodDesc,
                    new Type[] { source.ElementType, property.Type },
                    expression, Expression.Quote(Expression.Lambda(property, parameters)));

                methodAsc = "ThenBy";
                methodDesc = "ThenByDescending";
            }

            return source.Provider.CreateQuery(expression);
        }

        /// <summary>
        /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <param name="source">The <see cref="IQueryable"/> to return the first element of.</param>
        /// <returns>default if source is empty; otherwise, the first element in source.</returns>
        public static dynamic FirstOrDefault(this IQueryable source)
        {
            return source.Provider.Execute(Expression.Call(null,
                typeof(Queryable).GetTypeInfo().GetDeclaredMethods(nameof(Queryable.FirstOrDefault)).FirstOrDefault(mi => mi.IsGenericMethod).MakeGenericMethod(source.ElementType),
                source.Expression));
        }

        /// <summary>
        /// Converts the elements of an <see cref="IQueryable"/> to the specified type.
        /// </summary>
        /// <param name="source">The <see cref="IQueryable"/> that contains the elements to be converted.</param>
        /// <param name="type">The type to convert the elements of source to.</param>
        /// <returns>An <see cref="IQueryable"/> that contains each element of the source sequence converted to the specified type.</returns>
        public static IQueryable Cast(this IQueryable source, Type type)
        {
            return source.Provider.CreateQuery(Expression.Call(null,
                typeof(Queryable).GetTypeInfo().GetDeclaredMethods(nameof(Queryable.Cast)).FirstOrDefault(mi => mi.IsGenericMethod).MakeGenericMethod(type),
                source.Expression));
        }

        /// <summary>
        /// Returns distinct elements from a sequence by using the default equality comparer to compare values.
        /// </summary>
        /// <param name="source">The sequence to remove duplicate elements from.</param>
        /// <returns>An <see cref="IQueryable"/> that contains distinct elements from the source sequence.</returns>
        public static IQueryable Distinct(this IQueryable source)
        {
            return source.Provider.CreateQuery(Expression.Call(null,
                typeof(Queryable).GetTypeInfo().GetDeclaredMethods(nameof(Queryable.Distinct)).FirstOrDefault(mi => mi.IsGenericMethod).MakeGenericMethod(source.ElementType),
                source.Expression));
        }

        /// <summary>
        /// Filters using the specified filter descriptors.
        /// </summary>
        public static IQueryable Where(
            this IQueryable source,
            IEnumerable<FilterDescriptor> filters,
            LogicalFilterOperator logicalFilterOperator,
            FilterCaseSensitivity filterCaseSensitivity)
        {
            var whereMethod = typeof(QueryableExtension)
                .GetMethods()
                .First(m => m.Name == "Where" && m.IsGenericMethodDefinition && m.GetParameters().Any(p => p.ParameterType == typeof(IEnumerable<FilterDescriptor>)))
                .MakeGenericMethod(source.ElementType);

            return (IQueryable)whereMethod.Invoke(null, new object[] { source, filters, logicalFilterOperator, filterCaseSensitivity });
        }

        /// <summary>
        /// Filters using the specified filter descriptors.
        /// </summary>
        public static IQueryable<T> Where<T>(this IQueryable<T> source, IEnumerable<FilterDescriptor> filters,
            LogicalFilterOperator logicalFilterOperator, FilterCaseSensitivity filterCaseSensitivity)
        {
            if (filters == null || !filters.Any())
                return source;

            var parameter = Expression.Parameter(typeof(T), "x");
            Expression combinedExpression = null;

            foreach (var filter in filters)
            {
                var expression = GetExpression<T>(parameter, filter, filterCaseSensitivity, filter.Type);
                if (expression == null) continue;

                combinedExpression = combinedExpression == null
                    ? expression
                    : logicalFilterOperator == LogicalFilterOperator.And ?
                        Expression.AndAlso(combinedExpression, expression) :
                            Expression.OrElse(combinedExpression, expression);
            }

            if (combinedExpression == null)
                return source;

            var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);

            return source.Where(lambda);
        }

        internal static Expression GetNestedPropertyExpression(Expression expression, string property, Type type = null)
        {
            var parts = property.Split(new char[] { '.' }, 2);
            string currentPart = parts[0];
            Expression member;

            if (expression.Type.IsGenericType && typeof(IDictionary<,>).IsAssignableFrom(expression.Type.GetGenericTypeDefinition()) ||
                typeof(IDictionary).IsAssignableFrom(expression.Type) || typeof(System.Data.DataRow).IsAssignableFrom(expression.Type))
            {
                var key = currentPart.Split('"')[1];
                var typeString = currentPart.Split('(')[0];

                var indexer = typeof(System.Data.DataRow).IsAssignableFrom(expression.Type) ? 
                    Expression.Property(expression, expression.Type.GetProperty("Item", new[] { typeof(string) }), Expression.Constant(key)) :
                        Expression.Property(expression, expression.Type.GetProperty("Item"), Expression.Constant(key));
                member = Expression.Convert(
                    indexer,
                    parts.Length > 1 ? indexer.Type : type ?? Type.GetType(typeString.EndsWith("?") ? $"System.Nullable`1[System.{typeString.TrimEnd('?')}]" : $"System.{typeString}") ?? typeof(object));
            }
            else if (currentPart.Contains("[")) // Handle array or list indexing
            {
                var indexStart = currentPart.IndexOf('[');
                var propertyName = currentPart.Substring(0, indexStart);
                var indexString = currentPart.Substring(indexStart + 1, currentPart.Length - indexStart - 2);

                member = Expression.PropertyOrField(expression, propertyName);
                if (int.TryParse(indexString, out int index))
                {
                    if (member.Type.IsArray)
                    {
                        member = Expression.ArrayIndex(member, Expression.Constant(index));
                    }
                    else if (member.Type.IsGenericType &&
                             (member.Type.GetGenericTypeDefinition() == typeof(List<>) ||
                              typeof(IList<>).IsAssignableFrom(member.Type.GetGenericTypeDefinition())))
                    {
                        var itemProperty = member.Type.GetProperty("Item");
                        if (itemProperty != null)
                        {
                            member = Expression.Property(member, itemProperty, Expression.Constant(index));
                        }
                    }
                }
                else
                {
                    throw new ArgumentException($"Invalid index format: {indexString}");
                }
            }
            else if (expression.Type.IsInterface)
            {
                member = Expression.Property(expression,
                    new[] { expression.Type }.Concat(expression.Type.GetInterfaces()).FirstOrDefault(t => t.GetProperty(currentPart) != null),
                    currentPart
                );
            }
            else
            {
                member = Expression.PropertyOrField(expression, currentPart);
            }

            if (expression.Type.IsValueType && Nullable.GetUnderlyingType(expression.Type) == null)
            {
                expression = Expression.Convert(expression, typeof(object));
            }

            return parts.Length > 1 ? GetNestedPropertyExpression(member, parts[1], type) :
                (Nullable.GetUnderlyingType(member.Type) != null || member.Type == typeof(string)) ?
                    Expression.Condition(Expression.Equal(expression, Expression.Constant(null)), Expression.Constant(null, member.Type), member) :
                    member;
        }

        internal static Expression GetExpression<T>(ParameterExpression parameter, FilterDescriptor filter, FilterCaseSensitivity filterCaseSensitivity, Type type)
        {
            Type valueType = filter.FilterValue != null ? filter.FilterValue.GetType() : null;
            var isEnumerable = valueType != null && IsEnumerable(valueType) && valueType != typeof(string);

            Type secondValueType = filter.SecondFilterValue != null ? filter.SecondFilterValue.GetType() : null;

            Expression p = GetNestedPropertyExpression(parameter, filter.Property, type);

            Expression property = GetNestedPropertyExpression(parameter, !isEnumerable && !IsEnumerable(p.Type) ? filter.FilterProperty ?? filter.Property : filter.Property, type);

            Type collectionItemType = IsEnumerable(property.Type) && property.Type.IsGenericType ? property.Type.GetGenericArguments()[0] : null;

            ParameterExpression collectionItemTypeParameter = collectionItemType != null ? Expression.Parameter(collectionItemType, "x") : null;

            if (collectionItemType != null && filter.Property != filter.FilterProperty)
            {
                property = !string.IsNullOrEmpty(filter.FilterProperty) ? GetNestedPropertyExpression(collectionItemTypeParameter, filter.FilterProperty) : collectionItemTypeParameter;

                filter.FilterOperator = filter.FilterOperator == FilterOperator.In ? FilterOperator.Contains :
                    filter.FilterOperator == FilterOperator.NotIn ? FilterOperator.DoesNotContain : filter.FilterOperator;
            }

            var isEnum = !isEnumerable && (PropertyAccess.IsEnum(property.Type) || PropertyAccess.IsNullableEnum(property.Type));
            var caseInsensitive = property.Type == typeof(string) && !isEnumerable && filterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive;

            var isEnumerableProperty = IsEnumerable(property.Type) && property.Type != typeof(string);

            var constant = Expression.Constant(caseInsensitive ?
                $"{filter.FilterValue}".ToLowerInvariant() :
                    isEnum && !isEnumerable && filter.FilterValue != null ? Enum.ToObject(Nullable.GetUnderlyingType(property.Type) ?? property.Type, filter.FilterValue) : filter.FilterValue,
                    !isEnum && isEnumerable ? valueType : isEnumerableProperty ? valueType: property.Type);

            if (caseInsensitive && !isEnumerable)
            {
                property = Expression.Call(notNullCheck(property), typeof(string).GetMethod("ToLower", System.Type.EmptyTypes));
            }

            var secondConstant = filter.SecondFilterValue != null ?
                Expression.Constant(caseInsensitive ?
                $"{filter.SecondFilterValue}".ToLowerInvariant() :
                    isEnum && filter.SecondFilterValue != null ? Enum.ToObject(Nullable.GetUnderlyingType(property.Type) ?? property.Type, filter.SecondFilterValue) : filter.SecondFilterValue,
                    secondValueType != null && !isEnum && IsEnumerable(secondValueType) ? secondValueType : property.Type) : null;

            Expression primaryExpression = filter.FilterOperator switch
            {
                FilterOperator.Equals => Expression.Equal(notNullCheck(property), constant),
                FilterOperator.NotEquals => Expression.NotEqual(notNullCheck(property), constant),
                FilterOperator.LessThan => Expression.LessThan(notNullCheck(property), constant),
                FilterOperator.LessThanOrEquals => Expression.LessThanOrEqual(notNullCheck(property), constant),
                FilterOperator.GreaterThan => Expression.GreaterThan(notNullCheck(property), constant),
                FilterOperator.GreaterThanOrEquals => Expression.GreaterThanOrEqual(notNullCheck(property), constant),
                FilterOperator.Contains => isEnumerable ?
                    Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), new Type[] { property.Type }, constant, notNullCheck(property)) :
                         isEnumerableProperty ? 
                            Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), new Type[] { collectionItemType }, notNullCheck(property), constant) :
                                Expression.Call(notNullCheck(property), typeof(string).GetMethod("Contains", new[] { typeof(string) }), constant),
                FilterOperator.In => isEnumerable &&
                                    isEnumerableProperty ?
                    Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), new Type[] { collectionItemType },
                        Expression.Call(typeof(Enumerable), nameof(Enumerable.Intersect), new Type[] { collectionItemType }, constant, notNullCheck(property))) : Expression.Constant(true),
                FilterOperator.DoesNotContain => isEnumerable ?
                    Expression.Not(Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), new Type[] { property.Type }, constant, notNullCheck(property))) :
                        isEnumerableProperty ?
                            Expression.Not(Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), new Type[] { collectionItemType }, notNullCheck(property), constant)) : 
                                Expression.Not(Expression.Call(notNullCheck(property), typeof(string).GetMethod("Contains", new[] { typeof(string) }), constant)),
                FilterOperator.NotIn => isEnumerable &&
                                    isEnumerableProperty ?
                    Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), new Type[] { collectionItemType },
                        Expression.Call(typeof(Enumerable), nameof(Enumerable.Except), new Type[] { collectionItemType }, constant, notNullCheck(property))) : Expression.Constant(true),
                FilterOperator.StartsWith => Expression.Call(notNullCheck(property), typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), constant),
                FilterOperator.EndsWith => Expression.Call(notNullCheck(property), typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), constant),
                FilterOperator.IsNull => Expression.Equal(property, Expression.Constant(null, property.Type)),
                FilterOperator.IsNotNull => Expression.NotEqual(property, Expression.Constant(null, property.Type)),
                FilterOperator.IsEmpty => Expression.Equal(property, Expression.Constant(String.Empty)),
                FilterOperator.IsNotEmpty => Expression.NotEqual(property, Expression.Constant(String.Empty)),
                _ => null
            };

            if (collectionItemType != null && primaryExpression != null &&
                !(filter.FilterOperator == FilterOperator.In || filter.FilterOperator == FilterOperator.NotIn))
            {
                primaryExpression = Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), new Type[] { collectionItemType },
                    GetNestedPropertyExpression(parameter, filter.Property), Expression.Lambda(primaryExpression, collectionItemTypeParameter));
            }

            Expression secondExpression = null;
            if (secondConstant != null)
            {
                secondExpression = filter.SecondFilterOperator switch
                {
                    FilterOperator.Equals => Expression.Equal(notNullCheck(property), secondConstant),
                    FilterOperator.NotEquals => Expression.NotEqual(notNullCheck(property), secondConstant),
                    FilterOperator.LessThan => Expression.LessThan(notNullCheck(property), secondConstant),
                    FilterOperator.LessThanOrEquals => Expression.LessThanOrEqual(notNullCheck(property), secondConstant),
                    FilterOperator.GreaterThan => Expression.GreaterThan(notNullCheck(property), secondConstant),
                    FilterOperator.GreaterThanOrEquals => Expression.GreaterThanOrEqual(notNullCheck(property), secondConstant),
                    FilterOperator.Contains => Expression.Call(notNullCheck(property), typeof(string).GetMethod("Contains", new[] { typeof(string) }), secondConstant),
                    FilterOperator.DoesNotContain => Expression.Not(Expression.Call(notNullCheck(property), property.Type.GetMethod("Contains", new[] { typeof(string) }), secondConstant)),
                    FilterOperator.StartsWith => Expression.Call(notNullCheck(property), typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), secondConstant),
                    FilterOperator.EndsWith => Expression.Call(notNullCheck(property), typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), secondConstant),
                    FilterOperator.IsNull => Expression.Equal(property, Expression.Constant(null, property.Type)),
                    FilterOperator.IsNotNull => Expression.NotEqual(property, Expression.Constant(null, property.Type)),
                    FilterOperator.IsEmpty => Expression.Equal(property, Expression.Constant(String.Empty)),
                    FilterOperator.IsNotEmpty => Expression.NotEqual(property, Expression.Constant(String.Empty)),
                    _ => null
                };
            }

            if (collectionItemType != null && secondExpression != null &&
                !(filter.SecondFilterOperator == FilterOperator.In || filter.SecondFilterOperator == FilterOperator.NotIn))
            {
                secondExpression = Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), new Type[] { collectionItemType },
                    GetNestedPropertyExpression(parameter, filter.Property), Expression.Lambda(secondExpression, collectionItemTypeParameter));
            }

            if (primaryExpression != null && secondExpression != null)
            {
                return filter.LogicalFilterOperator switch
                {
                    LogicalFilterOperator.And => Expression.AndAlso(primaryExpression, secondExpression),
                    LogicalFilterOperator.Or => Expression.OrElse(primaryExpression, secondExpression),
                    _ => primaryExpression
                };
            }

            return primaryExpression;
        }

        /// <summary>
        /// The linq filter operators
        /// </summary>
        internal static readonly IDictionary<FilterOperator, string> LinqFilterOperators = new Dictionary<FilterOperator, string>
        {
            {FilterOperator.Equals, "=="},
            {FilterOperator.NotEquals, "!="},
            {FilterOperator.LessThan, "<"},
            {FilterOperator.LessThanOrEquals, "<="},
            {FilterOperator.GreaterThan, ">"},
            {FilterOperator.GreaterThanOrEquals, ">="},
            {FilterOperator.StartsWith, "StartsWith"},
            {FilterOperator.EndsWith, "EndsWith"},
            {FilterOperator.Contains, "Contains"},
            {FilterOperator.DoesNotContain, "DoesNotContain"},
            {FilterOperator.In, "In"},
            {FilterOperator.NotIn, "NotIn"},
            {FilterOperator.IsNull, "=="},
            {FilterOperator.IsEmpty, "=="},
            {FilterOperator.IsNotNull, "!="},
            {FilterOperator.IsNotEmpty, "!="},
            {FilterOperator.Custom, ""}
        };

        /// <summary>
        /// The o data filter operators
        /// </summary>
        internal static readonly IDictionary<FilterOperator, string> ODataFilterOperators = new Dictionary<FilterOperator, string>
        {
            {FilterOperator.Equals, "eq"},
            {FilterOperator.NotEquals, "ne"},
            {FilterOperator.LessThan, "lt"},
            {FilterOperator.LessThanOrEquals, "le"},
            {FilterOperator.GreaterThan, "gt"},
            {FilterOperator.GreaterThanOrEquals, "ge"},
            {FilterOperator.StartsWith, "startswith"},
            {FilterOperator.EndsWith, "endswith"},
            {FilterOperator.Contains, "contains"},
            {FilterOperator.DoesNotContain, "contains"},
            {FilterOperator.IsNull, "eq"},
            {FilterOperator.IsEmpty, "eq"},
            {FilterOperator.IsNotNull, "ne"},
            {FilterOperator.IsNotEmpty, "ne"},
            {FilterOperator.In, "in"},
            {FilterOperator.NotIn, "in"},
            {FilterOperator.Custom, ""}
        };

        /// <summary>
        /// Converts to list.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IList.</returns>
        public static IList ToList(IQueryable query)
        {
            var genericToList = typeof(Enumerable).GetMethod("ToList")
                .MakeGenericMethod(new Type[] { query.ElementType });
            return (IList)genericToList.Invoke(null, new[] { query });
        }

        /// <summary>
        /// Converts to filterstring.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columns">The columns.</param>
        /// <returns>System.String.</returns>
        public static string ToFilterString<T>(this IEnumerable<RadzenDataGridColumn<T>> columns)
        {
            Func<RadzenDataGridColumn<T>, bool> canFilter = (c) => c.Filterable && c.FilterPropertyType != null &&
               (!(c.GetFilterValue() == null || c.GetFilterValue() as string == string.Empty)
                || c.GetFilterOperator() == FilterOperator.IsNotNull || c.GetFilterOperator() == FilterOperator.IsNull
                || c.GetFilterOperator() == FilterOperator.IsEmpty || c.GetFilterOperator() == FilterOperator.IsNotEmpty)
               && c.GetFilterProperty() != null;

            Func<RadzenDataGridColumn<T>, bool> canFilterCustom = (c) => c.Filterable && c.FilterPropertyType != null &&
               (c.GetFilterOperator() == FilterOperator.Custom && c.GetCustomFilterExpression() != null)
               && c.GetFilterProperty() != null;

            var columnsToFilter = columns.Where(canFilter);
            var columnsWithCustomFilter = columns.Where(canFilterCustom);

            var grid = columns.FirstOrDefault()?.Grid;
            var gridLogicalFilterOperator = grid != null ? grid.LogicalFilterOperator : LogicalFilterOperator.And;
            var gridFilterCaseSensitivity = grid != null ? grid.FilterCaseSensitivity : FilterCaseSensitivity.Default;

            var serializer = new ExpressionSerializer();
            var filterExpression = "";

            if (columnsToFilter.Any())
            {
                var filters = columnsToFilter.Select(c => new FilterDescriptor()
                {
                    Property = c.Property,
                    FilterProperty = c.FilterProperty,
                    Type = c.FilterPropertyType,
                    FilterValue = c.GetFilterValue(),
                    FilterOperator = c.GetFilterOperator(),
                    SecondFilterValue = c.GetSecondFilterValue(),
                    SecondFilterOperator = c.GetSecondFilterOperator(),
                    LogicalFilterOperator = c.GetLogicalFilterOperator()
                });

                if (filters.Any())
                {
                    var parameter = Expression.Parameter(typeof(T), columnsWithCustomFilter.Any() ? "it" : "x");
                    Expression combinedExpression = null;

                    foreach (var filter in filters)
                    {
                        var expression = GetExpression<T>(parameter, filter, gridFilterCaseSensitivity, filter.Type);
                        if (expression == null) continue;

                        combinedExpression = combinedExpression == null
                            ? expression
                            : gridLogicalFilterOperator == LogicalFilterOperator.And ?
                                Expression.AndAlso(combinedExpression, expression) :
                                    Expression.OrElse(combinedExpression, expression);
                    }

                    if (combinedExpression != null)
                    {
                        filterExpression = serializer.Serialize(Expression.Lambda<Func<T, bool>>(combinedExpression, parameter));
                    }
                }

            }

            var customFilterExpression = "";

            if (columnsWithCustomFilter.Any())
            {
                var expressions = columnsWithCustomFilter.Select(c => (c.GetCustomFilterExpression() ?? "").Replace(" or ", " || ").Replace(" and ", " && ")).Where(e => !string.IsNullOrEmpty(e)).ToList();
                customFilterExpression = string.Join($"{(gridLogicalFilterOperator == LogicalFilterOperator.And ? " && " : " || ")}", expressions);

                return !string.IsNullOrEmpty(filterExpression) && !string.IsNullOrEmpty(customFilterExpression) ?
                    $"{filterExpression} {(gridLogicalFilterOperator == LogicalFilterOperator.And ? " && " : " || ")} {customFilterExpression}" :
                        !string.IsNullOrEmpty(customFilterExpression) ? "it => " + customFilterExpression : filterExpression;
            }

            return filterExpression;
        }

        /// <summary>
        /// Converts a RadzenDataFilter to a Linq-compatibly filter string
        /// </summary>
        /// <typeparam name="T">The type that is being filtered</typeparam>
        /// <param name="dataFilter">The RadzenDataFilter component</param>
        /// <returns>A Linq-compatible filter string</returns>
        public static string ToFilterString<T>(this RadzenDataFilter<T> dataFilter)
        {
            Func<CompositeFilterDescriptor, bool> canFilter = (c) => dataFilter.properties.Where(col => col.Property == c.Property).FirstOrDefault()?.FilterPropertyType != null &&
                           (!(c.FilterValue == null || c.FilterValue as string == string.Empty)
                            || c.FilterOperator == FilterOperator.IsNotNull || c.FilterOperator == FilterOperator.IsNull
                            || c.FilterOperator == FilterOperator.IsEmpty || c.FilterOperator == FilterOperator.IsNotEmpty)
                           && c.Property != null;

            if (dataFilter.Filters.Concat(dataFilter.Filters.SelectManyRecursive(i => i.Filters ?? Enumerable.Empty<CompositeFilterDescriptor>())).Where(canFilter).Any())
            {
                var serializer = new ExpressionSerializer();

                var filterExpressions = new List<Expression>();

                var parameter = Expression.Parameter(typeof(T), "x");

                foreach (var filter in dataFilter.Filters)
                {
                    AddWhereExpression<T>(parameter, filter, ref filterExpressions, dataFilter.FilterCaseSensitivity);
                }

                Expression combinedExpression = null;

                foreach (var expression in filterExpressions)
                {
                    combinedExpression = combinedExpression == null
                        ? expression
                        : dataFilter.LogicalFilterOperator == LogicalFilterOperator.And ?
                            Expression.AndAlso(combinedExpression, expression) :
                                Expression.OrElse(combinedExpression, expression);
                }

                if (combinedExpression != null)
                {
                    return serializer.Serialize(Expression.Lambda<Func<T, bool>>(combinedExpression, parameter));
                }
            }

            return "";
        }

        /// <summary>
        /// Converts a enumerable of CompositeFilterDescriptor to a Linq-compatibly filter string
        /// </summary>
        /// <typeparam name="T">The type that is being filtered</typeparam>
        /// <param name="filters">The enumerable of CompositeFilterDescriptor </param>
        /// <param name="logicalFilterOperator">The LogicalFilterOperator</param>
        /// <param name="filterCaseSensitivity">The FilterCaseSensitivity</param>
        /// <returns>A Linq-compatible filter string</returns>
        public static string ToFilterString<T>(this IEnumerable<CompositeFilterDescriptor> filters, 
            LogicalFilterOperator logicalFilterOperator = LogicalFilterOperator.And,
            FilterCaseSensitivity filterCaseSensitivity = FilterCaseSensitivity.Default)
        {
            Func<CompositeFilterDescriptor, bool> canFilter = (c) => 
                           (!(c.FilterValue == null || c.FilterValue as string == string.Empty)
                            || c.FilterOperator == FilterOperator.IsNotNull || c.FilterOperator == FilterOperator.IsNull
                            || c.FilterOperator == FilterOperator.IsEmpty || c.FilterOperator == FilterOperator.IsNotEmpty)
                           && c.Property != null;

            if (filters.Concat(filters.SelectManyRecursive(i => i.Filters ?? Enumerable.Empty<CompositeFilterDescriptor>())).Where(canFilter).Any())
            {
                var serializer = new ExpressionSerializer();

                var filterExpressions = new List<Expression>();

                var parameter = Expression.Parameter(typeof(T), "x");

                foreach (var filter in filters)
                {
                    AddWhereExpression<T>(parameter, filter, ref filterExpressions, filterCaseSensitivity);
                }

                Expression combinedExpression = null;

                foreach (var expression in filterExpressions)
                {
                    combinedExpression = combinedExpression == null
                        ? expression
                        : logicalFilterOperator == LogicalFilterOperator.And ?
                            Expression.AndAlso(combinedExpression, expression) :
                                Expression.OrElse(combinedExpression, expression);
                }

                if (combinedExpression != null)
                {
                    return serializer.Serialize(Expression.Lambda<Func<T, bool>>(combinedExpression, parameter));
                }
            }

            return "";
        }

        /// <summary>
        /// Gets the column o data filter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="column">The column.</param>
        /// <param name="filterValue">The specific value to filter by</param>
        /// <param name="columnFilterOperator">The operator used to compare to <paramref name="filterValue"/></param>
        /// <returns>System.String.</returns>
        internal static string GetColumnODataFilter<T>(RadzenDataGridColumn<T> column, object filterValue, FilterOperator columnFilterOperator)
        {
            var property = column.GetFilterProperty().Replace('.', '/');

            var odataFilterOperator = ODataFilterOperators[columnFilterOperator];

            var value = IsEnumerable(column.FilterPropertyType) && column.FilterPropertyType != typeof(string)
                ? null
                : (string)Convert.ChangeType(filterValue is DateTimeOffset ?
                            ((DateTimeOffset)filterValue).UtcDateTime : filterValue is DateOnly ?
                                ((DateOnly)filterValue).ToString("yyy-MM-dd", CultureInfo.InvariantCulture) :
                                    filterValue, typeof(string), CultureInfo.InvariantCulture);

            if (column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive && column.FilterPropertyType == typeof(string))
            {
                property = $"tolower({property})";
            }

            if (PropertyAccess.IsEnum(column.FilterPropertyType) || PropertyAccess.IsNullableEnum(column.FilterPropertyType))
            {
                return $"{property} {odataFilterOperator} '{value}'";
            }
            else if (column.FilterPropertyType == typeof(string))
            {
                if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.Contains)
                {
                    return column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ?
                        $"contains({property}, tolower('{value}'))" :
                        $"contains({property}, '{value}')";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.DoesNotContain)
                {
                    return column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ?
                        $"not(contains({property}, tolower('{value}')))" :
                        $"not(contains({property}, '{value}'))";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.StartsWith)
                {
                    return column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ?
                        $"startswith({property}, tolower('{value}'))" :
                        $"startswith({property}, '{value}')";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.EndsWith)
                {
                    return column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ?
                        $"endswith({property}, tolower('{value}'))" :
                        $"endswith({property}, '{value}')";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.Equals)
                {
                    return column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ?
                        $"{property} eq tolower('{value}')" :
                        $"{property} eq '{value}'";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.NotEquals)
                {
                    return column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ?
                        $"{property} ne tolower('{value}')" :
                        $"{property} ne '{value}'";
                }
                else if (columnFilterOperator == FilterOperator.IsNull || columnFilterOperator == FilterOperator.IsNotNull)
                {
                    return $"{property} {odataFilterOperator} null";
                }
                else if (columnFilterOperator == FilterOperator.IsEmpty || columnFilterOperator == FilterOperator.IsNotEmpty)
                {
                    return $"{property} {odataFilterOperator} ''";
                }
            }
            else if (IsEnumerable(column.FilterPropertyType) && column.FilterPropertyType != typeof(string))
            {
                var enumerableValue = ((IEnumerable)(filterValue != null ? filterValue : Enumerable.Empty<object>())).AsQueryable();

                var enumerableValueAsString = "(" + String.Join(",",
                        (enumerableValue.ElementType == typeof(string) ? enumerableValue.Cast<string>().Select(i => $@"'{i}'").Cast<object>() : enumerableValue.Cast<object>())) + ")";

                var enumerableValueAsStringOrForAny = String.Join(" or ",
                    (enumerableValue.ElementType == typeof(string) ? enumerableValue.Cast<string>()
                        .Select(i => $@"i/{property} eq '{i}'").Cast<object>() : enumerableValue.Cast<object>().Select(i => $@"i/{property} eq {i}").Cast<object>()));

                if (enumerableValue.Cast<object>().Any() && columnFilterOperator == FilterOperator.Contains)
                {
                    return $"{property} in {enumerableValueAsString}";
                }
                else if (enumerableValue.Cast<object>().Any() && columnFilterOperator == FilterOperator.DoesNotContain)
                {
                    return $"not({property} in {enumerableValueAsString})";
                }
                else if (enumerableValue.Cast<object>().Any() && columnFilterOperator == FilterOperator.In)
                {
                    return $"{column.Property}/any(i:{enumerableValueAsStringOrForAny})";
                }
                else if (enumerableValue.Cast<object>().Any() && columnFilterOperator == FilterOperator.NotIn)
                {
                    return $"not({column.Property}/any(i: {enumerableValueAsStringOrForAny}))";
                }
            }
            else if (PropertyAccess.IsNumeric(column.FilterPropertyType))
            {
                if (columnFilterOperator == FilterOperator.IsNull || columnFilterOperator == FilterOperator.IsNotNull)
                {
                    return $"{property} {odataFilterOperator} null";
                }
                else
                {
                    return $"{property} {odataFilterOperator} {value}";
                }
            }
            else if (column.FilterPropertyType == typeof(bool) || column.FilterPropertyType == typeof(bool?))
            {
                if (columnFilterOperator == FilterOperator.IsNull || columnFilterOperator == FilterOperator.IsNotNull)
                {
                    return $"{property} {odataFilterOperator} null";
                }
                else if (columnFilterOperator == FilterOperator.IsEmpty || columnFilterOperator == FilterOperator.IsNotEmpty)
                {
                    return $"{property} {odataFilterOperator} ''";
                }
                else
                {
                    return $"{property} eq {value.ToLower()}";
                }
            }
            else if (column.FilterPropertyType == typeof(DateTime) ||
                    column.FilterPropertyType == typeof(DateTime?) ||
                    column.FilterPropertyType == typeof(DateTimeOffset) ||
                    column.FilterPropertyType == typeof(DateTimeOffset?) ||
                    column.FilterPropertyType == typeof(DateOnly) ||
                    column.FilterPropertyType == typeof(DateOnly?))
            {
                if (columnFilterOperator == FilterOperator.IsNull || columnFilterOperator == FilterOperator.IsNotNull)
                {
                    return $"{property} {odataFilterOperator} null";
                }
                else if (columnFilterOperator == FilterOperator.IsEmpty || columnFilterOperator == FilterOperator.IsNotEmpty)
                {
                    return $"{property} {odataFilterOperator} ''";
                }
                else
                {
                    return $"{property} {odataFilterOperator} {(column.FilterPropertyType == typeof(DateOnly) || column.FilterPropertyType == typeof(DateOnly?) ? value : DateTime.Parse(value, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture))}";
                }
            }
            else if (column.FilterPropertyType == typeof(Guid) || column.FilterPropertyType == typeof(Guid?))
            {
                return $"{property} {odataFilterOperator} {value}";
            }

            return "";
        }

        /// <summary>
        /// Converts to odatafilterstring.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columns">The columns.</param>
        /// <returns>System.String.</returns>
        public static string ToODataFilterString<T>(this IEnumerable<RadzenDataGridColumn<T>> columns)
        {
            var columnsWithFilter = GetFilterableColumns(columns);

            if (columnsWithFilter.Any())
            {
                var gridLogicalFilterOperator = columns.FirstOrDefault()?.Grid?.LogicalFilterOperator;
                var gridBooleanOperator = gridLogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                var whereList = new List<string>();
                foreach (var column in columnsWithFilter)
                {
                    var property = column.GetFilterProperty().Replace('.', '/');

                    var value = column.GetFilterValue();
                    var secondValue = column.GetSecondFilterValue();

                    if (column.GetFilterOperator() == FilterOperator.Custom)
                    {
                        var customFilterExpression = column.GetCustomFilterExpression();
                        if (!string.IsNullOrEmpty(customFilterExpression))
                        {
                            whereList.Add(customFilterExpression);
                        }
                    }
                    else if (value != null || column.GetFilterOperator() == FilterOperator.IsNotNull || column.GetFilterOperator() == FilterOperator.IsNull
                        || column.GetFilterOperator() == FilterOperator.IsEmpty || column.GetFilterOperator() == FilterOperator.IsNotEmpty)
                    {
                        var linqOperator = ODataFilterOperators[column.GetFilterOperator()];
                        if (linqOperator == null)
                        {
                            linqOperator = "==";
                        }

                        var booleanOperator = column.LogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                        if (secondValue == null)
                        {
                            whereList.Add(column.GetColumnODataFilter());
                        }
                        else
                        {
                            whereList.Add($"({column.GetColumnODataFilter()} {booleanOperator} {column.GetColumnODataFilter(second: true)})");
                        }
                    }
                }

                return string.Join($" {gridBooleanOperator} ", whereList.Where(i => !string.IsNullOrEmpty(i)));
            }

            return "";
        }

        /// <summary>
        /// Gets if type is IEnumerable.
        /// </summary>
        public static bool IsEnumerable(Type type)
        {
            return (typeof(IEnumerable).IsAssignableFrom(type) || typeof(IEnumerable<>).IsAssignableFrom(type)) && type != typeof(string);
        }

        /// <summary>
        /// Wheres the specified columns.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="columns">The columns.</param>
        /// <returns>IQueryable&lt;T&gt;.</returns>
        public static IQueryable<T> Where<T>(this IQueryable<T> source, IEnumerable<RadzenDataGridColumn<T>> columns)
        {
            Func<RadzenDataGridColumn<T>, bool> canFilter = (c) => c.Filterable && c.FilterPropertyType != null &&
               (!(c.GetFilterValue() == null || c.GetFilterValue() as string == string.Empty)
                || c.GetFilterOperator() == FilterOperator.IsNotNull || c.GetFilterOperator() == FilterOperator.IsNull
                || c.GetFilterOperator() == FilterOperator.IsEmpty || c.GetFilterOperator() == FilterOperator.IsNotEmpty)
               && c.GetFilterProperty() != null;

            Func<RadzenDataGridColumn<T>, bool> canFilterCustom = (c) => c.Filterable && c.FilterPropertyType != null &&
               (c.GetFilterOperator() == FilterOperator.Custom && c.GetCustomFilterExpression() != null)
               && c.GetFilterProperty() != null;

            var columnsToFilter = columns.Where(canFilter);
            var grid = columns.FirstOrDefault()?.Grid;
            var gridLogicalFilterOperator = grid != null ? grid.LogicalFilterOperator : LogicalFilterOperator.And;
            var gridFilterCaseSensitivity = grid != null ? grid.FilterCaseSensitivity : FilterCaseSensitivity.Default;

            if (columnsToFilter.Any())
            {
                source = source.Where(columnsToFilter.Select(c => new FilterDescriptor()
                {
                    Property = c.Property,
                    FilterProperty = c.FilterProperty,
                    Type = c.FilterPropertyType,
                    FilterValue = c.GetFilterValue(),
                    FilterOperator = c.GetFilterOperator(),
                    SecondFilterValue = c.GetSecondFilterValue(),
                    SecondFilterOperator = c.GetSecondFilterOperator(),
                    LogicalFilterOperator = c.GetLogicalFilterOperator()
                }), gridLogicalFilterOperator, gridFilterCaseSensitivity);
            }

            var columnsWithCustomFilter = columns.Where(canFilterCustom);

            if (columnsToFilter.Any())
            {
                var expressions = columnsWithCustomFilter.Select(c => (c.GetCustomFilterExpression() ?? "").Replace(" or ", " || ").Replace(" and ", " && ")).Where(e => !string.IsNullOrEmpty(e)).ToList();
                source = expressions.Any() ?
                    System.Linq.Dynamic.Core.DynamicExtensions.Where(source, "it => " + string.Join($"{(gridLogicalFilterOperator == LogicalFilterOperator.And ? " && " : " || ")}", expressions)) : source;
            }

            return source;
        }

        /// <summary>
        /// Wheres the specified filters.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="dataFilter">The DataFilter.</param>
        /// <returns>IQueryable&lt;T&gt;.</returns>
        public static IQueryable<T> Where<T>(this IQueryable<T> source, RadzenDataFilter<T> dataFilter)
        {
            Func<CompositeFilterDescriptor, bool> canFilter = (c) => dataFilter.properties.Where(col => col.Property == c.Property).FirstOrDefault()?.FilterPropertyType != null &&
               (!(c.FilterValue == null || c.FilterValue as string == string.Empty)
                || c.FilterOperator == FilterOperator.IsNotNull || c.FilterOperator == FilterOperator.IsNull
                || c.FilterOperator == FilterOperator.IsEmpty || c.FilterOperator == FilterOperator.IsNotEmpty)
               && c.Property != null;

            if (dataFilter.Filters.Concat(dataFilter.Filters.SelectManyRecursive(i => i.Filters ?? Enumerable.Empty<CompositeFilterDescriptor>())).Where(canFilter).Any())
            {
                var filterExpressions = new List<Expression>();

                var parameter = Expression.Parameter(typeof(T), "x");

                foreach (var filter in dataFilter.Filters)
                {
                    AddWhereExpression<T>(parameter, filter, ref filterExpressions, dataFilter.FilterCaseSensitivity);
                }

                Expression combinedExpression = null;

                foreach (var expression in filterExpressions)
                {
                    combinedExpression = combinedExpression == null
                        ? expression
                        : dataFilter.LogicalFilterOperator == LogicalFilterOperator.And ?
                            Expression.AndAlso(combinedExpression, expression) :
                                Expression.OrElse(combinedExpression, expression);
                }

                if (combinedExpression != null)
                {
                    var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
                    return source.Where(lambda);
                }
            }

            return source;
        }

        /// <summary>
        /// Wheres the specified filters.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>IQueryable&lt;T&gt;.</returns>
        public static IQueryable<T> Where<T>(this IQueryable<T> source, IEnumerable<CompositeFilterDescriptor> filters, LogicalFilterOperator logicalFilterOperator, FilterCaseSensitivity filterCaseSensitivity)
        {
            Func<CompositeFilterDescriptor, bool> canFilter = (c) =>
               (!(c.FilterValue == null || c.FilterValue as string == string.Empty)
                || c.FilterOperator == FilterOperator.IsNotNull || c.FilterOperator == FilterOperator.IsNull
                || c.FilterOperator == FilterOperator.IsEmpty || c.FilterOperator == FilterOperator.IsNotEmpty)
               && c.Property != null;

            if (filters.Where(canFilter).Any())
            {
                var filterExpressions = new List<Expression>();

                var parameter = Expression.Parameter(typeof(T), "x");

                foreach (var filter in filters)
                {
                    AddWhereExpression<T>(parameter, filter, ref filterExpressions, filterCaseSensitivity);
                }

                Expression combinedExpression = null;

                foreach (var expression in filterExpressions)
                {
                    combinedExpression = combinedExpression == null
                        ? expression
                        : logicalFilterOperator == LogicalFilterOperator.And ?
                            Expression.AndAlso(combinedExpression, expression) :
                                Expression.OrElse(combinedExpression, expression);
                }

                if (combinedExpression != null)
                {
                    var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
                    return source.Where(lambda);
                }
            }

            return source;
        }

        private static void AddWhereExpression<T>(ParameterExpression parameter, CompositeFilterDescriptor filter, ref List<Expression> filterExpressions, FilterCaseSensitivity filterCaseSensitivity)
        {
            if (filter.Filters != null)
            {
                var innerFilterExpressions = new List<Expression>();

                foreach (var f in filter.Filters)
                {
                    AddWhereExpression<T>(parameter, f, ref innerFilterExpressions, filterCaseSensitivity);
                }

                if (innerFilterExpressions.Any())
                {
                    Expression combinedExpression = null;

                    foreach (var expression in innerFilterExpressions)
                    {
                        combinedExpression = combinedExpression == null
                            ? expression
                            : filter.LogicalFilterOperator == LogicalFilterOperator.And ?
                                Expression.AndAlso(combinedExpression, expression) :
                                    Expression.OrElse(combinedExpression, expression);
                    }

                    if (combinedExpression != null)
                    {
                        filterExpressions.Add(combinedExpression);
                    }
                }
            }
            else
            {
                if (filter.Property == null || filter.FilterOperator == null || (filter.FilterValue == null &&
                    filter.FilterOperator != FilterOperator.IsNull && filter.FilterOperator != FilterOperator.IsNotNull &&
                    filter.FilterOperator != FilterOperator.IsEmpty && filter.FilterOperator != FilterOperator.IsNotEmpty))
                {
                    return;
                }

                var f = new FilterDescriptor()
                {
                    Property = filter.Property,
                    FilterProperty = filter.FilterProperty,
                    FilterValue = filter.FilterValue,
                    FilterOperator = filter.FilterOperator ?? FilterOperator.Equals,
                    LogicalFilterOperator = filter.LogicalFilterOperator,
                    Type = filter.Type
                };

                var expression = GetExpression<T>(parameter, f, filterCaseSensitivity, f.Type);
                if (expression != null)
                {
                    filterExpressions.Add(expression);
                }
            }
        }

        /// <summary>
        /// Wheres the specified filters.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <param name="op">The StringFilterOperator.</param>
        /// <param name="cs">The FilterCaseSensitivity.</param>
        /// <returns>IQueryable&lt;T&gt;.</returns>
        public static IQueryable Where(this IQueryable source, string property, string value, StringFilterOperator op, FilterCaseSensitivity cs)
        {
            IQueryable result;

            if (!string.IsNullOrEmpty(value))
            {
                var ignoreCase = cs == FilterCaseSensitivity.CaseInsensitive;
                var parameter = Expression.Parameter(source.ElementType, "it");
                var inMemory = typeof(EnumerableQuery).IsAssignableFrom(source.GetType());

                Expression propertyExpression = parameter;

                if (!string.IsNullOrEmpty(property))
                {
                    propertyExpression = GetNestedPropertyExpression(parameter, property);
                }

                if (string.IsNullOrEmpty(property) && inMemory || 
                    propertyExpression != null && propertyExpression.Type != typeof(string))
                {
                    propertyExpression = Expression.Call(string.IsNullOrEmpty(property) && inMemory ? notNullCheck(parameter) : notNullCheck(propertyExpression), "ToString", Type.EmptyTypes);
                }

                if (ignoreCase)
                {
                    propertyExpression = Expression.Call(notNullCheck(propertyExpression), "ToLower", Type.EmptyTypes);
                }

                var constantExpression = Expression.Constant(ignoreCase ? value.ToLower() : value, typeof(string));
                Expression comparisonExpression = null;

                switch (op)
                {
                    case StringFilterOperator.Contains:
                        comparisonExpression = Expression.Call(notNullCheck(propertyExpression), "Contains", null, constantExpression);
                        break;
                    case StringFilterOperator.StartsWith:
                        comparisonExpression = Expression.Call(notNullCheck(propertyExpression), "StartsWith", null, constantExpression);
                        break;
                    case StringFilterOperator.EndsWith:
                        comparisonExpression = Expression.Call(notNullCheck(propertyExpression), "EndsWith", null, constantExpression);
                        break;
                    default:
                        comparisonExpression = Expression.Equal(propertyExpression, constantExpression);
                        break;
                }

                var lambda = Expression.Lambda(comparisonExpression, parameter);
                result = source.Provider.CreateQuery(Expression.Call(
                    typeof(Queryable),
                    "Where",
                    new Type[] { source.ElementType },
                    source.Expression,
                    lambda
                ));
            }
            else
            {
                result = source;
            }

            return result;
        }

        /// <summary>
        /// Converts to OData filter expression.
        /// </summary>
        /// <param name="dataFilter">The DataFilter.</param>
        /// <returns>System.String.</returns>
        public static string ToODataFilterString<T>(this RadzenDataFilter<T> dataFilter)
        {
            Func<CompositeFilterDescriptor, bool> canFilter = (c) => dataFilter.properties.Where(col => col.Property == c.Property).FirstOrDefault()?.FilterPropertyType != null &&
               (!(c.FilterValue == null || c.FilterValue as string == string.Empty)
                || c.FilterOperator == FilterOperator.IsNotNull || c.FilterOperator == FilterOperator.IsNull
                || c.FilterOperator == FilterOperator.IsEmpty || c.FilterOperator == FilterOperator.IsNotEmpty)
               && c.Property != null;

            if (dataFilter.Filters.Concat(dataFilter.Filters.SelectManyRecursive(i => i.Filters ?? Enumerable.Empty<CompositeFilterDescriptor>())).Where(canFilter).Any())
            {
                var filterExpressions = new List<string>();

                foreach (var filter in dataFilter.Filters)
                {
                    var ft = dataFilter.properties.Where(col => col.Property == filter.Property).FirstOrDefault()?.FilterPropertyType;
                    if (ft != null && ft != filter.Type)
                    {
                        filter.Type = ft;
                    }

                    AddODataExpression<T>(canFilter, filter, ref filterExpressions, dataFilter.LogicalFilterOperator, dataFilter.FilterCaseSensitivity);
                }

                return filterExpressions.Any() ?
                    string.Join($" {dataFilter.LogicalFilterOperator.ToString().ToLower()} ", filterExpressions)
                    : "";
            }
            return "";
        }

        /// <summary>
        /// Converts a enumerable of CompositeFilterDescriptor to a OData-compatibly filter string
        /// </summary>
        /// <param name="filters">The enumerable of CompositeFilterDescriptor </param>
        /// <param name="logicalFilterOperator">The LogicalFilterOperator</param>
        /// <param name="filterCaseSensitivity">The FilterCaseSensitivity</param>
        /// <returns>A OData-compatible filter string</returns>
        public static string ToODataFilterString<T>(this IEnumerable<CompositeFilterDescriptor> filters,
            LogicalFilterOperator logicalFilterOperator = LogicalFilterOperator.And,
            FilterCaseSensitivity filterCaseSensitivity = FilterCaseSensitivity.Default)
        {
            Func<CompositeFilterDescriptor, bool> canFilter = (c) => 
               (!(c.FilterValue == null || c.FilterValue as string == string.Empty)
                || c.FilterOperator == FilterOperator.IsNotNull || c.FilterOperator == FilterOperator.IsNull
                || c.FilterOperator == FilterOperator.IsEmpty || c.FilterOperator == FilterOperator.IsNotEmpty)
               && c.Property != null;

            if (filters.Concat(filters.SelectManyRecursive(i => i.Filters ?? Enumerable.Empty<CompositeFilterDescriptor>())).Where(canFilter).Any())
            {
                var filterExpressions = new List<string>();

                foreach (var filter in filters)
                {
                    AddODataExpression<T>(canFilter, filter, ref filterExpressions, logicalFilterOperator, filterCaseSensitivity);
                }

                return filterExpressions.Any() ?
                    string.Join($" {logicalFilterOperator.ToString().ToLower()} ", filterExpressions)
                    : "";
            }
            return "";
        }

        private static void AddODataExpression<T>(Func<CompositeFilterDescriptor, bool> canFilter, 
            CompositeFilterDescriptor filter, ref List<string> filterExpressions, 
            LogicalFilterOperator logicalFilterOperator = LogicalFilterOperator.And,
            FilterCaseSensitivity filterCaseSensitivity = FilterCaseSensitivity.Default)
        {
            if (filter.Filters != null)
            {
                var innerFilterExpressions = new List<string>();

                foreach (var f in filter.Filters)
                {
                    AddODataExpression<T>(canFilter, f, ref innerFilterExpressions, logicalFilterOperator, filterCaseSensitivity);
                }

                if (innerFilterExpressions.Any())
                {
                    filterExpressions.Add("(" + string.Join($" {filter.LogicalFilterOperator.ToString().ToLower()} ", innerFilterExpressions) + ")");
                }
            }
            else
            {
                if (filter.Property == null || filter.FilterOperator == null || (filter.FilterValue == null &&
                    filter.FilterOperator != FilterOperator.IsNull && filter.FilterOperator != FilterOperator.IsNotNull))
                {
                    return;
                }

                var property = filter.Property.Replace('.', '/');

                var parameter = Expression.Parameter(typeof(T), "x");

                var propertyExpression = GetNestedPropertyExpression(parameter, filter.Property);

                if (propertyExpression == null) return;

                var filterPropertyType = filter.Type ?? propertyExpression.Type;

                if (filterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive && filterPropertyType == typeof(string))
                {
                    property = $"tolower({property})";
                }

                if (filter.FilterOperator == FilterOperator.StartsWith || filter.FilterOperator == FilterOperator.EndsWith
                    || filter.FilterOperator == FilterOperator.Contains || filter.FilterOperator == FilterOperator.DoesNotContain)
                {
                    if (IsEnumerable(filterPropertyType) && filterPropertyType != typeof(string) &&
                        (filter.FilterOperator == FilterOperator.Contains || filter.FilterOperator == FilterOperator.DoesNotContain))
                    {
                        var enumerableValue = ((IEnumerable)(filter.FilterValue != null ? filter.FilterValue : Enumerable.Empty<object>())).AsQueryable();
                        var firstItemType = enumerableValue.Cast<object>().Any() ? enumerableValue.FirstOrDefault().GetType() : typeof(object);

                        var enumerableValueAsString = "(" + String.Join(",",
                                (enumerableValue.ElementType == typeof(string) || firstItemType == typeof(string) ? enumerableValue.Cast<string>().Select(i => $@"'{i}'").Cast<object>() : enumerableValue.Cast<object>())) + ")";

                        if (enumerableValue.Cast<object>().Any() && filter.FilterOperator == FilterOperator.Contains)
                        {
                            filterExpressions.Add($"{property} in {enumerableValueAsString}");
                        }
                        else if (enumerableValue.Cast<object>().Any() && filter.FilterOperator == FilterOperator.DoesNotContain)
                        {
                            filterExpressions.Add($"not({property} in {enumerableValueAsString})");
                        }
                    }
                    else
                    {
                        var expression = filterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ?
                            $"{ODataFilterOperators[filter.FilterOperator.Value]}({property}, tolower('{filter.FilterValue}'))" :
                            $"{ODataFilterOperators[filter.FilterOperator.Value]}({property}, '{filter.FilterValue}')";

                        if (filter.FilterOperator == FilterOperator.DoesNotContain)
                        {
                            expression = $"not({expression})";
                        }

                        filterExpressions.Add(expression);
                    }
                }
                else
                {
                    if (IsEnumerable(filterPropertyType) && filterPropertyType != typeof(string))
                        return;

                    var value = $"{filter.FilterValue}";

                    if (filter.FilterOperator == FilterOperator.IsNull || filter.FilterOperator == FilterOperator.IsNotNull)
                    {
                        value = $"null";
                    }
                    else if (filter.FilterOperator == FilterOperator.IsEmpty || filter.FilterOperator == FilterOperator.IsNotEmpty)
                    {
                        value = $"''";
                    }
                    else if (filterPropertyType == typeof(string) || PropertyAccess.IsEnum(filterPropertyType) || PropertyAccess.IsNullableEnum(filterPropertyType))
                    {
                        value = $"'{value}'";
                    }
                    else if (filterPropertyType == typeof(DateTime) || filterPropertyType == typeof(DateTime?))
                    {
                        try
                        {
                            value = Convert.ToDateTime(filter.FilterValue).ToString(CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            //
                        }

                        value = $"{DateTime.Parse(value, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)}";
                    }
                    else if (filterPropertyType == typeof(bool) || filterPropertyType == typeof(bool?))
                    {
                        value = $"{value?.ToLower()}";
                    }

                    filterExpressions.Add($@"{property} {ODataFilterOperators[filter.FilterOperator.Value]} {value}");
                }
            }
        }

        /// <summary>
        /// Ases the o data enumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <returns>ODataEnumerable&lt;T&gt;.</returns>
        public static ODataEnumerable<T> AsODataEnumerable<T>(this IEnumerable<T> source)
        {
            return new ODataEnumerable<T>(source);
        }

        /// <summary>
        /// Selects the many recursive.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="selector">The selector.</param>
        /// <returns>IEnumerable&lt;T&gt;.</returns>
        public static IEnumerable<T> SelectManyRecursive<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> selector)
        {
            var result = source.SelectMany(selector);
            if (!result.Any())
            {
                return result;
            }
            return result.Concat(result.SelectManyRecursive(selector));
        }

        private static List<RadzenDataGridColumn<T>> GetFilterableColumns<T>(IEnumerable<RadzenDataGridColumn<T>> columns)
        {
            return columns
                .Where(c => c.Filterable
                    && c.FilterPropertyType != null
                    && (!(c.GetFilterValue() == null || c.GetFilterValue() as string == string.Empty)
                        || !c.CanSetFilterValue()
                        || c.HasCustomFilter())
                    && c.GetFilterProperty() != null)
                .ToList();
        }
    }
}