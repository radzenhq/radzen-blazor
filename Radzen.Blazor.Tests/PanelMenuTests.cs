using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class PanelMenuTests
    {
        class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager(string uri)
            {
                Initialize("http://www.example.com/", uri);
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
            }
        }

        [Fact]
        public void RadzenPanelMenu_SetsOneActiveMenuItem()
        {
            using var ctx = new TestContext();

            ctx.Services.RemoveAll<NavigationManager>();
            ctx.Services.TryAddSingleton<NavigationManager>(new TestNavigationManager("http://www.example.com/datagrid-dynamic"));

            var component = ctx.RenderComponent<RadzenPanelMenu>();

            component.SetParametersAndRender(parameters => parameters.AddChildContent(builder => 
            {
                builder.OpenComponent<RadzenPanelMenuItem>(0);
                builder.AddAttribute(1, nameof(RadzenPanelMenuItem.Path), "/datagrid");
                builder.CloseComponent();

                builder.OpenComponent<RadzenPanelMenuItem>(2);
                builder.AddAttribute(3, nameof(RadzenPanelMenuItem.Path), "/datagrid-dynamic");
                builder.CloseComponent();
            }));

            var firstIndex = component.Markup.IndexOf("rz-navigation-item-wrapper-active");
            var lastIndex = component.Markup.LastIndexOf("rz-navigation-item-wrapper-active");

            Assert.NotEqual(-1, firstIndex);
            Assert.Equal(firstIndex, lastIndex);
        }

        [Fact]
        public void RadzenPanelMenu_MatchesQueryStringParameters()
        {
            using var ctx = new TestContext();

            ctx.Services.RemoveAll<NavigationManager>();
            ctx.Services.TryAddSingleton<NavigationManager>(new TestNavigationManager("http://www.example.com/foo?bar"));

            var component = ctx.RenderComponent<RadzenPanelMenu>();

            component.SetParametersAndRender(parameters => parameters.AddChildContent(builder => 
            {
                builder.OpenComponent<RadzenPanelMenuItem>(0);
                builder.AddAttribute(1, nameof(RadzenPanelMenuItem.Path), "/foo");
                builder.CloseComponent();
            }));

            Assert.Contains("rz-navigation-item-wrapper-active", component.Markup);
        }

        [Fact]
        public void RadzenPanelMenu_DoesNotMatcheQueryStringParametersWhenExactMatchIsSpecified()
        {
            using var ctx = new TestContext();

            ctx.Services.RemoveAll<NavigationManager>();
            ctx.Services.TryAddSingleton<NavigationManager>(new TestNavigationManager("http://www.example.com/foo?bar"));

            var component = ctx.RenderComponent<RadzenPanelMenu>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Match, NavLinkMatch.All).AddChildContent(builder => 
            {
                builder.OpenComponent<RadzenPanelMenuItem>(0);
                builder.AddAttribute(1, nameof(RadzenPanelMenuItem.Path), "/foo");
                builder.CloseComponent();
            }));

            Assert.DoesNotContain("rz-navigation-item-wrapper-active", component.Markup);
        }
    }
}