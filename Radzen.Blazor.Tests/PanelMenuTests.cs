using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Generic;
using System.Linq;
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

            public void SimulateNavigate(string uri)
            {
                Uri = ToAbsoluteUri(uri).AbsoluteUri;
                NotifyLocationChanged(false);
            }
        }

        private static string CreatePanelMenu(string currentAbsoluteUrl, NavLinkMatch match, params string[] urls)
            => CreatePanelMenu(currentAbsoluteUrl, match, new Dictionary<string, bool>(urls.Select(url => new KeyValuePair<string, bool>(url, false))));

        private static string CreatePanelMenu(string currentAbsoluteUrl, NavLinkMatch match, Dictionary<string, bool> urls)
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
                    builder.AddAttribute(1, nameof(RadzenPanelMenuItem.Path), url.Key);
                    builder.AddAttribute(2, nameof(RadzenPanelMenuItem.Disabled), url.Value);
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
        public void RadzenPanelMenu_CanDisableMenuItem()
        {
            var urls = new Dictionary<string, bool>
            {
                {"/datagrid", false},
                {"/disabled-url", true}
            };
            var component = CreatePanelMenu("http://www.example.com/", NavLinkMatch.All, urls);

            Assert.Contains("rz-state-disabled", component);
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

        private IRenderedComponent<RadzenPanelMenu> CreatePanelMenuWithItem(TestContext ctx, string currentAbsoluteUrl, string path, bool selected, bool bindSelected)
        {
            ctx.Services.RemoveAll<NavigationManager>();
            ctx.Services.TryAddSingleton<NavigationManager>(new TestNavigationManager(currentAbsoluteUrl));

            return ctx.RenderComponent<RadzenPanelMenu>(parameters => parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<RadzenPanelMenuItem>(0);
                builder.AddAttribute(1, nameof(RadzenPanelMenuItem.Path), path);
                builder.AddAttribute(2, nameof(RadzenPanelMenuItem.Selected), selected);
                if (bindSelected)
                {
                    builder.AddAttribute(3, nameof(RadzenPanelMenuItem.SelectedChanged), EventCallback.Factory.Create<bool>(this, _ => { }));
                }
                builder.CloseComponent();
            }));
        }

        [Fact]
        public void RadzenPanelMenu_DoesNotSelectFromUrl_WhenSelectedIsBound()
        {
            using var ctx = new TestContext();

            var component = CreatePanelMenuWithItem(ctx, "http://www.example.com/foo", "/foo", selected: false, bindSelected: true);

            Assert.DoesNotContain("rz-navigation-item-wrapper-active", component.Markup);
        }

        [Fact]
        public void RadzenPanelMenu_SelectsFromUrl_WhenSelectedIsNotBound()
        {
            using var ctx = new TestContext();

            var component = CreatePanelMenuWithItem(ctx, "http://www.example.com/foo", "/foo", selected: false, bindSelected: false);

            Assert.Contains("rz-navigation-item-wrapper-active", component.Markup);
        }

        [Fact]
        public void RadzenPanelMenu_HonorsBoundSelected_WhenUrlDoesNotMatch()
        {
            using var ctx = new TestContext();

            var component = CreatePanelMenuWithItem(ctx, "http://www.example.com/other", "/foo", selected: true, bindSelected: true);

            Assert.Contains("rz-navigation-item-wrapper-active", component.Markup);
        }

        [Fact]
        public void RadzenPanelMenu_KeepsBoundSelected_AfterLocationChange()
        {
            using var ctx = new TestContext();

            var component = CreatePanelMenuWithItem(ctx, "http://www.example.com/", "/foo", selected: false, bindSelected: true);

            var navigationManager = (TestNavigationManager)ctx.Services.GetService(typeof(NavigationManager));

            component.InvokeAsync(() => navigationManager.SimulateNavigate("/foo")).Wait();

            Assert.DoesNotContain("rz-navigation-item-wrapper-active", component.Markup);
        }

        [Fact]
        public void RadzenPanelMenu_SelectsFromUrl_AfterLocationChange()
        {
            using var ctx = new TestContext();

            var component = CreatePanelMenuWithItem(ctx, "http://www.example.com/", "/foo", selected: false, bindSelected: false);

            var navigationManager = (TestNavigationManager)ctx.Services.GetService(typeof(NavigationManager));

            component.InvokeAsync(() => navigationManager.SimulateNavigate("/foo")).Wait();

            Assert.Contains("rz-navigation-item-wrapper-active", component.Markup);
        }
    }
}