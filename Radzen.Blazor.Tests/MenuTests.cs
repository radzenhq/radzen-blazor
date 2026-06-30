using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class MenuTests
    {
        static RenderFragment MenuWithSubmenu => builder =>
        {
            builder.OpenComponent<RadzenMenuItem>(0);
            builder.AddAttribute(1, "Text", "Home");
            builder.AddAttribute(2, "id", "home-item");
            builder.CloseComponent();

            builder.OpenComponent<RadzenMenuItem>(3);
            builder.AddAttribute(4, "Text", "Data");
            builder.AddAttribute(5, "id", "data-item");
            builder.AddAttribute(6, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<RadzenMenuItem>(0);
                inner.AddAttribute(1, "Text", "Orders");
                inner.AddAttribute(2, "id", "orders-item");
                inner.CloseComponent();
            }));
            builder.CloseComponent();
        };

        [Fact]
        public void Menu_Renders_Menubar_Role_And_Orientation()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, MenuWithSubmenu);
            });

            var menubar = component.Find("ul[role=menubar]");
            Assert.Equal("horizontal", menubar.GetAttribute("aria-orientation"));
        }

        [Fact]
        public void Menu_Renders_MenuItem_Roles_And_Haspopup()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, MenuWithSubmenu);
            });

            var items = component.FindAll("li[role=menuitem]");
            Assert.True(items.Count >= 3);

            var dataItem = component.Find("#data-item");
            Assert.Equal("menu", dataItem.GetAttribute("aria-haspopup"));
            Assert.Equal("false", dataItem.GetAttribute("aria-expanded"));
            Assert.Equal("data-item-submenu", dataItem.GetAttribute("aria-controls"));

            var homeItem = component.Find("#home-item");
            Assert.Null(homeItem.GetAttribute("aria-haspopup"));
            Assert.Null(homeItem.GetAttribute("aria-expanded"));
        }

        [Fact]
        public void Menu_Exposes_ActiveDescendant_On_Focus()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, MenuWithSubmenu);
            });

            var menubar = component.Find("ul[role=menubar]");
            Assert.True(string.IsNullOrEmpty(menubar.GetAttribute("aria-activedescendant")));

            menubar.Focus();

            menubar = component.Find("ul[role=menubar]");
            Assert.Equal("home-item", menubar.GetAttribute("aria-activedescendant"));
        }

        [Fact]
        public void Menu_Keeps_Items_NonTabbable_And_Roves_ActiveDescendant()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, MenuWithSubmenu);
            });

            var menubar = component.Find("ul[role=menubar]");
            menubar.Focus();

            Assert.Equal("-1", component.Find("#home-item").GetAttribute("tabindex"));
            Assert.Equal("-1", component.Find("#data-item").GetAttribute("tabindex"));
            Assert.Equal("home-item", component.Find("ul[role=menubar]").GetAttribute("aria-activedescendant"));

            menubar = component.Find("ul[role=menubar]");
            menubar.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowRight" });

            Assert.Equal("-1", component.Find("#home-item").GetAttribute("tabindex"));
            Assert.Equal("-1", component.Find("#data-item").GetAttribute("tabindex"));
            Assert.Equal("data-item", component.Find("ul[role=menubar]").GetAttribute("aria-activedescendant"));
        }

        [Fact]
        public void Menu_ActiveDescendant_Moves_With_Arrow_Keys()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, MenuWithSubmenu);
            });

            var menubar = component.Find("ul[role=menubar]");
            menubar.Focus();

            menubar = component.Find("ul[role=menubar]");
            menubar.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowRight" });
            Assert.Equal("data-item", component.Find("ul[role=menubar]").GetAttribute("aria-activedescendant"));

            menubar = component.Find("ul[role=menubar]");
            menubar.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowLeft" });
            Assert.Equal("home-item", component.Find("ul[role=menubar]").GetAttribute("aria-activedescendant"));
        }

        [Fact]
        public void Menu_Aria_Expanded_Reflects_Submenu_State()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, MenuWithSubmenu);
            });

            var menubar = component.Find("ul[role=menubar]");
            menubar.Focus();

            menubar = component.Find("ul[role=menubar]");
            menubar.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowRight" });

            menubar = component.Find("ul[role=menubar]");
            menubar.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowDown" });

            Assert.Equal("true", component.Find("#data-item").GetAttribute("aria-expanded"));
        }

        [Fact]
        public void Menu_Home_End_Move_ActiveDescendant_To_First_And_Last()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, MenuWithSubmenu);
            });

            var menubar = component.Find("ul[role=menubar]");
            menubar.Focus();

            menubar = component.Find("ul[role=menubar]");
            menubar.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "End" });
            Assert.Equal("data-item", component.Find("ul[role=menubar]").GetAttribute("aria-activedescendant"));

            menubar = component.Find("ul[role=menubar]");
            menubar.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "Home" });
            Assert.Equal("home-item", component.Find("ul[role=menubar]").GetAttribute("aria-activedescendant"));
        }

        [Fact]
        public void Menu_Submenu_Is_Labelled_By_Parent_Item()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, MenuWithSubmenu);
            });

            var submenu = component.Find("#data-item-submenu");
            Assert.Equal("data-item", submenu.GetAttribute("aria-labelledby"));
        }

        [Fact]
        public void ContextMenu_ArrowDown_Moves_Focus_Without_Opening_Submenu()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.IsContextMenu, true);
                parameters.Add(p => p.Responsive, false);
                parameters.Add(p => p.ChildContent, MenuWithSubmenu);
            });

            var menu = component.Find("ul[role=menu]");
            menu.Focus();

            menu = component.Find("ul[role=menu]");
            menu.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowDown" });

            Assert.Equal("data-item", component.Find("ul[role=menu]").GetAttribute("aria-activedescendant"));
            Assert.Equal("false", component.Find("#data-item").GetAttribute("aria-expanded"));
        }

        [Fact]
        public void ContextMenu_ArrowRight_Opens_Submenu()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.IsContextMenu, true);
                parameters.Add(p => p.Responsive, false);
                parameters.Add(p => p.ChildContent, MenuWithSubmenu);
            });

            var menu = component.Find("ul[role=menu]");
            menu.Focus();

            menu = component.Find("ul[role=menu]");
            menu.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowDown" });

            menu = component.Find("ul[role=menu]");
            menu.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowRight" });

            Assert.Equal("true", component.Find("#data-item").GetAttribute("aria-expanded"));
            Assert.Equal("orders-item", component.Find("ul[role=menu]").GetAttribute("aria-activedescendant"));
        }

        [Fact]
        public void Menu_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>();

            Assert.Contains(@"rz-menu", component.Markup);
        }

        [Fact]
        public void Menu_Renders_Responsive_True()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.Responsive, true);
            });

            Assert.Contains("rz-menu-closed", component.Markup);
            Assert.Contains("rz-menu-toggle", component.Markup);
        }

        [Fact]
        public void Menu_Renders_Responsive_False()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.Responsive, false);
            });

            Assert.DoesNotContain("rz-menu-toggle", component.Markup);
            Assert.DoesNotContain("rz-menu-closed", component.Markup);
        }


        [Fact]
        public void Menu_Renders_CustomToggleAriaLabel()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.Responsive, true);
                parameters.Add(p => p.ToggleAriaLabel, "Open navigation");
            });

            Assert.Contains("Open navigation", component.Markup);
        }

        [Fact]
        public void Menu_Renders_Flyout_Class_When_Enabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.Flyout, true);
            });

            Assert.Contains("rz-menu-flyout", component.Markup);
        }

        [Fact]
        public void Menu_Does_Not_Render_Flyout_Class_By_Default()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>();

            Assert.DoesNotContain("rz-menu-flyout", component.Markup);
        }

        [Fact]
        public void Menu_Flyout_Arrow_Icon_Is_Right_For_Nested_Items()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.Flyout, true);
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    // Top-level item with children
                    builder.OpenComponent<RadzenMenuItem>(0);
                    builder.AddAttribute(1, "Text", "Parent");
                    builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
                    {
                        // Nested item with children (should get right arrow)
                        innerBuilder.OpenComponent<RadzenMenuItem>(0);
                        innerBuilder.AddAttribute(1, "Text", "Child");
                        innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(leafBuilder =>
                        {
                            leafBuilder.OpenComponent<RadzenMenuItem>(0);
                            leafBuilder.AddAttribute(1, "Text", "Leaf");
                            leafBuilder.CloseComponent();
                        }));
                        innerBuilder.CloseComponent();
                    }));
                    builder.CloseComponent();
                }));
            });

            var markup = component.Markup;

            // All flyout items with children should have keyboard_arrow_right
            Assert.Contains("keyboard_arrow_right", markup);
            Assert.DoesNotContain("keyboard_arrow_down", markup);
        }

        [Fact]
        public void Menu_Flyout_Submenu_Has_Data_Flyout_Attribute()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenMenu>(parameters =>
            {
                parameters.Add(p => p.Flyout, true);
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.OpenComponent<RadzenMenuItem>(0);
                    builder.AddAttribute(1, "Text", "Parent");
                    builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
                    {
                        innerBuilder.OpenComponent<RadzenMenuItem>(0);
                        innerBuilder.AddAttribute(1, "Text", "Child");
                        innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(leafBuilder =>
                        {
                            leafBuilder.OpenComponent<RadzenMenuItem>(0);
                            leafBuilder.AddAttribute(1, "Text", "Leaf");
                            leafBuilder.CloseComponent();
                        }));
                        innerBuilder.CloseComponent();
                    }));
                    builder.CloseComponent();
                }));
            });

            // Nested submenu ul should have data-flyout="true"
            Assert.Contains("data-flyout=\"true\"", component.Markup);
        }
    }
}

