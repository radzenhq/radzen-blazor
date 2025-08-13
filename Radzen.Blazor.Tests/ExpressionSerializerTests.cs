using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Radzen.Blazor.Tests
{
    class TestEntity
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public double Salary { get; set; }
        public float Score { get; set; }
        public decimal Balance { get; set; }
        public short Level { get; set; }
        public long Population { get; set; }
        public Status AccountStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
        public Guid Id { get; set; }
        public TimeOnly StartTime { get; set; }
        public DateOnly BirthDate { get; set; }
        public int[] Scores { get; set; }
        public List<string> Tags { get; set; }
        public List<TestEntity> Children { get; set; }
        public Address Address { get; set; }
        public double[] Salaries { get; set; }
        public float[] Heights { get; set; }
        public decimal[] Balances { get; set; }
        public short[] Levels { get; set; }
        public long[] Populations { get; set; }
        public string[] Names { get; set; }
        public Guid[] Ids { get; set; }
        public DateTime[] CreatedDates { get; set; }
        public DateTimeOffset[] UpdatedDates { get; set; }
        public TimeOnly[] StartTimes { get; set; }
        public DateOnly[] BirthDates { get; set; }
        public Status[] Statuses { get; set; }
    }

    enum Status
    {
        Active,
        Inactive,
        Suspended
    }

    class Address
    {
        public string City { get; set; }
        public string Country { get; set; }
    }

    public class ExpressionSerializerTests
    {
        private readonly ExpressionSerializer _serializer = new ExpressionSerializer();

        [Fact]
        public void Serializes_SimpleBinaryExpression()
        {
            Expression<Func<int, bool>> expr = e => e > 10;
            Assert.Equal("e => (e > 10)", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_StringEquality()
        {
            Expression<Func<TestEntity, bool>> expr = e => e.Name == "John";
            Assert.Equal("e => (e.Name == \"John\")", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_IntComparison()
        {
            Expression<Func<TestEntity, bool>> expr = e => e.Age > 18;
            Assert.Equal("e => (e.Age > 18)", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_DoubleComparison()
        {
            Expression<Func<TestEntity, bool>> expr = e => e.Salary < 50000.50;
            Assert.Equal("e => (e.Salary < 50000.5)", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_FloatComparison()
        {
            Expression<Func<TestEntity, bool>> expr = e => e.Score >= 85.3f;
            Assert.Equal("e => (e.Score >= 85.3)", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_DecimalComparison()
        {
            Expression<Func<TestEntity, bool>> expr = e => e.Balance <= 1000.75m;
            Assert.Equal("e => (e.Balance <= 1000.75)", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_ShortComparison()
        {
            Expression<Func<TestEntity, bool>> expr = e => e.Level == 3;
            Assert.Equal("e => (e.Level == 3)", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_LongComparison()
        {
            Expression<Func<TestEntity, bool>> expr = e => e.Population > 1000000L;
            Assert.Equal("e => (e.Population > 1000000)", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_EnumComparison()
        {
            Expression<Func<TestEntity, bool>> expr = e => e.AccountStatus == Status.Inactive;
            Assert.Equal("e => (e.AccountStatus == 1)", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_ArrayContainsValue()
        {
            Expression<Func<TestEntity, bool>> expr = e => e.Scores.Contains(100);
            Assert.Equal("e => e.Scores.Contains(100)", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_ArrayNotContainsValue()
        {
            Expression<Func<TestEntity, bool>> expr = e => !e.Scores.Contains(100);
            Assert.Equal("e => (!(e.Scores.Contains(100)))", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_ArrayInValue()
        {
            Expression<Func<TestEntity, bool>> expr = e => e.Scores.Intersect(new [] { 100 }).Any();
            Assert.Equal("e => e.Scores.Intersect(new [] { 100 }).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_ArrayNotInValue()
        {
            Expression<Func<TestEntity, bool>> expr = e => e.Scores.Except(new[] { 100 }).Any();
            Assert.Equal("e => e.Scores.Except(new [] { 100 }).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_ArrayInValueOposite()
        {
            Expression<Func<TestEntity, bool>> expr = e => new[] { 100 }.Intersect(e.Scores).Any();
            Assert.Equal("e => new [] { 100 }.Intersect(e.Scores).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_ArrayNotInValueOposite()
        {
            Expression<Func<TestEntity, bool>> expr = e => new[] { 100 }.Except(e.Scores).Any();
            Assert.Equal("e => new [] { 100 }.Except(e.Scores).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_IntArrayInValueOposite()
        {
            Expression<Func<TestEntity, bool>> expr = e => new[] { 100 }.Intersect(e.Scores).Any();
            Assert.Equal("e => new [] { 100 }.Intersect(e.Scores).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_IntArrayNotInValueOposite()
        {
            Expression<Func<TestEntity, bool>> expr = e => !new[] { 100 }.Intersect(e.Scores).Any();
            Assert.Equal("e => (!(new [] { 100 }.Intersect(e.Scores).Any()))", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_DoubleArrayInValueOposite()
        {
            Expression<Func<TestEntity, bool>> expr = e => new[] { 99.99 }.Intersect(e.Salaries).Any();
            Assert.Equal("e => new [] { 99.99 }.Intersect(e.Salaries).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_FloatArrayInValueOposite()
        {
            Expression<Func<TestEntity, bool>> expr = e => new[] { 5.5f }.Intersect(e.Heights).Any();
            Assert.Equal("e => new [] { 5.5 }.Intersect(e.Heights).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_DecimalArrayInValueOposite()
        {
            Expression<Func<TestEntity, bool>> expr = e => new[] { 1000.75m }.Intersect(e.Balances).Any();
            Assert.Equal("e => new [] { 1000.75 }.Intersect(e.Balances).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_ShortArrayInValueOposite()
        {
            Expression<Func<TestEntity, bool>> expr = e => new [] { (short)3 }.Intersect(e.Levels).Any();
            Assert.Equal("e => new [] { 3 }.Intersect(e.Levels).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_LongArrayInValueOposite()
        {
            Expression<Func<TestEntity, bool>> expr = e => new [] { 1000000L }.Intersect(e.Populations).Any();
            Assert.Equal("e => new [] { 1000000 }.Intersect(e.Populations).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_StringArrayInValueOposite()
        {
            Expression<Func<TestEntity, bool>> expr = e => new[] { "Alice", "Bob" }.Intersect(e.Names).Any();
            Assert.Equal("e => (new [] { \"Alice\", \"Bob\" }).Intersect(e.Names).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_GuidArrayInValueOposite()
        {
            Expression<Func<TestEntity, bool>> expr = e => new[] { Guid.Parse("12345678-1234-1234-1234-123456789abc") }.Intersect(e.Ids).Any();
            Assert.Equal("e => (new [] { Guid.Parse(\"12345678-1234-1234-1234-123456789abc\") }).Intersect(e.Ids).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_DateTimeArrayInValueOposite()
        {
            Expression<Func<TestEntity, bool>> expr = e => new[] { DateTime.Parse("2023-01-01T00:00:00.000Z") }.Intersect(e.CreatedDates).Any();
            Assert.Equal("e => (new [] { DateTime.Parse(\"2023-01-01T00:00:00.000Z\") }).Intersect(e.CreatedDates).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_DateTimeOffsetArrayInValueOposite()
        {
            Expression<Func<TestEntity, bool>> expr = e => new[] { DateTimeOffset.Parse("2023-01-01T10:30:00.000+00:00") }.Intersect(e.UpdatedDates).Any();
            Assert.Equal("e => (new [] { DateTimeOffset.Parse(\"2023-01-01T10:30:00.000+00:00\") }).Intersect(e.UpdatedDates).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_DateTimeWithRoundtripKind()
        {
            Expression<Func<TestEntity, bool>> expr = e =>
                DateTime.Parse("2023-01-01T00:00:00.000Z", null, DateTimeStyles.RoundtripKind) > e.CreatedAt;

            Assert.Equal(
                "e => (DateTime.Parse(\"2023-01-01T00:00:00.000Z\", null, (System.Globalization.DateTimeStyles)128) > e.CreatedAt)",
                _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_TimeOnlyArrayInValueOposite()
        {
            Expression<Func<TestEntity, bool>> expr = e => new[] { TimeOnly.Parse("12:00:00") }.Intersect(e.StartTimes).Any();
            Assert.Equal("e => (new [] { TimeOnly.Parse(\"12:00:00\") }).Intersect(e.StartTimes).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_DateOnlyArrayInValueOposite()
        {
            Expression<Func<TestEntity, bool>> expr = e => new[] { DateOnly.Parse("2000-01-01") }.Intersect(e.BirthDates).Any();
            Assert.Equal("e => (new [] { DateOnly.Parse(\"2000-01-01\") }).Intersect(e.BirthDates).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_EnumArrayInValueOposite()
        {
            Expression<Func<TestEntity, bool>> expr = e => new[] { Status.Active, Status.Inactive }.Intersect(e.Statuses).Any();
            Assert.Equal("e => (new [] { (Radzen.Blazor.Tests.Status)0, (Radzen.Blazor.Tests.Status)1 }).Intersect(e.Statuses).Any()", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_ListContainsValue()
        {
            Expression<Func<TestEntity, bool>> expr = e => e.Tags.Contains("VIP");
            Assert.Equal("e => e.Tags.Contains(\"VIP\")", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_ListNotContainsValue()
        {
            Expression<Func<TestEntity, bool>> expr = e => !e.Tags.Contains("VIP");
            Assert.Equal("e => (!(e.Tags.Contains(\"VIP\")))", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_ListAnyCheck()
        {
            Expression<Func<TestEntity, bool>> expr = e => e.Children.Any(c => c.Age > 18);
            Assert.Equal("e => e.Children.Any(c => (c.Age > 18))", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_ListNotAnyCheck()
        {
            Expression<Func<TestEntity, bool>> expr = e => !e.Children.Any(c => c.Age > 18);
            Assert.Equal("e => (!(e.Children.Any(c => (c.Age > 18))))", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_EntitySubPropertyCheck()
        {
            Expression<Func<TestEntity, bool>> expr = e => e.Address.City == "New York";
            Assert.Equal("e => (e.Address.City == \"New York\")", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_ComplexExpressionWithProperties()
        {
            Expression<Func<TestEntity, bool>> expr = e => e.Age > 18 && e.Tags.Contains("Member") || e.Address.City == "London";
            Assert.Equal("e => (((e.Age > 18) && e.Tags.Contains(\"Member\")) || (e.Address.City == \"London\"))", _serializer.Serialize(expr));
        }

        [Fact]
        public void Serializes_NotContains()
        {
            Expression<Func<TestEntity, bool>> expr = e => !e.Tags.Contains("Member");
            Assert.Equal("e => (!(e.Tags.Contains(\"Member\")))", _serializer.Serialize(expr));
        }
    }
}