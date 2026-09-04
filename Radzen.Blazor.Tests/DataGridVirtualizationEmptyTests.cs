using Bunit;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridVirtualizationEmptyTests
    {
        class Item
        {
            public int Id { get; set; }
        }

        static IRenderedComponent<RadzenDataGrid<Item>> Render(TestContext ctx, IEnumerable<Item> data)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            return ctx.RenderComponent<RadzenDataGrid<Item>>(p =>
            {
                p.Add(g => g.Data, data);
                p.Add(g => g.AllowVirtualization, true);
                p.Add(g => g.AllowPaging, true);
                p.Add(g => g.ShowPagingSummary, true);
                p.Add(g => g.PageSize, 10);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent<RadzenDataGridColumn<Item>>(0);
                    builder.AddAttribute(1, "Property", "Id");
                    builder.CloseComponent();
                });
            });
        }

        [Fact]
        public void EmptyVirtualizedGridReportsZeroItems()
        {
            using var ctx = new TestContext();

            var cut = Render(ctx, new List<Item>());

            Assert.Equal(0, cut.Instance.Count);
            Assert.DoesNotContain("1 items", cut.Markup);
        }

        [Fact]
        public void EmptyVirtualizedGridReportsZeroItemsAfterReload()
        {
            using var ctx = new TestContext();

            var cut = Render(ctx, Enumerable.Range(1, 25).Select(i => new Item { Id = i }).ToList());

            cut.WaitForAssertion(() => Assert.Equal(25, cut.Instance.Count));

            cut.SetParametersAndRender(p => p.Add(g => g.Data, new List<Item>()));

            Assert.Equal(0, cut.Instance.Count);
        }

        [Fact]
        public void VirtualizedGridStillCountsItems()
        {
            using var ctx = new TestContext();

            var cut = Render(ctx, Enumerable.Range(1, 25).Select(i => new Item { Id = i }).ToList());

            cut.WaitForAssertion(() => Assert.Equal(25, cut.Instance.Count));
            Assert.Contains("25 items", cut.Markup);
        }
    }
}
