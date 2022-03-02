using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class BreadCrumbTests
    {
        [Fact]
        public void BreadCrumb_Renders_TextProperty()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBreadCrumb<TestItem>>();

            var items = new[]
            {
                new TestItem { Text = "Test" }
            };

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(component => component.TextProperty, nameof(TestItem.Text));
                parameters.Add(component => component.Data, items);
            });

            Assert.Contains("Test", component.Markup);
        }

        [Fact]
        public void BreadCrumb_DoestNotRender_SeparatorWithOneItem()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBreadCrumb<TestItem>>();

            var items = new[]
            {
                new TestItem { Text = "Test" }
            };

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(component => component.TextProperty, nameof(TestItem.Text));
                parameters.Add(component => component.Data, items);
            });

            Assert.Contains("Test", component.Markup);
            Assert.DoesNotContain(component.Instance.Separator, component.Markup);
        }

        [Fact]
        public void BreadCrumb_Renders_LinkProperty()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBreadCrumb<TestItem>>();

            var items = new[]
            {
                new TestItem { Text = "Test", Link = "/test-link" }
            };

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(component => component.TextProperty, nameof(TestItem.Text));
                parameters.Add(component => component.LinkProperty, nameof(TestItem.Link));
                parameters.Add(component => component.Data, items);
            });

            Assert.Contains("/test-link", component.Markup);
        }

        [Fact]
        public void BreadCrumb_Renders_Separator()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBreadCrumb<TestItem>>();

            var items = new[]
            {
                new TestItem { Text = "Test", Link = "/test-link" },
                new TestItem { Text = "Test", Link = "/test-link" }
            };

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(component => component.TextProperty, nameof(TestItem.Text));
                parameters.Add(component => component.LinkProperty, nameof(TestItem.Link));
                parameters.Add(component => component.Separator, "SEPARATOR");
                parameters.Add(component => component.Data, items);
            });

            Assert.Contains("SEPARATOR", component.Markup);
        }

        [Fact]
        public void BreadCrumb_Renders_Template()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBreadCrumb<TestItem>>();

            var items = new[]
            {
                new TestItem { Text = "Test1", Link = "/test-link" },
                new TestItem { Text = "Test2", Link = "/test-link" }
            };

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(component => component.TextProperty, nameof(TestItem.Text));
                parameters.Add(component => component.LinkProperty, nameof(TestItem.Link));
                parameters.Add(component => component.Template, item => item.Text);
                parameters.Add(component => component.Data, items);
            });

            Assert.Contains("Test1", component.Markup);
            Assert.Contains("Test2", component.Markup);
        }

        [Fact]
        public void BreadCrumb_Renders_SeparatorTemplate()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenBreadCrumb<TestItem>>();

            var items = new[]
            {
                new TestItem { Text = "Test", Link = "/test-link" },
                new TestItem { Text = "Test", Link = "/test-link" },
                new TestItem { Text = "Test", Link = "/test-link" }
            };

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(component => component.TextProperty, nameof(TestItem.Text));
                parameters.Add(component => component.LinkProperty, nameof(TestItem.Link));
                parameters.Add(component => component.SeparatorTemplate, "<span>SEP</span>");
                parameters.Add(component => component.Data, items);
            });

            Assert.Contains("<span>SEP</span>", component.Markup);
        }
    }

    public class TestItem
    {
        public string Text { get; set; }
        public string Link { get; set; }
    }
}
