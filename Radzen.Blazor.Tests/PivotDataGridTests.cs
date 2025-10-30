using Bunit;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class PivotDataGridTests
    {
        private static readonly List<SalesData> SampleData = new()
        {
            new SalesData { Region = "North", Category = "Electronics", Product = "Laptop", Amount = 1000, Year = 2023 },
            new SalesData { Region = "North", Category = "Electronics", Product = "Laptop", Amount = 1500, Year = 2024 },
            new SalesData { Region = "South", Category = "Home", Product = "Vacuum", Amount = 500, Year = 2023 }
        };

        public class SalesData
        {
            public string Region { get; set; }
            public string Category { get; set; }
            public string Product { get; set; }
            public double Amount { get; set; }
            public int Year { get; set; }
        }

        [Fact]
        public void PivotDataGrid_Renders_CssClasses()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx);

            component.WaitForAssertion(() =>
            {
                Assert.Contains("rz-pivot-data-grid", component.Markup);
                Assert.Contains("rz-pivot-table", component.Markup);
            });
        }

        [Fact]
        public void PivotDataGrid_Renders_RowAndColumnHeaders()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx);

            component.WaitForAssertion(() =>
            {
                var table = component.Find(".rz-pivot-content .rz-pivot-table");
                var headers = table.GetElementsByClassName("rz-pivot-header-text").Select(h => h.TextContent.Trim()).ToList();
                var aggregateHeaders = table.GetElementsByClassName("rz-pivot-aggregate-header").Select(h => h.TextContent.Trim()).ToList();

                Assert.Contains("Region", headers);
                Assert.Contains("2023", headers);
                Assert.Contains("Sales", aggregateHeaders);
            });
        }

        [Fact]
        public void PivotDataGrid_AllowSorting_RendersSortableClass()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.AllowSorting, true);
            });

            component.WaitForAssertion(() =>
            {
                var sortableHeaders = component.FindAll(".rz-pivot-header-content.rz-sortable");
                Assert.NotEmpty(sortableHeaders);
            });
        }

        [Fact]
        public void PivotDataGrid_AllowFiltering_RendersFilterIcon()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.AllowFiltering, true);
            });

            component.WaitForAssertion(() =>
            {
                var filterIcons = component.FindAll(".rz-grid-filter-icon");
                Assert.NotEmpty(filterIcons);
            });
        }

        [Fact]
        public void PivotDataGrid_Renders_AggregateValues()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx);

            component.WaitForAssertion(() =>
            {
                Assert.Contains("1000", component.Markup);
                Assert.Contains("1500", component.Markup);
                Assert.Contains("500", component.Markup);
            });
        }

        [Fact]
        public void PivotDataGrid_DisallowFiltering_HidesFilterIcon()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.AllowFiltering, false);
            });

            component.WaitForAssertion(() =>
            {
                var filterIcons = component.FindAll(".rz-grid-filter-icon");
                Assert.Empty(filterIcons);
            });
        }

        [Fact]
        public void PivotDataGrid_DisallowSorting_HidesSortableClass()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.AllowSorting, false);
            });

            component.WaitForAssertion(() =>
            {
                var sortableHeaders = component.FindAll(".rz-pivot-header-content.rz-sortable");
                Assert.Empty(sortableHeaders);
            });
        }

        [Fact]
        public void PivotDataGrid_ShowColumnsTotals_RendersFooter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.ShowColumnsTotals, true);
            });

            component.WaitForAssertion(() =>
            {
                var footer = component.FindAll(".rz-pivot-footer");
                Assert.NotEmpty(footer);
            });
        }

        [Fact]
        public void PivotDataGrid_HideColumnsTotals_NoFooter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.ShowColumnsTotals, false);
            });

            component.WaitForAssertion(() =>
            {
                var footer = component.FindAll(".rz-pivot-footer");
                Assert.Empty(footer);
            });
        }

        [Fact]
        public void PivotDataGrid_Renders_DefaultEmptyText()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, new List<SalesData>());
                parameters.Add(p => p.AllowFieldsPicking, false);
            });

            component.WaitForAssertion(() =>
            {
                Assert.Contains("No records to display.", component.Markup);
            });
        }

        [Fact]
        public void PivotDataGrid_Renders_CustomEmptyText()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, new List<SalesData>());
                parameters.Add(p => p.EmptyText, "No data available");
                parameters.Add(p => p.AllowFieldsPicking, false);
            });

            component.WaitForAssertion(() =>
            {
                Assert.Contains("No data available", component.Markup);
            });
        }

        [Fact]
        public void PivotDataGrid_AllowPaging_RendersPager()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.AllowPaging, true);
                parameters.Add(p => p.PageSize, 2);
            });

            component.WaitForAssertion(() =>
            {
                Assert.Contains("rz-pager", component.Markup);
            });
        }

        [Fact]
        public void PivotDataGrid_DisallowPaging_HidesPager()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.AllowPaging, false);
            });

            component.WaitForAssertion(() =>
            {
                Assert.DoesNotContain("rz-pager", component.Markup);
            });
        }

        [Fact]
        public void PivotDataGrid_PagerPosition_Top()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.AllowPaging, true);
                parameters.Add(p => p.PagerPosition, PagerPosition.Top);
                parameters.Add(p => p.PageSize, 2);
            });

            component.WaitForAssertion(() =>
            {
                Assert.Contains("rz-pager", component.Markup);
            });
        }

        [Fact]
        public void PivotDataGrid_PagerPosition_TopAndBottom()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.AllowPaging, true);
                parameters.Add(p => p.PagerPosition, PagerPosition.TopAndBottom);
                parameters.Add(p => p.PageSize, 2);
            });

            component.WaitForAssertion(() =>
            {
                var pagers = component.FindAll(".rz-pager");
                Assert.True(pagers.Count >= 1); // Should have at least one pager
            });
        }

        [Fact]
        public void PivotDataGrid_Density_Compact()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.AllowPaging, true);
                parameters.Add(p => p.PageSize, 1); // Force pager to show with small page size
                parameters.Add(p => p.AllowFieldsPicking, false);
                parameters.Add(p => p.Density, Density.Compact);
            });

            component.WaitForAssertion(() =>
            {
                Assert.Contains("rz-density-compact", component.Markup);
            });
        }

        [Fact]
        public void PivotDataGrid_AllowAlternatingRows_True()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.AllowAlternatingRows, true);
            });

            component.WaitForAssertion(() =>
            {
                Assert.Contains("rz-grid-table-striped", component.Markup);
            });
        }

        [Fact]
        public void PivotDataGrid_AllowAlternatingRows_False()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.AllowAlternatingRows, false);
            });

            component.WaitForAssertion(() =>
            {
                Assert.DoesNotContain("rz-grid-table-striped", component.Markup);
            });
        }

        [Fact]
        public void PivotDataGrid_AllowFieldsPicking_ShowsPanel()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.AllowFieldsPicking, true);
            });

            component.WaitForAssertion(() =>
            {
                Assert.Contains("rz-panel", component.Markup);
            });
        }

        [Fact]
        public void PivotDataGrid_AllowFieldsPicking_False_HidesPanel()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.AllowFieldsPicking, false);
            });

            component.WaitForAssertion(() =>
            {
                Assert.DoesNotContain("rz-panel", component.Markup);
            });
        }

        [Fact]
        public void PivotDataGrid_Renders_AllowDrillDown()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.AllowDrillDown, true);
            });

            component.WaitForAssertion(() =>
            {
                // Should render pivot content
                Assert.Contains("rz-pivot-content", component.Markup);
            });
        }

        [Fact]
        public void PivotDataGrid_Renders_RowValues()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx);

            component.WaitForAssertion(() =>
            {
                var cells = component.FindAll(".rz-pivot-row-header");
                var cellTexts = cells.Select(c => c.TextContent.Trim()).ToList();
                
                Assert.Contains("North", cellTexts);
                Assert.Contains("South", cellTexts);
            });
        }

        [Fact]
        public void PivotDataGrid_Renders_MultipleRows()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, SampleData);

                parameters.Add<RenderFragment>(p => p.Rows, builder =>
                {
                    builder.OpenComponent<RadzenPivotRow<SalesData>>(0);
                    builder.AddAttribute(1, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Region));
                    builder.AddAttribute(2, nameof(RadzenPivotRow<SalesData>.Title), "Region");
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenPivotRow<SalesData>>(2);
                    builder.AddAttribute(3, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Category));
                    builder.AddAttribute(4, nameof(RadzenPivotRow<SalesData>.Title), "Category");
                    builder.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent<RadzenPivotColumn<SalesData>>(0);
                    builder.AddAttribute(1, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Year));
                    builder.AddAttribute(2, nameof(RadzenPivotColumn<SalesData>.Title), "Year");
                    builder.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Aggregates, builder =>
                {
                    builder.OpenComponent<RadzenPivotAggregate<SalesData>>(0);
                    builder.AddAttribute(1, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                    builder.AddAttribute(2, nameof(RadzenPivotAggregate<SalesData>.Title), "Sales");
                    builder.AddAttribute(3, nameof(RadzenPivotAggregate<SalesData>.Aggregate), AggregateFunction.Sum);
                    builder.CloseComponent();
                });
            });

            component.WaitForAssertion(() =>
            {
                var headers = component.FindAll(".rz-pivot-header-text").Select(h => h.TextContent.Trim()).ToList();
                Assert.Contains("Region", headers);
                Assert.Contains("Category", headers);
            });
        }

        [Fact]
        public void PivotDataGrid_Renders_MultipleAggregates()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, SampleData);

                parameters.Add<RenderFragment>(p => p.Rows, builder =>
                {
                    builder.OpenComponent<RadzenPivotRow<SalesData>>(0);
                    builder.AddAttribute(1, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Region));
                    builder.AddAttribute(2, nameof(RadzenPivotRow<SalesData>.Title), "Region");
                    builder.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent<RadzenPivotColumn<SalesData>>(0);
                    builder.AddAttribute(1, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Year));
                    builder.AddAttribute(2, nameof(RadzenPivotColumn<SalesData>.Title), "Year");
                    builder.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Aggregates, builder =>
                {
                    builder.OpenComponent<RadzenPivotAggregate<SalesData>>(0);
                    builder.AddAttribute(1, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                    builder.AddAttribute(2, nameof(RadzenPivotAggregate<SalesData>.Title), "Total Sales");
                    builder.AddAttribute(3, nameof(RadzenPivotAggregate<SalesData>.Aggregate), AggregateFunction.Sum);
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenPivotAggregate<SalesData>>(4);
                    builder.AddAttribute(5, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                    builder.AddAttribute(6, nameof(RadzenPivotAggregate<SalesData>.Title), "Count Sales");
                    builder.AddAttribute(7, nameof(RadzenPivotAggregate<SalesData>.Aggregate), AggregateFunction.Count);
                    builder.CloseComponent();
                });
            });

            component.WaitForAssertion(() =>
            {
                var aggregateHeaders = component.FindAll(".rz-pivot-aggregate-header").Select(h => h.TextContent.Trim()).ToList();
                Assert.Contains("Total Sales", aggregateHeaders);
                Assert.Contains("Count Sales", aggregateHeaders);
            });
        }

        [Fact]
        public void PivotDataGrid_Renders_AlternatingRowClasses()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderPivotDataGrid(ctx, parameters =>
            {
                parameters.Add(p => p.AllowAlternatingRows, true);
            });

            component.WaitForAssertion(() =>
            {
                Assert.Contains("rz-pivot-row-even", component.Markup);
                Assert.Contains("rz-pivot-row-odd", component.Markup);
            });
        }

        [Fact]
        public void PivotDataGrid_PageSize_DefaultsTo10()
        {
            var grid = new RadzenPivotDataGrid<SalesData>();
            Assert.Equal(10, grid.PageSize);
        }

        [Fact]
        public void PivotDataGrid_AllowSorting_DefaultsToTrue()
        {
            var grid = new RadzenPivotDataGrid<SalesData>();
            Assert.True(grid.AllowSorting);
        }

        [Fact]
        public void PivotDataGrid_AllowFiltering_DefaultsToTrue()
        {
            var grid = new RadzenPivotDataGrid<SalesData>();
            Assert.True(grid.AllowFiltering);
        }

        [Fact]
        public void PivotDataGrid_AllowAlternatingRows_DefaultsToTrue()
        {
            var grid = new RadzenPivotDataGrid<SalesData>();
            Assert.True(grid.AllowAlternatingRows);
        }

        [Fact]
        public void PivotDataGrid_AllowDrillDown_DefaultsToTrue()
        {
            var grid = new RadzenPivotDataGrid<SalesData>();
            Assert.True(grid.AllowDrillDown);
        }

        [Fact]
        public void PivotDataGrid_AllowFieldsPicking_DefaultsToTrue()
        {
            var grid = new RadzenPivotDataGrid<SalesData>();
            Assert.True(grid.AllowFieldsPicking);
        }

        private static IRenderedComponent<RadzenPivotDataGrid<SalesData>> RenderPivotDataGrid(TestContext ctx, Action<ComponentParameterCollectionBuilder<RadzenPivotDataGrid<SalesData>>>? configure = null)
        {
            return ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, SampleData);

                parameters.Add<RenderFragment>(p => p.Rows, builder =>
                {
                    builder.OpenComponent<RadzenPivotRow<SalesData>>(0);
                    builder.AddAttribute(1, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Region));
                    builder.AddAttribute(2, nameof(RadzenPivotRow<SalesData>.Title), "Region");
                    builder.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent<RadzenPivotColumn<SalesData>>(0);
                    builder.AddAttribute(1, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Year));
                    builder.AddAttribute(2, nameof(RadzenPivotColumn<SalesData>.Title), "Year");
                    builder.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Aggregates, builder =>
                {
                    builder.OpenComponent<RadzenPivotAggregate<SalesData>>(0);
                    builder.AddAttribute(1, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                    builder.AddAttribute(2, nameof(RadzenPivotAggregate<SalesData>.Title), "Sales");
                    builder.AddAttribute(3, nameof(RadzenPivotAggregate<SalesData>.Aggregate), AggregateFunction.Sum);
                    builder.CloseComponent();
                });

                configure?.Invoke(parameters);
            });
        }
    }
}

