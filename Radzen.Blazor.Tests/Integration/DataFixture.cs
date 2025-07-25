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
        public static IRenderedComponent<RadzenDataGrid<T>> GetTestDataGrid<T>(this IEnumerable<T> data, TestContext ctx, DataTable table = null)
        {
            var component = ctx.RenderComponent<RadzenDataGrid<T>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<T>>(p => p.Data, data);
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<bool>(p => p.AllowSorting, true);
                parameterBuilder.Add<FilterMode>(p => p.FilterMode, FilterMode.Simple);
                if (typeof(T) == typeof(DataRow))
                {
                    Assert.NotNull(table);
                    parameterBuilder.Add<RenderFragment>(p => p.Columns, b => dataColumnBuilder<T>(b, table.Columns[nameof(Employee.EmployeeID)]));
                    parameterBuilder.Add<RenderFragment>(p => p.Columns, b => dataColumnBuilder<T>(b, table.Columns[nameof(Employee.FirstName)]));
                    parameterBuilder.Add<RenderFragment>(p => p.Columns, b => dataColumnBuilder<T>(b, table.Columns[nameof(Employee.LastName)]));
                    parameterBuilder.Add<RenderFragment>(p => p.Columns, b => dataColumnBuilder<T>(b, table.Columns[nameof(Employee.HireDate)]));
                }
                else
                {
                    parameterBuilder.Add<RenderFragment>(p => p.Columns, b => columnBuilder<T>(b, nameof(Employee.EmployeeID)));
                    parameterBuilder.Add<RenderFragment>(p => p.Columns, b => columnBuilder<T>(b, nameof(Employee.FirstName)));
                    parameterBuilder.Add<RenderFragment>(p => p.Columns, b => columnBuilder<T>(b, nameof(Employee.LastName)));
                    parameterBuilder.Add<RenderFragment>(p => p.Columns, b => columnBuilder<T>(b, nameof(Employee.HireDate)));
                }
            });

            var dg = component.Instance;
            var cols = dg.ColumnsCollection.Count;
            var rows = dg.Data.Count();
            Assert.Equal(Cols, cols);
            Assert.Equal(Rows, rows);
            return component;
        }

        private static RenderTreeBuilder columnBuilder<T>(RenderTreeBuilder builder, string property, bool sortable = true, bool filterable = true)
        {
            builder.OpenComponent(0, typeof(RadzenDataGridColumn<T>));
            builder.AddAttribute(1, "Title", property);
            builder.AddAttribute(2, "Property", property);
            builder.AddAttribute(3, "Filterable", filterable);
            builder.AddAttribute(4, "Sortable", sortable);
            builder.CloseComponent();
            return builder;
        }
        private static RenderTreeBuilder dataColumnBuilder<T>(RenderTreeBuilder builder, DataColumn dataCol, bool sortable = true, bool filterable = true)
        {
            builder.OpenComponent(0, typeof(RadzenDataGridColumn<T>));
            builder.AddAttribute(1, "Title", dataCol.ColumnName);
            builder.AddAttribute(2, "Property", $"ItemArray[{dataCol.Table.Columns.IndexOf(dataCol)}]");
            builder.AddAttribute(3, "Type", dataCol.DataType);
            builder.AddAttribute(4, "Filterable", filterable);
            builder.AddAttribute(5, "Sortable", sortable);
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
            new object[] {DataFixtureHelpers.GetEmployees<Employee>(),null },
            new object[] {DataFixtureHelpers.GetEmployees<Employee>().AsQueryable(),null },
            new object[] { DataFixtureHelpers.GetEmployees<Employee>().GetDataTable().AsEnumerable(), DataFixtureHelpers.GetEmployees<Employee>().GetDataTable() },
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
