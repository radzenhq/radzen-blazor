using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Linq.Dynamic.Core;
using Xunit;

#nullable enable

namespace Radzen.Blazor.Tests;

public enum CarType
{
    Sedan,
    Coupe
}

public class ExpressionParserTests
{
    class Person
    {
        public short? Age { get; set; }
        public string? Name { get; set; }
        public bool? Famous { get; set; }
        public DateTime BirthDate { get; set; }
    }

    public class Car
    {
        public CarType Type { get; set; }
    }

    public enum Status
    {
        Office,
        Remote,
    }

    class OrderDetail
    {
        public Product? Product { get; set; }
        public int Quantity { get; set; }
        public Order? Order { get; set; }

        public List<WorkStatus>? WorkStatuses { get; set; }
        public List<Status>? Statuses { get; set; }
    }

    class WorkStatus
    {
        public string? Name { get; set; }
    }

    class Category
    {
        public string? CategoryName { get; set; }
    }

    class Order
    {
        public DateTime OrderDate { get; set; }
    }

    class Product
    {
        public string? ProductName { get; set; }

        public Category? Category { get; set; }
    }

    [Fact]
    public void Should_ParseStringLiteralExpression()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("p => p.Name == \"foo\"");

        var func = expression.Compile();

        Assert.True(func(new Person() { Name = "foo" }));
    }

    [Fact]
    public void Should_ThrowIfStringLiteralIsNotClosed()
    {
        Assert.Throws<InvalidOperationException>(() => ExpressionParser.ParsePredicate<Person>("p => p.Name == \"foo"));
    }

    [Fact]
    public void Should_ParseCharacterLiteral()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<char>>("p => p.Value == 'f'");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<char> { Value = 'f' }));
    }

    [Fact]
    public void Should_ThrowExceptionsWhenThereAreMultipleCharacters()
    {
        Assert.Throws<InvalidOperationException>(() => ExpressionParser.ParsePredicate<ItemWithGenericProperty<char>>("p => p.Value == 'foo'"));
    }

    [Fact]
    public void Should_ThrowIfCharacterLiteralIsNotClosed()
    {
        Assert.Throws<InvalidOperationException>(() => ExpressionParser.ParsePredicate<ItemWithGenericProperty<char>>("p => p.Value == 'f"));
    }

    [Fact]
    public void Should_ReturnTargetType()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<char>, char>("p => p.Value");

        var func = expression.Compile();

        Assert.Equal('\n', func(new ItemWithGenericProperty<char> { Value = '\n' }));
    }

    [Fact]
    public void Should_ParseEscapeSequenceInCharacterLiteral()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<char>>("p => p.Value == '\\n'");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<char> { Value = '\n' }));
    }

    [Fact]
    public void Should_ParseUnicodeEscapeSequenceInCharacterLiteral()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<char>>(@"p => p.Value == '\u000A'");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<char> { Value = '\n' }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<char>>(@"p => p.Value == '\x000A'");

        func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<char> { Value = '\n' }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<char>>(@"p => p.Value == '\x0A'");

        func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<char> { Value = '\n' }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<char>>(@"p => p.Value == '\x00A'");

        func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<char> { Value = '\n' }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<char>>(@"p => p.Value == '\U0000000a'");

        func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<char> { Value = '\n' }));
    }

    [Fact]
    public void Should_ThrowIfUnicodeEscapeSequenceIsTooLong()
    {
        Assert.Throws<InvalidOperationException>(() => ExpressionParser.ParsePredicate<ItemWithGenericProperty<char>>(@"p => p.Value == '\u00000'"));
    }

    [Fact]
    public void Should_ThrowIfUnicodeEscapeSequenceIsTooShort()
    {
        Assert.Throws<InvalidOperationException>(() => ExpressionParser.ParsePredicate<ItemWithGenericProperty<char>>(@"p => p.Value == '\u0'"));
    }

    [Fact]
    public void Should_ParseUnicodeEscapeSequenceInStringLiteral()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<string>>(@"p => p.Value == ""\u000A""");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<string> { Value = "\n" }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<string>>(@"p => p.Value == ""\x000A""");

        func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<string> { Value = "\n" }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<string>>(@"p => p.Value == ""\x0A""");

        func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<string> { Value = "\n" }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<string>>(@"p => p.Value == ""\x00A""");

        func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<string> { Value = "\n" }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<string>>(@"p => p.Value == ""\U0000000a""");

        func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<string> { Value = "\n" })); 
    }

    [Fact]
    public void Should_ParseEscapeSequenceInStringLiteral()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<string>>(@"p => p.Value == ""\n""");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<string> { Value = "\n" }));
    }

    [Fact]
    public void Should_ParseNotEqualsExpression()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("p => p.Name != \"foo\"");

        var func = expression.Compile();

        Assert.True(func(new Person() { Name = "bar" }));
        Assert.False(func(new Person() { Name = "foo" }));
    }

    [Fact]
    public void Should_ParseGreaterThanExpression()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("p => p.Age > 30");

        var func = expression.Compile();

        Assert.True(func(new Person() { Age = 31 }));
        Assert.False(func(new Person() { Age = 30 }));
        Assert.False(func(new Person() { Age = 29 }));
    }

    [Fact]
    public void Should_ParseLessThanExpression()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("p => p.Age < 30");

        var func = expression.Compile();

        Assert.True(func(new Person() { Age = 29 }));
        Assert.False(func(new Person() { Age = 30 }));
        Assert.False(func(new Person() { Age = 31 }));
    }

    [Fact]
    public void Should_ParseLessThanOrEqualExpression()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("p => p.Age <= 30");

        var func = expression.Compile();

        Assert.True(func(new Person() { Age = 29 }));
        Assert.True(func(new Person() { Age = 30 }));
        Assert.False(func(new Person() { Age = 31 }));
    }

    [Fact]
    public void Should_ParseGreaterThanOrEqualExpression()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("p => p.Age >= 30");

        var func = expression.Compile();

        Assert.True(func(new Person() { Age = 31 }));
        Assert.True(func(new Person() { Age = 30 }));
        Assert.False(func(new Person() { Age = 29 }));
    }

    [Fact]
    public void Should_ParseExpressionWithParentheses()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("p => (p.Name == \"foo\")");

        var func = expression.Compile();

        Assert.True(func(new Person() { Name = "foo" }));
    }

    [Fact]
    public void Should_ThrowIfParenthesesAreNotClosed()
    {
        Assert.Throws<InvalidOperationException>(() => ExpressionParser.ParsePredicate<Person>("p => (p.Name == \"foo\""));
    }

    [Fact]
    public void Should_ParseLogicalOperations()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("p => (p.Name == \"foo\" || p.Name == \"bar\") && p.Age == 50");

        var func = expression.Compile();

        Assert.True(func(new Person() { Name = "foo", Age = 50 }));
        Assert.True(func(new Person() { Name = "bar", Age = 50 }));
        Assert.False(func(new Person() { Name = "baz", Age = 50 }));
        Assert.False(func(new Person() { Name = "foo", Age = 51 }));
    }

    [Fact]
    public void Should_ParseFractionNumbersAsDouble()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<double>>("p => p.Value == 50.5");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<double> { Value = 50.5 }));
    }

    [Fact]
    public void Should_ParseNegativeNumbers()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<double>>("p => p.Value == -50.5");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<double> { Value = -50.5 }));
    }

    [Fact]
    public void Should_ParsePositiveNumbers()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<double>>("p => p.Value == +50.5");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<double> { Value = +50.5 }));
    }
    
    [Fact]
    public void Should_ParseFloatLiteral()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<float>>("p => p.Value == 50.5f");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<float> { Value = 50.5f }));
    }

    [Fact]
    public void Should_ParseDoubleLiteral()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<double>>("p => p.Value == 50.5d");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<double> { Value = 50.5 }));
    }

    [Fact]
    public void Should_ParseDecimalLiteral()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<decimal>>("p => p.Value == 50.5m");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<decimal> { Value = 50.5m }));
    }

    [Fact]
    public void Should_ParseLongLiteral()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<long>>("p => p.Value == 50L");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<long> { Value = 50L }));
    }

    class ItemWithGenericProperty<T>
    {
        public T Value { get; set; } = default!;
    }

    [Fact]
    public void Should_ParseAdditionExpression()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value + 5");

        var func = expression.Compile();

        Assert.Equal(26 + 5, func(new ItemWithGenericProperty<int> { Value = 26 }));
    }

    [Fact]
    public void Should_ParseBinaryAndExpressionWithAddition()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value + 5 & 1");
        var func = expression.Compile();

        Assert.Equal(25 + 5 & 1, func(new ItemWithGenericProperty<int> { Value = 25 }));
    }

    [Fact]
    public void Should_ParseBinaryAndExpressionWithMultiplication()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value * 5 & 1");
        var func = expression.Compile();

        Assert.Equal(25 * 5 & 1, func(new ItemWithGenericProperty<int> { Value = 25 }));
    }

    [Fact]
    public void Should_ParseBinaryOrExpressionWithAddition()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value + 5 | 1");
        var func = expression.Compile();

        Assert.Equal(25 + 5 | 1, func(new ItemWithGenericProperty<int> { Value = 25 }));
    }

    [Fact]
    public void Should_ParseBinaryOrExpressionWithMultiplication()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value * 5 | 1");
        var func = expression.Compile();

        Assert.Equal(25 * 5 | 1, func(new ItemWithGenericProperty<int> { Value = 25 }));
    }

    [Fact]
    public void Should_ParseBinaryXorExpressionWithAddition()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value + 5 ^ 1");
        var func = expression.Compile();

        Assert.Equal(25 + 5 ^ 1, func(new ItemWithGenericProperty<int> { Value = 25 }));
    }

    [Fact]
    public void Should_ParseBinaryXorExpressionWithMultiplication()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value * 5 ^ 1");
        var func = expression.Compile();

        Assert.Equal(25 * 5 ^ 1, func(new ItemWithGenericProperty<int> { Value = 25 }));
    }

    [Fact]
    public void Should_ParseLeftShiftExpressionWithAddition()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => (p.Value + 5) << 1");
        var func = expression.Compile();

        Assert.Equal((25 + 5) << 1, func(new ItemWithGenericProperty<int> { Value = 25 }));
    }

    [Fact]
    public void Should_ParseLeftShiftExpressionWithMultiplication()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => (p.Value * 5) << 1");
        var func = expression.Compile();

        Assert.Equal((25 * 5) << 1, func(new ItemWithGenericProperty<int> { Value = 25 }));
    }

    [Fact]
    public void Should_ParseRightShiftExpressionWithAddition()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => (p.Value + 5) >> 1");
        var func = expression.Compile();

        Assert.Equal((25 + 5) >> 1, func(new ItemWithGenericProperty<int> { Value = 25 }));
    }

    [Fact]
    public void Should_ParseRightShiftExpressionWithMultiplication()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => (p.Value * 5) >> 1");
        var func = expression.Compile();

        Assert.Equal((25 * 5) >> 1, func(new ItemWithGenericProperty<int> { Value = 25 }));
    }

    [Fact]
    public void Should_ParseMultipleAdditions()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value + 5 + 10");

        var func = expression.Compile();

        Assert.Equal(16 + 5 + 10, func(new ItemWithGenericProperty<int> { Value = 16 }));
    }

    [Fact]
    public void Should_ParseSubtractionExpression()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value - 5");

        var func = expression.Compile();

        Assert.Equal(36 - 5, func(new ItemWithGenericProperty<int> { Value = 36 }));
    }

    [Fact]
    public void Should_ParseMixedAdditionAndSubtraction()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value + 10 - 5");

        var func = expression.Compile();

        Assert.Equal(26 + 10 - 5, func(new ItemWithGenericProperty<int> { Value = 26 }));
    }

    [Fact]
    public void Should_ParseMultiplicationExpression()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value * 2");

        var func = expression.Compile();

        Assert.Equal(16 * 2, func(new ItemWithGenericProperty<int> { Value = 16 }));
    }

    [Fact]
    public void Should_RespectOperatorPrecedence()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value * 2 + 5");

        var func = expression.Compile();

        Assert.Equal(13 * 2 + 5, func(new ItemWithGenericProperty<int> { Value = 13 })); 

        var expression2 = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value * (2 + 5)");

        var func2 = expression2.Compile();

        Assert.Equal(5 * (2 + 5), func2(new ItemWithGenericProperty<int> { Value = 5 }));
    }

    [Fact]
    public void Should_ParseDivisionExpression()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value / 2");

        var func = expression.Compile();

        Assert.Equal(32 / 2, func(new ItemWithGenericProperty<int> { Value = 32 }));
    }

    [Fact]
    public void Should_ParseMixedMultiplicationAndDivision()
    {
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value * 2 / 4");

        var func = expression.Compile();

        Assert.Equal(32 * 2 / 4, func(new ItemWithGenericProperty<int> { Value = 32 }));

        var expression2 = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value * (8 / 4)");

        var func2 = expression2.Compile();

        Assert.Equal(8 * (8 / 4), func2(new ItemWithGenericProperty<int> { Value = 8 }));
    }

    [Fact]
    public void Should_ParseTernaryExpression()
    {
        var expression = ExpressionParser.ParseLambda<Person, string>("p => p.Age > 30 ? \"foo\" : \"bar\"");

        var func = expression.Compile();

        Assert.Equal("foo", func(new Person() { Age = 31 }));
        Assert.Equal("bar", func(new Person() { Age = 29 }));
    }

    [Fact]
    public void Should_ParseNestedTernaryExpression()
    {
        var expression = ExpressionParser.ParseLambda<Person, string>("p => p.Age > 30 ? \"foo\" : (p.Age < 20 ? \"bar\" : \"baz\")");

        var func = expression.Compile();

        Assert.Equal("foo", func(new Person() { Age = 31 }));
        Assert.Equal("bar", func(new Person() { Age = 19 }));
        Assert.Equal("baz", func(new Person() { Age = 25 }));
    }

    [Fact]
    public void Should_ParseConditionalAccessExpression()
    {
        var expression = ExpressionParser.ParsePredicate<OrderDetail>("it => it.Product?.ProductName == \"foo\"");

        var func = expression.Compile();

        Assert.True(func(new OrderDetail() { Product = new Product { ProductName = "foo" } }));
        Assert.False(func(new OrderDetail() { Product = new Product { ProductName = "bar" } }));
        Assert.False(func(new OrderDetail() { Product = null }));
    }

    [Fact]
    public void Should_ParseNullLiteral()
    {
        var expression = ExpressionParser.ParsePredicate<OrderDetail>("it => it.Product == null");

        var func = expression.Compile();

        Assert.True(func(new OrderDetail() { Product = null }));
        Assert.False(func(new OrderDetail() { Product = new Product() }));
    }

    [Fact]
    public void Should_ParseTrueLiteral()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("it => it.Famous == true");

        var func = expression.Compile();

        Assert.True(func(new Person() { Famous = true }));
        Assert.False(func(new Person() { Famous = false }));
    }

    [Fact]
    public void Should_ParseFalseLiteral()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("it => it.Famous == false");

        var func = expression.Compile();

        Assert.True(func(new Person() { Famous = false }));
        Assert.False(func(new Person() { Famous = true }));
    }

    [Fact]
    public void Support_MethodInvocation()
    {
        var expression = ExpressionParser.ParsePredicate<OrderDetail>("it => it.Product.ProductName.Contains(\"foo\")");

        var func = expression.Compile();

        Assert.True(func(new OrderDetail() { Product = new Product { ProductName = "foo" } }));
        Assert.False(func(new OrderDetail() { Product = new Product { ProductName = "bar" } }));
    }

    [Fact]
    public void Should_ParseConditionalMethodInvocation()
    {
        var expression = ExpressionParser.ParsePredicate<OrderDetail>("it => it.Product?.ProductName?.ToLower() == \"foo\"");

        var func = expression.Compile();

        Assert.True(func(new OrderDetail() { Product = new Product { ProductName = "FOO" } }));
        Assert.False(func(new OrderDetail() { Product = new Product { ProductName = "BAR" } }));
        Assert.False(func(new OrderDetail() { Product = null }));
    }

    [Fact]
    public void Should_ParseConditionalExpression()
    {
        var expression = ExpressionParser.ParsePredicate<OrderDetail>("it => (it.Product.ProductName == null ? \"\" : it.Product.ProductName).Contains(\"Queso\") && it.Quantity == 50");

        var func = expression.Compile();

        Assert.True(func(new OrderDetail() { Product = new Product { ProductName = "Queso" }, Quantity = 50 }));
    }

    [Fact]
    public void Should_ParseNestedLogicalOperations()
    {
        var expression = ExpressionParser.ParsePredicate<OrderDetail>("it => (it.Product.ProductName == null ? \"\" : it.Product.ProductName).Contains(\"Queso\") && (it.Quantity == 50 || it.Quantity == 12)");

        var func = expression.Compile();

        Assert.True(func(new OrderDetail() { Product = new Product { ProductName = "Queso" }, Quantity = 12 }));
    }

    [Fact]
    public void Should_SupportDateTimeParsing()
    {
        var expression = ExpressionParser.ParsePredicate<OrderDetail>("it => it.Order.OrderDate >= DateTime.Parse(\"2025-02-11\")");

        var func = expression.Compile();
        Assert.True(func(new OrderDetail { Order = new Order { OrderDate = new DateTime(2025, 2, 11) } }));
    }

    [Fact]
    public void Should_SupportDateTimeParsingWithCultureInfo()
    {
        var expression = ExpressionParser.ParsePredicate<OrderDetail>("it => it.Order.OrderDate >= DateTime.Parse(\"2025-02-11\", CultureInfo.InvariantCulture)");

        var func = expression.Compile();
        Assert.True(func(new OrderDetail { Order = new Order { OrderDate = new DateTime(2025, 2, 11) } }));
    }

    [Fact]
    public void Should_SupportDateOnlyParsing()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<DateOnly>>("it => it.Value >= DateOnly.Parse(\"2025-02-11\")");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<DateOnly> { Value = new DateOnly(2025, 2, 11) }));
    }

    [Fact]
    public void Should_SupportTimeOnlyParsing()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<TimeOnly>>("it => it.Value >= TimeOnly.Parse(\"12:00:00\")");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<TimeOnly> { Value = new TimeOnly(12, 0, 0) }));
    }

    [Fact]
    public void Should_SupportGuidParsing()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<Guid>>("it => it.Value == Guid.Parse(\"f0e7e7d8-4f4d-4b5f-8b3e-3f1d1b4f5f5f\")");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<Guid> { Value = Guid.Parse("f0e7e7d8-4f4d-4b5f-8b3e-3f1d1b4f5f5f") }));
    }

    [Fact]
    public void Should_SupportDateTimeOffsetParsing()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<DateTimeOffset>>("it => it.Value == DateTimeOffset.Parse(\"2025-02-11\")");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<DateTimeOffset> { Value = DateTimeOffset.Parse("2025-02-11") }));
    }

    [Fact]
    public void Should_SupportIntStaticMembers()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<int>>("it => it.Value == int.MaxValue");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<int> { Value = int.MaxValue }));
        Assert.False(func(new ItemWithGenericProperty<int> { Value = int.MinValue }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<int>>("it => it.Value == int.MinValue");
        func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<int> { Value = int.MinValue }));
        Assert.False(func(new ItemWithGenericProperty<int> { Value = int.MaxValue }));
    }

    [Fact]
    public void Should_SupportDecimalStaticMembers()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<decimal>>("it => it.Value == decimal.MaxValue");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<decimal> { Value = decimal.MaxValue }));
        Assert.False(func(new ItemWithGenericProperty<decimal> { Value = decimal.MinValue }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<decimal>>("it => it.Value == decimal.MinValue");
        func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<decimal> { Value = decimal.MinValue }));
        Assert.False(func(new ItemWithGenericProperty<decimal> { Value = decimal.MaxValue }));
    }

    [Fact]
    public void Should_SupportCharStaticMethods()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<char>>("it => char.IsLetter(it.Value)");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<char> { Value = 'A' }));
        Assert.False(func(new ItemWithGenericProperty<char> { Value = '1' }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<char>>("it => char.IsDigit(it.Value)");
        func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<char> { Value = '1' }));
        Assert.False(func(new ItemWithGenericProperty<char> { Value = 'A' }));
    }

    [Fact]
    public void Should_SupportDoubleStaticMembers()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<double>>("it => it.Value == double.MaxValue");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<double> { Value = double.MaxValue }));
        Assert.False(func(new ItemWithGenericProperty<double> { Value = double.MinValue }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<double>>("it => it.Value == double.MinValue");
        func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<double> { Value = double.MinValue }));
        Assert.False(func(new ItemWithGenericProperty<double> { Value = double.MaxValue }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<double>>("it => double.IsNaN(it.Value)");
        func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<double> { Value = double.NaN }));
        Assert.False(func(new ItemWithGenericProperty<double> { Value = 0.0 }));
    }

    [Fact]
    public void Should_SupportFloatStaticMembers()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<float>>("it => it.Value == float.MaxValue");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<float> { Value = float.MaxValue }));
        Assert.False(func(new ItemWithGenericProperty<float> { Value = float.MinValue }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<float>>("it => it.Value == float.MinValue");
        func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<float> { Value = float.MinValue }));
        Assert.False(func(new ItemWithGenericProperty<float> { Value = float.MaxValue }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<float>>("it => float.IsNaN(it.Value)");
        func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<float> { Value = float.NaN }));
        Assert.False(func(new ItemWithGenericProperty<float> { Value = 0.0f }));
    }

    [Fact]
    public void Should_SupportStringStaticMethods()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<string?>>("it => string.IsNullOrEmpty(it.Value)");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<string?> { Value = null }));
        Assert.True(func(new ItemWithGenericProperty<string?> { Value = "" }));
        Assert.False(func(new ItemWithGenericProperty<string?> { Value = "test" }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<string?>>("it => string.IsNullOrWhiteSpace(it.Value)");
        func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<string?> { Value = null }));
        Assert.True(func(new ItemWithGenericProperty<string?> { Value = " " }));
        Assert.False(func(new ItemWithGenericProperty<string?> { Value = "test" }));
    }

    [Fact]
    public void Should_SupportLongStaticMembers()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<long>>("it => it.Value == long.MaxValue");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<long> { Value = long.MaxValue }));
        Assert.False(func(new ItemWithGenericProperty<long> { Value = long.MinValue }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<long>>("it => it.Value == long.MinValue");
        func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<long> { Value = long.MinValue }));
        Assert.False(func(new ItemWithGenericProperty<long> { Value = long.MaxValue }));
    }

    [Fact]
    public void Should_SupportShortStaticMembers()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<short>>("it => it.Value == short.MaxValue");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<short> { Value = short.MaxValue }));
        Assert.False(func(new ItemWithGenericProperty<short> { Value = short.MinValue }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<short>>("it => it.Value == short.MinValue");
        func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<short> { Value = short.MinValue }));
        Assert.False(func(new ItemWithGenericProperty<short> { Value = short.MaxValue }));
    }

    [Fact]
    public void Should_SupportByteStaticMembers()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<byte>>("it => it.Value == byte.MaxValue");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<byte> { Value = byte.MaxValue }));
        Assert.False(func(new ItemWithGenericProperty<byte> { Value = byte.MinValue }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<byte>>("it => it.Value == byte.MinValue");
        func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<byte> { Value = byte.MinValue }));
        Assert.False(func(new ItemWithGenericProperty<byte> { Value = byte.MaxValue }));
    }

    [Fact]
    public void Should_SupportSByteStaticMembers()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<sbyte>>("it => it.Value == sbyte.MaxValue");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<sbyte> { Value = sbyte.MaxValue }));
        Assert.False(func(new ItemWithGenericProperty<sbyte> { Value = sbyte.MinValue }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<sbyte>>("it => it.Value == sbyte.MinValue");
        func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<sbyte> { Value = sbyte.MinValue }));
        Assert.False(func(new ItemWithGenericProperty<sbyte> { Value = sbyte.MaxValue }));
    }

    [Fact]
    public void Should_SupportUIntStaticMembers()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<uint>>("it => it.Value == uint.MaxValue");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<uint> { Value = uint.MaxValue }));
        Assert.False(func(new ItemWithGenericProperty<uint> { Value = uint.MinValue }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<uint>>("it => it.Value == uint.MinValue");
        func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<uint> { Value = uint.MinValue }));
        Assert.False(func(new ItemWithGenericProperty<uint> { Value = uint.MaxValue }));
    }

    [Fact]
    public void Should_SupportULongStaticMembers()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<ulong>>("it => it.Value == ulong.MaxValue");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<ulong> { Value = ulong.MaxValue }));
        Assert.False(func(new ItemWithGenericProperty<ulong> { Value = ulong.MinValue }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<ulong>>("it => it.Value == ulong.MinValue");
        func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<ulong> { Value = ulong.MinValue }));
        Assert.False(func(new ItemWithGenericProperty<ulong> { Value = ulong.MaxValue }));
    }

    [Fact]
    public void Should_SupportUShortStaticMembers()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<ushort>>("it => it.Value == ushort.MaxValue");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<ushort> { Value = ushort.MaxValue }));
        Assert.False(func(new ItemWithGenericProperty<ushort> { Value = ushort.MinValue }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<ushort>>("it => it.Value == ushort.MinValue");
        func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<ushort> { Value = ushort.MinValue }));
        Assert.False(func(new ItemWithGenericProperty<ushort> { Value = ushort.MaxValue }));
    }

    [Fact]
    public void Should_SupportBoolStaticMembers()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<bool>>("it => it.Value == true");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<bool> { Value = true }));
        Assert.False(func(new ItemWithGenericProperty<bool> { Value = false }));

        expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<bool>>("it => it.Value == false");
        func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<bool> { Value = false }));
        Assert.False(func(new ItemWithGenericProperty<bool> { Value = true }));
    }

    static Type? TypeLocator(string type)
    {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.FullName == type);
    }

    [Fact]
    public void Should_SupportEnumCasts()
    {
        var expression = ExpressionParser.ParsePredicate<Car>($"it => it.Type == (Radzen.Blazor.Tests.CarType)1", TypeLocator);

        var func = expression.Compile();

        Assert.True(func(new Car { Type = CarType.Coupe }));
    }

    [Fact]
    public void Should_SupportNullableTypeCasts()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<int?>>("it => it.Value == (int?)1");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<int?> { Value = 1 }));
    }

    [Fact]
    public void Should_SupportEnumerableExtensionMethods()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<int[]>>("it => it.Value.Contains(1)");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<int[]> { Value = [1] }));
    }

    [Fact]
    public void Should_SupportNonGenericEnumerableExtensionMethods()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<IList<int>>>("it => it.Value.Sum() == 1");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<IList<int>> { Value = new List<int> { 1 } }));
    }

    [Fact]
    public void Should_SupportImplicitArrayInitialization()
    {
        var expression = ExpressionParser.ParsePredicate<OrderDetail>("it => (new []{\"Tofu\"}).Contains(it.Product.ProductName)");
        var func = expression.Compile();

        Assert.True(func(new OrderDetail { Product = new Product { ProductName = "Tofu" } }));
    }

    [Fact]
    public void Should_SupportTypedArrayInitialization()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("it => new bool?[]{ false }.Contains(it.Famous)");
        var func = expression.Compile();

        Assert.True(func(new Person { Famous = false }));
    }

    [Fact]
    public void Should_SupportNullableProperties()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("it => it.Age == 50");

        var func = expression.Compile();

        Assert.True(func(new Person { Age = 50 }));
    }

    [Fact]
    public void Should_SupportNullablePropertiesWithArray()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("it => (new []{}).Contains(it.Famous)");

        var func = expression.Compile();

        Assert.False(func(new Person { Famous = null }));
    }

    [Fact]
    public void Should_SupportDateTimeWithArray()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("it => (new []{DateTime.Parse(\"5/5/2000 12:00:00 AM\")}).Contains(it.BirthDate)");

        var func = expression.Compile();

        Assert.True(func(new Person { BirthDate = DateTime.Parse("5/5/2000 12:00:00 AM") }));
    }

    [Fact]
    public void Should_SupportNumericConversion()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<double>>("it => it.Value == 50");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<double> { Value = 50.0 }));
    }

    [Fact]
    public void Should_SupportToLower()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("it => (it.Name == null ? \"\" : it.Name).ToLower().Contains(\"na\".ToLower())");

        var func = expression.Compile();

        Assert.True(func(new Person { Name = "Nana" }));
    }

    [Fact]
    public void Should_SupportArrayIndexAccess()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<int[]>>("it => it.Value[0] == 1");
        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<int[]> { Value = [1] }));
    }

    [Fact]
    public void Should_SupportDictionaryIndexAccess()
    {
        var expression = ExpressionParser.ParsePredicate<Dictionary<string, object>>("it => (int)it[\"foo\"] == 1");
        var func = expression.Compile();
        Assert.True(func(new Dictionary<string, object> { ["foo"] = 1 }));
    }

    [Fact]
    public void Should_SupportDictionaryIndexAccessWithNullableCast()
    {
        var expression = ExpressionParser.ParsePredicate<Dictionary<string, int?>>("it => (Int32?)it[\"foo\"] == null");
        var func = expression.Compile();
        Assert.True(func(new Dictionary<string, int?> { ["foo"] = null }));
    }

    [Fact]
    public void Should_SupportTernaryWithNullCoalescing()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<Int16?>>("x => ((((x == null) ? null : x.Value) ?? null) == 4)");

        var func = expression.Compile();

        Assert.False(func(new ItemWithGenericProperty<Int16?> { Value = null }));
        Assert.True(func(new ItemWithGenericProperty<Int16?> { Value = 4 }));
    }

    [Fact]
    public void Should_SupportNullCoalescing()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<double?>>("it => (it.Value ?? 0) == 0");

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<double?> { Value = null }));
    }

    [Fact]
    public void Should_ParseChainedNullCoalescing()
    {
        var expression = ExpressionParser.ParseLambda<Person, string>("p => p.Name ?? p.Age?.ToString() ?? \"default\"");

        var func = expression.Compile();

        Assert.Equal("default", func(new Person() { Name = null, Age = null }));
        Assert.Equal("42", func(new Person() { Name = null, Age = 42 }));
        Assert.Equal("John", func(new Person() { Name = "John", Age = 42 }));
    }

    [Fact]
    public void Should_ParseNestedNullCoalescing()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("it => ((it.Name ?? it.Age?.ToString()) ?? \"default\").Contains(\"foo\")");

        var func = expression.Compile();

        Assert.True(func(new Person { Name = "foo" }));
        Assert.False(func(new Person { Name = null, Age = null }));
    }

    [Fact]
    public void Should_SupportNullConditionAndCoalescence()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("it => (((it == null) ? null : it.Name) ?? \"\").Contains(\"Da\")");

        var func = expression.Compile();

        Assert.True(func(new Person { Name = "Dali" }));
    }

    [Fact]
    public void Should_SupportNestedLambdas()
    {
        var expression = ExpressionParser.ParsePredicate<OrderDetail>("it => it.WorkStatuses.Any(i => (new []{\"Office\"}).Contains(i.Name))");
        var func = expression.Compile();

        Assert.True(func(new OrderDetail { WorkStatuses = [new() { Name = "Office" }] }));
    }

    [Fact]
    public void Should_SupportUnaryNot()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("it => !it.Famous");
        var func = expression.Compile();

        Assert.True(func(new Person { Famous = false }));
        Assert.False(func(new Person { Famous = true }));
    }

    [Fact]
    public void Should_SupportEnumWithoutCasts()
    {
        var typeLocator = (string type) => AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.FullName == type);

        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<CarType[]>>("it => it.Value.Any(i => (new []{0}).Contains(i))", typeLocator);

        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<CarType[]> { Value = [CarType.Sedan] }));
    }

    [Fact]
    public void Should_SupportNestedLambdasWithEnums()
    {
        var expression = ExpressionParser.ParsePredicate<OrderDetail>("it => it.Statuses.Any(i => (new []{1}).Contains(i))");
        var func = expression.Compile();

        Assert.True(func(new OrderDetail { Statuses = new List<Status>() { (Status)1 } }));
    }

    [Fact]
    public void Should_SupportNestedLambdasWithComplexMethod()
    {
        var expression = ExpressionParser.ParsePredicate<OrderDetail>("it => new [] { (Status)1 }.Intersect(it.Statuses).Any()", type => typeof(Status));
        var func = expression.Compile();

        Assert.True(func(new OrderDetail { Statuses = new List<Status>() { (Status)1 } }));
    }

    [Fact]
    public void Should_CreateProjection()
    {
        var expression = ExpressionParser.ParseLambda<OrderDetail, object>("it => new { ProductName = it.Product.ProductName}");

        var func = expression.Compile();

        var result = func.DynamicInvoke(new OrderDetail { Product = new Product { ProductName = "Queso" } });

        var property = result?.GetType().GetProperty("ProductName");

        Assert.Equal(typeof(string), property?.PropertyType);

        Assert.Equal("Queso", property?.GetValue(result));
    }

    [Fact]
    public void Should_CreateProjectionFromNestedAccess()
    {
        var expression = ExpressionParser.ParseLambda<OrderDetail, object>("it => new { it.Product.Category.CategoryName }");

        var func = expression.Compile();

        var orderDetail = new OrderDetail { Product = new Product { Category = new Category { CategoryName = "Beverages" } } };

        var x = new { orderDetail?.Product?.Category?.CategoryName };

        var result = func.DynamicInvoke(orderDetail);

        var property = result?.GetType().GetProperty("CategoryName");

        Assert.Equal(typeof(string), property?.PropertyType);

        Assert.Equal("Beverages", property?.GetValue(result));
    }

    [Fact]
    public void Should_CreateProjectionFromNestedConditionalAccess()
    {
        var expression = ExpressionParser.ParseLambda<OrderDetail, object>("it => new { it.Product?.Category?.CategoryName }");

        var func = expression.Compile();

        var orderDetail = new OrderDetail { Product = new Product { Category = new Category { CategoryName = "Beverages" } } };

        var x = new { orderDetail?.Product?.Category?.CategoryName };

        var result = func.DynamicInvoke(orderDetail);

        var property = result?.GetType().GetProperty("CategoryName");

        Assert.Equal(typeof(string), property?.PropertyType);

        Assert.Equal("Beverages", property?.GetValue(result));
    }

    [Fact]
    public void Should_CreateProjectionFromConditionalAccess()
    {
        var expression = ExpressionParser.ParseLambda<OrderDetail, object>("it => new { ProductName = it.Product?.ProductName}");
        var func = expression.Compile();
        var result = func.DynamicInvoke(new OrderDetail { Product = null });

        var property = result?.GetType().GetProperty("ProductName");

        Assert.Equal(typeof(string), property?.PropertyType);
        Assert.Null(property?.GetValue(result));
    }

    [Fact]
    public void Should_CreateProjectionFromNestedConditionalAccessAndAssignment()
    {
        var expression = ExpressionParser.ParseLambda<OrderDetail, object>("it => new { CategoryName = it.Product?.Category?.CategoryName}");
        var func = expression.Compile();
        var result = func.DynamicInvoke(new OrderDetail { Product = null });

        var property = result?.GetType().GetProperty("CategoryName");

        Assert.Equal(typeof(string), property?.PropertyType);
        Assert.Null(property?.GetValue(result));
    }

    [Fact]
    public void Should_SupportStringConcatAndSubstring()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("it => (it.Name + \" Smith\").Substring(0, 5) == \"John \"");
        var func = expression.Compile();

        Assert.True(func(new Person { Name = "John" }));
        Assert.False(func(new Person { Name = "Jane" }));
    }

    [Fact]
    public void Should_ParseScientificNotation()
    {
        // Test double with scientific notation
        var doubleExpression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<double>>("it => it.Value == 1.23e4");
        var doubleFunc = doubleExpression.Compile();
        Assert.True(doubleFunc(new ItemWithGenericProperty<double> { Value = 12300.0 }));

        // Test negative exponent
        doubleExpression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<double>>("it => it.Value == 1.23e-4");
        doubleFunc = doubleExpression.Compile();
        Assert.True(doubleFunc(new ItemWithGenericProperty<double> { Value = 0.000123 }));

        // Test with capital E
        doubleExpression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<double>>("it => it.Value == 1.23E4");
        doubleFunc = doubleExpression.Compile();
        Assert.True(doubleFunc(new ItemWithGenericProperty<double> { Value = 12300.0 }));

        // Test with float suffix
        var floatExpression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<float>>("it => it.Value == 1.23e4f");
        var floatFunc = floatExpression.Compile();
        Assert.True(floatFunc(new ItemWithGenericProperty<float> { Value = 12300.0f }));
    }

    [Fact]
    public void Should_ParseHexNumbers()
    {
        // Test int with hex number
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<int>>("it => it.Value == 0x1A");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<int> { Value = 26 }));
    }

