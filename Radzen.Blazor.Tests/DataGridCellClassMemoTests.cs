using Bunit;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridCellClassMemoTests
    {
        class Item
        {
            public int Id { get; set; }
        }

        static IRenderedComponent<RadzenDataGrid<Item>> Render(TestContext ctx, Action<RenderTreeBuilderColumn> configure)
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
                    configure(new RenderTreeBuilderColumn(builder));
                    builder.CloseComponent();
                });
            });
        }

        readonly struct RenderTreeBuilderColumn(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            public void Add(string name, object value) => builder.AddAttribute(10, name, value);
        }

        [Fact]
        public void CachedCellCssClass_ReturnsCssClass_AndReusesTheSameStringAcrossCells()
        {
            using var ctx = new TestContext();
            var column = Render(ctx, c => c.Add(nameof(RadzenDataGridColumn<Item>.CssClass), "my-cls")).Instance.ColumnsCollection.Single();

            var first = column.GetCachedCellCssClass("", "");
            var second = column.GetCachedCellCssClass("", "");

            Assert.Equal("my-cls", first);
            Assert.Same(first, second);
        }

        [Fact]
        public void CachedCellCssClass_InvalidatesWhenCssClassChanges()
        {
            using var ctx = new TestContext();
            var column = Render(ctx, c => c.Add(nameof(RadzenDataGridColumn<Item>.CssClass), "my-cls")).Instance.ColumnsCollection.Single();

            Assert.Equal("my-cls", column.GetCachedCellCssClass("", ""));

            column.CssClass = "other";

            Assert.Equal("other", column.GetCachedCellCssClass("", ""));
        }

        [Fact]
        public void CachedCellCssClass_InvalidatesWhenFrozenOrCompositeChanges()
        {
            using var ctx = new TestContext();
            var column = Render(ctx, c => c.Add(nameof(RadzenDataGridColumn<Item>.CssClass), "my-cls")).Instance.ColumnsCollection.Single();

            Assert.Equal("my-cls", column.GetCachedCellCssClass("", ""));
            Assert.Equal("my-cls rz-composite-cell", column.GetCachedCellCssClass("", "rz-composite-cell"));
        }

        [Fact]
        public void DataCell_RendersColumnCssClass()
        {
            using var ctx = new TestContext();
            var cut = Render(ctx, c => c.Add(nameof(RadzenDataGridColumn<Item>.CssClass), "my-cls"));

            var cells = cut.FindAll("td[role=\"gridcell\"]");
            Assert.NotEmpty(cells);
            Assert.All(cells, cell => Assert.Contains("my-cls", cell.GetAttribute("class") ?? ""));
        }

        [Fact]
        public void CalculatedCssClass_AppliesDistinctClassPerRow()
        {
            using var ctx = new TestContext();
            Func<RadzenDataGridColumn<Item>, Item, string> calc = (_, item) => item.Id == 1 ? "cls-a" : "cls-b";
            var cut = Render(ctx, c => c.Add(nameof(RadzenDataGridColumn<Item>.CalculatedCssClass), calc));

            var classes = cut.FindAll("td[role=\"gridcell\"]").Select(c => c.GetAttribute("class") ?? "").ToList();
            Assert.Contains(classes, c => c.Contains("cls-a"));
            Assert.Contains(classes, c => c.Contains("cls-b"));
        }

        [Fact]
        public void ShowCellDataAsTooltip_RendersTitle_WithLazySpanAttributes()
        {
            using var ctx = new TestContext();
            var cut = Render(ctx, c => c.Add(nameof(RadzenDataGridColumn<Item>.ShowCellDataAsTooltip), true));

            var titled = cut.FindAll("span[title]");
            Assert.Contains(titled, s => s.GetAttribute("title") == "1");
            Assert.Contains(titled, s => s.GetAttribute("title") == "2");
        }
    }
}
