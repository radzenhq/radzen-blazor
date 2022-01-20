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

        private static string CreatePanelMenu(string currentAbsoluteUrl, NavLinkMatch match, params string[] urls)
        {
            using var ctx = new TestContext();

            ctx.Services.RemoveAll<NavigationManager>();
            ctx.Services.TryAddSingleton<NavigationManager>(new TestNavigationManager(currentAbsoluteUrl));

            var component = ctx.RenderComponent<RadzenPanelMenu>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Match, match).AddChildContent(builder => 
            {
                foreach (var url in urls)
                {
                    builder.OpenComponent<RadzenPanelMenuItem>(0);
                    builder.AddAttribute(1, nameof(RadzenPanelMenuItem.Path), url);
                    builder.CloseComponent();
                }
            }));

            return component.Markup;
        }

        [Fact]
        public void RadzenPanelMenu_SetsOneActiveMenuItem()
        {
            var component = CreatePanelMenu("http://www.example.com/datagrid-dynamic", NavLinkMatch.All, "/datagrid", "/datagrid-dynamic");

            var firstIndex = component.IndexOf("rz-navigation-item-wrapper-active");
            var lastIndex = component.LastIndexOf("rz-navigation-item-wrapper-active");

            Assert.NotEqual(-1, firstIndex);
            Assert.Equal(firstIndex, lastIndex);
        }

        [Fact]
        public void RadzenPanelMenu_MatchesQueryStringParameters()
        {
            var component = CreatePanelMenu("http://www.example.com/foo?bar", NavLinkMatch.Prefix, "/foo");

            Assert.Contains("rz-navigation-item-wrapper-active", component);
        }

        [Fact]
        public void RadzenPanelMenu_DoesNotMatchQueryStringParametersWhenExactMatchIsSpecified()
        {
            var component = CreatePanelMenu("http://www.example.com/foo?bar", NavLinkMatch.All, "/foo");

            Assert.DoesNotContain("rz-navigation-item-wrapper-active", component);
        }

        [Fact]
        public void RadzenPanelMenu_DoesNotMatchRootWithEverything()
        {
            var component = CreatePanelMenu("http://www.example.com/foo", NavLinkMatch.Prefix, "/");

            Assert.DoesNotContain("rz-navigation-item-wrapper-active", component);
        }

        [Fact]
        public void RadzenPanelMenu_MatchesRoot()
        {
            var component = CreatePanelMenu("http://www.example.com/", NavLinkMatch.Prefix, "/");

            Assert.Contains("rz-navigation-item-wrapper-active", component);
        }

        [Fact]
        public void RadzenPanelMenu_MatchesRootWithoutTrailingSlash()
        {
            var component = CreatePanelMenu("http://www.example.com", NavLinkMatch.Prefix, "/");

            Assert.Contains("rz-navigation-item-wrapper-active", component);
        }
    }
}