/*
    [Fact]
    public void Should_SupportComplexMathOperations()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("it => Math.Pow(it.Age ?? 0, 2) + Math.Sqrt(16) == 34");
        var func = expression.Compile();
        Assert.True(func(new Person { Age = 5 }));
    }
*/

    [Fact]
    public void Should_SupportDeepNestedPropertyAccess()
    {
        var f = (OrderDetail it) => it.Product?.Category?.CategoryName?.Length > 0;

        var expression = ExpressionParser.ParsePredicate<OrderDetail>("it => it.Product?.Category?.CategoryName?.Length > 0");
        var func = expression.Compile();
        Assert.True(func(new OrderDetail { Product = new Product { Category = new Category { CategoryName = "Test" } } }));
    }

    [Fact]
    public void Should_SupportDateTimeOperations()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("it => (it.BirthDate.AddYears(1) - DateTime.Now).TotalDays > 0");
        var func = expression.Compile();
        Assert.True(func(new Person { BirthDate = DateTime.Now.AddDays(400) }));
    }

    [Fact]
    public void Should_SupportComplexConditionals()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("it => (it.Age > 18 ? \"Adult\" : \"Minor\") == \"Adult\" && (it.Famous ?? false)");
        var func = expression.Compile();
        Assert.True(func(new Person { Age = 25, Famous = true }));
    }

    [Fact]
    public void Should_SupportMethodChaining()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("it => it.Name?.Trim().ToUpper().Contains(\"JOHN\") == true");
        var func = expression.Compile();
        Assert.True(func(new Person { Name = " John " }));
    }

    [Fact]
    public void Should_SupportMultipleCasts()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<object>>("it => (int)(double)it.Value == 42");
        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<object> { Value = 42.0 }));
        Assert.True(func(new ItemWithGenericProperty<object> { Value = 42.5 })); // Will be truncated to 42
        Assert.False(func(new ItemWithGenericProperty<object> { Value = 41.9 }));
    }

    [Fact]
    public void Should_SupportMultipleCastsWithNullable()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<object?>>("it => (int?)(double?)it.Value == 42");
        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<object?> { Value = 42.0 }));
        Assert.True(func(new ItemWithGenericProperty<object?> { Value = 42.5 })); // Will be truncated to 42
        Assert.False(func(new ItemWithGenericProperty<object?> { Value = 41.9 }));
        Assert.False(func(new ItemWithGenericProperty<object?> { Value = null })); // null is not equal to 42
    }

    [Fact]
    public void Should_SupportMultipleCastsWithDifferentTypes()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<object>>("it => ((int)(double)it.Value).ToString() == \"42\"");
        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<object> { Value = 42.0 }));
        Assert.True(func(new ItemWithGenericProperty<object> { Value = 42.5 })); // Will be truncated to 42
        Assert.False(func(new ItemWithGenericProperty<object> { Value = 41.9 }));
    }

    [Fact]
    public void Should_SupportNestedTypeCasts()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<object>>("it => ((int)((double)((decimal)it.Value))).ToString() == \"42\"");
        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<object> { Value = 42.0m }));
        Assert.True(func(new ItemWithGenericProperty<object> { Value = 42.5m })); // Will be truncated to 42
        Assert.False(func(new ItemWithGenericProperty<object> { Value = 41.9m }));
    }

    [Fact]
    public void Should_SupportThreeLevelCasts()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<object>>("it => (int)(double)(decimal)it.Value == 42");
        var func = expression.Compile();

        Assert.True(func(new ItemWithGenericProperty<object> { Value = 42.0m }));
        Assert.True(func(new ItemWithGenericProperty<object> { Value = 42.5m })); // Will be truncated to 42
        Assert.False(func(new ItemWithGenericProperty<object> { Value = 41.9m }));
    }

    [Fact]
    public void Should_SupportComplexArrayOperations()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<int[]>>("it => it.Value.Length > 2");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<int[]> { Value = new[] { 1, 2, 3 } }));
        Assert.False(func(new ItemWithGenericProperty<int[]> { Value = new[] { 1, 2 } }));
    }

    [Fact]
    public void Should_SupportNestedMethodCallsWithParameters()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("p => p.Name.Substring(0, 4).ToUpper() == \"JOHN\"");
        var func = expression.Compile();
        Assert.True(func(new Person { Name = "Johnny" }));
        Assert.False(func(new Person { Name = "Jane" }));
    }

    [Fact]
    public void Should_SupportComplexStaticMethodCombinations()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("p => Math.Abs(p.Age ?? 0) > 10");
        var func = expression.Compile();
        Assert.True(func(new Person { Age = 15 }));
        Assert.False(func(new Person { Age = 5 }));
    }

    [Fact]
    public void Should_SupportComplexTypeConversions()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<object>>("it => ((int)((double)((decimal)it.Value))).ToString() == \"42\"");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<object> { Value = 42.0m }));
        Assert.True(func(new ItemWithGenericProperty<object> { Value = 42.5m })); // Will be truncated to 42
        Assert.False(func(new ItemWithGenericProperty<object> { Value = 41.9m }));
    }

    [Fact]
    public void Should_SupportNestedTernaryOperations()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("p => (p.Age > 18 ? \"Adult\" : \"Minor\") == \"Adult\"");
        var func = expression.Compile();
        Assert.True(func(new Person { Age = 20 }));
        Assert.False(func(new Person { Age = 15 }));
    }

    [Fact]
    public void Should_SupportComplexStringFormatting()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("p => string.Format(\"{0}\", p.Name) == p.Name");
        var func = expression.Compile();
        Assert.True(func(new Person { Name = "John" }));
        Assert.True(func(new Person { Name = "Jane" }));
    }

    [Fact]
    public void Should_SupportNestedCollectionOperations()
    {
        var expression = ExpressionParser.ParsePredicate<OrderDetail>("o => o.WorkStatuses.Count > 0");
        var func = expression.Compile();
        Assert.True(func(new OrderDetail { WorkStatuses = new List<WorkStatus> { new() { Name = "Completed" } } }));
        Assert.False(func(new OrderDetail { WorkStatuses = new List<WorkStatus>() }));
    }

    [Fact]
    public void Should_SupportConditionalsOfMixedType()
    {
        var expression = ExpressionParser.ParsePredicate<Person>("x => ((x.Age == 1) && (((x == null) ? null : x.Name) ?? \"\").Contains(\"D\"))");
        var func = expression.Compile();

        Assert.True(func(new Person { Age = 1, Name = "Dali" }));
    }

    [Fact]
    public void Should_SupportConvertMethods()
    {
        var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<object>>("it => Convert.ToInt32(it.Value) == 42");
        var func = expression.Compile();
        Assert.True(func(new ItemWithGenericProperty<object> { Value = "42" }));
        Assert.False(func(new ItemWithGenericProperty<object> { Value = "41" }));
    }

    [Fact]
    public void Should_SelectByString()
    {
        var list = new List<OrderDetail> { new OrderDetail { Product = new Product { ProductName = "Chai" } } }.AsQueryable();

        var result = DynamicExtensions.Select(list, "Product.ProductName as ProductName");

        Assert.Equal("Chai", result.ElementType?.GetProperty("ProductName")?.GetValue(result.FirstOrDefault()));
    }

    [Fact]
    public void Should_SelectByWithUntypedIQueryableString()
    {
        IQueryable list = new List<OrderDetail> { new OrderDetail { Product = new Product { ProductName = "Chai" } } }.AsQueryable();

        var result = DynamicExtensions.Select(list, "Product.ProductName as ProductName");

        Assert.Equal("Chai", result.ElementType.GetProperty("ProductName")?.GetValue(result.FirstOrDefault()));
    }

    [Fact]
    public void Should_RespectBinaryOperatorPrecedence()
    {
        // Test AND, OR, XOR precedence
        var expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value & 3 | 4 ^ 1");
        var func = expression.Compile();
        Assert.Equal(25 & 3 | 4 ^ 1, func(new ItemWithGenericProperty<int> { Value = 25 }));

        // Test shift operators precedence
        expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value << 2 >> 1");
        func = expression.Compile();
        Assert.Equal(25 << 2 >> 1, func(new ItemWithGenericProperty<int> { Value = 25 }));

        // Test arithmetic and shift precedence
        expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value * 2 << 1 + 1");
        func = expression.Compile();
        Assert.Equal(25 * 2 << 1 + 1, func(new ItemWithGenericProperty<int> { Value = 25 }));

        // Test complex combination of bitwise operators
        expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value & 7 | 12 ^ 3 & 5");
        func = expression.Compile();
        Assert.Equal(25 & 7 | 12 ^ 3 & 5, func(new ItemWithGenericProperty<int> { Value = 25 }));

        // Test arithmetic and bitwise operator precedence
        expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value * 2 & 15 + 3");
        func = expression.Compile();
        Assert.Equal(25 * 2 & 15 + 3, func(new ItemWithGenericProperty<int> { Value = 25 }));

        // Test shift and bitwise operator precedence
        expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value << 2 & 15 | 7");
        func = expression.Compile();
        Assert.Equal(25 << 2 & 15 | 7, func(new ItemWithGenericProperty<int> { Value = 25 }));

        // Test parentheses with mixed operators
        expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => (p.Value & 15) << (2 + 1)");
        func = expression.Compile();
        Assert.Equal((25 & 15) << (2 + 1), func(new ItemWithGenericProperty<int> { Value = 25 }));

        // Test complex expression with multiple operators and parentheses
        expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => (p.Value * 2 + 1) << 2 & 15 | 7 ^ 3");
        func = expression.Compile();
        Assert.Equal((25 * 2 + 1) << 2 & 15 | 7 ^ 3, func(new ItemWithGenericProperty<int> { Value = 25 }));

        // Test shift operators with multiplication and addition
        expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => p.Value * 2 >> 1 + 1 << 2");
        func = expression.Compile();
        Assert.Equal(25 * 2 >> 1 + 1 << 2, func(new ItemWithGenericProperty<int> { Value = 25 }));

        // Test nested parentheses with mixed operators
        expression = ExpressionParser.ParseLambda<ItemWithGenericProperty<int>, int>("p => ((p.Value & 7) << 2) | (15 & (3 + 2))");
        func = expression.Compile();
        Assert.Equal(((25 & 7) << 2) | (15 & (3 + 2)), func(new ItemWithGenericProperty<int> { Value = 25 }));
    }
}