using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class MenuTests
    {
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

            // Top-level item should still have keyboard_arrow_down
            Assert.Contains("keyboard_arrow_down", markup);
            // Nested item with children should have keyboard_arrow_right
            Assert.Contains("keyboard_arrow_right", markup);
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

