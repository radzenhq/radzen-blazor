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

            var component = ctx.RenderComponent<RadzenGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.CloseComponent();
                });
            });

            // Main
            Assert.Contains(@$"rz-datatable-scrollable-wrapper", component.Markup);
            Assert.Contains(@$"rz-datatable-scrollable-view", component.Markup);

            // Header
            Assert.Contains(@$"rz-datatable-scrollable-header", component.Markup);
            Assert.Contains(@$"rz-datatable-scrollable-header-box", component.Markup);
            Assert.Contains(@$"rz-datatable-thead", component.Markup);
            Assert.Contains(@$"rz-datatable-scrollable-colgroup", component.Markup);

            //Body
            Assert.Contains(@$"rz-datatable-scrollable-body", component.Markup);
            Assert.Contains(@$"rz-datatable-scrollable-table-wrapper", component.Markup);
            Assert.Contains(@$"rz-datatable-data", component.Markup);
            Assert.Contains(@$"rz-datatable-hoverable-rows", component.Markup);

            // Footer
            Assert.DoesNotContain(@$"rz-datatable-scrollable-footer", component.Markup);
            Assert.DoesNotContain(@$"rz-datatable-scrollable-footer-box", component.Markup);

            //Columns
            Assert.DoesNotContain(@$"rz-sortable-column", component.Markup);
        }

        // Columns tests
        [Fact]
        public void DataGrid_Renders_ColumnPropertyParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenGridColumn<dynamic>));
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

            var component = ctx.RenderComponent<RadzenGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenGridColumn<dynamic>));
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
            Assert.Empty(title.GetAttribute("title"));
        }

        [Fact]
        public void DataGrid_Renders_AllowSortingParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenGridColumn<dynamic>));
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

            var component = ctx.RenderComponent<RadzenGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenGridColumn<dynamic>));
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

            var component = ctx.RenderComponent<RadzenGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenGridColumn<dynamic>));
                    builder.AddAttribute(1, "Property", "Id");
                    builder.AddAttribute(2, "Title", "Id");
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
            });

            Assert.Contains(@$"rz-cell-filter", component.Markup);

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowFiltering, false);
            });

            Assert.DoesNotContain(@$"rz-cell-filter", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_ColumnFilterableParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenGridColumn<dynamic>));
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

            var component = ctx.RenderComponent<RadzenGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, new[] { new { Id = 1 }, new { Id = 2 }, new { Id = 3 } });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenGridColumn<dynamic>));
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

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<int>>(p => p.Data, new[] { 1, 2, 3 });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenGridColumn<int>));

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

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<int>>(p => p.Data, new[] { 1, 2, 3 });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenGridColumn<int>));

                    builder.AddAttribute(1, "FooterTemplate", (RenderFragment)delegate (RenderTreeBuilder b)
                    {
                        b.AddMarkupContent(2, "Footer");
                    });

                    builder.CloseComponent();
                });
            });

            Assert.Contains(@$"rz-datatable-scrollable-footer", component.Markup);
            Assert.Contains(@$"rz-datatable-scrollable-footer-box", component.Markup);
            Assert.Contains(@$"Footer", component.Markup);
        }

        // Sorting tests
        [Fact]
        public void DataGrid_Renders_ColumnSortIcon()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenGrid<dynamic>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<dynamic>>(p => p.Data, Enumerable.Range(0, 100).Select(i => new { Id = i }));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenGridColumn<dynamic>));
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

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.AllowPaging, true));

            Assert.Contains(@$"rz-paginator-bottom", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_PagerPositionTopParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<PagerPosition>(p => p.PagerPosition, PagerPosition.Top);
            });

            Assert.Contains(@$"rz-paginator", component.Markup);
            Assert.DoesNotContain(@$"rz-paginator-bottom", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_PagerPositionTopAndBottomParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<PagerPosition>(p => p.PagerPosition, PagerPosition.TopAndBottom);
            });

            Assert.Contains(@$"rz-paginator", component.Markup);
            Assert.Contains(@$"rz-paginator-bottom", component.Markup);
        }

        [Fact]
        public void DataGrid_Renders_PagerDensityDefault()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

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

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

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

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Array.Empty<int>()));
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

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Array.Empty<int>()));
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

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Array.Empty<int>()));
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

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;
            LoadDataArgs newArgs = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; newArgs = args; });
            });

            component.Find(".rz-paginator-next").Click();

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

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;
            LoadDataArgs newArgs = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; newArgs = args; });
            });

            component.Find(".rz-paginator-last").Click();

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

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;
            LoadDataArgs newArgs = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; newArgs = args; });
            });

            component.Find(".rz-paginator-next").Click();
            component.Find(".rz-paginator-prev").Click();

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

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;
            LoadDataArgs newArgs = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; newArgs = args; });
            });

            component.Find(".rz-paginator-next").Click();
            component.Find(".rz-paginator-first").Click();

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

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; });
            });

            component.Find(".rz-paginator-first").Click();

            Assert.False(raised);
        }

        [Fact]
        public void DataGrid_NotRaises_LoadDataEventOnPrevPageClickWhenOnFirstPage()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; });
            });

            component.Find(".rz-paginator-prev").Click();

            Assert.False(raised);
        }

        [Fact]
        public void DataGrid_NotRaises_LoadDataEventOnLastPageClickWhenOnLastPage()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowPaging, true);
            });

            component.Find(".rz-paginator-last").Click();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; });
            });

            component.Find(".rz-paginator-last").Click();

            Assert.False(raised);
        }

        [Fact]
        public void DataGrid_NotRaises_LoadDataEventOnNextPageClickWhenOnLastPage()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowPaging, true);
            });

            component.Find(".rz-paginator-last").Click();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; });
            });

            component.Find(".rz-paginator-next").Click();

            Assert.False(raised);
        }

        [Fact]
        public void DataGrid_Respects_PageSizeParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenGrid<int>>(parameterBuilder => parameterBuilder.Add<IEnumerable<int>>(p => p.Data, Enumerable.Range(0, 100)));

            var raised = false;
            LoadDataArgs newArgs = null;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowPaging, true);
                parameters.Add<int>(p => p.PageSize, 20);
                parameters.Add<LoadDataArgs>(p => p.LoadData, args => { raised = true; newArgs = args; });
            });

            component.Find(".rz-paginator-next").Click();

            Assert.True(raised);
            Assert.True(newArgs.Skip == 20);
            Assert.True(newArgs.Top == 20);
        }
    }
}
