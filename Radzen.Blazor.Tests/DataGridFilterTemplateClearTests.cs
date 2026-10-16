using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridFilterTemplateClearTests
    {
        public class Item
        {
            public int Id { get; set; }
            public DateTime? Date { get; set; }
        }

        public class Host : ComponentBase
        {
            public DateTime? From = new DateTime(2024, 1, 1);
            public DateTime? To = new DateTime(2024, 12, 31);
            public RadzenDataGrid<Item> Grid = default!;
            public List<List<FilterDescriptor>> Loads = new();
            public int InFlight;
            public int MaxInFlight;

            async Task Load(LoadDataArgs args)
            {
                var n = Interlocked.Increment(ref InFlight);
                MaxInFlight = Math.Max(MaxInFlight, n);
                Loads.Add(args.Filters?.ToList() ?? new List<FilterDescriptor>());
                await Task.Delay(20);
                Interlocked.Decrement(ref InFlight);
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<RadzenDataGrid<Item>>(0);
                builder.AddAttribute(1, "Data", new List<Item>());
                builder.AddAttribute(2, "Count", 0);
                builder.AddAttribute(3, "AllowFiltering", true);
                builder.AddAttribute(4, "LoadData", EventCallback.Factory.Create<LoadDataArgs>(this, Load));
                builder.AddAttribute(5, "Columns", (RenderFragment)(columns =>
                {
                    columns.OpenComponent<RadzenDataGridColumn<Item>>(0);
                    columns.AddAttribute(1, "Property", "Date");
                    columns.AddAttribute(2, "Type", typeof(DateTime?));
                    columns.AddAttribute(3, "FilterValue", From);
                    columns.AddAttribute(4, "FilterOperator", FilterOperator.GreaterThanOrEquals);
                    columns.AddAttribute(5, "SecondFilterValue", To);
                    columns.AddAttribute(6, "SecondFilterOperator", FilterOperator.LessThanOrEquals);
                    columns.AddAttribute(7, "LogicalFilterOperator", LogicalFilterOperator.And);
                    columns.AddAttribute(8, "FilterTemplate", (RenderFragment<RadzenDataGridColumn<Item>>)(c => template => template.AddContent(0, "template")));
                    columns.CloseComponent();
                }));
                builder.AddComponentReferenceCapture(6, r => Grid = (RadzenDataGrid<Item>)r);
                builder.CloseComponent();
            }

            public void Rerender() => StateHasChanged();
        }

        static TestContext CreateContext()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");
            return ctx;
        }

        static async Task<IRenderedComponent<Host>> RenderHost(TestContext ctx)
        {
            var cut = ctx.RenderComponent<Host>();
            await Task.Delay(100);
            cut.Instance.Loads.Clear();
            cut.Instance.MaxInFlight = 0;
            return cut;
        }

        [Fact]
        public async Task ClearFilters_RestoresDeclaredOperators_AndClearsTemplateValues()
        {
            using var ctx = CreateContext();
            var cut = await RenderHost(ctx);
            var host = cut.Instance;
            var column = host.Grid.ColumnsCollection.First();

            await cut.InvokeAsync(async () =>
            {
                column.ClearFilters();
                await host.Grid.FirstPage(true);
            });

            Assert.Equal(FilterOperator.GreaterThanOrEquals, column.GetFilterOperator());
            Assert.Equal(FilterOperator.LessThanOrEquals, column.GetSecondFilterOperator());
            Assert.Null(column.GetFilterValue());
            Assert.Null(column.GetSecondFilterValue());
            Assert.Empty(host.Loads.Last());
        }

        [Fact]
        public async Task Reset_NullingBoundValues_LoadsOnceWithoutFilter()
        {
            using var ctx = CreateContext();
            var cut = await RenderHost(ctx);
            var host = cut.Instance;

            await cut.InvokeAsync(async () =>
            {
                host.From = null;
                host.To = null;
                foreach (var column in host.Grid.ColumnsCollection)
                {
                    column.ClearFilters();
                }
                var reload = host.Grid.FirstPage(true);
                host.Rerender();
                await reload;
            });
            await Task.Delay(100);

            Assert.Equal(1, host.MaxInFlight);
            Assert.Single(host.Loads);
            Assert.Empty(host.Loads[0]);
        }

        [Fact]
        public async Task PickingValueAfterClear_UsesDeclaredOperator()
        {
            using var ctx = CreateContext();
            var cut = await RenderHost(ctx);
            var host = cut.Instance;

            await cut.InvokeAsync(async () =>
            {
                host.From = null;
                host.To = null;
                host.Grid.ColumnsCollection.First().ClearFilters();
                await host.Grid.FirstPage(true);
                host.Rerender();
            });
            await Task.Delay(100);
            host.Loads.Clear();

            await cut.InvokeAsync(() =>
            {
                host.From = new DateTime(2024, 3, 1);
                host.Rerender();
            });
            await Task.Delay(100);

            var filter = Assert.Single(host.Loads.Last());
            Assert.Equal(FilterOperator.GreaterThanOrEquals, filter.FilterOperator);
            Assert.Equal(new DateTime(2024, 3, 1), filter.FilterValue);
        }

        [Fact]
        public async Task ChangingBothBoundValues_ReloadsOnce()
        {
            using var ctx = CreateContext();
            var cut = await RenderHost(ctx);
            var host = cut.Instance;

            await cut.InvokeAsync(() =>
            {
                host.From = null;
                host.To = null;
                host.Rerender();
            });
            await Task.Delay(100);

            var column = host.Grid.ColumnsCollection.First();
            Assert.Equal(1, host.MaxInFlight);
            Assert.Single(host.Loads);
            Assert.Empty(host.Loads[0]);
            Assert.Null(column.GetFilterValue());
            Assert.Null(column.GetSecondFilterValue());
        }

        [Fact]
        public void ClearFilters_DoesNotRestoreDeclaredValuelessOperator()
        {
            using var ctx = CreateContext();
            var cut = ctx.RenderComponent<RadzenDataGrid<Item>>(parameters =>
            {
                parameters.Add(p => p.Data, new List<Item>());
                parameters.Add(p => p.AllowFiltering, true);
                parameters.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent<RadzenDataGridColumn<Item>>(0);
                    builder.AddAttribute(1, "Property", "Date");
                    builder.AddAttribute(2, "FilterOperator", FilterOperator.IsNotNull);
                    builder.CloseComponent();
                });
            });
            var column = cut.Instance.ColumnsCollection.First();

            column.ClearFilters();

            Assert.Equal(FilterOperator.Equals, column.GetFilterOperator());
        }

        [Fact]
        public async Task ValuelessOperatorRoundTrip_KeepsBoundValue()
        {
            using var ctx = CreateContext();
            var cut = await RenderHost(ctx);
            var column = cut.Instance.Grid.ColumnsCollection.First();

            column.SetFilterValue(null);
            column.SetFilterValue(null, false);
            column.SetFilterOperator(FilterOperator.IsNull);
            column.SetFilterOperator(FilterOperator.GreaterThanOrEquals);

            Assert.Equal(new DateTime(2024, 1, 1), column.GetFilterValue());
        }

        [Fact]
        public async Task LoadingClearedSettings_ClearsTemplateValues()
        {
            using var ctx = CreateContext();
            var cut = await RenderHost(ctx);
            var grid = cut.Instance.Grid;
            var column = grid.ColumnsCollection.First();
            var settings = new DataGridColumnSettings { Property = "Date" };

            grid.SetColumnFilterValueFromSettings(column, settings, true);
            grid.SetColumnFilterValueFromSettings(column, settings, false);

            Assert.Null(column.GetFilterValue());
            Assert.Null(column.GetSecondFilterValue());
        }
    }
}
