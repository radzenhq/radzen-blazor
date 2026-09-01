using System;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class AsyncQueryExecutorTests
    {
        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Team { get; set; }
        }

        sealed class RecordingExecutor : IAsyncQueryExecutor
        {
            public int CountCalls;
            public int ToListCalls;
            public int LargestToList;
            public bool Supported = true;

            public bool IsSupported<T>(IQueryable<T> queryable) => Supported;

            public Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
            {
                CountCalls++;
                return Task.FromResult(queryable.Count());
            }

            public Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
            {
                ToListCalls++;

                var items = queryable.ToList();

                LargestToList = Math.Max(LargestToList, items.Count);

                return Task.FromResult(items);
            }
        }

        static List<Person> People(int n) =>
            Enumerable.Range(1, n).Select(i => new Person { Id = i, Name = "Person " + i, Team = "Team " + (i % 3) }).ToList();

        static object CoordinatorFor(RadzenComponent component)
        {
            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

            object host = null;

            for (var type = component.GetType(); type != null && host == null; type = type.BaseType)
            {
                host = type.GetField("asyncQuery", flags)?.GetValue(component);
            }

            return host == null ? null : typeof(AsyncQueryHost).GetField("coordinator", flags).GetValue(host);
        }

        static IRenderedComponent<RadzenDataGrid<Person>> RenderGrid(TestContext ctx, IEnumerable<Person> data, int pageSize) =>
            ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, data);
                p.Add(g => g.AllowPaging, true);
                p.Add(g => g.PageSize, pageSize);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.CloseComponent();
                    builder.OpenComponent(2, typeof(RadzenDataGridColumn<Person>));
                    builder.AddAttribute(3, "Property", "Name");
                    builder.CloseComponent();
                });
            });

        [Fact]
        public void DataGrid_VirtualizationWithPaging_UsesAsyncExecutor()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new RecordingExecutor();
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People(60).AsQueryable());
                p.Add(g => g.AllowPaging, true);
                p.Add(g => g.AllowVirtualization, true);
                p.Add(g => g.PageSize, 10);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.CloseComponent();
                    builder.OpenComponent(2, typeof(RadzenDataGridColumn<Person>));
                    builder.AddAttribute(3, "Property", "Name");
                    builder.CloseComponent();
                });
            });

            Assert.True(executor.CountCalls > 0, "the count should have gone through the executor");
            Assert.True(executor.ToListCalls > 0, "the page should have been materialized through the executor");

            Assert.Equal(10, executor.LargestToList);

            Assert.Equal(60, component.Instance.Count);
            Assert.Contains("Person 1", component.Markup);
        }

        [Fact]
        public void DataGrid_UsesAsyncExecutor_ForCountAndPage_WhenRegistered()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new RecordingExecutor();
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var component = RenderGrid(ctx, People(23).AsQueryable(), pageSize: 10);

            Assert.True(executor.CountCalls > 0, "CountAsync should have been called");
            Assert.True(executor.ToListCalls > 0, "ToListAsync should have been called");
            Assert.NotNull(CoordinatorFor(component.Instance));

            Assert.Equal(23, component.Instance.Count);
            Assert.Contains(">Person 1<", component.Markup);
            Assert.Contains(">Person 10<", component.Markup);
            Assert.DoesNotContain(">Person 11<", component.Markup);
        }

        [Fact]
        public void DataGrid_FallsBackToSync_WhenExecutorDoesNotSupportQueryable()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new RecordingExecutor { Supported = false };
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var component = RenderGrid(ctx, People(23).AsQueryable(), pageSize: 10);

            Assert.Equal(0, executor.CountCalls);
            Assert.Equal(0, executor.ToListCalls);
            Assert.Null(CoordinatorFor(component.Instance));
            Assert.Equal(23, component.Instance.Count);
            Assert.Contains(">Person 10<", component.Markup);
            Assert.DoesNotContain(">Person 11<", component.Markup);
        }

        [Fact]
        public void DataGrid_Virtualization_UsesAsyncExecutor()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new RecordingExecutor();
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People(500).AsQueryable());
                p.Add(g => g.AllowVirtualization, true);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.CloseComponent();
                });
            });

            Assert.True(executor.CountCalls > 0, "CountAsync should have been called by LoadItems");
            Assert.True(executor.ToListCalls > 0, "ToListAsync should have been called by LoadItems");
        }

        sealed class GatedExecutor : IAsyncQueryExecutor
        {
            readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            int calls;
            public readonly List<CancellationToken> CountTokens = new();

            public void Release() => release.TrySetResult();

            public bool Supported = true;

            public bool IsSupported<T>(IQueryable<T> queryable) => Supported;

            public async Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
            {
                CountTokens.Add(cancellationToken);
                if (Interlocked.Increment(ref calls) > 1)
                {
                    await release.Task;
                    cancellationToken.ThrowIfCancellationRequested();
                }
                return queryable.Count();
            }

            public Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
                => Task.FromResult(queryable.ToList());
        }

        [Fact]
        public async Task DataGrid_CancelsSupersededLoad()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new GatedExecutor();
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var component = RenderGrid(ctx, People(30).AsQueryable(), pageSize: 10);

            var loadA = component.InvokeAsync(() => component.Instance.Reload());
            var loadB = component.InvokeAsync(() => component.Instance.Reload());
            executor.Release();
            await Task.WhenAll(loadA, loadB);

            Assert.True(executor.CountTokens.Count >= 3);
            Assert.True(executor.CountTokens[1].IsCancellationRequested, "the superseded load's token must be cancelled");
            Assert.False(executor.CountTokens[^1].IsCancellationRequested, "the latest load's token must remain active");
            Assert.Equal(30, component.Instance.Count);
        }

        [Fact]
        public async Task DataGrid_CancelsSupersededLoad_WhenTheReplacementIsSynchronous()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new GatedExecutor();
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var component = RenderGrid(ctx, People(30).AsQueryable(), pageSize: 10);

            var parked = component.InvokeAsync(() => component.Instance.Reload());

            executor.Supported = false;
            var replacement = component.InvokeAsync(() => component.Instance.Reload());

            executor.Release();
            await Task.WhenAll(parked, replacement);

            Assert.True(executor.CountTokens[1].IsCancellationRequested,
                "the superseded load's token must be cancelled even though the replacement is synchronous");
            Assert.Equal(30, component.Instance.Count);
        }

        sealed class AdvisoryGatedExecutor : IAsyncQueryExecutor
        {
            readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            int calls;

            public void Release() => release.TrySetResult();

            public bool Supported = true;

            public const int StaleCount = 4242;

            public bool IsSupported<T>(IQueryable<T> queryable) => Supported;

            public async Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
            {
                if (Interlocked.Increment(ref calls) > 1)
                {
                    await release.Task;

                    return StaleCount;
                }

                return queryable.Count();
            }

            public Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
                => Task.FromResult(queryable.ToList());
        }

        [Fact]
        public async Task DataGrid_SupersededLoad_PublishesNothing_WhenTheReplacementIsSynchronous()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new AdvisoryGatedExecutor();
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var component = RenderGrid(ctx, People(30).AsQueryable(), pageSize: 10);

            var parked = component.InvokeAsync(() => component.Instance.Reload());

            executor.Supported = false;

            var replacement = component.InvokeAsync(() => component.Instance.Reload());

            await component.InvokeAsync(() => { });

            Assert.Empty(await component.InvokeAsync(() => component.Instance.PagedView.Cast<object>().ToList()));

            executor.Release();

            await parked;
            await replacement;

            Assert.NotEqual(AdvisoryGatedExecutor.StaleCount, component.Instance.Count);
            Assert.Equal(30, component.Instance.Count);
            Assert.Equal(10, await component.InvokeAsync(() => component.Instance.PagedView.Count()));
        }

        sealed class IgnoresCancellationExecutor : IAsyncQueryExecutor
        {
            readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            readonly TaskCompletionSource hold = new(TaskCreationOptions.RunContinuationsAsynchronously);

            int calls;

            public bool Parked { get; private set; }

            public bool Held { get; private set; }

            public void Release() => release.TrySetResult();

            public void Continue() => hold.TrySetResult();

            public bool IsSupported<T>(IQueryable<T> queryable) => true;

            public async Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
            {
                var call = Interlocked.Increment(ref calls);

                if (call == 2)
                {
                    Parked = true;

                    await release.Task;

                    Parked = false;
                }
                else if (call == 3)
                {
                    Held = true;

                    await hold.Task;

                    Held = false;
                }

                return queryable.Count();
            }

            public Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
                => Task.FromResult(queryable.ToList());
        }

        class SearchableDropDownDataGrid<T> : RadzenDropDownDataGrid<T>
        {
            public Task Search() =>
                OnFilter(new Microsoft.AspNetCore.Components.ChangeEventArgs());
        }

        [Fact]
        public async Task DropDownDataGrid_ASupersededLoadDoesNotReplaceTheView_WhenTheExecutorIgnoresCancellation()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new IgnoresCancellationExecutor();
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var searchBox = ctx.JSInterop.Setup<string>("Radzen.getInputValue", _ => true);
            searchBox.SetResult(string.Empty);

            var people = People(30).AsQueryable();

            var component = ctx.RenderComponent<SearchableDropDownDataGrid<Person>>(p =>
            {
                p.Add(d => d.Data, people);
                p.Add(d => d.TextProperty, nameof(Person.Name));
                p.Add(d => d.AllowFiltering, true);
                p.Add(d => d.PageSize, 10);
            });

            string[] Names() => component.Instance.View.Cast<Person>().Select(x => x.Name).ToArray();

            var unsearched = Names();

            searchBox.SetResult("Person 1");

            var parked = component.InvokeAsync(() => component.Instance.Search());

            component.WaitForState(() => executor.Parked);

            searchBox.SetResult("Person 25");

            var replacement = component.InvokeAsync(() => component.Instance.Search());

            executor.Release();

            await parked;

            Assert.True(SpinWait.SpinUntil(() => executor.Held, TimeSpan.FromSeconds(5)));

            Assert.Equal(unsearched, Names());

            executor.Continue();

            await replacement;

            Assert.Equal(new[] { "Person 25" }, Names());
        }

        [Fact]
        public void DataGrid_GroupedVirtualization_UsesAsyncExecutor()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new RecordingExecutor();
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People(500).AsQueryable());
                p.Add(g => g.AllowVirtualization, true);
                p.Add(g => g.AllowGrouping, true);
                p.Add(g => g.Groups, new System.Collections.ObjectModel.ObservableCollection<GroupDescriptor>
                {
                    new GroupDescriptor { Property = nameof(Person.Team) }
                });
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                    builder.OpenComponent(2, typeof(RadzenDataGridColumn<Person>));
                    builder.AddAttribute(3, "Property", "Team");
                    builder.CloseComponent();
                });
            });

            Assert.True(executor.ToListCalls > 0, "ToListAsync should have been called by LoadGroups");
            Assert.Contains("Team 0", component.Markup);
        }

        [Fact]
        public void DataGrid_Grouped_UsesAsyncExecutor()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new RecordingExecutor();
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People(30).AsQueryable());
                p.Add(g => g.Groups, new System.Collections.ObjectModel.ObservableCollection<GroupDescriptor>
                {
                    new GroupDescriptor { Property = nameof(Person.Team) }
                });
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                    builder.OpenComponent(2, typeof(RadzenDataGridColumn<Person>));
                    builder.AddAttribute(3, "Property", "Team");
                    builder.CloseComponent();
                });
            });

            Assert.True(executor.ToListCalls > 0, "ToListAsync should have been called for the grouped view");
            Assert.Contains("Team 0", component.Markup);

            Assert.Equal(0, executor.CountCalls);
            Assert.Equal(30, component.Instance.Count);
        }

        [Fact]
        public void DataGrid_ObjectTyped_Sorted_UsesAsyncExecutor()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new RecordingExecutor();
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var component = ctx.RenderComponent<RadzenDataGrid<object>>(p =>
            {
                p.Add<IEnumerable<object>>(g => g.Data, People(30).Cast<object>().ToList().AsQueryable());
                p.Add(g => g.AllowPaging, true);
                p.Add(g => g.AllowSorting, true);
                p.Add(g => g.PageSize, 10);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<object>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.CloseComponent();
                    builder.OpenComponent(2, typeof(RadzenDataGridColumn<object>));
                    builder.AddAttribute(3, "Property", "Name");
                    builder.CloseComponent();
                });
            });

            Assert.True(executor.CountCalls > 0, "CountAsync should have been called");
            Assert.True(executor.ToListCalls > 0, "ToListAsync should have materialized the page");

            var countBefore = executor.CountCalls;
            var toListBefore = executor.ToListCalls;
            component.InvokeAsync(() => component.Instance.OrderBy("Id")).GetAwaiter().GetResult();

            Assert.True(executor.CountCalls > countBefore, "sorting should have counted through the async seam");
            Assert.True(executor.ToListCalls - toListBefore >= 2, "sorting should have run the type probe and the page");
            Assert.Equal(30, component.Instance.Count);
            Assert.Contains(">Person 1<", component.Markup);
            Assert.DoesNotContain(">Person 11<", component.Markup);
        }

        [Fact]
        public void DropDownDataGrid_UsesAsyncExecutor()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new RecordingExecutor();
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var component = ctx.RenderComponent<RadzenDropDownDataGrid<int>>(p =>
            {
                p.Add(d => d.Data, People(30).Cast<object>());
                p.Add(d => d.TextProperty, nameof(Person.Name));
                p.Add(d => d.ValueProperty, nameof(Person.Id));
            });

            Assert.True(executor.CountCalls > 0, "CountAsync should have been called by OnLoadData");
            Assert.True(executor.ToListCalls > 0, "ToListAsync should have been called by OnLoadData");
        }


        sealed class MarkerProvider : IQueryProvider
        {
            readonly IQueryProvider inner;

            public MarkerProvider(IQueryProvider inner) => this.inner = inner;

            public IQueryable CreateQuery(Expression expression) => inner.CreateQuery(expression);

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
                new MarkerQueryable<TElement>(inner.CreateQuery<TElement>(expression));

            public object? Execute(Expression expression) => inner.Execute(expression);

            public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);
        }

        sealed class MarkerQueryable<T> : IQueryable<T>
        {
            readonly IQueryable<T> inner;

            public MarkerQueryable(IQueryable<T> inner)
            {
                this.inner = inner;
                Provider = new MarkerProvider(inner.Provider);
            }

            public Type ElementType => inner.ElementType;
            public Expression Expression => inner.Expression;
            public IQueryProvider Provider { get; }
            public IEnumerator<T> GetEnumerator() => inner.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => inner.GetEnumerator();
        }

        sealed class ProviderSniffingExecutor : IAsyncQueryExecutor
        {
            public int CountCalls;
            public int ToListCalls;

            public bool IsSupported<T>(IQueryable<T> queryable) => queryable.Provider is MarkerProvider;

            public Task<int> CountAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
            {
                CountCalls++;
                return Task.FromResult(queryable.Count());
            }

            public Task<List<T>> ToListAsync<T>(IQueryable<T> queryable, CancellationToken cancellationToken = default)
            {
                ToListCalls++;
                return Task.FromResult(queryable.ToList());
            }
        }

        [Fact]
        public void DropDownBase_Virtualization_PreservesQueryProvider()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new ProviderSniffingExecutor();
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            ctx.RenderComponent<RadzenDropDown<int>>(p =>
            {
                p.Add(d => d.Data, new MarkerQueryable<Person>(People(500).AsQueryable()));
                p.Add(d => d.TextProperty, nameof(Person.Name));
                p.Add(d => d.ValueProperty, nameof(Person.Id));
                p.Add(d => d.AllowVirtualization, true);
            });

            Assert.True(executor.ToListCalls > 0,
                "DropDownBase.LoadItems should have reached the executor with the provider intact");
        }

        [Fact]
        public void DataGrid_PreservesQueryProvider()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new ProviderSniffingExecutor();
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add<IEnumerable<Person>>(g => g.Data, new MarkerQueryable<Person>(People(30).AsQueryable()));
                p.Add(g => g.AllowPaging, true);
                p.Add(g => g.PageSize, 10);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
            });

            Assert.True(executor.ToListCalls > 0, "the grid should have reached the executor");
            Assert.Equal(30, component.Instance.Count);
            Assert.Contains(">Person 1<", component.Markup);
            Assert.DoesNotContain(">Person 11<", component.Markup);
        }

        [Fact]
        public void DataList_UnpagedVirtualized_CountsWithoutMaterializingWholeQueryable()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new RecordingExecutor();
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var component = ctx.RenderComponent<RadzenDataList<Person>>(p =>
            {
                p.Add(d => d.Data, People(500).AsQueryable());
                p.Add(d => d.AllowVirtualization, true);
                p.Add<RenderFragment<Person>>(d => d.Template, item => builder => builder.AddContent(0, item.Name));
            });

            Assert.True(executor.CountCalls > 0, "the count should still have gone through the seam");
            Assert.True(executor.ToListCalls > 0, "the windows should have come through the seam too");
            Assert.True(executor.LargestToList < 500,
                $"nothing should have materialized the whole queryable; largest was {executor.LargestToList}");
            Assert.Equal(500, component.Instance.Count);
        }

