using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ProfileMenuTests
    {
        [Fact]
        public void ProfileMenu_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>();

            Assert.Contains("rz-profile-menu", component.Markup);
            Assert.Contains("rz-menu", component.Markup);
        }

        [Fact]
        public void ProfileMenu_Renders_RoleMenu()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>();

            Assert.Contains(@"role=""menu""", component.Markup);
        }

        [Fact]
        public void ProfileMenu_Renders_ToggleAriaLabel()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.Add(p => p.ToggleAriaLabel, "User menu");
            });

            Assert.Contains("User menu", component.Markup);
        }

        [Fact]
        public void ProfileMenu_Renders_DefaultAriaLabel()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>();

            Assert.Contains("Profile menu", component.Markup);
        }

        [Fact]
        public void ProfileMenu_ShowIcon_True_RendersIcon()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.Add(p => p.ShowIcon, true);
            });

            Assert.Contains("rz-navigation-item-icon-children", component.Markup);
        }

        [Fact]
        public void ProfileMenu_ShowIcon_False_HidesIcon()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.Add(p => p.ShowIcon, false);
            });

            Assert.DoesNotContain("rz-navigation-item-icon-children", component.Markup);
        }

        [Fact]
        public void ProfileMenu_Renders_Template()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.Add(p => p.Template, (RenderFragment)(builder =>
                {
                    builder.AddContent(0, "John Doe");
                }));
            });

            Assert.Contains("John Doe", component.Markup);
        }

        [Fact]
        public void ProfileMenu_StartsCollapsed()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>();

            Assert.Contains("rz-state-collapsed", component.Markup);
            Assert.Contains(@"aria-expanded=""false""", component.Markup);
        }

        [Fact]
        public void ProfileMenu_Toggle_ExpandsMenu()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>();

            component.InvokeAsync(() => component.Instance.Toggle(new MouseEventArgs()));

            Assert.Contains("rz-state-expanded", component.Markup);
            Assert.Contains(@"aria-expanded=""true""", component.Markup);
        }

        [Fact]
        public void ProfileMenu_Toggle_Twice_CollapsesMenu()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>();

            component.InvokeAsync(() => component.Instance.Toggle(new MouseEventArgs()));
            component.InvokeAsync(() => component.Instance.Toggle(new MouseEventArgs()));

            Assert.Contains("rz-state-collapsed", component.Markup);
        }

        [Fact]
        public void ProfileMenu_Close_CollapsesMenu()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>();

            // Open first
            component.InvokeAsync(() => component.Instance.Toggle(new MouseEventArgs()));
            Assert.Contains("rz-state-expanded", component.Markup);

            // Close
            component.InvokeAsync(() => component.Instance.Close());
            Assert.Contains(@"aria-hidden=""true""", component.Markup);
        }

        [Fact]
        public void ProfileMenu_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-profile-menu", component.Markup);
        }

        [Fact]
        public void ProfileMenu_AddItem_TracksItems()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>();

            var item = new RadzenProfileMenuItem();
            component.Instance.AddItem(item);

            Assert.Single(component.Instance.items);
        }

        [Fact]
        public void ProfileMenu_AddItem_NoDuplicates()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>();

            var item = new RadzenProfileMenuItem();
            component.Instance.AddItem(item);
            component.Instance.AddItem(item);

            Assert.Single(component.Instance.items);
        }

        [Fact]
        public void ProfileMenu_IsFocused_ReturnsFalse_WhenNoFocus()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>();

            var item = new RadzenProfileMenuItem();
            component.Instance.AddItem(item);

            Assert.False(component.Instance.IsFocused(item));
        }

        [Fact]
        public void ProfileMenu_IsFocused_ReturnsTrue_WhenFocused()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>();

            var item = new RadzenProfileMenuItem();
            component.Instance.AddItem(item);
            component.Instance.focusedIndex = 0;

            Assert.True(component.Instance.IsFocused(item));
        }

        [Fact]
        public void ProfileMenu_Renders_AriaHaspopup()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>();

            Assert.Contains(@"aria-haspopup=""menu""", component.Markup);
        }
    }
}
