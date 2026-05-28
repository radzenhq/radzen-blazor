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

        private static readonly List<SalesData> AggregationData = new()
        {
            new SalesData { Region = "North", Category = "Electronics", Product = "Laptop",  Amount = 100,  Year = 2023 },
            new SalesData { Region = "North", Category = "Electronics", Product = "Tablet",  Amount = 200,  Year = 2023 },
            new SalesData { Region = "North", Category = "Electronics", Product = "Phone",   Amount = 50,   Year = 2024 },
            new SalesData { Region = "North", Category = "Furniture",   Product = "Desk",    Amount = 300,  Year = 2023 },
            new SalesData { Region = "South", Category = "Electronics", Product = "Monitor", Amount = 1000, Year = 2024 },
            new SalesData { Region = "South", Category = "Furniture",   Product = "Chair",   Amount = 400,  Year = 2024 },
        };

        [Fact]
        public void PivotDataGrid_Sum_ComputesCorrectCellValues()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderAggregationGrid(ctx, AggregateFunction.Sum);

            component.WaitForAssertion(() =>
            {
                var northValues = GetValueCellTexts(component, "North");
                var southValues = GetValueCellTexts(component, "South");

                Assert.Equal(new[] { "600", "50" }, northValues);
                Assert.Equal(new[] { "", "1400" }, southValues);
            });
        }

        [Fact]
        public void PivotDataGrid_Count_ComputesCorrectCellValues()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderAggregationGrid(ctx, AggregateFunction.Count);

            component.WaitForAssertion(() =>
            {
                var northValues = GetValueCellTexts(component, "North");
                var southValues = GetValueCellTexts(component, "South");

                Assert.Equal(new[] { "3", "1" }, northValues);
                Assert.Equal(new[] { "", "2" }, southValues);
            });
        }

        [Fact]
        public void PivotDataGrid_Average_ComputesCorrectCellValues()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderAggregationGrid(ctx, AggregateFunction.Average);

            component.WaitForAssertion(() =>
            {
                var northValues = GetValueCellTexts(component, "North");
                var southValues = GetValueCellTexts(component, "South");

                Assert.Equal(new[] { "200", "50" }, northValues);
                Assert.Equal(new[] { "", "700" }, southValues);
            });
        }

        [Fact]
        public void PivotDataGrid_Min_ComputesCorrectCellValues()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderAggregationGrid(ctx, AggregateFunction.Min);

            component.WaitForAssertion(() =>
            {
                var northValues = GetValueCellTexts(component, "North");
                var southValues = GetValueCellTexts(component, "South");

                Assert.Equal(new[] { "100", "50" }, northValues);
                Assert.Equal(new[] { "", "400" }, southValues);
            });
        }

        [Fact]
        public void PivotDataGrid_Max_ComputesCorrectCellValues()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderAggregationGrid(ctx, AggregateFunction.Max);

            component.WaitForAssertion(() =>
            {
                var northValues = GetValueCellTexts(component, "North");
                var southValues = GetValueCellTexts(component, "South");

                Assert.Equal(new[] { "300", "50" }, northValues);
                Assert.Equal(new[] { "", "1000" }, southValues);
            });
        }

        [Fact]
        public void PivotDataGrid_RowTotals_AreCorrect()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderAggregationGrid(ctx, AggregateFunction.Sum, parameters =>
            {
                parameters.Add(p => p.ShowRowsTotals, true);
            });

            component.WaitForAssertion(() =>
            {
                Assert.Equal(new[] { "650" }, GetTotalCellTexts(component, "North"));
                Assert.Equal(new[] { "1400" }, GetTotalCellTexts(component, "South"));
            });
        }

        [Fact]
        public void PivotDataGrid_ColumnTotals_AreCorrect()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderAggregationGrid(ctx, AggregateFunction.Sum, parameters =>
            {
                parameters.Add(p => p.ShowColumnsTotals, true);
            });

            component.WaitForAssertion(() =>
            {
                var totals = component.FindAll("tfoot.rz-pivot-footer td.rz-pivot-footer-value")
                    .Select(c => c.TextContent.Trim()).ToList();

                Assert.Equal(new[] { "600", "1450" }, totals);
            });
        }

        [Fact]
        public void PivotDataGrid_GrandTotal_IsCorrect()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderAggregationGrid(ctx, AggregateFunction.Sum, parameters =>
            {
                parameters.Add(p => p.ShowColumnsTotals, true);
                parameters.Add(p => p.ShowRowsTotals, true);
            });

            component.WaitForAssertion(() =>
            {
                var grand = component.FindAll("tfoot.rz-pivot-footer td.rz-pivot-footer-total")
                    .Select(c => c.TextContent.Trim()).ToList();

                Assert.Equal(new[] { "2050" }, grand);
            });
        }

        [Fact]
        public void PivotDataGrid_MultiLevelRows_RenderCorrectValues()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, AggregationData);
                parameters.Add(p => p.AllowDrillDown, false);
                parameters.Add(p => p.AllowFieldsPicking, false);

                parameters.Add<RenderFragment>(p => p.Rows, b =>
                {
                    b.OpenComponent<RadzenPivotRow<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Region));
                    b.AddAttribute(2, nameof(RadzenPivotRow<SalesData>.Title), "Region");
                    b.CloseComponent();
                    b.OpenComponent<RadzenPivotRow<SalesData>>(3);
                    b.AddAttribute(4, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Category));
                    b.AddAttribute(5, nameof(RadzenPivotRow<SalesData>.Title), "Category");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenPivotColumn<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Year));
                    b.AddAttribute(2, nameof(RadzenPivotColumn<SalesData>.Title), "Year");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Aggregates, b =>
                {
                    b.OpenComponent<RadzenPivotAggregate<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                    b.AddAttribute(2, nameof(RadzenPivotAggregate<SalesData>.Title), "Amount");
                    b.AddAttribute(3, nameof(RadzenPivotAggregate<SalesData>.Aggregate), AggregateFunction.Sum);
                    b.CloseComponent();
                });
            });

            component.WaitForAssertion(() =>
            {
                var rows = component.FindAll("tbody.rz-pivot-body tr.rz-pivot-row");
                Assert.Equal(4, rows.Count);

                Assert.Equal(new[] { "300", "50" }, ValuesOf(rows[0]));
                Assert.Equal(new[] { "300", "" }, ValuesOf(rows[1]));
                Assert.Equal(new[] { "", "1000" }, ValuesOf(rows[2]));
                Assert.Equal(new[] { "", "400" }, ValuesOf(rows[3]));
            });
        }

        [Fact]
        public void PivotDataGrid_MultiLevelColumns_RenderCorrectValuesAndColspan()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, AggregationData);
                parameters.Add(p => p.AllowDrillDown, false);
                parameters.Add(p => p.AllowFieldsPicking, false);

                parameters.Add<RenderFragment>(p => p.Rows, b =>
                {
                    b.OpenComponent<RadzenPivotRow<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Region));
                    b.AddAttribute(2, nameof(RadzenPivotRow<SalesData>.Title), "Region");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenPivotColumn<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Category));
                    b.AddAttribute(2, nameof(RadzenPivotColumn<SalesData>.Title), "Category");
                    b.CloseComponent();
                    b.OpenComponent<RadzenPivotColumn<SalesData>>(3);
                    b.AddAttribute(4, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Year));
                    b.AddAttribute(5, nameof(RadzenPivotColumn<SalesData>.Title), "Year");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Aggregates, b =>
                {
                    b.OpenComponent<RadzenPivotAggregate<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                    b.AddAttribute(2, nameof(RadzenPivotAggregate<SalesData>.Title), "Amount");
                    b.AddAttribute(3, nameof(RadzenPivotAggregate<SalesData>.Aggregate), AggregateFunction.Sum);
                    b.CloseComponent();
                });
            });

            component.WaitForAssertion(() =>
            {
                Assert.Equal(new[] { "300", "50", "300", "" }, GetValueCellTexts(component, "North"));
                Assert.Equal(new[] { "", "1000", "", "400" }, GetValueCellTexts(component, "South"));

                var categoryHeaders = component.FindAll(".rz-pivot-content thead .rz-pivot-column-header")
                    .Where(h => h.TextContent.Trim() is "Electronics" or "Furniture")
                    .ToList();
                Assert.Equal(2, categoryHeaders.Count);
                Assert.All(categoryHeaders, h => Assert.Equal("2", h.GetAttribute("colspan")));
            });
        }

        [Fact]
        public void PivotDataGrid_Filter_ReducesRenderedRows()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, AggregationData);
                parameters.Add(p => p.AllowDrillDown, false);
                parameters.Add(p => p.AllowFieldsPicking, false);

                parameters.Add<RenderFragment>(p => p.Rows, b =>
                {
                    b.OpenComponent<RadzenPivotRow<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Region));
                    b.AddAttribute(2, nameof(RadzenPivotRow<SalesData>.Title), "Region");
                    b.AddAttribute(3, nameof(RadzenPivotRow<SalesData>.FilterValue), "North");
                    b.AddAttribute(4, nameof(RadzenPivotRow<SalesData>.FilterOperator), FilterOperator.Equals);
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenPivotColumn<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Year));
                    b.AddAttribute(2, nameof(RadzenPivotColumn<SalesData>.Title), "Year");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Aggregates, b =>
                {
                    b.OpenComponent<RadzenPivotAggregate<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                    b.AddAttribute(2, nameof(RadzenPivotAggregate<SalesData>.Title), "Amount");
                    b.AddAttribute(3, nameof(RadzenPivotAggregate<SalesData>.Aggregate), AggregateFunction.Sum);
                    b.CloseComponent();
                });
            });

            component.WaitForAssertion(() =>
            {
                var rowHeaderTexts = component.FindAll("tbody.rz-pivot-body td.rz-pivot-row-header")
                    .Select(c => c.TextContent.Trim()).ToList();

                Assert.Contains("North", rowHeaderTexts);
                Assert.DoesNotContain("South", rowHeaderTexts);
            });
        }

        [Fact]
        public void PivotDataGrid_Sort_OrdersRows()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            IRenderedComponent<RadzenPivotDataGrid<SalesData>> Render(SortOrder order)
            {
                return ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
                {
                    parameters.Add(p => p.Data, AggregationData);
                    parameters.Add(p => p.AllowDrillDown, false);
                    parameters.Add(p => p.AllowFieldsPicking, false);

                    parameters.Add<RenderFragment>(p => p.Rows, b =>
                    {
                        b.OpenComponent<RadzenPivotRow<SalesData>>(0);
                        b.AddAttribute(1, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Region));
                        b.AddAttribute(2, nameof(RadzenPivotRow<SalesData>.Title), "Region");
                        b.AddAttribute(3, nameof(RadzenPivotRow<SalesData>.SortOrder), (SortOrder?)order);
                        b.CloseComponent();
                    });

                    parameters.Add<RenderFragment>(p => p.Columns, b =>
                    {
                        b.OpenComponent<RadzenPivotColumn<SalesData>>(0);
                        b.AddAttribute(1, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Year));
                        b.AddAttribute(2, nameof(RadzenPivotColumn<SalesData>.Title), "Year");
                        b.CloseComponent();
                    });

                    parameters.Add<RenderFragment>(p => p.Aggregates, b =>
                    {
                        b.OpenComponent<RadzenPivotAggregate<SalesData>>(0);
                        b.AddAttribute(1, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                        b.AddAttribute(2, nameof(RadzenPivotAggregate<SalesData>.Title), "Amount");
                        b.AddAttribute(3, nameof(RadzenPivotAggregate<SalesData>.Aggregate), AggregateFunction.Sum);
                        b.CloseComponent();
                    });
                });
            }

            var asc = Render(SortOrder.Ascending);
            asc.WaitForAssertion(() =>
            {
                var labels = asc.FindAll("tbody.rz-pivot-body td.rz-pivot-row-header")
                    .Select(c => c.TextContent.Trim()).ToList();
                Assert.Equal(new[] { "North", "South" }, labels);
            });

            var desc = Render(SortOrder.Descending);
            desc.WaitForAssertion(() =>
            {
                var labels = desc.FindAll("tbody.rz-pivot-body td.rz-pivot-row-header")
                    .Select(c => c.TextContent.Trim()).ToList();
                Assert.Equal(new[] { "South", "North" }, labels);
            });
        }

        [Fact]
        public void PivotDataGrid_SortByAggregate_OrdersRowsByAggregatedValue()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, AggregationData);
                parameters.Add(p => p.AllowDrillDown, false);
                parameters.Add(p => p.AllowFieldsPicking, false);

                parameters.Add<RenderFragment>(p => p.Rows, b =>
                {
                    b.OpenComponent<RadzenPivotRow<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Region));
                    b.AddAttribute(2, nameof(RadzenPivotRow<SalesData>.Title), "Region");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenPivotColumn<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Year));
                    b.AddAttribute(2, nameof(RadzenPivotColumn<SalesData>.Title), "Year");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Aggregates, b =>
                {
                    b.OpenComponent<RadzenPivotAggregate<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                    b.AddAttribute(2, nameof(RadzenPivotAggregate<SalesData>.Title), "Amount");
                    b.AddAttribute(3, nameof(RadzenPivotAggregate<SalesData>.Aggregate), AggregateFunction.Sum);
                    b.AddAttribute(4, nameof(RadzenPivotAggregate<SalesData>.SortOrder), (SortOrder?)SortOrder.Descending);
                    b.CloseComponent();
                });
            });

            component.WaitForAssertion(() =>
            {
                var labels = component.FindAll("tbody.rz-pivot-body td.rz-pivot-row-header")
                    .Select(c => c.TextContent.Trim()).ToList();

                Assert.Equal(new[] { "South", "North" }, labels);
            });
        }

        [Fact]
        public async System.Threading.Tasks.Task PivotDataGrid_ToggleRowDrillDown_ExpandsHierarchy()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, AggregationData);
                parameters.Add(p => p.AllowDrillDown, true);
                parameters.Add(p => p.AllowFieldsPicking, false);

                parameters.Add<RenderFragment>(p => p.Rows, b =>
                {
                    b.OpenComponent<RadzenPivotRow<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Region));
                    b.AddAttribute(2, nameof(RadzenPivotRow<SalesData>.Title), "Region");
                    b.CloseComponent();
                    b.OpenComponent<RadzenPivotRow<SalesData>>(3);
                    b.AddAttribute(4, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Category));
                    b.AddAttribute(5, nameof(RadzenPivotRow<SalesData>.Title), "Category");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenPivotColumn<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Year));
                    b.AddAttribute(2, nameof(RadzenPivotColumn<SalesData>.Title), "Year");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Aggregates, b =>
                {
                    b.OpenComponent<RadzenPivotAggregate<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                    b.AddAttribute(2, nameof(RadzenPivotAggregate<SalesData>.Title), "Amount");
                    b.AddAttribute(3, nameof(RadzenPivotAggregate<SalesData>.Aggregate), AggregateFunction.Sum);
                    b.CloseComponent();
                });
            });

            Assert.Equal(2, component.FindAll("tbody.rz-pivot-body tr.rz-pivot-row").Count);

            await component.InvokeAsync(() => component.Instance.ToggleRowDrillDown("North"));

            component.WaitForAssertion(() =>
            {
                var labels = component.FindAll("tbody.rz-pivot-body td.rz-pivot-row-header")
                    .Select(c => c.TextContent.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();

                Assert.Contains("Electronics", labels);
                Assert.Contains("Furniture", labels);
                Assert.Equal(3, component.FindAll("tbody.rz-pivot-body tr.rz-pivot-row").Count);
            });
        }

        [Fact]
        public void PivotDataGrid_CollapsedGroup_ShowsAggregatedValueAcrossDescendants()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, AggregationData);
                parameters.Add(p => p.AllowDrillDown, true);
                parameters.Add(p => p.AllowFieldsPicking, false);

                parameters.Add<RenderFragment>(p => p.Rows, b =>
                {
                    b.OpenComponent<RadzenPivotRow<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Region));
                    b.AddAttribute(2, nameof(RadzenPivotRow<SalesData>.Title), "Region");
                    b.CloseComponent();
                    b.OpenComponent<RadzenPivotRow<SalesData>>(3);
                    b.AddAttribute(4, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Category));
                    b.AddAttribute(5, nameof(RadzenPivotRow<SalesData>.Title), "Category");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenPivotColumn<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Year));
                    b.AddAttribute(2, nameof(RadzenPivotColumn<SalesData>.Title), "Year");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Aggregates, b =>
                {
                    b.OpenComponent<RadzenPivotAggregate<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                    b.AddAttribute(2, nameof(RadzenPivotAggregate<SalesData>.Title), "Amount");
                    b.AddAttribute(3, nameof(RadzenPivotAggregate<SalesData>.Aggregate), AggregateFunction.Sum);
                    b.CloseComponent();
                });
            });

            component.WaitForAssertion(() =>
            {
                Assert.Equal(new[] { "600", "50" }, GetValueCellTexts(component, "North"));
                Assert.Equal(new[] { "", "1400" }, GetValueCellTexts(component, "South"));
            });
        }

        [Fact]
        public void PivotDataGrid_DictionaryData_AggregatesValues()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var data = new List<IDictionary<string, object>>
            {
                new Dictionary<string, object> { ["Region"] = "North", ["Year"] = 2023, ["Amount"] = 100.0 },
                new Dictionary<string, object> { ["Region"] = "North", ["Year"] = 2023, ["Amount"] = 200.0 },
                new Dictionary<string, object> { ["Region"] = "South", ["Year"] = 2024, ["Amount"] = 300.0 },
            };

            var regionExpr = PropertyAccess.GetDynamicPropertyExpression("Region", typeof(string));
            var yearExpr = PropertyAccess.GetDynamicPropertyExpression("Year", typeof(int));
            var amountExpr = PropertyAccess.GetDynamicPropertyExpression("Amount", typeof(double));

            var component = ctx.RenderComponent<RadzenPivotDataGrid<IDictionary<string, object>>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.AllowDrillDown, false);
                parameters.Add(p => p.AllowFieldsPicking, false);

                parameters.Add<RenderFragment>(p => p.Rows, b =>
                {
                    b.OpenComponent<RadzenPivotRow<IDictionary<string, object>>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotRow<IDictionary<string, object>>.Property), regionExpr);
                    b.AddAttribute(2, nameof(RadzenPivotRow<IDictionary<string, object>>.Title), "Region");
                    b.AddAttribute(3, nameof(RadzenPivotRow<IDictionary<string, object>>.Type), typeof(string));
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenPivotColumn<IDictionary<string, object>>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotColumn<IDictionary<string, object>>.Property), yearExpr);
                    b.AddAttribute(2, nameof(RadzenPivotColumn<IDictionary<string, object>>.Title), "Year");
                    b.AddAttribute(3, nameof(RadzenPivotColumn<IDictionary<string, object>>.Type), typeof(int));
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Aggregates, b =>
                {
                    b.OpenComponent<RadzenPivotAggregate<IDictionary<string, object>>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotAggregate<IDictionary<string, object>>.Property), amountExpr);
                    b.AddAttribute(2, nameof(RadzenPivotAggregate<IDictionary<string, object>>.Title), "Amount");
                    b.AddAttribute(3, nameof(RadzenPivotAggregate<IDictionary<string, object>>.Aggregate), AggregateFunction.Count);
                    b.AddAttribute(4, nameof(RadzenPivotAggregate<IDictionary<string, object>>.Type), typeof(double));
                    b.CloseComponent();
                });
            });

            component.WaitForAssertion(() =>
            {
                var rows = component.FindAll("tbody.rz-pivot-body tr.rz-pivot-row");
                Assert.Equal(2, rows.Count);

                Assert.Equal(new[] { "2", "" }, GetValueCellTexts(component, "North"));
                Assert.Equal(new[] { "", "1" }, GetValueCellTexts(component, "South"));
            });
        }

        [Fact]
        public void PivotDataGrid_FormatString_AppliedToValueAndTotalCells()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, AggregationData);
                parameters.Add(p => p.AllowDrillDown, false);
                parameters.Add(p => p.AllowFieldsPicking, false);
                parameters.Add(p => p.ShowRowsTotals, true);
                parameters.Add(p => p.ShowColumnsTotals, true);

                parameters.Add<RenderFragment>(p => p.Rows, b =>
                {
                    b.OpenComponent<RadzenPivotRow<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Region));
                    b.AddAttribute(2, nameof(RadzenPivotRow<SalesData>.Title), "Region");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenPivotColumn<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Year));
                    b.AddAttribute(2, nameof(RadzenPivotColumn<SalesData>.Title), "Year");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Aggregates, b =>
                {
                    b.OpenComponent<RadzenPivotAggregate<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                    b.AddAttribute(2, nameof(RadzenPivotAggregate<SalesData>.Title), "Amount");
                    b.AddAttribute(3, nameof(RadzenPivotAggregate<SalesData>.Aggregate), AggregateFunction.Sum);
                    b.AddAttribute(4, nameof(RadzenPivotAggregate<SalesData>.FormatString), "${0:0}");
                    b.AddAttribute(5, nameof(RadzenPivotAggregate<SalesData>.FormatProvider), (System.IFormatProvider)System.Globalization.CultureInfo.InvariantCulture);
                    b.CloseComponent();
                });
            });

            component.WaitForAssertion(() =>
            {
                Assert.Equal(new[] { "$600", "$50" }, GetValueCellTexts(component, "North"));
                Assert.Equal(new[] { "$650" }, GetTotalCellTexts(component, "North"));

                var footerVals = component.FindAll("tfoot.rz-pivot-footer td.rz-pivot-footer-value")
                    .Select(c => c.TextContent.Trim()).ToList();
                Assert.Equal(new[] { "$600", "$1450" }, footerVals);
            });
        }

        [Fact]
        public void PivotDataGrid_NullPropertyValues_HandledGracefully()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var data = new List<SalesData>
            {
                new() { Region = "North", Category = "Electronics", Product = "Laptop", Amount = 100, Year = 2023 },
                new() { Region = null,    Category = "Electronics", Product = "Tablet", Amount = 200, Year = 2023 },
                new() { Region = "South", Category = "Furniture",   Product = "Chair",  Amount = 400, Year = 2024 },
            };

            var component = ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.AllowDrillDown, false);
                parameters.Add(p => p.AllowFieldsPicking, false);

                parameters.Add<RenderFragment>(p => p.Rows, b =>
                {
                    b.OpenComponent<RadzenPivotRow<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Region));
                    b.AddAttribute(2, nameof(RadzenPivotRow<SalesData>.Title), "Region");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenPivotColumn<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Year));
                    b.AddAttribute(2, nameof(RadzenPivotColumn<SalesData>.Title), "Year");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Aggregates, b =>
                {
                    b.OpenComponent<RadzenPivotAggregate<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                    b.AddAttribute(2, nameof(RadzenPivotAggregate<SalesData>.Title), "Amount");
                    b.AddAttribute(3, nameof(RadzenPivotAggregate<SalesData>.Aggregate), AggregateFunction.Sum);
                    b.CloseComponent();
                });
            });

            component.WaitForAssertion(() =>
            {
                var rows = component.FindAll("tbody.rz-pivot-body tr.rz-pivot-row");
                Assert.Equal(3, rows.Count);
                Assert.Contains("100", GetValueCellTexts(component, "North"));
                Assert.Contains("400", GetValueCellTexts(component, "South"));
            });
        }

        [Fact]
        public void PivotDataGrid_IQueryableData_ComputesCorrectCellValues()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            IQueryable<SalesData> data = AggregationData.AsQueryable();
            var component = RenderSumGrid(ctx, data);

            component.WaitForAssertion(() =>
            {
                Assert.Equal(new[] { "600", "50" }, GetValueCellTexts(component, "North"));
                Assert.Equal(new[] { "", "1400" }, GetValueCellTexts(component, "South"));
            });
        }

        [Fact]
        public void PivotDataGrid_IQueryableData_TotalsAndGrandTotalAreCorrect()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = RenderSumGrid(ctx, AggregationData.AsQueryable(), parameters =>
            {
                parameters.Add(p => p.ShowRowsTotals, true);
                parameters.Add(p => p.ShowColumnsTotals, true);
            });

            component.WaitForAssertion(() =>
            {
                Assert.Equal(new[] { "650" }, GetTotalCellTexts(component, "North"));
                Assert.Equal(new[] { "1400" }, GetTotalCellTexts(component, "South"));

                var footerVals = component.FindAll("tfoot.rz-pivot-footer td.rz-pivot-footer-value")
                    .Select(c => c.TextContent.Trim()).ToList();
                Assert.Equal(new[] { "600", "1450" }, footerVals);

                var grand = component.FindAll("tfoot.rz-pivot-footer td.rz-pivot-footer-total")
                    .Select(c => c.TextContent.Trim()).ToList();
                Assert.Equal(new[] { "2050" }, grand);
            });
        }

        [Fact]
        public void PivotDataGrid_IQueryableData_FilterIsApplied()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, AggregationData.AsQueryable());
                parameters.Add(p => p.AllowDrillDown, false);
                parameters.Add(p => p.AllowFieldsPicking, false);

                parameters.Add<RenderFragment>(p => p.Rows, b =>
                {
                    b.OpenComponent<RadzenPivotRow<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Region));
                    b.AddAttribute(2, nameof(RadzenPivotRow<SalesData>.Title), "Region");
                    b.AddAttribute(3, nameof(RadzenPivotRow<SalesData>.FilterValue), "North");
                    b.AddAttribute(4, nameof(RadzenPivotRow<SalesData>.FilterOperator), FilterOperator.Equals);
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenPivotColumn<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Year));
                    b.AddAttribute(2, nameof(RadzenPivotColumn<SalesData>.Title), "Year");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Aggregates, b =>
                {
                    b.OpenComponent<RadzenPivotAggregate<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                    b.AddAttribute(2, nameof(RadzenPivotAggregate<SalesData>.Title), "Amount");
                    b.AddAttribute(3, nameof(RadzenPivotAggregate<SalesData>.Aggregate), AggregateFunction.Sum);
                    b.CloseComponent();
                });
            });

            component.WaitForAssertion(() =>
            {
                var rowHeaderTexts = component.FindAll("tbody.rz-pivot-body td.rz-pivot-row-header")
                    .Select(c => c.TextContent.Trim()).ToList();

                Assert.Contains("North", rowHeaderTexts);
                Assert.DoesNotContain("South", rowHeaderTexts);
            });
        }

        [Fact]
        public void PivotDataGrid_IQueryableData_SortOrdersRows()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, AggregationData.AsQueryable());
                parameters.Add(p => p.AllowDrillDown, false);
                parameters.Add(p => p.AllowFieldsPicking, false);

                parameters.Add<RenderFragment>(p => p.Rows, b =>
                {
                    b.OpenComponent<RadzenPivotRow<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Region));
                    b.AddAttribute(2, nameof(RadzenPivotRow<SalesData>.Title), "Region");
                    b.AddAttribute(3, nameof(RadzenPivotRow<SalesData>.SortOrder), (SortOrder?)SortOrder.Descending);
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenPivotColumn<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Year));
                    b.AddAttribute(2, nameof(RadzenPivotColumn<SalesData>.Title), "Year");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Aggregates, b =>
                {
                    b.OpenComponent<RadzenPivotAggregate<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                    b.AddAttribute(2, nameof(RadzenPivotAggregate<SalesData>.Title), "Amount");
                    b.AddAttribute(3, nameof(RadzenPivotAggregate<SalesData>.Aggregate), AggregateFunction.Sum);
                    b.CloseComponent();
                });
            });

            component.WaitForAssertion(() =>
            {
                var labels = component.FindAll("tbody.rz-pivot-body td.rz-pivot-row-header")
                    .Select(c => c.TextContent.Trim()).ToList();
                Assert.Equal(new[] { "South", "North" }, labels);
            });
        }

        [Fact]
        public void PivotDataGrid_IQueryableData_SourceEnumeratedLazilyAndNotPerCell()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            // Same structure, different number of distinct column values => different cell counts.
            var fewColumns = new CountingEnumerable<SalesData>(new List<SalesData>
            {
                new() { Region = "North", Category = "Electronics", Product = "A", Amount = 100, Year = 2023 },
                new() { Region = "North", Category = "Electronics", Product = "B", Amount = 200, Year = 2024 },
                new() { Region = "South", Category = "Furniture",   Product = "C", Amount = 300, Year = 2023 },
            });

            var manyColumns = new CountingEnumerable<SalesData>(new List<SalesData>
            {
                new() { Region = "North", Category = "Electronics", Product = "A", Amount = 100, Year = 2021 },
                new() { Region = "North", Category = "Electronics", Product = "B", Amount = 200, Year = 2022 },
                new() { Region = "North", Category = "Electronics", Product = "C", Amount = 200, Year = 2023 },
                new() { Region = "North", Category = "Electronics", Product = "D", Amount = 200, Year = 2024 },
                new() { Region = "South", Category = "Furniture",   Product = "E", Amount = 300, Year = 2025 },
            });

            var fewGrid = RenderSumGrid(ctx, fewColumns.AsQueryable());
            var manyGrid = RenderSumGrid(ctx, manyColumns.AsQueryable());

            fewGrid.WaitForAssertion(() => Assert.NotEmpty(fewGrid.FindAll("td.rz-pivot-value-cell")));
            manyGrid.WaitForAssertion(() => Assert.NotEmpty(manyGrid.FindAll("td.rz-pivot-value-cell")));

            // The many-columns grid renders more value cells than the few-columns grid...
            Assert.True(manyGrid.FindAll("td.rz-pivot-value-cell").Count > fewGrid.FindAll("td.rz-pivot-value-cell").Count);

            // ...yet the IQueryable source is enumerated the same (small) number of times,
            // proving the page is materialized once and cells are not computed by re-querying the source.
            Assert.True(fewColumns.EnumerationCount > 0);
            Assert.Equal(fewColumns.EnumerationCount, manyColumns.EnumerationCount);
        }

        private sealed class CountingEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> source;
            public int EnumerationCount { get; private set; }

            public CountingEnumerable(IEnumerable<T> source) => this.source = source;

            public IEnumerator<T> GetEnumerator()
            {
                EnumerationCount++;
                return source.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static IRenderedComponent<RadzenPivotDataGrid<SalesData>> RenderSumGrid(
            TestContext ctx,
            IEnumerable<SalesData> data,
            Action<ComponentParameterCollectionBuilder<RadzenPivotDataGrid<SalesData>>> configure = null)
        {
            return ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.AllowDrillDown, false);
                parameters.Add(p => p.AllowFieldsPicking, false);

                parameters.Add<RenderFragment>(p => p.Rows, b =>
                {
                    b.OpenComponent<RadzenPivotRow<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Region));
                    b.AddAttribute(2, nameof(RadzenPivotRow<SalesData>.Title), "Region");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenPivotColumn<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Year));
                    b.AddAttribute(2, nameof(RadzenPivotColumn<SalesData>.Title), "Year");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Aggregates, b =>
                {
                    b.OpenComponent<RadzenPivotAggregate<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                    b.AddAttribute(2, nameof(RadzenPivotAggregate<SalesData>.Title), "Amount");
                    b.AddAttribute(3, nameof(RadzenPivotAggregate<SalesData>.Aggregate), AggregateFunction.Sum);
                    b.CloseComponent();
                });

                configure?.Invoke(parameters);
            });
        }

        private static IRenderedComponent<RadzenPivotDataGrid<SalesData>> RenderAggregationGrid(
            TestContext ctx,
            AggregateFunction aggregate,
            Action<ComponentParameterCollectionBuilder<RadzenPivotDataGrid<SalesData>>> configure = null)
        {
            return ctx.RenderComponent<RadzenPivotDataGrid<SalesData>>(parameters =>
            {
                parameters.Add(p => p.Data, AggregationData);
                parameters.Add(p => p.AllowDrillDown, false);
                parameters.Add(p => p.AllowFieldsPicking, false);

                parameters.Add<RenderFragment>(p => p.Rows, b =>
                {
                    b.OpenComponent<RadzenPivotRow<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotRow<SalesData>.Property), nameof(SalesData.Region));
                    b.AddAttribute(2, nameof(RadzenPivotRow<SalesData>.Title), "Region");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenPivotColumn<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotColumn<SalesData>.Property), nameof(SalesData.Year));
                    b.AddAttribute(2, nameof(RadzenPivotColumn<SalesData>.Title), "Year");
                    b.CloseComponent();
                });

                parameters.Add<RenderFragment>(p => p.Aggregates, b =>
                {
                    b.OpenComponent<RadzenPivotAggregate<SalesData>>(0);
                    b.AddAttribute(1, nameof(RadzenPivotAggregate<SalesData>.Property), nameof(SalesData.Amount));
                    b.AddAttribute(2, nameof(RadzenPivotAggregate<SalesData>.Title), "Amount");
                    b.AddAttribute(3, nameof(RadzenPivotAggregate<SalesData>.Aggregate), aggregate);
                    b.CloseComponent();
                });

                configure?.Invoke(parameters);
            });
        }

        private static string[] GetValueCellTexts<T>(IRenderedComponent<RadzenPivotDataGrid<T>> component, string rowHeaderText)
        {
            var rows = component.FindAll("tbody.rz-pivot-body tr.rz-pivot-row");
            var row = rows.First(r => r.GetElementsByClassName("rz-pivot-row-header")
                .Any(h => h.TextContent.Trim() == rowHeaderText));
            return ValuesOf(row);
        }

        private static string[] GetTotalCellTexts<T>(IRenderedComponent<RadzenPivotDataGrid<T>> component, string rowHeaderText)
        {
            var rows = component.FindAll("tbody.rz-pivot-body tr.rz-pivot-row");
            var row = rows.First(r => r.GetElementsByClassName("rz-pivot-row-header")
                .Any(h => h.TextContent.Trim() == rowHeaderText));
            return row.GetElementsByClassName("rz-pivot-total-cell")
                .Select(c => c.TextContent.Trim()).ToArray();
        }

        private static string[] ValuesOf(AngleSharp.Dom.IElement row)
        {
            return row.GetElementsByClassName("rz-pivot-value-cell")
                .Select(c => c.TextContent.Trim()).ToArray();
        }

        private static IRenderedComponent<RadzenPivotDataGrid<SalesData>> RenderPivotDataGrid(TestContext ctx, Action<ComponentParameterCollectionBuilder<RadzenPivotDataGrid<SalesData>>> configure = null)
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

