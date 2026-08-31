using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class EntityFrameworkAdapterTests : IDisposable
    {
        readonly SqliteConnection connection = new("DataSource=:memory:");
        readonly Ctx context;

        public EntityFrameworkAdapterTests()
        {
            connection.Open();

            context = new Ctx(new DbContextOptionsBuilder<Ctx>().UseSqlite(connection).Options);
            context.Database.EnsureCreated();

            context.People.AddRange(Enumerable.Range(1, 40).Select(i => new Employee
            {
                Id = i,
                Name = "Name" + i,
                Department = i % 4 == 0 ? "Ops" : i % 3 == 0 ? "Sales" : "Engineering",
                Salary = 1000 + i,
            }));

            context.SaveChanges();
        }

        public void Dispose()
        {
            context.Dispose();
            connection.Dispose();
            GC.SuppressFinalize(this);
        }

        Counting executor = null!;

        IRenderedComponent<RadzenDataGrid<Employee>> Render(TestContext ctx,
            Action<ComponentParameterCollectionBuilder<RadzenDataGrid<Employee>>>? extra = null,
            bool paging = true)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");
            ctx.Services.AddRadzenQueryableEntityFrameworkAdapter();

            executor = new Counting(ctx.Services.BuildServiceProvider()
                .GetRequiredService<IAsyncQueryExecutor>());

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            return ctx.RenderComponent<RadzenDataGrid<Employee>>(p =>
            {
                p.Add(g => g.Data, context.People);
                p.Add(g => g.AllowPaging, paging);
                p.Add(g => g.PageSize, 5);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    var s = 0;

                    foreach (var (property, title) in new[]
                             {
                                 ("Name", "Name"), ("Department", "Department"), ("Salary", "Salary"),
                             })
                    {
                        builder.OpenComponent<RadzenDataGridColumn<Employee>>(s++);
                        builder.AddAttribute(s++, "Property", property);
                        builder.AddAttribute(s++, "Title", title);
                        builder.CloseComponent();
                    }
                });
                extra?.Invoke(p);
            });
        }

        static string[] Names(IRenderedComponent<RadzenDataGrid<Employee>> cut) =>
            cut.FindAll("tbody tr.rz-data-row")
                .Select(row => row.QuerySelectorAll("td")[0].TextContent.Trim()).ToArray();

        [Fact]
        public void TheAdapterRegistrationIsAllThatIsNeeded()
        {
            using var ctx = new TestContext();

            var cut = Render(ctx);

            cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll("tbody tr.rz-data-row").Count));
            Assert.Equal(new[] { "Name1", "Name2", "Name3", "Name4", "Name5" }, Names(cut));

            Assert.True(executor.ToListCalls > 0, "the page should have been fetched through the adapter");
            Assert.True(executor.CountCalls > 0, "the total should have been counted through the adapter");
        }

        [Fact]
        public void ThePagerCountsWhatTheDatabaseSays()
        {
            using var ctx = new TestContext();

            var cut = Render(ctx, p => p.Add(g => g.ShowPagingSummary, true));

            cut.WaitForAssertion(() =>
                Assert.Contains("40", cut.Find(".rz-pager-summary").TextContent, StringComparison.Ordinal));

            Assert.True(executor.CountCalls > 0, "the total should be a COUNT the database ran");
        }

        [Fact]
        public void SortingIsTranslatedRatherThanAppliedToThePage()
        {
            using var ctx = new TestContext();

            var cut = Render(ctx, p => p.Add(g => g.AllowSorting, true));

            cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll("tbody tr.rz-data-row").Count));

            cut.InvokeAsync(() => cut.Instance.OrderByDescending("Salary")).GetAwaiter().GetResult();

            cut.WaitForAssertion(() => Assert.Equal(
                new[] { "Name40", "Name39", "Name38", "Name37", "Name36" }, Names(cut)));

            Assert.True(executor.ToListCalls > 0, "the ordered page should have come through the adapter");
        }

        [Fact]
        public void TheCountCarriesTheFilter()
        {
            using var ctx = new TestContext();

            var cut = Render(ctx, p =>
            {
                p.Add(g => g.AllowFiltering, true);
                p.Add(g => g.ShowPagingSummary, true);
            });

            cut.WaitForAssertion(() => Assert.Equal(5, cut.FindAll("tbody tr.rz-data-row").Count));

            cut.InvokeAsync(() => cut.Instance.ColumnsCollection[1]
                .SetFilterValue("Ops")).GetAwaiter().GetResult();
            cut.InvokeAsync(() => cut.Instance.Reload()).GetAwaiter().GetResult();

            cut.WaitForAssertion(() =>
                Assert.Contains("10", cut.Find(".rz-pager-summary").TextContent, StringComparison.Ordinal));
        }

        [Fact]
        public void AVirtualizedGridFetchesItsWindowsThroughTheAdapter()
        {
            using var ctx = new TestContext();

            var cut = Render(ctx, p => p.Add(g => g.AllowVirtualization, true), paging: false);

            cut.WaitForAssertion(() => Assert.Equal(40, cut.FindAll("tbody tr.rz-data-row").Count));
            Assert.Equal("Name1", Names(cut)[0]);

            Assert.True(executor.ToListCalls > 0, "the window should have come through the adapter");
        }

        sealed class Counting : IAsyncQueryExecutor
        {
            readonly IAsyncQueryExecutor inner;

            public Counting(IAsyncQueryExecutor inner) => this.inner = inner;

            public int CountCalls { get; private set; }

            public int ToListCalls { get; private set; }

            public bool IsSupported<T>(IQueryable<T> queryable) => inner.IsSupported(queryable);

            public Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken token = default)
            {
                CountCalls++;

                return inner.CountAsync(queryable, token);
            }

            public Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken token = default)
            {
                ToListCalls++;

                return inner.ToListAsync(queryable, token);
            }
        }

        public class Employee
        {
            public int Id { get; set; }

            public string Name { get; set; } = "";

            public string Department { get; set; } = "";

            public decimal Salary { get; set; }
        }

        public class Ctx : DbContext
        {
            public Ctx(DbContextOptions<Ctx> options) : base(options)
            {
            }

            public DbSet<Employee> People => Set<Employee>();
        }
    }
}
