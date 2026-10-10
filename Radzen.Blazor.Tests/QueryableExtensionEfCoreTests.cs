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

        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public List<CustomerOrder> Orders { get; set; } = new();
        }

        public class CustomerOrder
        {
            public int Id { get; set; }
            public int CustomerId { get; set; }
            public string Number { get; set; }
        }

        public class CustomerDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public IEnumerable<string> OrderNumbers { get; set; }
        }

        class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions options) : base(options) { }

            public DbSet<Client> Clients { get; set; }
            public DbSet<Order> Orders { get; set; }
            public DbSet<OrderLine> OrderLines { get; set; }
            public DbSet<Customer> Customers { get; set; }
            public DbSet<CustomerOrder> CustomerOrders { get; set; }
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

            context.Customers.AddRange(
                new Customer { Id = 1, Name = "a", Orders = { new CustomerOrder { Id = 1, Number = "123" }, new CustomerOrder { Id = 2, Number = "456" } } },
                new Customer { Id = 2, Name = "b" },
                new Customer { Id = 3, Name = "c", Orders = { new CustomerOrder { Id = 3, Number = "789" } } });

            context.SaveChanges();

            return context;
        }

        static IQueryable<CustomerDto> ProjectCustomers(TestDbContext context) => context.Customers.Select(c => new CustomerDto
        {
            Id = c.Id,
            Name = c.Name,
            OrderNumbers = c.Orders.Select(o => o.Number).ToList()
        });

        [Theory]
        [InlineData(FilterOperator.Contains, "123", CollectionFilterMode.Any, new[] { 1 })]
        [InlineData(FilterOperator.Equals, "789", CollectionFilterMode.Any, new[] { 3 })]
        [InlineData(FilterOperator.Contains, "9", CollectionFilterMode.All, new[] { 2, 3 })]
        public void Where_ProjectedCollectionProperty_TranslatesToSql(FilterOperator filterOperator, string value, CollectionFilterMode mode, int[] expected)
        {
            using var context = CreateContext();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = nameof(CustomerDto.OrderNumbers), FilterValue = value, FilterOperator = filterOperator, Type = typeof(IEnumerable<string>), CollectionFilterMode = mode }
            };

            var query = ProjectCustomers(context).Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.CaseInsensitive);
            var sql = query.ToQueryString();
            var result = query.OrderBy(c => c.Id).ToList();

            Assert.Contains("EXISTS", sql);
            Assert.Equal(expected, result.Select(r => r.Id));
        }

        [Theory]
        [InlineData(FilterOperator.IsEmpty, new[] { 2 })]
        [InlineData(FilterOperator.IsNotEmpty, new[] { 1, 3 })]
        public void Where_ProjectedCollectionProperty_EmptyOperators_TranslateToSql(FilterOperator filterOperator, int[] expected)
        {
            using var context = CreateContext();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor { Property = nameof(CustomerDto.OrderNumbers), FilterOperator = filterOperator, Type = typeof(IEnumerable<string>) }
            };

            var query = ProjectCustomers(context).Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.CaseInsensitive);
            var sql = query.ToQueryString();
            var result = query.OrderBy(c => c.Id).ToList();

            Assert.Equal(expected, result.Select(r => r.Id));
        }

        [Fact]
        public void Where_ProjectedCollectionProperty_SecondFilter_TranslatesToSql()
        {
            using var context = CreateContext();

            var filters = new List<FilterDescriptor>
            {
                new FilterDescriptor
                {
                    Property = nameof(CustomerDto.OrderNumbers), Type = typeof(IEnumerable<string>),
                    FilterValue = "123", FilterOperator = FilterOperator.Equals,
                    SecondFilterValue = "789", SecondFilterOperator = FilterOperator.Equals,
                    LogicalFilterOperator = LogicalFilterOperator.Or
                }
            };

            var query = ProjectCustomers(context).Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.Default);
            var result = query.OrderBy(c => c.Id).ToList();

            Assert.Equal(new[] { 1, 3 }, result.Select(r => r.Id));
        }

        [Fact]
        public void Where_CompositeFilter_ProjectedCollectionProperty_TranslatesToSql()
        {
            using var context = CreateContext();

            var filters = new List<CompositeFilterDescriptor>
            {
                new CompositeFilterDescriptor { Property = nameof(CustomerDto.OrderNumbers), Type = typeof(IEnumerable<string>), FilterValue = "456", FilterOperator = FilterOperator.Contains }
            };

            var query = ProjectCustomers(context).Where(filters, LogicalFilterOperator.And, FilterCaseSensitivity.CaseInsensitive);
            var result = query.OrderBy(c => c.Id).ToList();

            Assert.Equal(new[] { 1 }, result.Select(r => r.Id));
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
