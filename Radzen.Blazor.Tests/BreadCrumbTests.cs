using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class BreadCrumbTests
    {
        [Fact]
        public void BreadCrumb_Renders_Items()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBreadCrumb>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(c => c.ChildContent, builder =>
          {
              builder.OpenComponent<RadzenBreadCrumbItem>(0);
              builder.AddAttribute(1, nameof(RadzenBreadCrumbItem.Text), "Test");
              builder.CloseComponent();
          });
            });
            //@"<RadzenBreadCrumbItem Text=""Test"" />"
            Assert.Contains(@"class=""rz-breadcrumb-item", component.Markup);
            Assert.Contains(">Test</", component.Markup);
        }

        [Fact]
        public void BreadCrumb_Renders_Icon()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBreadCrumb>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(c => c.ChildContent, builder =>
          {
              builder.OpenComponent<RadzenBreadCrumbItem>(0);
              builder.AddAttribute(1, nameof(RadzenBreadCrumbItem.Text), "Test");
              builder.AddAttribute(2, nameof(RadzenBreadCrumbItem.Icon), "add");
              builder.CloseComponent();
          });
            });

            //@"<RadzenBreadCrumbItem Text=""Test"" />"
            Assert.Contains("<i", component.Markup);
            Assert.Contains(">add</i>", component.Markup);
        }

        [Fact]
        public void BreadCrumb_Renders_Link()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBreadCrumb>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(c => c.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenBreadCrumbItem>(0);
                    builder.AddAttribute(1, nameof(RadzenBreadCrumbItem.Text), "Test");
                    builder.AddAttribute(2, nameof(RadzenBreadCrumbItem.Icon), "add");
                    builder.AddAttribute(3, nameof(RadzenBreadCrumbItem.Path), "/badge");
                    builder.CloseComponent();
                });
            });

            //@"<RadzenBreadCrumbItem Text=""Test"" />"
            Assert.Contains("<i", component.Markup);
            Assert.Contains(">add</i>", component.Markup);
            Assert.Contains("<a href=\"/badge", component.Markup);
        }

        [Fact]
        public void BreadCrumb_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenBreadCrumb>();

            Assert.Contains("rz-breadcrumb", component.Markup);
        }

        [Fact]
        public void BreadCrumb_Renders_MultipleItems()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBreadCrumb>(parameters =>
            {
                parameters.Add(c => c.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenBreadCrumbItem>(0);
                    builder.AddAttribute(1, nameof(RadzenBreadCrumbItem.Text), "Home");
                    builder.AddAttribute(2, nameof(RadzenBreadCrumbItem.Path), "/");
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenBreadCrumbItem>(3);
                    builder.AddAttribute(4, nameof(RadzenBreadCrumbItem.Text), "Products");
                    builder.CloseComponent();
                });
            });

            Assert.Contains(">Home</", component.Markup);
            Assert.Contains(">Products</", component.Markup);
        }

        [Fact]
        public void BreadCrumb_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenBreadCrumb>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-breadcrumb", component.Markup);
        }

        [Fact]
        public void BreadCrumb_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenBreadCrumb>(parameters =>
            {
                parameters.Add(p => p.Style, "margin:1rem");
            });

            Assert.Contains("margin:1rem", component.Markup);
        }

        [Fact]
        public void BreadCrumb_Item_WithoutPath_DoesNotRenderLink()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBreadCrumb>(parameters =>
            {
                parameters.Add(c => c.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenBreadCrumbItem>(0);
                    builder.AddAttribute(1, nameof(RadzenBreadCrumbItem.Text), "Current Page");
                    builder.CloseComponent();
                });
            });

            Assert.Contains(">Current Page</", component.Markup);
            Assert.DoesNotContain("<a href", component.Markup);
        }
    }
}
