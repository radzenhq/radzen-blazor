using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SplitButtonTests
    {
        private static IRenderedComponent<RadzenSplitButton> RenderWithItems(TestContext ctx)
        {
            return ctx.RenderComponent<RadzenSplitButton>(parameters => parameters
                .Add(p => p.Text, "Save")
                .AddChildContent<RadzenSplitButtonItem>(item => item
                    .Add(p => p.Text, "Save and Close")
                    .Add(p => p.Value, "save-close"))
                .AddChildContent<RadzenSplitButtonItem>(item => item
                    .Add(p => p.Text, "Save As...")
                    .Add(p => p.Value, "save-as")));
        }

        [Fact]
        public void SplitButton_Renders_MenuRoles()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderWithItems(ctx);

            Assert.Contains(@"role=""menu""", component.Markup);
            Assert.Equal(2, component.FindAll(@"li[role=""menuitem""]").Count);
        }

        [Fact]
        public void SplitButton_Renders_ToggleButton_AriaHasPopupAndExpanded()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderWithItems(ctx);

            var toggle = component.Find("button.rz-splitbutton-menubutton");

            Assert.Equal("menu", toggle.GetAttribute("aria-haspopup"));
            Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
        }

        [Fact]
        public void SplitButton_ArrowDown_OpensMenu_SetsAriaExpandedAndActiveDescendant()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderWithItems(ctx);

            component.Find("button.rz-splitbutton-menubutton").KeyDown(new KeyboardEventArgs { Code = "ArrowDown" });

            var toggle = component.Find("button.rz-splitbutton-menubutton");

            Assert.Equal("true", toggle.GetAttribute("aria-expanded"));

            var activeDescendant = component.Find(@"ul[role=""menu""]").GetAttribute("aria-activedescendant");
            Assert.False(string.IsNullOrEmpty(activeDescendant));

            var firstItem = component.FindAll(@"li[role=""menuitem""]")[0];
            Assert.Equal(firstItem.Id, activeDescendant);
        }

        [Fact]
        public void SplitButton_ArrowDown_MovesActiveDescendant()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderWithItems(ctx);

            component.Find("button.rz-splitbutton-menubutton").KeyDown(new KeyboardEventArgs { Code = "ArrowDown" });
            component.Find(@"ul[role=""menu""]").KeyDown(new KeyboardEventArgs { Code = "ArrowDown" });

            var items = component.FindAll(@"li[role=""menuitem""]");
            var activeDescendant = component.Find(@"ul[role=""menu""]").GetAttribute("aria-activedescendant");

            Assert.Equal(items[1].Id, activeDescendant);
            Assert.Equal("-1", items[0].GetAttribute("tabindex"));
            Assert.Equal("-1", items[1].GetAttribute("tabindex"));
        }

        [Fact]
        public void SplitButton_End_ActivatesLastItem()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderWithItems(ctx);

            component.Find("button.rz-splitbutton-menubutton").KeyDown(new KeyboardEventArgs { Code = "ArrowDown" });
            component.Find(@"ul[role=""menu""]").KeyDown(new KeyboardEventArgs { Code = "End" });

            var items = component.FindAll(@"li[role=""menuitem""]");
            var activeDescendant = component.Find(@"ul[role=""menu""]").GetAttribute("aria-activedescendant");

            Assert.Equal(items[items.Count - 1].Id, activeDescendant);
        }

        [Fact]
        public void SplitButton_Home_ActivatesFirstItem()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderWithItems(ctx);

            component.Find("button.rz-splitbutton-menubutton").KeyDown(new KeyboardEventArgs { Code = "ArrowDown" });
            component.Find(@"ul[role=""menu""]").KeyDown(new KeyboardEventArgs { Code = "End" });
            component.Find(@"ul[role=""menu""]").KeyDown(new KeyboardEventArgs { Code = "Home" });

            var items = component.FindAll(@"li[role=""menuitem""]");
            var activeDescendant = component.Find(@"ul[role=""menu""]").GetAttribute("aria-activedescendant");

            Assert.Equal(items[0].Id, activeDescendant);
        }

        [Fact]
        public void SplitButton_ArrowUp_OpensMenu_FocusesLastItem()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderWithItems(ctx);

            component.Find("button.rz-splitbutton-menubutton").KeyDown(new KeyboardEventArgs { Code = "ArrowUp" });

            var items = component.FindAll(@"li[role=""menuitem""]");
            var activeDescendant = component.Find(@"ul[role=""menu""]").GetAttribute("aria-activedescendant");

            Assert.Equal(items[items.Count - 1].Id, activeDescendant);
        }

        [Fact]
        public void SplitButton_Escape_ClosesMenu_ClearsAriaExpandedAndActiveDescendant()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderWithItems(ctx);

            component.Find("button.rz-splitbutton-menubutton").KeyDown(new KeyboardEventArgs { Code = "ArrowDown" });
            Assert.Equal("true", component.Find("button.rz-splitbutton-menubutton").GetAttribute("aria-expanded"));

            component.Find(@"ul[role=""menu""]").KeyDown(new KeyboardEventArgs { Code = "Escape" });

            Assert.Equal("false", component.Find("button.rz-splitbutton-menubutton").GetAttribute("aria-expanded"));
            Assert.Null(component.Find(@"ul[role=""menu""]").GetAttribute("aria-activedescendant"));
        }

        [Fact]
        public void SplitButton_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitButton>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void SplitButton_Renders_TextParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitButton>();

            var text = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Text, text));

            Assert.Contains(@$"<span class=""rz-button-text"">{text}</span>", component.Markup);
        }

        [Fact]
        public void SplitButton_Renders_IconParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitButton>();

            var icon = "account_circle";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Icon, icon));

            Assert.Contains(@$"<i class=""notranslate rz-button-icon-left rzi"" aria-hidden=""true"">{icon}</i>", component.Markup);
        }

        [Fact]
        public void SplitButton_Renders_IconAndTextParameters()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitButton>();

            var text = "Test";
            var icon = "account_circle";

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.Text, text);
                parameters.Add(p => p.Icon, icon);
            });

            Assert.Contains(@$"<i class=""notranslate rz-button-icon-left rzi"" aria-hidden=""true"">{icon}</i>", component.Markup);
            Assert.Contains(@$"<span class=""rz-button-text"">{text}</span>", component.Markup);
        }

        [Fact]
        public void SplitButton_Renders_ImageParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitButton>();

            var image = "test.png";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Image, image));

            Assert.Contains(@$"<img class=""notranslate rz-button-icon-left rzi"" src=""{image}"" alt=""image"" />", component.Markup);
        }

        [Fact]
        public void SplitButton_Renders_ImageAndTextParameters()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitButton>();

            var text = "Test";
            var image = "test.png";

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.Text, text);
                parameters.Add(p => p.Image, image);
                parameters.Add(p => p.ImageAlternateText, text);
            });

            Assert.Contains(@$"<img class=""notranslate rz-button-icon-left rzi"" src=""{image}"" alt=""{text}"" />", component.Markup);
            Assert.Contains(@$"<span class=""rz-button-text"">{text}</span>", component.Markup);
        }

        [Fact]
        public void SplitButton_Renders_ButtonContent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            RenderFragment buttonContent = (builder) => builder.AddMarkupContent(0, "<strong>Custom button content</strong>");

            var text = "Test";
            var component = ctx.RenderComponent<RadzenSplitButton>(parameters => parameters
            .Add(p => p.ButtonContent, buttonContent)
            .Add(p => p.Text, text));

            Assert.Contains(@$"<strong>Custom button content</strong>", component.Markup);
            Assert.DoesNotContain(@$"<span class=""rz-button-text"">{text}</span>", component.Markup);
        }

        [Fact]
        public void SplitButton_Renders_DisabledParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitButton>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Disabled, true));

            Assert.Contains(@$"rz-state-disabled", component.Markup);
        }

        [Fact]
        public void SplitButton_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitButton>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }

        [Fact]
        public void SplitButton_RecreatesClickHandler_WhenVisibleToggled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderWithItems(ctx);

            Assert.Equal(1, ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.createSplitButton"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, false));

            Assert.Equal(1, ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.destroyPopup"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            Assert.Equal(2, ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.createSplitButton"));
        }

        [Fact]
        public void SplitButton_CreatesClickHandler_WhenInitiallyHiddenBecomesVisible()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitButton>(parameters => parameters
                .Add(p => p.Text, "Save")
                .Add(p => p.Visible, false));

            Assert.Equal(0, ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.createSplitButton"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            Assert.Equal(1, ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.createSplitButton"));
        }

        [Fact]
        public void SplitButton_DoesNotRecreateClickHandler_OnUnrelatedRender()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = RenderWithItems(ctx);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Text, "Updated"));

            Assert.Equal(1, ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.createSplitButton"));
        }

        [Fact]
        public void SplitButton_Raises_ClickEvent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSplitButton>();

            var clicked = false;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Click, args => { clicked = true; }));

            component.Find("button").Click();

            Assert.True(clicked);
        }
    }
}
