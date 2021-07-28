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

            var markup = new Regex(@"\s\s+").Replace(component.Markup, "").Trim();

            Assert.Contains(@$"<span class=""rz-cell-data"">1</span>", markup);
            Assert.Contains(@$"<span class=""rz-cell-data"">2</span>", markup);
            Assert.Contains(@$"<span class=""rz-cell-data"">3</span>", markup);
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

            var markup = new Regex(@"\s\s+").Replace(component.Markup, "").Trim();

            Assert.Contains(@$"<span class=""rz-column-title"">MyId</span>", markup);
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

        [Fact]
        public void DataGrid_View_Use_ClientSide_Filtering_When_LoadData_Empty()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<Tuple<string>>>(parameterBuilder => {
                parameterBuilder.Add(p => p.Data, new[] { new Tuple<string>("Name_0"), new Tuple<string>("Name_1"), new Tuple<string>("Name_2") });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder => {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Tuple<string>>));
                    builder.AddAttribute(1, "Property", "Item1");
                    builder.AddAttribute(2, "Filterable", true);
                    builder.AddAttribute(3, "FilterOperator", FilterOperator.Equals);
                    builder.AddAttribute(4, "FilterValue", "Name_1");
                    builder.AddAttribute(5, "Type", typeof(string));
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
            });

            Assert.Equal<IEnumerable<string>>(component.Instance.View.Select(x => x.Item1), new[] { "Name_1" });
        }

        [Fact]
        public void DataGrid_View_Use_ServerSide_Filtering_When_LoadData_Used()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<Tuple<string>>>(parameterBuilder => {
                parameterBuilder.Add(p => p.Data, new[] { new Tuple<string>("Name_0"), new Tuple<string>("Name_1"), new Tuple<string>("Name_2") });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder => {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Tuple<string>>));
                    builder.AddAttribute(1, "Property", "Item1");
                    builder.AddAttribute(2, "Filterable", true);
                    builder.AddAttribute(3, "FilterOperator", FilterOperator.Equals);
                    builder.AddAttribute(4, "FilterValue", "Name_1");
                    builder.AddAttribute(5, "Type", typeof(string));
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<LoadDataArgs>(p => p.LoadData, args => { });
            });

            Assert.Equal<IEnumerable<string>>(component.Instance.View.Select(x => x.Item1), new[] { "Name_0", "Name_1", "Name_2" });
        }

        [Fact]
        public void DataGrid_View_Use_Custom_Filtering()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            Func<string, RenderFragment> columnRenderFragmentFunc = (filterValue) => {
                return delegate (RenderTreeBuilder builder)
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Tuple<int, List<Tuple<int, string>>>>));
                    builder.AddAttribute(1, "Property", "Item2");
                    builder.AddAttribute(2, "Filterable", true);
                    builder.AddAttribute(3, "FilterOperator", FilterOperator.Contains);
                    builder.AddAttribute(4, "FilterValue", filterValue);
                    builder.AddAttribute(5, "Type", typeof(string));
                    builder.AddAttribute(6, "CustomFiltering", (Func<CustomFilteringArgs<Tuple<int, List<Tuple<int, string>>>>, bool>)(args => {
                        if (args.Property == "Item2")
                        {
                            switch (args.FilterOperator)
                            {
                                case FilterOperator.Contains:
                                    return args.Data.Item2.Any(x => x.Item2.Contains(args.FilterValue as string));
                                default:
                                    break;
                            }
                        }
                        return false;

                    }));
                    builder.CloseComponent();
                };
            };

            var component = ctx.RenderComponent<RadzenDataGrid<Tuple<int, List<Tuple<int, string>>>>>(parameterBuilder => {
                parameterBuilder.Add(p => p.Data, new[] {
                    new Tuple<int, List<Tuple<int, string>>>(0, new List<Tuple<int, string>>() { new Tuple<int, string>(100, "Detail_00"), new Tuple<int, string>(101, "Detail_01") }),
                    new Tuple<int, List<Tuple<int, string>>>(1, new List<Tuple<int, string>>() { new Tuple<int, string>(110, "Detail_10"), new Tuple<int, string>(111, "Detail_11") }),
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, columnRenderFragmentFunc("Detail_1"));
            });

            Assert.Equal<IEnumerable<string>>(component.Instance.View.SelectMany(x => x.Item2).Select(x => x.Item2), new string[] { "Detail_10", "Detail_11" });

            component.SetParametersAndRender(parameters => {
                parameters.Add<RenderFragment>(p => p.Columns, columnRenderFragmentFunc("nonexistent"));
            });

            IEnumerable<Tuple<int, List<Tuple<int, string>>>> filtered = null;
            component.InvokeAsync(() => filtered = component.Instance.View.ToList()).Wait();
            Assert.Equal<IEnumerable<string>>(filtered.SelectMany(x => x.Item2).Select(x => x.Item2), Enumerable.Empty<string>());
        }
    }
}
