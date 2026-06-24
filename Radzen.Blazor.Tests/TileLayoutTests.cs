using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TileLayoutTests
    {
        private static TestContext CreateContext(Rect rect = null)
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");
            ctx.JSInterop.Setup<Rect>("Radzen.clientRect", _ => true)
                .SetResult(rect ?? new Rect { Width = 1200, Height = 600 });
            return ctx;
        }

        [Fact]
        public void TileLayout_Renders_WithClassName()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenTileLayout>();

            Assert.Contains("rz-tile-layout", component.Markup);
            Assert.Contains("rz-tile-layout-cells", component.Markup);
        }

        [Fact]
        public void TileLayout_Renders_StyleParameter()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenTileLayout>(parameters =>
                parameters.Add(p => p.Style, "height: 400px;"));

            Assert.Contains("height: 400px;", component.Markup);
        }

        [Fact]
        public void TileLayout_Renders_ColumnsAndGap()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenTileLayout>(parameters =>
            {
                parameters.Add(p => p.Columns, 6);
                parameters.Add(p => p.Gap, 12);
                parameters.Add(p => p.RowHeight, 100);
            });

            Assert.Contains("grid-template-columns: repeat(6, minmax(0, 1fr));", component.Markup);
            Assert.Contains("gap: 12px;", component.Markup);
            Assert.Contains("grid-auto-rows: 100px;", component.Markup);
        }

        [Fact]
        public void TileLayout_Renders_FixedRows()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenTileLayout>(parameters =>
            {
                parameters.Add(p => p.Rows, 4);
                parameters.Add(p => p.RowHeight, 80);
            });

            Assert.Contains("grid-template-rows: repeat(4, 80px);", component.Markup);
        }

        [Fact]
        public void TileLayoutItem_Renders_Placement()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenTileLayout>(parameters =>
                parameters.AddChildContent<RadzenTileLayoutItem>(item =>
                {
                    item.Add(i => i.Title, "Tile");
                    item.Add(i => i.Col, 2);
                    item.Add(i => i.Row, 3);
                    item.Add(i => i.ColSpan, 4);
                    item.Add(i => i.RowSpan, 2);
                }));

            Assert.Contains("rz-tile-layout-item", component.Markup);
            Assert.Contains("grid-column: 2 / span 4; grid-row: 3 / span 2;", component.Markup);
            Assert.Contains("Tile", component.Markup);
        }

        [Fact]
        public void TileLayoutItem_Renders_TitleAndIcon()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenTileLayout>(parameters =>
                parameters.AddChildContent<RadzenTileLayoutItem>(item =>
                {
                    item.Add(i => i.Title, "Sales");
                    item.Add(i => i.Icon, "paid");
                }));

            Assert.Contains("rz-tile-layout-item-header", component.Markup);
            Assert.Contains("Sales", component.Markup);
            Assert.Contains("paid", component.Markup);
        }

        [Fact]
        public void TileLayoutItem_EditMode_RendersHandles()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenTileLayout>(parameters =>
            {
                parameters.Add(p => p.EditMode, true);
                parameters.AddChildContent<RadzenTileLayoutItem>(item => item.Add(i => i.Title, "T"));
            });

            Assert.Contains("rz-tile-layout-item-resize-handle", component.Markup);
            Assert.Contains("rz-tile-layout-item-header-draggable", component.Markup);
        }

        [Fact]
        public void TileLayoutItem_ViewMode_HidesHandles()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenTileLayout>(parameters =>
            {
                parameters.Add(p => p.EditMode, false);
                parameters.AddChildContent<RadzenTileLayoutItem>(item => item.Add(i => i.Title, "T"));
            });

            Assert.DoesNotContain("rz-tile-layout-item-resize-handle", component.Markup);
            Assert.DoesNotContain("rz-tile-layout-item-header-draggable", component.Markup);
        }

        [Fact]
        public void TileLayout_ShowGrid_RendersOverlay()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenTileLayout>(parameters =>
            {
                parameters.Add(p => p.ShowGrid, true);
                parameters.Add(p => p.Columns, 3);
                parameters.Add(p => p.Rows, 2);
            });

            Assert.Contains("rz-tile-layout-overlay", component.Markup);
            Assert.Contains("rz-tile-layout-overlay-cell", component.Markup);
            Assert.Contains("background-color: var(--rz-tile-layout-overlay-background-color, var(--rz-primary-lighter));", component.Markup);
            Assert.Contains("border: 1px dashed var(--rz-tile-layout-overlay-border-color, var(--rz-primary));", component.Markup);
            // 3 columns x 2 rows = 6 overlay cells.
            var count = component.FindAll(".rz-tile-layout-overlay-cell").Count;
            Assert.Equal(6, count);
        }

        [Fact]
        public void TileLayout_HideGrid_DoesNotRenderOverlay()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenTileLayout>(parameters =>
            {
                parameters.Add(p => p.ShowGrid, false);
                parameters.Add(p => p.Columns, 3);
                parameters.Add(p => p.Rows, 2);
            });

            Assert.DoesNotContain("rz-tile-layout-overlay", component.Markup);
            Assert.DoesNotContain("rz-tile-layout-overlay-cell", component.Markup);
        }

        [Fact]
        public void TileLayout_Move_CommitsAndRaisesChange()
        {
            using var ctx = CreateContext();

            var changedCol = -1;
            RadzenTileLayoutItem changedItem = null;

            var component = ctx.RenderComponent<RadzenTileLayout>(parameters =>
            {
                parameters.Add(p => p.Columns, 12);
                parameters.Add(p => p.EditMode, true);
                parameters.Add(p => p.Change, EventCallback.Factory.Create<RadzenTileLayoutItem>(this, i => changedItem = i));
                parameters.AddChildContent<RadzenTileLayoutItem>(item =>
                {
                    item.Add(i => i.Title, "T");
                    item.Add(i => i.Col, 2);
                    item.Add(i => i.Row, 1);
                    item.Add(i => i.ColChanged, EventCallback.Factory.Create<int>(this, v => changedCol = v));
                });
            });

            // cellWidth = (1200 - 11*8)/12 = 92.67; stride = 100.67. dx ~ 101 => +1 column.
            var header = component.Find(".rz-tile-layout-item-header");
            header.TriggerEvent("onpointerdown", new PointerEventArgs { ClientX = 100, ClientY = 100, PointerId = 1, Buttons = 1 });

            var cells = component.Find(".rz-tile-layout-cells");
            cells.TriggerEvent("onpointermove", new PointerEventArgs { ClientX = 201, ClientY = 100, PointerId = 1, Buttons = 1 });
            cells.TriggerEvent("onpointerup", new PointerEventArgs { ClientX = 201, ClientY = 100, PointerId = 1, Buttons = 0 });

            Assert.Equal(3, changedCol);
            Assert.NotNull(changedItem);
            Assert.Equal(3, changedItem.Col);
        }

        [Fact]
        public void TileLayout_Resize_CommitsRowSpan()
        {
            using var ctx = CreateContext();

            var changedRowSpan = -1;

            var component = ctx.RenderComponent<RadzenTileLayout>(parameters =>
            {
                parameters.Add(p => p.Columns, 12);
                parameters.Add(p => p.RowHeight, 80);
                parameters.Add(p => p.EditMode, true);
                parameters.AddChildContent<RadzenTileLayoutItem>(item =>
                {
                    item.Add(i => i.Title, "T");
                    item.Add(i => i.Col, 1);
                    item.Add(i => i.Row, 1);
                    item.Add(i => i.RowSpan, 1);
                    item.Add(i => i.RowSpanChanged, EventCallback.Factory.Create<int>(this, v => changedRowSpan = v));
                });
            });

            // strideY = RowHeight + Gap = 88. dy ~ 88 => +1 row span.
            var handle = component.Find(".rz-tile-layout-item-resize-handle");
            handle.TriggerEvent("onpointerdown", new PointerEventArgs { ClientX = 100, ClientY = 100, PointerId = 1, Buttons = 1 });

            var cells = component.Find(".rz-tile-layout-cells");
            cells.TriggerEvent("onpointermove", new PointerEventArgs { ClientX = 100, ClientY = 188, PointerId = 1, Buttons = 1 });
            cells.TriggerEvent("onpointerup", new PointerEventArgs { ClientX = 100, ClientY = 188, PointerId = 1, Buttons = 0 });

            Assert.Equal(2, changedRowSpan);
        }

        [Fact]
        public void TileLayout_PointerUpWithoutMove_DoesNotRaiseChange()
        {
            using var ctx = CreateContext();

            var changeRaised = false;

            var component = ctx.RenderComponent<RadzenTileLayout>(parameters =>
            {
                parameters.Add(p => p.EditMode, true);
                parameters.Add(p => p.Change, EventCallback.Factory.Create<RadzenTileLayoutItem>(this, _ => changeRaised = true));
                parameters.AddChildContent<RadzenTileLayoutItem>(item =>
                {
                    item.Add(i => i.Title, "T");
                    item.Add(i => i.Col, 2);
                    item.Add(i => i.Row, 2);
                });
            });

            var header = component.Find(".rz-tile-layout-item-header");
            header.TriggerEvent("onpointerdown", new PointerEventArgs { ClientX = 100, ClientY = 100, PointerId = 1, Buttons = 1 });

            var cells = component.Find(".rz-tile-layout-cells");
            cells.TriggerEvent("onpointerup", new PointerEventArgs { ClientX = 100, ClientY = 100, PointerId = 1, Buttons = 0 });

            Assert.False(changeRaised);
        }

        [Fact]
        public void TileLayout_Move_ClampsWithinBounds()
        {
            using var ctx = CreateContext();

            var changedCol = -1;

            var component = ctx.RenderComponent<RadzenTileLayout>(parameters =>
            {
                parameters.Add(p => p.Columns, 12);
                parameters.Add(p => p.EditMode, true);
                parameters.AddChildContent<RadzenTileLayoutItem>(item =>
                {
                    item.Add(i => i.Title, "T");
                    item.Add(i => i.Col, 1);
                    item.Add(i => i.Row, 1);
                    item.Add(i => i.ColSpan, 3);
                    item.Add(i => i.ColChanged, EventCallback.Factory.Create<int>(this, v => changedCol = v));
                });
            });

            var header = component.Find(".rz-tile-layout-item-header");
            header.TriggerEvent("onpointerdown", new PointerEventArgs { ClientX = 0, ClientY = 0, PointerId = 1, Buttons = 1 });

            var cells = component.Find(".rz-tile-layout-cells");
            // Drag far to the right - should clamp so Col + ColSpan - 1 <= Columns => Col <= 10.
            cells.TriggerEvent("onpointermove", new PointerEventArgs { ClientX = 5000, ClientY = 0, PointerId = 1, Buttons = 1 });
            cells.TriggerEvent("onpointerup", new PointerEventArgs { ClientX = 5000, ClientY = 0, PointerId = 1, Buttons = 0 });

            Assert.Equal(10, changedCol);
        }
    }
}
