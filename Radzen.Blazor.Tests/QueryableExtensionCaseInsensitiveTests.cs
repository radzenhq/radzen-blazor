using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Radzen;
using Xunit;

namespace Radzen.Blazor.Tests
{
    // In-memory case-insensitive string filtering compares with OrdinalIgnoreCase (no per-row ToLower
    // allocation); a non-in-memory (e.g. EF) source keeps the ToLower path so it can translate to SQL.
    public class QueryableExtensionCaseInsensitiveTests
    {
        private class Item
        {
            public string Name { get; set; }
        }

        private static IQueryable<Item> Data() => new List<Item>
        {
            new() { Name = "Alice" },
            new() { Name = "BOB" },
            new() { Name = "charlie" },
            new() { Name = null },
        }.AsQueryable();

        private static List<FilterDescriptor> Filter(FilterOperator op, string value) =>
            [ new FilterDescriptor { Property = "Name", FilterProperty = "Name", Type = typeof(string), FilterValue = value, FilterOperator = op } ];

        [Theory]
        [InlineData(FilterOperator.Contains, "LIC", new[] { "Alice" })]
        [InlineData(FilterOperator.Contains, "b", new[] { "BOB" })]
        [InlineData(FilterOperator.Equals, "alice", new[] { "Alice" })]
        [InlineData(FilterOperator.StartsWith, "CHAR", new[] { "charlie" })]
        [InlineData(FilterOperator.EndsWith, "OB", new[] { "BOB" })]
        public void InMemory_CaseInsensitive_Matches_RegardlessOfCase(FilterOperator op, string value, string[] expected)
        {
            var result = Data().Where(Filter(op, value), LogicalFilterOperator.And, FilterCaseSensitivity.CaseInsensitive)
                .Select(i => i.Name).ToList();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void InMemory_CaseInsensitive_DoesNotContain_ExcludesMatchRegardlessOfCase()
        {
            var result = Data().Where(Filter(FilterOperator.DoesNotContain, "LIC"), LogicalFilterOperator.And, FilterCaseSensitivity.CaseInsensitive)
                .Select(i => i.Name).ToList();

            Assert.DoesNotContain("Alice", result);
            Assert.Contains("BOB", result);
        }

        [Fact]
        public void InMemory_CaseInsensitive_NonStringFilterValue_IsStringified_NotThrown()
        {
            // A non-string FilterValue on a string column must be coerced to string; passing it straight
            // into Expression.Constant(value, typeof(string)) on the ordinal path would throw ArgumentException.
            var data = new List<Item> { new() { Name = "Order 42" }, new() { Name = "Order 7" } }.AsQueryable();
            var filter = new List<FilterDescriptor>
            {
                new() { Property = "Name", FilterProperty = "Name", Type = typeof(string), FilterValue = 42, FilterOperator = FilterOperator.Contains }
            };

            var result = data.Where(filter, LogicalFilterOperator.And, FilterCaseSensitivity.CaseInsensitive)
                .Select(i => i.Name).ToList();

            Assert.Equal(new[] { "Order 42" }, result);
        }

        [Fact]
        public void InMemory_CaseInsensitive_UsesOrdinalIgnoreCase_NotToLower()
        {
            var parameter = Expression.Parameter(typeof(Item), "x");
            var filter = Filter(FilterOperator.Contains, "lic")[0];

            var inMemory = QueryableExtension.GetExpression<Item>(parameter, filter, FilterCaseSensitivity.CaseInsensitive, typeof(string), useOrdinalIgnoreCaseStrings: true)!.ToString();
            Assert.Contains("OrdinalIgnoreCase", inMemory);
            Assert.DoesNotContain("ToLower", inMemory);

            // The default (EF-safe) path still lowers, so it can translate to SQL LOWER().
            var efSafe = QueryableExtension.GetExpression<Item>(parameter, filter, FilterCaseSensitivity.CaseInsensitive, typeof(string))!.ToString();
            Assert.Contains("ToLower", efSafe);
            Assert.DoesNotContain("OrdinalIgnoreCase", efSafe);
        }
    }
}
