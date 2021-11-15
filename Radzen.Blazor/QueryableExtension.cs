using Radzen.Blazor;
using System;
using System.Collections;
using System.Collections.Generic;
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
            {FilterOperator.DoesNotContain, "DoesNotContain"}
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
            {FilterOperator.DoesNotContain, "DoesNotContain"}
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
        public static string ToFilterString<T>(this IEnumerable<RadzenGridColumn<T>> columns)
        {
            Func<RadzenGridColumn<T>, bool> canFilter = (c) => c.Filterable && !string.IsNullOrEmpty(c.Type) &&
                !(c.FilterValue == null || c.FilterValue as string == string.Empty) && c.GetFilterProperty() != null;

            if (columns.Where(canFilter).Any())
            {
                var gridLogicalFilterOperator = columns.FirstOrDefault()?.Grid?.LogicalFilterOperator;
                var gridBooleanOperator = gridLogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                var whereList = new List<string>();
                foreach (var column in columns.Where(canFilter))
                {
                    var value = (string)Convert.ChangeType(column.FilterValue, typeof(string));
                    var secondValue = (string)Convert.ChangeType(column.SecondFilterValue, typeof(string));

                    var columnType = column.Type;
                    var columnFormat = column.Format;

                    if (!string.IsNullOrEmpty(columnType) && !string.IsNullOrEmpty(value))
                    {
                        var linqOperator = FilterOperators[column.FilterOperator];
                        if (linqOperator == null)
                        {
                            linqOperator = "==";
                        }

                        var booleanOperator = column.LogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                        if (string.IsNullOrEmpty(secondValue))
                        {
                            whereList.Add(GetColumnFilter(column));
                        }
                        else
                        {
                            whereList.Add($"({GetColumnFilter(column)} {booleanOperator} {GetColumnFilter(column, true)})");
                        }
                    }
                }

                return string.Join($" {gridBooleanOperator} ", whereList.Where(i => !string.IsNullOrEmpty(i)));
            }

            return "";
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
                !(c.GetFilterValue() == null || c.GetFilterValue() as string == string.Empty) && c.GetFilterProperty() != null;

            if (columns.Where(canFilter).Any())
            {
                var gridLogicalFilterOperator = columns.FirstOrDefault()?.Grid?.LogicalFilterOperator;
                var gridBooleanOperator = gridLogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                var whereList = new List<string>();
                foreach (var column in columns.Where(canFilter))
                {
                    string value = "";
                    string secondValue = "";

                    if (PropertyAccess.IsDate(column.FilterPropertyType))
                    {
                        var v = column.GetFilterValue();
                        var sv = column.GetSecondFilterValue();
                        if (v != null)
                        {
                            value = v is DateTime ? ((DateTime)v).ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : v is DateTimeOffset ? ((DateTimeOffset)v).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : "";
                        }
                        if (sv != null)
                        {
                            secondValue = sv is DateTime ? ((DateTime)sv).ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : sv is DateTimeOffset ? ((DateTimeOffset)sv).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : "";
                        }
                    }
                    else if (PropertyAccess.GetElementType(column.GetFilterValue().GetType()) == typeof(string))
                    {
                        var linqOperator = LinqFilterOperators[column.GetFilterOperator()];
                        if (linqOperator == null)
                        {
                            linqOperator = "==";
                        }

                        var booleanOperator = column.LogicalFilterOperator == LogicalFilterOperator.And ? " and " : " or ";

                        List<string> multivalueFilterList = new List<string>();
                        IEnumerable<string> filterValues = (IEnumerable<string>)column.GetFilterValue();
                        //Combine the linq queries for all filter values selected
                        foreach (string item in filterValues)
                        {
                            multivalueFilterList.Add(GetColumnFilter(column, item));
                        }

                        if (multivalueFilterList.Count > 0)
                            whereList.Add($"({String.Join(booleanOperator, multivalueFilterList)})");
                    }
                    else
                    {
                        value = (string)Convert.ChangeType(column.GetFilterValue(), typeof(string));
                        secondValue = (string)Convert.ChangeType(column.GetSecondFilterValue(), typeof(string));
                    }

                    if (!string.IsNullOrEmpty(value))
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
        /// Gets the column filter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="column">The column.</param>
        /// <param name="second">if set to <c>true</c> [second].</param>
        /// <returns>System.String.</returns>
        private static string GetColumnFilter<T>(RadzenGridColumn<T> column, bool second = false)
        {
            var property = PropertyAccess.GetProperty(column.GetFilterProperty());

            if (property.IndexOf(".") != -1)
            {
                property = $"({property})";
            }

            if (column.Type == "string" && string.IsNullOrEmpty(column.Format))
            {
                property = $@"({property} == null ? """" : {property})";
            }

            var columnFilterOperator = !second ? column.FilterOperator : column.SecondFilterOperator;

            var linqOperator = FilterOperators[columnFilterOperator];
            if (linqOperator == null)
            {
                linqOperator = "==";
            }

            var value = !second ? (string)Convert.ChangeType(column.FilterValue, typeof(string)) :
                (string)Convert.ChangeType(column.SecondFilterValue, typeof(string));

            var columnType = column.Type;
            var columnFormat = column.Format;

            if (columnType == "string")
            {
                string filterCaseSensitivityOperator = column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? ".ToLower()" : "";

                if (columnFormat == "date-time" || columnFormat == "date")
                {
                    var dateTimeValue = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
                    var finalDate = dateTimeValue.TimeOfDay == TimeSpan.Zero ? dateTimeValue.Date : dateTimeValue;
                    var dateFormat = dateTimeValue.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd" : "yyyy-MM-ddTHH:mm:ssZ";

                    return $@"{property} {linqOperator} DateTime(""{finalDate.ToString(dateFormat)}"")";
                }
                else if (columnFormat == "time")
                {
                    return $"{property} {linqOperator} duration'{value}'";
                }
                else if (columnFormat == "uuid")
                {
                    return $@"{property} {linqOperator} Guid(""{value}"")";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == "contains")
                {
                    return $@"({property} == null ? """" : {property}){filterCaseSensitivityOperator}.Contains(""{value}""{filterCaseSensitivityOperator})";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == "DoesNotContain")
                {
                    return $@"({property} == null ? """" : !{property}){filterCaseSensitivityOperator}.Contains(""{value}""{filterCaseSensitivityOperator})";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == "startswith")
                {
                    return $@"({property} == null ? """" : {property}){filterCaseSensitivityOperator}.StartsWith(""{value}""{filterCaseSensitivityOperator})";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == "endswith")
                {
                    return $@"({property} == null ? """" : {property}){filterCaseSensitivityOperator}.EndsWith(""{value}""{filterCaseSensitivityOperator})";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == "eq")
                {
                    return $@"({property} == null ? """" : {property}){filterCaseSensitivityOperator} == ""{value}""{filterCaseSensitivityOperator}";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == "ne")
                {
                    return $@"({property} == null ? """" : {property}){filterCaseSensitivityOperator} != ""{value}""{filterCaseSensitivityOperator}";
                }
            }
            else if (columnType == "number" || columnType == "integer")
            {
                return $"{property} {linqOperator} {value}";
            }
            else if (columnType == "boolean")
            {
                return $"{property} == {value}";
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

            var columnFilterOperator = !second ? column.GetFilterOperator() : column.GetSecondFilterOperator();

            var linqOperator = LinqFilterOperators[columnFilterOperator];
            if (linqOperator == null)
            {
                linqOperator = "==";
            }

            if (column.FilterPropertyType == typeof(string))
            {
                string filterCaseSensitivityOperator = column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? ".ToLower()" : "";

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
            }
            else if (column.FilterPropertyType == typeof(System.Enum) || column.FilterPropertyType.BaseType == typeof(System.Enum))
            {
                return $@"{property} {linqOperator} ""{value}""";
            }
            else if (PropertyAccess.IsNumeric(column.FilterPropertyType))
            {
                return $"{property} {linqOperator} {value}";
            }
            else if (column.FilterPropertyType == typeof(DateTime) ||
                    column.FilterPropertyType == typeof(DateTime?) ||
                    column.FilterPropertyType == typeof(DateTimeOffset) ||
                    column.FilterPropertyType == typeof(DateTimeOffset?))
            {
                var dateTimeValue = DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
                var finalDate = dateTimeValue.TimeOfDay == TimeSpan.Zero ? dateTimeValue.Date : dateTimeValue;
                var dateFormat = dateTimeValue.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd" : "yyyy-MM-ddTHH:mm:ssZ";
                var dateFunction = column.FilterPropertyType == typeof(DateTimeOffset) || column.FilterPropertyType == typeof(DateTimeOffset?) ? "DateTimeOffset" : "DateTime";

                return $@"{property} {linqOperator} {dateFunction}(""{finalDate.ToString(dateFormat)}"")";
            }
            else if (column.FilterPropertyType == typeof(bool) || column.FilterPropertyType == typeof(bool?))
            {
                return $"{property} == {value}";
            }
            else if (column.FilterPropertyType == typeof(Guid) || column.FilterPropertyType == typeof(Guid?))
            {
                return $@"{property} {linqOperator} Guid(""{value}"")";
            }

            return "";
        }

        /// <summary>
        /// Gets the column o data filter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="column">The column.</param>
        /// <param name="second">if set to <c>true</c> [second].</param>
        /// <returns>System.String.</returns>
        private static string GetColumnODataFilter<T>(RadzenGridColumn<T> column, bool second = false)
        {
            var columnType = column.Type;
            var columnFormat = column.Format;

            var property = column.GetFilterProperty().Replace('.', '/');

            var columnFilterOperator = !second ? column.FilterOperator : column.SecondFilterOperator;

            var value = !second ? (string)Convert.ChangeType(column.FilterValue, typeof(string)) :
                (string)Convert.ChangeType(column.SecondFilterValue, typeof(string));

            if (column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive && columnType == "string" && string.IsNullOrEmpty(columnFormat))
            {
                property = $"tolower({property})";
            }

            if (columnType == "string")
            {
                if (columnFormat == "date-time" || columnFormat == "date")
                {
                    return $"{property} {columnFilterOperator} {DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}";
                }
                else if (columnFormat == "time")
                {
                    return $"{property} {columnFilterOperator} duration'{value}'";
                }
                else if (columnFormat == "uuid")
                {
                    return $"{property} {columnFilterOperator} {value}";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == "contains")
                {
                    return column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ?
                        $"contains({property}, tolower('{value}'))" :
                        $"contains({property}, '{value}')";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == "DoesNotContain")
                {
                    return column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ?
                        $"not(contains({property}, tolower('{value}')))" :
                        $"not(contains({property}, '{value}'))";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == "startswith")
                {
                    return column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ?
                        $"startswith({property}, tolower('{value}'))" :
                        $"startswith({property}, '{value}')";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == "endswith")
                {
                    return column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ?
                        $"endswith({property}, tolower('{value}'))" :
                        $"endswith({property}, '{value}')";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == "eq")
                {
                    return column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ?
                        $"{property} eq tolower('{value}')" :
                        $"{property} eq '{value}'";
                }
                else if (!string.IsNullOrEmpty(value) && columnFilterOperator == "ne")
                {
                    return column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ?
                        $"{property} ne tolower('{value}')" :
                        $"{property} ne '{value}'";
                }
            }
            else if (columnType == "number" || columnType == "integer")
            {
                return $"{property} {columnFilterOperator} {value}";
            }
            else if (columnType == "boolean")
            {
                return $"{property} eq {value.ToLower()}";
            }

            return "";
        }

        /// <summary>
        /// Gets the column o data filter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="column">The column.</param>
        /// <param name="second">if set to <c>true</c> [second].</param>
        /// <returns>System.String.</returns>
        private static string GetColumnODataFilter<T>(RadzenDataGridColumn<T> column, bool second = false)
        {
            var property = column.GetFilterProperty().Replace('.', '/');

            var columnFilterOperator = !second ? column.GetFilterOperator() : column.GetSecondFilterOperator();

            var value = !second ? (string)Convert.ChangeType(column.GetFilterValue(), typeof(string)) :
                (string)Convert.ChangeType(column.GetSecondFilterValue(), typeof(string));

            if (column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive && column.FilterPropertyType == typeof(string))
            {
                property = $"tolower({property})";
            }

            if (column.FilterPropertyType == typeof(string))
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
            }
            else if (PropertyAccess.IsNumeric(column.FilterPropertyType))
            {
                return $"{property} {ODataFilterOperators[columnFilterOperator]} {value}";
            }
            else if (column.FilterPropertyType == typeof(bool) || column.FilterPropertyType == typeof(bool?))
            {
                return $"{property} eq {value.ToLower()}";
            }
            else if (column.FilterPropertyType == typeof(DateTime) ||
                    column.FilterPropertyType == typeof(DateTime?) ||
                    column.FilterPropertyType == typeof(DateTimeOffset) ||
                    column.FilterPropertyType == typeof(DateTimeOffset?))
            {
                return $"{property} {ODataFilterOperators[columnFilterOperator]} {DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}";
            }
            else if (column.FilterPropertyType == typeof(Guid) || column.FilterPropertyType == typeof(Guid?))
            {
                return $"{property} {ODataFilterOperators[columnFilterOperator]} {value}";
            }

            return "";
        }

        /// <summary>
        /// Converts to odatafilterstring.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columns">The columns.</param>
        /// <returns>System.String.</returns>
        public static string ToODataFilterString<T>(this IEnumerable<RadzenGridColumn<T>> columns)
        {
            Func<RadzenGridColumn<T>, bool> canFilter = (c) => c.Filterable && !string.IsNullOrEmpty(c.Type) &&
                !(c.FilterValue == null || c.FilterValue as string == string.Empty) && c.GetFilterProperty() != null;

            if (columns.Where(canFilter).Any())
            {
                var gridLogicalFilterOperator = columns.FirstOrDefault()?.Grid?.LogicalFilterOperator;
                var gridBooleanOperator = gridLogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                var whereList = new List<string>();
                foreach (var column in columns.Where(canFilter))
                {
                    var property = column.GetFilterProperty().Replace('.', '/');

                    var value = (string)Convert.ChangeType(column.FilterValue, typeof(string));
                    var secondValue = (string)Convert.ChangeType(column.SecondFilterValue, typeof(string));

                    var columnType = column.Type;
                    var columnFormat = column.Format;

                    if (!string.IsNullOrEmpty(columnType) && !string.IsNullOrEmpty(value))
                    {
                        var linqOperator = FilterOperators[column.FilterOperator];
                        if (linqOperator == null)
                        {
                            linqOperator = "==";
                        }

                        var booleanOperator = column.LogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                        if (string.IsNullOrEmpty(secondValue))
                        {
                            whereList.Add(GetColumnODataFilter(column));
                        }
                        else
                        {
                            whereList.Add($"({GetColumnODataFilter(column)} {booleanOperator} {GetColumnODataFilter(column, true)})");
                        }
                    }
                }

                return string.Join($" {gridBooleanOperator} ", whereList.Where(i => !string.IsNullOrEmpty(i)));
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
            Func<RadzenDataGridColumn<T>, bool> canFilter = (c) => c.Filterable && c.FilterPropertyType != null &&
                !(c.GetFilterValue() == null || c.GetFilterValue() as string == string.Empty) && c.GetFilterProperty() != null;

            if (columns.Where(canFilter).Any())
            {
                var gridLogicalFilterOperator = columns.FirstOrDefault()?.Grid?.LogicalFilterOperator;
                var gridBooleanOperator = gridLogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                var whereList = new List<string>();
                foreach (var column in columns.Where(canFilter))
                {
                    var property = column.GetFilterProperty().Replace('.', '/');

                    var value = (string)Convert.ChangeType(column.GetFilterValue(), typeof(string));
                    var secondValue = (string)Convert.ChangeType(column.GetSecondFilterValue(), typeof(string));

                    if (!string.IsNullOrEmpty(value))
                    {
                        var linqOperator = ODataFilterOperators[column.GetFilterOperator()];
                        if (linqOperator == null)
                        {
                            linqOperator = "==";
                        }

                        var booleanOperator = column.LogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                        if (string.IsNullOrEmpty(secondValue))
                        {
                            whereList.Add(GetColumnODataFilter(column));
                        }
                        else
                        {
                            whereList.Add($"({GetColumnODataFilter(column)} {booleanOperator} {GetColumnODataFilter(column, true)})");
                        }
                    }
                }

                return string.Join($" {gridBooleanOperator} ", whereList.Where(i => !string.IsNullOrEmpty(i)));
            }

            return "";
        }

        /// <summary>
        /// Wheres the specified columns.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="columns">The columns.</param>
        /// <returns>IQueryable&lt;T&gt;.</returns>
        public static IQueryable<T> Where<T>(this IQueryable<T> source, IEnumerable<RadzenGridColumn<T>> columns)
        {
            Func<RadzenGridColumn<T>, bool> canFilter = (c) => c.Filterable && !string.IsNullOrEmpty(c.Type) &&
                !(c.FilterValue == null || c.FilterValue as string == string.Empty) && c.GetFilterProperty() != null;

            if (columns.Where(canFilter).Any())
            {
                var gridLogicalFilterOperator = columns.FirstOrDefault()?.Grid?.LogicalFilterOperator;
                var gridBooleanOperator = gridLogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                var index = 0;
                var whereList = new Dictionary<string, IEnumerable<object>>();
                foreach (var column in columns.Where(canFilter))
                {
                    var property = PropertyAccess.GetProperty(column.GetFilterProperty());

                    if (property.IndexOf(".") != -1)
                    {
                        property = $"({property})";
                    }

                    if (column.Type == "string" && string.IsNullOrEmpty(column.Format))
                    {
                        property = $@"({property} == null ? """" : {property})";
                    }

                    string filterCaseSensitivityOperator = column.Type == "string" && string.IsNullOrEmpty(column.Format) &&
                        column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? ".ToLower()" : "";

                    var comparison = FilterOperators[column.FilterOperator];

                    var booleanOperator = column.LogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                    if (column.SecondFilterValue == null)
                    {
                        if (comparison == "StartsWith" || comparison == "EndsWith" || comparison == "Contains")
                        {
                            whereList.Add($@"{property}{filterCaseSensitivityOperator}.{comparison}(@{index}{filterCaseSensitivityOperator})", new object[] { column.FilterValue });
                            index++;
                        }
                        else if (comparison == "DoesNotContain")
                        {
                            whereList.Add($@"!{property}{filterCaseSensitivityOperator}.Contains(@{index}{filterCaseSensitivityOperator})", new object[] { column.FilterValue });
                            index++;
                        }
                        else
                        {
                            whereList.Add($@"{property}{filterCaseSensitivityOperator} {comparison} @{index}{filterCaseSensitivityOperator}", new object[] { column.FilterValue });
                            index++;
                        }
                    }
                    else
                    {
                        var firstFilter = comparison == "StartsWith" || comparison == "EndsWith" || comparison == "Contains" ?
                            $@"{property}{filterCaseSensitivityOperator}.{comparison}(@{index}{filterCaseSensitivityOperator})" :
                            comparison == "DoesNotContain" ? $@"!{property}{filterCaseSensitivityOperator}.Contains(@{index}{filterCaseSensitivityOperator})" :
                            $@"{property}{filterCaseSensitivityOperator} {comparison} @{index}{filterCaseSensitivityOperator}";
                        index++;

                        var secondComparison = FilterOperators[column.SecondFilterOperator];
                        var secondFilter = secondComparison == "StartsWith" || secondComparison == "EndsWith" || secondComparison == "Contains" ?
                            $@"{property}{filterCaseSensitivityOperator}.{secondComparison}(@{index}{filterCaseSensitivityOperator})" :
                            secondComparison == "DoesNotContain" ? $@"!{property}{filterCaseSensitivityOperator}.Contains(@{index}{filterCaseSensitivityOperator})" :
                            $@"{property}{filterCaseSensitivityOperator} {secondComparison} @{index}{filterCaseSensitivityOperator}";
                        index++;

                        whereList.Add($@"({firstFilter} {booleanOperator} {secondFilter})", new object[] { column.FilterValue, column.SecondFilterValue });
                    }
                }

                return source.Where(string.Join($" {gridBooleanOperator} ", whereList.Keys), whereList.Values.SelectMany(i => i.ToArray()).ToArray());
            }

            return source;
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
                !(c.GetFilterValue() == null || c.GetFilterValue() as string == string.Empty) && c.GetFilterProperty() != null;

            if (columns.Where(canFilter).Any())
            {
                var gridLogicalFilterOperator = columns.FirstOrDefault()?.Grid?.LogicalFilterOperator;
                var gridBooleanOperator = gridLogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                var index = 0;
                var whereList = new Dictionary<string, IEnumerable<object>>();
                foreach (var column in columns.Where(canFilter))
                {
                    var property = PropertyAccess.GetProperty(column.GetFilterProperty());

                    if (property.IndexOf(".") != -1)
                    {
                        property = $"({property})";
                    }

                    if (column.FilterPropertyType == typeof(string))
                    {
                        property = $@"({property} == null ? """" : {property})";
                    }

                    string filterCaseSensitivityOperator = column.FilterPropertyType == typeof(string) &&
                        column.Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? ".ToLower()" : "";

                    var comparison = LinqFilterOperators[column.GetFilterOperator()];

                    var booleanOperator = column.LogicalFilterOperator == LogicalFilterOperator.And ? "and" : "or";

                    if (column.GetSecondFilterValue() == null)
                    {
                        if (comparison == "StartsWith" || comparison == "EndsWith" || comparison == "Contains")
                        {
                            whereList.Add($@"{property}{filterCaseSensitivityOperator}.{comparison}(@{index}{filterCaseSensitivityOperator})", new object[] { column.GetFilterValue() });
                            index++;
                        }
                        else if (comparison == "DoesNotContain")
                        {
                            whereList.Add($@"!{property}{filterCaseSensitivityOperator}.Contains(@{index}{filterCaseSensitivityOperator})", new object[] { column.GetFilterValue() });
                            index++;
                        }
                        else
                        {
                            whereList.Add($@"{property}{filterCaseSensitivityOperator} {comparison} @{index}{filterCaseSensitivityOperator}", new object[] { column.GetFilterValue() });
                            index++;
                        }
                    }
                    else
                    {
                        var firstFilter = comparison == "StartsWith" || comparison == "EndsWith" || comparison == "Contains" ?
                            $@"{property}{filterCaseSensitivityOperator}.{comparison}(@{index}{filterCaseSensitivityOperator})" :
                            comparison == "DoesNotContain" ? $@"!{property}{filterCaseSensitivityOperator}.Contains(@{index}{filterCaseSensitivityOperator})" :
                            $@"{property}{filterCaseSensitivityOperator} {comparison} @{index}{filterCaseSensitivityOperator}";
                        index++;

                        var secondComparison = LinqFilterOperators[column.GetSecondFilterOperator()];
                        var secondFilter = secondComparison == "StartsWith" || secondComparison == "EndsWith" || secondComparison == "Contains" ?
                            $@"{property}{filterCaseSensitivityOperator}.{secondComparison}(@{index}{filterCaseSensitivityOperator})" :
                            secondComparison == "DoesNotContain" ? $@"!{property}{filterCaseSensitivityOperator}.Contains(@{index}{filterCaseSensitivityOperator})" :
                            $@"{property}{filterCaseSensitivityOperator} {secondComparison} @{index}{filterCaseSensitivityOperator}";
                        index++;

                        whereList.Add($@"({firstFilter} {booleanOperator} {secondFilter})", new object[] { column.GetFilterValue(), column.GetSecondFilterValue() });
                    }
                }

                return source.Where(string.Join($" {gridBooleanOperator} ", whereList.Keys), whereList.Values.SelectMany(i => i.ToArray()).ToArray());
            }

            return source;
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
    }
}
