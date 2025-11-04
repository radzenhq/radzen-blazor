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

        // Collection operator tests - In/NotIn
        [Fact]
        public void Where_SupportsInOperator_WithStringArray()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Name", 
                    FilterValue = new[] { "Alice", "Bob" },
                    FilterOperator = FilterOperator.Contains
                }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Name == "Alice");
            Assert.Contains(result, r => r.Name == "Bob");
        }

        [Fact]
        public void Where_SupportsDoesNotContainOperator_WithStringArray()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Name", 
                    FilterValue = new[] { "Alice", "Bob" },
                    FilterOperator = FilterOperator.DoesNotContain
                }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(3, result.Count);
            Assert.DoesNotContain(result, r => r.Name == "Alice");
            Assert.DoesNotContain(result, r => r.Name == "Bob");
        }

        [Fact]
        public void Where_SupportsInOperator_WithNumericValues()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Value", 
                    FilterValue = new[] { 100, 200, 300 },
                    FilterOperator = FilterOperator.Contains
                }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(3, result.Count);
            Assert.All(result, r => Assert.True(r.Value == 100 || r.Value == 200 || r.Value == 300));
        }

        [Fact]
        public void Where_FiltersWithDoesNotContainOperator()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Name", 
                    FilterValue = "li",
                    FilterOperator = FilterOperator.DoesNotContain
                }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(3, result.Count);
            Assert.DoesNotContain(result, r => r.Name == "Alice");
            Assert.DoesNotContain(result, r => r.Name == "Charlie");
        }

        [Fact]
        public void Where_FiltersWithContainsOperator_EmptyString()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Name", 
                    FilterValue = "",
                    FilterOperator = FilterOperator.Contains
                }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Empty string contains returns all items (all strings contain empty string)
            Assert.Equal(5, result.Count);
        }

        [Fact]
        public void Where_FiltersWithContainsOperator_SingleValue()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Name", 
                    FilterValue = new[] { "Alice" },
                    FilterOperator = FilterOperator.Contains
                }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Single(result);
            Assert.Equal("Alice", result[0].Name);
        }

        [Fact]
        public void Where_FiltersWithDoesNotContainOperator_AllValues()
        {
            var data = GetTestData().AsQueryable();
            var allNames = data.Select(x => x.Name).ToArray();
            
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Name", 
                    FilterValue = allNames,
                    FilterOperator = FilterOperator.DoesNotContain
                }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Empty(result);
        }

        [Fact]
        public void Where_FiltersArrayProperty_WithSpecificValue()
        {
            var testData = new List<TestItem>
            {
                new TestItem { Id = 1, Name = "Item1", Value = 100, Date = DateTime.Now, IsActive = true, Category = new TestCategory { Name = "A", Priority = 1 } },
                new TestItem { Id = 2, Name = "Item2", Value = 200, Date = DateTime.Now, IsActive = false, Category = new TestCategory { Name = "B", Priority = 2 } },
                new TestItem { Id = 3, Name = "Item3", Value = 300, Date = DateTime.Now, IsActive = true, Category = new TestCategory { Name = "C", Priority = 3 } }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Name", 
                    FilterValue = "Item1",
                    FilterOperator = FilterOperator.Contains
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public void Where_FiltersArrayProperty_WithDoesNotContain()
        {
            var testData = new List<TestItem>
            {
                new TestItem { Id = 1, Name = "Item1", Value = 100, Date = DateTime.Now, IsActive = true, Category = new TestCategory { Name = "A", Priority = 1 } },
                new TestItem { Id = 2, Name = "Item2", Value = 200, Date = DateTime.Now, IsActive = false, Category = new TestCategory { Name = "B", Priority = 2 } },
                new TestItem { Id = 3, Name = "Item3", Value = 300, Date = DateTime.Now, IsActive = true, Category = new TestCategory { Name = "C", Priority = 3 } }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Name", 
                    FilterValue = "Item1",
                    FilterOperator = FilterOperator.DoesNotContain
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, r => r.Name == "Item1");
        }

        [Fact]
        public void Where_FiltersWithContainsOperator_CaseInsensitive()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Name", 
                    FilterValue = "ALICE",
                    FilterOperator = FilterOperator.Contains
                }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.CaseInsensitive).ToList();
            
            Assert.Single(result);
            Assert.Equal("Alice", result[0].Name);
        }

        [Fact]
        public void Where_FiltersWithMultipleContainsOperators_Combined()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Name", 
                    FilterValue = new[] { "Alice", "Bob" },
                    FilterOperator = FilterOperator.Contains
                },
                new FilterDescriptor 
                { 
                    Property = "Value", 
                    FilterValue = 100,
                    FilterOperator = FilterOperator.GreaterThanOrEquals
                }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.True((r.Name == "Alice" || r.Name == "Bob") && r.Value >= 100));
        }

        [Fact]
        public void Where_FiltersWithContains_OnMultipleProperties()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Name", 
                    FilterValue = new[] { "Alice", "Charlie" },
                    FilterOperator = FilterOperator.Contains
                },
                new FilterDescriptor 
                { 
                    Property = "Value", 
                    FilterValue = new[] { 100, 150 },
                    FilterOperator = FilterOperator.Contains
                }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Name == "Alice" && r.Value == 100);
            Assert.Contains(result, r => r.Name == "Charlie" && r.Value == 150);
        }

        [Fact]
        public void Where_FiltersWithContainsOperator_MultipleValues()
        {
            var data = GetTestData().AsQueryable();
            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Value", 
                    FilterValue = new[] { 100, 150, 200 },
                    FilterOperator = FilterOperator.Contains
                }
            };
            
            var result = data.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(3, result.Count);
        }

        // Property/FilterProperty tests for collection item filtering
        [Fact]
        public void Where_FiltersCollectionItemProperty_WithEquals()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 }, new { Name = "tag4", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag1", Value = 50 }, new { Name = "tag5", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = "tag1",
                    FilterOperator = FilterOperator.Equals
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithContains()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "important", Value = 10 }, new { Name = "urgent", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "normal", Value = 30 }, new { Name = "low", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "important", Value = 50 }, new { Name = "high", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = "import",
                    FilterOperator = FilterOperator.Contains
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithStartsWith()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag_alpha", Value = 10 }, new { Name = "tag_beta", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "item_gamma", Value = 30 }, new { Name = "item_delta", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag_epsilon", Value = 50 }, new { Name = "other", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = "tag_",
                    FilterOperator = FilterOperator.StartsWith
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithEndsWith()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "file.txt", Value = 10 }, new { Name = "doc.pdf", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "image.png", Value = 30 }, new { Name = "photo.jpg", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "data.txt", Value = 50 }, new { Name = "info.doc", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = ".txt",
                    FilterOperator = FilterOperator.EndsWith
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithNotEquals()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "active", Value = 10 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "inactive", Value = 20 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "pending", Value = 30 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = "inactive",
                    FilterOperator = FilterOperator.NotEquals
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithDoesNotContain()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "important-task", Value = 10 }, new { Name = "important-note", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "simple", Value = 30 }, new { Name = "basic", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "important-meeting", Value = 50 }, new { Name = "review", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = "important",
                    FilterOperator = FilterOperator.DoesNotContain
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 2);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_Numeric_WithGreaterThan()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 }, new { Name = "tag4", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 50 }, new { Name = "tag6", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Value",
                    FilterValue = 40,
                    FilterOperator = FilterOperator.GreaterThan
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Single(result);
            Assert.Equal(3, result[0].Id);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_Numeric_WithLessThan()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 }, new { Name = "tag4", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 50 }, new { Name = "tag6", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Value",
                    FilterValue = 25,
                    FilterOperator = FilterOperator.LessThan
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_Numeric_WithGreaterThanOrEquals()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 }, new { Name = "tag4", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 50 }, new { Name = "tag6", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Value",
                    FilterValue = 50,
                    FilterOperator = FilterOperator.GreaterThanOrEquals
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Single(result);
            Assert.Equal(3, result[0].Id);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_Numeric_WithLessThanOrEquals()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 }, new { Name = "tag4", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 50 }, new { Name = "tag6", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Value",
                    FilterValue = 20,
                    FilterOperator = FilterOperator.LessThanOrEquals
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithIsNull()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = (string)null, Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 }, new { Name = "tag4", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = (string)null, Value = 50 }, new { Name = "tag6", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterOperator = FilterOperator.IsNull
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithIsNotNull()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = (string)null, Value = 10 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 50 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterOperator = FilterOperator.IsNotNull
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 2);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_CaseInsensitive()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "IMPORTANT", Value = 10 }, new { Name = "urgent", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "normal", Value = 30 }, new { Name = "low", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "Important", Value = 50 }, new { Name = "high", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = "important",
                    FilterOperator = FilterOperator.Contains
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.CaseInsensitive).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_MultipleConditions_WithAnd()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 15 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 }, new { Name = "tag4", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag1", Value = 50 }, new { Name = "tag5", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Value",
                    FilterValue = 20,
                    FilterOperator = FilterOperator.LessThan,
                    SecondFilterValue = 10,
                    SecondFilterOperator = FilterOperator.GreaterThan,
                    LogicalFilterOperator = LogicalFilterOperator.And
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Only items with any tag Value < 20 AND any tag Value > 10
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_MultipleConditions_WithOr()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 }, new { Name = "tag4", Value = 70 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag1", Value = 50 }, new { Name = "tag5", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Value",
                    FilterValue = 20,
                    FilterOperator = FilterOperator.LessThan,
                    SecondFilterValue = 60,
                    SecondFilterOperator = FilterOperator.GreaterThan,
                    LogicalFilterOperator = LogicalFilterOperator.Or
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Items with tags where Value<20 OR Value>60
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 2);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithIsEmpty()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "", Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 }, new { Name = "tag4", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "", Value = 50 }, new { Name = "tag6", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterOperator = FilterOperator.IsEmpty
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithIsNotEmpty()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "", Value = 10 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 50 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterOperator = FilterOperator.IsNotEmpty
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 2);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_CombinedWithRegularFilter()
        {
            var testData = new[]
            {
                new { Id = 1, Name = "Product1", Tags = new[] { new { Label = "premium", Price = 100 } }.ToList() },
                new { Id = 2, Name = "Product2", Tags = new[] { new { Label = "standard", Price = 50 } }.ToList() },
                new { Id = 3, Name = "Product3", Tags = new[] { new { Label = "premium", Price = 150 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Label",
                    FilterValue = "premium",
                    FilterOperator = FilterOperator.Equals
                },
                new FilterDescriptor 
                { 
                    Property = "Name",
                    FilterValue = "Product1",
                    FilterOperator = FilterOperator.Contains
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public void Where_FiltersNestedCollectionItemProperty()
        {
            var testData = new[]
            {
                new { Id = 1, Items = new[] { new { Name = "item1", Meta = new { Category = "A" } } }.ToList() },
                new { Id = 2, Items = new[] { new { Name = "item2", Meta = new { Category = "B" } } }.ToList() },
                new { Id = 3, Items = new[] { new { Name = "item3", Meta = new { Category = "A" } } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Items",
                    FilterProperty = "Meta.Category",
                    FilterValue = "A",
                    FilterOperator = FilterOperator.Equals
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersNestedCollectionItemProperty_WithIn()
        {
            var testData = new[]
            {
                new { Id = 1, Items = new[] { new { Name = "item1", Meta = new { Category = "A" } } }.ToList() },
                new { Id = 2, Items = new[] { new { Name = "item2", Meta = new { Category = "B" } } }.ToList() },
                new { Id = 3, Items = new[] { new { Name = "item3", Meta = new { Category = "C" } } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor
                {
                    Property = "Items",
                    FilterProperty = "Meta.Category",
                    FilterValue = new[] { "A", "C" },
                    FilterOperator = FilterOperator.In
                }
            };

            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersNestedCollectionItemProperty_WithNotIn()
        {
            var testData = new[]
            {
                new { Id = 1, Items = new[] { new { Name = "item1", Meta = new { Category = "A" } } }.ToList() },
                new { Id = 2, Items = new[] { new { Name = "item2", Meta = new { Category = "B" } } }.ToList() },
                new { Id = 3, Items = new[] { new { Name = "item3", Meta = new { Category = "C" } } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor
                {
                    Property = "Items",
                    FilterProperty = "Meta.Category",
                    FilterValue = new[] { "A" },
                    FilterOperator = FilterOperator.NotIn
                }
            };

            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 2);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersNestedCollectionItemProperty_WithContains()
        {
            var testData = new[]
            {
                new { Id = 1, Items = new[] { new { Name = "item1", Meta = new { Category = "Alpha" } } }.ToList() },
                new { Id = 2, Items = new[] { new { Name = "item2", Meta = new { Category = "Beta" } } }.ToList() },
                new { Id = 3, Items = new[] { new { Name = "item3", Meta = new { Category = "Alpine" } } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor
                {
                    Property = "Items",
                    FilterProperty = "Meta.Category",
                    FilterValue = "Al",
                    FilterOperator = FilterOperator.Contains
                }
            };

            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersNestedCollectionItemProperty_WithDoesNotContain()
        {
            var testData = new[]
            {
                new { Id = 1, Items = new[] { new { Name = "item1", Meta = new { Category = "Entertainment" } } }.ToList() },
                new { Id = 2, Items = new[] { new { Name = "item2", Meta = new { Category = "Sports" } } }.ToList() },
                new { Id = 3, Items = new[] { new { Name = "item3", Meta = new { Category = "Edutainment" } } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor
                {
                    Property = "Items",
                    FilterProperty = "Meta.Category",
                    FilterValue = "tain",
                    FilterOperator = FilterOperator.DoesNotContain
                }
            };

            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();

            // Excludes categories containing "tain" -> Entertainment, Edutainment
            Assert.Single(result);
            Assert.Equal(2, result[0].Id);
        }

        // Nested collection (Items) with Meta also a collection. Use indexed access to Meta[0].Category
        [Fact]
        public void Where_FiltersDoubleNested_WithIn_OnFirstMeta()
        {
            var testData = new[]
            {
                new { Id = 1, Items = new[] { new { Name = "item1", Meta = new[] { new { Category = "A" }, new { Category = "X" } }.ToList() } }.ToList() },
                new { Id = 2, Items = new[] { new { Name = "item2", Meta = new[] { new { Category = "B" } }.ToList() } }.ToList() },
                new { Id = 3, Items = new[] { new { Name = "item3", Meta = new[] { new { Category = "C" } }.ToList() } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor
                {
                    Property = "Items",
                    FilterProperty = "Meta[0].Category",
                    FilterValue = new[] { "A", "C" },
                    FilterOperator = FilterOperator.In
                }
            };

            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersDoubleNested_WithNotIn_OnFirstMeta()
        {
            var testData = new[]
            {
                new { Id = 1, Items = new[] { new { Name = "item1", Meta = new[] { new { Category = "A" } }.ToList() } }.ToList() },
                new { Id = 2, Items = new[] { new { Name = "item2", Meta = new[] { new { Category = "B" } }.ToList() } }.ToList() },
                new { Id = 3, Items = new[] { new { Name = "item3", Meta = new[] { new { Category = "C" } }.ToList() } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor
                {
                    Property = "Items",
                    FilterProperty = "Meta[0].Category",
                    FilterValue = new[] { "A" },
                    FilterOperator = FilterOperator.NotIn
                }
            };

            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 2);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersDoubleNested_WithContains_OnFirstMeta()
        {
            var testData = new[]
            {
                new { Id = 1, Items = new[] { new { Name = "item1", Meta = new[] { new { Category = "Alpha" } }.ToList() } }.ToList() },
                new { Id = 2, Items = new[] { new { Name = "item2", Meta = new[] { new { Category = "Beta" } }.ToList() } }.ToList() },
                new { Id = 3, Items = new[] { new { Name = "item3", Meta = new[] { new { Category = "Alpine" } }.ToList() } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor
                {
                    Property = "Items",
                    FilterProperty = "Meta[0].Category",
                    FilterValue = "Al",
                    FilterOperator = FilterOperator.Contains
                }
            };

            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersDoubleNested_WithDoesNotContain_OnFirstMeta()
        {
            var testData = new[]
            {
                new { Id = 1, Items = new[] { new { Name = "item1", Meta = new[] { new { Category = "Entertainment" } }.ToList() } }.ToList() },
                new { Id = 2, Items = new[] { new { Name = "item2", Meta = new[] { new { Category = "Sports" } }.ToList() } }.ToList() },
                new { Id = 3, Items = new[] { new { Name = "item3", Meta = new[] { new { Category = "Edutainment" } }.ToList() } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor
                {
                    Property = "Items",
                    FilterProperty = "Meta[0].Category",
                    FilterValue = "tain",
                    FilterOperator = FilterOperator.DoesNotContain
                }
            };

            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();

            Assert.Single(result);
            Assert.Equal(2, result[0].Id);
        }

        // CollectionFilterMode tests
        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_Any_WithEquals()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 }, new { Name = "tag4", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag1", Value = 50 }, new { Name = "tag5", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = "tag1",
                    FilterOperator = FilterOperator.Equals,
                    CollectionFilterMode = CollectionFilterMode.Any
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where at least one tag has Name == "tag1"
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_All_WithEquals()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "active", Value = 10 }, new { Name = "active", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "active", Value = 30 }, new { Name = "inactive", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "active", Value = 50 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = "active",
                    FilterOperator = FilterOperator.Equals,
                    CollectionFilterMode = CollectionFilterMode.All
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where all tags have Name == "active"
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_Any_WithGreaterThan()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 }, new { Name = "tag4", Value = 60 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 5 }, new { Name = "tag6", Value = 8 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Value",
                    FilterValue = 50,
                    FilterOperator = FilterOperator.GreaterThan,
                    CollectionFilterMode = CollectionFilterMode.Any
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where at least one tag has Value > 50
            Assert.Single(result);
            Assert.Equal(2, result[0].Id);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_All_WithGreaterThan()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 60 }, new { Name = "tag2", Value = 70 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 }, new { Name = "tag4", Value = 60 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 55 }, new { Name = "tag6", Value = 80 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Value",
                    FilterValue = 50,
                    FilterOperator = FilterOperator.GreaterThan,
                    CollectionFilterMode = CollectionFilterMode.All
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where all tags have Value > 50
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_Any_WithContains()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "important-task", Value = 10 }, new { Name = "review", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "normal", Value = 30 }, new { Name = "basic", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "important-meeting", Value = 50 }, new { Name = "urgent", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = "important",
                    FilterOperator = FilterOperator.Contains,
                    CollectionFilterMode = CollectionFilterMode.Any
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where at least one tag contains "important"
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_All_WithContains()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "important-task", Value = 10 }, new { Name = "important-note", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "important-meeting", Value = 30 }, new { Name = "basic", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "important-reminder", Value = 50 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = "important",
                    FilterOperator = FilterOperator.Contains,
                    CollectionFilterMode = CollectionFilterMode.All
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where all tags contain "important"
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_Any_WithStartsWith()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "prefix_alpha", Value = 10 }, new { Name = "other_beta", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "prefix_gamma", Value = 30 }, new { Name = "prefix_delta", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "other_epsilon", Value = 50 }, new { Name = "other_zeta", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = "prefix_",
                    FilterOperator = FilterOperator.StartsWith,
                    CollectionFilterMode = CollectionFilterMode.Any
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where at least one tag starts with "prefix_"
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 2);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_All_WithStartsWith()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "prefix_alpha", Value = 10 }, new { Name = "prefix_beta", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "prefix_gamma", Value = 30 }, new { Name = "other_delta", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "prefix_epsilon", Value = 50 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = "prefix_",
                    FilterOperator = FilterOperator.StartsWith,
                    CollectionFilterMode = CollectionFilterMode.All
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where all tags start with "prefix_"
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_Any_WithEndsWith()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "file.txt", Value = 10 }, new { Name = "doc.pdf", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "image.png", Value = 30 }, new { Name = "photo.txt", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "data.csv", Value = 50 }, new { Name = "info.doc", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = ".txt",
                    FilterOperator = FilterOperator.EndsWith,
                    CollectionFilterMode = CollectionFilterMode.Any
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where at least one tag ends with ".txt"
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 2);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_All_WithEndsWith()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "file.txt", Value = 10 }, new { Name = "doc.txt", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "image.txt", Value = 30 }, new { Name = "photo.png", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "data.txt", Value = 50 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = ".txt",
                    FilterOperator = FilterOperator.EndsWith,
                    CollectionFilterMode = CollectionFilterMode.All
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where all tags end with ".txt"
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_Any_WithNotEquals()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "active", Value = 10 }, new { Name = "inactive", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "inactive", Value = 30 }, new { Name = "inactive", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "pending", Value = 50 }, new { Name = "active", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = "inactive",
                    FilterOperator = FilterOperator.NotEquals,
                    CollectionFilterMode = CollectionFilterMode.Any
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where at least one tag is not equal to "inactive"
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_All_WithNotEquals()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "active", Value = 10 }, new { Name = "pending", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "active", Value = 30 }, new { Name = "inactive", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "pending", Value = 50 }, new { Name = "active", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = "inactive",
                    FilterOperator = FilterOperator.NotEquals,
                    CollectionFilterMode = CollectionFilterMode.All
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where all tags are not equal to "inactive"
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_Any_WithDoesNotContain()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "test-alpha", Value = 10 }, new { Name = "beta", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "test-gamma", Value = 30 }, new { Name = "test-delta", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "epsilon", Value = 50 }, new { Name = "zeta", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = "test",
                    FilterOperator = FilterOperator.DoesNotContain,
                    CollectionFilterMode = CollectionFilterMode.Any
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where at least one tag does not contain "test"
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_All_WithDoesNotContain()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "alpha", Value = 10 }, new { Name = "beta", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "test-gamma", Value = 30 }, new { Name = "delta", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "epsilon", Value = 50 }, new { Name = "zeta", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterValue = "test",
                    FilterOperator = FilterOperator.DoesNotContain,
                    CollectionFilterMode = CollectionFilterMode.All
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where all tags do not contain "test"
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_Any_WithLessThan()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 10 }, new { Name = "tag2", Value = 50 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 60 }, new { Name = "tag4", Value = 70 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 15 }, new { Name = "tag6", Value = 80 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Value",
                    FilterValue = 20,
                    FilterOperator = FilterOperator.LessThan,
                    CollectionFilterMode = CollectionFilterMode.Any
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where at least one tag has Value < 20
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_All_WithLessThan()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 10 }, new { Name = "tag2", Value = 15 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 18 }, new { Name = "tag4", Value = 70 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 5 }, new { Name = "tag6", Value = 12 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Value",
                    FilterValue = 20,
                    FilterOperator = FilterOperator.LessThan,
                    CollectionFilterMode = CollectionFilterMode.All
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where all tags have Value < 20
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_Any_WithLessThanOrEquals()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 20 }, new { Name = "tag2", Value = 50 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 60 }, new { Name = "tag4", Value = 70 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 15 }, new { Name = "tag6", Value = 80 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Value",
                    FilterValue = 20,
                    FilterOperator = FilterOperator.LessThanOrEquals,
                    CollectionFilterMode = CollectionFilterMode.Any
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where at least one tag has Value <= 20
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_All_WithLessThanOrEquals()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 18 }, new { Name = "tag4", Value = 70 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 5 }, new { Name = "tag6", Value = 12 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Value",
                    FilterValue = 20,
                    FilterOperator = FilterOperator.LessThanOrEquals,
                    CollectionFilterMode = CollectionFilterMode.All
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where all tags have Value <= 20
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_Any_WithGreaterThanOrEquals()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 50 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 10 }, new { Name = "tag4", Value = 15 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 60 }, new { Name = "tag6", Value = 30 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Value",
                    FilterValue = 50,
                    FilterOperator = FilterOperator.GreaterThanOrEquals,
                    CollectionFilterMode = CollectionFilterMode.Any
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where at least one tag has Value >= 50
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_All_WithGreaterThanOrEquals()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 50 }, new { Name = "tag2", Value = 60 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 55 }, new { Name = "tag4", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 70 }, new { Name = "tag6", Value = 80 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Value",
                    FilterValue = 50,
                    FilterOperator = FilterOperator.GreaterThanOrEquals,
                    CollectionFilterMode = CollectionFilterMode.All
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where all tags have Value >= 50
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_Any_WithIsNull()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = (string)null, Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 }, new { Name = "tag4", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = (string)null, Value = 50 }, new { Name = "tag6", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterOperator = FilterOperator.IsNull,
                    CollectionFilterMode = CollectionFilterMode.Any
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where at least one tag has null Name
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_All_WithIsNull()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = (string)null, Value = 10 }, new { Name = (string)null, Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = (string)null, Value = 30 }, new { Name = "tag4", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = (string)null, Value = 50 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterOperator = FilterOperator.IsNull,
                    CollectionFilterMode = CollectionFilterMode.All
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where all tags have null Name
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_Any_WithIsNotNull()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = (string)null, Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = (string)null, Value = 30 }, new { Name = (string)null, Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 50 }, new { Name = "tag6", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterOperator = FilterOperator.IsNotNull,
                    CollectionFilterMode = CollectionFilterMode.Any
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where at least one tag has non-null Name
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_All_WithIsNotNull()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 }, new { Name = (string)null, Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 50 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterOperator = FilterOperator.IsNotNull,
                    CollectionFilterMode = CollectionFilterMode.All
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where all tags have non-null Name
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_Any_WithIsEmpty()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "", Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 }, new { Name = "tag4", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "", Value = 50 }, new { Name = "tag6", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterOperator = FilterOperator.IsEmpty,
                    CollectionFilterMode = CollectionFilterMode.Any
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where at least one tag has empty Name
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_All_WithIsEmpty()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "", Value = 10 }, new { Name = "", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "", Value = 30 }, new { Name = "tag4", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "", Value = 50 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterOperator = FilterOperator.IsEmpty,
                    CollectionFilterMode = CollectionFilterMode.All
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where all tags have empty Name
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_Any_WithIsNotEmpty()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "", Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "", Value = 30 }, new { Name = "", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 50 }, new { Name = "tag6", Value = 60 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterOperator = FilterOperator.IsNotEmpty,
                    CollectionFilterMode = CollectionFilterMode.Any
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where at least one tag has non-empty Name
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }

        [Fact]
        public void Where_FiltersCollectionItemProperty_WithCollectionFilterMode_All_WithIsNotEmpty()
        {
            var testData = new[]
            {
                new { Id = 1, Tags = new[] { new { Name = "tag1", Value = 10 }, new { Name = "tag2", Value = 20 } }.ToList() },
                new { Id = 2, Tags = new[] { new { Name = "tag3", Value = 30 }, new { Name = "", Value = 40 } }.ToList() },
                new { Id = 3, Tags = new[] { new { Name = "tag5", Value = 50 } }.ToList() }
            }.AsQueryable();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor 
                { 
                    Property = "Tags",
                    FilterProperty = "Name",
                    FilterOperator = FilterOperator.IsNotEmpty,
                    CollectionFilterMode = CollectionFilterMode.All
                }
            };
            
            var result = testData.Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default).ToList();
            
            // Should return items where all tags have non-empty Name
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 1);
            Assert.Contains(result, r => r.Id == 3);
        }
    }
}

