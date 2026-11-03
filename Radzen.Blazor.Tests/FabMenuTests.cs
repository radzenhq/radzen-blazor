using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class FabMenuTests
    {
        static IRenderedComponent<RadzenFabMenu> Render(TestContext ctx, bool open, string ariaLabel = "Actions")
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            return ctx.RenderComponent<RadzenFabMenu>(parameters => parameters
                .Add(p => p.IsOpen, open)
                .Add(p => p.AriaLabel, ariaLabel)
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<RadzenFabMenuItem>(0);
                    builder.AddAttribute(1, "Icon", "edit");
                    builder.AddAttribute(2, "AriaLabel", "Edit");
                    builder.CloseComponent();
                    builder.OpenComponent<RadzenFabMenuItem>(3);
                    builder.AddAttribute(4, "Icon", "save");
                    builder.AddAttribute(5, "AriaLabel", "Save");
                    builder.AddAttribute(6, "role", "button");
                    builder.AddAttribute(7, "TabIndex", 0);
                    builder.CloseComponent();
                }));
        }

        [Fact]
        public void FabMenu_Items_AreMenuItems_ReachedWithArrowKeys()
        {
            using var ctx = new TestContext();
            var component = Render(ctx, open: true);

            var menu = component.Find("[role=menu]");
            var items = menu.QuerySelectorAll("button");

            Assert.Equal("Actions", menu.GetAttribute("aria-label"));
            Assert.Equal(2, items.Length);
            Assert.Equal("menuitem", items[0].GetAttribute("role"));
            Assert.Equal("-1", items[0].GetAttribute("tabindex"));
            Assert.Equal("button", items[1].GetAttribute("role"));
            Assert.Equal("0", items[1].GetAttribute("tabindex"));
        }

        [Fact]
        public void FabMenu_Toggle_DescribesTheMenu()
        {
            using var ctx = new TestContext();
            var component = Render(ctx, open: false);

            var toggle = component.Find("button");

            Assert.Equal("menu", toggle.GetAttribute("aria-haspopup"));
            Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
            Assert.Empty(component.FindAll("[role=menu]"));
        }

        [Fact]
        public void FabMenu_CreatesClientHandlersOnce_AndDestroysThemOnDispose()
        {
            using var ctx = new TestContext();
            var component = Render(ctx, open: false);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.IsOpen, true));

            Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.createFabMenu"));

            ctx.DisposeComponents();

            Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.destroyFabMenu"));
        }

        [Fact]
        public async Task FabMenu_KeyboardActivation_FocusesFirstItem_MouseDoesNot()
        {
            using var ctx = new TestContext();
            var component = Render(ctx, open: false);

            await component.Find("button").ClickAsync(new MouseEventArgs { Detail = 0 });

            var register = ctx.JSInterop.Invocations.Last(i => i.Identifier == "Radzen.registerFabMenu");
            Assert.Equal(true, register.Arguments[2]);

            await component.Find("button").ClickAsync(new MouseEventArgs { Detail = 1 });
            await component.Find("button").ClickAsync(new MouseEventArgs { Detail = 1 });

            register = ctx.JSInterop.Invocations.Last(i => i.Identifier == "Radzen.registerFabMenu");
            Assert.Equal(false, register.Arguments[2]);
        }

        [Fact]
        public async Task FabMenu_OpenAsync_FromClient_OpensAndFocusesFirstItem()
        {
            using var ctx = new TestContext();
            var opened = false;
            var component = Render(ctx, open: false);
            component.SetParametersAndRender(parameters => parameters.Add(p => p.IsOpenChanged, (bool value) => opened = value));

            await component.InvokeAsync(() => component.Instance.OpenAsync(true));

            Assert.True(opened);
            Assert.NotEmpty(component.FindAll("[role=menuitem]"));
            var register = ctx.JSInterop.Invocations.Last(i => i.Identifier == "Radzen.registerFabMenu");
            Assert.Equal(true, register.Arguments[2]);
        }

        [Fact]
        public void FabMenu_Visible_Toggle_Recreates_Keyboard_Handlers()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenFabMenu>(parameters => parameters
                .Add(p => p.Visible, false)
                .Add(p => p.AriaLabel, "Actions"));

            Assert.Equal(string.Empty, component.Markup.Trim());
            Assert.Empty(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.createFabMenu"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            Assert.Contains("rz-fabmenu", component.Markup);
            Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.createFabMenu"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, false));

            Assert.Equal(string.Empty, component.Markup.Trim());
            Assert.Single(ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.destroyFabMenu"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            Assert.Equal(2, ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.createFabMenu"));
        }
    }
}
