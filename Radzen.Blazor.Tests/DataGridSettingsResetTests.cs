using Bunit;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridSettingsResetTests
    {
        class Row
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
        }

        // Declared out of their markup order on purpose: the rendered order is Name, City, Id, and
        // the declaration order is Id, Name, City. A reset that reads OrderIndex gives the first;
        // one that rebuilds the column list without reading it gives the second.
        static IRenderedComponent<RadzenDataGrid<Row>> Render(TestContext ctx)
        {
            return ctx.RenderComponent<RadzenDataGrid<Row>>(builder =>
            {
                builder.Add(p => p.Data, new List<Row> { new Row { Id = 1, Name = "One", City = "Paris" } });
                builder.Add(p => p.AllowColumnReorder, true);
                builder.Add(p => p.SettingsChanged, EventCallback.Factory.Create<DataGridSettings>(ctx.Renderer, _ => { }));
                builder.Add<RenderFragment>(p => p.Columns, columns =>
                {
                    columns.OpenComponent(0, typeof(RadzenDataGridColumn<Row>));
                    columns.AddAttribute(1, "Property", "Id");
                    columns.AddAttribute(2, "Title", "Id");
                    columns.AddAttribute(3, "OrderIndex", 2);
                    columns.CloseComponent();

                    columns.OpenComponent(4, typeof(RadzenDataGridColumn<Row>));
                    columns.AddAttribute(5, "Property", "Name");
                    columns.AddAttribute(6, "Title", "Name");
                    columns.AddAttribute(7, "OrderIndex", 0);
                    columns.CloseComponent();

                    columns.OpenComponent(8, typeof(RadzenDataGridColumn<Row>));
                    columns.AddAttribute(9, "Property", "City");
                    columns.AddAttribute(10, "Title", "City");
                    columns.AddAttribute(11, "OrderIndex", 1);
                    columns.CloseComponent();
                });
            });
        }

        static string[] HeaderTitles(IRenderedComponent<RadzenDataGrid<Row>> component) =>
            component.FindAll("th .rz-column-title-content")
                .Select(cell => cell.TextContent.Trim())
                .ToArray();

        [Fact]
        public async Task DataGrid_SettingsSetToNull_RestoresDeclaredOrderIndex()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = Render(ctx);
            var grid = component.Instance;

            Assert.Equal(new[] { "Name", "City", "Id" }, HeaderTitles(component));

            // The state a reset is undoing: something was stored, so the setter's null branch runs.
            await component.InvokeAsync(() => grid.Settings = new DataGridSettings());
            await component.InvokeAsync(() => grid.Settings = null);

            component.Render();

            // Not Id, Name, City - that is the order the columns were declared in, which is what a
            // reset that rebuilds the list from allColumns without ordering it produces.
            Assert.Equal(new[] { "Name", "City", "Id" }, HeaderTitles(component));
        }
    }
}
