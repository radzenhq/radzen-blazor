using Bunit;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridColumnParameterViewTests
    {
        class Item
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        static IRenderedComponent<RadzenDataGrid<Item>> Render(TestContext ctx)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var data = new List<Item>
            {
                new Item { Id = 2, Name = "Bravo" },
                new Item { Id = 1, Name = "Alpha" },
            };

            return ctx.RenderComponent<RadzenDataGrid<Item>>(pb =>
            {
                pb.Add(p => p.LoadData, async args => { await Task.Delay(5); });
                pb.Add(p => p.Data, data);
                pb.Add(p => p.AllowSorting, true);
                pb.Add(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenDataGridColumn<Item>>(0);
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Item>.Property), nameof(Item.Name));
                    b.AddAttribute(2, nameof(RadzenDataGridColumn<Item>.SortOrder), SortOrder.Ascending);
                    b.AddAttribute(3, nameof(RadzenDataGridColumn<Item>.Template), (RenderFragment<Item>)(item => cb => cb.AddContent(0, item.Name)));
                    b.CloseComponent();
                    b.OpenComponent<RadzenDataGridColumn<Item>>(3);
                    b.AddAttribute(4, nameof(RadzenDataGridColumn<Item>.Property), nameof(Item.Id));
                    b.CloseComponent();
                });
            });
        }

        [Fact]
        public async Task ColumnInitialSortOrder_ReappliedAfterReset_WithAsyncLoadData_DoesNotThrowExpiredParameterView()
        {
            using var ctx = new TestContext();
            var cut = Render(ctx);

            await cut.InvokeAsync(() => cut.Instance.Reset(true));
            cut.Render();
            await Task.Delay(200);

            if (ctx.Renderer.UnhandledException.IsCompleted)
            {
                var exception = await ctx.Renderer.UnhandledException;
                Assert.True(false, $"Unhandled renderer exception: {exception}");
            }

            cut.Render();
            Assert.NotEmpty(cut.FindAll("tr.rz-data-row"));
        }
    }
}
