using System;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Radzen;

class ExpressionSyntaxVisitor : CSharpSyntaxVisitor<Expression>
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
    var right = Visit(node.Right);

    if (right.Type != left.Type)
    {
      right = Expression.Convert(right, left.Type);
    }

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
    var targetType = typeLocator?.Invoke(node.Type.ToString());

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

      var visitor = new ExpressionSyntaxVisitor(Expression.Parameter(itemType, lambda.Parameter.Identifier.Text), typeLocator);

      return visitor.Visit(lambda);
    }

    return Visit(argument.Expression);
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
          return Expression.Call(genericMethod, new[] { instance }.Concat(arguments));
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
}

public static class ExpressionParser
{
  public static Expression<Func<T, bool>> Parse<T>(string expression, Func<string, Type> typeLocator = null)
  {
    var syntaxTree = CSharpSyntaxTree.ParseText(expression);
    var root = syntaxTree.GetRoot();

    var lambdaExpression = root.DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().FirstOrDefault();

    if (lambdaExpression == null)
    {
      throw new ArgumentException("Invalid lambda expression.");
    }

    var parameter = Expression.Parameter(typeof(T), lambdaExpression.Parameter.Identifier.Text);

    var visitor = new ExpressionSyntaxVisitor(parameter, typeLocator);
    var body = visitor.Visit(lambdaExpression.Body);

    return Expression.Lambda<Func<T, bool>>(body, parameter);
  }
}
