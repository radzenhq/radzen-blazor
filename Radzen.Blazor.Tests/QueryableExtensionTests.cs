using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Radzen;

namespace Radzen.Blazor.Tests
{
    public class QueryableExtensionTests
    {
        private class TestItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Value { get; set; }
            public DateTime Date { get; set; }
            public bool IsActive { get; set; }
            public TestCategory Category { get; set; }
        }

        private class TestCategory
        {
            public string Name { get; set; }
            public int Priority { get; set; }
        }

        private List<TestItem> GetTestData()
        {
            return new List<TestItem>
            {
                new TestItem { Id = 1, Name = "Alice", Value = 100, Date = new DateTime(2023, 1, 1), IsActive = true, Category = new TestCategory { Name = "A", Priority = 1 } },
                new TestItem { Id = 2, Name = "Bob", Value = 200, Date = new DateTime(2023, 6, 1), IsActive = false, Category = new TestCategory { Name = "B", Priority = 2 } },
                new TestItem { Id = 3, Name = "Charlie", Value = 150, Date = new DateTime(2023, 12, 1), IsActive = true, Category = new TestCategory { Name = "A", Priority = 1 } },
                new TestItem { Id = 4, Name = "David", Value = 300, Date = new DateTime(2023, 3, 1), IsActive = false, Category = new TestCategory { Name = "C", Priority = 3 } },
                new TestItem { Id = 5, Name = "Eve", Value = 250, Date = new DateTime(2023, 9, 1), IsActive = true, Category = new TestCategory { Name = "B", Priority = 2 } }
            };
        }

        // OrderBy tests
        [Fact]
        public void OrderBy_SortsAscending_ByDefault()
        {
            var data = GetTestData().AsQueryable();
            
            var result = data.OrderBy("Value").ToList();
            
            Assert.Equal(100, result[0].Value);
            Assert.Equal(150, result[1].Value);
            Assert.Equal(200, result[2].Value);
            Assert.Equal(250, result[3].Value);
            Assert.Equal(300, result[4].Value);
        }

        [Fact]
        public void OrderBy_SortsDescending_WhenSpecified()
        {
            var data = GetTestData().AsQueryable();
            
            var result = data.OrderBy("Value desc").ToList();
            
            Assert.Equal(300, result[0].Value);
            Assert.Equal(250, result[1].Value);
            Assert.Equal(200, result[2].Value);
            Assert.Equal(150, result[3].Value);
            Assert.Equal(100, result[4].Value);
        }

        [Fact]
        public void OrderBy_HandlesMultipleProperties()
        {
            var data = GetTestData().AsQueryable();
            
            var result = data.OrderBy("Category.Priority, Value desc").ToList();
            
            Assert.Equal(1, result[0].Category.Priority);
            Assert.Equal(150, result[0].Value);
        }

        [Fact]
        public void OrderBy_HandlesNestedProperties()
        {
            var data = GetTestData().AsQueryable();
            
            var result = data.OrderBy("Category.Name").ToList();
            
            Assert.Equal("A", result[0].Category.Name);
            Assert.Equal("A", result[1].Category.Name);
            Assert.Equal("B", result[2].Category.Name);
        }

        [Fact]
        public void OrderBy_HandlesLambdaExpression()
        {
            var data = GetTestData().AsQueryable();
            
            var result = data.OrderBy("x => x.Value").ToList();
            
            Assert.Equal(100, result[0].Value);
            Assert.Equal(150, result[1].Value);
        }

