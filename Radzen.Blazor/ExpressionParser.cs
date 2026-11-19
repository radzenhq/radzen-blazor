using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Linq;

namespace Radzen;

#nullable enable

/// <summary>
/// Parse lambda expressions from strings.
/// </summary>
public class ExpressionParser
{
    /// <summary>
    /// Parses a lambda expression that returns a boolean value.
    /// </summary>
    public static Expression<Func<T, bool>> ParsePredicate<T>(string expression, Func<string, Type?>? typeResolver = null)
    {
        return ParseLambda<T, bool>(expression, typeResolver);
    }

    /// <summary>
    /// Parses a lambda expression that returns a typed result.
    /// </summary>
    public static Expression<Func<T, TResult>> ParseLambda<T, TResult>(string expression, Func<string, Type?>? typeResolver = null)
    {
        var lambda = ParseLambda<T>(expression, typeResolver);

        return Expression.Lambda<Func<T, TResult>>(lambda.Body, lambda.Parameters[0]);
    }

    /// <summary>
    /// Parses a lambda expression that returns untyped result.
    /// </summary>
    public static LambdaExpression ParseLambda<T>(string expression, Func<string, Type?>? typeLocator = null)
    {
        return ParseLambda(expression, typeof(T), typeLocator);
    }

    /// <summary>
    /// Parses a lambda expression that returns untyped result.
    /// </summary>
    public static LambdaExpression ParseLambda(string expression, Type type, Func<string, Type?>? typeResolver = null)
    {
        var parser = new ExpressionParser(expression, typeResolver);

        return parser.ParseLambda(type);
    }

    private readonly List<Token> tokens;
    private int position;
    private readonly Func<string, Type?>? typeResolver;
    private readonly Stack<ParameterExpression> parameterStack = new();
    private int anonymousTypeCount;

    private ExpressionParser(string expression, Func<string, Type?>? typeResolver = null)
    {
        this.typeResolver = typeResolver;
        tokens = ExpressionLexer.Scan(expression);
    }

    Token Expect(TokenType type)
    {
        if (position >= tokens.Count)
        {
            throw new InvalidOperationException($"Unexpected end of expression. Expected token: {type}");
        }

        var token = tokens[position];

        if (token.Type != type)
        {
            throw new InvalidOperationException($"Unexpected token: {token.Type}. Expected: {type}");
        }

        position++;

        return token;
    }

    void Advance(int count)
    {
        position += count;
    }

    Token Peek(int offset = 0)
    {
        if (position + offset >= tokens.Count)
        {
            return new Token(TokenType.None, string.Empty);
        }

        return tokens[position + offset];
    }

    private LambdaExpression ParseLambda(Type paramType)
    {
        var parameterIdentifier = Expect(TokenType.Identifier);

        var parameter = Expression.Parameter(paramType, parameterIdentifier.Value);

        parameterStack.Push(parameter);

        Expect(TokenType.EqualsGreaterThan);

        var body = ParseExpression(parameter);

        parameterStack.Pop();

        EnsureComplexity(body);

        return Expression.Lambda(body, parameter);
    }

    private const int MaxExpressionDepth = 300;
    private const int OperandDepthCost = 3;
    private const int MaxAnonymousTypes = 64;
    private const int MaxAnonymousTypeMembers = 256;
    private const int MaxTypeNameParts = 32;
    private const long MaxExpressionSize = 100_000;

    private static void EnsureComplexity(Expression root)
    {
        var measured = new Dictionary<Expression, (long Size, int Depth)>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<(Expression Node, bool Expanded)>();

        pending.Push((root, false));

        while (pending.Count > 0)
        {
            var (node, expanded) = pending.Pop();

            if (measured.ContainsKey(node))
            {
                continue;
            }

            if (!expanded)
            {
                EnsureAllowed(node);

                pending.Push((node, true));

                foreach (var child in GetChildren(node))
                {
                    if (!measured.ContainsKey(child))
                    {
                        pending.Push((child, false));
                    }
                }

                continue;
            }

            long size = 1;
            var depth = 0;

            foreach (var child in GetChildren(node))
            {
                var (childSize, childDepth) = measured[child];

                size += childSize;
                depth = Math.Max(depth, childDepth);
            }

            depth += node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse ? 1 : OperandDepthCost;

            if (depth > MaxExpressionDepth || size > MaxExpressionSize)
            {
                throw new InvalidOperationException("Expression is too complex.");
            }

            measured[node] = (size, depth);
        }
    }

    private static MemberExpression PublicPropertyOrField(Expression expression, string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy;

        var property = expression.Type.GetProperty(name, flags);

        if (property != null)
        {
            return Expression.Property(expression, property);
        }

        var field = expression.Type.GetField(name, flags);

        if (field != null)
        {
            return Expression.Field(expression, field);
        }

        throw new ArgumentException($"'{Shorten(name)}' is not a member of type '{expression.Type}'", nameof(name));
    }

    private static IEnumerable<Expression> GetChildren(Expression node)
    {
        switch (node)
        {
            case BinaryExpression binary:
                yield return binary.Left;
                yield return binary.Right;
                break;
            case UnaryExpression unary:
                yield return unary.Operand;
                break;
            case ConditionalExpression conditional:
                yield return conditional.Test;
                yield return conditional.IfTrue;
                yield return conditional.IfFalse;
                break;
            case MethodCallExpression call:
                if (call.Object != null)
                {
                    yield return call.Object;
                }
                foreach (var argument in call.Arguments)
                {
                    yield return argument;
                }
                break;
            case MemberExpression member:
                if (member.Expression != null)
                {
                    yield return member.Expression;
                }
                break;
            case IndexExpression index:
                if (index.Object != null)
                {
                    yield return index.Object;
                }
                foreach (var argument in index.Arguments)
                {
                    yield return argument;
                }
                break;
            case NewArrayExpression array:
                foreach (var element in array.Expressions)
                {
                    yield return element;
                }
                break;
            case MemberInitExpression init:
                foreach (var binding in init.Bindings)
                {
                    if (binding is MemberAssignment assignment)
                    {
                        yield return assignment.Expression;
                    }
                }
                break;
            case NewExpression creation:
                foreach (var argument in creation.Arguments)
                {
                    yield return argument;
                }
                break;
            case LambdaExpression lambda:
                yield return lambda.Body;
                break;
            case InvocationExpression invocation:
                yield return invocation.Expression;
                foreach (var argument in invocation.Arguments)
                {
                    yield return argument;
                }
                break;
            case TypeBinaryExpression typeBinary:
                yield return typeBinary.Expression;
                break;
        }
    }

