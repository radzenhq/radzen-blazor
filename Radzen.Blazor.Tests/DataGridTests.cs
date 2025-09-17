using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static System.Reflection.Metadata.BlobBuilder;

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

        // Filter tests

        /// <summary>
        /// Utility class for testing.
        /// </summary>
        /// <remarks>
        /// Tests that involves filtering on <see cref="RadzenDataGrid{TItem}"/> requires the generic parameter to be a specific type.
        /// They do not work using <c>object</c> or <c>dynamic</c>.
        /// </remarks>
        /// <param name="Name"></param>
        /// <param name="Roles"></param>
        private sealed record User(string Name, IEnumerable<Role> Roles);
        /// <summary>
        /// Utility class for testing.
        /// </summary>
        /// <remarks>
        /// Tests that involves filtering on <see cref="RadzenDataGrid{TItem}"/> requires the generic parameter to be a specific type.
        /// They do not work using <c>object</c> or <c>dynamic</c>.
        /// </remarks>
        /// <param name="Id"></param>
        /// <param name="Description"></param>
        private sealed record Role(int Id, string Description);

        [Fact]
        public async Task DataGrid_FilterBySubProperties_ReturnsDataFiltered()
        {
            // Arrange
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            Role admin = new(0, "Admin");
            Role guest = new(1, "Guest");
            User moe = new("Moe", [admin]);
            User tom = new("Tom", [admin, guest]);
            User sam = new("Sam", [guest]);

            User[] data = [moe, tom, sam];

            var component = ctx.RenderComponent<RadzenDataGrid<User>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.AllowFiltering, true);
                parameters.Add(p => p.FilterMode, FilterMode.CheckBoxList);
                parameters.Add(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<User>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.AddAttribute(2, "Title", "User");
                    builder.CloseComponent();
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<User>));
                    builder.AddAttribute(1, "Property", "Roles");
                    builder.AddAttribute(2, "FilterProperty", "Id");
                    builder.AddAttribute(3, "Type", typeof(IEnumerable<Role>));
                    builder.AddAttribute(4, "Title", "Roles");
                    builder.CloseComponent();
                });
            });

            // Act
            await component.InvokeAsync(() => component
                .Instance
                .ColumnsCollection
                .First(c => c.Property == "Roles")
                .SetFilterValueAsync(new[] { 1 })
            );

            component.Render();

            var filteredData = await component.InvokeAsync(component.Instance.View.ToArray);

            // Assert
            Assert.DoesNotContain(moe, filteredData);
            Assert.Contains(sam, filteredData);
            Assert.Contains(tom, filteredData);
        }

        [Fact]
        public async Task DataGrid_LoadFilterSettingsFromJson_ReturnsDataFiltered()
        {
            // Arrange
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            Role admin = new(0, "Admin");
            Role guest = new(1, "Guest");
            User moe = new("Moe", [admin]);
            User tom = new("Tom", [admin, guest]);
            User sam = new("Sam", [guest]);

            User[] data = [moe, tom, sam];

            string settings = string.Empty;

            var component = ctx.RenderComponent<RadzenDataGrid<User>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.AllowFiltering, true);
                parameters.Add(p => p.FilterMode, FilterMode.CheckBoxList);
                parameters.Add(p => p.LoadSettings, OnLoadSettings);
                parameters.Add(p => p.SettingsChanged, OnSettingsChanged);
                parameters.Add(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<User>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.AddAttribute(2, "Title", "User");
                    builder.CloseComponent();
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<User>));
                    builder.AddAttribute(1, "Property", "Roles");
                    builder.AddAttribute(2, "FilterProperty", "Id");
                    builder.AddAttribute(3, "Type", typeof(IEnumerable<Role>));
                    builder.AddAttribute(4, "Title", "Roles");
                    builder.CloseComponent();
                });
            });

            void OnSettingsChanged(DataGridSettings args)
            {
                settings = JsonSerializer.Serialize(args);
            }

            void OnLoadSettings(DataGridLoadSettingsEventArgs args)
            {
                if (string.IsNullOrEmpty(settings)) return;

                args.Settings = JsonSerializer.Deserialize<DataGridSettings>(settings);
            }

            // Act
            await component.InvokeAsync(() => component
                .Instance
                .ColumnsCollection
                .First(c => c.Property == "Roles")
                .SetFilterValueAsync(new[] { 1 })
            );

            component.Render();

            var filteredData = await component.InvokeAsync(component.Instance.View.ToArray);

            // Assert
            Assert.DoesNotContain(moe, filteredData);
            Assert.Contains(sam, filteredData);
            Assert.Contains(tom, filteredData);
        }
    }
}