        // Where tests with FilterDescriptor
        [Fact]
        public void Where_FiltersWithEquals()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Name", FilterValue = "Alice", FilterOperator = FilterOperator.Equals }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Single(result);
            Assert.Equal("Alice", result[0].Name);
        }

        [Fact]
        public void Where_FiltersWithNotEquals()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Name", FilterValue = "Alice", FilterOperator = FilterOperator.NotEquals }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(4, result.Count);
            Assert.DoesNotContain(result, r => r.Name == "Alice");
        }

        [Fact]
        public void Where_FiltersWithContains()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Name", FilterValue = "li", FilterOperator = FilterOperator.Contains }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Collection(result,
                r => Assert.Equal("Alice", r.Name),
                r => Assert.Equal("Charlie", r.Name));
        }

        [Fact]
        public void Where_FiltersWithStartsWith()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Name", FilterValue = "Al", FilterOperator = FilterOperator.StartsWith }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Single(result);
            Assert.Equal("Alice", result[0].Name);
        }

        [Fact]
        public void Where_FiltersWithEndsWith()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Name", FilterValue = "ice", FilterOperator = FilterOperator.EndsWith }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Single(result);
            Assert.Equal("Alice", result[0].Name);
        }

        [Fact]
        public void Where_FiltersWithLessThan()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Value", FilterValue = 200, FilterOperator = FilterOperator.LessThan }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.True(r.Value < 200));
        }

        [Fact]
        public void Where_FiltersWithLessThanOrEquals()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Value", FilterValue = 200, FilterOperator = FilterOperator.LessThanOrEquals }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(3, result.Count);
            Assert.All(result, r => Assert.True(r.Value <= 200));
        }

        [Fact]
        public void Where_FiltersWithGreaterThan()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Value", FilterValue = 200, FilterOperator = FilterOperator.GreaterThan }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.True(r.Value > 200));
        }

        [Fact]
        public void Where_FiltersWithGreaterThanOrEquals()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Value", FilterValue = 200, FilterOperator = FilterOperator.GreaterThanOrEquals }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(3, result.Count);
            Assert.All(result, r => Assert.True(r.Value >= 200));
        }

        [Fact]
        public void Where_FiltersWithIsNull()
        {
            var data = new List<TestItem>
            {
                new TestItem { Id = 1, Name = null },
                new TestItem { Id = 2, Name = "Test" }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Name", FilterOperator = FilterOperator.IsNull }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Single(result);
            Assert.Null(result[0].Name);
        }

        [Fact]
        public void Where_FiltersWithIsNotNull()
        {
            var data = new List<TestItem>
            {
                new TestItem { Id = 1, Name = null },
                new TestItem { Id = 2, Name = "Test" }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Name", FilterOperator = FilterOperator.IsNotNull }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Single(result);
            Assert.NotNull(result[0].Name);
        }

        [Fact]
        public void Where_FiltersWithIsEmpty()
        {
            var data = new List<TestItem>
            {
                new TestItem { Id = 1, Name = "" },
                new TestItem { Id = 2, Name = "Test" }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Name", FilterOperator = FilterOperator.IsEmpty }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Single(result);
            Assert.Equal(string.Empty, result[0].Name);
        }

        [Fact]
        public void Where_FiltersWithIsNotEmpty()
        {
            var data = new List<TestItem>
            {
                new TestItem { Id = 1, Name = "" },
                new TestItem { Id = 2, Name = "Test" }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Name", FilterOperator = FilterOperator.IsNotEmpty }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Single(result);
            Assert.Equal("Test", result[0].Name);
        }

        [Fact]
        public void Where_CombinesFiltersWithAnd()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Value", FilterValue = 150, FilterOperator = FilterOperator.GreaterThan },
                new FilterDescriptor { Property = "IsActive", FilterValue = true, FilterOperator = FilterOperator.Equals }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(1, result.Count);
            Assert.Equal("Eve", result[0].Name);
        }

        [Fact]
        public void Where_CombinesFiltersWithOr()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Value", FilterValue = 100, FilterOperator = FilterOperator.Equals },
                new FilterDescriptor { Property = "Value", FilterValue = 300, FilterOperator = FilterOperator.Equals }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.Or, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Value == 100);
            Assert.Contains(result, r => r.Value == 300);
        }

        [Fact]
        public void Where_RespectsCaseInsensitivity()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Name", FilterValue = "alice", FilterOperator = FilterOperator.Equals }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.CaseInsensitive).ToList();
            
            Assert.Single(result);
            Assert.Equal("Alice", result[0].Name);
        }

        [Fact]
        public void Where_ReturnsAllWhenNoFilters()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>();
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(5, result.Count);
        }

        // Aggregate function tests
        [Fact]
        public void Sum_CalculatesCorrectly()
        {
            var data = GetTestData().AsQueryable();
            var values = data.Select("Value");
            
            var result = values.Sum(typeof(int));
            
            Assert.Equal(1000, result);
        }

        [Fact]
        public void Average_CalculatesCorrectly()
        {
            var data = GetTestData().AsQueryable();
            
            var result = data.Select(x => x.Value).Average();
            
            Assert.Equal(200, result);
        }

        [Fact]
        public void Min_ReturnsCorrectValue()
        {
            var data = GetTestData().AsQueryable();
            
            var result = data.Select(x => x.Value).Min();
            
            Assert.Equal(100, result);
        }

        [Fact]
        public void Max_ReturnsCorrectValue()
        {
            var data = GetTestData().AsQueryable();
            
            var result = data.Select(x => x.Value).Max();
            
            Assert.Equal(300, result);
        }

        // FirstOrDefault and LastOrDefault tests
        [Fact]
        public void FirstOrDefault_ReturnsFirstElement()
        {
            var data = GetTestData().AsQueryable();
            
            var result = data.FirstOrDefault();
            
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public void FirstOrDefault_ReturnsNullWhenEmpty()
        {
            var data = new List<TestItem>().AsQueryable();
            
            var result = data.FirstOrDefault();
            
            Assert.Null(result);
        }

        [Fact]
        public void LastOrDefault_ReturnsLastElement()
        {
            var data = GetTestData().AsQueryable();
            
            var result = data.LastOrDefault();
            
            Assert.NotNull(result);
            Assert.Equal(5, result.Id);
        }

        [Fact]
        public void LastOrDefault_ReturnsNullWhenEmpty()
        {
            var data = new List<TestItem>().AsQueryable();
            
            var result = data.LastOrDefault();
            
            Assert.Null(result);
        }

        // Select tests
        [Fact]
        public void Select_ProjectsProperty()
        {
            var data = GetTestData().AsQueryable();
            
            var result = data.Select("Name");
            
            Assert.Equal(5, result.Cast<string>().ToList().Count);
        }

        [Fact]
        public void Select_ProjectsNestedProperty()
        {
            var data = GetTestData().AsQueryable();
            
            var result = data.Select("Category.Name");
            
            Assert.Equal(5, result.Cast<string>().ToList().Count);
        }

        // Distinct tests
        [Fact]
        public void Distinct_RemovesDuplicates()
        {
            var data = new List<TestItem>
            {
                new TestItem { Id = 1, Name = "Test" },
                new TestItem { Id = 2, Name = "Test" },
                new TestItem { Id = 3, Name = "Other" }
            }.AsQueryable();

            var names = data.Select("Name");
            var result = names.Distinct();
            
            Assert.True(QueryableExtension.ToList(result).Count == 2);
        }

        // Cast tests
        [Fact]
        public void Cast_ConvertsType()
        {
            var data = GetTestData().AsQueryable();
            var values = data.Select("Value");
            
            var result = values.Cast(typeof(int));
            
            Assert.IsAssignableFrom<IQueryable<int>>(result);
        }

        // ToList tests
        [Fact]
        public void ToList_ConvertsQueryableToList()
        {
            var data = GetTestData().AsQueryable();
            
            var result = QueryableExtension.ToList(data);
            
            Assert.IsType<List<TestItem>>(result);
            Assert.Equal(5, result.Count);
        }

        // IsEnumerable tests
        [Fact]
        public void IsEnumerable_ReturnsTrueForList()
        {
            Assert.True(QueryableExtension.IsEnumerable(typeof(List<int>)));
        }

        [Fact]
        public void IsEnumerable_ReturnsTrueForArray()
        {
            Assert.True(QueryableExtension.IsEnumerable(typeof(int[])));
        }

        [Fact]
        public void IsEnumerable_ReturnsFalseForString()
        {
            Assert.False(QueryableExtension.IsEnumerable(typeof(string)));
        }

        [Fact]
        public void IsEnumerable_ReturnsFalseForInt()
        {
            Assert.False(QueryableExtension.IsEnumerable(typeof(int)));
        }

        // Complex filtering scenarios
        [Fact]
        public void Where_HandlesNestedPropertyFiltering()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Category.Name", FilterValue = "A", FilterOperator = FilterOperator.Equals }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal("A", r.Category.Name));
        }

        [Fact]
        public void Where_HandlesDateTimeFiltering()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Date", FilterValue = new DateTime(2023, 6, 1), FilterOperator = FilterOperator.GreaterThan }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.True(r.Date > new DateTime(2023, 6, 1)));
        }

        [Fact]
        public void Where_HandlesBooleanFiltering()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "IsActive", FilterValue = true, FilterOperator = FilterOperator.Equals }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(3, result.Count);
            Assert.All(result, r => Assert.True(r.IsActive));
        }

        [Fact]
        public void Where_HandlesSecondFilterWithAnd()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Value", 
                    FilterValue = 100, 
                    FilterOperator = FilterOperator.GreaterThan,
                    SecondFilterValue = 300,
                    SecondFilterOperator = FilterOperator.LessThan,
                    LogicalFilterOperator = LogicalFilterOperator.And
                }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(3, result.Count);
            Assert.All(result, r => Assert.True(r.Value > 100 && r.Value < 300));
        }

        [Fact]
        public void Where_HandlesSecondFilterWithOr()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Value", 
                    FilterValue = 150, 
                    FilterOperator = FilterOperator.LessThan,
                    SecondFilterValue = 250,
                    SecondFilterOperator = FilterOperator.GreaterThan,
                    LogicalFilterOperator = LogicalFilterOperator.Or
                }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Values less than 150 (100) OR greater than 250 (300) = 2 records
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.True(r.Value < 150 || r.Value > 250));
        }
    }
}