    private Expression ParseExpression(ParameterExpression parameter)
    {
        var left = ParseBinary(parameter);
        var token = Peek();

        if (token.Type is TokenType.AmpersandAmpersand)
        {
            Advance(1);

            var right = ParseExpression(parameter) ?? throw new InvalidOperationException($"Expected expression after {token.Value} at position {position}");

            left = Expression.AndAlso(left, right);
        }
        else if (token.Type is TokenType.BarBar)
        {
            Advance(1);

            var right = ParseExpression(parameter) ?? throw new InvalidOperationException($"Expected expression after {token.Value} at position {position}");

            left = Expression.OrElse(left, right);
        }

        return left;
    }

    private Expression ParseBinary(ParameterExpression parameter)
    {
        EnsureStack();

        var left = ParseNullCoalescing(parameter);
        var token = Peek();

        if (token.Type is TokenType.EqualsEquals or TokenType.NotEquals or TokenType.GreaterThan or TokenType.LessThan or TokenType.LessThanOrEqual or TokenType.GreaterThanOrEqual)
        {
            Advance(1);
            var right = ParseBinary(parameter) ?? throw new InvalidOperationException($"Expected expression after {token.Value} at position {position}");
            left = Expression.MakeBinary(token.Type.ToExpressionType(), left, ConvertIfNeeded(right, left.Type));
        }

        return left;
    }

    private Expression ParseNullCoalescing(ParameterExpression parameter)
    {
        var left = ParseTernary(parameter);
        var token = Peek();

        while (token.Type == TokenType.QuestionMarkQuestionMark)
        {
            Advance(1);

            var right = ParseTernary(parameter) ?? throw new InvalidOperationException($"Expected expression after ?? at position {position}");

            if (right.Type == typeof(object))
            {
                right = ConvertIfNeeded(right, left.Type);
            }

            left = Expression.Coalesce(left, right);

            token = Peek();
        }

        return left;
    }

    private Expression ParseTernary(ParameterExpression parameter)
    {
        var condition = ParseOr(parameter);

        if (Peek().Type == TokenType.QuestionMark)
        {
            Advance(1);

            var trueExpression = ParseOr(parameter);

            Expect(TokenType.Colon);

            var falseExpression = ParseOr(parameter);

            if (trueExpression is ConstantExpression trueConst && trueConst.Value == null && falseExpression is not ConstantExpression)
            {
                trueExpression = Expression.Constant(null, falseExpression.Type);
            }
            else if (falseExpression is ConstantExpression falseConst && falseConst.Value == null && trueExpression is not ConstantExpression)
            {
                falseExpression = Expression.Constant(null, trueExpression.Type);
            }

            var ternary = Expression.Condition(condition, trueExpression, falseExpression);

            return ParseMemberAccess(ternary, parameter);
        }

        return ParseMemberAccess(condition, parameter);
    }

    private Expression ParseMemberAccess(Expression expression, ParameterExpression parameter)
    {
        EnsureStack();

        var token = Peek();
        while (token.Type is TokenType.Dot or TokenType.QuestionDot or TokenType.OpenBracket)
        {
            if (token.Type == TokenType.Dot)
            {
                Advance(1);
                token = Expect(TokenType.Identifier);
                if (Peek().Type == TokenType.OpenParen)
                {
                    expression = ParseInvocation(expression, token.Value, parameter);
                }
                else
                {
                    expression = PublicPropertyOrField(expression, token.Value);
                }
            }
            else if (token.Type == TokenType.QuestionDot)
            {
                Advance(1);
                token = Expect(TokenType.Identifier);

                var check = Expression.Equal(expression, Expression.Constant(null));

                if (Peek().Type == TokenType.OpenParen)
                {
                    var call = ParseInvocation(expression, token.Value, parameter);
                    expression = Expression.Condition(check, Expression.Constant(null, call.Type), call);
                }
                else
                {
                    var access = PublicPropertyOrField(expression, token.Value);
                    expression = Expression.Condition(check, Expression.Default(access.Type), access);

                    var nextToken = Peek();

                    if (nextToken.Type == TokenType.Dot || nextToken.Type == TokenType.QuestionDot)
                    {
                        var nextAccess = ParseMemberAccess(access, parameter);

                        expression = Expression.Condition(check, Expression.Default(nextAccess.Type), nextAccess);
                    }
                }
            }
            else if (token.Type == TokenType.OpenBracket)
            {
                Advance(1);
                var index = ParseExpression(parameter);
                Expect(TokenType.CloseBracket);

                if (expression.Type.IsArray)
                {
                    expression = Expression.ArrayIndex(expression, index);
                }
                else
                {
                    var indexer = expression.Type.GetProperty("Item") ?? throw new InvalidOperationException($"Type {expression.Type} does not have an indexer property");

                    expression = Expression.Property(expression, indexer, index);
                }

            }

            token = Peek();
        }

        return expression;
    }

