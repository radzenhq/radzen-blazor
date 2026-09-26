using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace Radzen
{
    internal sealed class ODataQueryState
    {
        internal static readonly ODataQueryState Empty = new();

        internal ODataTerm? Filter { get; private set; }

        internal string[] OrderBy { get; private set; } = [];

        internal int? Skip { get; private set; }

        internal int? Top { get; private set; }

        internal bool Count { get; private set; }

        internal ODataExpand[] Expand { get; private set; } = [];

        internal string[] IncludePath { get; private set; } = [];

        internal ODataTerm? PreFilter { get; private set; }

        internal string? Transformation { get; private set; }

        internal IReadOnlyDictionary<string, string>? Members { get; private set; }

        private ODataQueryState Copy() => (ODataQueryState)MemberwiseClone();

        internal ODataQueryState Where(LambdaExpression predicate, string method)
        {
            EnsureNotPaged(method);
            var copy = Copy();
            copy.Filter = ODataTerm.AndAlso(Filter, ODataTranslator.Translate(predicate, Members));
            return copy;
        }

        internal ODataQueryState Order(LambdaExpression keySelector, bool descending, bool then, string method)
        {
            EnsureNotPaged(method);
            var key = ODataTranslator.Value(keySelector, Members) + (descending ? " desc" : string.Empty);
            var copy = Copy();
            copy.OrderBy = then ? [.. OrderBy, key] : [key];
            return copy;
        }

        internal ODataQueryState WithSkip(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            if (Top != null)
            {
                throw new InvalidOperationException("Skip cannot follow Take: LINQ would skip items of the page that Take returned while OData skips before it takes. Call Skip before Take.");
            }

            if (Skip != null)
            {
                throw new InvalidOperationException("Skip can be called once: OData has one $skip. Pass the total count to a single Skip.");
            }

            var copy = Copy();
            copy.Skip = count;
            return copy;
        }

        internal ODataQueryState WithTop(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            if (Top != null)
            {
                throw new InvalidOperationException("Take can be called once: OData has one $top. Pass the smaller count to a single Take.");
            }

            var copy = Copy();
            copy.Top = count;
            return copy;
        }

        internal ODataQueryState WithCount()
        {
            var copy = Copy();
            copy.Count = true;
            return copy;
        }

        internal ODataQueryState Include(LambdaExpression navigation, bool then)
        {
            var (path, options) = ODataExpand.Parse(navigation);
            var full = then ? [.. IncludePath, .. path] : path;
            var copy = Copy();
            copy.Expand = ODataExpand.Add(Expand, full, 0, options);
            copy.IncludePath = full;
            return copy;
        }

        internal void EnsureGroupable()
        {
            if (Expand.Length > 0)
            {
                throw new InvalidOperationException("GroupBy cannot follow Include: an OData $apply response holds the groups, which have no navigation properties to $expand.");
            }

            if (OrderBy.Length > 0 || Skip != null || Top != null || Count)
            {
                throw new InvalidOperationException("GroupBy cannot follow OrderBy, Skip, Take or WithCount: OData applies $orderby, $skip, $top and $count to the groups that $apply returns. Call them after Select.");
            }
        }

        internal ODataQueryState Group(LambdaExpression keySelector, List<(string Component, string Path)> keys, LambdaExpression selector)
        {
            var (transformation, members) = ODataAggregate.Translate(keySelector, keys, selector);

            return new ODataQueryState
            {
                PreFilter = Filter,
                Transformation = transformation,
                Members = members,
            };
        }

        internal List<KeyValuePair<string, string>> Options(LoadDataArgs? args)
        {
            var options = new List<KeyValuePair<string, string>>();

            if (Transformation != null)
            {
                var filter = Combine(args?.Filter, PreFilter);
                var apply = (filter == null ? string.Empty : $"filter({filter})/") + Transformation + (Filter == null ? string.Empty : $"/filter({Filter.Text})");
                Add(options, "$apply", apply);
                Add(options, "$orderby", string.Join(",", OrderBy));
                Add(options, "$skip", Skip);
                Add(options, "$top", Top);
                Add(options, "$count", Count ? "true" : null);
            }
            else
            {
                Add(options, "$filter", Combine(args?.Filter, Filter));
                Add(options, "$expand", ODataExpand.Render(Expand));
                Add(options, "$orderby", string.IsNullOrEmpty(args?.OrderBy) ? string.Join(",", OrderBy) : args.OrderBy);
                Add(options, "$skip", args?.Skip ?? Skip);
                Add(options, "$top", args?.Top ?? Top);
                Add(options, "$count", Count || args != null ? "true" : null);
            }

            return options;
        }

        internal string ToQueryString(bool encoded)
        {
            return string.Join("&", Options(null).Select(option => $"{option.Key}={(encoded ? Uri.EscapeDataString(option.Value) : option.Value)}"));
        }

        private void EnsureNotPaged(string method)
        {
            if (Skip != null || Top != null)
            {
                throw new InvalidOperationException($"{method} cannot follow Skip or Take: LINQ would apply it to the page while OData applies $skip and $top last. Call {method} before Skip and Take.");
            }
        }

        private static string? Combine(string? grid, ODataTerm? typed)
        {
            if (string.IsNullOrEmpty(grid))
            {
                return typed?.Text;
            }

            return typed == null ? grid : $"({grid}) and ({typed.Text})";
        }

        private static void Add(List<KeyValuePair<string, string>> options, string name, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                options.Add(new(name, value));
            }
        }

        private static void Add(List<KeyValuePair<string, string>> options, string name, int? value)
        {
            if (value != null)
            {
                options.Add(new(name, value.Value.ToString(CultureInfo.InvariantCulture)));
            }
        }
    }
}
