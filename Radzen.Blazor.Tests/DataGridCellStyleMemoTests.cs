using Bunit;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridCellStyleMemoTests
    {
        class Item
        {
            public int Id { get; set; }
        }

        static IRenderedComponent<RadzenDataGrid<Item>> Render(TestContext ctx)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            return ctx.RenderComponent<RadzenDataGrid<Item>>(parameterBuilder =>
            {
                parameterBuilder.Add(p => p.Data, new List<Item> { new() { Id = 1 }, new() { Id = 2 } });
                parameterBuilder.Add(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Item>));
                    builder.AddAttribute(1, nameof(RadzenDataGridColumn<Item>.Property), "Id");
                    builder.AddAttribute(2, nameof(RadzenDataGridColumn<Item>.TextAlign), TextAlign.Right);
                    builder.CloseComponent();
                });
            });
        }

        [Fact]
        public void GetStyle_ReturnsCellStyle_AndReusesTheSameStringAcrossCells()
        {
            using var ctx = new TestContext();
            var column = Render(ctx).Instance.ColumnsCollection.Single();

            var first = column.GetStyle(forCell: true);
            var second = column.GetStyle(forCell: true);

            Assert.Contains("text-align:right", first);
            // The row-independent data-cell style is memoized, so repeated calls reuse the same string.
            Assert.Same(first, second);
        }

        [Fact]
        public void GetStyle_Memo_InvalidatesWhenInputChanges()
        {
            using var ctx = new TestContext();
            var column = Render(ctx).Instance.ColumnsCollection.Single();

            Assert.Contains("text-align:right", column.GetStyle(forCell: true));

            column.TextAlign = TextAlign.Center;

            Assert.Contains("text-align:center", column.GetStyle(forCell: true));
        }
    }
}