    private MethodCallExpression ParseInvocation(Expression expression, string methodName, ParameterExpression parameter)
    {
        Advance(1);

        var arguments = new List<Expression>();

        if (Peek().Type != TokenType.CloseParen)
        {
            while (Peek().Type != TokenType.CloseParen)
            {
                var token = Peek();

                if (token.Type == TokenType.Identifier && Peek(1).Type == TokenType.EqualsGreaterThan)
                {
                    var lambdaParameterName = token.Value;

                    Advance(2);

                    Type? lambdaParameterType = null;

                    var extensionMethod = FindEnumerableMethod(methodName, 2);

                    if (extensionMethod != null)
                    {
                        lambdaParameterType = GetItemType(expression.Type);
                    }

                    if (lambdaParameterType == null)
                    {
                        throw new InvalidOperationException($"Could not infer type for lambda parameter {lambdaParameterName}");
                    }

                    var lambdaParameter = Expression.Parameter(lambdaParameterType, lambdaParameterName);
                    parameterStack.Push(lambdaParameter);
                    var lambdaBody = ParseExpression(lambdaParameter);
                    parameterStack.Pop();
                    arguments.Add(Expression.Lambda(lambdaBody, lambdaParameter));
                }
                else
                {
                    arguments.Add(ParseExpression(parameter));
                }

                if (Peek().Type == TokenType.Comma)
                {
                    Advance(1);
                }
            }
        }

        Expect(TokenType.CloseParen);

        var argumentTypes = arguments.Select(a => a.Type).ToArray();

        var method = expression.Type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, null, argumentTypes, null);

        if (method != null)
        {
            return Expression.Call(expression, method, arguments);
        }

        method = typeof(System.Collections.IEnumerable).IsAssignableFrom(expression.Type)
            ? FindEnumerableMethod(methodName, arguments.Count + 1)
            : null;

        if (method != null)
        {
            var argumentType = GetItemType(expression.Type);

            if (argumentType == null)
            {
                throw new InvalidOperationException($"Cannot determine item type for {expression.Type}");
            }

            if (method.IsGenericMethodDefinition)
            {
                method = method.MakeGenericMethod(argumentType);
            }

            var parameters = method.GetParameters();

            var argumentsWithInstance = new[] { expression }.Concat(arguments).ToArray();

            return Expression.Call(method, argumentsWithInstance.Select((a, index) => ConvertIfNeeded(a, parameters[index].ParameterType)));
        }

