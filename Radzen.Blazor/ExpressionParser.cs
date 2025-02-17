using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;
using System.Reflection.Emit;

namespace Radzen;

static class DynamicTypeFactory
{
    public static Type CreateType(string typeName, string[] propertyNames, Type[] propertyTypes)
    {
        if (propertyNames.Length != propertyTypes.Length)
        {
            throw new ArgumentException("Property names and types count mismatch.");
        }

        var assemblyName = new AssemblyName("DynamicTypesAssembly");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicTypesModule");

        var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);

        for (int i = 0; i < propertyNames.Length; i++)
        {
            var fieldBuilder = typeBuilder.DefineField("_" + propertyNames[i], propertyTypes[i], FieldAttributes.Private);
            var propertyBuilder = typeBuilder.DefineProperty(propertyNames[i], PropertyAttributes.None, propertyTypes[i], null);

            var getterMethod = typeBuilder.DefineMethod(
                "get_" + propertyNames[i],
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                propertyTypes[i],
                Type.EmptyTypes);

            var getterIl = getterMethod.GetILGenerator();
            getterIl.Emit(OpCodes.Ldarg_0);
            getterIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getterIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getterMethod);

            var setterMethod = typeBuilder.DefineMethod(
                      "set_" + propertyNames[i],
                      MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                      null,
                      new[] { propertyTypes[i] });

            var setterIl = setterMethod.GetILGenerator();
            setterIl.Emit(OpCodes.Ldarg_0);
            setterIl.Emit(OpCodes.Ldarg_1);
            setterIl.Emit(OpCodes.Stfld, fieldBuilder);
            setterIl.Emit(OpCodes.Ret);

            propertyBuilder.SetSetMethod(setterMethod);
        }

        var dynamicType = typeBuilder.CreateType();
        return dynamicType;
    }
}

class ExpressionSyntaxVisitor<T> : CSharpSyntaxVisitor<Expression>
{
    private readonly ParameterExpression parameter;
    private readonly Func<string, Type> typeLocator;

    public ExpressionSyntaxVisitor(ParameterExpression parameter, Func<string, Type> typeLocator)
    {
        this.parameter = parameter;
        this.typeLocator = typeLocator;
    }

    public override Expression VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        var left = Visit(node.Left);

        var right = ConvertIfNeeded(Visit(node.Right), left.Type);

