using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
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

    class Node
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
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

        public bool IsDiscontinued { get; set; }
        public int UnitsInStock { get; set; }
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
    public void Should_SupportEqualityBetweenNullableAndNonNullable()
    {
        var expression = ExpressionParser.ParsePredicate<Node>("it => it.Id == it.ParentId");
        var func = expression.Compile();

        Assert.True(func(new Node { Id = 1, ParentId = 1 }));
        Assert.False(func(new Node { Id = 1, ParentId = null }));
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

    class EmployeeWithHireDate
    {
        public DateTime? HireDate { get; set; }
        public DateOnly? HireDateOnly { get; set; }
    }

    [Fact]
    public void Should_SupportNullableDateTimeArrayWithSpecifyKindAndNullableProperty()
    {
        var predicate = "x => new System.DateTime?[] { DateTime.SpecifyKind(DateTime.Parse(\"2012-04-01\", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind), DateTimeKind.Unspecified) }.Contains((x.HireDate ?? null))";
        var expression = ExpressionParser.ParsePredicate<EmployeeWithHireDate>(predicate);
        var func = expression.Compile();

        var hireDate = DateTime.SpecifyKind(DateTime.Parse("2012-04-01", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind), DateTimeKind.Unspecified);
        Assert.True(func(new EmployeeWithHireDate { HireDate = hireDate }));
        Assert.False(func(new EmployeeWithHireDate { HireDate = DateTime.Parse("2013-01-01") }));
    }

    [Fact]
    public void Should_SupportNullableDateOnlyArrayAndNullableProperty()
    {
        var predicate = "x => new System.DateOnly?[] { DateOnly.Parse(\"2012-04-01\") }.Contains((x.HireDateOnly ?? null))";
        var expression = ExpressionParser.ParsePredicate<EmployeeWithHireDate>(predicate);
        var func = expression.Compile();

        var hireDate = DateOnly.Parse("2012-04-01");
        Assert.True(func(new EmployeeWithHireDate { HireDateOnly = hireDate }));
        Assert.False(func(new EmployeeWithHireDate { HireDateOnly = DateOnly.Parse("2013-01-01") }));
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

    [Fact]
    public void Should_ParseDefaultBool()
    {
        var expression = ExpressionParser.ParsePredicate<OrderDetail>(
            "x => ((x.Product == null ? default(bool) : x.Product.IsDiscontinued) == true)");

        var func = expression.Compile();

        Assert.True(func(new OrderDetail { Product = new Product { IsDiscontinued = true } }));
        Assert.False(func(new OrderDetail { Product = new Product { IsDiscontinued = false } }));
        Assert.False(func(new OrderDetail { Product = null }));
    }

    [Fact]
    public void Should_ParseDefaultInt()
    {
        var expression = ExpressionParser.ParseLambda<OrderDetail, int>(
            "x => (x.Product == null ? default(int) : x.Product.UnitsInStock)");

        var func = expression.Compile();

        Assert.Equal(42, func(new OrderDetail { Product = new Product { UnitsInStock = 42 } }));
        Assert.Equal(0, func(new OrderDetail { Product = null }));
    }

    [Fact]
    public void Should_ParseDefaultNullableBool()
    {
        var expression = ExpressionParser.ParsePredicate<Person>(
            "x => (default(bool?) == null)");

        var func = expression.Compile();

        Assert.True(func(new Person()));
    }

    public enum TicketStatus { Open, Closed }

    public record Pair(string Left, string Right);

    public class Ticket
    {
        public string? City { get; set; }
        public TicketStatus Status { get; set; }
        public List<TicketStatus> Statuses { get; set; } = new();
        public HashSet<string> Tags { get; set; } = new();
        public Pair? Pair { get; set; }
        public Type? Kind { get; set; }
        public Func<string>? Factory { get; set; }
        public object? Payload { get; set; }
        internal string? Hidden { get; set; }
        private readonly string secret = "secret";
        public string Reveal() => secret;
        public bool IsOpen() => Status == TicketStatus.Open;
        public bool IsClosed() => Status == TicketStatus.Closed;
    }

    public static class Helpers
    {
        public const string Name = "radzen";
        public static readonly string Shared = "shared";
        public static string Normalize(string value) => value;
    }

    internal static class InternalHelpers
    {
        public const string Name = "internal";
    }

    public static class AllowedHelpers
    {
        public static readonly string Shared = "shared";
        public static string Normalize(string value) => value.Trim();
    }

    public class Plain
    {
        public string? Name { get; set; }
    }

    public class Convertible
    {
        public static explicit operator Convertible(string value) => new();
        public static implicit operator string(Convertible value) => string.Empty;
        public static Convertible operator +(Convertible left, Convertible right) => left;
    }

    private static string NameOf(Type type) => type.FullName!.Replace("+", ".", StringComparison.Ordinal);

    private static readonly string PlainName = NameOf(typeof(Plain));
    private static readonly string HelpersName = NameOf(typeof(Helpers));
    private static readonly string AllowedHelpersName = NameOf(typeof(AllowedHelpers));
    private static readonly string ConvertibleName = NameOf(typeof(Convertible));
    private static readonly string TicketStatusName = NameOf(typeof(TicketStatus));

    private static IQueryable<Ticket> Data() => new List<Ticket>
    {
        new() { City = "Seattle", Status = TicketStatus.Open, Statuses = { TicketStatus.Open }, Tags = { "a" } },
        new() { City = "Redmond", Status = TicketStatus.Closed, Statuses = { TicketStatus.Closed } },
    }.AsQueryable();

    private static LambdaExpression Parse(string expression) => ExpressionParser.ParseLambda<Ticket>(expression);

    private static Func<Ticket, bool> Compile(string expression) => ExpressionParser.ParsePredicate<Ticket>(expression).Compile();

    private static IQueryable Project(string selector) => DynamicExtensions.Select(Data(), selector);

    private static string Repeat(string value, int count) => new StringBuilder().Insert(0, value, count).ToString();

    private static void AssertRejected(Func<LambdaExpression> parse, string expectedMessage)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => parse());
        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public void ApplicationTypes_CanBeNamedInCastsDefaultsAndArrays()
    {
        Assert.True(Compile($"x => (({PlainName})x.Payload).Name == \"a\"")(new Ticket { Payload = new Plain { Name = "a" } }));
        Assert.True(Compile($"x => default({PlainName}) == null")(new Ticket()));
        Assert.True(Compile($"x => new {PlainName}[] {{ }}.Length == 0")(new Ticket()));
    }

    [Fact]
    public void ApplicationConstant_IsReadable_OnlyOnPublicTypes()
    {
        Assert.True(Compile($"x => x.City == {HelpersName}.Name")(new Ticket { City = "radzen" }));
        Assert.Throws<InvalidOperationException>(() => Parse($"x => x.City == {NameOf(typeof(InternalHelpers))}.Name"));
    }

    [Theory]
    [InlineData("Normalize(x.City) == null")]
    [InlineData("Shared == x.City")]
    public void ApplicationStaticMembers_AreNotAllowedByDefault(string member)
    {
        AssertRejected(() => Parse($"x => {HelpersName}.{member}"), "is not allowed");
    }

    [Fact]
    public void AllowedType_StaticMembersWork()
    {
        ExpressionParser.AllowType(typeof(AllowedHelpers));

        Assert.True(Compile($"x => {AllowedHelpersName}.Normalize(x.City) == {AllowedHelpersName}.Shared")(new Ticket { City = " shared " }));
        Assert.Equal("Seattle", Data().Where($"x => {AllowedHelpersName}.Normalize(x.City) == \"Seattle\"").Single().City);
    }

    [Fact]
    public void ModelMethods_NeedToBeAllowed()
    {
        AssertRejected(() => Parse("x => x.IsOpen()"), "ExpressionParser.AllowMethod");

        ExpressionParser.AllowMethod(typeof(Ticket).GetMethod(nameof(Ticket.IsClosed))!);

        Assert.True(Compile("x => x.IsClosed()")(new Ticket { Status = TicketStatus.Closed }));
        AssertRejected(() => Parse("x => x.IsOpen()"), "is not allowed");
    }

    [Theory]
    [InlineData("x => object.ReferenceEquals(x, x)")]
    [InlineData("x => string.Intern(x.City) == \"a\"")]
    [InlineData("x => CultureInfo.GetCultureInfo(\"en-US\").Name == \"en-US\"")]
    [InlineData("x => System.Linq.Enumerable.Range(0, 5).Count() == 5")]
    [InlineData("x => x.Tags.Add(\"b\")")]
    [InlineData("x => ((System.Text.StringBuilder)x.Payload).Append(\"a\") != null")]
    public void MembersOutsideTheAllowList_AreRejected(string expression)
    {
        AssertRejected(() => Parse(expression), "is not allowed");
    }

    [Fact]
    public void CollectionReadsAndFrameworkProperties_Work()
    {
        Assert.True(Compile("x => x.Tags.Contains(\"a\")")(new Ticket { Tags = { "a" } }));
        Assert.True(Compile("x => ((System.Text.StringBuilder)x.Payload).Length == 2")(new Ticket { Payload = new StringBuilder("ab") }));
        Assert.True(Compile("x => CultureInfo.InvariantCulture.Name == \"\"")(new Ticket()));
    }

    [Theory]
    [InlineData("x => x.City.GetType() == null")]
    [InlineData("x => x.GetType() == null")]
    [InlineData("x => ((object)x).GetType() == null")]
    [InlineData("x => x.City?.GetType() == null")]
    [InlineData("x => x.Kind.Name == null")]
    [InlineData("x => x.Kind == null")]
    [InlineData("x => x.Factory == null")]
    [InlineData("x => ((System.Type)x.Payload) == null")]
    [InlineData("x => default(System.Type) == null")]
    public void ReflectionAndDelegates_AreNotAllowed(string expression)
    {
        AssertRejected(() => Parse(expression), "is not allowed");
    }

    [Theory]
    [InlineData("x => (({0})x.City) == null")]
    [InlineData("x => ({0})null == x.City")]
    [InlineData("x => x.City == ({0})x.Payload")]
    [InlineData("x => new [] {{ ({0})null, x.City }}.Length == 2")]
    [InlineData("x => (({0})x.Payload + ({0})x.Payload) == null")]
    public void ApplicationOperators_AreNotAllowedByDefault(string format)
    {
        AssertRejected(() => Parse(string.Format(CultureInfo.InvariantCulture, format, ConvertibleName)), "is not allowed");
    }

    [Fact]
    public void EqualityOnApplicationTypesAndFrameworkOperators_Work()
    {
        Assert.True(Compile("x => x.Pair == null")(new Ticket()));
        Assert.False(Compile("x => x.Pair == null")(new Ticket { Pair = new Pair("a", "b") }));
        Assert.True(Compile("x => (System.Version)x.Payload >= (System.Version)x.Payload")(new Ticket { Payload = new Version(1, 2) }));
        Assert.True(Compile("x => ((decimal)x.City.Length) == 7")(new Ticket { City = "Seattle" }));
        Assert.True(Compile("x => (System.Numerics.BigInteger)x.Payload == (System.Numerics.BigInteger)x.Payload")(new Ticket { Payload = new System.Numerics.BigInteger(5) }));
        Assert.True(Compile("x => ((System.Uri)x.Payload).Host == \"radzen.com\"")(new Ticket { Payload = new Uri("https://radzen.com/") }));
    }

    [Theory]
    [InlineData("x => x.City.PadLeft(3, '0') == \"007\"")]
    [InlineData("x => x.City.PadRight(3) == \"007\"")]
    public void PaddingMethods_AreNotAllowed(string expression)
    {
        AssertRejected(() => Parse(expression), "is not allowed");
    }

    [Theory]
    [InlineData("x => x.secret == null")]
    [InlineData("x => x.Hidden == null")]
    public void NonPublicMembers_AreNotAccessible(string expression)
    {
        Assert.Throws<ArgumentException>(() => Parse(expression));
    }

    [Fact]
    public void PublicMembers_AreMatchedIgnoringCase()
    {
        Assert.True(Compile("x => x.city == \"Seattle\"")(new Ticket { City = "Seattle" }));
    }

    [Theory]
    [InlineData("x => ((int?)1).Repeat(5).Count() == 5")]
    [InlineData("x => new object[] { 1 }.Repeat(5).Count() == 5")]
    public void EnumerableMethods_RequireSequenceSource(string expression)
    {
        AssertRejected(() => Parse(expression), "No suitable method");
    }

    [Theory]
    [InlineData("a\\b")]
    [InlineData("ends with \\")]
    [InlineData("say \"hi\"")]
    [InlineData("it's")]
    [InlineData("\\\\server\\share")]
    public void SerializedStringValue_RoundTrips(string value)
    {
        var parameter = Expression.Parameter(typeof(Ticket), "e");
        var expression = Expression.Lambda<Func<Ticket, bool>>(
            Expression.Equal(Expression.Property(parameter, nameof(Ticket.City)), Expression.Constant(value, typeof(string))), parameter);

        var rows = new List<Ticket> { new() { City = value }, new() { City = "other" }, new() { City = null } }.AsQueryable();
        var filtered = rows.Where(new ExpressionSerializer().Serialize(expression)).ToList();

        Assert.Single(filtered);
        Assert.Equal(value, filtered[0].City);
    }

    [Fact]
    public void ManyAnonymousTypes_AreRejected()
    {
        var shapes = string.Join(", ", Enumerable.Range(0, 100).Select(i => $"new {{ P{i} = 1 }}"));

        AssertRejected(() => Parse($"x => new [] {{ (object)null, {shapes} }}.Length > 0"), "too complex");
    }

    [Fact]
    public void AnonymousTypeWithManyMembers_IsRejected()
    {
        var members = string.Join(", ", Enumerable.Range(0, 300).Select(i => $"P{i} = 1"));

        AssertRejected(() => Parse($"x => new {{ {members} }}"), "too complex");
    }

    [Fact]
    public void LongDottedName_IsRejected()
    {
        Assert.ThrowsAny<Exception>(() => Parse("x => a" + Repeat(".a", 20000)));
    }

    [Fact]
    public void DeeplyNestedParentheses_AreRejected()
    {
        AssertRejected(() => Parse("x => " + new string('(', 5000) + "1" + new string(')', 5000)), "too ");
    }

    [Fact]
    public void DeeplyNestedUnaryOperators_AreRejected()
    {
        AssertRejected(() => Parse("x => " + new string('!', 5000) + "true"), "too ");
    }

    [Theory]
    [InlineData(".Trim()", 150)]
    [InlineData(" + \"a\"", 150)]
    [InlineData(" ?? x.City", 150)]
    public void LongChain_IsRejected(string link, int count)
    {
        AssertRejected(() => Parse("x => x.City" + Repeat(link, count)), "too complex");
    }

    [Fact]
    public void LongChain_SplitByParentheses_IsRejected()
    {
        AssertRejected(() => Parse("x => ((x.City" + Repeat(".Trim()", 300) + ")" + Repeat(".Trim()", 300) + ") != null"), "too complex");
    }

    [Fact]
    public void LogicalChain_WithManyOperands_IsParsed()
    {
        var predicate = Compile("x => x.City != \"0\"" + string.Concat(Enumerable.Range(1, 250).Select(i => $" && x.City != \"{i}\"")));

        Assert.True(predicate(new Ticket { City = "Seattle" }));
        Assert.False(predicate(new Ticket { City = "125" }));
    }

    [Fact]
    public void LongLogicalChain_IsRejected()
    {
        AssertRejected(() => Parse("x => true" + Repeat(" && true", 600)), "too complex");
    }

    [Fact]
    public void LongNullConditionalChain_IsRejected()
    {
        AssertRejected(() => Parse("x => x.City" + Repeat("?.Trim()", 20) + " == null"), "too complex");
    }

    [Fact]
    public void NullConditionalChain_SplitByParentheses_IsRejected()
    {
        AssertRejected(() => Parse("x => ((x.City" + Repeat("?.Trim()", 10) + ")" + Repeat("?.Trim()", 10) + ") != null"), "too complex");
    }

    [Fact]
    public void ManyNullConditionalChains_AreRejected()
    {
        var chain = "x.City" + Repeat("?.Trim()", 12) + " != null";

        AssertRejected(() => Parse("x => " + chain + Repeat(" && " + chain, 19)), "too complex");
    }

    [Fact]
    public void FilterWithManyConditions_IsParsed()
    {
        var predicate = Compile("x => " + new string('(', 119) + "x.City != \"0\"" + string.Concat(Enumerable.Range(1, 119).Select(i => $" && x.City != \"{i}\")")));

        Assert.True(predicate(new Ticket { City = "Seattle" }));
        Assert.False(predicate(new Ticket { City = "60" }));
    }

    [Fact]
    public void NullConditionalChain_WithinLimit_Runs()
    {
        var predicate = Compile("x => x.City" + Repeat("?.Trim()", 12) + " == \"Seattle\"");

        Assert.True(predicate(new Ticket { City = " Seattle " }));
        Assert.False(predicate(new Ticket { City = null }));
    }

    [Fact]
    public void EnumArray_ReferencedByName_IsResolved()
    {
        var predicate = Compile($"x => new {TicketStatusName}[] {{ 1 }}.Contains(x.Status)");

        Assert.False(predicate(new Ticket { Status = TicketStatus.Open }));
        Assert.True(predicate(new Ticket { Status = TicketStatus.Closed }));
    }

    [Fact]
    public void SerializedEnumArrayFilter_RoundTrips()
    {
        Expression<Func<Ticket, bool>> expression = x => new[] { TicketStatus.Closed }.Intersect(x.Statuses).Any();

        var filtered = Data().Where(new ExpressionSerializer().Serialize(expression)).ToList();

        Assert.Single(filtered);
        Assert.Equal("Redmond", filtered[0].City);
    }

    [Fact]
    public void NestedProjections_Work()
    {
        Assert.Equal(2, Project("x => new { A = new { B = x.City } }").Cast<object>().Count());
        Assert.Equal(2, Project("x => new { A = new { C = x.Status } }").Cast<object>().Count());
        Assert.Equal(2, Project("x => new { A = new { P = x.City }, B = new { Q = x.Status } }").Cast<object>().Count());
    }

    [Fact]
    public void Projection_WithSameShape_ReusesGeneratedType()
    {
        var first = Project("x => new { Town = x.City, State = x.Status }").ElementType;
        var second = Project("x => new { Town = x.City, State = x.Status }").ElementType;

        Assert.Same(first, second);
    }

    [Fact]
    public void FrameworkMembers_StillWork()
    {
        Assert.Equal("Redmond", Data().OrderBy("x => Convert.ToString(x.City)").First().City);
        Assert.Equal(2, Data().Where("x => TimeSpan.FromHours(1).Hours == 1").Count());
        Assert.Equal(2, Data().Where("x => DateTime.Now.Year > 2000").Count());
        Assert.Equal("Seattle", Data().Where("x => x.City.ToUpper().StartsWith(\"SEA\")").Single().City);
        Assert.Equal("Seattle", Data().Where("x => x.City.Contains(\"SEA\", (System.StringComparison)5)").Single().City);
        Assert.Equal("Seattle", Data().Where("x => x.Statuses.Any(s => s == 0) && x.City.Length > 3").Single().City);
    }
}
