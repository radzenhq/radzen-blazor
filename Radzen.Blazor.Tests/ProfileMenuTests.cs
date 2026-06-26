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

        [Fact]
        public void ProfileMenu_Toggle_HasRoleMenu_OnPopup()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.AddChildContent<RadzenProfileMenuItem>(itemParameters =>
                {
                    itemParameters.Add(p => p.Text, "Profile");
                });
            });

            var menu = component.Find("ul.rz-navigation-menu");

            Assert.Equal("menu", menu.GetAttribute("role"));
        }

        [Fact]
        public void ProfileMenu_Trigger_HasAriaHaspopupAndExpanded()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>();

            var toggle = component.Find("div.rz-navigation-item-wrapper");

            Assert.Equal("menu", toggle.GetAttribute("aria-haspopup"));
            Assert.Equal("false", toggle.GetAttribute("aria-expanded"));

            component.InvokeAsync(() => component.Instance.Toggle(new MouseEventArgs()));

            Assert.Equal("true", component.Find("div.rz-navigation-item-wrapper").GetAttribute("aria-expanded"));
        }

        [Fact]
        public void ProfileMenu_OuterList_IsPresentation_NotMenu()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>();

            var root = component.Find("ul.rz-profile-menu");

            Assert.Equal("presentation", root.GetAttribute("role"));
        }

        [Fact]
        public void ProfileMenu_Item_HasRoleMenuItem()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.AddChildContent<RadzenProfileMenuItem>(itemParameters =>
                {
                    itemParameters.Add(p => p.Text, "Profile");
                });
            });

            var item = component.Find("li[role=menuitem]");

            Assert.NotNull(item);
        }

        [Fact]
        public void ProfileMenu_ArrowDown_ExposesActiveDescendant()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.AddChildContent<RadzenProfileMenuItem>(itemParameters =>
                {
                    itemParameters.Add(p => p.Text, "Profile");
                });
                parameters.AddChildContent<RadzenProfileMenuItem>(itemParameters =>
                {
                    itemParameters.Add(p => p.Text, "Logout");
                });
            });

            var root = component.Find("ul.rz-profile-menu");
            root.KeyDown(new KeyboardEventArgs { Code = "ArrowDown" });

            var menu = component.Find("ul.rz-navigation-menu");
            var activeId = menu.GetAttribute("aria-activedescendant");

            Assert.False(string.IsNullOrEmpty(activeId));

            var activeItem = component.Find($"li[id=\"{activeId}\"]");
            Assert.Equal("menuitem", activeItem.GetAttribute("role"));
        }

        [Fact]
        public void ProfileMenu_RovingTabindex_FollowsActiveItem()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.AddChildContent<RadzenProfileMenuItem>(itemParameters =>
                {
                    itemParameters.Add(p => p.Text, "Profile");
                });
                parameters.AddChildContent<RadzenProfileMenuItem>(itemParameters =>
                {
                    itemParameters.Add(p => p.Text, "Logout");
                });
            });

            var items = component.FindAll("li[role=menuitem]");
            Assert.All(items, item => Assert.Equal("-1", item.GetAttribute("tabindex")));

            var root = component.Find("ul.rz-profile-menu");
            root.KeyDown(new KeyboardEventArgs { Code = "ArrowDown" });

            var activeItems = component.FindAll("li[role=menuitem][tabindex=\"0\"]");
            Assert.Single(activeItems);
        }

        [Fact]
        public void ProfileMenu_End_ActivatesLastItem()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.AddChildContent<RadzenProfileMenuItem>(itemParameters =>
                {
                    itemParameters.Add(p => p.Text, "Profile");
                });
                parameters.AddChildContent<RadzenProfileMenuItem>(itemParameters =>
                {
                    itemParameters.Add(p => p.Text, "Logout");
                });
            });

            var root = component.Find("ul.rz-profile-menu");
            root.KeyDown(new KeyboardEventArgs { Code = "End" });

            Assert.Equal(1, component.Instance.focusedIndex);

            var menu = component.Find("ul.rz-navigation-menu");
            var activeId = menu.GetAttribute("aria-activedescendant");
            var lastItem = component.FindAll("li[role=menuitem]")[1];

            Assert.Equal(lastItem.Id, activeId);
        }

        [Fact]
        public void ProfileMenu_Home_ActivatesFirstItem()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.AddChildContent<RadzenProfileMenuItem>(itemParameters =>
                {
                    itemParameters.Add(p => p.Text, "Profile");
                });
                parameters.AddChildContent<RadzenProfileMenuItem>(itemParameters =>
                {
                    itemParameters.Add(p => p.Text, "Logout");
                });
            });

            var root = component.Find("ul.rz-profile-menu");
            root.KeyDown(new KeyboardEventArgs { Code = "End" });
            root.KeyDown(new KeyboardEventArgs { Code = "Home" });

            Assert.Equal(0, component.Instance.focusedIndex);
        }

        [Fact]
        public void ProfileMenu_Escape_ClosesAndClearsActiveDescendant()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenProfileMenu>(parameters =>
            {
                parameters.AddChildContent<RadzenProfileMenuItem>(itemParameters =>
                {
                    itemParameters.Add(p => p.Text, "Profile");
                });
            });

            var root = component.Find("ul.rz-profile-menu");
            root.KeyDown(new KeyboardEventArgs { Code = "ArrowDown" });

            Assert.True(component.Instance.focusedIndex >= 0);

            root.KeyDown(new KeyboardEventArgs { Code = "Escape" });

            Assert.Equal(-1, component.Instance.focusedIndex);

            var menu = component.Find("ul.rz-navigation-menu");
            Assert.True(string.IsNullOrEmpty(menu.GetAttribute("aria-activedescendant")));
            Assert.Equal("true", menu.GetAttribute("aria-hidden"));
        }
    }
}
