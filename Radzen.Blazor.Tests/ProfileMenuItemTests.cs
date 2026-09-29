using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ProfileMenuItemTests
    {
        [Fact]
        public void ProfileMenuItem_Renders_TextParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

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
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

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
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

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
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

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
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

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

        class TestAntiforgeryStateProvider(AntiforgeryRequestToken token) : AntiforgeryStateProvider
        {
            public override AntiforgeryRequestToken GetAntiforgeryToken() => token;
        }

        static IRenderedComponent<RadzenProfileMenu> RenderMenuWithActionItem(TestContext ctx, string method = null)
        {
            return ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.AddChildContent<RadzenProfileMenuItem>(item =>
                {
                    item.Add(p => p.Text, "Sign out");
                    item.Add(p => p.Icon, "power_settings_new");
                    item.Add(p => p.Action, "Account/Logout");

                    if (method != null)
                    {
                        item.Add(p => p.Method, method);
                    }
                });
            });
        }

        [Fact]
        public void ProfileMenuItem_Action_RendersFormThatPostsToAction()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddSingleton<AntiforgeryStateProvider>(new TestAntiforgeryStateProvider(null));

            var component = RenderMenuWithActionItem(ctx);

            var form = component.Find("li[role=menuitem] form");

            Assert.Equal("Account/Logout", form.GetAttribute("action"));
            Assert.Equal("post", form.GetAttribute("method"));
        }

        [Fact]
        public void ProfileMenuItem_Action_UsesMethod()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddSingleton<AntiforgeryStateProvider>(new TestAntiforgeryStateProvider(null));

            var component = RenderMenuWithActionItem(ctx, "put");

            Assert.Equal("put", component.Find("li[role=menuitem] form").GetAttribute("method"));
        }

        [Fact]
        public void ProfileMenuItem_Action_IncludesAntiforgeryToken()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddSingleton<AntiforgeryStateProvider>(new TestAntiforgeryStateProvider(new AntiforgeryRequestToken("token-value", "__RequestVerificationToken")));

            var component = RenderMenuWithActionItem(ctx);

            var input = component.Find("li[role=menuitem] form input[type=hidden]");

            Assert.Equal("__RequestVerificationToken", input.GetAttribute("name"));
            Assert.Equal("token-value", input.GetAttribute("value"));
        }

        [Fact]
        public void ProfileMenuItem_Action_OmitsAntiforgeryTokenWhenNoneIsAvailable()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddSingleton<AntiforgeryStateProvider>(new TestAntiforgeryStateProvider(null));

            var component = RenderMenuWithActionItem(ctx);

            Assert.Empty(component.FindAll("li[role=menuitem] form input"));
        }

        [Fact]
        public void ProfileMenuItem_Action_WithGetMethod_OmitsAntiforgeryToken()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddSingleton<AntiforgeryStateProvider>(new TestAntiforgeryStateProvider(new AntiforgeryRequestToken("token-value", "__RequestVerificationToken")));

            var component = RenderMenuWithActionItem(ctx, "get");

            Assert.Empty(component.FindAll("li[role=menuitem] form input"));
        }

        [Fact]
        public void ProfileMenuItem_Action_RendersSubmitButtonLikeOtherItems()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddSingleton<AntiforgeryStateProvider>(new TestAntiforgeryStateProvider(null));

            var component = RenderMenuWithActionItem(ctx);

            var button = component.Find("li[role=menuitem] > .rz-navigation-item-wrapper > form > button");

            Assert.Equal("submit", button.GetAttribute("type"));
            Assert.Equal("-1", button.GetAttribute("tabindex"));
            Assert.Equal("rz-navigation-item-link", button.GetAttribute("class"));
            Assert.Equal("power_settings_new", button.QuerySelector("i.notranslate.rzi.rz-navigation-item-icon")?.TextContent);
            Assert.Equal("Sign out", button.QuerySelector("span.rz-navigation-item-text")?.TextContent);
        }

        [Fact]
        public void ProfileMenuItem_WithoutAction_DoesNotRenderForm()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.AddChildContent<RadzenProfileMenuItem>(item =>
                {
                    item.Add(p => p.Text, "Profile");
                    item.Add(p => p.Icon, "person");
                });
            });

            Assert.Empty(component.FindAll("form"));
            Assert.Empty(component.FindAll("button"));
            Assert.Single(component.FindAll("li[role=menuitem] div.rz-navigation-item-link"));
        }

        [Fact]
        public void ProfileMenuItem_PathTakesPrecedenceOverAction()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.AddChildContent<RadzenProfileMenuItem>(item =>
                {
                    item.Add(p => p.Text, "Settings");
                    item.Add(p => p.Path, "/settings");
                    item.Add(p => p.Action, "Account/Logout");
                });
            });

            Assert.Empty(component.FindAll("form"));
            Assert.Contains("href=\"/settings\"", component.Markup);
        }
    }
}
