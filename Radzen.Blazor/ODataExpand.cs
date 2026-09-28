using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace Radzen
{
    internal sealed class ODataExpand(string name, string? options, ODataExpand[] children)
    {
        internal string Name { get; } = name;

        internal string? Options { get; } = options;

        internal ODataExpand[] Children { get; } = children;

        internal static (string[] Path, string? Options) Parse(LambdaExpression navigation)
        {
            var parameter = navigation.Parameters[0];
            var operations = new List<MethodCallExpression>();
            var node = ODataTranslator.Unconverted(navigation.Body);

            while (node is MethodCallExpression call && (call.Method.DeclaringType == typeof(Enumerable) || call.Method.DeclaringType == typeof(Queryable)) && call.Arguments.Count > 0)
            {
                operations.Add(call);
                node = ODataTranslator.Unconverted(call.Arguments[0]);
            }

            var path = new List<string>();
            var last = node.Type;

            while (node is MemberExpression member && member.Expression != null)
            {
                path.Add(member.Member.Name);
                node = ODataTranslator.Unconverted(member.Expression);
            }

            if (node != parameter || path.Count == 0 || last == typeof(string) || last.IsValueType)
            {
                throw new NotSupportedException($"ODataQuery cannot include {navigation.Body}: Include expands a navigation property of {parameter.Type.Name}, e.g. {parameter.Name} => {parameter.Name}.Team, optionally filtered with Where, OrderBy, ThenBy, Skip and Take; include the next level with ThenInclude.");
            }

            path.Reverse();
            operations.Reverse();

            return ([.. path], operations.Count == 0 ? null : RenderOptions(operations));
        }

        private static string? RenderOptions(List<MethodCallExpression> operations)
        {
            ODataTerm? filter = null;
            var orderBy = new List<string>();
            int? skip = null;
            int? top = null;

            foreach (var operation in operations)
            {
                var name = operation.Method.Name;

                if (name is "Where" or "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending" && (skip != null || top != null))
                {
                    throw new NotSupportedException($"ODataQuery cannot include {operation}: {name} cannot follow Skip or Take, since LINQ would apply it to the page while OData applies $skip and $top last.");
                }

                switch (name)
                {
                    case "Where" when operation.Arguments.Count == 2 && ODataTranslator.Unquote(operation.Arguments[1]).Parameters.Count == 1:
                        filter = ODataTerm.AndAlso(filter, ODataTranslator.Translate(ODataTranslator.Unquote(operation.Arguments[1])));
                        break;
                    case "OrderBy" or "OrderByDescending" when operation.Arguments.Count == 2:
                        orderBy.Clear();
                        orderBy.Add(Key(operation, name == "OrderByDescending"));
                        break;
                    case "ThenBy" or "ThenByDescending" when operation.Arguments.Count == 2:
                        orderBy.Add(Key(operation, name == "ThenByDescending"));
                        break;
                    case "Skip" when skip == null && top == null && operation.Arguments[1].Type == typeof(int):
                        skip = Count(operation);
                        break;
                    case "Take" when top == null && operation.Arguments[1].Type == typeof(int):
                        top = Count(operation);
                        break;
                    default:
                        throw new NotSupportedException($"ODataQuery cannot include {operation}: a filtered include takes Where, OrderBy, OrderByDescending, ThenBy, ThenByDescending, then at most one Skip and one Take, Skip before Take.");
                }
            }

            var options = new List<string>();

            if (filter != null)
            {
                options.Add($"$filter={filter.Text}");
            }

            if (orderBy.Count > 0)
            {
                options.Add($"$orderby={string.Join(",", orderBy)}");
            }

            if (skip != null)
            {
                options.Add($"$skip={skip.Value.ToString(CultureInfo.InvariantCulture)}");
            }

            if (top != null)
            {
                options.Add($"$top={top.Value.ToString(CultureInfo.InvariantCulture)}");
            }

            return options.Count == 0 ? null : string.Join(";", options);
        }

        private static string Key(MethodCallExpression operation, bool descending)
        {
            return ODataTranslator.Value(ODataTranslator.Unquote(operation.Arguments[1])) + (descending ? " desc" : string.Empty);
        }

        private static int Count(MethodCallExpression operation)
        {
            return ODataTranslator.Evaluate(operation.Arguments[1]) is int count && count >= 0
                ? count
                : throw new NotSupportedException($"ODataQuery cannot include {operation}: Skip and Take need a captured count that is not negative.");
        }

        internal static ODataExpand[] Add(ODataExpand[] nodes, string[] path, int index, string? options)
        {
            var name = path[index];
            var position = Array.FindIndex(nodes, node => node.Name == name);
            var existing = position >= 0 ? nodes[position] : new ODataExpand(name, null, []);
            var last = index == path.Length - 1;
            var nodeOptions = existing.Options;

            if (last && options != null)
            {
                if (nodeOptions != null && nodeOptions != options)
                {
                    throw new InvalidOperationException($"Include of {string.Join(".", path)} with ({options}) conflicts with its earlier include with ({nodeOptions}): OData expands a navigation property once, so filter, order and page it in one Include.");
                }

                nodeOptions = options;
            }

            var updated = new ODataExpand(name, nodeOptions, last ? existing.Children : Add(existing.Children, path, index + 1, options));

            if (position < 0)
            {
                return [.. nodes, updated];
            }

            var result = (ODataExpand[])nodes.Clone();
            result[position] = updated;
            return result;
        }

        internal static string Render(ODataExpand[] nodes)
        {
            return string.Join(",", nodes.Select(RenderNode));
        }

        private static string RenderNode(ODataExpand node)
        {
            var options = new List<string>();

            if (!string.IsNullOrEmpty(node.Options))
            {
                options.Add(node.Options);
            }

            if (node.Children.Length > 0)
            {
                options.Add($"$expand={Render(node.Children)}");
            }

            return options.Count == 0 ? node.Name : $"{node.Name}({string.Join(";", options)})";
        }
    }
}
