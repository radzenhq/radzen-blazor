using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests.Integration
{
    public static class DataFixtureHelpers
    {
        public const int Rows = 9;
        public const int Cols = 6;
        public static IRenderedComponent<RadzenDataGrid<T>> GetTestDataGridIncCols<T>(this IEnumerable<T> data, TestContext ctx)
        {
            var component = ctx.RenderComponent<RadzenDataGrid<T>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<T>>(p => p.Data, data);
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
                parameterBuilder.Add<bool>(p => p.AllowSorting, true);
                parameterBuilder.Add<RenderFragment>(p => p.Columns, b => columnBuilder<T>(b, nameof(Employee.EmployeeID), data));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, b => columnBuilder<T>(b, nameof(Employee.FirstName), data));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, b => columnBuilder<T>(b, nameof(Employee.LastName), data));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, b => columnBuilder<T>(b, nameof(Employee.TitleOfCourtesy), data));
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


        public static RenderTreeBuilder columnBuilder<T>(this RenderTreeBuilder b, string property, IEnumerable<T> data, bool sortable = true, bool filterable = true, FilterMode? filterMode = null, object filterValue = null)
        {
            int sequ = 0;
            if (data is IEnumerable<DataRow> rows)
            {
                Assert.NotEmpty(rows);
                var table = (data.First() as DataRow).Table;
                Assert.NotNull(table);
                DataColumn dataCol = table.Columns[property];
                Assert.NotNull(dataCol);
                b.OpenComponent(sequ++, typeof(RadzenDataGridColumn<T>));
                b.AddAttribute(sequ++, "Title", dataCol.ColumnName);
                b.AddAttribute(sequ++, "Property", $"ItemArray[{dataCol.Table.Columns.IndexOf(dataCol)}]");
                b.AddAttribute(sequ++, "Type", dataCol.DataColType());
            }
            else
            {
                b.OpenComponent(sequ++, typeof(RadzenDataGridColumn<T>));
                b.AddAttribute(sequ++, "Title", property);
                b.AddAttribute(sequ++, "Property", property);
            }

            b.AddAttribute(sequ++, "Sortable", sortable);
            b.AddAttribute(sequ++, "Filterable", filterable);
            if (filterMode != null) b.AddAttribute(sequ++, "FilterMode", filterMode);
            if (filterValue != null) b.AddAttribute(sequ++, "FilterValue", filterValue);
            b.CloseComponent();
            return b;
        }

        public static DataTable GetDataTable<T>(this IEnumerable<T> employees)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);

            PropertyInfo[] properties = typeof(T).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                var col = new DataColumn();
                col.ColumnName = property.Name;
                col.DataType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                col.AllowDBNull = Nullable.GetUnderlyingType(property.PropertyType) != null || !property.PropertyType.IsValueType;
                dataTable.Columns.Add(col);
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
                var options = new JsonSerializerOptions
                {
                    Converters =
                        {
                            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                        }
                };
                var employees = JsonSerializer.Deserialize<IEnumerable<T>>(fs, options);
                return employees;
            }
        }

        public static Type DataColType(this DataColumn dataCol)
        {
            return dataCol.AllowDBNull && dataCol.DataType.IsValueType
                ? typeof(Nullable<>).MakeGenericType(dataCol.DataType)
                : dataCol.DataType;
        }
    }

    public class TestGridData : IEnumerable<object[]>
    {
        private readonly IEnumerable<object[]> _data = new List<object[]>
        {
            //new object[] {DataFixtureHelpers.GetEmployees<Employee>() },
            //new object[] {DataFixtureHelpers.GetEmployees<Employee>().AsQueryable()},
            //new object[] {DataFixtureHelpers.GetEmployees<Employee>().AsODataEnumerable()},
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
