using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridSelectionHighlightTests
    {
        class Order
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        static List<Order> Orders() => Enumerable.Range(1, 3).Select(i => new Order { Id = i, Name = "Order " + i }).ToList();

        static void Parameters(ComponentParameterCollectionBuilder<RadzenDataGrid<Order>> pb, List<Order> data, IList<Order> selected, bool valueFirst, string keyProperty, Action<RowRenderEventArgs<Order>> rowRender)
        {
            if (keyProperty != null)
            {
                pb.Add(p => p.KeyProperty, keyProperty);
            }

            if (valueFirst)
            {
                AddValue(pb, selected);
            }

            pb.Add(p => p.Data, data);
            pb.Add(p => p.AllowColumnResize, true);
            pb.Add(p => p.SelectionMode, DataGridSelectionMode.Single);

            if (!valueFirst)
            {
                AddValue(pb, selected);
            }

            if (rowRender != null)
            {
                pb.Add(p => p.RowRender, rowRender);
            }

            pb.Add(p => p.Columns, b =>
            {
                b.OpenComponent<RadzenDataGridColumn<Order>>(0);
                b.AddAttribute(1, nameof(RadzenDataGridColumn<Order>.Property), nameof(Order.Name));
                b.CloseComponent();
            });
        }

        static void AddValue(ComponentParameterCollectionBuilder<RadzenDataGrid<Order>> pb, IList<Order> selected)
        {
            pb.Add(p => p.Value, selected);
            pb.Add(p => p.ValueChanged, EventCallback.Factory.Create<IList<Order>>(new object(), (IList<Order> _) => { }));
        }

        static IRenderedComponent<RadzenDataGrid<Order>> Render(TestContext ctx, List<Order> data, IList<Order> selected, bool valueFirst, string keyProperty, Action<RowRenderEventArgs<Order>> rowRender = null)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            return ctx.RenderComponent<RadzenDataGrid<Order>>(pb => Parameters(pb, data, selected, valueFirst, keyProperty, rowRender));
        }

        static void AssertHighlighted(IRenderedComponent<RadzenDataGrid<Order>> grid, string text)
        {
            var highlighted = grid.FindAll("tr.rz-state-highlight");

            Assert.Single(highlighted);
            Assert.Contains(text, highlighted[0].TextContent);
        }

        [Theory]
        [InlineData(false, null)]
        [InlineData(true, null)]
        [InlineData(false, "Id")]
        [InlineData(true, "Id")]
        public void DataAndValue_SetInSameRender_AfterNullData_HighlightsRow(bool valueFirst, string keyProperty)
        {
            using var ctx = new TestContext();

            var grid = Render(ctx, null, null, valueFirst, keyProperty);

            Assert.Empty(grid.FindAll("tr.rz-state-highlight"));

            var data = Orders();

            grid.SetParametersAndRender(pb => Parameters(pb, data, new List<Order> { data[1] }, valueFirst, keyProperty, null));

            AssertHighlighted(grid, "Order 2");
            Assert.Same(data[1], grid.Instance.Value.Single());
        }

        [Theory]
        [InlineData(false, null)]
        [InlineData(true, null)]
        [InlineData(false, "Id")]
        [InlineData(true, "Id")]
        public void DataAndValue_SetInSameRender_WithFilteredData_HighlightsRow(bool valueFirst, string keyProperty)
        {
            using var ctx = new TestContext();
            var all = Orders();

            var grid = Render(ctx, all, new List<Order>(), valueFirst, keyProperty);

            Assert.Empty(grid.FindAll("tr.rz-state-highlight"));

            var filtered = all.Where(o => o.Id > 1).ToList();

            grid.SetParametersAndRender(pb => Parameters(pb, filtered, new List<Order> { filtered[0] }, valueFirst, keyProperty, null));

            AssertHighlighted(grid, "Order 2");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DataChange_KeepsValue_WhenValueParameterIsUnchanged(bool valueFirst)
        {
            using var ctx = new TestContext();
            var data = Orders();
            var selected = new List<Order> { data[1] };

            var grid = Render(ctx, data, selected, valueFirst, null);

            AssertHighlighted(grid, "Order 2");

            grid.SetParametersAndRender(pb => Parameters(pb, data.ToList(), selected, valueFirst, null, null));

            AssertHighlighted(grid, "Order 2");
            Assert.Same(selected, grid.Instance.Value);
        }

        [Fact]
        public void Reset_WithRowState_ClearsValueAndHighlight()
        {
            using var ctx = new TestContext();
            var data = Orders();

            var grid = Render(ctx, data, new List<Order> { data[1] }, false, null);

            AssertHighlighted(grid, "Order 2");

            grid.InvokeAsync(() => grid.Instance.Reset(true, true));
            grid.Render();

            Assert.Empty(grid.FindAll("tr.rz-state-highlight"));
            Assert.Empty(grid.Instance.Value);
        }

        [Fact]
        public void RowRender_ClassAttribute_IsMergedWithHighlightClass()
        {
            using var ctx = new TestContext();
            var data = Orders();

            var grid = Render(ctx, data, new List<Order> { data[1] }, false, "Id", args => args.Attributes.Add("class", args.Data.Id == 2 ? "selected-row" : "other-row"));

            AssertHighlighted(grid, "Order 2");

            var rows = grid.FindAll("tr.rz-data-row");

            Assert.Equal(3, rows.Count);
            Assert.Contains("selected-row", rows[1].ClassName);
            Assert.All(rows.Where((_, i) => i != 1), r => Assert.Contains("other-row", r.ClassName));
            Assert.All(rows, r => Assert.Contains("rz-data-row", r.ClassName));
        }

        [Fact]
        public void RowRender_ClassAttribute_IsMergedWhenRowClickIsHandled()
        {
            using var ctx = new TestContext();
            var data = Orders();
            Order clicked = null;

            var grid = Render(ctx, data, new List<Order> { data[1] }, false, "Id", args => args.Attributes.Add("class", "custom-row"));

            grid.SetParametersAndRender(pb => pb.Add(p => p.RowClick, EventCallback.Factory.Create<DataGridRowMouseEventArgs<Order>>(new object(), (DataGridRowMouseEventArgs<Order> a) => clicked = a.Data)));

            var rows = grid.FindAll("tr.rz-data-row");

            Assert.All(rows, r => Assert.Contains("custom-row", r.ClassName));
            Assert.Contains("rz-state-highlight", rows[1].ClassName);

            rows[0].Click();

            Assert.Same(data[0], clicked);
        }
    }
}
