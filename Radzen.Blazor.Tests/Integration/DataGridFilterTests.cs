using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Bunit;
using Bunit.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Radzen.Blazor.Tests.Integration
{

    public class DataGridFilterTests : DataFixture
    {

        #region Filter Simple
        [Theory]
        [ClassData(typeof(TestGridData))]
        public void FilterTestAsync<T>(IEnumerable<T> data)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = data.GetTestDataGrid(ctx);

            component.SetParametersAndRender(p =>
                p.Add(s => s.Columns, b =>
                {
                    b.columnBuilder<T>(nameof(Employee.EmployeeID), data, filterMode: FilterMode.Simple);
                    b.columnBuilder<T>(nameof(Employee.FirstName), data, filterMode: FilterMode.Simple);
                    b.columnBuilder<T>(nameof(Employee.Title), data, filterMode: FilterMode.Simple);
                    b.columnBuilder<T>(nameof(Employee.HireDate), data, filterMode: FilterMode.Simple);
                }));

            var dg = component.Instance;
            var cols = dg.ColumnsCollection.Count;
            var rows = dg.Data.Count();
            Assert.Equal(_cols, cols);
            Assert.Equal(_rows, rows);

            var trs = component.FindAll("tbody > tr.rz-data-row").Count();
            Assert.Equal(_rows, trs);

            //Filter String
            ChangeInuptValue(component, "span.rz-cell-filter-label > input", nameof(Employee.FirstName), "an");
            Assert.Equal(2, GridRowCount(component));
            ChangeInuptValue(component, "span.rz-cell-filter-label > input", nameof(Employee.FirstName), null);
            Assert.Equal(_rows, GridRowCount(component));

            //Filter Int
            ChangeInuptValue(component, "span.rz-numeric > input", nameof(Employee.EmployeeID), 1);
            Assert.Equal(1, GridRowCount(component));
            ChangeInuptValue(component, "span.rz-numeric > input", nameof(Employee.EmployeeID), null);
            Assert.Equal(_rows, GridRowCount(component));

            //Filter Date
            ChangeDatetimeValueAsync(component, nameof(Employee.HireDate), new DateTime(2013, 10, 17), 3);
            var dataFilter = component.FindAll("div.rz-date-filter").FirstOrDefault(x => x.OuterHtml.Contains(nameof(Employee.HireDate)));

            Assert.Equal(3, GridRowCount(component));

        }

        private IHtmlInputElement ChangeInuptValue<T>(IRenderedComponent<RadzenDataGrid<T>> component, string css, string colName, object changeTo)
        {
            var inputelems = component.FindAll(css);
            IHtmlInputElement thisInput = inputelems.FirstOrDefault(x => x.OuterHtml.Contains(colName)) as IHtmlInputElement;
            Assert.NotNull(thisInput);
            thisInput.Change(changeTo);
            return thisInput;
        }

        private void ChangeDatetimeValueAsync<T>(IRenderedComponent<RadzenDataGrid<T>> component, string colName, DateTime changeTo, int expected)
        {
            RadzenDataGrid<T> grid = component.Instance;
            var col = grid.ColumnsCollection.OfType<RadzenDataGridColumn<T>>().FirstOrDefault(c=>c.Title ==  colName);
            col.FilterOperator = FilterOperator.Equals;
            col.FilterValue = changeTo;

            grid.Reload();
            component.WaitForAssertion(() =>
            {
                var view = grid.View; // Try accessing here, after reload
                Assert.Equal(expected, view.Count());
            }, timeout: TimeSpan.FromSeconds(1));
        }

        private int GridRowCount<T>(IRenderedComponent<RadzenDataGrid<T>> component) => component.FindAll("tbody > tr.rz-data-row").Count();
        #endregion


        #region Filter Checkbox
        [Theory]
        [ClassData(typeof(TestGridData))]
        public void FilterCheckboxTestAsync<T>(IEnumerable<T> data)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = data.GetTestDataGrid(ctx);
            component.SetParametersAndRender(p =>
                p.Add(s => s.Columns, b =>
                {
                    b.columnBuilder<T>(nameof(Employee.EmployeeID), data, filterMode: FilterMode.CheckBoxList);
                    b.columnBuilder<T>(nameof(Employee.FirstName), data, filterMode: FilterMode.CheckBoxList);
                    b.columnBuilder<T>(nameof(Employee.Title), data, filterMode: FilterMode.CheckBoxList);
                    b.columnBuilder<T>(nameof(Employee.HireDate), data, filterMode: FilterMode.CheckBoxList);
                }));

            var dg = component.Instance;
            var cols = dg.ColumnsCollection.Count;
            var rows = dg.Data.Count();
            Assert.Equal(_cols, cols);
            Assert.Equal(_rows, rows);

            var trs = component.FindAll("tbody > tr.rz-data-row").Count();
            Assert.Equal(_rows, trs);

            RadzenDataGrid<T> grid;

            //Filter Date
            grid = component.Instance;
            SetFilterList(grid, "li.rz-multiselect-item", nameof(Employee.HireDate), new DateTime?[] { new DateTime(2013, 10, 17) });
            component.WaitForAssertion(() =>
            {
                var view = grid.View;
                Assert.Equal(3, view.Count());
            }, timeout: TimeSpan.FromSeconds(1));
            component.Dispose();

            //Filter String
            component = data.GetTestDataGrid(ctx);
            component.SetParametersAndRender(p =>
                p.Add(s => s.Columns, b =>
                {
                    b.columnBuilder<T>(nameof(Employee.EmployeeID), data, filterMode: FilterMode.CheckBoxList);
                    b.columnBuilder<T>(nameof(Employee.FirstName), data, filterMode: FilterMode.CheckBoxList);
                    b.columnBuilder<T>(nameof(Employee.Title), data, filterMode: FilterMode.CheckBoxList);
                    b.columnBuilder<T>(nameof(Employee.HireDate), data, filterMode: FilterMode.CheckBoxList);
                }));
            grid = component.Instance;
            SetFilterList(grid, "li.rz-multiselect-item", nameof(Employee.Title), new[] { "Sales Representative", "Vice President, Sales" });
            component.WaitForAssertion(() =>
            {
                var view = grid.View;
                Assert.Equal(7, view.Count());
            }, timeout: TimeSpan.FromSeconds(1));
            component.Dispose();

            //Filter Int
            component = data.GetTestDataGrid(ctx);
            component.SetParametersAndRender(p =>
                p.Add(s => s.Columns, b =>
                {
                    b.columnBuilder<T>(nameof(Employee.EmployeeID), data, filterMode: FilterMode.CheckBoxList);
                    b.columnBuilder<T>(nameof(Employee.FirstName), data, filterMode: FilterMode.CheckBoxList);
                    b.columnBuilder<T>(nameof(Employee.Title), data, filterMode: FilterMode.CheckBoxList);
                    b.columnBuilder<T>(nameof(Employee.HireDate), data, filterMode: FilterMode.CheckBoxList);
                }));
            grid = component.Instance;
            SetFilterList(grid, "li.rz-multiselect-item", nameof(Employee.EmployeeID), new[] { 1, 2, 3 });
            component.WaitForAssertion(() =>
            {
                var view = grid.View;
                Assert.Equal(3, view.Count());
            }, timeout: TimeSpan.FromSeconds(1));
            component.Dispose();

        }

        private void SetFilterList<T,V>(RadzenDataGrid<T> grid, string css, string colName, V[] changeTo)
        {
            var col = grid.ColumnsCollection.OfType<RadzenDataGridColumn<T>>().FirstOrDefault(c => c.Title == colName);
            col.FilterOperator = FilterOperator.Equals;
            col.FilterValue = changeTo;

            grid.Reload();

        }


        #endregion
    }
}
