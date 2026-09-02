using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class QueryableExtensionEfCoreTests
    {
        public class Client
        {
            public int Id { get; set; }
            public long? ClientNr { get; set; }
            public long Nr { get; set; }
            public string Name { get; set; }
        }

        public class Order
        {
            public int Id { get; set; }
            public List<OrderLine> Lines { get; set; }
        }

        public class OrderLine
        {
            public int Id { get; set; }
            public int OrderId { get; set; }
            public string Product { get; set; }
        }

        class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions options) : base(options) { }

            public DbSet<Client> Clients { get; set; }
            public DbSet<Order> Orders { get; set; }
            public DbSet<OrderLine> OrderLines { get; set; }
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

            context.Clients.AddRange(
                new Client { Id = 1, ClientNr = 100, Nr = 100, Name = "a" },
                new Client { Id = 2, ClientNr = 200, Nr = 200, Name = "b" },
                new Client { Id = 3, ClientNr = 300, Nr = 300, Name = "c" },
                new Client { Id = 4, ClientNr = null, Nr = 400, Name = null });

            context.Orders.AddRange(
                new Order { Id = 1, Lines = new List<OrderLine> { new OrderLine { Id = 1, Product = "apple" }, new OrderLine { Id = 2, Product = "pear" } } },
                new Order { Id = 2, Lines = new List<OrderLine>() },
                new Order { Id = 3, Lines = new List<OrderLine> { new OrderLine { Id = 3, Product = "apple" } } });

            context.SaveChanges();

            return context;
        }

        [Theory]
        [InlineData(CollectionFilterMode.Any, new[] { 1, 3 })]
        [InlineData(CollectionFilterMode.All, new[] { 2, 3 })]
        public void Where_CollectionItemProperty_TranslatesToSql(CollectionFilterMode mode, int[] expected)
        {
            using var context = CreateContext();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Lines", FilterProperty = "Product", FilterValue = "apple", FilterOperator = FilterOperator.Equals, CollectionFilterMode = mode }
            };

            var query = context.Orders.AsQueryable().Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default);
            var sql = query.ToQueryString();
            var result = query.OrderBy(o => o.Id).ToList();

            Assert.Equal(expected, result.Select(r => r.Id));
        }

        [Theory]
        [InlineData(FilterOperator.IsEmpty, new[] { 2 })]
        [InlineData(FilterOperator.IsNotEmpty, new[] { 1, 3 })]
        [InlineData(FilterOperator.IsNotNull, new[] { 1, 2, 3 })]
        [InlineData(FilterOperator.IsNull, new int[0])]
        public void Where_CollectionProperty_NullAndEmptyOperators_TranslateToSql(FilterOperator filterOperator, int[] expected)
        {
            using var context = CreateContext();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Lines", FilterOperator = filterOperator }
            };

            var query = context.Orders.AsQueryable().Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default);
            var sql = query.ToQueryString();
            var result = query.OrderBy(o => o.Id).ToList();

            Assert.Equal(expected, result.Select(r => r.Id));
        }

        [Fact]
        public void Where_CollectionItemProperty_In_TranslatesToSql()
        {
            using var context = CreateContext();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Lines", FilterProperty = "Product", FilterValue = new[] { "pear" }, FilterOperator = FilterOperator.In }
            };

            var query = context.Orders.AsQueryable().Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default);
            var sql = query.ToQueryString();
            var result = query.ToList();

            Assert.Equal(new[] { 1 }, result.Select(r => r.Id));
        }

        [Fact]
        public void Where_In_NullableColumn_TranslatesToSql()
        {
            using var context = CreateContext();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "ClientNr", FilterValue = new long[] { 100, 300 }, FilterOperator = FilterOperator.In }
            };

            var query = context.Clients.AsQueryable().Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default);
            var sql = query.ToQueryString();
            var result = query.ToList();

            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, r => r.ClientNr == null);
        }

        [Fact]
        public void Where_NotIn_NullableColumn_TranslatesToSql()
        {
            using var context = CreateContext();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "ClientNr", FilterValue = new long[] { 100, 300 }, FilterOperator = FilterOperator.NotIn }
            };

            var query = context.Clients.AsQueryable().Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default);
            var sql = query.ToQueryString();
            var result = query.ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == 2);
            Assert.Contains(result, r => r.Id == 4);
        }

        [Fact]
        public void Where_In_NonNullableColumn_TranslatesToSql()
        {
            using var context = CreateContext();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Nr", FilterValue = new List<long> { 100, 300 }, FilterOperator = FilterOperator.In }
            };

            var query = context.Clients.AsQueryable().Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default);
            var result = query.ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Where_In_StringColumn_TranslatesToSql()
        {
            using var context = CreateContext();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = "Name", FilterValue = new List<string> { "a", "c" }, FilterOperator = FilterOperator.In }
            };

            var query = context.Clients.AsQueryable().Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default);
            var result = query.ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Where_SecondFilterValue_In_TranslatesToSql()
        {
            using var context = CreateContext();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor
                {
                    Property = "ClientNr",
                    FilterValue = 100L,
                    FilterOperator = FilterOperator.Equals,
                    SecondFilterValue = new long[] { 200, 300 },
                    SecondFilterOperator = FilterOperator.In,
                    LogicalFilterOperator = LogicalFilterOperator.Or
                }
            };

            var query = context.Clients.AsQueryable().Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default);
            var sql = query.ToQueryString();
            var result = query.ToList();

            Assert.Equal(3, result.Count);
        }
    }
}
