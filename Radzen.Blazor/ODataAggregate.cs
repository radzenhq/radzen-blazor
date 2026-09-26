using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Radzen
{
    internal static class ODataAggregate
    {
        private const string Selectable = "a grouped Select assigns g.Key, the members of a composite g.Key and the aggregates g.Count(), g.Sum(x => ...), g.Min(x => ...), g.Max(x => ...), g.Average(x => ...) and g.Select(x => ...).Distinct().Count() to the members of a new result.";

        internal static (string GroupBy, Dictionary<string, string> Members) Translate(LambdaExpression keySelector, List<(string Component, string Path)> keys, LambdaExpression selector)
        {
            var group = selector.Parameters[0];
            var members = new Dictionary<string, string>();
            var aggregates = new List<string>();

            foreach (var (name, value) in Bindings(selector))
            {
                var node = ODataTranslator.Unconverted(value);

                if (Component(node, group) is { } component)
                {
                    if (keys.Count == 0)
                    {
                        throw new NotSupportedException($"ODataQuery cannot select {name} = {value}: a constant key puts every entity in one group and OData returns no value for it; select only aggregates.");
                    }

                    var path = keys.First(key => key.Component == component).Path;

                    if (path.Contains('/', StringComparison.Ordinal))
                    {
                        throw new NotSupportedException($"ODataQuery cannot select {name} = {value}: OData returns the group key {path} nested under {path[..path.IndexOf('/', StringComparison.Ordinal)]}, so it cannot fill {name}; group by a property of {keySelector.Parameters[0].Type.Name} instead.");
                    }

                    if (path != name)
                    {
                        throw new NotSupportedException($"ODataQuery cannot select {name} = {value}: OData returns the group key under its own name, so name the member {path}.");
                    }

                    members[name] = path;
                }
                else if (node is MethodCallExpression call && Aggregate(call, group) is { } aggregate)
                {
                    aggregates.Add($"{aggregate} as {name}");
                    members[name] = name;
                }
                else
                {
                    throw new NotSupportedException($"ODataQuery cannot select {name} = {value}: {Selectable}");
                }
            }

            if (keys.Count == 0)
            {
                return aggregates.Count > 0
                    ? ($"aggregate({string.Join(",", aggregates)})", members)
                    : throw new NotSupportedException($"ODataQuery cannot select {selector.Body}: a constant key puts every entity in one group, so select at least one aggregate, e.g. g => new Total {{ Amount = g.Sum(x => x.Price) }}.");
            }

            var groupBy = $"groupby(({string.Join(",", keys.Select(key => key.Path).Distinct())}){(aggregates.Count > 0 ? $",aggregate({string.Join(",", aggregates)})" : string.Empty)})";

            return (groupBy, members);
        }

        internal static List<(string Component, string Path)> Keys(LambdaExpression keySelector)
        {
            if (ODataTranslator.IsConstant(keySelector))
            {
                return [];
            }

            var body = ODataTranslator.Unconverted(keySelector.Body);

            return body switch
            {
                NewExpression key when key.Arguments.Count > 0 => key.Arguments
                    .Select((argument, index) => (Name(key.Members?[index], index), ODataTranslator.Path(keySelector, argument)))
                    .ToList(),
                MemberInitExpression key => key.Bindings
                    .Select(binding => binding is MemberAssignment assignment
                        ? (assignment.Member.Name, ODataTranslator.Path(keySelector, assignment.Expression))
                        : throw new NotSupportedException($"ODataQuery cannot group by {binding}: a composite key assigns properties, e.g. x => new {{ x.Currency, x.Country }}."))
                    .ToList(),
                _ => [(string.Empty, ODataTranslator.Path(keySelector, keySelector.Body))],
            };
        }

        private static string Name(MemberInfo? member, int index)
        {
            if (member == null)
            {
                return $"Item{index + 1}";
            }

            return member.Name.StartsWith("get_", StringComparison.Ordinal) ? member.Name[4..] : member.Name;
        }

        private static IEnumerable<(string Name, Expression Value)> Bindings(LambdaExpression selector)
        {
            switch (selector.Body)
            {
                case MemberInitExpression { NewExpression.Arguments.Count: 0 } result:
                    return result.Bindings.Select(binding => binding is MemberAssignment assignment
                        ? (assignment.Member.Name, assignment.Expression)
                        : throw new NotSupportedException($"ODataQuery cannot select {binding}: {Selectable}"));
                case NewExpression { Members: { } members } result:
                    return result.Arguments.Select((argument, index) => (Name(members[index], index), argument));
                default:
                    throw new NotSupportedException($"ODataQuery cannot select {selector.Body}: {Selectable} For example g => new Total {{ Currency = g.Key, Amount = g.Sum(x => x.Price) }}.");
            }
        }

        private static string? Component(Expression node, ParameterExpression group)
        {
            return node switch
            {
                MemberExpression { Member.Name: "Key", Expression: ParameterExpression parameter } when parameter == group => string.Empty,
                MemberExpression { Expression: MemberExpression { Member.Name: "Key", Expression: ParameterExpression parameter } } member when parameter == group => member.Member.Name,
                _ => null,
            };
        }

        private static string? Aggregate(MethodCallExpression call, ParameterExpression group)
        {
            if (call.Method.DeclaringType != typeof(Enumerable))
            {
                return null;
            }

            var arguments = call.Arguments;
            var source = ODataTranslator.Unconverted(arguments[0]);

            switch (call.Method.Name)
            {
                case "Count" or "LongCount" when arguments.Count == 1 && source == group:
                    return "$count";
                case "Count" or "LongCount" when arguments.Count == 1
                    && source is MethodCallExpression { Method.Name: "Distinct", Arguments: [var distinct] }
                    && ODataTranslator.Unconverted(distinct) is MethodCallExpression { Method.Name: "Select", Arguments: [var items, var selector] }
                    && ODataTranslator.Unconverted(items) == group:
                    return $"{ODataTranslator.Aggregated(ODataTranslator.Unquote(selector))} with countdistinct";
                case "Sum" or "Min" or "Max" or "Average" when arguments.Count == 2 && source == group:
                    return $"{ODataTranslator.Aggregated(ODataTranslator.Unquote(arguments[1]))} with {call.Method.Name.ToLowerInvariant()}";
                default:
                    return null;
            }
        }
    }
}
