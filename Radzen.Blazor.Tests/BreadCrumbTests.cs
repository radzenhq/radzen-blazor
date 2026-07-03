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

        [Fact]
        public void BreadCrumb_Renders_NavigationLandmark_WithDefaultAriaLabel()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBreadCrumb>();

            Assert.Contains("<nav", component.Markup);
            Assert.Contains("aria-label=\"breadcrumb\"", component.Markup);
        }

        [Fact]
        public void BreadCrumb_Renders_CustomAriaLabel()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBreadCrumb>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, "You are here");
            });

            Assert.Contains("aria-label=\"You are here\"", component.Markup);
        }

        [Fact]
        public void BreadCrumb_Item_WithoutPath_Renders_AriaCurrentPage()
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

            Assert.Contains("aria-current=\"page\"", component.Markup);
        }

        [Fact]
        public void BreadCrumb_NonLastItem_WithPath_DoesNotRender_AriaCurrentPage()
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
                    builder.AddAttribute(5, nameof(RadzenBreadCrumbItem.Path), "/products");
                    builder.CloseComponent();
                });
            });

            var items = component.FindAll("li.rz-breadcrumb-item");

            Assert.Equal(2, items.Count);
            Assert.Null(items[0].GetAttribute("aria-current"));
        }

        [Fact]
        public void BreadCrumb_LastItem_WithPath_Renders_AriaCurrentPage()
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
                    builder.AddAttribute(5, nameof(RadzenBreadCrumbItem.Path), "/products");
                    builder.CloseComponent();
                });
            });

            var items = component.FindAll("li.rz-breadcrumb-item");

            Assert.Equal("page", items[1].GetAttribute("aria-current"));
        }

        [Fact]
        public void BreadCrumb_Renders_OrderedList_WithListItems()
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

            var list = component.Find("nav > ol.rz-breadcrumb-list");

            Assert.Equal(2, list.QuerySelectorAll("li.rz-breadcrumb-item").Length);
        }
    }
}
