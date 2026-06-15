using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridCheckBoxListFilterCollectionTests
    {
        public class Product
        {
            public int Id { get; set; }
            public string ProductName { get; set; }
        }

        public class OrderDetail
        {
            public int Id { get; set; }
            public int OrderId { get; set; }
            public Product Product { get; set; }
        }

        public class Order
        {
            public int Id { get; set; }
            public ICollection<OrderDetail> OrderDetails { get; set; }
        }

        class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions options) : base(options) { }

            public DbSet<Order> Orders { get; set; }
            public DbSet<OrderDetail> OrderDetails { get; set; }
            public DbSet<Product> Products { get; set; }
        }

        static TestDbContext CreateContext()
        {
            var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new TestDbContext(options);
            context.Database.EnsureCreated();

            var queso = new Product { Id = 1, ProductName = "Queso Cabrales" };
            var chai = new Product { Id = 2, ProductName = "Chai" };
            var chang = new Product { Id = 3, ProductName = "Chang" };

            context.Orders.Add(new Order
            {
                Id = 1,
                OrderDetails = new List<OrderDetail>
                {
                    new OrderDetail { Id = 1, Product = queso },
                    new OrderDetail { Id = 2, Product = chai },
                }
            });
            context.Orders.Add(new Order
            {
                Id = 2,
                OrderDetails = new List<OrderDetail> { new OrderDetail { Id = 3, Product = chang } }
            });

            context.SaveChanges();

            return context;
        }

        static List<object> LoadFilterValues(IQueryable<Order> orders, string typedFilter)
        {
            const string property = nameof(Order.OrderDetails);
            const string filterProperty = "Product.ProductName";

            IQueryable query = orders;

            if (!string.IsNullOrEmpty(typedFilter))
            {
                query = QueryableExtension.Where(query, new FilterDescriptor[]
                {
                    new FilterDescriptor()
                    {
                        Property = property,
                        FilterProperty = filterProperty,
                        FilterValue = typedFilter,
                        FilterOperator = FilterOperator.Contains
                    }
                }, LogicalFilterOperator.Or, FilterCaseSensitivity.CaseInsensitive);
            }

            var childQuery = query.SelectMany(property);

            if (!string.IsNullOrEmpty(typedFilter))
            {
                childQuery = childQuery.Where(filterProperty, typedFilter, StringFilterOperator.Contains, FilterCaseSensitivity.CaseInsensitive);
            }

            var distinctQuery = childQuery.Select(filterProperty).Distinct();

            return QueryableExtension.ToList(distinctQuery).Cast<object>().ToList();
        }

        [Fact]
        public void Typing_Queso_Returns_Only_Matching_ProductNames_Over_RealIQueryable()
        {
            using var context = CreateContext();

            var result = LoadFilterValues(context.Orders, "Queso").Cast<string>().ToList();

            Assert.Equal(new[] { "Queso Cabrales" }, result);
        }

        [Fact]
        public void Typing_NonExistent_Value_Returns_Empty_Over_RealIQueryable()
        {
            using var context = CreateContext();

            var result = LoadFilterValues(context.Orders, "ZZZ_DoesNotExist");

            Assert.Empty(result);
        }

        [Fact]
        public void No_Filter_Returns_All_Distinct_ProductNames_Over_RealIQueryable()
        {
            using var context = CreateContext();

            var result = LoadFilterValues(context.Orders, null).Cast<string>().OrderBy(x => x).ToList();

            Assert.Equal(new[] { "Chai", "Chang", "Queso Cabrales" }, result);
        }
    }
}
