using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class AsyncSeamProviderConcurrencyTests : IDisposable
    {
        readonly SqliteConnection connection = new("DataSource=:memory:");
        readonly Parking parking = new();
        readonly EntityFrameworkQueryTests.Ctx context;

        public AsyncSeamProviderConcurrencyTests()
        {
            connection.Open();

            context = new EntityFrameworkQueryTests.Ctx(
                new DbContextOptionsBuilder<EntityFrameworkQueryTests.Ctx>()
                    .UseSqlite(connection)
                    .AddInterceptors(parking)
                    .Options);

            context.Database.EnsureCreated();

            context.People.AddRange(Enumerable.Range(1, 500).Select(i => new EntityFrameworkQueryTests.Employee
            {
                Id = i,
                Name = "Name" + i,
                Department = "Engineering",
                Salary = 1000 + i,
            }));

            context.SaveChanges();
        }

        public void Dispose()
        {
            parking.Release();

            context.Dispose();
            connection.Dispose();

            GC.SuppressFinalize(this);
        }

        sealed class Parking : DbCommandInterceptor
        {
            readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

            int armed;

            public int Started;

            public void Arm() => Volatile.Write(ref armed, 1);

            public void Release() => release.TrySetResult();

            public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
                DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
                CancellationToken cancellationToken = default)
            {
                if (Interlocked.CompareExchange(ref armed, 0, 1) == 1)
                {
                    Interlocked.Increment(ref Started);

                    await release.Task;
                }
                else
                {
                    Interlocked.Increment(ref Started);
                }

                return result;
            }
        }

        IRenderedComponent<RadzenListBox<EntityFrameworkQueryTests.Employee>> VirtualListBox(TestContext ctx,
            IAsyncQueryExecutor? instead = null)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            if (instead != null)
            {
                ctx.Services.AddSingleton(instead);
            }

            var cut = ctx.RenderComponent<RadzenListBox<EntityFrameworkQueryTests.Employee>>(p =>
            {
                p.Add(d => d.Data, context.People);
                p.Add(d => d.TextProperty, "Name");
                p.Add(d => d.AllowVirtualization, true);
                p.Add(d => d.PageSize, 5);
            });

            cut.WaitForAssertion(() => Assert.Contains("Name1", cut.Markup));

            return cut;
        }

        [Fact]
        public async Task ASecondLookupWaitsForTheFirstRatherThanRacingItAtTheProvider()
        {
            using var ctx = new TestContext();

            var cut = VirtualListBox(ctx);

            parking.Arm();

            var before = parking.Started;

            var end = cut.InvokeAsync(() => cut.Instance.VirtualItemAt(499));

            Assert.True(SpinWait.SpinUntil(() => parking.Started == before + 1, TimeSpan.FromSeconds(5)));

            var home = cut.InvokeAsync(() => cut.Instance.VirtualItemAt(200));

            await cut.InvokeAsync(() => { });

            Assert.Equal(before + 1, parking.Started);

            parking.Release();

            Assert.Null(await end);

            var row = await home as EntityFrameworkQueryTests.Employee;

            Assert.Equal("Name201", row?.Name);
        }

        IRenderedComponent<RadzenDataGrid<EntityFrameworkQueryTests.Employee>> PagedGrid(TestContext ctx,
            IAsyncQueryExecutor? instead = null)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");
            if (instead != null)
            {
                ctx.Services.AddSingleton(instead);
            }

            var cut = ctx.RenderComponent<RadzenDataGrid<EntityFrameworkQueryTests.Employee>>(p =>
            {
                p.Add(g => g.Data, context.People);
                p.Add(g => g.AllowPaging, true);
                p.Add(g => g.PageSize, 5);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent<RadzenDataGridColumn<EntityFrameworkQueryTests.Employee>>(0);
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
            });

            cut.WaitForAssertion(() => Assert.Contains("Name1", cut.Markup));

            return cut;
        }

        [Fact]
        public async Task ASecondLoadWaitsForTheFirstRatherThanRacingItAtTheProvider()
        {
            using var ctx = new TestContext();

            var cut = PagedGrid(ctx);

            parking.Arm();

            var before = parking.Started;

            var first = cut.InvokeAsync(() => cut.Instance.Reload());

            Assert.True(SpinWait.SpinUntil(() => parking.Started > before, TimeSpan.FromSeconds(5)));

            var second = cut.InvokeAsync(() => cut.Instance.Reload());

            await cut.InvokeAsync(() => { });

            Assert.Equal(before + 1, parking.Started);

            parking.Release();

            await second;
            await first;

            Assert.Contains("Name1", cut.Markup);
        }

        sealed class Switchable : IAsyncQueryExecutor
        {
            readonly IAsyncQueryExecutor inner;

            public Switchable(IAsyncQueryExecutor inner) => this.inner = inner;

            public bool Supported = true;

            public bool IsSupported<T>(IQueryable<T> queryable) => Supported && inner.IsSupported(queryable);

            public Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken token = default) =>
                inner.CountAsync(queryable, token);

            public Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken token = default) =>
                inner.ToListAsync(queryable, token);

            public bool CanFallBackToSynchronous(Exception exception) =>
                inner.CanFallBackToSynchronous(exception);
        }

        [Fact]
        public async Task ASynchronousReplacementWaitsForTheQueryItSuperseded()
        {
            using var ctx = new TestContext();

            var executor = new Switchable(RealExecutor());

            var cut = PagedGrid(ctx, executor);

            parking.Arm();

            var before = parking.Started;

            var superseded = cut.InvokeAsync(() => cut.Instance.Reload());

            Assert.True(SpinWait.SpinUntil(() => parking.Started > before, TimeSpan.FromSeconds(5)));

            executor.Supported = false;

            var replacement = cut.InvokeAsync(async () =>
            {
                await cut.Instance.Reload();

                return cut.Instance.PagedView.Count();
            });

            parking.Release();

            await superseded;

            Assert.Equal(5, await replacement);
        }

        IRenderedComponent<RadzenDataList<EntityFrameworkQueryTests.Employee>> PagedList(TestContext ctx,
            IAsyncQueryExecutor instead)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddSingleton(instead);

            var cut = ctx.RenderComponent<RadzenDataList<EntityFrameworkQueryTests.Employee>>(p =>
            {
                p.Add(l => l.Data, context.People);
                p.Add(l => l.AllowPaging, true);
                p.Add(l => l.PageSize, 5);
                p.Add(l => l.Template, (EntityFrameworkQueryTests.Employee e) => (RenderFragment)(b => b.AddContent(0, e.Name)));
            });

            cut.WaitForAssertion(() => Assert.Contains("Name1", cut.Markup));

            return cut;
        }

        sealed class ParkingBothHalves : IAsyncQueryExecutor
        {
            readonly IAsyncQueryExecutor inner;

            readonly TaskCompletionSource page = new(TaskCreationOptions.RunContinuationsAsynchronously);
            readonly TaskCompletionSource count = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public ParkingBothHalves(IAsyncQueryExecutor inner) => this.inner = inner;

            public bool Supported = true;

            public bool Armed;

            public int Pages;

            public int Counts;

            public void ReleasePage() => page.TrySetResult();

            public void ReleaseCount() => count.TrySetResult();

            public bool IsSupported<T>(IQueryable<T> queryable) => Supported && inner.IsSupported(queryable);

            public async Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken token = default)
            {
                if (Armed && Interlocked.Increment(ref Pages) == 1)
                {
                    await page.Task;
                }

                return await inner.ToListAsync(queryable, CancellationToken.None);
            }

            public async Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken token = default)
            {
                if (Armed && Interlocked.Increment(ref Counts) == 1)
                {
                    await count.Task;
                }

                return await inner.CountAsync(queryable, CancellationToken.None);
            }

            public bool CanFallBackToSynchronous(Exception exception) =>
                inner.CanFallBackToSynchronous(exception);
        }

        [Fact]
        public async Task ASynchronousReplacementWaitsForTheWholeSupersededLoadAndNotJustItsCurrentQuery()
        {
            using var ctx = new TestContext();

            var executor = new ParkingBothHalves(RealExecutor());

            var cut = PagedList(ctx, executor);

            executor.Armed = true;

            var superseded = cut.InvokeAsync(() => cut.Instance.Reload());

            Assert.True(SpinWait.SpinUntil(() => executor.Counts > 0, TimeSpan.FromSeconds(5)),
                "the count query should have parked");

            executor.Supported = false;

            var replacement = cut.InvokeAsync(async () =>
            {
                await cut.Instance.Reload();

                return cut.Instance.PagedView.Cast<object>().Count();
            });

            await cut.InvokeAsync(() => { });

            executor.ReleaseCount();

            Assert.True(SpinWait.SpinUntil(() => executor.Pages > 0, TimeSpan.FromSeconds(5)),
                "the page that follows the count should have started");

            await cut.InvokeAsync(() => { });

            Assert.False(replacement.IsCompleted,
                "the replacement must still be waiting: the load it superseded has a query still to make");

            executor.ReleasePage();

            await superseded;

            Assert.Equal(5, await replacement);
        }

        [Fact]
        public async Task ABusyContextIsNotMistakenForAQueryTheProviderCannotTranslate()
        {
            var concurrency = await ProvokeConcurrencyException();

            Assert.IsType<InvalidOperationException>(concurrency);

            using var ctx = new TestContext();

            var throwing = new Throwing(RealExecutor());

            Assert.True(throwing.CanFallBackToSynchronous(new NotSupportedException("cannot be translated")));
            Assert.False(throwing.CanFallBackToSynchronous(concurrency));

            var cut = VirtualListBox(ctx, throwing);

            throwing.Failure = new NotSupportedException("The LINQ expression could not be translated.");

            var translated = await cut.InvokeAsync(() => cut.Instance.VirtualItemAt(200))
                as EntityFrameworkQueryTests.Employee;

            Assert.Equal("Name201", translated?.Name);

            throwing.Failure = concurrency;

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => cut.InvokeAsync(() => cut.Instance.VirtualItemAt(300)));
        }

        sealed class Throwing : IAsyncQueryExecutor
        {
            readonly IAsyncQueryExecutor inner;

            public Throwing(IAsyncQueryExecutor inner) => this.inner = inner;

            public Exception? Failure;

            public bool IsSupported<T>(IQueryable<T> queryable) => true;

            public Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken token = default) =>
                Failure == null ? Task.FromResult(queryable.Count()) : Task.FromException<int>(Failure);

            public Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken token = default) =>
                Failure == null ? Task.FromResult(queryable.ToList()) : Task.FromException<List<T>>(Failure);

            public bool CanFallBackToSynchronous(Exception exception) =>
                inner.CanFallBackToSynchronous(exception);
        }

        static IAsyncQueryExecutor RealExecutor() => AsyncEnumerableQueryExecutor.Instance;

        async Task<Exception> ProvokeConcurrencyException()
        {
            parking.Arm();

            var first = context.People.Skip(400).Take(1).ToListAsync();

            Assert.True(SpinWait.SpinUntil(() => parking.Started > 0, TimeSpan.FromSeconds(5)));

            Exception? caught = null;

            try
            {
                await context.People.Skip(1).Take(1).ToListAsync();
            }
            catch (Exception exception)
            {
                caught = exception;
            }

            parking.Release();

            await first;

            Assert.NotNull(caught);

            return caught!;
        }
    }
}
