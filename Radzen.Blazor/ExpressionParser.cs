using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
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
        var lambda = ParseLambda(expression, typeof(T), typeResolver);

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
    private int position = 0;
    private readonly Func<string, Type?>? typeResolver;
    private readonly Stack<ParameterExpression> parameterStack = new();

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

        return Expression.Lambda(body, parameter);
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
                    expression = Expression.PropertyOrField(expression, token.Value);
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
                    var access = Expression.PropertyOrField(expression, token.Value);

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

                    var extensionMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == 2);

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

        method = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                   .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == arguments.Count + 1);

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

        throw new InvalidOperationException($"No suitable method '{methodName}' found for type '{expression.Type}'");
    }

    private static Type? GetItemType(Type enumerableOrArray)
    {
        return enumerableOrArray.IsArray ? enumerableOrArray.GetElementType() : enumerableOrArray.GetGenericArguments()[0];
    }

    private Expression? ParseTerm(ParameterExpression parameter)
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

            if (Peek(1).Type == TokenType.OpenParen)
            {
                Advance(1);
                return ParseInvocation(parameter, token.Value, parameter);
            }

            throw new InvalidOperationException($"Unexpected identifier: {token.Value}");
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
                                    Expression expr = propertyName == parameter.Name ? (Expression)parameter : Expression.Property(parameter, propertyName);
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
                    var dynamicType = DynamicTypeFactory.CreateType(parameter.Type.Name, propertyNames, propertyTypes);
                    var bindings = properties.Select(p => Expression.Bind(dynamicType.GetProperty(p.Name)!, p.Expression));
                    return Expression.MemberInit(Expression.New(dynamicType), bindings);
                }
                else
                {
                    Type? elementType = null;
                    var nullable = false;

                    if (token.Type == TokenType.Identifier)
                    {
                        var typeName = token.Value;
                        elementType = GetWellKnownType(typeName);
                        Advance(1);

                        if (Peek().Type == TokenType.QuestionMark)
                        {
                            nullable = true;
                            Advance(1);
                        }
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

                    if (nullable)
                    {
                        elementType = typeof(Nullable<>).MakeGenericType(elementType);
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

            var type = GetWellKnownType(name) ?? typeResolver?.Invoke(name) ?? throw new InvalidOperationException($"Could not resolve type: {typeName}");

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
                throw new InvalidOperationException($"Member {token.Value} not found on type {type.Name}");
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

        var method = type.GetMethod(methodName, [.. arguments.Select(a => a.Type)]) ?? throw new InvalidOperationException($"Method {methodName} not found on type {type.Name}");

        return Expression.Call(null, method, arguments);
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