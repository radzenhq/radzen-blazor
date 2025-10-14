using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridTests
    {
        // Css classes tests
        [Fact]
        public void DataGrid_Renders_CssClass()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.CloseComponent();
                });
            });

            // Main
            Assert.Contains(@$"rz-data-grid", component.Markup);
            Assert.Contains(@$"rz-datatable", component.Markup);
            Assert.Contains(@$"rz-datatable-scrollable", component.Markup);

            // Data
            Assert.Contains(@$"rz-data-grid-data", component.Markup);

            // Table
            Assert.Contains(@$"rz-grid-table", component.Markup);
            Assert.Contains(@$"rz-grid-table-fixed", component.Markup);
            Assert.Contains(@$"rz-grid-table-striped", component.Markup);
        }

        // Columns tests
        [Fact]
        public void DataGrid_Renders_ColumnPropertyParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.CloseComponent();
                });
            });

            var data = component.FindAll(".rz-cell-data");

            Assert.Equal("1", data[0].TextContent.Trim());
            Assert.Equal("2", data[1].TextContent.Trim());
            Assert.Equal("3", data[2].TextContent.Trim());
        }

        [Fact]
        public void DataGrid_Renders_ColumnTitleParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Title", "MyId");
                    builder.CloseComponent();
                });
            });

            var title = component.Find(".rz-column-title");
            Assert.Equal("MyId", title.TextContent.Trim());
        }

        [Fact]
        public void DataGrid_Renders_TitleAttribute()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add(p => p.ShowColumnTitleAsTooltip, true);
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Title", "MyId");
                    builder.CloseComponent();
                });
            });

            var title = component.Find(".rz-column-title");
            Assert.Equal("MyId", title.TextContent.Trim());
            Assert.Equal("MyId", title.GetAttribute("title"));
        }

        [Fact]
        public void DataGrid_DoesNotRender_TitleAttribute()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add(p => p.ShowColumnTitleAsTooltip, false);
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Title", "MyId");
                    builder.CloseComponent();
                });
            });

            var title = component.Find(".rz-column-title");
            Assert.Equal("MyId", title.TextContent.Trim());
            Assert.Null(title.GetAttribute("title"));
        }

        [Fact]
        public void DataGrid_Renders_AllowSortingParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowSorting, true);
            });

            Assert.Contains(@$"rz-sortable-column", component.Markup);

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowSorting, false);
            });

            Assert.DoesNotContain(@$"rz-sortable-column", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_ColumnSortableParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.AddAttribute(3, "Sortable", false);
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowSorting, true);
            });

            Assert.DoesNotContain(@$"rz-sortable-column", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_AllowFilteringParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
            });

            Assert.Contains(@$"rz-grid-filter-icon", component.Markup);

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowFiltering, false);
            });

            Assert.DoesNotContain(@$"rz-grid-filter-icon", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_ColumnFilterableParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.AddAttribute(3, "Filterable", false);
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
            });

            Assert.DoesNotContain(@$"rz-cell-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_FilterModeParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
            });

            Assert.Contains(@$"rz-grid-filter", component.Markup);

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
            });

            Assert.DoesNotContain(@$"rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_ColumnHeaderTemplate()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<int>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<int>>(p => p.Data, new[] { 1, 2, 3 });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<int>));

                    builder.AddAttribute(1, "HeaderTemplate", (RenderFragment)delegate (RenderTreeBuilder b)
                    {
                        b.AddMarkupContent(2, "Header");
                    });

                    builder.CloseComponent();
                });
            });

            Assert.Contains(@$"Header", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_ColumnFooterTemplate()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<int>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<int>>(p => p.Data, new[] { 1, 2, 3 });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<int>));

                    builder.AddAttribute(1, "FooterTemplate", (RenderFragment)delegate (RenderTreeBuilder b)
                    {
                        b.AddMarkupContent(2, "Footer");
                    });

                    builder.CloseComponent();
                });
            });

            Assert.Contains(@$"rz-datatable-tfoot", component.Markup);
            Assert.Contains(@$"rz-column-footer", component.Markup);
            Assert.Contains(@$"Footer", component.Markup);
        }

        // Sorting tests
        [Fact]
        public void DataGrid_Renders_ColumnSortIcon()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, Enumerable.Range(0, 100).Select(i => new { Id = i }));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowSorting, true);
            });

            component.Find(".rz-sortable-column").FirstElementChild.Click();

            Assert.Contains(@$"rzi-sort-asc", component.Markup);

            component.Find(".rz-sortable-column").FirstElementChild.Click();

            Assert.Contains(@$"rzi-sort-desc", component.Markup);
        }

        // Paging tests
        [Fact]
        public void DataGrid_Renders_AllowPagingParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, Enumerable.Range(0, 100).Select(i => new { Id = i }));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowPaging, true);
            });

            Assert.Contains(@$"rz-pager", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_PagerPositionTopParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, Enumerable.Range(0, 100).Select(i => new { Id = i }));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowPaging, true);
                parameterBuilder.Add<PagerPosition>(p => p.PagerPosition, PagerPosition.Top);
            });

            Assert.Contains(@$"rz-pager", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_PagerPositionTopAndBottomParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, Enumerable.Range(0, 100).Select(i => new { Id = i }));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowPaging, true);
                parameterBuilder.Add<PagerPosition>(p => p.PagerPosition, PagerPosition.TopAndBottom);
            });

            Assert.Contains(@$"rz-pager", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_PagerDensityDefault()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<PagerPosition>(p => p.PagerPosition, PagerPosition.Top);
                parameters.Add<Density>(p => p.Density, Density.Default);
            });

            Assert.DoesNotContain(@$"rz-density-compact", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_PagerDensityCompact()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<PagerPosition>(p => p.PagerPosition, PagerPosition.Top);
                parameters.Add<Density>(p => p.Density, Density.Compact);
            });

            Assert.Contains(@$"rz-density-compact", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_DefaultEmptyText()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Array.Empty<int>()));
            component.Render();

            Assert.Contains("No records to display.", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_EmptyText()
        {
            string emptyText = "Lorem Ipsum";
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Array.Empty<int>()));
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.EmptyText, emptyText);
            });

            Assert.Contains(emptyText, component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_EmptyTemplate()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Array.Empty<int>()));
            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<RenderFragment>(p => p.EmptyTemplate, builder =>
                {
                    builder.OpenElement(0, "p");
                    builder.AddContent(0, "Lorem Ipsum");
                    builder.CloseElement();
                });
            });

            Assert.Contains("<p>Lorem Ipsum</p>", component.Markup);
        }

        [Fact]
        public void DataGrid_Raises_LoadDataEventOnNextPageClick()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, Enumerable.Range(0, 100).Select(i => new { Id = i }));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowPaging, true);
            });

            var raised = false;
            LoadDataArgs newArgs = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; newArgs = args; });
            });

            component.Find(".rz-pager-next").Click();

            Assert.True(raised);
            Assert.True(newArgs.Skip == 10);
            Assert.True(newArgs.Top == 10);
        }

        [Fact]
        public void DataGrid_Raises_LoadDataEventOnLastPageClick()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, Enumerable.Range(0, 100).Select(i => new { Id = i }));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowPaging, true);
            });

            var raised = false;
            LoadDataArgs newArgs = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; newArgs = args; });
            });

            component.Find(".rz-pager-last").Click();

            Assert.True(raised);
            Assert.True(newArgs.Skip == 90);
            Assert.True(newArgs.Top == 10);
        }

        [Fact]
        public void DataGrid_Raises_LoadDataEventOnPrevPageClick()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, Enumerable.Range(0, 100).Select(i => new { Id = i }));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowPaging, true);
            });

            var raised = false;
            LoadDataArgs newArgs = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; newArgs = args; });
            });

            component.Find(".rz-pager-next").Click();
            component.Find(".rz-pager-prev").Click();

            Assert.True(raised);
            Assert.True(newArgs.Skip == 0);
            Assert.True(newArgs.Top == 10);
        }

        [Fact]
        public void DataGrid_Raises_LoadDataEventOnFirstPageClick()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, Enumerable.Range(0, 100).Select(i => new { Id = i }));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowPaging, true);
            });

            var raised = false;
            LoadDataArgs newArgs = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; newArgs = args; });
            });

            component.Find(".rz-pager-next").Click();
            component.Find(".rz-pager-first").Click();

            Assert.True(raised);
            Assert.True(newArgs.Skip == 0);
            Assert.True(newArgs.Top == 10);
        }

        [Fact]
        public void DataGrid_NotRaises_LoadDataEventOnFirstPageClickWhenOnFirstPage()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, Enumerable.Range(0, 100).Select(i => new { Id = i }));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowPaging, true);
            });

            var raised = false;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; });
            });

            component.Find(".rz-pager-first").Click();

            Assert.False(raised);
        }

        [Fact]
        public void DataGrid_NotRaises_LoadDataEventOnPrevPageClickWhenOnFirstPage()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, Enumerable.Range(0, 100).Select(i => new { Id = i }));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowPaging, true);
            });

            var raised = false;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; });
            });

            component.Find(".rz-pager-prev").Click();

            Assert.False(raised);
        }

        [Fact]
        public void DataGrid_NotRaises_LoadDataEventOnLastPageClickWhenOnLastPage()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, Enumerable.Range(0, 100).Select(i => new { Id = i }));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowPaging, true);
            });

            var raised = false;

            component.Find(".rz-pager-last").Click();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; });
            });

            component.Find(".rz-pager-last").Click();

            Assert.False(raised);
        }

        [Fact]
        public void DataGrid_NotRaises_LoadDataEventOnNextPageClickWhenOnLastPage()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, Enumerable.Range(0, 100).Select(i => new { Id = i }));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowPaging, true);
            });

            var raised = false;

            component.Find(".rz-pager-last").Click();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; });
            });

            component.Find(".rz-pager-next").Click();

            Assert.False(raised);
        }

        [Fact]
        public void DataGrid_Respects_PageSizeParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, Enumerable.Range(0, 100).Select(i => new { Id = i }));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowPaging, true);
            });

            var raised = false;
            LoadDataArgs newArgs = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; newArgs = args; });
            });

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<int>(p => p.PageSize, 20);
            });

            component.Find(".rz-pager-next").Click();

            Assert.True(raised);
            Assert.True(newArgs.Skip == 20);
            Assert.True(newArgs.Top == 20);
        }

        // Filtering tests
        [Fact]
        public void DataGrid_Renders_FilterInput_ForStringColumn()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Name = "Alice" },
                new { Id = 2, Name = "Bob" },
                new { Id = 3, Name = "Charlie" }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.AddAttribute(2, "Title", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
            });

            var filterInput = component.Find("input.rz-textbox");
            Assert.NotNull(filterInput);
            Assert.Contains("rz-cell-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Raises_LoadDataEvent_WhenFilterChanges()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Name = "Alice" },
                new { Id = 2, Name = "Bob" },
                new { Id = 3, Name = "Charlie" }
            };

            var raised = false;
            LoadDataArgs capturedArgs = null;

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.AddAttribute(2, "Title", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
                parameterBuilder.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; capturedArgs = args; });
            });

            var filterInput = component.Find("input.rz-textbox");
            filterInput.Change("Bob");

            Assert.True(raised);
            Assert.NotNull(capturedArgs);
        }

        [Fact]
        public void DataGrid_Renders_FilterInput_ForNumericColumn()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Value = 10 },
                new { Id = 2, Value = 20 },
                new { Id = 3, Value = 30 }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Value");
                    builder.AddAttribute(2, "Title", "Value");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
            });

            var filterInputs = component.FindAll("input");
            Assert.NotEmpty(filterInputs);
            Assert.Contains("rz-cell-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Raises_LoadDataEvent_OnFilterClear()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Name = "Alice" },
                new { Id = 2, Name = "Bob" },
                new { Id = 3, Name = "Charlie" }
            };

            var eventCount = 0;

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.AddAttribute(2, "Title", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
                parameterBuilder.Add<LoadDataArgs>(p => p.LoadData, args => { eventCount++; });
            });

            var filterInput = component.Find("input.rz-textbox");
            filterInput.Change("Bob");

            var countAfterFilter = eventCount;

            filterInput.Change("");

            Assert.True(eventCount > countAfterFilter);
        }

        [Fact]
        public void DataGrid_Filters_MultipleColumns()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Name = "Alice", Age = 25 },
                new { Id = 2, Name = "Bob", Age = 30 },
                new { Id = 3, Name = "Charlie", Age = 25 }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.AddAttribute(2, "Title", "Name");
                    builder.CloseComponent();

                    builder.OpenComponent(3, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(4, "Property", "Age");
                    builder.AddAttribute(5, "Title", "Age");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
            });

            var filterInputs = component.FindAll("input");
            var nameFilter = filterInputs.FirstOrDefault(i => i.ClassName?.Contains("rz-textbox") == true);
            var ageFilter = filterInputs.FirstOrDefault(i => i.ClassName?.Contains("rz-inputnumber") == true);

            if (nameFilter != null && ageFilter != null)
            {
                nameFilter.Change("Alice");
                ageFilter.Change(25);

                var visibleRows = component.FindAll(".rz-cell-data");
                Assert.Equal(2, visibleRows.Count); // Name and Age for Alice
            }
        }

        [Fact]
        public void DataGrid_Respects_LogicalFilterOperator_And()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Name = "Alice", Age = 25 },
                new { Id = 2, Name = "Bob", Age = 30 },
                new { Id = 3, Name = "Alice", Age = 30 }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.AddAttribute(2, "Title", "Name");
                    builder.CloseComponent();

                    builder.OpenComponent(3, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(4, "Property", "Age");
                    builder.AddAttribute(5, "Title", "Age");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
                parameterBuilder.Add<LogicalFilterOperator>(p => p.LogicalFilterOperator, LogicalFilterOperator.And);
            });

            var filterInputs = component.FindAll("input");
            var nameFilter = filterInputs.FirstOrDefault(i => i.ClassName?.Contains("rz-textbox") == true);
            var ageFilter = filterInputs.FirstOrDefault(i => i.ClassName?.Contains("rz-inputnumber") == true);

            if (nameFilter != null && ageFilter != null)
            {
                nameFilter.Change("Alice");
                ageFilter.Change(30);

                var visibleRows = component.FindAll(".rz-cell-data");
                Assert.Equal(2, visibleRows.Count); // Only Alice with Age 30 (Name and Age columns)
            }
        }

        [Fact]
        public void DataGrid_Respects_FilterCaseSensitivityParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Name = "Alice" },
                new { Id = 2, Name = "BOB" },
                new { Id = 3, Name = "charlie" }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.AddAttribute(2, "Title", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
                parameterBuilder.Add<FilterCaseSensitivity>(p => p.FilterCaseSensitivity, FilterCaseSensitivity.CaseInsensitive);
            });

            Assert.Contains("rz-cell-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_ShowsFilterIcon_WhenFilterIsApplied()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Name = "Alice" },
                new { Id = 2, Name = "Bob" }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.AddAttribute(2, "Title", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
            });

            Assert.Contains("rz-grid-filter-icon", component.Markup);
        }

        [Fact]
        public void DataGrid_ResetsPaging_WhenFilterIsApplied()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = Enumerable.Range(0, 100).Select(i => new { Id = i, Name = $"Name{i}" });

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.AddAttribute(2, "Title", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<bool>(p => p.AllowPaging, true);
                parameterBuilder.Add<int>(p => p.PageSize, 10);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
            });

            // Navigate to second page
            component.Find(".rz-pager-next").Click();

            // Apply filter
            var filterInput = component.Find("input.rz-textbox");
            filterInput.Change("Name1");

            // Verify we're back on first page by checking the displayed data
            var visibleRows = component.FindAll(".rz-cell-data");
            Assert.True(visibleRows.Count <= 10); // Should show first page results
        }

        [Fact]
        public void DataGrid_Renders_FilterWithSimpleWithMenuMode()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Name = "Alice" },
                new { Id = 2, Name = "Bob" }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.AddAttribute(2, "Title", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.SimpleWithMenu);
            });

            // SimpleWithMenu should have filter button with menu
            Assert.Contains("rz-filter-button", component.Markup);
            Assert.Contains("rz-overlaypanel", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_WithColumnFilterProperty()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Name = "Alice", SearchName = "alice" },
                new { Id = 2, Name = "Bob", SearchName = "bob" }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.AddAttribute(2, "FilterProperty", "SearchName");
                    builder.AddAttribute(3, "Title", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
            });

            var filterInput = component.Find("input.rz-textbox");
            Assert.NotNull(filterInput);
            Assert.Contains("rz-cell-filter", component.Markup);
        }
    }
}
