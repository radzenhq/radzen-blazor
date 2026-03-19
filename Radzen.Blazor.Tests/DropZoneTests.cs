using Bunit;
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
    }
}