        throw new InvalidOperationException($"No suitable method '{Shorten(methodName)}' found for type '{expression.Type}'");
    }

    private static MethodInfo? FindEnumerableMethod(string methodName, int parameterCount)
    {
        return typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == methodName
                && m.GetParameters().Length == parameterCount
                && typeof(System.Collections.IEnumerable).IsAssignableFrom(m.GetParameters()[0].ParameterType));
    }

    private static Type? GetItemType(Type enumerableOrArray)
    {
        if (enumerableOrArray.IsArray)
        {
            return enumerableOrArray.GetElementType();
        }

        if (enumerableOrArray.IsGenericType)
        {
            return enumerableOrArray.GetGenericArguments()[0];
        }

        return enumerableOrArray.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))?
            .GetGenericArguments()[0];
    }

    private static void EnsureStack()
    {
        try
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
        }
        catch (InsufficientExecutionStackException)
        {
            throw new InvalidOperationException("Expression nesting is too deep.");
        }
    }

    private Expression? ParseTerm(ParameterExpression parameter)
    {
        EnsureStack();

        return ParseTermCore(parameter);
    }

    private Expression? ParseTermCore(ParameterExpression parameter)
    {
        var token = Peek();

        if (token.Type == TokenType.None)
        {
            return null;
        }

        if (token.Type == TokenType.OpenParen)
        {
            Advance(1);

            if (TryParseCastExpression(parameter, out var expression))
            {
                return expression;
            }

            expression = ParseExpression(parameter);

            Expect(TokenType.CloseParen);

            return expression;
        }

        if (token.Type == TokenType.Identifier)
        {
            if (token.Value == "default" && Peek(1).Type == TokenType.OpenParen)
            {
                Advance(2);

                if (!TryParseTypeReference(out var defaultType))
                {
                    throw new InvalidOperationException($"Could not resolve type in default expression at position {position}.");
                }

                Expect(TokenType.CloseParen);

                return Expression.Default(defaultType);
            }

            var matchingParameter = parameterStack.FirstOrDefault(p => p.Name == token.Value);
            if (matchingParameter != null)
            {
                Advance(1);
                return ParseMemberAccess(matchingParameter, parameter);
            }

            var type = GetWellKnownType(token.Value);

            if (type != null)
            {
                Advance(1);
                return ParseStaticMemberAccess(type, parameter);
            }

            if (TryParseQualifiedType(out var qualifiedType))
            {
                return ParseStaticMemberAccess(qualifiedType, parameter);
            }

            if (Peek(1).Type == TokenType.OpenParen)
            {
                Advance(1);
                return ParseInvocation(parameter, token.Value, parameter);
            }

            throw new InvalidOperationException($"Unexpected identifier: {Shorten(token.Value)}");
        }

        if (token.Type == TokenType.ExclamationMark)
        {
            Advance(1);

            var operand = ParseTerm(parameter) ?? throw new InvalidOperationException($"Expected expression after ! at position {position}");

            operand = ConvertIfNeeded(operand, typeof(bool));

            return Expression.Not(operand);
        }

        if (token.Type == TokenType.Minus)
        {
            Advance(1);

            var operand = ParseTerm(parameter) ?? throw new InvalidOperationException($"Expected expression after - at position {position}");

            return Expression.Negate(operand);
        }

        if (token.Type == TokenType.Plus)
        {
            Advance(1);

            var operand = ParseTerm(parameter) ?? throw new InvalidOperationException($"Expected expression after + at position {position}");

            return operand;
        }

        switch (token.Type)
        {
            case TokenType.CharacterLiteral:
            case TokenType.StringLiteral:
            case TokenType.NullLiteral:
            case TokenType.NumericLiteral:
            case TokenType.TrueLiteral:
            case TokenType.FalseLiteral:
                Advance(1);
                return token.ToConstantExpression();
            case TokenType.New:
                Advance(1);

                token = Peek();

                if (token.Type == TokenType.OpenBrace)
                {
                    Advance(1);

                    var properties = new List<(string Name, Expression Expression)>();

                    if (Peek().Type != TokenType.CloseBrace)
                    {
                        do
                        {
                            token = Peek();
                            string propertyName;
                            Expression propertyExpression;

                            if (token.Type == TokenType.Identifier)
                            {
                                propertyName = token.Value;
                                Advance(1);
                                if (Peek().Type == TokenType.Dot || Peek().Type == TokenType.QuestionDot)
                                {
                                    // Handle nested property access
                                    Expression expr = propertyName == parameter.Name ? (Expression)parameter : PublicPropertyOrField(parameter, propertyName);
                                    propertyExpression = ParseMemberAccess(expr, parameter);

                                    // Get the last identifier token's value
                                    var lastToken = tokens[position - 1];
                                    if (lastToken.Type == TokenType.Identifier)
                                    {
                                        propertyName = lastToken.Value;
                                    }
                                }
                                else
                                {
                                    Expect(TokenType.Equals);
                                    propertyExpression = ParseExpression(parameter);
                                }
                            }
                            else
                            {
                                propertyExpression = ParseExpression(parameter);

                                if (propertyExpression is MemberExpression memberExpression)
                                {
                                    propertyName = memberExpression.Member.Name;
                                }
                                else
                                {
                                    throw new InvalidOperationException($"Invalid anonymous type member expression at position {position}");
                                }
                            }

                            properties.Add((propertyName, propertyExpression));

                            if (Peek().Type == TokenType.Comma)
                            {
                                Advance(1);
                            }
                            else
                            {
                                break;
                            }
                        } while (Peek().Type != TokenType.CloseBrace);
                    }

                    Expect(TokenType.CloseBrace);

                    var propertyTypes = properties.Select(p => p.Expression.Type).ToArray();
                    var propertyNames = properties.Select(p => p.Name).ToArray();

                    if (++anonymousTypeCount > MaxAnonymousTypes || properties.Count > MaxAnonymousTypeMembers)
                    {
                        throw new InvalidOperationException("Expression is too complex.");
                    }

                    var dynamicType = DynamicTypeFactory.CreateType(parameter.Type.Name, propertyNames, propertyTypes);
                    var bindings = properties.Select(p => Expression.Bind(dynamicType.GetProperty(p.Name)!, p.Expression));
                    return Expression.MemberInit(Expression.New(dynamicType), bindings);
                }
                else
                {
                    Type? elementType = null;

                    if (TryParseQualifiedArrayType(out var parsedElementType))
                    {
                        elementType = parsedElementType;
                    }

                    Expect(TokenType.OpenBracket);
                    Expect(TokenType.CloseBracket);
                    Expect(TokenType.OpenBrace);

                    var elements = new List<Expression>();
                    if (Peek().Type != TokenType.CloseBrace)
                    {
                        do
                        {
                            elements.Add(ParseExpression(parameter));
                            if (Peek().Type == TokenType.Comma)
                            {
                                Advance(1);
                            }
                            else
                            {
                                break;
                            }
                        } while (Peek().Type != TokenType.CloseBrace);
                    }

                    Expect(TokenType.CloseBrace);

                    if (elementType == null)
                    {
                        elementType = elements.Count > 0 ? elements[0].Type : typeof(object);
                    }

                    return Expression.NewArrayInit(elementType, elements.Select(e => ConvertIfNeeded(e, elementType)));
                }
            default:
                throw new InvalidOperationException($"Unexpected token: {token.Type} at position {position}");
        }
    }

    private bool TryParseCastExpression(ParameterExpression parameter, out Expression expression)
    {
        expression = null!;

        var token = Peek();

        if (token.Type != TokenType.Identifier)
        {
            return false;
        }

        var typeName = new StringBuilder(token.Value);
        var index = position + 1;
        var typeCast = true;
        var nullable = false;

        while (index < tokens.Count)
        {
            token = tokens[index];

            if (token.Type == TokenType.Dot)
            {
                index++;
                if (index >= tokens.Count || tokens[index].Type != TokenType.Identifier)
                {
                    typeCast = false;
                    break;
                }
                typeName.Append('.').Append(tokens[index].Value);
                index++;
            }
            else if (token.Type == TokenType.QuestionMark)
            {
                nullable = true;
                index++;
                if (index >= tokens.Count || tokens[index].Type != TokenType.CloseParen)
                {
                    typeCast = false;
                    break;
                }
            }
            else if (token.Type == TokenType.CloseParen)
            {
                break;
            }
            else
            {
                typeCast = false;
                break;
            }
        }

        if (typeCast && index < tokens.Count && tokens[index].Type == TokenType.CloseParen)
        {
            var name = typeName.ToString();

            var type = ResolveType(name) ?? throw new InvalidOperationException($"Could not resolve type: {Shorten(name)}");

            if (nullable && type.IsValueType)
            {
                type = typeof(Nullable<>).MakeGenericType(type);
            }

            position = index;

            Advance(1);

            if (Peek().Type == TokenType.OpenParen && TryParseCastExpression(parameter, out var innerExpression))
            {
                expression = Expression.Convert(innerExpression, type);
            }
            else
            {
                var source = ParseTerm(parameter) ?? throw new InvalidOperationException($"Expected expression to cast at position {position}");
                expression = Expression.Convert(source, type);
            }

            return true;
        }

        return false;
    }

    private Expression ParseStaticMemberAccess(Type type, ParameterExpression parameter)
    {
        Expect(TokenType.Dot);

        var token = Expect(TokenType.Identifier);

        if (Peek().Type == TokenType.OpenParen)
        {
            return ParseStaticInvocation(type, token.Value, parameter);
        }
        else
        {
            var member = (MemberInfo?)type.GetProperty(token.Value) ?? type.GetField(token.Value);

            if (member == null)
            {
                throw new InvalidOperationException($"Member {Shorten(token.Value)} not found on type {type.Name}");
            }

            return Expression.MakeMemberAccess(null, member);
        }

        throw new InvalidOperationException($"Expected method invocation after {token.Value} at position {position}");
    }

    private Expression ParseStaticInvocation(Type type, string methodName, ParameterExpression parameter)
    {
        Advance(1);

        var arguments = new List<Expression>();

        if (Peek().Type != TokenType.CloseParen)
        {
            arguments.Add(ParseExpression(parameter));

            while (Peek().Type == TokenType.Comma)
            {
                Advance(1);
                arguments.Add(ParseExpression(parameter));
            }
        }

        Expect(TokenType.CloseParen);

        var method = type.GetMethod(methodName, [.. arguments.Select(a => a.Type)]) ?? throw new InvalidOperationException($"Method {Shorten(methodName)} not found on type {type.Name}");

        return Expression.Call(null, method, arguments);
    }

    /// <summary>
    /// Tries to parse a type reference (e.g. bool, int, System.DateTime, System.DateTime?) and returns the resolved type.
    /// Advances position past the type name. Does not backtrack — expects the full token sequence to be a type.
    /// Used for contexts like default(T) where the entire content is a type name.
    /// </summary>
    private bool TryParseTypeReference(out Type type)
    {
        type = null!;

        var token = Peek();
        if (token.Type != TokenType.Identifier)
        {
            return false;
        }

        var startPosition = position;
        var parts = new List<string> { token.Value };
        Advance(1);

        while (Peek().Type == TokenType.Dot)
        {
            Advance(1);
            token = Peek();
            if (token.Type != TokenType.Identifier)
            {
                position = startPosition;
                return false;
            }
            parts.Add(token.Value);
            Advance(1);
        }

        var nullable = false;
        if (Peek().Type == TokenType.QuestionMark)
        {
            nullable = true;
            Advance(1);
        }

        var typeName = string.Join(".", parts);
        var resolvedType = ResolveType(typeName);
        if (resolvedType != null)
        {
            if (nullable && resolvedType.IsValueType)
            {
                resolvedType = typeof(Nullable<>).MakeGenericType(resolvedType);
            }

            type = resolvedType;
            return true;
        }

        position = startPosition;
        return false;
    }

    /// <summary>
    /// Tries to parse a qualified type name (e.g. System.DateTime or System.DateTime?) and returns the resolved type.
    /// Advances position past the type name. The next token will be . for member access or [ for array.
    /// Uses backtracking to find the longest resolvable type prefix (e.g. System.DateTime in System.DateTime.SpecifyKind).
    /// </summary>
    private bool TryParseQualifiedType(out Type type)
    {
        type = null!;

        var token = Peek();
        if (token.Type != TokenType.Identifier)
        {
            return false;
        }

        var startPosition = position;
        var parts = new List<string> { token.Value };
        Advance(1);

        while (Peek().Type == TokenType.Dot)
        {
            Advance(1);
            token = Peek();
            if (token.Type != TokenType.Identifier)
            {
                return false;
            }
            parts.Add(token.Value);
            Advance(1);
        }

        var nullable = false;
        if (Peek().Type == TokenType.QuestionMark)
        {
            nullable = true;
            Advance(1);
        }

        for (var i = Math.Min(parts.Count, MaxTypeNameParts); i >= 1; i--)
        {
            var typeName = string.Join(".", parts.Take(i));
            var resolvedType = ResolveType(typeName);
            if (resolvedType != null)
            {
                if (nullable && resolvedType.IsValueType)
                {
                    resolvedType = typeof(Nullable<>).MakeGenericType(resolvedType);
                }

                position = startPosition;
                var tokensToConsume = (i * 2) - 1 + (nullable ? 1 : 0);
                for (var t = 0; t < tokensToConsume; t++)
                {
                    Advance(1);
                }

                type = resolvedType;
                return true;
            }
        }

        position = startPosition;
        return false;
    }

    /// <summary>
    /// Tries to parse a qualified array element type (e.g. System.DateTime? or DateTime) before [].
    /// Advances position past the type name to the [. Returns the element type for the array.
    /// </summary>
    private bool TryParseQualifiedArrayType(out Type? elementType)
    {
        elementType = null;

        var token = Peek();
        if (token.Type != TokenType.Identifier)
        {
            return false;
        }

        var startPosition = position;
        var parts = new List<string> { token.Value };
        Advance(1);

        while (Peek().Type == TokenType.Dot)
        {
            Advance(1);
            token = Peek();
            if (token.Type != TokenType.Identifier)
            {
                position = startPosition;
                return false;
            }
            parts.Add(token.Value);
            Advance(1);
        }

        var nullable = false;
        if (Peek().Type == TokenType.QuestionMark)
        {
            nullable = true;
            Advance(1);
        }

        if (Peek().Type != TokenType.OpenBracket)
        {
            position = startPosition;
            return false;
        }

        for (var i = Math.Min(parts.Count, MaxTypeNameParts); i >= 1; i--)
        {
            var typeName = string.Join(".", parts.Take(i));
            var resolvedType = ResolveType(typeName);
            if (resolvedType != null)
            {
                if (nullable && resolvedType.IsValueType)
                {
                    resolvedType = typeof(Nullable<>).MakeGenericType(resolvedType);
                }
                elementType = resolvedType;
                return true;
            }
        }

        position = startPosition;
        return false;
    }

    private Type? ResolveType(string typeName)
    {
        return GetWellKnownType(typeName)
            ?? ResolveWellKnownFullName(typeName)
            ?? typeResolver?.Invoke(typeName)
            ?? ResolveLoadedType(typeName);
    }

    private static string Shorten(string value) => value.Length > 100 ? value[..100] + "..." : value;

    private static volatile Dictionary<string, Type>? typeNameCache;
    private static int typeNameCacheVersion;
    private static int typeNameCacheSubscribed;

    private static Type? ResolveLoadedType(string typeName)
    {
        var cache = typeNameCache;

        if (cache == null)
        {
            if (Interlocked.Exchange(ref typeNameCacheSubscribed, 1) == 0)
            {
                AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
            }

            var version = Volatile.Read(ref typeNameCacheVersion);

            cache = BuildTypeNameCache(AppDomain.CurrentDomain.GetAssemblies());

            if (version == Volatile.Read(ref typeNameCacheVersion))
            {
                typeNameCache = cache;

                if (version != Volatile.Read(ref typeNameCacheVersion))
                {
                    typeNameCache = null;
                }
            }
        }

        return cache.TryGetValue(typeName, out var type) ? type : null;
    }

    private static void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
    {
        if (args.LoadedAssembly.IsDynamic || args.LoadedAssembly.IsCollectible)
        {
            return;
        }

        Interlocked.Increment(ref typeNameCacheVersion);
        typeNameCache = null;
    }

    private static Dictionary<string, Type> BuildTypeNameCache(Assembly[] assemblies)
    {
        var map = new Dictionary<string, Type>(StringComparer.Ordinal);

        foreach (var type in assemblies.Where(a => !a.IsCollectible && !a.IsDynamic).SelectMany(GetLoadableTypes))
        {
            if (type.IsGenericTypeDefinition || !(type.IsVisible || type.IsEnum))
            {
                continue;
            }

            var fullName = type.FullName;
            if (fullName != null)
            {
                var key = fullName.Replace("+", ".", StringComparison.Ordinal);

                if (!map.TryGetValue(key, out var existing) || (!existing.IsVisible && type.IsVisible))
                {
                    map[key] = type;
                }
            }
        }

        return map;
    }

    private static Type? ResolveWellKnownFullName(string typeName)
    {
        if (!typeName.StartsWith("System.", StringComparison.Ordinal))
        {
            return null;
        }

        var resolved = GetWellKnownType(typeName.Split('.')[^1]);

        return resolved?.FullName == typeName ? resolved : null;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
    }

    private static readonly HashSet<Type> AllowedFrameworkTypes = new()
    {
        typeof(DateTime), typeof(DateOnly), typeof(TimeOnly), typeof(DateTimeOffset),
        typeof(Guid), typeof(TimeSpan), typeof(DateTimeStyles), typeof(DateTimeKind),
        typeof(double), typeof(float), typeof(int), typeof(long), typeof(short),
        typeof(byte), typeof(sbyte), typeof(uint), typeof(ulong), typeof(ushort),
        typeof(bool), typeof(char), typeof(decimal), typeof(string),
        typeof(Math), typeof(Convert), typeof(Enum),
        typeof(Uri), typeof(Version), typeof(Int128), typeof(UInt128), typeof(Half),
        typeof(System.Numerics.BigInteger), typeof(IntPtr), typeof(UIntPtr)
    };

    private static readonly HashSet<string> ComparisonOperatorNames = new(StringComparer.Ordinal)
    {
        "op_Equality", "op_Inequality", "op_GreaterThan", "op_LessThan", "op_GreaterThanOrEqual", "op_LessThanOrEqual"
    };

    private static readonly HashSet<string> DeniedStringMethodNames = new(StringComparer.Ordinal)
    {
        nameof(string.Intern), nameof(string.IsInterned), nameof(string.PadLeft), nameof(string.PadRight)
    };

    private static readonly HashSet<string> ObjectMethodNames = new(StringComparer.Ordinal)
    {
        nameof(ToString), nameof(Equals), nameof(GetHashCode), nameof(IComparable.CompareTo)
    };

    private static readonly HashSet<string> CollectionReadMethodNames = new(StringComparer.Ordinal)
    {
        "Contains", "ContainsKey", "ContainsValue", "IndexOf", "LastIndexOf", "GetValueOrDefault",
        "IsSubsetOf", "IsSupersetOf", "IsProperSubsetOf", "IsProperSupersetOf", "Overlaps", "SetEquals",
        "ToArray", "ToList", "AsReadOnly"
    };

    private static readonly ConcurrentDictionary<Type, bool> allowedTypes = new();
    private static readonly ConcurrentDictionary<(Module Module, int Token), bool> allowedMethods = new();

    /// <summary>
    /// Allows expression strings to call the specified method. By default only methods of framework value
    /// types, <see cref="Math"/>, <see cref="Convert"/> and LINQ operators can be called.
    /// </summary>
    /// <example>
    /// <code>
    /// ExpressionParser.AllowMethod(typeof(Order).GetMethod(nameof(Order.IsOverdue))!);
    /// </code>
    /// </example>
    public static void AllowMethod(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);

        allowedMethods[(method.Module, method.MetadataToken)] = true;
    }

    /// <summary>
    /// Allows expression strings to call the public methods and operators of the specified type and to read
    /// its static properties and fields.
    /// </summary>
    /// <example>
    /// <code>
    /// ExpressionParser.AllowType(typeof(MyApp.Filters.Helpers));
    /// </code>
    /// </example>
    public static void AllowType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        allowedTypes[type] = true;
    }

    private static bool IsTypeAllowed(Type type)
    {
        return allowedTypes.ContainsKey(type) || (type.IsGenericType && allowedTypes.ContainsKey(type.GetGenericTypeDefinition()));
    }

    private static bool IsSequence(Type type) => typeof(System.Collections.IEnumerable).IsAssignableFrom(type);

    private static bool IsMethodAllowed(MethodInfo method)
    {
        var declaringType = method.DeclaringType;

        if (declaringType == null)
        {
            return false;
        }

        if (allowedMethods.ContainsKey((method.Module, method.MetadataToken)) || IsTypeAllowed(declaringType))
        {
            return true;
        }

        if (declaringType == typeof(Enumerable) || declaringType == typeof(Queryable))
        {
            var parameters = method.GetParameters();

            return parameters.Length > 0 && IsSequence(parameters[0].ParameterType);
        }

        if (declaringType == typeof(string) && DeniedStringMethodNames.Contains(method.Name))
        {
            return false;
        }

        if (AllowedFrameworkTypes.Contains(declaringType) || declaringType.IsEnum)
        {
            return true;
        }

        if (declaringType.IsGenericType && declaringType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return true;
        }

        if (method.IsStatic)
        {
            return method.IsSpecialName && ComparisonOperatorNames.Contains(method.Name);
        }

        return ObjectMethodNames.Contains(method.Name) || (CollectionReadMethodNames.Contains(method.Name) && IsSequence(declaringType));
    }

    private static bool IsStaticReadAllowed(MemberInfo member)
    {
        var declaringType = member.DeclaringType;

        if (declaringType == null)
        {
            return false;
        }

        return AllowedFrameworkTypes.Contains(declaringType) || declaringType == typeof(CultureInfo) || declaringType.IsEnum
            || (member is FieldInfo { IsLiteral: true } && declaringType.IsVisible) || IsTypeAllowed(declaringType);
    }

    private static Type Unwrap(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        while (underlying.IsArray && underlying.GetElementType() is { } elementType)
        {
            underlying = elementType;
        }

        return underlying;
    }

    private static bool IsReflectionType(Type type)
    {
        var t = Unwrap(type);
        return typeof(MemberInfo).IsAssignableFrom(t)
            || typeof(Assembly).IsAssignableFrom(t)
            || typeof(Module).IsAssignableFrom(t)
            || typeof(Delegate).IsAssignableFrom(t)
            || t == typeof(AppDomain)
            || t == typeof(Type)
            || (t.Namespace != null && t.Namespace.StartsWith("System.Reflection", StringComparison.Ordinal));
    }

    private static string DisplayName(Type? type)
    {
        if (type == null)
        {
            return string.Empty;
        }

        var name = (type.IsGenericType ? type.GetGenericTypeDefinition() : type).FullName ?? type.Name;

        return name.Replace("+", ".", StringComparison.Ordinal);
    }

    private static void EnsureAllowed(Expression node)
    {
        if (node.NodeType != ExpressionType.Lambda && IsReflectionType(node.Type))
        {
            throw new InvalidOperationException($"Access to reflection type '{Unwrap(node.Type).FullName}' is not allowed.");
        }

        var supported = node switch
        {
            BinaryExpression or UnaryExpression or ConditionalExpression or MethodCallExpression or MemberExpression
                or IndexExpression or NewArrayExpression or LambdaExpression or ConstantExpression or ParameterExpression
                or DefaultExpression or TypeBinaryExpression => true,
            NewExpression creation => creation.Type.Assembly.IsDynamic,
            MemberInitExpression init => init.Bindings.All(binding => binding is MemberAssignment),
            _ => false
        };

        if (!supported)
        {
            throw new InvalidOperationException($"Expression of kind '{node.NodeType}' is not allowed.");
        }

        var method = node switch
        {
            MethodCallExpression call => call.Method,
            BinaryExpression binary => binary.Method,
            UnaryExpression unary => unary.Method,
            _ => null
        };

        if (method != null && !IsMethodAllowed(method))
        {
            var typeName = DisplayName(method.DeclaringType);

            throw new InvalidOperationException($"Method '{method.Name}' of type '{typeName}' is not allowed in an expression string. "
                + $"To allow it, call ExpressionParser.AllowMethod(typeof({typeName}).GetMethod(\"{method.Name}\")) or ExpressionParser.AllowType(typeof({typeName})) when the application starts.");
        }

        if (node is MemberExpression { Expression: null } member && !IsStaticReadAllowed(member.Member))
        {
            var typeName = DisplayName(member.Member.DeclaringType);

            throw new InvalidOperationException($"Static member '{member.Member.Name}' of type '{typeName}' is not allowed in an expression string. "
                + $"To allow it, call ExpressionParser.AllowType(typeof({typeName})) when the application starts.");
        }
    }

    private static Type? GetWellKnownType(string typeName)
    {
        return typeName switch
        {
            nameof(DateTime) => typeof(DateTime),
            nameof(DateOnly) => typeof(DateOnly),
            nameof(TimeOnly) => typeof(TimeOnly),
            nameof(DateTimeOffset) => typeof(DateTimeOffset),
            nameof(Guid) => typeof(Guid),
            nameof(CultureInfo) => typeof(CultureInfo),
            nameof(DateTimeStyles) => typeof(DateTimeStyles),
            nameof(DateTimeKind) => typeof(DateTimeKind),
            nameof(Double) or "double" => typeof(double),
            nameof(Single) or "float" => typeof(float),
            nameof(Int32) or "int" => typeof(int),
            nameof(Int64) or "long" => typeof(long),
            nameof(Int16) or "short" => typeof(short),
            nameof(Byte) or "byte" => typeof(byte),
            nameof(SByte) or "sbyte" => typeof(sbyte),
            nameof(UInt32) or "uint" => typeof(uint),
            nameof(UInt64) or "ulong" => typeof(ulong),
            nameof(UInt16) or "ushort" => typeof(ushort),
            nameof(Boolean) or "bool" => typeof(bool),
            nameof(Char) or "char" => typeof(char),
            nameof(Decimal) or "decimal" => typeof(decimal),
            nameof(String) or "string" => typeof(string),
            nameof(Math) => typeof(Math),
            nameof(Convert) => typeof(Convert),
            nameof(TimeSpan) => typeof(TimeSpan),
            nameof(StringComparison) => typeof(StringComparison),
            nameof(DayOfWeek) => typeof(DayOfWeek),
            nameof(Object) or "object" => typeof(object),
            nameof(Uri) => typeof(Uri),
            nameof(Version) => typeof(Version),
            nameof(Int128) => typeof(Int128),
            nameof(UInt128) => typeof(UInt128),
            nameof(Half) => typeof(Half),
            _ => null
        };
    }

    private Expression ParseOr(ParameterExpression parameter)
    {
        var left = ParseMemberAccess(ParseAnd(parameter), parameter);

        var token = Peek();
        while (token.Type == TokenType.BarBar)
        {
            Advance(1);
            var right = ParseMemberAccess(ParseAnd(parameter) ?? throw new InvalidOperationException($"Expected expression after || at position {position}"), parameter);
            left = Expression.OrElse(left, right);
            token = Peek();
        }

        return left;
    }

    private Expression ParseAnd(ParameterExpression parameter)
    {
        var left = ParseMemberAccess(ParseComparison(parameter), parameter);

        var token = Peek();
        while (token.Type == TokenType.AmpersandAmpersand)
        {
            Advance(1);
            var right = ParseMemberAccess(ParseComparison(parameter) ?? throw new InvalidOperationException($"Expected expression after && at position {position}"), parameter);
            left = Expression.AndAlso(left, right);
            token = Peek();
        }

        return left;
    }

    private Expression ParseComparison(ParameterExpression parameter)
    {
        var left = ParseShift(parameter);

        var token = Peek();
        if (token.Type is TokenType.EqualsEquals or TokenType.NotEquals or TokenType.GreaterThan or TokenType.LessThan or TokenType.LessThanOrEqual or TokenType.GreaterThanOrEqual)
        {
            Advance(1);
            var right = ParseShift(parameter) ?? throw new InvalidOperationException($"Expected expression after {token.Value} at position {position}");
            left = Expression.MakeBinary(token.Type.ToExpressionType(), left, ConvertIfNeeded(right, left.Type));
        }

        return ParseBinaryAnd(left, parameter);
    }

    private Expression ParseBinaryAnd(Expression left, ParameterExpression parameter)
    {
        var token = Peek();
        while (token.Type == TokenType.Ampersand)
        {
            Advance(1);
            var right = ParseShift(parameter) ?? throw new InvalidOperationException($"Expected expression after & at position {position}");
            left = Expression.MakeBinary(ExpressionType.And, left, ConvertIfNeeded(right, left.Type));
            token = Peek();
        }

        return ParseBinaryXor(left, parameter);
    }

    private Expression ParseBinaryXor(Expression left, ParameterExpression parameter)
    {
        var token = Peek();
        while (token.Type == TokenType.Caret)
        {
            Advance(1);
            var right = ParseBinaryAnd(ParseShift(parameter), parameter) ?? throw new InvalidOperationException($"Expected expression after ^ at position {position}");
            left = Expression.MakeBinary(ExpressionType.ExclusiveOr, left, ConvertIfNeeded(right, left.Type));
            token = Peek();
        }

        return ParseBinaryOr(left, parameter);
    }

    private Expression ParseBinaryOr(Expression left, ParameterExpression parameter)
    {
        var token = Peek();
        while (token.Type == TokenType.Bar)
        {
            Advance(1);
            var right = ParseBinaryXor(ParseShift(parameter), parameter) ?? throw new InvalidOperationException($"Expected expression after | at position {position}");
            left = Expression.MakeBinary(ExpressionType.Or, left, ConvertIfNeeded(right, left.Type));
            token = Peek();
        }

        return left;
    }

    private Expression ParseShift(ParameterExpression parameter)
    {
        var left = ParseAdditive(parameter);

        var token = Peek();
        while (token.Type is TokenType.LessThanLessThan or TokenType.GreaterThanGreaterThan)
        {
            Advance(1);
            var right = ParseAdditive(parameter) ?? throw new InvalidOperationException($"Expected expression after {token.Value} at position {position}");
            left = Expression.MakeBinary(token.Type.ToExpressionType(), left, ConvertIfNeeded(right, left.Type));
            token = Peek();
        }

        return left;
    }

    private Expression ParseAdditive(ParameterExpression parameter)
    {
        var left = ParseMultiplicative(parameter);

        var token = Peek();
        while (token.Type is TokenType.Plus or TokenType.Minus)
        {
            Advance(1);
            var right = ParseMultiplicative(parameter) ?? throw new InvalidOperationException($"Expected expression after {token.Value} at position {position}");

            if (token.Type == TokenType.Plus && left.Type == typeof(string))
            {
                left = Expression.Call(null, typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)])!, left, ConvertIfNeeded(right, typeof(string)));
            }
            else
            {
                left = Expression.MakeBinary(token.Type.ToExpressionType(), left, ConvertIfNeeded(right, left.Type));
            }

            token = Peek();
        }

        return left;
    }

    private Expression ParseMultiplicative(ParameterExpression parameter)
    {
        var left = ParseTerm(parameter) ?? throw new InvalidOperationException($"Expected expression at position {position}");

        var token = Peek();
        while (token.Type is TokenType.Star or TokenType.Slash)
        {
            Advance(1);
            var right = ParseTerm(parameter) ?? throw new InvalidOperationException($"Expected expression after {token.Value} at position {position}");
            left = Expression.MakeBinary(token.Type.ToExpressionType(), left, ConvertIfNeeded(right, left.Type));
            token = Peek();
        }

        return left;
    }

    private static Expression ConvertIfNeeded(Expression expression, Type targetType)
    {
        if (expression is not LambdaExpression)
        {
            return expression.Type == targetType ? expression : Expression.Convert(expression, targetType);
        }

        return expression;
    }
}