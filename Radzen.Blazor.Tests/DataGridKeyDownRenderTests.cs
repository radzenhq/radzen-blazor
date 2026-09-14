using Bunit;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using Xunit;

namespace Radzen.Blazor.Tests
{
    // Keys the grid does not handle (Shift, Control, letters...) must not re-render it on every keydown.
    // Holding such a key repeats keydown and re-rendered a wide grid continuously (#1361).
    public class DataGridKeyDownRenderTests
    {
        class Item
        {
            public int Id { get; set; }
        }

        static IRenderedComponent<RadzenDataGrid<Item>> Render(TestContext ctx,
            System.Action<ComponentParameterCollectionBuilder<RadzenDataGrid<Item>>>? extra = null)
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
                extra?.Invoke(pb);
            });
        }

        static readonly KeyboardEventArgs Shift = new() { Key = "Shift", Code = "ShiftLeft", ShiftKey = true };

        static readonly KeyboardEventArgs ArrowDown = new() { Key = "ArrowDown", Code = "ArrowDown" };

        [Fact]
        public void RepeatedUnhandledKey_DoesNotRerenderTheGrid()
        {
            using var ctx = new TestContext();
            var cut = Render(ctx);
            var grid = cut.Find(".rz-data-grid-data");

            grid.KeyDown(Shift);
            var renderCount = cut.RenderCount;

            grid.KeyDown(Shift);
            grid.KeyDown(Shift);
            grid.KeyDown(new KeyboardEventArgs { Key = "a", Code = "KeyA" });

            Assert.Equal(renderCount, cut.RenderCount);
        }

        [Fact]
        public void FirstUnhandledKey_StillRerenders_ToClearPreventDefault()
        {
            using var ctx = new TestContext();
            var cut = Render(ctx);
            var renderCount = cut.RenderCount;

            cut.Find(".rz-data-grid-data").KeyDown(Shift);

            Assert.True(cut.RenderCount > renderCount);
        }

        [Fact]
        public void UnhandledKey_AfterNavigationKey_StillRerenders()
        {
            using var ctx = new TestContext();
            var cut = Render(ctx);
            var grid = cut.Find(".rz-data-grid-data");

            grid.KeyDown(Shift);
            var renderCount = cut.RenderCount;

            grid.KeyDown(ArrowDown);
            Assert.True(cut.RenderCount > renderCount);

            renderCount = cut.RenderCount;
            grid.KeyDown(Shift);
            Assert.True(cut.RenderCount > renderCount);
        }

        [Fact]
        public void RepeatedUnhandledKey_StillRaisesKeyDown()
        {
            using var ctx = new TestContext();
            var keys = new List<string?>();
            var cut = Render(ctx, pb => pb.Add(p => p.KeyDown, (KeyboardEventArgs a) => keys.Add(a.Code)));
            var grid = cut.Find(".rz-data-grid-data");

            grid.KeyDown(Shift);
            grid.KeyDown(Shift);

            Assert.Equal(new[] { "ShiftLeft", "ShiftLeft" }, keys);
        }
    }
}
