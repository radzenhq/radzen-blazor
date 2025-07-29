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
using System.Diagnostics;
using System.IO;
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
        public void FilterTestSimpleAsync<T>(IEnumerable<T> data)
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
                    b.columnBuilder<T>(nameof(Employee.LastName), data, filterMode: FilterMode.Simple);
                    b.columnBuilder<T>(nameof(Employee.TitleOfCourtesy), data, filterMode: FilterMode.Simple);
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
            Assert.Equal(3, GridRowCount(component));
            ChangeDatetimeValueAsync(component, nameof(Employee.HireDate), null, 9);
            Assert.Equal(_rows, GridRowCount(component));

        }


        [Fact]
        public void NullableEnumPocoTest()
        {
            var data = Enumerable.Range(0, 9).Select(i =>
            new EmployeeEnums
            {
                ID = i,
                Gender = i < 3 ? GenderType.Mr : i < 6 ? GenderType.Ms : GenderType.Unknown,
                Status = i < 3 ? StatusType.Active : i < 6 ? StatusType.Inactive : null,
                Color = i < 2 ? ColorType.Red : i < 4 ? ColorType.AlmondGreen : i < 6 ? ColorType.AppleBlueSeaGreen : ColorType.AzureBlue,
            });

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");


            var component = data.GetTestDataGrid(ctx);
            component.SetParametersAndRender(p =>
                p.Add(s => s.Columns, b =>
                {
                    b.columnBuilder<EmployeeEnums>(nameof(EmployeeEnums.ID), data);
                    b.columnBuilder<EmployeeEnums>(nameof(EmployeeEnums.Gender), data);
                    b.columnBuilder<EmployeeEnums>(nameof(EmployeeEnums.Status), data);
                    b.columnBuilder<EmployeeEnums>(nameof(EmployeeEnums.Color), data);
                }));
            var dg = component.Instance;
            var cols = dg.ColumnsCollection.Count;
            var rows = dg.Data.Count();
            Assert.Equal(4, cols);
            Assert.Equal(9, rows);

            var cells = component.FindAll(".rz-cell-data");
            Assert.Contains(cells, x => x.InnerHtml == "Mr");
            Assert.Contains(cells, x => x.InnerHtml == "Inactive");
            Assert.Contains(cells, x => x.InnerHtml == "Almond Green");
            Assert.Contains(cells, x => x.InnerHtml == "");
        }

        [Fact]
        public void NullableEnumTableTest()
        {
            var dataValues = Enumerable.Range(0, 9).Select(i =>
            new EmployeeEnums
            {
                ID = i,
                Gender = i < 3 ? GenderType.Mr : i < 6 ? GenderType.Ms : GenderType.Unknown,
                Status = i < 3 ? StatusType.Active : i < 6 ? StatusType.Inactive : null,
                Color = i < 2 ? ColorType.Red : i < 4 ? ColorType.AlmondGreen : i < 6 ? ColorType.AppleBlueSeaGreen : ColorType.AzureBlue,
            });

            IEnumerable<DataRow> data = dataValues.GetDataTable().AsEnumerable();

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");


            var component = data.GetTestDataGrid(ctx);
            component.SetParametersAndRender(p =>
                p.Add(s => s.Columns, b =>
                {
                    b.columnBuilder(nameof(EmployeeEnums.ID), data);
                    b.columnBuilder(nameof(EmployeeEnums.Gender), data);
                    b.columnBuilder(nameof(EmployeeEnums.Status), data);
                    b.columnBuilder(nameof(EmployeeEnums.Color), data);
                }));
            var dg = component.Instance;
            var cols = dg.ColumnsCollection.Count;
            var rows = dg.Data.Count();
            Assert.Equal(4, cols);
            Assert.Equal(9, rows);

            var cells = component.FindAll(".rz-cell-data");
            Assert.Contains(cells, x => x.InnerHtml == "Mr");
            Assert.Contains(cells, x => x.InnerHtml == "Inactive");
            Assert.Contains(cells, x => x.InnerHtml == "Almond Green");
            Assert.Contains(cells, x => x.InnerHtml == "");

            //Check Sorting works with null values
            var sortHead = component.FindAll(".rz-column-title")
                .FirstOrDefault(x => x.TextContent.Contains("Status"));
            Assert.NotNull(sortHead);

            cells = component.FindAll(".rz-cell-data");
            Assert.Equal("0", cells[0 * cols].TextContent.Trim());
            Assert.Equal("1", cells[1 * cols].TextContent.Trim());
            Assert.Equal("2", cells[2 * cols].TextContent.Trim());
            Assert.Equal("3", cells[3 * cols].TextContent.Trim());

            sortHead.Click();
            cells = component.FindAll(".rz-cell-data");
            Assert.Equal("6", cells[0 * cols].TextContent.Trim());
            Assert.Equal("7", cells[1 * cols].TextContent.Trim());
            Assert.Equal("8", cells[2 * cols].TextContent.Trim());
            Assert.Equal("3", cells[3 * cols].TextContent.Trim());
        }

        [Fact]
        public void NullableEnumTableFilterTest()
        {
            var dataValues = Enumerable.Range(0, 9).Select(i =>
            new EmployeeEnums
            {
                ID = i,
                Gender = i < 3 ? GenderType.Mr : i < 6 ? GenderType.Ms : GenderType.Unknown,
                Status = i < 3 ? StatusType.Active : i < 6 ? StatusType.Inactive : null,
                Color = i < 2 ? ColorType.Red : i < 4 ? ColorType.AlmondGreen : i < 6 ? ColorType.AppleBlueSeaGreen : ColorType.AzureBlue,
            });

            IEnumerable<DataRow> data = dataValues.GetDataTable().AsEnumerable();

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");


            var component = data.GetTestDataGrid(ctx);
            component.SetParametersAndRender(p =>
                p.Add(s => s.Columns, b =>
                {
                    b.columnBuilder(nameof(EmployeeEnums.ID), data);
                    b.columnBuilder(nameof(EmployeeEnums.Gender), data, filterable: true, filterMode: FilterMode.CheckBoxList);
                    b.columnBuilder(nameof(EmployeeEnums.Status), data, filterable: true, filterMode: FilterMode.CheckBoxList);
                    b.columnBuilder(nameof(EmployeeEnums.Color), data, filterable: true, filterMode: FilterMode.CheckBoxList);
                }));
            var dg = component.Instance;
            var cols = dg.ColumnsCollection.Count;
            var rows = dg.Data.Count();
            Assert.Equal(4, cols);
            Assert.Equal(9, rows);

            RadzenDataGrid<DataRow> grid;
            var cells = component.FindAll(".rz-cell-data");
            Assert.Contains(cells, x => x.InnerHtml == "Mr");
            Assert.Contains(cells, x => x.InnerHtml == "Ms");

            //Filter Enum
            grid = component.Instance;
            SetEnumDropdown(grid, "li.rz-dropdown-item", nameof(EmployeeEnums.Gender), new[] { GenderType.Mr });
            component.WaitForAssertion(() =>
            {
                var view = grid.View;
                Assert.Equal(3, view.Count());
            }, timeout: TimeSpan.FromSeconds(1));

            cells = component.FindAll(".rz-cell-data");
            Assert.Contains(cells, x => x.InnerHtml == "Mr");
            Assert.DoesNotContain(cells, x => x.InnerHtml == "Ms");
            component.Dispose();

            component = data.GetTestDataGrid(ctx);
            component.SetParametersAndRender(p =>
                p.Add(s => s.Columns, b =>
                {
                    b.columnBuilder(nameof(EmployeeEnums.ID), data);
                    b.columnBuilder(nameof(EmployeeEnums.Gender), data, filterable: true, filterMode: FilterMode.CheckBoxList);
                    b.columnBuilder(nameof(EmployeeEnums.Status), data, filterable: true, filterMode: FilterMode.CheckBoxList);
                    b.columnBuilder(nameof(EmployeeEnums.Color), data, filterable: true, filterMode: FilterMode.CheckBoxList);
                }));


            cells = component.FindAll(".rz-cell-data");
            Assert.Contains(cells, x => x.InnerHtml == "Active");
            Assert.Contains(cells, x => x.InnerHtml == "Inactive");

            //Filter Enum
            grid = component.Instance;
            SetEnumDropdown(grid, "li.rz-dropdown-item", nameof(EmployeeEnums.Status), new  StatusType?[] { StatusType.Active });
            component.WaitForAssertion(() =>
            {
                var view = grid.View;
                Assert.Equal(3, view.Count());
            }, timeout: TimeSpan.FromSeconds(1));

            cells = component.FindAll(".rz-cell-data");
            Assert.Contains(cells, x => x.InnerHtml == "Active");
            Assert.DoesNotContain(cells, x => x.InnerHtml == "Inactive");
            component.Dispose();
        }

        [Theory]
        [ClassData(typeof(TestGridData))]
        public void FilterEnumTestSimpleAsync<T>(IEnumerable<T> data)
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
                    b.columnBuilder<T>(nameof(Employee.LastName), data, filterMode: FilterMode.Simple);
                    b.columnBuilder<T>(nameof(Employee.TitleOfCourtesy), data, filterMode: FilterMode.Simple);
                    b.columnBuilder<T>(nameof(Employee.Title), data, filterMode: FilterMode.Simple);
                    b.columnBuilder<T>(nameof(Employee.HireDate), data, filterMode: FilterMode.Simple);
                }));

            var dg = component.Instance;
            var cols = dg.ColumnsCollection.Count;
            var rows = dg.Data.Count();
            Assert.Equal(_cols, cols);
            Assert.Equal(_rows, rows);

            var cells = component.FindAll(".rz-cell-data");
            Assert.Contains(cells, x => x.InnerHtml == "Mr");
            Assert.Contains(cells, x => x.InnerHtml == "Ms");

            RadzenDataGrid<T> grid;

            //Filter Enum
            grid = component.Instance;
            SetEnumDropdown(grid, "li.rz-dropdown-item", nameof(Employee.TitleOfCourtesy),  CourtesyEnum.Mr);
            component.WaitForAssertion(() =>
            {
                var view = grid.View;
                Assert.Equal(3, view.Count());
            }, timeout: TimeSpan.FromSeconds(1));

            cells = component.FindAll(".rz-cell-data");
            Assert.Contains(cells, x => x.InnerHtml == "Mr");
            Assert.DoesNotContain(cells, x => x.InnerHtml == "Ms");
            component.Dispose();
        }

        private void SetEnumDropdown<T, V>(RadzenDataGrid<T> grid, string css, string colName, V changeTo)
        {
            var col = grid.ColumnsCollection.OfType<RadzenDataGridColumn<T>>().FirstOrDefault(c => c.Title == colName);
            col.FilterOperator = FilterOperator.Equals;
            col.FilterValue = changeTo;

            grid.Reload();

        }

        private IHtmlInputElement ChangeInuptValue<T>(IRenderedComponent<RadzenDataGrid<T>> component, string css, string colName, object changeTo)
        {
            var inputelems = component.FindAll(css);
            IHtmlInputElement thisInput = inputelems.FirstOrDefault(x => x.OuterHtml.Contains(colName)) as IHtmlInputElement;
            Assert.NotNull(thisInput);
            thisInput.Change(changeTo);
            return thisInput;
        }

        private void ChangeDatetimeValueAsync<T>(IRenderedComponent<RadzenDataGrid<T>> component, string colName, DateTime? changeTo, int expected)
        {
            RadzenDataGrid<T> grid = component.Instance;
            var col = grid.ColumnsCollection.OfType<RadzenDataGridColumn<T>>().FirstOrDefault(c => c.Title == colName);
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
                    b.columnBuilder<T>(nameof(Employee.LastName), data, filterMode: FilterMode.CheckBoxList);
                    b.columnBuilder<T>(nameof(Employee.TitleOfCourtesy), data, filterMode: FilterMode.CheckBoxList);
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
                    b.columnBuilder<T>(nameof(Employee.LastName), data, filterMode: FilterMode.CheckBoxList);
                    b.columnBuilder<T>(nameof(Employee.TitleOfCourtesy), data, filterMode: FilterMode.CheckBoxList);
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
                    b.columnBuilder<T>(nameof(Employee.LastName), data, filterMode: FilterMode.CheckBoxList);
                    b.columnBuilder<T>(nameof(Employee.TitleOfCourtesy), data, filterMode: FilterMode.CheckBoxList);
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

        private void SetFilterList<T, V>(RadzenDataGrid<T> grid, string css, string colName, V[] changeTo)
        {
            var col = grid.ColumnsCollection.OfType<RadzenDataGridColumn<T>>().FirstOrDefault(c => c.Title == colName);
            col.FilterOperator = FilterOperator.Equals;
            col.FilterValue = changeTo;

            grid.Reload();

        }


        #endregion

        #region filter != property
        [Theory]
        [ClassData(typeof(TestGridData))]
        public void FilterOnDifferentPropAsync<T>(IEnumerable<T> data)
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
                    b.columnBuilder<T>(nameof(Employee.HireDate), data, filterMode: FilterMode.Simple);

                    int sequ = 0;
                    b.OpenComponent(sequ++, typeof(RadzenDataGridColumn<T>));
                    b.AddAttribute(sequ++, "Title", nameof(Employee.LastName));
                    b.AddAttribute(sequ++, "Sortable", true);
                    b.AddAttribute(sequ++, "Filterable", true);
                    b.AddAttribute(sequ++, "FilterMode", FilterMode.Simple);
                    if (data is IEnumerable<DataRow> rows)
                    {
                        Assert.NotEmpty(rows);
                        var table = (data.First() as DataRow).Table;
                        Assert.NotNull(table);
                        DataColumn dataCol = table.Columns["LastName"];
                        Assert.NotNull(dataCol);
                        b.AddAttribute(sequ++, "Property", $"ItemArray[{dataCol.Table.Columns.IndexOf(dataCol)}]");
                        b.AddAttribute(sequ++, "Type", dataCol.DataColType());
                        Debug.Print($"DataRow: {sequ}");
                    }
                    else
                    {
                        b.AddAttribute(sequ++, "Property", nameof(Employee.LastName));
                        b.AddAttribute(sequ++, "FilterProperty", nameof(Employee.FirstName));
                        Debug.Print($"Emplyee: {sequ}");
                    }
                    b.CloseComponent();
                }));

            var dg = component.Instance;
            var cols = dg.ColumnsCollection.Count;
            var rows = dg.Data.Count();
            Assert.Equal(4, cols);
            Assert.Equal(_rows, rows);


        }
        #endregion
    }
}
