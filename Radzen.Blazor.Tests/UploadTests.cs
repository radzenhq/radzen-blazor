using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class UploadTests
    {
        [Fact]
        public void Upload_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>();

            Assert.Contains(@"rz-fileupload", component.Markup);
        }

        [Fact]
        public void Upload_Renders_Disabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains("rz-state-disabled", component.Markup);
        }

        [Fact]
        public void Upload_Renders_ChooseText()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters =>
            {
                parameters.Add(p => p.ChooseText, "Select Files");
            });

            Assert.Contains("Select Files", component.Markup);
        }

        [Fact]
        public void Upload_Renders_DefaultChooseText()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>();

            Assert.Contains("Choose", component.Markup);
        }

        [Fact]
        public void Upload_Renders_Icon()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters =>
            {
                parameters.Add(p => p.Icon, "upload");
            });

            Assert.Contains("upload", component.Markup);
            Assert.Contains("rzi", component.Markup);
        }

        [Fact]
        public void Upload_Renders_Multiple_Attribute()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
            });

            Assert.Contains("multiple", component.Markup);
        }

        [Fact]
        public void Upload_Renders_Accept_Attribute()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters =>
            {
                parameters.Add(p => p.Accept, "image/*");
            });

            Assert.Contains("accept=\"image/*\"", component.Markup);
        }

        [Fact]
        public void Upload_Renders_DefaultTabIndex_OnChooseButton()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>();

            Assert.Equal("0", component.Find(".rz-fileupload-choose").GetAttribute("tabindex"));
        }

        [Fact]
        public void Upload_Renders_TabIndex_OnChooseButton_NotWrapper()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters =>
            {
                parameters.Add(p => p.TabIndex, 22);
            });

            Assert.Equal("22", component.Find(".rz-fileupload-choose").GetAttribute("tabindex"));
            Assert.False(component.Find(".rz-fileupload").HasAttribute("tabindex"));
        }

        [Fact]
        public void Upload_Renders_DisabledTabIndex_OnChooseButton()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenUpload>(parameters =>
            {
                parameters.Add(p => p.TabIndex, 22);
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Equal("-1", component.Find(".rz-fileupload-choose").GetAttribute("tabindex"));
        }
    }
}

