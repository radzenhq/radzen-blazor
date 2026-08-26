using Bunit;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    // Row clicks are raised once per row from the <tr> (RowAttributes), cell clicks once per cell from
    // the <td>.
    public class DataGridRowClickDispatchTests
    {
        class Item
        {
            public int Id { get; set; }
        }

        static IRenderedComponent<RadzenDataGrid<Item>> Render(TestContext ctx,
            System.Action<ComponentParameterCollectionBuilder<RadzenDataGrid<Item>>> extra)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            return ctx.RenderComponent<RadzenDataGrid<Item>>(pb =>
            {
                pb.Add(p => p.Data, new List<Item> { new() { Id = 1 }, new() { Id = 2 } });
                pb.Add(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenDataGridColumn<Item>>(0);
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Item>.Property), "Id");
                    b.CloseComponent();
                });
                extra(pb);
            });
        }

        [Fact]
        public void RowClick_FiresFromTheRowElement_WithThatRowsData()
        {
            using var ctx = new TestContext();
            Item? clicked = null;
            var cut = Render(ctx, pb => pb.Add(p => p.RowClick, (DataGridRowMouseEventArgs<Item> a) => clicked = a.Data));

            cut.FindAll("tr.rz-data-row")[1].Click();

            Assert.NotNull(clicked);
            Assert.Equal(2, clicked!.Id);
        }

        [Fact]
        public void RowClick_SelectsTheRow_WhenSelectionIsOn()
        {
            using var ctx = new TestContext();
            Item? selected = null;
            var cut = Render(ctx, pb =>
            {
                pb.Add(p => p.SelectionMode, DataGridSelectionMode.Single);
                pb.Add(p => p.RowSelect, (Item i) => selected = i);
            });

            cut.FindAll("tr.rz-data-row")[0].Click();

            Assert.NotNull(selected);
            Assert.Equal(1, selected!.Id);
        }

        [Fact]
        public void CellClick_StillFiresFromTheCell_WithItsColumn()
        {
            using var ctx = new TestContext();
            string? column = null;
            var cut = Render(ctx, pb => pb.Add(p => p.CellClick, (DataGridCellMouseEventArgs<Item> a) => column = a.Column?.Property));

            cut.FindAll("td[role=\"gridcell\"]")[0].Click();

            Assert.Equal("Id", column);
        }

        [Fact]
        public void RowRender_Onclick_IsPreserved_AndRunsAfterTheBuiltInRowClick()
        {
            using var ctx = new TestContext();
            var order = new List<string>();
            var cut = Render(ctx, pb =>
            {
                pb.Add(p => p.RowClick, (DataGridRowMouseEventArgs<Item> _) => order.Add("row"));
                pb.Add(p => p.RowRender, (RowRenderEventArgs<Item> a) =>
                    a.Attributes["onclick"] = (System.Action)(() => order.Add("consumer")));
            });

            cut.FindAll("tr.rz-data-row")[1].Click();

            // Both fire, and the built-in row click (with its selection) runs first, so a consumer
            // onclick that reads selection state still sees the post-click state.
            Assert.Equal(new[] { "row", "consumer" }, order);
        }

        [Fact]
        public void RowRender_StringOnclick_IsKept_AndBuiltInRowClickStillFiresFromTheCell()
        {
            using var ctx = new TestContext();
            // A string HTML handler can't be chained with the built-in delegate, so it stays verbatim on the
            // row and the built-in row click is raised from the cell instead - both must survive.
            Item? rowClicked = null;
            var cut = Render(ctx, pb =>
            {
                pb.Add(p => p.RowClick, (DataGridRowMouseEventArgs<Item> a) => rowClicked = a.Data);
                pb.Add(p => p.RowRender, (RowRenderEventArgs<Item> a) => a.Attributes["onclick"] = "window.probe=true");
            });

            var row = cut.FindAll("tr.rz-data-row")[1];
            Assert.Equal("window.probe=true", row.GetAttribute("onclick"));

            row.QuerySelector("td[role=\"gridcell\"]")!.Click();
            Assert.NotNull(rowClicked);
            Assert.Equal(2, rowClicked!.Id);
        }

        [Fact]
        public void NoRowHandler_IsWired_WhenNoRowOrCellClickIsSet()
        {
            using var ctx = new TestContext();
            var cut = Render(ctx, _ => { });

            var row = cut.FindAll("tr.rz-data-row")[0];
            Assert.DoesNotContain("onclick", row.Attributes.Select(a => a.Name));
        }
    }
}