        return Expression.MakeBinary(ParseBinaryOperator(node.OperatorToken), left, right);
    }


    public override Expression VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var expression = Visit(node.Expression) ?? parameter;
        return Expression.PropertyOrField(expression, node.Name.Identifier.Text);
    }

    public override Expression VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        return Expression.Constant(ParseLiteral(node));
    }

    public override Expression VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (node.Identifier.Text == parameter.Name)
        {
            return parameter;
        }

        return node.Identifier.Text switch
        {
            nameof(DateTime) => Expression.Constant(typeof(DateTime)),
            nameof(DateOnly) => Expression.Constant(typeof(DateOnly)),
            nameof(TimeOnly) => Expression.Constant(typeof(TimeOnly)),
            nameof(Guid) => Expression.Constant(typeof(Guid)),
            _ => throw new NotSupportedException("Unsupported identifier: " + node.Identifier.Text),
        };
    }

    public override Expression VisitConditionalExpression(ConditionalExpressionSyntax node)
    {
        var condition = Visit(node.Condition);
        var whenTrue = Visit(node.WhenTrue);
        var whenFalse = Visit(node.WhenFalse);
        return Expression.Condition(condition, whenTrue, whenFalse);
    }

    public override Expression VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
    {
        return Visit(node.Expression);
    }

    public override Expression VisitCastExpression(CastExpressionSyntax node)
    {
        var typeName = node.Type.ToString();

        var nullable = typeName.EndsWith("?");

        if (nullable)
        {
            typeName = typeName[..^1];
        }

        var targetType = typeName switch
        {
            nameof(Int32) => typeof(int),
            nameof(Int64) => typeof(long),
            nameof(Double) => typeof(double),
            nameof(Single) => typeof(float),
            nameof(Decimal) => typeof(decimal),
            nameof(String) => typeof(string),
            nameof(Boolean) => typeof(bool),
            nameof(DateTime) => typeof(DateTime),
            nameof(DateOnly) => typeof(DateOnly),
            nameof(TimeOnly) => typeof(TimeOnly),
            nameof(Guid) => typeof(Guid),
            nameof(Char) => typeof(char),
            "int" => typeof(int),
            "long" => typeof(long),
            "double" => typeof(double),
            "float" => typeof(float),
            "decimal" => typeof(decimal),
            "string" => typeof(string),
            "char" => typeof(char),
            _ => typeLocator?.Invoke(typeName)
        };

        if (nullable)
        {
            targetType = typeof(Nullable<>).MakeGenericType(targetType);
        }

        if (targetType == null)
        {
            throw new NotSupportedException("Unsupported cast type: " + node.Type);
        }

        var operand = Visit(node.Expression);
        return Expression.Convert(operand, targetType);
    }

    public override Expression VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node)
    {
        var expressions = node.Initializer.Expressions.Select(Visit).ToArray();
        var elementType = expressions.Length > 0 ? expressions[0].Type : typeof(object);
        return Expression.NewArrayInit(elementType, expressions);
    }

    private Expression CallStaticMethod(Type type, string methodName, Expression[] arguments, Type[] argumentTypes)
    {
        var methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, argumentTypes);

        if (methodInfo != null)
        {
            return Expression.Call(methodInfo, arguments);
        }

        throw new NotSupportedException("Method not found: " + methodName);
    }

    public override Expression DefaultVisit(SyntaxNode node)
    {
        throw new NotSupportedException("Unsupported syntax: " + node.GetType().Name);
    }

    public override Expression VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
    {
        var body = Visit(node.Body);

        return Expression.Lambda(body, parameter);
    }

    private Expression VisitArgument(Expression instance, ArgumentSyntax argument)
    {
        if (argument.Expression is SimpleLambdaExpressionSyntax lambda)
        {
            var itemType = GetItemType(instance.Type);

            var visitor = new ExpressionSyntaxVisitor<T>(Expression.Parameter(itemType, lambda.Parameter.Identifier.Text), typeLocator);

            return visitor.Visit(lambda);
        }

        return Visit(argument.Expression);
    }

    private static Expression ConvertIfNeeded(Expression expression, Type targetType)
    {
        if (expression is not LambdaExpression)
        {
            return expression.Type == targetType ? expression : Expression.Convert(expression, targetType);
        }

        return expression;
    }

    public override Expression VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is MemberAccessExpressionSyntax methodCall)
        {
            var instance = Visit(methodCall.Expression);
            var arguments = node.ArgumentList.Arguments.Select(a => VisitArgument(instance, a)).ToArray();
            var argumentTypes = arguments.Select(a => a.Type).ToArray();

            if (instance is ConstantExpression constant && constant.Value is Type type)
            {
                return CallStaticMethod(type, methodCall.Name.Identifier.Text, arguments, argumentTypes);
            }

            var instanceType = instance.Type;
            var methodInfo = instanceType.GetMethod(methodCall.Name.Identifier.Text, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, argumentTypes);

            if (methodInfo == null)
            {
                methodInfo = typeof(Enumerable)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == methodCall.Name.Identifier.Text && m.GetParameters().Length == arguments.Length + 1);

                if (methodInfo != null)
                {
                    var itemType = GetItemType(instanceType);
                    var genericMethod = methodInfo.MakeGenericMethod(itemType);

                    return Expression.Call(genericMethod, new[] { instance }.Concat(arguments.Select(a => ConvertIfNeeded(a, itemType))));
                }
            }

            if (methodInfo == null)
            {
                throw new NotSupportedException("Unsupported method call: " + methodCall.Name.Identifier.Text);
            }

            return Expression.Call(instance, methodInfo, arguments);
        }

        throw new NotSupportedException("Unsupported invocation expression: " + node.ToString());
    }

    private static Type GetItemType(Type enumerableOrArray)
    {
        return enumerableOrArray.IsArray ? enumerableOrArray.GetElementType() : enumerableOrArray.GetGenericArguments()[0];
    }


    private static object ParseLiteral(LiteralExpressionSyntax literal)
    {
        return literal.Kind() switch
        {
            SyntaxKind.StringLiteralExpression => literal.Token.ValueText,
            SyntaxKind.NumericLiteralExpression => literal.Token.Value,
            SyntaxKind.TrueLiteralExpression => true,
            SyntaxKind.FalseLiteralExpression => false,
            SyntaxKind.NullLiteralExpression => null,
            _ => throw new NotSupportedException("Unsupported literal: " + literal),
        };
    }

    private static ExpressionType ParseBinaryOperator(SyntaxToken token)
    {
        return token.Kind() switch
        {
            SyntaxKind.EqualsEqualsToken => ExpressionType.Equal,
            SyntaxKind.LessThanToken => ExpressionType.LessThan,
            SyntaxKind.GreaterThanToken => ExpressionType.GreaterThan,
            SyntaxKind.LessThanEqualsToken => ExpressionType.LessThanOrEqual,
            SyntaxKind.GreaterThanEqualsToken => ExpressionType.GreaterThanOrEqual,
            SyntaxKind.ExclamationEqualsToken => ExpressionType.NotEqual,
            SyntaxKind.AmpersandAmpersandToken => ExpressionType.AndAlso,
            SyntaxKind.BarBarToken => ExpressionType.OrElse,
            _ => throw new NotSupportedException("Unsupported operator: " + token.Text),
        };
    }

    public override Expression VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node)
    {
        var properties = node.Initializers.Select(init =>
        {
            var name = init.NameEquals?.Name.Identifier.Text ?? ((IdentifierNameSyntax)init.Expression).ToString();
            var value = Visit(init.Expression);
            return new { Name = name, Value = value };
        }).ToList();

        var propertyNames = properties.Select(p => p.Name).ToArray();
        var propertyTypes = properties.Select(p => p.Value.Type).ToArray();
        var dynamicType = DynamicTypeFactory.CreateType(typeof(T).Name, propertyNames, propertyTypes);

        var bindings = properties.Select(p => Expression.Bind(dynamicType.GetProperty(p.Name), p.Value));
        return Expression.MemberInit(Expression.New(dynamicType), bindings);
    }

    public override Expression VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
    {
        var expression = Visit(node.Expression);
        var whenNotNull = Visit(node.WhenNotNull);

        if (expression.Type.IsValueType && Nullable.GetUnderlyingType(expression.Type) == null)
        {
            throw new NotSupportedException("Conditional access is not supported on non-nullable value types: " + expression.Type);
        }

        if (!expression.Type.IsValueType || Nullable.GetUnderlyingType(expression.Type) != null)
        {
            return Expression.Condition(Expression.NotEqual(expression, Expression.Constant(null, expression.Type)),
                whenNotNull, Expression.Default(whenNotNull.Type)
            );
        }

        return whenNotNull;
    }

    public override Expression VisitMemberBindingExpression(MemberBindingExpressionSyntax node)
    {
        if (node.Parent is ConditionalAccessExpressionSyntax conditional)
        {
            var expression = Visit(conditional.Expression);
            var memberName = node.Name.Identifier.Text;
            return Expression.PropertyOrField(expression, memberName);
        }

        throw new NotSupportedException("Unsupported member binding: " + node.ToString());
    }

    public override Expression VisitElementAccessExpression(ElementAccessExpressionSyntax node)
    {
        var expression = Visit(node.Expression);
        var arguments = node.ArgumentList.Arguments.Select(arg => Visit(arg.Expression)).ToArray();

        if (expression.Type.IsArray)
        {
            return Expression.ArrayIndex(expression, arguments);
        }

        var indexer = expression.Type.GetProperties()
            .FirstOrDefault(p => p.GetIndexParameters().Length == arguments.Length);

        if (indexer != null)
        {
            return Expression.MakeIndex(expression, indexer, arguments);
        }

        throw new NotSupportedException("Unsupported element access: " + node.ToString());
    }
  }

