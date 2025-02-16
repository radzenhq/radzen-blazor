﻿using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
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
      public string Name { get; set; }
      public bool? Famous { get; set; }
      public DateTime BirthDate { get; set; }
    }

    public class Car
    {
      public CarType Type { get; set; }
    }


    class OrderDetail
    {
      public Product Product { get; set; }
      public int Quantity { get; set; }
      public Order Order { get; set; }

      public List<WorkStatus> WorkStatuses { get; set; }
    }

    class Order
    {
      public DateTime OrderDate { get; set; }
    }

    class Product
    {
      public string ProductName { get; set; }
    }

    [Fact]
    public void Should_ParseBindaryExpression()
    {
      var expression = ExpressionParser.Parse<Person>("p => p.Name == \"foo\"");

      var func = expression.Compile();

      Assert.True(func(new Person() { Name = "foo" }));
    }

    [Fact]
    public void Should_ParseConditionalExpression()
    {
      var expression = ExpressionParser.Parse<OrderDetail>("it => (it.Product.ProductName == null ? \"\" : it.Product.ProductName).Contains(\"Queso\") && it.Quantity == 50");

      var func = expression.Compile();

      Assert.True(func(new OrderDetail() { Product = new Product { ProductName = "Queso" }, Quantity = 50 }));
    }

    [Fact]
    public void Should_ParseNestedLogicalOperations()
    {
      var expression = ExpressionParser.Parse<OrderDetail>("it => (it.Product.ProductName == null ? \"\" : it.Product.ProductName).Contains(\"Queso\") && (it.Quantity == 50 || it.Quantity == 12)");

      var func = expression.Compile();

      Assert.True(func(new OrderDetail() { Product = new Product { ProductName = "Queso" }, Quantity = 12 }));

    }

    [Fact]
    public void Should_SupportDateTimeParsing()
    {
      var expression = ExpressionParser.Parse<OrderDetail>("it => it.Order.OrderDate >= DateTime.Parse(\"2025-02-11\")");

      var func = expression.Compile();
      Assert.True(func(new OrderDetail { Order = new Order { OrderDate = new DateTime(2025, 2, 11) } }));
    }

    class ItemWithGenericProperty<T>
    {
      public T Value { get; set; }
    }

    [Fact]
    public void Should_SupportDateOnlyParsing()
    {
      var expression = ExpressionParser.Parse<ItemWithGenericProperty<DateOnly>>("it => it.Value >= DateOnly.Parse(\"2025-02-11\")");

      var func = expression.Compile();

      Assert.True(func(new ItemWithGenericProperty<DateOnly> { Value = new DateOnly(2025, 2, 11) }));
    }

    [Fact]
    public void Should_SupportTimeOnlyParsing()
    {
      var expression = ExpressionParser.Parse<ItemWithGenericProperty<TimeOnly>>("it => it.Value >= TimeOnly.Parse(\"12:00:00\")");
      var func = expression.Compile();
      Assert.True(func(new ItemWithGenericProperty<TimeOnly> { Value = new TimeOnly(12, 0, 0) }));
    }

    [Fact]
    public void Should_SupportGuidParsing()
    {
      var expression = ExpressionParser.Parse<ItemWithGenericProperty<Guid>>("it => it.Value == Guid.Parse(\"f0e7e7d8-4f4d-4b5f-8b3e-3f1d1b4f5f5f\")");
      var func = expression.Compile();
      Assert.True(func(new ItemWithGenericProperty<Guid> { Value = Guid.Parse("f0e7e7d8-4f4d-4b5f-8b3e-3f1d1b4f5f5f") }));
    }

    [Fact]
    public void Should_SupportEnumWithCasts()
    {
      var typeLocator = (string type) => AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.FullName == type);

      var expression = ExpressionParser.Parse<ItemWithGenericProperty<CarType[]>>("it => it.Value.Any(i => (new []{0}).Contains(i))", typeLocator);

      var func = expression.Compile();

      Assert.True(func(new ItemWithGenericProperty<CarType[]> { Value = [CarType.Sedan] }));
    }

    [Fact]
    public void Should_SupportEnumCollections()
    {
      var typeLocator = (string type) => AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.FullName == type);

      var expression = ExpressionParser.Parse<Car>($"it => it.Type == (Radzen.Blazor.Tests.CarType)1", typeLocator);

      var func = expression.Compile();

      Assert.True(func(new Car { Type = CarType.Coupe }));
    }

    [Fact]
    public void Should_SupportCollectionLiterals()
    {
      var expression = ExpressionParser.Parse<OrderDetail>("it => (new []{\"Tofu\"}).Contains(it.Product.ProductName)");
      var func = expression.Compile();

      Assert.True(func(new OrderDetail { Product = new Product { ProductName = "Tofu" } }));
    }

    class WorkStatus
    {
      public string Name { get; set; }
    }


    [Fact]
    public void Should_SupportNestedLambdas()
    {
      var expression = ExpressionParser.Parse<OrderDetail>("it => it.WorkStatuses.Any(i => (new []{\"Office\"}).Contains(i.Name))");
      var func = expression.Compile();

      Assert.True(func(new OrderDetail { WorkStatuses = [new() { Name = "Office" }] }));
    }

    [Fact]
    public void Should_SupportToLower()
    {
      var expression = ExpressionParser.Parse<Person>("it => (it.Name == null ? \"\" : it.Name).ToLower().Contains(\"na\".ToLower())");

      var func = expression.Compile();

      Assert.True(func(new Person { Name = "Nana" }));
    }

    [Fact]
    public void Should_SupportNullableProperties()
    {
        var expression = ExpressionParser.Parse<Person>("it => it.Age == 50");

        var func = expression.Compile();

        Assert.True(func(new Person { Age = 50 }));
    }

    [Fact]
    public void Should_SupportNullablePropertiesWithArray()
    {
        var expression = ExpressionParser.Parse<Person>("it => (new []{}).Contains(it.Famous)");

        var func = expression.Compile();

        Assert.False(func(new Person { Famous = null }));
    }

    [Fact]
    public void Should_SupportDateTimeWithArray()
    {
        var expression = ExpressionParser.Parse<Person>("it => (new []{DateTime.Parse(\"5/5/2000 12:00:00 AM\")}).Contains(it.BirthDate)");

        var func = expression.Compile();

        Assert.True(func(new Person { BirthDate = DateTime.Parse("5/5/2000 12:00:00 AM") }));
    }

    [Fact]
    public void Should_SupportNumericConversion()
    {
      var expression = ExpressionParser.Parse<ItemWithGenericProperty<double>>("it => it.Value == 50");

      var func = expression.Compile();

      Assert.True(func(new ItemWithGenericProperty<double> { Value = 50.0 }));
    }
  }
}
