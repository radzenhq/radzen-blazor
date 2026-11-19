using Bunit;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridNonColumnPropertyTests
    {
        class Row
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            internal string Secret { get; set; } = string.Empty;
        }

        static List<Row> Rows() => new()
        {
            new Row { Id = 1, Name = "A", City = "Sofia" },
            new Row { Id = 2, Name = "B", City = "Paris" },
            new Row { Id = 3, Name = "C", City = "Berlin" },
            new Row { Id = 4, Name = "D", City = "Paris" }
        };

        static TestContext CreateContext()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");
            return ctx;
        }

        static IRenderedComponent<RadzenDataGrid<Row>> Render(TestContext ctx, System.Action<ComponentParameterCollectionBuilder<RadzenDataGrid<Row>>> configure, params (string Name, object Value)[] nameColumnAttributes)
        {
            return ctx.RenderComponent<RadzenDataGrid<Row>>(builder =>
            {
                builder.Add(p => p.Data, Rows());
                builder.Add<RenderFragment>(p => p.Columns, columns =>
                {
                    columns.OpenComponent(0, typeof(RadzenDataGridColumn<Row>));
                    columns.AddAttribute(1, "Property", "Id");
                    columns.CloseComponent();

                    columns.OpenComponent(2, typeof(RadzenDataGridColumn<Row>));
                    columns.AddAttribute(3, "Property", "Name");
                    var sequence = 4;
                    foreach (var attribute in nameColumnAttributes)
                    {
                        columns.AddAttribute(sequence++, attribute.Name, attribute.Value);
                    }
                    columns.CloseComponent();
                });
                configure(builder);
            });
        }

        static string[] Ids(IRenderedComponent<RadzenDataGrid<Row>> component) =>
            component.FindAll("tbody tr.rz-data-row").Select(row => row.QuerySelector("td")!.TextContent.Trim()).ToArray();

        [Fact]
        public void DataGrid_SortsByPropertyWithoutColumn()
        {
            using var ctx = CreateContext();

            var component = Render(ctx, builder => builder.Add(p => p.AllowSorting, true),
                ("SortProperty", "City"), ("SortOrder", SortOrder.Ascending));

            Assert.Equal(new[] { "3", "2", "4", "1" }, Ids(component));
        }

        [Fact]
        public void DataGrid_FiltersByPropertyWithoutColumn()
        {
            using var ctx = CreateContext();

            var component = Render(ctx, builder => builder.Add(p => p.AllowFiltering, true),
                ("FilterProperty", "City"), ("FilterValue", "Paris"));

            Assert.Equal(new[] { "2", "4" }, Ids(component));
        }

        [Fact]
        public async Task DataGrid_LoadSettings_KeepsGroupsOfPublicPropertiesOnly()
        {
            using var ctx = CreateContext();

            var component = Render(ctx, builder =>
            {
                builder.Add(p => p.AllowGrouping, true);
                builder.Add(p => p.SettingsChanged, EventCallback.Factory.Create<DataGridSettings>(ctx.Renderer, _ => { }));
            });
            var grid = component.Instance;

            await component.InvokeAsync(() => grid.Settings = new DataGridSettings
            {
                Groups = new[]
                {
                    new GroupDescriptor { Property = "City" },
                    new GroupDescriptor { Property = "Unknown" },
                    new GroupDescriptor { Property = "Secret" }
                }
            });
            await component.InvokeAsync(() => grid.ReloadSettings(true));
            component.Render();

            Assert.Equal(new[] { "City" }, grid.Groups.Select(g => g.Property).ToArray());
            Assert.Equal(3, component.FindAll("tbody tr.rz-group-row").Count);
        }

        [Fact]
        public async Task DataGrid_LoadData_SortAndFilterStringsForPropertyWithoutColumn_CanBeApplied()
        {
            using var ctx = CreateContext();
            LoadDataArgs captured = null!;

            var component = Render(ctx, builder =>
            {
                builder.Add(p => p.AllowSorting, true);
                builder.Add(p => p.AllowFiltering, true);
                builder.Add(p => p.Count, 4);
                builder.Add<LoadDataArgs>(p => p.LoadData, args => captured = args);
            }, ("SortProperty", "City"), ("SortOrder", SortOrder.Descending), ("FilterProperty", "City"), ("FilterValue", "a"));

            await component.InvokeAsync(() => component.Instance.Reload());

            Assert.NotNull(captured);

            var result = Rows().AsQueryable().Where(captured.Filter).OrderBy(captured.OrderBy).ToList();

            Assert.Equal(new[] { 1, 2, 4 }, result.Select(row => row.Id).ToArray());
        }
    }
}
