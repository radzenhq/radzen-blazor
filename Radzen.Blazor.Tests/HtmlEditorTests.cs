using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class HtmlEditorTests
    {
        [Fact]
        public void HtmlEditor_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            var component = ctx.RenderComponent<RadzenHtmlEditor>();

            Assert.Contains(@"rz-html-editor", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_ShowToolbar_True()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters =>
            {
                parameters.Add(p => p.ShowToolbar, true);
            });

            Assert.Contains("rz-html-editor-toolbar", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_ShowToolbar_False()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters =>
            {
                parameters.Add(p => p.ShowToolbar, false);
            });

            Assert.DoesNotContain("rz-html-editor-toolbar", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_Mode_Design()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters =>
            {
                parameters.Add(p => p.Mode, HtmlEditorMode.Design);
            });

            // Design mode shows the content editable div
            Assert.Contains("contenteditable", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_Mode_Source()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters =>
            {
                parameters.Add(p => p.Mode, HtmlEditorMode.Source);
            });

            // Source mode shows the textarea for HTML editing
            Assert.Contains("rz-html-editor-source", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_Disabled_Attribute()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains("disabled", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_ContentArea()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            var component = ctx.RenderComponent<RadzenHtmlEditor>();

            Assert.Contains("rz-html-editor-content", component.Markup);
        }
    }
}

