using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Radzen
{
    internal sealed class ODataTranslator
    {
        private const string Supported = "ODataQuery translates comparisons, &&, ||, !, null checks, arithmetic, enums and Enum.HasFlag, captured values, the string methods Contains, StartsWith, EndsWith, IndexOf, Substring, ToLower, ToUpper, Trim and Length, Any, All and Count on collections, Contains on a captured list, date parts, Math.Floor, Math.Ceiling and Math.Round with MidpointRounding.AwayFromZero.";

        private readonly Dictionary<ParameterExpression, string> scope = [];
        private readonly IReadOnlyDictionary<string, string>? rootMembers;
        private readonly Expression? aggregated;

        private ODataTranslator(ParameterExpression root, IReadOnlyDictionary<string, string>? rootMembers, Expression? aggregated = null)
        {
            scope[root] = string.Empty;
            this.rootMembers = rootMembers;
            this.aggregated = aggregated;
        }

        internal static ODataTerm Translate(LambdaExpression lambda, IReadOnlyDictionary<string, string>? rootMembers = null)
        {
            return new ODataTranslator(lambda.Parameters[0], rootMembers).Visit(lambda.Body);
        }

        internal static string Filter(LambdaExpression predicate)
        {
            return Translate(predicate).Text;
        }

        internal static string Value(LambdaExpression selector, IReadOnlyDictionary<string, string>? rootMembers = null)
        {
            var term = Translate(selector, rootMembers);

            if (term.IsConstant || term.IsRoot)
            {
                throw new NotSupportedException($"ODataQuery cannot order or aggregate by {selector.Body}: select a property of {selector.Parameters[0].Type.Name}, e.g. {selector.Parameters[0].Name} => {selector.Parameters[0].Name}.Name.");
            }

            return term.Text;
        }

        internal static string Aggregated(LambdaExpression selector)
        {
            var term = new ODataTranslator(selector.Parameters[0], null, selector.Body).Visit(selector.Body);

            if (term.IsConstant || term.IsRoot)
            {
                throw new NotSupportedException($"ODataQuery cannot aggregate {selector.Body}: aggregate a property of {selector.Parameters[0].Type.Name} or an expression of its properties, e.g. {selector.Parameters[0].Name} => {selector.Parameters[0].Name}.Price * {selector.Parameters[0].Name}.Quantity.");
            }

            return term.Precedence < ODataTerm.Primary ? $"({term.Text})" : term.Text;
        }

        internal static bool IsConstant(LambdaExpression selector)
        {
            return !new ODataTranslator(selector.Parameters[0], null).Uses(selector.Body);
        }

        internal static string Path(LambdaExpression selector, Expression body)
        {
            var term = new ODataTranslator(selector.Parameters[0], null).Visit(body);

            if (!term.IsPath || term.IsRoot)
            {
                throw new NotSupportedException($"ODataQuery cannot group by {body}: OData groups by property paths, so select a property of {selector.Parameters[0].Type.Name} or of a record it references, e.g. {selector.Parameters[0].Name} => {selector.Parameters[0].Name}.Team.Name.");
            }

            return term.Text;
        }

        internal static object? Evaluate(Expression node)
        {
            switch (node)
            {
                case ConstantExpression constant:
                    return constant.Value;
                case MemberExpression { Member: FieldInfo field } member:
                    return field.GetValue(member.Expression == null ? null : Evaluate(member.Expression));
                case MemberExpression { Member.Name: "HasValue", Expression: { } nullable } when Nullable.GetUnderlyingType(nullable.Type) != null:
                    return Evaluate(nullable) != null;
                case MemberExpression { Member.Name: "Value", Expression: { } nullable } when Nullable.GetUnderlyingType(nullable.Type) != null:
                    return Evaluate(nullable) ?? throw new InvalidOperationException($"{nullable} has no value.");
                case MemberExpression { Member: PropertyInfo property } member when property.GetIndexParameters().Length == 0:
                    return property.GetValue(member.Expression == null ? null : Evaluate(member.Expression));
                case UnaryExpression { NodeType: ExpressionType.Convert, Method: null } convert when convert.Type == typeof(object) || Nullable.GetUnderlyingType(convert.Type) == convert.Operand.Type:
                    return Evaluate(convert.Operand);
                case MethodCallExpression call when IsSpanContains(call):
                    var value = Evaluate(call.Arguments[1]);
                    return Items(Evaluate(SpanSource(call.Arguments[0])), call).Cast<object?>().Any(item => Equals(item, value));
                default:
                    return Expression.Lambda<Func<object?>>(Expression.Convert(node, typeof(object))).Compile(preferInterpretation: true)();
            }
        }

        private ODataTerm Visit(Expression node)
        {
            if (!Uses(node))
            {
                return ODataTerm.Constant(Evaluate(node));
            }

            return node switch
            {
                BinaryExpression binary => Binary(binary),
                UnaryExpression unary => Unary(unary),
                MemberExpression member => Member(member),
                MethodCallExpression call => Call(call),
                ParameterExpression parameter => ODataTerm.Path(scope[parameter]),
                ConditionalExpression => throw Unsupported(node, "OData has no conditional operator; write the condition with && and ||, e.g. (x.Urgent && x.Priority > 1) || (!x.Urgent && x.Priority > 3)."),
                TypeBinaryExpression => throw Unsupported(node, "OData type checks are not supported; filter by a property instead."),
                InvocationExpression => throw Unsupported(node, "a lambda or delegate cannot be invoked in OData; write the condition inline."),
                _ => throw Unsupported(node, Supported),
            };
        }

        private ODataTerm Binary(BinaryExpression binary)
        {
            switch (binary.NodeType)
            {
                case ExpressionType.AndAlso:
                case ExpressionType.And when IsBoolean(binary.Type):
                    return Logical(binary.Left, binary.Right, true);
                case ExpressionType.OrElse:
                case ExpressionType.Or when IsBoolean(binary.Type):
                    return Logical(binary.Left, binary.Right, false);
                case ExpressionType.Equal:
                    return Compare(binary, "eq");
                case ExpressionType.NotEqual:
                    return Compare(binary, "ne");
                case ExpressionType.LessThan:
                    return Compare(binary, "lt");
                case ExpressionType.LessThanOrEqual:
                    return Compare(binary, "le");
                case ExpressionType.GreaterThan:
                    return Compare(binary, "gt");
                case ExpressionType.GreaterThanOrEqual:
                    return Compare(binary, "ge");
                case ExpressionType.Add or ExpressionType.AddChecked when binary.Type == typeof(string):
                    return Function(binary, "concat", binary.Left, binary.Right);
                case ExpressionType.Add or ExpressionType.AddChecked:
                    return Arithmetic(binary, "add", ODataTerm.Additive, true);
                case ExpressionType.Subtract or ExpressionType.SubtractChecked:
                    return Arithmetic(binary, "sub", ODataTerm.Additive, false);
                case ExpressionType.Multiply or ExpressionType.MultiplyChecked:
                    return Arithmetic(binary, "mul", ODataTerm.Multiplicative, true);
                case ExpressionType.Divide:
                    return Arithmetic(binary, "div", ODataTerm.Multiplicative, false);
                case ExpressionType.Modulo:
                    return Arithmetic(binary, "mod", ODataTerm.Multiplicative, false);
                case ExpressionType.Coalesce:
                    throw Unsupported(binary, "OData has no ?? operator; compare with null explicitly, e.g. x.Price == null || x.Price > 5.");
                default:
                    throw Unsupported(binary, Supported);
            }
        }

        private ODataTerm Logical(Expression leftNode, Expression rightNode, bool and)
        {
            var left = Visit(leftNode);

            if (left.IsConstant && left.Value is bool leftValue)
            {
                return leftValue == and ? Visit(rightNode) : left;
            }

            var right = Visit(rightNode);

            if (right.IsConstant && right.Value is bool rightValue)
            {
                return rightValue == and ? left : right;
            }

            return ODataTerm.Join(Operand(leftNode, left, 0), Operand(rightNode, right, 0), and);
        }

        private ODataTerm Operand(Expression node, ODataTerm term, int precedence)
        {
            return ODataTerm.Expression(Render(node, term, precedence, null, false), term.Precedence);
        }

        private ODataTerm Compare(BinaryExpression comparison, string name)
        {
            if (comparison.Method != null && !IsScalar(comparison.Method.DeclaringType))
            {
                throw Unsupported(comparison, $"the {comparison.Method.DeclaringType?.Name} {comparison.NodeType} operator has no OData equivalent; compare its properties instead.");
            }

            var left = Visit(comparison.Left);
            var right = Visit(comparison.Right);
            var type = Context(left.IsConstant ? comparison.Right : comparison.Left);

            return ODataTerm.Expression($"{Render(comparison.Left, left, ODataTerm.Comparison + 1, type, right.IsDate)} {name} {Render(comparison.Right, right, ODataTerm.Comparison + 1, type, left.IsDate)}", ODataTerm.Comparison);
        }

        private ODataTerm Arithmetic(BinaryExpression binary, string name, int precedence, bool associative)
        {
            if (binary.Method != null && !IsScalar(binary.Method.DeclaringType))
            {
                throw Unsupported(binary, $"the {binary.Method.DeclaringType?.Name} {binary.NodeType} operator has no OData equivalent.");
            }

            var left = Visit(binary.Left);
            var right = Visit(binary.Right);
            var type = Context(left.IsConstant ? binary.Right : binary.Left);

            return ODataTerm.Expression($"{Render(binary.Left, left, precedence, type, false)} {name} {Render(binary.Right, right, associative ? precedence : precedence + 1, type, false)}", precedence);
        }

        private ODataTerm Unary(UnaryExpression unary)
        {
            switch (unary.NodeType)
            {
                case ExpressionType.Not when IsBoolean(unary.Type):
                    if (unary.Operand is MemberExpression { Member.Name: "HasValue", Expression: { } nullable } && Nullable.GetUnderlyingType(nullable.Type) != null)
                    {
                        return ODataTerm.Expression($"{Render(nullable, Visit(nullable), ODataTerm.Comparison + 1, null, false)} eq null", ODataTerm.Comparison);
                    }

                    var operand = Visit(unary.Operand);

                    if (operand.IsConstant && operand.Value is bool value)
                    {
                        return ODataTerm.Constant(!value);
                    }

                    var text = Render(unary.Operand, operand, 0, null, false);

                    return ODataTerm.Expression(operand.Precedence >= ODataTerm.Unary ? $"not {text}" : $"not ({text})", ODataTerm.Unary);
                case ExpressionType.Negate or ExpressionType.NegateChecked:
                    return ODataTerm.Expression($"-{Render(unary.Operand, Visit(unary.Operand), ODataTerm.Unary + 1, null, false)}", ODataTerm.Unary);
                case ExpressionType.UnaryPlus:
                    return Visit(unary.Operand);
                case ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs when IsOpenProperty(unary.Operand):
                    return Visit(unary.Operand);
                case ExpressionType.Convert or ExpressionType.ConvertChecked when IsNumericConversion(unary) && Cast(unary) is { } edm:
                    return ODataTerm.Expression($"cast({Render(unary.Operand, Visit(unary.Operand), 0, null, false)},{edm})", ODataTerm.Primary);
                case ExpressionType.Convert or ExpressionType.ConvertChecked when IsNumericConversion(unary) && Widens(unary.Operand.Type, unary.Type):
                    return Visit(unary.Operand);
                case ExpressionType.Convert or ExpressionType.ConvertChecked when unary.Method == null && !unary.Operand.Type.IsValueType && unary.Type.IsAssignableFrom(unary.Operand.Type):
                    return Visit(unary.Operand);
                case ExpressionType.Convert or ExpressionType.ConvertChecked:
                    throw Unsupported(unary, $"OData has no conversion from {unary.Operand.Type.Name} to {unary.Type.Name}; compare values of the same type, or use Math.Floor, Math.Ceiling or Math.Round(value, MidpointRounding.AwayFromZero) to drop a fraction.");
                case ExpressionType.ArrayLength:
                    return ODataTerm.Expression($"{Collection(unary, unary.Operand)}/$count", ODataTerm.Primary);
                default:
                    throw Unsupported(unary, Supported);
            }
        }

        private ODataTerm Member(MemberExpression member)
        {
            var owner = member.Expression!;
            var name = member.Member.Name;

            if (Nullable.GetUnderlyingType(owner.Type) != null)
            {
                if (name == "HasValue")
                {
                    return ODataTerm.Expression($"{Render(owner, Visit(owner), ODataTerm.Comparison + 1, null, false)} ne null", ODataTerm.Comparison);
                }

                if (name == "Value")
                {
                    return Visit(owner);
                }
            }

            if (owner.Type == typeof(string) && name == "Length")
            {
                return Function(member, "length", owner);
            }

            if (DatePart(owner.Type, name) is { } function)
            {
                return name == "Date" ? ODataTerm.Date($"date({Render(owner, Visit(owner), 0, null, false)})") : Function(member, function, owner);
            }

            if (name == "Count" && IsCollection(owner.Type))
            {
                return ODataTerm.Expression($"{Collection(member, owner)}/$count", ODataTerm.Primary);
            }

            var path = Visit(owner);

            if (!path.IsPath)
            {
                throw Unsupported(member, $"OData reads {name} only from the item, a record it references or a lambda variable, not from {owner}.");
            }

            if (!path.IsRoot)
            {
                return ODataTerm.Path($"{path.Text}/{name}");
            }

            if (rootMembers == null)
            {
                return ODataTerm.Path(name);
            }

            return rootMembers.TryGetValue(name, out var mapped)
                ? ODataTerm.Path(mapped)
                : throw Unsupported(member, $"{name} is not a group key or an aggregate of the Select that made the result.");
        }

        private ODataTerm Call(MethodCallExpression call)
        {
            var method = call.Method;
            var type = method.DeclaringType;

            if (type == typeof(string))
            {
                return StringCall(call);
            }

            if (type == typeof(Enumerable) || type == typeof(Queryable))
            {
                return EnumerableCall(call);
            }

            if (IsSpanContains(call))
            {
                return In(call, SpanSource(call.Arguments[0]), call.Arguments[1]);
            }

            if (type == typeof(Math))
            {
                return MathCall(call);
            }

            if (type == typeof(Enum) && method.Name == "HasFlag" && call.Object != null && !Uses(call.Arguments[0]))
            {
                var flags = Unconverted(call.Object);
                return ODataTerm.Expression($"{Render(flags, Visit(flags), ODataTerm.Primary, null, false)} has {ODataTerm.Literal(Evaluate(Unconverted(call.Arguments[0])))}", ODataTerm.Comparison);
            }

            if (method.Name == "Contains" && call.Object != null && call.Arguments.Count == 1 && IsCollection(call.Object.Type))
            {
                return In(call, call.Object, call.Arguments[0]);
            }

            if (IsOpenProperty(call))
            {
                return OpenProperty(call);
            }

            if (method.Name == "Equals" && call.Object != null && call.Arguments.Count == 1 && IsScalar(type))
            {
                return Equality(call, call.Object, Unconverted(call.Arguments[0]));
            }

            throw Unsupported(call, $"{type?.Name}.{method.Name} has no OData equivalent. {Supported}");
        }

        private ODataTerm StringCall(MethodCallExpression call)
        {
            var text = call.Object;
            var arguments = call.Arguments;

            switch (call.Method.Name)
            {
                case "Contains" or "StartsWith" or "EndsWith" when text != null && arguments.Count == 1 && IsText(arguments[0].Type):
                    return Function(call, call.Method.Name.ToLowerInvariant(), text, arguments[0]);
                case "Contains" or "StartsWith" or "EndsWith" when text != null && arguments.Count == 2 && IsText(arguments[0].Type) && arguments[1].Type == typeof(StringComparison):
                    return Comparison(call, call.Method.Name.ToLowerInvariant(), text, arguments[0], arguments[1]);
                case "IndexOf" when text != null && arguments.Count == 1 && IsText(arguments[0].Type):
                    return Function(call, "indexof", text, arguments[0]);
                case "IndexOf" when text != null && arguments.Count == 2 && IsText(arguments[0].Type) && arguments[1].Type == typeof(StringComparison):
                    return Comparison(call, "indexof", text, arguments[0], arguments[1]);
                case "Substring" when text != null && arguments.Count is 1 or 2:
                    return Function(call, "substring", [text, .. arguments]);
                case "ToLower" or "ToLowerInvariant" when text != null && arguments.Count == 0:
                    return Function(call, "tolower", text);
                case "ToUpper" or "ToUpperInvariant" when text != null && arguments.Count == 0:
                    return Function(call, "toupper", text);
                case "Trim" when text != null && arguments.Count == 0:
                    return Function(call, "trim", text);
                case "IsNullOrEmpty" when text == null && arguments.Count == 1:
                    var value = Render(arguments[0], Visit(arguments[0]), ODataTerm.Comparison + 1, null, false);
                    return ODataTerm.Expression($"{value} eq null or {value} eq ''", ODataTerm.Or);
                case "Concat" when text == null && arguments.Count is >= 2 and <= 4 && arguments.All(argument => Unconverted(argument).Type == typeof(string)):
                    var concat = Visit(Unconverted(arguments[0]));
                    for (var index = 1; index < arguments.Count; index++)
                    {
                        var next = Unconverted(arguments[index]);
                        concat = ODataTerm.Expression($"concat({Render(arguments[0], concat, 0, null, false)},{Render(next, Visit(next), 0, null, false)})", ODataTerm.Primary);
                    }
                    return concat;
                case "Equals" when text != null && arguments.Count == 1 && arguments[0].Type == typeof(string):
                    return Equality(call, text, arguments[0]);
                case "Equals" when text == null && arguments.Count == 2 && arguments[1].Type == typeof(string):
                    return Equality(call, arguments[0], arguments[1]);
                case "Compare" or "CompareTo" or "CompareOrdinal":
                    throw Unsupported(call, "OData compares strings with eq, ne, lt, le, gt and ge only through ==, != or string methods; use == or StartsWith instead.");
                default:
                    throw Unsupported(call, $"string.{call.Method.Name} with these arguments has no OData equivalent; OData supports Contains, StartsWith, EndsWith, IndexOf, Substring, ToLower, ToUpper, Trim, Length, string.IsNullOrEmpty and string.Concat.");
            }
        }

        private ODataTerm Comparison(MethodCallExpression call, string function, Expression text, Expression value, Expression comparison)
        {
            if (Uses(comparison))
            {
                throw Unsupported(call, "the StringComparison must be a constant.");
            }

            switch ((StringComparison)Evaluate(comparison)!)
            {
                case StringComparison.Ordinal:
                    return Function(call, function, text, value);
                case StringComparison.OrdinalIgnoreCase:
                    return ODataTerm.Expression($"{function}(tolower({Render(text, Visit(text), 0, null, false)}),tolower({Render(value, Visit(value), 0, null, false)}))", ODataTerm.Primary);
                default:
                    throw Unsupported(call, "OData compares strings ordinally or as the server's collation does, never by a .NET culture; use StringComparison.Ordinal, StringComparison.OrdinalIgnoreCase or ToLower() on both sides.");
            }
        }

        private ODataTerm Equality(Expression node, Expression left, Expression right)
        {
            if (left.Type != right.Type)
            {
                throw Unsupported(node, "use == to compare two values of the same type.");
            }

            return Compare(Expression.MakeBinary(ExpressionType.Equal, left, right, false, null), "eq");
        }

        private ODataTerm EnumerableCall(MethodCallExpression call)
        {
            var arguments = call.Arguments;

            switch (call.Method.Name)
            {
                case "Any" when arguments.Count == 1:
                    return ODataTerm.Expression($"{Collection(call, arguments[0])}/any()", ODataTerm.Primary);
                case "Any" or "All" when arguments.Count == 2:
                    return Lambda(call, call.Method.Name == "Any", arguments[0], Unquote(arguments[1]));
                case "Count" or "LongCount" when arguments.Count == 1:
                    return ODataTerm.Expression($"{Collection(call, arguments[0])}/$count", ODataTerm.Primary);
                case "Count" or "LongCount":
                    throw Unsupported(call, "OData counts all items of a collection; use Any(predicate) to test for a match, or Count() without a predicate.");
                case "Contains" when arguments.Count == 2:
                    return In(call, arguments[0], arguments[1]);
                default:
                    throw Unsupported(call, $"Enumerable.{call.Method.Name} has no OData equivalent in a filter; OData supports Any, All, Count and Contains on a captured list.");
            }
        }

        private ODataTerm Lambda(MethodCallExpression call, bool any, Expression source, LambdaExpression predicate)
        {
            var collection = Collection(call, source);
            var parameter = predicate.Parameters[0];
            var name = Allocate(parameter.Name);

            scope[parameter] = name;

            ODataTerm body;

            try
            {
                body = Visit(predicate.Body);
            }
            finally
            {
                scope.Remove(parameter);
            }

            if (body.IsConstant && body.Value is bool value)
            {
                if (any)
                {
                    return value ? ODataTerm.Expression($"{collection}/any()", ODataTerm.Primary) : body;
                }

                return value ? body : ODataTerm.Expression($"not {collection}/any()", ODataTerm.Unary);
            }

            return ODataTerm.Expression($"{collection}/{(any ? "any" : "all")}({name}:{Render(predicate.Body, body, 0, null, false)})", ODataTerm.Primary);
        }

        private string Allocate(string? name)
        {
            var candidate = IsIdentifier(name) ? name! : "x";
            var unique = candidate;

            for (var index = 1; scope.ContainsValue(unique); index++)
            {
                unique = $"{candidate}{index}";
            }

            return unique;
        }

        private string Collection(Expression node, Expression source)
        {
            var collection = Visit(source);

            if (!collection.IsPath || collection.IsRoot)
            {
                throw Unsupported(node, $"OData applies any, all and $count to a collection property of the item, not to {source}.");
            }

            return collection.Text;
        }

        private ODataTerm In(Expression node, Expression source, Expression value)
        {
            if (Uses(source))
            {
                throw Unsupported(node, $"OData tests a captured list with in, not a collection property; use {source}.Any(item => item == value) instead.");
            }

            var type = Context(value);
            var term = Visit(value);
            var items = Items(Evaluate(source), node).Cast<object?>().Select(item => Literal(item, type, term.IsDate)).ToList();

            if (items.Count == 0)
            {
                return ODataTerm.Constant(false);
            }

            return ODataTerm.Expression($"{Render(value, term, ODataTerm.Comparison + 1, type, false)} in ({string.Join(",", items)})", ODataTerm.Comparison);
        }

        private ODataTerm MathCall(MethodCallExpression call)
        {
            var arguments = call.Arguments;

            switch (call.Method.Name)
            {
                case "Floor" or "Ceiling" when arguments.Count == 1:
                    return Function(call, call.Method.Name.ToLowerInvariant(), arguments[0]);
                case "Round" when arguments.Count == 2 && arguments[1].Type == typeof(MidpointRounding) && !Uses(arguments[1]) && Evaluate(arguments[1]) is MidpointRounding.AwayFromZero:
                    return Function(call, "round", arguments[0]);
                case "Round":
                    throw Unsupported(call, "OData round() rounds a midpoint away from zero to a whole number; use Math.Round(value, MidpointRounding.AwayFromZero).");
                default:
                    throw Unsupported(call, $"Math.{call.Method.Name} has no OData equivalent; OData supports Math.Floor, Math.Ceiling and Math.Round(value, MidpointRounding.AwayFromZero).");
            }
        }

        private ODataTerm OpenProperty(MethodCallExpression call)
        {
            var container = (MemberExpression)call.Object!;
            var key = call.Arguments[0];

            if (Uses(key) || Evaluate(key) is not string name || !IsIdentifier(name))
            {
                throw Unsupported(call, "the key of a dynamic property must be a constant OData identifier, e.g. x.Data[\"remaining\"].");
            }

            var owner = Visit(container.Expression!);

            if (!owner.IsPath)
            {
                throw Unsupported(call, $"OData reads a dynamic property only from the item, a record it references or a lambda variable, not from {container.Expression}.");
            }

            return ODataTerm.Path(owner.IsRoot ? name : $"{owner.Text}/{name}");
        }

        private ODataTerm Function(Expression node, string name, params Expression[] arguments)
        {
            return ODataTerm.Expression($"{name}({string.Join(",", arguments.Select(argument => Render(argument, Visit(argument), 0, null, false)))})", ODataTerm.Primary);
        }

        private static string Render(Expression node, ODataTerm term, int precedence, Type? type, bool date)
        {
            if (term.IsConstant)
            {
                if (term.Value != null && !IsScalar(term.Value.GetType()))
                {
                    throw Unsupported(node, "OData compares values, not whole records; compare a property such as the key instead.");
                }

                return Literal(term.Value, type, date);
            }

            if (term.IsRoot)
            {
                throw Unsupported(node, "OData compares values, not whole records; compare a property such as the key instead.");
            }

            return term.Precedence < precedence ? $"({term.Text})" : term.Text;
        }

        private static string Literal(object? value, Type? type, bool date)
        {
            if (type != null && type.IsEnum && value != null && value is not Enum && IsIntegral(value))
            {
                value = Enum.ToObject(type, value);
            }
            else if (type != null && !type.IsEnum && type.IsPrimitive && value is Enum member)
            {
                value = Convert.ChangeType(member, Enum.GetUnderlyingType(member.GetType()), System.Globalization.CultureInfo.InvariantCulture);
            }

            if (date && value is DateTime day)
            {
                return day.TimeOfDay == TimeSpan.Zero
                    ? DateOnly.FromDateTime(day).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                    : throw new NotSupportedException($"ODataQuery cannot compare date() with {day:O}, which has a time of day: compare it with the date alone, e.g. value.Date.");
            }

            return ODataTerm.Literal(value);
        }

        private bool Uses(Expression node)
        {
            var finder = new ParameterFinder(scope);

            finder.Visit(node);

            if (finder.Foreign != null)
            {
                throw Unsupported(node, $"it uses {finder.Foreign.Name}, a parameter of an outer lambda that OData cannot reach here; use a captured value or the lambda's own parameter.");
            }

            return finder.Found;
        }

        private static NotSupportedException Unsupported(Expression node, string alternative)
        {
            return new NotSupportedException($"ODataQuery cannot translate {node} to OData: {alternative}");
        }

        internal static LambdaExpression Unquote(Expression node)
        {
            return (LambdaExpression)(node is UnaryExpression { NodeType: ExpressionType.Quote } quote ? quote.Operand : node);
        }

        private static IEnumerable Items(object? source, Expression node)
        {
            return source as IEnumerable ?? throw Unsupported(node, "the captured list is null.");
        }

        private static bool IsSpanContains(MethodCallExpression call)
        {
            return call.Method.DeclaringType == typeof(MemoryExtensions) && call.Method.Name == "Contains" && call.Arguments.Count is 2 or 3
                && (call.Arguments.Count == 2 || call.Arguments[2] is ConstantExpression { Value: null });
        }

        private static Expression SpanSource(Expression node)
        {
            return node switch
            {
                MethodCallExpression { Method.Name: "op_Implicit", Arguments: [var array] } => array,
                UnaryExpression { NodeType: ExpressionType.Convert, Method.Name: "op_Implicit" } convert => convert.Operand,
                _ => node,
            };
        }

        private static bool IsOpenProperty(Expression node)
        {
            return node is MethodCallExpression { Method.Name: "get_Item", Object: MemberExpression { Expression: not null } container, Arguments: [var key] }
                && key.Type == typeof(string)
                && typeof(IDictionary<string, object>).IsAssignableFrom(container.Type);
        }

        private static Type Context(Expression node)
        {
            var type = Unconverted(node).Type;
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        internal static Expression Unconverted(Expression node)
        {
            return node is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert ? Unconverted(convert.Operand) : node;
        }

        private static bool Widens(Type from, Type to)
        {
            from = Nullable.GetUnderlyingType(from) ?? from;
            to = Nullable.GetUnderlyingType(to) ?? to;
            return from == to || Type.GetTypeCode(from) >= TypeCode.SByte && Type.GetTypeCode(to) >= Type.GetTypeCode(from) && Type.GetTypeCode(to) <= TypeCode.Decimal;
        }

        private string? Cast(UnaryExpression convert)
        {
            if (aggregated == null)
            {
                return null;
            }

            var from = Nullable.GetUnderlyingType(convert.Operand.Type) ?? convert.Operand.Type;
            var to = Nullable.GetUnderlyingType(convert.Type) ?? convert.Type;
            var drops = Nullable.GetUnderlyingType(convert.Operand.Type) != null && Nullable.GetUnderlyingType(convert.Type) == null;

            if (from == to || from.IsEnum || to.IsEnum || !(convert == aggregated || drops || !Widens(from, to)))
            {
                return null;
            }

            return Type.GetTypeCode(to) switch
            {
                TypeCode.SByte when IsNumeric(from) => "Edm.SByte",
                TypeCode.Byte when IsNumeric(from) => "Edm.Byte",
                TypeCode.Int16 when IsNumeric(from) => "Edm.Int16",
                TypeCode.Int32 when IsNumeric(from) => "Edm.Int32",
                TypeCode.Int64 when IsNumeric(from) => "Edm.Int64",
                TypeCode.Single when IsNumeric(from) => "Edm.Single",
                TypeCode.Double when IsNumeric(from) => "Edm.Double",
                TypeCode.Decimal when IsNumeric(from) => "Edm.Decimal",
                _ => null,
            };
        }

        private static bool IsNumericConversion(UnaryExpression convert)
        {
            return convert.Method == null || convert.Method.DeclaringType == typeof(decimal) && convert.Method.Name is "op_Implicit" or "op_Explicit";
        }

        private static bool IsNumeric(Type type) => Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal;

        private static string? DatePart(Type type, string name)
        {
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            {
                return name switch
                {
                    "Year" or "Month" or "Day" or "Hour" or "Minute" or "Second" => name.ToLowerInvariant(),
                    "Date" => "date",
                    _ => null,
                };
            }

            if (type == typeof(DateOnly))
            {
                return name is "Year" or "Month" or "Day" ? name.ToLowerInvariant() : null;
            }

            if (type == typeof(TimeOnly))
            {
                return name is "Hour" or "Minute" or "Second" ? name.ToLowerInvariant() : null;
            }

            return null;
        }

        private static bool IsBoolean(Type type) => type == typeof(bool) || type == typeof(bool?);

        private static bool IsText(Type type) => type == typeof(string) || type == typeof(char);

        private static bool IsCollection(Type type) => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);

        private static bool IsIntegral(object value) => value is sbyte or byte or short or ushort or int or uint or long or ulong;

        private static bool IsScalar(Type? type)
        {
            type = type == null ? null : Nullable.GetUnderlyingType(type) ?? type;
            return type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset)
                || type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(TimeSpan) || type == typeof(Guid)
                || type != null && (type.IsPrimitive || type.IsEnum);
        }

        internal static bool IsIdentifier(string? name)
        {
            return !string.IsNullOrEmpty(name) && (char.IsLetter(name[0]) || name[0] == '_') && name.All(character => char.IsLetterOrDigit(character) || character == '_');
        }

        private sealed class ParameterFinder(Dictionary<ParameterExpression, string> scope) : ExpressionVisitor
        {
            private readonly HashSet<ParameterExpression> declared = [];

            internal bool Found { get; private set; }

            internal ParameterExpression? Foreign { get; private set; }

            protected override Expression VisitLambda<TDelegate>(Expression<TDelegate> node)
            {
                declared.UnionWith(node.Parameters);
                return base.VisitLambda(node);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (scope.ContainsKey(node))
                {
                    Found = true;
                }
                else if (!declared.Contains(node))
                {
                    Foreign ??= node;
                }

                return node;
            }
        }
    }
}
