using Bunit;
using System.Collections.Generic;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TocTests
    {
        [Fact]
        public void TocItem_Renders_With_Attributes()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTocItem>(parameters =>
            {
                parameters.Add(p => p.Attributes, new Dictionary<string, object>
                {
                    { "data-enhance-nav", "false" },
                    { "aria-label", "Table of Contents Item" }
                });
            });

            Assert.Contains("data-enhance-nav=\"false\"", component.Markup);
            Assert.Contains("aria-label=\"Table of Contents Item\"", component.Markup);
        }

        [Fact]
        public void TocItem_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTocItem>();

            Assert.Contains("rz-toc-item", component.Markup);
        }

        [Fact]
        public void TocItem_Renders_Text()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTocItem>(parameters =>
            {
                parameters.Add(p => p.Text, "Introduction");
            });

            Assert.Contains("Introduction", component.Markup);
        }

        [Fact]
        public void TocItem_Renders_Selector()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTocItem>(parameters =>
            {
                parameters.Add(p => p.Selector, "#section-1");
            });

            Assert.Equal("#section-1", component.Instance.Selector);
        }

        [Fact]
        public void TocItem_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTocItem>(parameters =>
            {
                parameters.AddChildContent("<span>Nested Items</span>");
            });

            Assert.Contains("Nested Items", component.Markup);
        }

        [Fact]
        public void TocItem_Renders_WrapperClass()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTocItem>();

            Assert.Contains("rz-toc-item-wrapper", component.Markup);
        }

        [Fact]
        public void TocItem_Renders_LinkClass()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTocItem>(parameters =>
            {
                parameters.Add(p => p.Text, "Link Item");
            });

            Assert.Contains("rz-toc-link", component.Markup);
        }
    }
}