/// <summary>
/// Parse lambda expressions from strings.
/// </summary>
public static class ExpressionParser
{
    /// <summary>
    /// Parses a lambda expression that returns a boolean value.
    /// </summary>
    /// <example>
    public static Expression<Func<T, bool>> ParsePredicate<T>(string expression, Func<string, Type> typeLocator = null)
    {
        return ParseLambda<T, bool>(expression, typeLocator);
    }

    /// <summary>
    /// Parses a lambda expression that returns a typed result.
    /// </summary>
    public static Expression<Func<T, TResult>> ParseLambda<T, TResult>(string expression, Func<string, Type> typeLocator = null)
    {
        var (parameter, body) = Parse<T>(expression, typeLocator);

        return Expression.Lambda<Func<T, TResult>>(body, parameter);
    }

    private static (ParameterExpression, Expression) Parse<T>(string expression, Func<string, Type> typeLocator)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(expression);
        var root = syntaxTree.GetRoot();
        var lambdaExpression = root.DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().FirstOrDefault();
        if (lambdaExpression == null)
        {
            throw new ArgumentException("Invalid lambda expression.");
        }
        var parameter = Expression.Parameter(typeof(T), lambdaExpression.Parameter.Identifier.Text);
        var visitor = new ExpressionSyntaxVisitor<T>(parameter, typeLocator);
        var body = visitor.Visit(lambdaExpression.Body);
        return (parameter, body);
    }

    /// <summary>
    /// Parses a lambda expression that returns untyped result.
    /// </summary>
    public static LambdaExpression ParseLambda<T>(string expression, Func<string, Type> typeLocator = null)
    {
        var (parameter, body) = Parse<T>(expression, typeLocator);

        return Expression.Lambda(body, parameter);
    }
}
