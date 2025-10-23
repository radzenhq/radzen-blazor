using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ProfileMenuItemTests
    {
        [Fact]
        public void ProfileMenuItem_Renders_TextParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenProfileMenuItem>(0);
                    builder.AddAttribute(1, "Text", "Profile");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("Profile", component.Markup);
        }

        [Fact]
        public void ProfileMenuItem_Renders_IconParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenProfileMenuItem>(0);
                    builder.AddAttribute(1, "Icon", "account_circle");
                    builder.AddAttribute(2, "Text", "Profile");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("account_circle", component.Markup);
        }

        [Fact]
        public void ProfileMenuItem_Template_OverridesText()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenProfileMenuItem>(0);
                    builder.AddAttribute(1, "Text", "This should not appear");
                    builder.AddAttribute(2, "Template", (RenderFragment)((templateBuilder) =>
                    {
                        templateBuilder.OpenElement(0, "span");
                        templateBuilder.AddAttribute(1, "class", "template-content");
                        templateBuilder.AddContent(2, "Template Content");
                        templateBuilder.CloseElement();
                    }));
                    builder.CloseComponent();
                });
            });

            // Template should be rendered
            Assert.Contains("template-content", component.Markup);
            // Text should not be rendered in navigation-item-text span when Template is present
            Assert.DoesNotContain("<span class=\"rz-navigation-item-text\">This should not appear</span>", component.Markup);
        }

        [Fact]
        public void ProfileMenuItem_Renders_TemplateWithSwitch()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenProfileMenuItem>(0);
                    builder.AddAttribute(1, "Icon", "settings");
                    builder.AddAttribute(2, "Template", (RenderFragment)((templateBuilder) =>
                    {
                        templateBuilder.OpenComponent<RadzenSwitch>(0);
                        templateBuilder.CloseComponent();
                    }));
                    builder.CloseComponent();
                });
            });

            // Icon should still be rendered
            Assert.Contains("settings", component.Markup);
            // Switch should be rendered from template
            Assert.Contains("rz-switch", component.Markup);
        }

        [Fact]
        public void ProfileMenuItem_Renders_PathParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenProfileMenuItem>(0);
                    builder.AddAttribute(1, "Text", "Settings");
                    builder.AddAttribute(2, "Path", "/settings");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("href=\"/settings\"", component.Markup);
        }
    }
}

