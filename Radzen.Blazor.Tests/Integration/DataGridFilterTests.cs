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
            var dataFilter = component.FindAll("div.rz-date-filter").FirstOrDefault(x => x.OuterHtml.Contains(nameof(Employee.HireDate)));

            //component.SetParametersAndRender(p =>
            //    p.Add(s => s.Columns, b =>
            //    {
            //        b.columnBuilder<T>(nameof(Employee.EmployeeID), data, filterMode: FilterMode.Simple);
            //        b.columnBuilder<T>(nameof(Employee.FirstName), data, filterMode: FilterMode.Simple);
            //        b.columnBuilder<T>(nameof(Employee.Title), data, filterMode: FilterMode.Simple);

            //        b.OpenComponent(0, typeof(RadzenDataGridColumn<T>));
            //        b.AddAttribute(1, "Title", nameof(Employee.HireDate));
            //        b.AddAttribute(2, "Filterable", true);
            //        b.AddAttribute(3, "FilterValue", new DateOnly(2013, 10, 13));
            //        b.AddAttribute(4, "Property", typeof(T) == typeof(DataRow) ?
            //            $"ItemArray[6]" :
            //            nameof(Employee.HireDate));
            //        b.AddAttribute(5, "Type", typeof(DateTime));
            //        b.CloseComponent();
            //    }));

            //Assert.Equal(3, GridRowCount(component));

        }

        private IHtmlInputElement ChangeInuptValue<T>(IRenderedComponent<RadzenDataGrid<T>> component, string css, string colName, object changeTo)
        {
            var inputelems = component.FindAll(css);
            IHtmlInputElement thisInput = inputelems.FirstOrDefault(x => x.OuterHtml.Contains(colName)) as IHtmlInputElement;
            Assert.NotNull(thisInput);
            thisInput.Change(changeTo);
            return thisInput;
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

            //Filter String
            ClickListItem(component, "li.rz-multiselect-item", nameof(Employee.Title), "Sales Representative");
            Assert.Equal(6, GridRowCount(component));
            ChangeInuptValue(component, "span.rz-cell-filter-label > input", nameof(Employee.FirstName), null);
            Assert.Equal(_rows, GridRowCount(component));

            ////Filter Int
            //ChangeInuptValue(component, "span.rz-numeric > input", nameof(Employee.EmployeeID), 1);
            //Assert.Equal(1, GridRowCount(component));
            //ChangeInuptValue(component, "span.rz-numeric > input", nameof(Employee.EmployeeID), null);
            //Assert.Equal(_rows, GridRowCount(component));

            ////Filter Date
            //var dataFilter = component.FindAll("div.rz-date-filter").FirstOrDefault(x => x.OuterHtml.Contains(nameof(Employee.HireDate)));

        }

        private void ClickListItem<T>(IRenderedComponent<RadzenDataGrid<T>> component, string css, string colName, string value)
        {
            var col = component.FindComponents<RadzenDataGridColumn<T>>().FirstOrDefault(x => x.Instance.Title == colName);
            Assert.NotNull(col);
            col.Find("icon").Click();
            
            var inputelems = component.FindAll(css);
            var thisInput = inputelems.FirstOrDefault(x => x.OuterHtml.Contains(value));
            Assert.NotNull(thisInput);
            thisInput.Click();
        }


        #endregion
    }
}
