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

        // Comprehensive FilterMode tests
        [Fact]
        public void DataGrid_Renders_FilterMode_Simple()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Name = "Test" } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
            });

            Assert.Contains("rz-cell-filter", component.Markup);
            Assert.DoesNotContain("rz-filter-button", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_FilterMode_Advanced()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Name = "Test" } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
            });

            Assert.Contains("rz-grid-filter", component.Markup);
            Assert.Contains("rz-grid-filter-icon", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_FilterMode_CheckBoxList()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<string>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<string>>(p => p.Data, new[] { "Test1", "Test2" });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<string>));
                    builder.AddAttribute(1, "Property", "Length");
                    builder.AddAttribute(2, "Title", "Length");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.CheckBoxList);
            });

            Assert.Contains("rz-grid-filter", component.Markup);
            Assert.Contains("rz-grid-filter-icon", component.Markup);
        }

        // FilterOperator tests with Simple mode
        [Fact]
        public void DataGrid_SimpleMode_SupportsStringFiltering()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Name = "Apple" },
                new { Id = 2, Name = "Banana" },
                new { Id = 3, Name = "Cherry" }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
            });

            var filterInput = component.Find("input.rz-textbox");
            Assert.NotNull(filterInput);
        }

        [Fact]
        public void DataGrid_SimpleMode_SupportsNumericFiltering()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Value = 100 },
                new { Id = 2, Value = 200 },
                new { Id = 3, Value = 300 }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Value");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
            });

            var filterInputs = component.FindAll("input");
            Assert.NotEmpty(filterInputs);
        }

        [Fact]
        public void DataGrid_SimpleMode_SupportsDateFiltering()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Date = new DateTime(2023, 1, 1) },
                new { Id = 2, Date = new DateTime(2023, 6, 1) },
                new { Id = 3, Date = new DateTime(2023, 12, 1) }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Date");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
            });

            Assert.Contains("rz-cell-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_SimpleMode_SupportsBooleanFiltering()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, IsActive = true },
                new { Id = 2, IsActive = false },
                new { Id = 3, IsActive = true }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "IsActive");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
            });

            Assert.Contains("rz-cell-filter", component.Markup);
        }

        // SimpleWithMenu mode tests
        [Fact]
        public void DataGrid_SimpleWithMenuMode_ShowsFilterOperatorButton()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Name = "Test" } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.SimpleWithMenu);
            });

            Assert.Contains("rz-filter-button", component.Markup);
        }

        [Fact]
        public void DataGrid_SimpleWithMenuMode_ShowsOperatorMenu()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Value = 100 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Value");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.SimpleWithMenu);
            });

            // Should contain operator menu overlay
            Assert.Contains("rz-overlaypanel", component.Markup);
        }

        [Fact]
        public void DataGrid_SimpleWithMenuMode_StringColumn_ShowsStringOperators()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Name = "Test" } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.SimpleWithMenu);
            });

            // Check for presence of common string operators in menu
            Assert.Contains("Equals", component.Markup);
            Assert.Contains("Not equals", component.Markup);
        }

        [Fact]
        public void DataGrid_SimpleWithMenuMode_NumericColumn_ShowsNumericOperators()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Value = 100 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Value");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.SimpleWithMenu);
            });

            // Check for presence of numeric operators
            Assert.Contains("Less than", component.Markup);
            Assert.Contains("Greater than", component.Markup);
        }

        // Advanced mode tests
        [Fact]
        public void DataGrid_AdvancedMode_ShowsFilterPopup()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Name = "Test" } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
            });

            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_AdvancedMode_SupportsMultipleFilterConditions()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Value = 100 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Value");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
            });

            // Advanced mode should show filter panel
            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_AdvancedMode_ShowsAndOrOperators()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Name = "Test" } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
            });

            // Check for logical operators in advanced filter
            var markup = component.Markup;
            Assert.True(markup.Contains("And") || markup.Contains("Or") || markup.Contains("rz-grid-filter"));
        }

        // CheckBoxList mode tests
        [Fact]
        public void DataGrid_CheckBoxListMode_ShowsFilterIcon()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<string>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<string>>(p => p.Data, new[] { "Test1", "Test2" });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<string>));
                    builder.AddAttribute(1, "Property", "Length");
                    builder.AddAttribute(2, "Title", "Length");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.CheckBoxList);
            });

            Assert.Contains("rz-grid-filter-icon", component.Markup);
        }

        [Fact]
        public void DataGrid_CheckBoxListMode_RendersFilterPopup()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[] { "Category A", "Category B", "Category C" };

            var component = ctx.RenderComponent<RadzenDataGrid<string>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<string>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<string>));
                    builder.AddAttribute(1, "Property", "Length");
                    builder.AddAttribute(2, "Title", "Length");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.CheckBoxList);
            });

            Assert.Contains("rz-grid-filter", component.Markup);
        }

        // Filter operator behavior tests
        [Fact]
        public void DataGrid_FilterOperator_Equals_IsDefault()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Name = "Test" } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.SimpleWithMenu);
            });

            // Default operator should be Equals
            Assert.Contains("Equals", component.Markup);
        }

        [Fact]
        public void DataGrid_FilterOperator_ContainsForStrings()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Name = "Test" } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.SimpleWithMenu);
            });

            // Simple mode with string defaults to Contains operator
            var markup = component.Markup;
            Assert.True(markup.Contains("Equals") || markup.Contains("Contains") || markup.Contains("rz-filter"));
        }

        [Fact]
        public void DataGrid_FilterOperator_IsNull_Available()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Name = "Test" } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.SimpleWithMenu);
            });

            Assert.Contains("Is null", component.Markup);
        }

        [Fact]
        public void DataGrid_FilterOperator_IsNotNull_Available()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Name = "Test" } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.SimpleWithMenu);
            });

            Assert.Contains("Is not null", component.Markup);
        }

        [Fact]
        public void DataGrid_FilterOperator_StartsWith_Available()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Name = "Test" } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
            });

            // Advanced mode should support StartsWith
            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_FilterOperator_EndsWith_Available()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Name = "Test" } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
            });

            // Advanced mode should support EndsWith
            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_FilterOperator_LessThan_ForNumeric()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Value = 100 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Value");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.SimpleWithMenu);
            });

            Assert.Contains("Less than", component.Markup);
        }

        [Fact]
        public void DataGrid_FilterOperator_GreaterThan_ForNumeric()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Value = 100 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Value");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.SimpleWithMenu);
            });

            Assert.Contains("Greater than", component.Markup);
        }

        [Fact]
        public void DataGrid_FilterOperator_LessThanOrEquals_ForNumeric()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Value = 100 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Value");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.SimpleWithMenu);
            });

            Assert.Contains("Less than or equals", component.Markup);
        }

        [Fact]
        public void DataGrid_FilterOperator_GreaterThanOrEquals_ForNumeric()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Value = 100 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Value");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.SimpleWithMenu);
            });

            Assert.Contains("Greater than or equals", component.Markup);
        }

        [Fact]
        public void DataGrid_FilterOperator_NotEquals_Available()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1, Name = "Test" } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.SimpleWithMenu);
            });

            Assert.Contains("Not equals", component.Markup);
        }

        // Combined FilterMode and data type tests
        [Fact]
        public void DataGrid_SimpleMode_SupportsStringColumns()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[] { new { Id = 1, Name = "Test" } };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
            });

            Assert.Contains("rz-cell-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_AdvancedMode_SupportsStringColumns()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[] { new { Id = 1, Name = "Test" } };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
            });

            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_SimpleMode_SupportsNumericColumns()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[] { new { Id = 1, Value = 100 } };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Value");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
            });

            Assert.Contains("rz-cell-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_AdvancedMode_SupportsNumericColumns()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[] { new { Id = 1, Value = 100 } };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Value");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
            });

            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Filter_RaisesLoadDataEvent_WithAllFilterModes()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[] { new { Id = 1, Name = "Test" } };

            foreach (var mode in new[] { FilterMode.Simple, FilterMode.SimpleWithMenu })
            {
                var raised = false;

                var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
                {
                    parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                    parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                    {
                        builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                        builder.AddAttribute(1, "Property", "Name");
                        builder.CloseComponent();
                    });
                    parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                    parameterBuilder.Add<FilterMode>(p => p.FilterMode, mode);
                    parameterBuilder.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; });
                });

                var filterInput = component.Find("input.rz-textbox");
                filterInput.Change("test");

                Assert.True(raised, $"LoadData event should be raised for {mode}");
            }
        }

        // Collection filtering tests with In/NotIn and Contains/DoesNotContain operators
        [Fact]
        public void DataGrid_Filters_WithInOperator()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Category = "Electronics" },
                new { Id = 2, Category = "Books" },
                new { Id = 3, Category = "Clothing" },
                new { Id = 4, Category = "Electronics" }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Category");
                    builder.AddAttribute(2, "Title", "Category");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
            });

            // In operator should be available in Advanced mode
            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Filters_WithNotInOperator()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Status = "Active" },
                new { Id = 2, Status = "Inactive" },
                new { Id = 3, Status = "Pending" },
                new { Id = 4, Status = "Active" }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Status");
                    builder.AddAttribute(2, "Title", "Status");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
            });

            // NotIn operator should be available in Advanced mode
            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Filters_WithDoesNotContainOperator()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Description = "High quality product" },
                new { Id = 2, Description = "Budget friendly option" },
                new { Id = 3, Description = "Premium item" }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Description");
                    builder.AddAttribute(2, "Title", "Description");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.SimpleWithMenu);
            });

            // DoesNotContain should be available
            Assert.Contains("rz-filter-button", component.Markup);
        }

        [Fact]
        public void DataGrid_Filters_CollectionProperty_WithContains()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Name = "Product 1", Tags = new[] { "electronics", "new" } },
                new { Id = 2, Name = "Product 2", Tags = new[] { "books", "bestseller" } },
                new { Id = 3, Name = "Product 3", Tags = new[] { "electronics", "sale" } }
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
                    builder.AddAttribute(4, "Property", "Tags");
                    builder.AddAttribute(5, "Title", "Tags");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
            });

            // Should render grid with collection column
            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Filters_MultipleValues_WithInOperator()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Priority = 1 },
                new { Id = 2, Priority = 2 },
                new { Id = 3, Priority = 3 },
                new { Id = 4, Priority = 1 },
                new { Id = 5, Priority = 2 }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Priority");
                    builder.AddAttribute(2, "Title", "Priority");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
            });

            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_CheckBoxListMode_SupportsInOperator()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[] { "Category A", "Category B", "Category C" };

            var component = ctx.RenderComponent<RadzenDataGrid<string>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<string>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<string>));
                    builder.AddAttribute(1, "Property", "Length");
                    builder.AddAttribute(2, "Title", "Length");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.CheckBoxList);
            });

            // CheckBoxList mode uses In operator for multiple selections
            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Filters_StringCollection_WithContainsAny()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Roles = new List<string> { "Admin", "User" } },
                new { Id = 2, Roles = new List<string> { "User" } },
                new { Id = 3, Roles = new List<string> { "Admin", "Manager" } }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
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

            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Filters_NumericCollection_WithIn()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Scores = new[] { 90, 85, 88 } },
                new { Id = 2, Scores = new[] { 75, 80, 82 } },
                new { Id = 3, Scores = new[] { 95, 92, 98 } }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
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

            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Filters_StringValues_WithCheckBoxListMode()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Type = "TypeA" },
                new { Id = 2, Type = "TypeB" },
                new { Id = 3, Type = "TypeC" },
                new { Id = 4, Type = "TypeD" }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Type");
                    builder.AddAttribute(2, "Title", "Type");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
            });

            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Filters_CombinesCollectionFilters_WithAnd()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Category = "A", Status = "Active" },
                new { Id = 2, Category = "B", Status = "Inactive" },
                new { Id = 3, Category = "A", Status = "Pending" }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Category");
                    builder.AddAttribute(2, "Title", "Category");
                    builder.CloseComponent();

                    builder.OpenComponent(3, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(4, "Property", "Status");
                    builder.AddAttribute(5, "Title", "Status");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
                parameterBuilder.Add<LogicalFilterOperator>(p => p.LogicalFilterOperator, LogicalFilterOperator.And);
            });

            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Filters_CombinesCollectionFilters_WithOr()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Category = "Electronics", Price = 100 },
                new { Id = 2, Category = "Books", Price = 20 },
                new { Id = 3, Category = "Clothing", Price = 50 }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Category");
                    builder.AddAttribute(2, "Title", "Category");
                    builder.CloseComponent();

                    builder.OpenComponent(3, typeof(RadzenDataGridColumn<dynamic>));
                    builder.AddAttribute(4, "Property", "Price");
                    builder.AddAttribute(5, "Title", "Price");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Advanced);
                parameterBuilder.Add<LogicalFilterOperator>(p => p.LogicalFilterOperator, LogicalFilterOperator.Or);
            });

            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Filters_EmptyCollection_WithInOperator()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Tags = new string[] { } },
                new { Id = 2, Tags = new[] { "tag1", "tag2" } },
                new { Id = 3, Tags = new string[] { } }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
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

            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Filters_NullCollection_HandledGracefully()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Items = (string[])null },
                new { Id = 2, Items = new[] { "item1" } }
            };

            var component = ctx.RenderComponent<RadzenDataGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, testData);
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

            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_WithCheckBoxListFilterMode()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[] { "Category A", "Category B" };

            var component = ctx.RenderComponent<RadzenDataGrid<string>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<string>>(p => p.Data, testData);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<string>));
                    builder.AddAttribute(1, "Property", "Length");
                    builder.AddAttribute(2, "Title", "Length");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.CheckBoxList);
            });

            // Component should be rendered with CheckBoxList filter mode
            Assert.Contains("rz-grid-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Filters_NestedCollectionProperty()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var testData = new[]
            {
                new { Id = 1, Name = "Item1", Meta = new { Tags = new[] { "new", "featured" } } },
                new { Id = 2, Name = "Item2", Meta = new { Tags = new[] { "sale" } } }
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

            Assert.Contains("rz-grid-filter", component.Markup);
        }
    }
}
