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
    }
}
