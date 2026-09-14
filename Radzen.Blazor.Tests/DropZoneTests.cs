using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DropZoneTests
    {
        [Fact]
        public void DropZoneContainer_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDropZoneContainer<string>>();

            Assert.Contains("rz-dropzone-container", component.Markup);
        }

        [Fact]
        public void DropZoneContainer_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDropZoneContainer<string>>(parameters =>
            {
                parameters.AddChildContent("<div>Zone Content</div>");
            });

            Assert.Contains("Zone Content", component.Markup);
        }

        [Fact]
        public void DropZoneContainer_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDropZoneContainer<string>>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-dropzone-container", component.Markup);
        }

        [Fact]
        public void DropZone_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDropZoneContainer<string>>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenDropZone<string>>(0);
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-dropzone", component.Markup);
        }

        [Fact]
        public void DropZone_Renders_WithValue()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDropZoneContainer<string>>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenDropZone<string>>(0);
                    builder.AddAttribute(1, nameof(RadzenDropZone<string>.Value), "zone1");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-dropzone", component.Markup);
        }

        [Fact]
        public void DropZone_Renders_Footer()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDropZoneContainer<string>>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenDropZone<string>>(0);
                    builder.AddAttribute(1, nameof(RadzenDropZone<string>.Footer),
                        (Microsoft.AspNetCore.Components.RenderFragment)(b => b.AddContent(0, "Footer Content")));
                    builder.CloseComponent();
                });
            });

            Assert.Contains("Footer Content", component.Markup);
        }

        [Fact]
        public void DropZoneContainer_Renders_Items_WithSelector()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var data = new[] { "Item1", "Item2", "Item3" };

            var component = ctx.RenderComponent<RadzenDropZoneContainer<string>>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.ItemSelector, (item, zone) => true);
                parameters.Add(p => p.Template, (string item) => builder =>
                {
                    builder.AddContent(0, item);
                });
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenDropZone<string>>(0);
                    builder.CloseComponent();
                });
            });

            Assert.Contains("Item1", component.Markup);
            Assert.Contains("Item2", component.Markup);
            Assert.Contains("Item3", component.Markup);
        }

        [Fact]
        public void DropZoneContainer_Payload_IsNull_Initially()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDropZoneContainer<string>>();

            Assert.Null(component.Instance.Payload);
        }

        [Fact]
        public void DropZoneContainer_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDropZoneContainer<string>>(parameters =>
            {
                parameters.Add(p => p.Style, "display:flex");
            });

            Assert.Contains("display:flex", component.Markup);
        }

        static IRenderedComponent<RadzenDropZoneContainer<string>> RenderZoneWithItems(TestContext ctx)
        {
            return ctx.RenderComponent<RadzenDropZoneContainer<string>>(parameters =>
            {
                parameters.Add(p => p.Data, new[] { "Item1", "Item2" });
                parameters.Add(p => p.ItemSelector, (item, zone) => true);
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenDropZone<string>>(0);
                    builder.CloseComponent();
                });
            });
        }

        static DragEventArgs DragArgs() => new DragEventArgs { DataTransfer = new DataTransfer() };

        [Fact]
        public void DropZone_KeepsCanDropClass_WhenDraggingOverItsItems()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderZoneWithItems(ctx);

            component.Find(".rz-dropzone").TriggerEvent("ondragenter", DragArgs());
            component.Find(".rz-dropzone").TriggerEvent("ondragover", DragArgs());
            Assert.Contains("rz-can-drop", component.Find(".rz-dropzone").ClassList);

            // Moving the pointer from the zone onto one of its items: the browser raises dragenter on the
            // item (bubbling to the zone) and then dragleave on the zone, although the pointer is still inside it.
            component.FindAll(".rz-dropzone-item")[0].TriggerEvent("ondragenter", DragArgs());
            component.Find(".rz-dropzone").TriggerEvent("ondragleave", DragArgs());
            Assert.Contains("rz-can-drop", component.Find(".rz-dropzone").ClassList);

            // Moving from the first item to the second one: dragleave on the first item bubbles to the zone.
            component.FindAll(".rz-dropzone-item")[1].TriggerEvent("ondragenter", DragArgs());
            component.FindAll(".rz-dropzone-item")[0].TriggerEvent("ondragleave", DragArgs());
            Assert.Contains("rz-can-drop", component.Find(".rz-dropzone").ClassList);
        }

        [Fact]
        public void DropZone_RemovesCanDropClass_WhenDragLeavesTheZone()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderZoneWithItems(ctx);

            component.Find(".rz-dropzone").TriggerEvent("ondragenter", DragArgs());
            component.FindAll(".rz-dropzone-item")[0].TriggerEvent("ondragenter", DragArgs());
            component.Find(".rz-dropzone").TriggerEvent("ondragleave", DragArgs());
            component.FindAll(".rz-dropzone-item")[0].TriggerEvent("ondragover", DragArgs());
            Assert.Contains("rz-can-drop", component.Find(".rz-dropzone").ClassList);

            component.FindAll(".rz-dropzone-item")[0].TriggerEvent("ondragleave", DragArgs());
            Assert.DoesNotContain("rz-can-drop", component.Find(".rz-dropzone").ClassList);
        }
    }
}