[Fact]
        public void DataList_UnpagedNotVirtualized_MaterializesThroughTheSeam()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new RecordingExecutor();
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var component = ctx.RenderComponent<RadzenDataList<Person>>(p =>
            {
                p.Add(d => d.Data, People(120).AsQueryable());
                p.Add(d => d.AllowPaging, false);
                p.Add(d => d.AllowVirtualization, false);
                p.Add<RenderFragment<Person>>(d => d.Template, item => builder => builder.AddContent(0, item.Name));
            });

            Assert.True(executor.ToListCalls > 0, "the rows should have come through the seam");
            Assert.Equal(0, executor.CountCalls);
            Assert.Equal(120, component.Instance.Count);
        }

        [Fact]
        public void PivotDataGrid_UsesAsyncExecutor()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var executor = new RecordingExecutor();
            ctx.Services.AddSingleton<IAsyncQueryExecutor>(executor);

            var component = ctx.RenderComponent<RadzenPivotDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People(30).AsQueryable());
                p.Add<RenderFragment>(g => g.Rows, builder =>
                {
                    builder.OpenComponent<RadzenPivotRow<Person>>(0);
                    builder.AddAttribute(1, nameof(RadzenPivotRow<Person>.Property), nameof(Person.Name));
                    builder.AddAttribute(2, nameof(RadzenPivotRow<Person>.Title), "Name");
                    builder.CloseComponent();
                });
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent<RadzenPivotColumn<Person>>(0);
                    builder.AddAttribute(1, nameof(RadzenPivotColumn<Person>.Property), nameof(Person.Team));
                    builder.AddAttribute(2, nameof(RadzenPivotColumn<Person>.Title), "Team");
                    builder.CloseComponent();
                });
                p.Add<RenderFragment>(g => g.Aggregates, builder =>
                {
                    builder.OpenComponent<RadzenPivotAggregate<Person>>(0);
                    builder.AddAttribute(1, nameof(RadzenPivotAggregate<Person>.Property), nameof(Person.Id));
                    builder.AddAttribute(2, nameof(RadzenPivotAggregate<Person>.Title), "Id");
                    builder.AddAttribute(3, nameof(RadzenPivotAggregate<Person>.Aggregate), AggregateFunction.Sum);
                    builder.CloseComponent();
                });
            });

            Assert.True(executor.ToListCalls > 0, "the pivot grid should have materialized through the seam");
            Assert.Equal(30, component.Instance.Count);
        }

        [Fact]
        public void DataGrid_WorksWithoutAnyExecutorRegistered()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderGrid(ctx, People(23).AsQueryable(), pageSize: 10);

            Assert.Null(CoordinatorFor(component.Instance));
            Assert.Equal(23, component.Instance.Count);
            Assert.Contains(">Person 10<", component.Markup);
            Assert.DoesNotContain(">Person 11<", component.Markup);
        }
    }
}
