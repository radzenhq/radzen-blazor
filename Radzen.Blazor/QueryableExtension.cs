using Radzen.Blazor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace Radzen
{
    /// <summary>
    /// Class QueryableExtension.
    /// </summary>
    public static class QueryableExtension
    {
        /// <summary>
        /// The filter operators
        /// </summary>
        internal static readonly IDictionary<string, string> FilterOperators = new Dictionary<string, string>
        {
            {"eq", "="},
            {"ne", "!="},
            {"lt", "<"},
            {"le", "<="},
            {"gt", ">"},
            {"ge", ">="},
            {"startswith", "StartsWith"},
            {"endswith", "EndsWith"},
            {"contains", "Contains"},
            {"DoesNotContain", "Contains"}
        };

        /// <summary>
        /// The linq filter operators
        /// </summary>
        internal static readonly IDictionary<FilterOperator, string> LinqFilterOperators = new Dictionary<FilterOperator, string>
        {
            {FilterOperator.Equals, "="},
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
            {FilterOperator.DoesNotContain, "DoesNotContain"},
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
            var columnsWithFilter = GetFilterableColumns(columns);

            if (columnsWithFilter.Any())
            {
                var gridLogicalFilterOperator = columns.FirstOrDefault()?.Grid?.LogicalFilterOperator;
                var gridBooleanOperator = gridLogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                var whereList = new List<string>();
                foreach (var column in columnsWithFilter)
                {
                    string value = "";
                    string secondValue = "";

                    var v = column.GetFilterValue();
                    var sv = column.GetSecondFilterValue();

                    if (column.GetFilterOperator() == FilterOperator.Custom)
                    {
                        var customFilterExpression = column.GetCustomFilterExpression();
                        if (!string.IsNullOrEmpty(customFilterExpression))
                        {
                            whereList.Add(customFilterExpression);
                        }
                    }
                    else if (PropertyAccess.IsDate(column.FilterPropertyType))
                    {
                        if (v != null)
                        {
                           
                            value = 
                                v is DateTime ? ((DateTime) v).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)
                                : v is DateTimeOffset ? ((DateTimeOffset) v).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)
                                : 
#if NET6_0_OR_GREATER
                                v is DateOnly ? ((DateOnly) v).ToString("yyy-MM-dd", CultureInfo.InvariantCulture) : "";
#else
                                    "";
#endif
                        }
                        if (sv != null)
                        {
                            secondValue = 
                                sv is DateTime ? ((DateTime)sv).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture) 
                                : sv is DateTimeOffset ? ((DateTimeOffset)sv).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture) 
                                : 
#if NET6_0_OR_GREATER
                                sv is DateOnly ? ((DateOnly) sv).ToString("yyy-MM-dd", CultureInfo.InvariantCulture) : "";
#else
                            "";
#endif
                        }
                    }
                    else if (PropertyAccess.IsEnum(column.FilterPropertyType) || PropertyAccess.IsNullableEnum(column.FilterPropertyType))
                    {
                        if (v != null)
                        {
                            value = ((int)v).ToString();
                        }
                        if (sv != null)
                        {
                            secondValue = ((int)sv).ToString();
                        }
                    }
                    else if (IsEnumerable(column.FilterPropertyType) && column.FilterPropertyType != typeof(string))
                    {
                        var enumerableValue = ((IEnumerable)(v != null ? v : Enumerable.Empty<object>())).AsQueryable();
                        var enumerableSecondValue = ((IEnumerable)(sv != null ? sv : Enumerable.Empty<object>())).AsQueryable();

                        string baseType = column.FilterPropertyType.GetGenericArguments().Count() == 1 ? column.FilterPropertyType.GetGenericArguments()[0].Name : "";

                        var enumerableValueAsString = "new " + baseType + "[]{" + String.Join(",",
                                (enumerableValue.ElementType == typeof(string) ? enumerableValue.Cast<string>().Select(i => $@"""{i}""").Cast<object>() : enumerableValue.Cast<object>())) + "}";

                        var enumerableSecondValueAsString = "new " + baseType + "[]{" + String.Join(",",
                                (enumerableSecondValue.ElementType == typeof(string) ? enumerableSecondValue.Cast<string>().Select(i => $@"""{i}""").Cast<object>() : enumerableSecondValue.Cast<object>())) + "}";

                        if (enumerableValue?.Any() == true)
                        {
                            var columnFilterOperator = column.GetFilterOperator();
                            var columnSecondFilterOperator = column.GetSecondFilterOperator();
                            var linqOperator = LinqFilterOperators[column.GetFilterOperator()];
                            if (linqOperator == null)
                            {
                                linqOperator = "==";
                            }

                            var booleanOperator = column.LogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                            var property = PropertyAccess.GetProperty(column.GetFilterProperty());

                            if (property.IndexOf(".") != -1)
                            {
                                property = $"({property})";
                            }

                            if (sv == null)
                            {
                                if (columnFilterOperator == FilterOperator.Contains || columnFilterOperator == FilterOperator.DoesNotContain)
                                {
                                    whereList.Add($@"{(columnFilterOperator == FilterOperator.DoesNotContain ? "!" : "")}({enumerableValueAsString}).Contains({property})");
                                }
                                else if (columnFilterOperator == FilterOperator.In || columnFilterOperator == FilterOperator.NotIn)
                                {
	                                whereList.Add($@"({property}).{(columnFilterOperator == FilterOperator.NotIn ? "Except" : "Intersect")}({enumerableValueAsString}).Any()");
                                }
                            }
                            else
                            {
                                if ((columnFilterOperator == FilterOperator.Contains || columnFilterOperator == FilterOperator.DoesNotContain) &&
                                        (columnSecondFilterOperator == FilterOperator.Contains || columnSecondFilterOperator == FilterOperator.DoesNotContain))
                                {
                                    whereList.Add($@"{(columnFilterOperator == FilterOperator.DoesNotContain ? "!" : "")}({enumerableValueAsString}).Contains({property}) {booleanOperator} {(columnSecondFilterOperator == FilterOperator.DoesNotContain ? "!" : "")}({enumerableSecondValueAsString}).Contains({property})");
                                }
                                else if ((columnFilterOperator == FilterOperator.In || columnFilterOperator == FilterOperator.NotIn) &&
                                         (columnSecondFilterOperator == FilterOperator.In || columnSecondFilterOperator == FilterOperator.NotIn))
                                {
	                                whereList.Add($@"({property}).{(columnFilterOperator == FilterOperator.NotIn ? "Except" : "Intersect")}({enumerableValueAsString}).Any() {booleanOperator} ({property}).{(columnSecondFilterOperator == FilterOperator.NotIn ? "Except" : "Intersect")}({enumerableSecondValueAsString}).Any()");
                                }
                            }
                        }
                    }
                    else
                    {
                        value = (string)Convert.ChangeType(column.GetFilterValue(), typeof(string), CultureInfo.InvariantCulture);
                        secondValue = (string)Convert.ChangeType(column.GetSecondFilterValue(), typeof(string), CultureInfo.InvariantCulture);
                    }

                    if (!string.IsNullOrEmpty(value) || column.GetFilterOperator() == FilterOperator.IsNotNull
                                                    || column.GetFilterOperator() == FilterOperator.IsNull
                                                    || column.GetFilterOperator() == FilterOperator.IsEmpty
                                                    || column.GetFilterOperator() == FilterOperator.IsNotEmpty)
                    {
                        var linqOperator = LinqFilterOperators[column.GetFilterOperator()];
                        if (linqOperator == null)
                        {
                            linqOperator = "==";
                        }

                        var booleanOperator = column.LogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                        if (string.IsNullOrEmpty(secondValue))
                        {
                            whereList.Add(GetColumnFilter(column, value));
                        }
                        else
                        {
                            whereList.Add($"({GetColumnFilter(column, value)} {booleanOperator} {GetColumnFilter(column, secondValue, true)})");
                        }
                    }
                }

                return string.Join($" {gridBooleanOperator} ", whereList.Where(i => !string.IsNullOrEmpty(i)));
            }

            return "";
        }

        /// <summary>
        /// Converts a RadzenDataFilter to a Linq-compatibly filter string
        /// </summary>
        /// <typeparam name="T">The type that is being filtered</typeparam>
        /// <param name="filter">The RadzenDataFilter component</param>
        /// <returns>A Linq-compatible filter string</returns>
        public static string ToFilterString<T>(this RadzenDataFilter<T> filter)
        {
            return CompositeFilterToFilterString<T>(filter.Filters, filter, filter.LogicalFilterOperator);
        }

        /// <summary>
        /// Creates a Linq-compatible filter string for a list of CompositeFilterDescriptions
        /// </summary>
        /// <typeparam name="T">The type that is being filtered</typeparam>
        /// <param name="filters">The list if filters</param>
        /// <param name="Datafilter">The RadzenDataFilter component</param>
        /// <param name="filterOperator">Whether filter elements should be and-ed or or-ed</param>
        /// <returns></returns>
        private static string CompositeFilterToFilterString<T>(IEnumerable<CompositeFilterDescriptor> filters, RadzenDataFilter<T> Datafilter, LogicalFilterOperator filterOperator)
        {
            if (filters.Any())
            {
                var LogicalFilterOperator = filterOperator;
                var BooleanOperator = LogicalFilterOperator == LogicalFilterOperator.And ? "&&" : "||";

                var whereList = new List<string>();
                foreach (var column in filters)
                {
                    if (column.Filters is not null && column.Filters.Any())
                    {
                        whereList.Add($"({CompositeFilterToFilterString(column.Filters, Datafilter, column.LogicalFilterOperator)})");
                    }
                    if (column.Property is not null)
                    {
                        whereList.Add($"{GetColumnFilter(Datafilter, column, Datafilter.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive)}");
                    }
                }

                return string.Join($" {BooleanOperator} ", whereList.Where(i => !string.IsNullOrEmpty(i)));
            }

            return "";

        }

        /// <summary>
        /// Create a single linq-compatible filter string for one datafilter element (includes sub-lists)
        /// </summary>
        /// <typeparam name="T">The type that is being filtered</typeparam>
        /// <param name="dataFilter">The RadzenDataFilter component</param>
        /// <param name="column">The filter elements for which to create the filter string</param>
        /// <param name="caseSensitive">Whether filtering is case sensitive or not</param>
        /// <returns></returns>
        private static string GetColumnFilter<T>(RadzenDataFilter<T> dataFilter, CompositeFilterDescriptor column, bool caseSensitive)
        {
            var property = column.Property;

            if (property.Contains(".", StringComparison.CurrentCulture))
            {
                property = $"({property})";
            }

            var columnInfo = dataFilter.properties.Where(c => c.Property == column.Property).FirstOrDefault();
            if (columnInfo == null) return "";

            var columnType = columnInfo.FilterPropertyType;

            var columnFilterOperator = column.FilterOperator;

            var linqOperator = LinqFilterOperators[columnFilterOperator.Value];
            linqOperator ??= "==";

            var value = "";

            if (columnType == typeof(string))
            {
                value = (string)Convert.ChangeType(column.FilterValue, typeof(string));
                value = value?.Replace("\"", "\\\"");

                string filterCaseSensitivityOperator = caseSensitive ? ".ToLower()" : "";

                if (!string.IsNullOrEmpty(value) && column.FilterOperator == FilterOperator.Contains)
                {
                    return $@"({property} == null ? """" : {property}){filterCaseSensitivityOperator}.Contains(""{value}""{filterCaseSensitivityOperator})";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.DoesNotContain)
                {
                    return $@"({property} == null ? """" : !{property}){filterCaseSensitivityOperator}.Contains(""{value}""{filterCaseSensitivityOperator})";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.StartsWith)
                {
                    return $@"({property} == null ? """" : {property}){filterCaseSensitivityOperator}.StartsWith(""{value}""{filterCaseSensitivityOperator})";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.EndsWith)
                {
                    return $@"({property} == null ? """" : {property}){filterCaseSensitivityOperator}.EndsWith(""{value}""{filterCaseSensitivityOperator})";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.Equals)
                {
                    return $@"({property} == null ? """" : {property}){filterCaseSensitivityOperator} == ""{value}""{filterCaseSensitivityOperator}";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.NotEquals)
                {
                    return $@"({property} == null ? """" : {property}){filterCaseSensitivityOperator} != ""{value}""{filterCaseSensitivityOperator}";
                }
            }
            else if (PropertyAccess.IsNumeric(columnType))
            {
                value = (string)Convert.ChangeType(column.FilterValue, typeof(string));

                return $"{property} {linqOperator} {value}";
            }
            else if (columnType == typeof(bool))
            {
                value = (string)Convert.ChangeType(column.FilterValue, typeof(string));

                return $"{property} == {value}";
            }
            else if (PropertyAccess.IsDate(columnType))
            {
                var v = column.FilterValue;
                if (v != null)
                {
                    value = $"DateTime({(v is DateTime time ? time.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture) : v is DateTimeOffset offset ? offset.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture) : "")})";
                }
            }
            else if (PropertyAccess.IsEnum(columnType) || PropertyAccess.IsNullableEnum(columnType))
            {
                var v = column.FilterValue;
                if (v != null)
                {
                    value = ((int)v).ToString();
                }
            }
            else if (IsEnumerable(columnType) && columnType != typeof(string))
            {
                var v = column.FilterValue;
                var enumerableValue = ((IEnumerable)(v ?? Enumerable.Empty<object>())).AsQueryable();

                string baseType = columnType.GetGenericArguments().Count() == 1 ? columnType.GetGenericArguments()[0].Name : "";

                var enumerableValueAsString = "new " + baseType + "[]{" + String.Join(",",
                        (enumerableValue.ElementType == typeof(string) ? enumerableValue.Cast<string>().Select(i => $@"""{i}""").Cast<object>() : enumerableValue.Cast<object>())) + "}";


                if (enumerableValue?.Any() == true)
                {
                    if (property.Contains("."))
                    {
                        property = $"({property})";
                    }

                    if (columnFilterOperator == FilterOperator.Contains || columnFilterOperator == FilterOperator.DoesNotContain)
                    {
                        return $@"{(columnFilterOperator == FilterOperator.DoesNotContain ? "!" : "")}({enumerableValueAsString}).Contains({property})";
                    }
                    else if (columnFilterOperator == FilterOperator.In || columnFilterOperator == FilterOperator.NotIn)
                    {
                        return $@"({property}).{(columnFilterOperator == FilterOperator.NotIn ? "Except" : "Intersect")}({enumerableValueAsString}).Any()";
                    }
                }
            }
            else
            {
                value = (string)Convert.ChangeType(column.FilterValue, typeof(string), CultureInfo.InvariantCulture);
            }
            if (!string.IsNullOrEmpty(value) || column.FilterOperator == FilterOperator.IsNotNull
                                            || column.FilterOperator == FilterOperator.IsNull
                                            || column.FilterOperator == FilterOperator.IsEmpty
                                            || column.FilterOperator == FilterOperator.IsNotEmpty)
            {
                return $"({property} {linqOperator} {value})";
            }

            return "";
        }

        /// <summary>
        /// Gets the column filter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="column">The column.</param>
        /// <param name="value">The value.</param>
        /// <param name="second">if set to <c>true</c> [second].</param>
        /// <returns>System.String.</returns>
        private static string GetColumnFilter<T>(RadzenDataGridColumn<T> column, string value, bool second = false)
        {
            var property = PropertyAccess.GetProperty(column.GetFilterProperty());

            if (property.IndexOf(".") != -1)
            {
                property = $"({property})";
            }
            bool hasNp = property.Contains("np(");
            string npProperty = hasNp ? property : $@"np({property})";

            var columnFilterOperator = !second ? column.GetFilterOperator() : column.GetSecondFilterOperator();

            if (columnFilterOperator == FilterOperator.Custom)
            {
                return "";
            }

            var linqOperator = LinqFilterOperators[columnFilterOperator];
            if (linqOperator == null)
            {
                linqOperator = "==";
            }
            bool isDateOnly = false;

#if NET6_0_OR_GREATER
            if (column.FilterPropertyType == typeof(DateOnly) || column.FilterPropertyType == typeof(DateOnly?))
            {
                isDateOnly = true;
            }
#endif

            if (column.FilterPropertyType == typeof(string))
            {
                string filterCaseSensitivityOperator = column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? ".ToLower()" : "";
                value = value?.Replace("\"", "\\\"");

                if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.Contains)
                {
                    return $@"({property} == null ? """" : {property}){filterCaseSensitivityOperator}.Contains(""{value}""{filterCaseSensitivityOperator})";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.DoesNotContain)
                {
                    return $@"!({property} == null ? """" : {property}){filterCaseSensitivityOperator}.Contains(""{value}""{filterCaseSensitivityOperator})";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.StartsWith)
                {
                    return $@"({property} == null ? """" : {property}){filterCaseSensitivityOperator}.StartsWith(""{value}""{filterCaseSensitivityOperator})";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.EndsWith)
                {
                    return $@"({property} == null ? """" : {property}){filterCaseSensitivityOperator}.EndsWith(""{value}""{filterCaseSensitivityOperator})";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.Equals)
                {
                    return $@"({property} == null ? """" : {property}){filterCaseSensitivityOperator} == ""{value}""{filterCaseSensitivityOperator}";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == FilterOperator.NotEquals)
                {
                    return $@"({property} == null ? """" : {property}){filterCaseSensitivityOperator} != ""{value}""{filterCaseSensitivityOperator}";
                }
                else if (columnFilterOperator == FilterOperator.IsNull)
                {
                    return npProperty + " == null";
                }
                else if (columnFilterOperator == FilterOperator.IsEmpty)
                {
                    return npProperty + @" == """"";
                }
                else if (columnFilterOperator == FilterOperator.IsNotEmpty)
                {
                    return npProperty + @" != """"";
                }
                else if (columnFilterOperator == FilterOperator.IsNotNull)
                {
                    return npProperty + @" != null";
                }
            }
            else if (PropertyAccess.IsNumeric(column.FilterPropertyType))
            {
                if (columnFilterOperator == FilterOperator.IsNull || columnFilterOperator == FilterOperator.IsNotNull)
                {
                    return $"{property} {linqOperator} null";
                }
                else if (columnFilterOperator == FilterOperator.IsEmpty || columnFilterOperator == FilterOperator.IsNotEmpty)
                {
                    return $@"{property} {linqOperator} """"";
                }
                else
                {
                    return $"{property} {linqOperator} {value}";
                }
            }
            else if (column.FilterPropertyType == typeof(DateTime) ||
                    column.FilterPropertyType == typeof(DateTime?) ||
                    column.FilterPropertyType == typeof(DateTimeOffset) ||
                    column.FilterPropertyType == typeof(DateTimeOffset?) || isDateOnly)
            {
                if (columnFilterOperator == FilterOperator.IsNull || columnFilterOperator == FilterOperator.IsNotNull)
                {
                    return $"{property} {linqOperator} null";
                }
                else if (columnFilterOperator == FilterOperator.IsEmpty || columnFilterOperator == FilterOperator.IsNotEmpty)
                {
                    return $@"{property} {linqOperator} """"";
                }
                else
                {
                    var dateTimeValue = DateTime.Parse(value, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);
                    var finalDate = dateTimeValue.TimeOfDay == TimeSpan.Zero ? dateTimeValue.Date : dateTimeValue;
                    var dateFormat = dateTimeValue.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd" : "yyyy-MM-ddTHH:mm:ss.fffZ";
                  
                    string dateFunction = "DateTime"; //fallback to datetime, if it's an offset or dateonly use that
                    if (column.FilterPropertyType == typeof(DateTimeOffset) || column.FilterPropertyType == typeof(DateTimeOffset?))
                        dateFunction = "DateTimeOffset";
                    else
                    {
#if NET6_0_OR_GREATER
                        if (column.FilterPropertyType == typeof(DateOnly) || column.FilterPropertyType == typeof(DateOnly?))
                            dateFunction = "DateOnly";
#endif
                    }

                    
                    return $@"{property} {linqOperator} {dateFunction}(""{finalDate.ToString(dateFormat, CultureInfo.InvariantCulture)}"")";
                }
            }
            else if (column.FilterPropertyType == typeof(bool) || column.FilterPropertyType == typeof(bool?))
            {
                return $"{property} == {value}";
            }
            else if (column.FilterPropertyType == typeof(Guid) || column.FilterPropertyType == typeof(Guid?))
            {
                if (columnFilterOperator == FilterOperator.IsNull || columnFilterOperator == FilterOperator.IsNotNull)
                {
                    return $"{property} {linqOperator} null";
                }
                else if (columnFilterOperator == FilterOperator.IsEmpty || columnFilterOperator == FilterOperator.IsNotEmpty)
                {
                    return $@"{property} {linqOperator} """"";
                }
                else
                {
                    return $@"{property} {linqOperator} Guid(""{value}"")";
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
                : (string)Convert.ChangeType(filterValue, typeof(string), CultureInfo.InvariantCulture);

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

                if (enumerableValue.Any() && columnFilterOperator == FilterOperator.Contains)
                {
                    return $"{property} in {enumerableValueAsString}";
                }
                else if (enumerableValue.Any() && columnFilterOperator == FilterOperator.DoesNotContain)
                {
                    return $"not({property} in {enumerableValueAsString})";
                }
                else if (enumerableValue.Any() && columnFilterOperator == FilterOperator.In)
                {
                    return $"{column.Property}/any(i:{enumerableValueAsStringOrForAny})";
                }
                else if (enumerableValue.Any() && columnFilterOperator == FilterOperator.NotIn)
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
                    column.FilterPropertyType == typeof(DateTimeOffset?))
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
                    return $"{property} {odataFilterOperator} {DateTime.Parse(value, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)}";
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
            return typeof(IEnumerable).IsAssignableFrom(type) || typeof(IEnumerable<>).IsAssignableFrom(type);
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
                || c.GetFilterOperator() == FilterOperator.IsEmpty || c.GetFilterOperator() == FilterOperator.IsNotEmpty
                || (c.GetFilterOperator() == FilterOperator.Custom && c.GetCustomFilterExpression() != null))
               && c.GetFilterProperty() != null;

            if (columns.Where(canFilter).Any())
            {
                var gridLogicalFilterOperator = columns.FirstOrDefault()?.Grid?.LogicalFilterOperator;
                var gridBooleanOperator = gridLogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                var index = 0;
                var whereList = new Dictionary<string, IEnumerable<object>>();
                foreach (var column in columns.Where(canFilter))
                {
                    if (column.GetFilterOperator() == FilterOperator.Custom)
                    {
                        var customFilterExpression = column.GetCustomFilterExpression();
                        if (!string.IsNullOrEmpty(customFilterExpression))
                        {
                            whereList.Add(customFilterExpression, Enumerable.Empty<object>());
                        }
                    }
                    else
                    {
                        var property = PropertyAccess.GetProperty(column.GetFilterProperty());

                    if (property.IndexOf(".") != -1)
                    {
                        property = $"({property})";
                    }

                    if (column.FilterPropertyType == typeof(string) &&
                        !(column.GetFilterOperator() == FilterOperator.IsNotNull || column.GetFilterOperator() == FilterOperator.IsNull
                            || column.GetFilterOperator() == FilterOperator.IsEmpty || column.GetFilterOperator() == FilterOperator.IsNotEmpty))
                    {
                        property = $@"({property} == null ? """" : {property})";
                    }

                    string filterCaseSensitivityOperator = column.FilterPropertyType == typeof(string)
                           && column.GetFilterOperator() != FilterOperator.IsNotNull && column.GetFilterOperator() != FilterOperator.IsNull
                           && column.GetFilterOperator() != FilterOperator.IsEmpty && column.GetFilterOperator() != FilterOperator.IsNotEmpty
                           && column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? ".ToLower()" : "";


                    var comparison = LinqFilterOperators[column.GetFilterOperator()];

                    var booleanOperator = column.LogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                    if (column.GetSecondFilterValue() == null)
                    {
                        if (comparison == "StartsWith" || comparison == "EndsWith" || comparison == "Contains")
                        {
                            if (IsEnumerable(column.FilterPropertyType) && column.FilterPropertyType != typeof(string) && comparison == "Contains")
                            {
                                whereList.Add($@"(@{index}).Contains({property})", new object[] { column.GetFilterValue() });
                            }
                            else
                            {
                                whereList.Add($@"{property}{filterCaseSensitivityOperator}.{comparison}(@{index}{filterCaseSensitivityOperator})", new object[] { column.GetFilterValue() });
                            }

                            index++;
                        }
                        else if (comparison == "DoesNotContain")
                        {
                            if (IsEnumerable(column.FilterPropertyType) && column.FilterPropertyType != typeof(string) && comparison == "DoesNotContain")
                            {
                                whereList.Add($@"!(@{index}).Contains({property})", new object[] { column.GetFilterValue() });
                            }
                            else
                            {
                                whereList.Add($@"!{property}{filterCaseSensitivityOperator}.Contains(@{index}{filterCaseSensitivityOperator})", new object[] { column.GetFilterValue() });
                            }

                            index++;
                        }
                        else if (comparison == "In" || comparison == "NotIn")
                        {
                            if (IsEnumerable(column.FilterPropertyType) && column.FilterPropertyType != typeof(string) &&
                                    IsEnumerable(column.PropertyType) && column.PropertyType != typeof(string))
                            {
                                whereList.Add($@"{(comparison == "NotIn" ? "!" : "")}{property}.Any(i => i in @{index})", new object[] { column.GetFilterValue() });
                                index++;
                            }
                            else if (IsEnumerable(column.FilterPropertyType) && column.FilterPropertyType != typeof(string) &&
                                column.Property != column.FilterProperty && !string.IsNullOrEmpty(column.FilterProperty))
                            {
                                whereList.Add($@"{(comparison == "NotIn" ? "!" : "")}{column.Property}.Any(i => i.{column.FilterProperty} in @{index})", new object[] { column.GetFilterValue() });
                                index++;
                            }
                        }
                        else if (!(IsEnumerable(column.FilterPropertyType) && column.FilterPropertyType != typeof(string)))
                        {
                            whereList.Add($@"{property}{filterCaseSensitivityOperator} {comparison} @{index}{filterCaseSensitivityOperator}", new object[] { column.GetFilterValue() });
                            index++;
                        }
                    }
                    else
                    {
                        var secondComparison = LinqFilterOperators[column.GetSecondFilterOperator()];

                        if (IsEnumerable(column.FilterPropertyType) && column.FilterPropertyType != typeof(string) &&
                            (comparison == "Contains" || comparison == "DoesNotContain") &&
                                (secondComparison == "Contains" || secondComparison == "DoesNotContain"))
                        {
                            var firstFilter = $@"{(comparison == "DoesNotContain" ? "!" : "")}(@{index}).Contains({property})";
                            index++;

                            var secondFilter = $@"{(secondComparison == "DoesNotContain" ? "!" : "")}(@{index}).Contains({property})";
                            index++;

                            whereList.Add($@"({firstFilter} {booleanOperator} {secondFilter})", new object[] { column.GetFilterValue(), column.GetSecondFilterValue() });
                        }
                        else
                        {
                            var firstFilter = comparison == "StartsWith" || comparison == "EndsWith" || comparison == "Contains" ?
                                $@"{property}{filterCaseSensitivityOperator}.{comparison}(@{index}{filterCaseSensitivityOperator})" :
                                comparison == "DoesNotContain" ? $@"!{property}{filterCaseSensitivityOperator}.Contains(@{index}{filterCaseSensitivityOperator})" :
                                $@"{property}{filterCaseSensitivityOperator} {comparison} @{index}{filterCaseSensitivityOperator}";
                            index++;

                            var secondFilter = secondComparison == "StartsWith" || secondComparison == "EndsWith" || secondComparison == "Contains" ?
                                $@"{property}{filterCaseSensitivityOperator}.{secondComparison}(@{index}{filterCaseSensitivityOperator})" :
                                secondComparison == "DoesNotContain" ? $@"!{property}{filterCaseSensitivityOperator}.Contains(@{index}{filterCaseSensitivityOperator})" :
                                $@"{property}{filterCaseSensitivityOperator} {secondComparison} @{index}{filterCaseSensitivityOperator}";
                            index++;

                            whereList.Add($@"({firstFilter} {booleanOperator} {secondFilter})", new object[] { column.GetFilterValue(), column.GetSecondFilterValue() });
                        }
                    }
                }
                }

                return whereList.Keys.Any() ?
                    source.Where(DynamicLinqCustomTypeProvider.ParsingConfig, string.Join($" {gridBooleanOperator} ", whereList.Keys), whereList.Values.SelectMany(i => i.ToArray()).ToArray())
                    : source;
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
                var index = 0;
                var filterExpressions = new List<string>();
                var filterValues = new List<object[]>();

                foreach (var filter in dataFilter.Filters)
                {
                    AddWhereExpression(canFilter, filter, ref filterExpressions, ref filterValues, ref index, dataFilter);
                }

                return filterExpressions.Any() ?
                    source.Where(DynamicLinqCustomTypeProvider.ParsingConfig, string.Join($" {dataFilter.LogicalFilterOperator.ToString().ToLower()} ", filterExpressions), filterValues.SelectMany(i => i.ToArray()).ToArray())
                    : source;
            }

            return source;
        }

        private static void AddWhereExpression<T>(Func<CompositeFilterDescriptor, bool> canFilter, CompositeFilterDescriptor filter, ref List<string> filterExpressions, ref List<object[]> filterValues, ref int index, RadzenDataFilter<T> dataFilter)
        {
            if (filter.Filters != null)
            {
                var innerFilterExpressions = new List<string>();

                foreach (var f in filter.Filters)
                {
                    AddWhereExpression(canFilter, f, ref innerFilterExpressions, ref filterValues, ref index, dataFilter);
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

                var property = PropertyAccess.GetProperty(filter.Property);

                if (property.IndexOf(".") != -1)
                {
                    property = $"({property})";
                }

                var column = dataFilter.properties.Where(c => c.Property == filter.Property).FirstOrDefault();
                if (column == null) return;

                if (column.FilterPropertyType == typeof(string) &&
                    !(filter.FilterOperator == FilterOperator.IsNotNull || filter.FilterOperator == FilterOperator.IsNull
                        || filter.FilterOperator == FilterOperator.IsEmpty || filter.FilterOperator == FilterOperator.IsNotEmpty))
                {
                    property = $@"({property} == null ? """" : {property})";
                }

                string filterCaseSensitivityOperator = column.FilterPropertyType == typeof(string)
                       && filter.FilterOperator != FilterOperator.IsNotNull && filter.FilterOperator != FilterOperator.IsNull
                       && filter.FilterOperator != FilterOperator.IsEmpty && filter.FilterOperator != FilterOperator.IsNotEmpty
                       && dataFilter.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? ".ToLower()" : "";


                var comparison = LinqFilterOperators[filter.FilterOperator.Value];

                if (comparison == "StartsWith" || comparison == "EndsWith" || comparison == "Contains")
                {
                    if (column.FilterPropertyType == typeof(string) && filter.FilterValue == null)
                    {
                        filter.FilterValue = "";
                    }

                    if (IsEnumerable(column.FilterPropertyType) && column.FilterPropertyType != typeof(string) && comparison == "Contains")
                    {
                        filterExpressions.Add($@"(@{index}).Contains({property})");
                        filterValues.Add(new object[] { filter.FilterValue  });
                    }
                    else
                    {
                        filterExpressions.Add($@"{property}{filterCaseSensitivityOperator}.{comparison}(@{index}{filterCaseSensitivityOperator})");
                        filterValues.Add(new object[] { filter.FilterValue });
                    }

                    index++;
                }
                else if (comparison == "DoesNotContain")
                {
                    if (column.FilterPropertyType == typeof(string) && filter.FilterValue == null)
                    {
                        filter.FilterValue = "";
                    }

                    if (IsEnumerable(column.FilterPropertyType) && column.FilterPropertyType != typeof(string) && comparison == "DoesNotContain")
                    {
                        filterExpressions.Add($@"!(@{index}).Contains({property})");
                        filterValues.Add(new object[] { filter.FilterValue });
                    }
                    else
                    {
                        filterExpressions.Add($@"!{property}{filterCaseSensitivityOperator}.Contains(@{index}{filterCaseSensitivityOperator})");
                        filterValues.Add(new object[] { filter.FilterValue });
                    }

                    index++;
                }
                else if (comparison == "In" || comparison == "NotIn")
                {
                    if (IsEnumerable(column.FilterPropertyType) && column.FilterPropertyType != typeof(string))
                    {
                        filterExpressions.Add($@"{(comparison == "NotIn" ? "!" : "")}{property}.Any(i => i in @{index})");
                        filterValues.Add(new object[] { filter.FilterValue });

                        index++;
                    }
                }
                else if (!(IsEnumerable(column.FilterPropertyType) && column.FilterPropertyType != typeof(string)))
                {
                    var value = filter.FilterValue;

                    if (column.FilterPropertyType == typeof(DateTimeOffset) || column.FilterPropertyType == typeof(DateTimeOffset?))
                    {
                        value = filter.FilterValue != null ? (object)(new DateTimeOffset((DateTime)filter.FilterValue, TimeSpan.Zero)) : null;
                    }

                    filterExpressions.Add($@"{property}{filterCaseSensitivityOperator} {comparison} @{index}{filterCaseSensitivityOperator}");
                    filterValues.Add(new object[] { value });
                    index++;
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

                var query = new List<string>();

                if (!string.IsNullOrEmpty(property))
                {
                    query.Add(property);
                }

                if (typeof(EnumerableQuery).IsAssignableFrom(source.GetType()))
                {
                    query.Add("ToString()");
                }

                if (ignoreCase)
                {
                    query.Add("ToLower()");
                }

                query.Add($"{Enum.GetName(typeof(StringFilterOperator), op)}(@0)");

                var search = ignoreCase ? value.ToLower() : value;

                if (source.ElementType == typeof(Enum))
                {
                    result = source.Cast<Enum>()
                        .Where((Func<Enum, bool>)(i =>
                        {
                            var v = ignoreCase ? i.GetDisplayDescription().ToLower() : i.GetDisplayDescription();

                            if (op == StringFilterOperator.Contains)
                            {
                                return v.Contains(search);
                            }
                            else if (op == StringFilterOperator.StartsWith)
                            {
                                return v.StartsWith(search);
                            }
                            else if (op == StringFilterOperator.EndsWith)
                            {
                                return v.EndsWith(search);
                            }

                            return v == search;
                        })).AsQueryable();
                }
                else
                {
                    result = source.Where(DynamicLinqCustomTypeProvider.ParsingConfig, string.Join(".", query), search);
                }
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
                    AddODataExpression(canFilter, filter, ref filterExpressions, dataFilter);
                }

                return filterExpressions.Any() ?
                    string.Join($" {dataFilter.LogicalFilterOperator.ToString().ToLower()} ", filterExpressions)
                    : "";
            }
            return "";
        }

        private static void AddODataExpression<T>(Func<CompositeFilterDescriptor, bool> canFilter, CompositeFilterDescriptor filter, ref List<string> filterExpressions, RadzenDataFilter<T> dataFilter)
        {
            if (filter.Filters != null)
            {
                var innerFilterExpressions = new List<string>();

                foreach (var f in filter.Filters)
                {
                    AddODataExpression(canFilter, f, ref innerFilterExpressions, dataFilter);
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

                var column = dataFilter.properties.Where(c => c.Property == filter.Property).FirstOrDefault();
                if (column == null) return;

                if (dataFilter.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive && column.FilterPropertyType == typeof(string))
                {
                    property = $"tolower({property})";
                }

                if (filter.FilterOperator == FilterOperator.StartsWith || filter.FilterOperator == FilterOperator.EndsWith
                    || filter.FilterOperator == FilterOperator.Contains || filter.FilterOperator == FilterOperator.DoesNotContain)
                {
                    if (IsEnumerable(column.FilterPropertyType) && column.FilterPropertyType != typeof(string) &&
                        (filter.FilterOperator == FilterOperator.Contains || filter.FilterOperator == FilterOperator.DoesNotContain))
                    {
                        var enumerableValue = ((IEnumerable)(filter.FilterValue != null ? filter.FilterValue : Enumerable.Empty<object>())).AsQueryable();
                        var firstItemType = enumerableValue.Any() ? enumerableValue.FirstOrDefault().GetType() : typeof(object);

                        var enumerableValueAsString = "(" + String.Join(",",
                                (enumerableValue.ElementType == typeof(string) || firstItemType == typeof(string) ? enumerableValue.Cast<string>().Select(i => $@"'{i}'").Cast<object>() : enumerableValue.Cast<object>())) + ")";

                        if (enumerableValue.Any() && filter.FilterOperator == FilterOperator.Contains)
                        {
                            filterExpressions.Add($"{property} in {enumerableValueAsString}");
                        }
                        else if (enumerableValue.Any() && filter.FilterOperator == FilterOperator.DoesNotContain)
                        {
                            filterExpressions.Add($"not({property} in {enumerableValueAsString})");
                        }
                    }
                    else
                    {
                        var expression = dataFilter.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ?
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
                    if (IsEnumerable(column.FilterPropertyType) && column.FilterPropertyType != typeof(string))
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
                    else if (column.FilterPropertyType == typeof(string) || PropertyAccess.IsEnum(column.FilterPropertyType) || PropertyAccess.IsNullableEnum(column.FilterPropertyType))
                    {
                        value = $"'{value}'";
                    }
                    else if (column.FilterPropertyType == typeof(DateTime) || column.FilterPropertyType == typeof(DateTime?))
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
                    else if (column.FilterPropertyType == typeof(bool) || column.FilterPropertyType == typeof(bool?))
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
