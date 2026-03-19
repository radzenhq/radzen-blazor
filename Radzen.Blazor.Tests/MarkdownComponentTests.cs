using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class MarkdownComponentTests
    {
        [Fact]
        public void Markdown_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMarkdown>();

            Assert.Contains("rz-markdown", component.Markup);
        }

        [Fact]
        public void Markdown_Renders_TextParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMarkdown>(parameters =>
            {
                parameters.Add(p => p.Text, "Hello World");
            });

            Assert.Contains("Hello World", component.Markup);
        }

        [Fact]
        public void Markdown_Renders_Heading()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMarkdown>(parameters =>
            {
                parameters.Add(p => p.Text, "# Heading 1");
            });

            Assert.Contains("<h1", component.Markup);
            Assert.Contains("Heading 1", component.Markup);
        }

        [Fact]
        public void Markdown_Renders_Bold()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMarkdown>(parameters =>
            {
                parameters.Add(p => p.Text, "This is **bold** text");
            });

            Assert.Contains("<strong", component.Markup);
            Assert.Contains("bold", component.Markup);
        }

        [Fact]
        public void Markdown_Renders_Italic()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMarkdown>(parameters =>
            {
                parameters.Add(p => p.Text, "This is *italic* text");
            });

            Assert.Contains("<em", component.Markup);
            Assert.Contains("italic", component.Markup);
        }

        [Fact]
        public void Markdown_Renders_Link()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMarkdown>(parameters =>
            {
                parameters.Add(p => p.Text, "[Click here](https://example.com)");
            });

            Assert.Contains("<a", component.Markup);
            Assert.Contains("Click here", component.Markup);
        }

        [Fact]
        public void Markdown_Renders_UnorderedList()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMarkdown>(parameters =>
            {
                parameters.Add(p => p.Text, "- Item 1\n- Item 2");
            });

            Assert.Contains("<ul", component.Markup);
            Assert.Contains("Item 1", component.Markup);
            Assert.Contains("Item 2", component.Markup);
        }

        [Fact]
        public void Markdown_Renders_Paragraph()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMarkdown>(parameters =>
            {
                parameters.Add(p => p.Text, "A simple paragraph.");
            });

            Assert.Contains("<p", component.Markup);
            Assert.Contains("A simple paragraph.", component.Markup);
        }

        [Fact]
        public void Markdown_Renders_CodeBlock()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMarkdown>(parameters =>
            {
                parameters.Add(p => p.Text, "```\nvar x = 1;\n```");
            });

            Assert.Contains("<code", component.Markup);
            Assert.Contains("var x = 1;", component.Markup);
        }

        [Fact]
        public void Markdown_Renders_InlineCode()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMarkdown>(parameters =>
            {
                parameters.Add(p => p.Text, "Use `Console.WriteLine` here");
            });

            Assert.Contains("<code", component.Markup);
            Assert.Contains("Console.WriteLine", component.Markup);
        }

        [Fact]
        public void Markdown_Renders_H2()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMarkdown>(parameters =>
            {
                parameters.Add(p => p.Text, "## Sub Heading");
            });

            Assert.Contains("<h2", component.Markup);
            Assert.Contains("Sub Heading", component.Markup);
        }

        [Fact]
        public void Markdown_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMarkdown>(parameters =>
            {
                parameters.Add(p => p.Style, "max-width:800px");
            });

            Assert.Contains("max-width:800px", component.Markup);
        }

        [Fact]
        public void Markdown_Renders_Blockquote()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMarkdown>(parameters =>
            {
                parameters.Add(p => p.Text, "> This is a quote");
            });

            Assert.Contains("<blockquote", component.Markup);
            Assert.Contains("This is a quote", component.Markup);
        }

        [Fact]
        public void Markdown_EmptyText_StillRenders()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMarkdown>();

            Assert.Contains("rz-markdown", component.Markup);
        }
    }
}
