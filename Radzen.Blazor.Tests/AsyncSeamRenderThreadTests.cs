using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class AsyncSeamRenderThreadTests
    {
        public class Row
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        sealed class CountingProvider : IQueryProvider
        {
            readonly IQueryProvider inner;

            public CountingProvider(IQueryProvider inner) => this.inner = inner;

            public int Executions { get; private set; }

            public Func<bool>? InFlight { get; set; }

            public int Overlapping { get; private set; }

            void Note()
            {
                Executions++;

                if (InFlight?.Invoke() == true)
                {
                    Overlapping++;
                }
            }

            public IQueryable CreateQuery(Expression expression) => inner.CreateQuery(expression);

            public IQueryable<T> CreateQuery<T>(Expression expression) =>
                new Counting<T>(inner.CreateQuery<T>(expression), this);

            public object Execute(Expression expression)
            {
                Note();

                return inner.Execute(expression);
            }

            public T Execute<T>(Expression expression)
            {
                Note();

                return inner.Execute<T>(expression);
            }

            public void Walked() => Note();
        }

        sealed class Counting<T> : IQueryable<T>
        {
            readonly IQueryable<T> inner;
            readonly CountingProvider provider;

            public Counting(IQueryable<T> inner, CountingProvider provider)
            {
                this.inner = inner;
                this.provider = provider;
            }

            public Type ElementType => inner.ElementType;

            public Expression Expression => inner.Expression;

            public IQueryProvider Provider => provider;

            public IEnumerator<T> GetEnumerator()
            {
                provider.Walked();

                return inner.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        sealed class YieldingExecutor : IAsyncQueryExecutor
        {
            public int Started { get; private set; }

            public int Finished { get; private set; }

            public bool IsSupported<T>(IQueryable<T> queryable) => true;

            public async Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken token = default)
            {
                Started++;

                await Task.Yield();

                Finished++;

                return queryable.Count();
            }

            public async Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken token = default)
            {
                Started++;

                await Task.Yield();

                Finished++;

                return queryable.ToList();
            }
        }

        static (CountingProvider Provider, IQueryable<Row> Source) Source(int count)
        {
            var backing = Enumerable.Range(1, count)
                .Select(i => new Row { Id = i, Name = "R" + i }).ToList().AsQueryable();

            var provider = new CountingProvider(backing.Provider);

            return (provider, new Counting<Row>(backing, provider));
        }

        static IRenderedComponent<RadzenDataGrid<Row>> Grid(TestContext ctx, IQueryable<Row> source,
            Action<ComponentParameterCollectionBuilder<RadzenDataGrid<Row>>> extra)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            return ctx.RenderComponent<RadzenDataGrid<Row>>(p =>
            {
                p.Add(g => g.Data, source);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Row>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                extra(p);
            });
        }

        [Fact]
        public void VirtualizationWithPagingGoesThroughTheExecutor()
        {
            using var ctx = new TestContext();

            var (provider, source) = Source(40);
            var executor = new YieldingExecutor();

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = Grid(ctx, source, p =>
            {
                p.Add(g => g.AllowVirtualization, true);
                p.Add(g => g.AllowPaging, true);
                p.Add(g => g.PageSize, 5);
            });

            cut.WaitForAssertion(() => Assert.True(executor.Started > 0,
                "the page should be counted and fetched through the executor"));

            cut.WaitForAssertion(() => Assert.Equal(40, cut.Instance.Count));
        }

        sealed class UntranslatableExecutor : IAsyncQueryExecutor
        {
            public bool IsSupported<T>(IQueryable<T> queryable) => true;

            public Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken token = default) =>
                throw new InvalidOperationException("The LINQ expression could not be translated.");

            public Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken token = default) =>
                throw new InvalidOperationException("The LINQ expression could not be translated.");
        }

        [Fact]
        public void AQueryTheProviderCannotTranslateFallsBackRatherThanThrowing()
        {
            using var ctx = new TestContext();

            var (_, source) = Source(40);

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(new UntranslatableExecutor());

            var cut = Grid(ctx, source, p =>
            {
                p.Add(g => g.AllowPaging, true);
                p.Add(g => g.PageSize, 5);
            });

            Assert.Equal(5, cut.FindAll("tbody tr.rz-data-row").Count);
            Assert.Equal(40, cut.Instance.Count);
        }

        [Fact]
        public void ADropDownFallsBackWhenTheProviderCannotTranslate()
        {
            using var ctx = new TestContext();

            var (_, source) = Source(40);

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(new UntranslatableExecutor());

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var cut = ctx.RenderComponent<RadzenDropDown<Row>>(p =>
            {
                p.Add(d => d.Data, source);
                p.Add(d => d.TextProperty, "Name");
                p.Add(d => d.AllowVirtualization, true);
            });

            Assert.NotNull(cut.Instance);
        }

        sealed class ParkingExecutor : IAsyncQueryExecutor
        {
            readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            int calls;

            int executing;

            public bool Parked { get; private set; }

            public bool Executing => Volatile.Read(ref executing) > 0;

            public void Release() => release.TrySetResult();

            public bool IsSupported<T>(IQueryable<T> queryable) => true;

            public async Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken token = default)
            {
                await Park();

                return Run(queryable.Count);
            }

            public async Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken token = default)
            {
                await Park();

                return Run(queryable.ToList);
            }

            TResult Run<TResult>(Func<TResult> query)
            {
                Interlocked.Increment(ref executing);

                try
                {
                    return query();
                }
                finally
                {
                    Interlocked.Decrement(ref executing);
                }
            }

            async Task Park()
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    Parked = true;

                    await release.Task;

                    Parked = false;
                }
            }
        }

        [Fact]
        public void AVirtualizedGridDoesNotProbeTheSourceWhileItsWindowIsInFlight()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var (provider, source) = Source(40);
            var executor = new ParkingExecutor();

            provider.InFlight = () => executor.Parked && !executor.Executing;

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = Grid(ctx, source, p =>
            {
                p.Add(g => g.AllowVirtualization, true);
                p.Add(g => g.PageSize, 5);
            });

            cut.WaitForState(() => executor.Parked);

            cut.Render();
            cut.Render();

            Assert.Equal(0, provider.Overlapping);

            executor.Release();
        }

        [Fact]
        public void AVirtualizedListBoxDoesNotEnumerateTheSourceWhileItsWindowIsInFlight()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var (provider, source) = Source(40);
            var executor = new ParkingExecutor();

            provider.InFlight = () => executor.Parked && !executor.Executing;

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = ctx.RenderComponent<RadzenListBox<Row>>(p =>
            {
                p.Add(d => d.Data, source);
                p.Add(d => d.TextProperty, "Name");
                p.Add(d => d.AllowVirtualization, true);
                p.Add(d => d.PageSize, 5);
            });

            cut.WaitForState(() => executor.Parked);

            cut.Render();
            cut.Render();

            var root = cut.Find("div.rz-listbox");

            root.KeyDown(new KeyboardEventArgs { Code = "ArrowDown", Key = "ArrowDown" });
            root.KeyDown(new KeyboardEventArgs { Code = "ArrowDown", Key = "ArrowDown" });
            root.KeyDown(new KeyboardEventArgs { Code = "End", Key = "End" });

            Assert.Equal(0, provider.Overlapping);

            executor.Release();
        }

        sealed class ImmediateExecutor : IAsyncQueryExecutor
        {
            public int Queries { get; private set; }

            public bool IsSupported<T>(IQueryable<T> queryable) => true;

            public Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken token = default)
            {
                Queries++;

                return Task.FromResult(queryable.Count());
            }

            public Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken token = default)
            {
                Queries++;

                return Task.FromResult(queryable.ToList());
            }
        }

        [Fact]
        public void AVirtualizedGridDoesNotProbeTheSourceOnItsFirstRender()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var (provider, source) = Source(40);
            var executor = new ImmediateExecutor();

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = Grid(ctx, source, p =>
            {
                p.Add(g => g.AllowVirtualization, true);
                p.Add(g => g.PageSize, 5);
            });

            cut.WaitForAssertion(() => Assert.Contains("R1", cut.Markup));

            Assert.True(executor.Queries > 0, "the window should be fetched through the executor");
            Assert.Equal(executor.Queries, provider.Executions);
        }

        [Fact]
        public void AGroupedVirtualizedGridDoesNotProbeTheSourceWhileItsWindowIsInFlight()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var (provider, source) = Source(40);
            var executor = new ParkingExecutor();

            provider.InFlight = () => executor.Parked && !executor.Executing;

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = ctx.RenderComponent<RadzenDataGrid<Row>>(p =>
            {
                p.Add(g => g.Data, source);
                p.Add(g => g.AllowVirtualization, true);
                p.Add(g => g.AllowGrouping, true);
                p.Add(g => g.PageSize, 5);
                p.Add(g => g.Groups, new System.Collections.ObjectModel.ObservableCollection<GroupDescriptor>
                {
                    new GroupDescriptor { Property = nameof(Row.Name) }
                });
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Row>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
            });

            cut.WaitForState(() => executor.Parked);

            cut.Render();
            cut.Render();

            Assert.Equal(0, provider.Overlapping);

            executor.Release();
        }

        [Fact]
        public void AVirtualizedGridRecoversFromAFilterThatMatchedNothing()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var (_, source) = Source(40);

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(new ImmediateExecutor());

            void Build(ComponentParameterCollectionBuilder<RadzenDataGrid<Row>> p, string? filter)
            {
                p.Add(g => g.Data, source);
                p.Add(g => g.AllowVirtualization, true);
                p.Add(g => g.AllowFiltering, true);
                p.Add(g => g.PageSize, 5);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Row>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.AddAttribute(2, "Filterable", true);
                    builder.AddAttribute(3, "FilterValue", filter);
                    builder.AddAttribute(4, "FilterOperator", FilterOperator.Contains);
                    builder.CloseComponent();
                });
            }

            var cut = ctx.RenderComponent<RadzenDataGrid<Row>>(p => Build(p, null));

            Assert.Contains("R1", cut.Markup);

            cut.SetParametersAndRender(p => Build(p, "matches-nothing"));

            Assert.DoesNotContain("R1", cut.Markup);

            cut.SetParametersAndRender(p => Build(p, null));

            Assert.Contains("R1", cut.Markup);
        }

        [Fact]
        public void AVirtualizedListBoxFetchesAnOutOfWindowJumpThroughTheExecutor()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var (provider, source) = Source(40);
            var executor = new ImmediateExecutor();

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = ctx.RenderComponent<RadzenListBox<Row>>(p =>
            {
                p.Add(d => d.Data, source);
                p.Add(d => d.TextProperty, "Name");
                p.Add(d => d.AllowVirtualization, true);
                p.Add(d => d.PageSize, 5);
            });

            cut.WaitForAssertion(() => Assert.True(executor.Queries > 0));

            var root = cut.Find("div.rz-listbox");

            root.KeyDown(new KeyboardEventArgs { Code = "End", Key = "End" });

            Assert.Equal(executor.Queries, provider.Executions);
        }

        sealed class ParkingRowExecutor : IAsyncQueryExecutor
        {
            readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public int Parked;

            public CancellationToken RowToken;

            public bool Untranslatable;

            public void Release() => release.TrySetResult();

            public bool IsSupported<T>(IQueryable<T> queryable) => true;

            public Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken token = default) =>
                Task.FromResult(queryable.Count());

            public async Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken token = default)
            {
                if (queryable.Expression.ToString().Contains("Take(1)", StringComparison.Ordinal))
                {
                    RowToken = token;

                    Interlocked.Increment(ref Parked);

                    await release.Task;

                    if (Untranslatable)
                    {
                        throw new NotSupportedException("The LINQ expression could not be translated.");
                    }
                }

                return queryable.ToList();
            }
        }

        static IRenderedComponent<RadzenListBox<Row>> VirtualListBox(TestContext ctx, IQueryable<Row> source)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var cut = ctx.RenderComponent<RadzenListBox<Row>>(p =>
            {
                p.Add(d => d.Data, source);
                p.Add(d => d.TextProperty, "Name");
                p.Add(d => d.AllowVirtualization, true);
                p.Add(d => d.PageSize, 5);
            });

            cut.WaitForAssertion(() => Assert.Contains("R1", cut.Markup));

            return cut;
        }

        [Fact]
        public async Task AnOutOfWindowLookupIsSupersededByALaterOne()
        {
            using var ctx = new TestContext();

            var (_, source) = Source(500);
            var executor = new ParkingRowExecutor();

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = VirtualListBox(ctx, source);

            var end = cut.InvokeAsync(() => cut.Instance.VirtualItemAt(499));

            Assert.True(SpinWait.SpinUntil(() => executor.Parked == 1, TimeSpan.FromSeconds(5)));

            var later = cut.InvokeAsync(() => cut.Instance.VirtualItemAt(200));

            await cut.InvokeAsync(() => { });

            Assert.Equal(1, executor.Parked);

            executor.Release();

            Assert.Null(await end);
            Assert.Equal("R201", ((await later) as Row)?.Name);
        }

        [Fact]
        public async Task AnOutOfWindowLookupIsSupersededByAWindowHit()
        {
            using var ctx = new TestContext();

            var (_, source) = Source(500);
            var executor = new ParkingRowExecutor();

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = VirtualListBox(ctx, source);

            var end = cut.InvokeAsync(() => cut.Instance.VirtualItemAt(499));

            Assert.True(SpinWait.SpinUntil(() => executor.Parked == 1, TimeSpan.FromSeconds(5)));

            Assert.Equal("R1", (await cut.InvokeAsync(() => cut.Instance.VirtualItemAt(0)) as Row)?.Name);

            executor.Release();

            Assert.Null(await end);
        }

        [Fact]
        public async Task AnOutOfWindowLookupThatFallsBackIsStillGuarded()
        {
            using var ctx = new TestContext();

            var (_, source) = Source(500);
            var executor = new ParkingRowExecutor { Untranslatable = true };

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = VirtualListBox(ctx, source);

            var lookup = cut.InvokeAsync(() => cut.Instance.VirtualItemAt(499));

            Assert.True(SpinWait.SpinUntil(() => executor.Parked == 1, TimeSpan.FromSeconds(5)));

            var (_, replacement) = Source(3);

            cut.SetParametersAndRender(p => p.Add(d => d.Data, replacement));

            executor.Release();

            Assert.Null(await lookup);
        }

        [Fact]
        public async Task ReplacingTheQueryCancelsTheLookupStillAtTheProvider()
        {
            using var ctx = new TestContext();

            var (_, source) = Source(500);
            var executor = new ParkingRowExecutor();

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = VirtualListBox(ctx, source);

            var lookup = cut.InvokeAsync(() => cut.Instance.VirtualItemAt(499));

            Assert.True(SpinWait.SpinUntil(() => executor.Parked == 1, TimeSpan.FromSeconds(5)));

            Assert.False(executor.RowToken.IsCancellationRequested);

            var (_, replacement) = Source(3);

            cut.SetParametersAndRender(p => p.Add(d => d.Data, replacement));

            Assert.True(executor.RowToken.IsCancellationRequested,
                "invalidating the query must stand down the lookup still holding the provider");

            executor.Release();

            Assert.Null(await lookup);
        }

        [Fact]
        public async Task DisposingCancelsTheLookupStillAtTheProvider()
        {
            using var ctx = new TestContext();

            var (_, source) = Source(500);
            var executor = new ParkingRowExecutor();

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = VirtualListBox(ctx, source);

            var lookup = cut.InvokeAsync(() => cut.Instance.VirtualItemAt(499));

            Assert.True(SpinWait.SpinUntil(() => executor.Parked == 1, TimeSpan.FromSeconds(5)));

            Assert.False(executor.RowToken.IsCancellationRequested);

            await cut.InvokeAsync(() => cut.Instance.Dispose());

            Assert.True(executor.RowToken.IsCancellationRequested);

            executor.Release();

            await lookup;
        }

        sealed class ParkingWindowExecutor : IAsyncQueryExecutor
        {
            readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public int Parked;

            public CancellationToken WindowToken;

            public void Release() => release.TrySetResult();

            public bool IsSupported<T>(IQueryable<T> queryable) => true;

            public async Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken token = default)
            {
                await Park(token);

                return queryable.Count();
            }

            public async Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken token = default)
            {
                await Park(token);

                return queryable.ToList();
            }

            async Task Park(CancellationToken token)
            {
                WindowToken = token;

                Interlocked.Increment(ref Parked);

                await release.Task;
            }
        }

        [Fact]
        public async Task ReloadingCancelsTheVirtualizationRequestBeforeWaitingForIt()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var (_, source) = Source(500);
            var executor = new ParkingWindowExecutor();

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = Grid(ctx, source, p =>
            {
                p.Add(g => g.AllowVirtualization, true);
                p.Add(g => g.AllowPaging, false);
                p.Add(g => g.PageSize, 5);
            });

            Assert.True(SpinWait.SpinUntil(() => executor.Parked > 0, TimeSpan.FromSeconds(5)));

            Assert.False(executor.WindowToken.IsCancellationRequested);

            var reload = cut.InvokeAsync(() => cut.Instance.Reload());

            await cut.InvokeAsync(() => { });

            Assert.True(executor.WindowToken.IsCancellationRequested,
                "the outstanding window fetch must be cancelled before the reload waits for it");

            executor.Release();

            await reload;
        }

        sealed class TimingOutExecutor : IAsyncQueryExecutor
        {
            public bool Failing;

            public bool IsSupported<T>(IQueryable<T> queryable) => true;

            public Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken token = default) =>
                Failing
                    ? throw new OperationCanceledException("the provider gave up")
                    : Task.FromResult(queryable.Count());

            public Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken token = default) =>
                Failing
                    ? throw new OperationCanceledException("the provider gave up")
                    : Task.FromResult(queryable.ToList());
        }

        [Fact]
        public async Task AnUnaskedForCancellationIsNotReportedAsAnEmptyWindow()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var (_, source) = Source(40);

            var executor = new TimingOutExecutor();

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = Grid(ctx, source, p =>
            {
                p.Add(g => g.AllowVirtualization, true);
                p.Add(g => g.AllowPaging, false);
                p.Add(g => g.PageSize, 5);
            });

            executor.Failing = true;

            var request = new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderRequest(
                0, 5, CancellationToken.None);

            var thrown = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => cut.InvokeAsync(async () => await cut.Instance.LoadItems(request)));

            Assert.Contains("the provider gave up", Flatten(thrown));
        }

        [Fact]
        public async Task AnUnaskedForCancellationDoesNotLookLikeASupersededLoad()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var (_, source) = Source(40);

            var executor = new TimingOutExecutor();

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = Grid(ctx, source, p =>
            {
                p.Add(g => g.AllowPaging, true);
                p.Add(g => g.PageSize, 5);
            });

            executor.Failing = true;

            var thrown = await Assert.ThrowsAnyAsync<Exception>(
                () => cut.InvokeAsync(() => cut.Instance.Reload()));

            Assert.Contains("the provider gave up", Flatten(thrown));
        }

        [Fact]
        public async Task AnUnaskedForCancellationIsNotReportedAsAMissingRow()
        {
            using var ctx = new TestContext();

            var (_, source) = Source(500);
            var executor = new TimingOutExecutor();

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = VirtualListBox(ctx, source);

            executor.Failing = true;

            var thrown = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => cut.InvokeAsync(() => cut.Instance.VirtualItemAt(499)));

            Assert.Contains("the provider gave up", Flatten(thrown));
        }

        [Fact]
        public async Task DisposingTheComponentDoesNotDisposeTheTokenALoadStillHolds()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var (_, source) = Source(40);
            var executor = new WaitHandleExecutor();

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = Grid(ctx, source, p =>
            {
                p.Add(g => g.AllowPaging, true);
                p.Add(g => g.PageSize, 5);
            });

            executor.Arm();

            var load = cut.InvokeAsync(() => cut.Instance.Reload());

            Assert.True(SpinWait.SpinUntil(() => executor.Parked, TimeSpan.FromSeconds(5)));

            await cut.InvokeAsync(() => cut.Instance.Dispose());

            executor.Release();

            await load;

            Assert.Null(executor.Failure);
            Assert.True(executor.Cancelled, "the parked load should have been cancelled");
        }

        [Fact]
        public async Task DisposingADataListCancelsItsVirtualRequestWithoutDisposingTheToken()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var (_, source) = Source(40);
            var executor = new WaitHandleExecutor();

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = ctx.RenderComponent<RadzenDataList<Row>>(p =>
            {
                p.Add(d => d.Data, source);
                p.Add(d => d.AllowVirtualization, true);
                p.Add<RenderFragment<Row>>(d => d.Template,
                    item => builder => builder.AddContent(0, item.Name));
            });

            executor.Arm();

            var load = cut.InvokeAsync(() => cut.Instance.Virtualize!.RefreshDataAsync());

            Assert.True(SpinWait.SpinUntil(() => executor.Parked, TimeSpan.FromSeconds(5)));

            await cut.InvokeAsync(() => cut.Instance.Dispose());

            executor.Release();

            await load;

            Assert.Null(executor.Failure);
            Assert.True(executor.Cancelled, "the parked virtual request should have been cancelled");
        }

        sealed class WaitHandleExecutor : IAsyncQueryExecutor
        {
            readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

            bool armed;

            public bool Parked { get; private set; }

            public bool Cancelled { get; private set; }

            public Exception? Failure { get; private set; }

            public void Arm() => armed = true;

            public void Release() => release.TrySetResult();

            public bool IsSupported<T>(IQueryable<T> queryable) => true;

            public async Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken token = default)
            {
                await Park(token);

                return queryable.Count();
            }

            public async Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken token = default)
            {
                await Park(token);

                return queryable.ToList();
            }

            async Task Park(CancellationToken token)
            {
                if (!armed || Parked)
                {
                    return;
                }

                Parked = true;

                await release.Task;

                try
                {
                    Cancelled = token.WaitHandle.WaitOne(0) || token.IsCancellationRequested;
                }
                catch (Exception exception)
                {
                    Failure = exception;
                }
            }
        }

        static string Flatten(Exception exception)
        {
            var text = new System.Text.StringBuilder();

            for (var current = exception; current != null; current = current.InnerException)
            {
                text.Append(current.Message).Append(' ');
            }

            return text.ToString();
        }

        [Fact]
        public void EveryPivotProviderExecutionIsOneTheExecutorAskedFor()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var (provider, source) = Source(40);
            var executor = new YieldingExecutor();

            provider.InFlight = () => executor.Started > executor.Finished;

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = ctx.RenderComponent<RadzenPivotDataGrid<Row>>(p =>
            {
                p.Add(g => g.Data, source);
                p.Add(g => g.AllowPaging, true);
                p.Add(g => g.PageSize, 5);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenPivotColumn<Row>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                p.Add<RenderFragment>(g => g.Rows, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenPivotRow<Row>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                p.Add<RenderFragment>(g => g.Aggregates, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenPivotAggregate<Row>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Aggregate", AggregateFunction.Sum);
                    builder.CloseComponent();
                });
            });

            cut.WaitForAssertion(() => Assert.Equal(40, cut.Instance.Count));

            Assert.Equal(0, provider.Overlapping);
            Assert.True(executor.Started > 0, "the page should be counted and fetched through the executor");
        }

        [Fact]
        public void EveryProviderExecutionIsOneTheExecutorAskedFor()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var (provider, source) = Source(40);
            var executor = new YieldingExecutor();

            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var cut = ctx.RenderComponent<RadzenDataGrid<Row>>(p =>
            {
                p.Add(g => g.Data, source);
                p.Add(g => g.AllowPaging, true);
                p.Add(g => g.PageSize, 5);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Row>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
            });

            cut.WaitForAssertion(() => Assert.Equal(40, cut.Instance.Count));

            cut.InvokeAsync(() => cut.Instance.GoToPage(2)).GetAwaiter().GetResult();

            Assert.Equal(executor.Started, provider.Executions);
        }
    }
}
