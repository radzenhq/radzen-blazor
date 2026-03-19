using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class PanelMenuItemTests
    {
        [Fact]
        public void PanelMenuItem_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenPanelMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenPanelMenuItem>(0);
                    builder.AddAttribute(1, nameof(RadzenPanelMenuItem.Text), "Home");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-navigation-item", component.Markup);
        }

        [Fact]
        public void PanelMenuItem_Renders_Text()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenPanelMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenPanelMenuItem>(0);
                    builder.AddAttribute(1, nameof(RadzenPanelMenuItem.Text), "Dashboard");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("Dashboard", component.Markup);
        }

        [Fact]
        public void PanelMenuItem_Renders_Icon()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenPanelMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenPanelMenuItem>(0);
                    builder.AddAttribute(1, nameof(RadzenPanelMenuItem.Text), "Home");
                    builder.AddAttribute(2, nameof(RadzenPanelMenuItem.Icon), "home");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("home", component.Markup);
        }

        [Fact]
        public void PanelMenuItem_Renders_Path()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenPanelMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenPanelMenuItem>(0);
                    builder.AddAttribute(1, nameof(RadzenPanelMenuItem.Text), "Orders");
                    builder.AddAttribute(2, nameof(RadzenPanelMenuItem.Path), "/orders");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("/orders", component.Markup);
        }

        [Fact]
        public void PanelMenuItem_Renders_Disabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenPanelMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenPanelMenuItem>(0);
                    builder.AddAttribute(1, nameof(RadzenPanelMenuItem.Text), "Disabled Item");
                    builder.AddAttribute(2, nameof(RadzenPanelMenuItem.Disabled), true);
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-state-disabled", component.Markup);
        }

        [Fact]
        public void PanelMenuItem_Renders_NestedItems()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenPanelMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenPanelMenuItem>(0);
                    builder.AddAttribute(1, nameof(RadzenPanelMenuItem.Text), "Parent");
                    builder.AddAttribute(2, nameof(RadzenPanelMenuItem.ChildContent), (RenderFragment)(childBuilder =>
                    {
                        childBuilder.OpenComponent<RadzenPanelMenuItem>(0);
                        childBuilder.AddAttribute(1, nameof(RadzenPanelMenuItem.Text), "Child");
                        childBuilder.CloseComponent();
                    }));
                    builder.CloseComponent();
                });
            });

            Assert.Contains("Parent", component.Markup);
            Assert.Contains("Child", component.Markup);
        }

        [Fact]
        public void PanelMenuItem_Renders_Image()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenPanelMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenPanelMenuItem>(0);
                    builder.AddAttribute(1, nameof(RadzenPanelMenuItem.Text), "Profile");
                    builder.AddAttribute(2, nameof(RadzenPanelMenuItem.Image), "avatar.png");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("avatar.png", component.Markup);
        }

        [Fact]
        public void PanelMenuItem_Renders_MultipleItems()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenPanelMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenPanelMenuItem>(0);
                    builder.AddAttribute(1, nameof(RadzenPanelMenuItem.Text), "First");
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenPanelMenuItem>(2);
                    builder.AddAttribute(3, nameof(RadzenPanelMenuItem.Text), "Second");
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenPanelMenuItem>(4);
                    builder.AddAttribute(5, nameof(RadzenPanelMenuItem.Text), "Third");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("First", component.Markup);
            Assert.Contains("Second", component.Markup);
            Assert.Contains("Third", component.Markup);
        }

        [Fact]
        public void PanelMenuItem_Renders_Target()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenPanelMenu>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenPanelMenuItem>(0);
                    builder.AddAttribute(1, nameof(RadzenPanelMenuItem.Text), "External");
                    builder.AddAttribute(2, nameof(RadzenPanelMenuItem.Path), "/external");
                    builder.AddAttribute(3, nameof(RadzenPanelMenuItem.Target), "_blank");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("_blank", component.Markup);
        }
    }
}
