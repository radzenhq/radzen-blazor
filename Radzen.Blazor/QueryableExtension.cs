using Radzen.Blazor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace Radzen
{
    public static class QueryableExtension
    {
        private static readonly IDictionary<string, string> FilterOperators = new Dictionary<string, string>
        {
            {"eq", "="},
            {"ne", "!="},
            {"lt", "<"},
            {"le", "<="},
            {"gt", ">"},
            {"ge", ">="},
            {"startswith", "StartsWith"},
            {"endswith", "EndsWith"},
            {"contains", "Contains"}
        };

        public static IList ToList(IQueryable query)
        {
            var genericToList = typeof(Enumerable).GetMethod("ToList")
                .MakeGenericMethod(new Type[] { query.ElementType });
            return (IList)genericToList.Invoke(null, new[] { query });
        }

        public static string ToFilterString<T>(this IEnumerable<RadzenGridColumn<T>> columns)
        {
            Func<RadzenGridColumn<T>, bool> canFilter = (c) => c.Filterable && !string.IsNullOrEmpty(c.Type) &&
                !(c.FilterValue == null || c.FilterValue as string == string.Empty) && c.GetFilterProperty() != null;

            if (columns.Where(canFilter).Any())
            {
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

                return string.Join(" and ", whereList.Where(i => !string.IsNullOrEmpty(i)));
            }

            return "";
        }

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
                    return $@"{property} == null ? """" : {property}{filterCaseSensitivityOperator} == {value}{filterCaseSensitivityOperator}";
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
                    return $"{property} eq {value}";
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

        public static string ToODataFilterString<T>(this IEnumerable<RadzenGridColumn<T>> columns)
        {
            Func<RadzenGridColumn<T>, bool> canFilter = (c) => c.Filterable && !string.IsNullOrEmpty(c.Type) &&
                !(c.FilterValue == null || c.FilterValue as string == string.Empty) && c.GetFilterProperty() != null;

            if (columns.Where(canFilter).Any())
            {
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

                return string.Join(" and ", whereList.Where(i => !string.IsNullOrEmpty(i)));
            }

            return "";
        }

        public static IQueryable<T> Where<T>(this IQueryable<T> source, IEnumerable<RadzenGridColumn<T>> columns)
        {
            Func<RadzenGridColumn<T>, bool> canFilter = (c) => c.Filterable && !string.IsNullOrEmpty(c.Type) &&
                !(c.FilterValue == null || c.FilterValue as string == string.Empty) && c.GetFilterProperty() != null;

            if (columns.Where(canFilter).Any())
            {
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
                            $@"{property}{filterCaseSensitivityOperator} {comparison} @{index}{filterCaseSensitivityOperator}";
                        index++;

                        var secondComparison = FilterOperators[column.SecondFilterOperator];
                        var secondFilter = secondComparison == "StartsWith" || secondComparison == "EndsWith" || secondComparison == "Contains" ?
                            $@"{property}{filterCaseSensitivityOperator}.{secondComparison}(@{index}{filterCaseSensitivityOperator})" :
                            $@"{property}{filterCaseSensitivityOperator} {secondComparison} @{index}{filterCaseSensitivityOperator}";
                        index++;

                        whereList.Add($@"({firstFilter} {booleanOperator} {secondFilter})", new object[] { column.FilterValue, column.SecondFilterValue });
                    }
                }

                return source.Where(string.Join(" and ", whereList.Keys), whereList.Values.SelectMany(i => i.ToArray()).ToArray());
            }

            return source;
        }

        public static ODataEnumerable<T> AsODataEnumerable<T>(this IEnumerable<T> source)
        {
            return new ODataEnumerable<T>(source);
        }
    }
}
