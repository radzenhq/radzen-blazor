using System.Collections.Generic;
using System.Linq;
using Radzen;
using Xunit;

namespace Radzen.Blazor.Tests
{
    // The dropdown/autocomplete search's string-property Where overload compares in-memory
    // case-insensitively with OrdinalIgnoreCase (no per-item ToLower).
    public class DropDownFilterCaseInsensitiveTests
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
        }.AsQueryable();

        private static List<string> Search(string value, StringFilterOperator op) =>
            Data().Where("Name", value, op, FilterCaseSensitivity.CaseInsensitive).OfType<Item>().Select(i => i.Name).ToList();

        [Theory]
        [InlineData("LIC", StringFilterOperator.Contains, new[] { "Alice" })]
        [InlineData("b", StringFilterOperator.Contains, new[] { "BOB" })]
        [InlineData("char", StringFilterOperator.StartsWith, new[] { "charlie" })]
        [InlineData("OB", StringFilterOperator.EndsWith, new[] { "BOB" })]
        public void InMemorySearch_CaseInsensitive_MatchesRegardlessOfCase(string value, StringFilterOperator op, string[] expected)
        {
            Assert.Equal(expected, Search(value, op));
        }

        [Fact]
        public void InMemorySearch_UsesOrdinalIgnoreCase_NotToLower()
        {
            var query = Data().Where("Name", "lic", StringFilterOperator.Contains, FilterCaseSensitivity.CaseInsensitive);
            var expression = query.Expression.ToString();

            Assert.Contains("OrdinalIgnoreCase", expression);
            Assert.DoesNotContain("ToLower", expression);
        }
    }
}
