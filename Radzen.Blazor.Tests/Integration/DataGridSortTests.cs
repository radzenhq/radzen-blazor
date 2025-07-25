using AngleSharp.Html.Dom;
using Bunit;
using Bunit.Extensions;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Radzen.Blazor.Tests.Integration
{

    public class DataGridSortTests : DataFixture
    {

        [Fact]
        public void TestingFact()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var data = DataFixtureHelpers.GetEmployees<Employee>();

            var component = ctx.RenderComponent<RadzenDataGrid<Employee>>(parameterBuilder =>
            {
                //parameterBuilder.Add<IEnumerable<Employee>>(p => p.Data, data);
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<bool>(p => p.AllowSorting, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Employee>));
                    builder.AddAttribute(1, "Property", nameof(Employee.EmployeeID));
                    builder.AddAttribute(2, "Filterable", false);
                    builder.CloseComponent();
                });
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Employee>));
                    builder.AddAttribute(1, "Property", "FirstName");
                    builder.AddAttribute(2, "Filterable", true);
                    builder.CloseComponent();
                });
            });

            component.SetParametersAndRender(p => p.Add<IEnumerable<Employee>>(c => c.Data, data));
            RadzenDataGrid<Employee> dg = component.Instance;
            var cols = dg.ColumnsCollection.Count;
            var col = dg.ColumnsCollection.FirstOrDefault(x => x.Property == "FirstName");

            var cells = component.FindAll(".rz-cell-data");
            Assert.Equal(_rows * cols, cells.Count);
            Assert.Equal("1", cells[0 * cols].TextContent.Trim());
            Assert.Equal("2", cells[1 * cols].TextContent.Trim());
            Assert.Equal("3", cells[2 * cols].TextContent.Trim());

            var allCols = component.FindComponents<RadzenDataGridColumn<Employee>>();
            var fnCol = allCols.FirstOrDefault(x => x.Instance.Property == nameof(Employee.EmployeeID));
            Assert.NotNull(fnCol);
            fnCol.SetParametersAndRender(p => p.Add(s => s.SortOrder, SortOrder.Descending));
            component.SetParametersAndRender();

            cells = component.FindAll(".rz-cell-data");
            Assert.Equal("9", cells[0 * cols].TextContent.Trim());
            Assert.Equal("8", cells[1 * cols].TextContent.Trim());
            Assert.Equal("7", cells[2 * cols].TextContent.Trim());
        }


        [Theory]
        [ClassData(typeof(TestGridData))]
        public void SortStringTest<T>(IEnumerable<T> data, DataTable table = null)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = data.GetTestDataGrid(ctx, table);

            var dg = component.Instance;
            var cols = dg.ColumnsCollection.Count;
            var rows = dg.Data.Count();

            var sortCol = component.FindComponents<RadzenDataGridColumn<T>>().FirstOrDefault(x => x.Instance.Title == nameof(Employee.FirstName));
            Assert.NotNull(sortCol); var sortHead = component.FindAll(".rz-column-title")
                .FirstOrDefault(x => x.TextContent.Contains(nameof(Employee.FirstName)));
            Assert.NotNull(sortHead);

            var cells = component.FindAll(".rz-cell-data");
            Assert.Equal(_rows * cols, cells.Count);
            Assert.Equal("1", cells[0 * cols].TextContent.Trim());
            Assert.Equal("2", cells[1 * cols].TextContent.Trim());
            Assert.Equal("3", cells[2 * cols].TextContent.Trim());

            sortHead.Click();
            //Assert.Equal(SortOrder.Descending, sortCol.Instance.SortOrder);
            cells = component.FindAll(".rz-cell-data");
            Assert.Equal("2", cells[0 * cols].TextContent.Trim());
            Assert.Equal("9", cells[1 * cols].TextContent.Trim());
            Assert.Equal("3", cells[2 * cols].TextContent.Trim());

            sortHead.Click();
            //Assert.Equal(SortOrder.Ascending, sortCol.Instance.SortOrder);
            cells = component.FindAll(".rz-cell-data");
            Assert.Equal("5", cells[0 * cols].TextContent.Trim());
            Assert.Equal("1", cells[1 * cols].TextContent.Trim());
            Assert.Equal("6", cells[2 * cols].TextContent.Trim());

        }

        [Theory]
        [ClassData(typeof(TestGridData))]
        public void SortDateTest<T>(IEnumerable<T> data, DataTable table = null)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = data.GetTestDataGrid(ctx, table);

            var dg = component.Instance;
            var cols = dg.ColumnsCollection.Count;
            var rows = dg.Data.Count();

            var sortCol = component.FindComponents<RadzenDataGridColumn<T>>().FirstOrDefault(x => x.Instance.Title == nameof(Employee.HireDate));
            Assert.NotNull(sortCol); var sortHead = component.FindAll(".rz-column-title")
                .FirstOrDefault(x => x.TextContent.Contains(nameof(Employee.HireDate)));
            Assert.NotNull(sortHead);

            var cells = component.FindAll(".rz-cell-data");
            Assert.Equal(_rows * cols, cells.Count);
            Assert.Equal("1", cells[0 * cols].TextContent.Trim());
            Assert.Equal("2", cells[1 * cols].TextContent.Trim());
            Assert.Equal("3", cells[2 * cols].TextContent.Trim());

            sortHead.Click();
            //Assert.Equal(SortOrder.Descending, sortCol.Instance.SortOrder);
            cells = component.FindAll(".rz-cell-data");
            Assert.Equal("3", cells[0 * cols].TextContent.Trim());
            Assert.Equal("1", cells[1 * cols].TextContent.Trim());
            Assert.Equal("2", cells[2 * cols].TextContent.Trim());

            sortHead.Click();
            //Assert.Equal(SortOrder.Ascending, sortCol.Instance.SortOrder);
            cells = component.FindAll(".rz-cell-data");
            Assert.Equal("9", cells[0 * cols].TextContent.Trim());
            Assert.Equal("8", cells[1 * cols].TextContent.Trim());
            Assert.Equal("5", cells[2 * cols].TextContent.Trim());

        }

        [Theory]
        [ClassData(typeof(TestGridData))]
        public void SortIntTestAsync<T>(IEnumerable<T> data, DataTable table = null)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = data.GetTestDataGrid(ctx, table);

            var dg = component.Instance;
            var cols = dg.ColumnsCollection.Count;
            var rows = dg.Data.Count();
            Assert.Equal(_cols, cols);
            Assert.Equal(_rows, rows);

            var sortCol = component.FindComponents<RadzenDataGridColumn<T>>().FirstOrDefault(x => x.Instance.Title == nameof(Employee.EmployeeID));
            Assert.NotNull(sortCol);
            var sortHead = component.FindAll(".rz-column-title")
                .FirstOrDefault(x=>x.TextContent.Contains(nameof(Employee.EmployeeID)));
            Assert.NotNull(sortHead);

            var cells = component.FindAll(".rz-cell-data");
            Assert.Equal(_rows * cols, cells.Count);
            Assert.Equal("1", cells[0 * cols].TextContent.Trim());
            Assert.Equal("2", cells[1 * cols].TextContent.Trim());
            Assert.Equal("3", cells[2 * cols].TextContent.Trim());

            sortHead.Click();
            //sortCol.SetParametersAndRender(p => p.Add(s => s.SortOrder, SortOrder.Ascending));
            //await dg.ChangeState();

            cells = component.FindAll(".rz-cell-data");
            Assert.Equal(_rows * cols, cells.Count);
            Assert.Equal("1", cells[0 * cols].TextContent.Trim());
            Assert.Equal("2", cells[1 * cols].TextContent.Trim());
            Assert.Equal("3", cells[2 * cols].TextContent.Trim());

            sortHead.Click();
            //sortCol.SetParametersAndRender(p => p.Add(s => s.SortOrder, SortOrder.Descending));
            //await dg.ChangeState();

            cells = component.FindAll(".rz-cell-data");
            Assert.Equal("9", cells[0 * cols].TextContent.Trim());
            Assert.Equal("8", cells[1 * cols].TextContent.Trim());
            Assert.Equal("7", cells[2 * cols].TextContent.Trim());
        }


    }
}
