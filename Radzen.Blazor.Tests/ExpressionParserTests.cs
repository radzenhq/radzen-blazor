using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
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

        public enum Status
        {
            Office,
            Remote,
        }
        class OrderDetail
        {
            public Product Product { get; set; }
            public int Quantity { get; set; }
            public Order Order { get; set; }

            public List<WorkStatus> WorkStatuses { get; set; }
            public List<Status> Statuses { get; set; }
        }

        class Category
        {
            public string CategoryName { get; set; }

        }

        class Order
        {
            public DateTime OrderDate { get; set; }
        }

        class Product
        {
            public string ProductName { get; set; }

            public Category Category { get; set; }
        }

        [Fact]
        public void Should_ParseBindaryExpression()
        {
            var expression = ExpressionParser.ParsePredicate<Person>("p => p.Name == \"foo\"");

            var func = expression.Compile();

            Assert.True(func(new Person() { Name = "foo" }));
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

        class ItemWithGenericProperty<T>
        {
            public T Value { get; set; }
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
        public void Should_SupportEnumWithCasts()
        {
            var typeLocator = (string type) => AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.FullName == type);

            var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<CarType[]>>("it => it.Value.Any(i => (new []{0}).Contains(i))", typeLocator);

            var func = expression.Compile();

            Assert.True(func(new ItemWithGenericProperty<CarType[]> { Value = [CarType.Sedan] }));
        }

        [Fact]
        public void Should_SupportNullableCollection()
        {
            var expression = ExpressionParser.ParsePredicate<Person>("it => new bool?[]{ false }.Contains(it.Famous)");
            var func = expression.Compile();

            Assert.True(func(new Person { Famous = false }));
        }

        [Fact]
        public void Should_SupportEnumCollections()
        {
            var typeLocator = (string type) => AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.FullName == type);

            var expression = ExpressionParser.ParsePredicate<Car>($"it => it.Type == (Radzen.Blazor.Tests.CarType)1", typeLocator);

            var func = expression.Compile();

            Assert.True(func(new Car { Type = CarType.Coupe }));
        }

        [Fact]
        public void Should_SupportCollectionLiterals()
        {
            var expression = ExpressionParser.ParsePredicate<OrderDetail>("it => (new []{\"Tofu\"}).Contains(it.Product.ProductName)");
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
            var expression = ExpressionParser.ParsePredicate<OrderDetail>("it => it.WorkStatuses.Any(i => (new []{\"Office\"}).Contains(i.Name))");
            var func = expression.Compile();

            Assert.True(func(new OrderDetail { WorkStatuses = [new() { Name = "Office" }] }));
        }

        [Fact]
        public void Should_SupportNestedLambdasWithNot()
        {
            var expression = ExpressionParser.ParsePredicate<OrderDetail>("it => it.WorkStatuses.Any(i => !(new []{\"Office\"}).Contains(i.Name))");
            var func = expression.Compile();

            Assert.False(func(new OrderDetail { WorkStatuses = [new() { Name = "Office" }] }));
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
        public void Should_SupportToLower()
        {
            var expression = ExpressionParser.ParsePredicate<Person>("it => (it.Name == null ? \"\" : it.Name).ToLower().Contains(\"na\".ToLower())");

            var func = expression.Compile();

            Assert.True(func(new Person { Name = "Nana" }));
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
        public void Should_SupportNullCoalescence()
        {
            var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<double?>>("it => (it.Value ?? 0) == 0");

            var func = expression.Compile();

            Assert.True(func(new ItemWithGenericProperty<double?> { Value = null }));
        }

        [Fact]
        public void Should_SupportNullConditionAndCoalescence()
        {
            var expression = ExpressionParser.ParsePredicate<Person>("it => (((it == null) ? null : it.Name) ?? \"\").Contains(\"Da\")");

            var func = expression.Compile();

            Assert.True(func(new Person { Name = "Dali" }));
        }

        [Fact]
        public void Should_CreateProjection()
        {
            var expression = ExpressionParser.ParseLambda<OrderDetail>("it => new { ProductName = it.Product.ProductName}");

            var func = expression.Compile();

            var result = func.DynamicInvoke(new OrderDetail { Product = new Product { ProductName = "Queso" } });

            var property = result.GetType().GetProperty("ProductName");

            Assert.Equal(typeof(string), property.PropertyType);

            Assert.Equal("Queso", property.GetValue(result));
        }

        [Fact]
        public void Should_CreateProjectionFromNestedAccess()
        {
            var expression = ExpressionParser.ParseLambda<OrderDetail>("it => new { it.Product.Category.CategoryName }");

            var func = expression.Compile();

            var orderDetail = new OrderDetail { Product = new Product { Category = new Category { CategoryName = "Beverages" } } };

            var x = new {  orderDetail?.Product?.Category?.CategoryName };

            var result = func.DynamicInvoke(orderDetail);

            var property = result.GetType().GetProperty("CategoryName");

            Assert.Equal(typeof(string), property.PropertyType);

            Assert.Equal("Beverages", property.GetValue(result));
        }

        [Fact]
        public void Should_CreateProjectionFromNestedConditionalAccess()
        {
            var expression = ExpressionParser.ParseLambda<OrderDetail>("it => new { it.Product?.Category?.CategoryName }");

            var func = expression.Compile();

            var orderDetail = new OrderDetail { Product = new Product { Category = new Category { CategoryName = "Beverages" } } };

            var x = new { orderDetail?.Product?.Category?.CategoryName };

            var result = func.DynamicInvoke(orderDetail);

            var property = result.GetType().GetProperty("CategoryName");

            Assert.Equal(typeof(string), property.PropertyType);

            Assert.Equal("Beverages", property.GetValue(result));
        }

        [Fact]
        public void Should_CreateProjectionFromConditionalAccess()
        {
            var expression = ExpressionParser.ParseLambda<OrderDetail>("it => new { ProductName = it.Product?.ProductName}");
            var func = expression.Compile();
            var result = func.DynamicInvoke(new OrderDetail { Product = null });

            var property = result.GetType().GetProperty("ProductName");

            Assert.Equal(typeof(string), property.PropertyType);
            Assert.Null(property.GetValue(result));
        }

        [Fact]
        public void Should_CreateProjectionFromNestedConditionalAccessAndAssignment()
        {
            var expression = ExpressionParser.ParseLambda<OrderDetail>("it => new { CategoryName = it.Product?.Category?.CategoryName}");
            var func = expression.Compile();
            var result = func.DynamicInvoke(new OrderDetail { Product = null });

            var property = result.GetType().GetProperty("CategoryName");

            Assert.Equal(typeof(string), property.PropertyType);
            Assert.Null(property.GetValue(result));
        }

        [Fact]
        public void Should_SelectByString()
        {
            var list = new List<OrderDetail> { new OrderDetail { Product = new Product { ProductName = "Chai" } } }.AsQueryable();

            var result = DynamicExtensions.Select(list, "Product.ProductName as ProductName");

            Assert.Equal("Chai", result.ElementType.GetProperty("ProductName").GetValue(result.FirstOrDefault()));
        }

        [Fact]
        public void Should_SelectByWithUntypedIQueryableString()
        {
            IQueryable list = new List<OrderDetail> { new OrderDetail { Product = new Product { ProductName = "Chai" } } }.AsQueryable();

            var result = DynamicExtensions.Select(list, "Product.ProductName as ProductName");

            Assert.Equal("Chai", result.ElementType.GetProperty("ProductName").GetValue(result.FirstOrDefault()));
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
            var expression = ExpressionParser.ParsePredicate<Dictionary<string, object>>("it => (Int32?)it[\"foo\"] == null");
            var func = expression.Compile();
            Assert.True(func(new Dictionary<string, object> { ["foo"] = null }));
        }

        [Fact]
        public void Should_SupportArrayIndexAccess()
        {
            var expression = ExpressionParser.ParsePredicate<ItemWithGenericProperty<int[]>>("it => it.Value[0] == 1");
            var func = expression.Compile();

            Assert.True(func(new ItemWithGenericProperty<int[]> { Value = [1] }));
        }
    }
}
