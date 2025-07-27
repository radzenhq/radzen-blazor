using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests.Integration
{
    public static class DataFixtureHelpers
    {
        public const int Rows = 9;
        public const int Cols = 4;
        public static IRenderedComponent<RadzenDataGrid<T>> GetTestDataGridIncCols<T>(this IEnumerable<T> data, TestContext ctx)
        {
            var component = ctx.RenderComponent<RadzenDataGrid<T>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<T>>(p => p.Data, data);
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<bool>(p => p.AllowSorting, true);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, b => columnBuilder<T>(b, nameof(Employee.EmployeeID), data));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, b => columnBuilder<T>(b, nameof(Employee.FirstName), data));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, b => columnBuilder<T>(b, nameof(Employee.Title), data));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, b => columnBuilder<T>(b, nameof(Employee.HireDate), data));

            });

            var dg = component.Instance;
            var cols = dg.ColumnsCollection.Count;
            var rows = dg.Data.Count();
            Assert.Equal(Cols, cols);
            Assert.Equal(Rows, rows);
            return component;
        }
        public static IRenderedComponent<RadzenDataGrid<T>> GetTestDataGrid<T>(this IEnumerable<T> data, TestContext ctx)
        {
            var component = ctx.RenderComponent<RadzenDataGrid<T>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<T>>(p => p.Data, data);
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<bool>(p => p.AllowSorting, true);
            });

            var dg = component.Instance;
            var cols = dg.ColumnsCollection.Count;
            var rows = dg.Data.Count();
            Assert.Equal(0, cols);
            Assert.Equal(Rows, rows);
            return component;
        }


        public static RenderTreeBuilder columnBuilder<T>(this RenderTreeBuilder builder, string property, IEnumerable<T> data, bool sortable = true, bool filterable = true, FilterMode? filterMode = null, object filterValue = null)
        {
            int sequ = 0;
            if (data is IEnumerable<DataRow> rows)
            {
                Assert.NotEmpty(rows);
                var table = (data.First() as DataRow).Table;
                Assert.NotNull(table);
                DataColumn dataCol = table.Columns[property];
                Assert.NotNull(dataCol);
                builder.OpenComponent(sequ++, typeof(RadzenDataGridColumn<T>));
                builder.AddAttribute(sequ++, "Title", dataCol.ColumnName);
                builder.AddAttribute(sequ++, "Property", $"ItemArray[{dataCol.Table.Columns.IndexOf(dataCol)}]");
                builder.AddAttribute(sequ++, "Type", dataCol.DataType);
            }
            else
            {
                builder.OpenComponent(sequ++, typeof(RadzenDataGridColumn<T>));
                builder.AddAttribute(sequ++, "Title", property);
                builder.AddAttribute(sequ++, "Property", property);
            }

            builder.AddAttribute(sequ++, "Sortable", sortable);
            builder.AddAttribute(sequ++, "Filterable", filterable);
            if (filterMode != null) builder.AddAttribute(sequ++, "FilterMode", filterMode);
            if (filterValue != null) builder.AddAttribute(sequ++, "FilterValue", filterValue);
            builder.CloseComponent();
            return builder;
        }


        public static DataTable GetDataTable<T>(this IEnumerable<T> employees)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);

            PropertyInfo[] properties = typeof(T).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                dataTable.Columns.Add(property.Name, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
            }
            foreach (T item in employees)
            {
                DataRow row = dataTable.NewRow();
                foreach (PropertyInfo property in properties)
                {
                    row[property.Name] = property.GetValue(item, null) ?? DBNull.Value;
                }
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        public static IEnumerable<T> GetEmployees<T>()
        {
            using (FileStream fs = new FileStream(@"Integration\Employees.json", FileMode.Open, FileAccess.Read))
            {
                var employees = JsonSerializer.Deserialize<IEnumerable<T>>(fs);
                return employees;
            }
        }
    }

    public class TestGridData : IEnumerable<object[]>
    {
        private readonly IEnumerable<object[]> _data = new List<object[]>
        {
            new object[] {DataFixtureHelpers.GetEmployees<Employee>() },
            new object[] {DataFixtureHelpers.GetEmployees<Employee>().AsQueryable()},
            new object[] {DataFixtureHelpers.GetEmployees<Employee>().GetDataTable().AsEnumerable()},
        };

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class TestGridFilterData : IEnumerable<object[]>
    {
        private readonly IEnumerable<object[]> _data = new List<object[]>
        {
            new object[] {DataFixtureHelpers.GetEmployees<Employee>() },
            new object[] {DataFixtureHelpers.GetEmployees<Employee>().AsQueryable()},
            new object[] {DataFixtureHelpers.GetEmployees<Employee>().GetDataTable().AsEnumerable()},
        };

        public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class DataFixture
    {
        protected const int _rows = DataFixtureHelpers.Rows;
        protected const int _cols = DataFixtureHelpers.Cols;

        public DataFixture()
        {
        }

    }
}
