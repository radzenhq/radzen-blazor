using Bunit;
using Bunit.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class PagerTests
    {
        [Fact]
        public void RadzenPager_AutoHide_If_Count_Is_Less_Than_PageSize()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPager>(parameters =>
            {
                parameters.Add<int>(p => p.PageSize, 20);
                parameters.Add<int>(p => p.Count, 100);
            });

            component.Render();

            Assert.Contains(@$"rz-pager", component.Markup);

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<int>(p => p.PageSize, 101);
                parameters.Add<int>(p => p.Count, 100);
            });
            Assert.DoesNotContain(@$"rz-pager", component.Markup);
        }

        [Fact]
        public void RadzenPager_Dont_AutoHide_If_PageSizeOptions_Specified()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPager>(parameters =>
            {
                parameters.Add<int>(p => p.PageSize, 101);
                parameters.Add<int>(p => p.Count, 100);
                parameters.Add<IEnumerable<int>>(p => p.PageSizeOptions, new int[] { 3, 7, 15 });
            });

            component.Render();

            Assert.Contains(@$"rz-pager", component.Markup);
            Assert.Contains(@$"rz-dropdown-trigger", component.Markup);
        }

        [Fact]
        public async Task RadzenPager_Renders_Summary() {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPager>(parameters => {
                parameters.Add<int>(p => p.PageSize, 10);
                parameters.Add<int>(p => p.Count, 100);
                parameters.Add<bool>(p => p.ShowPagingSummary, true);
            });
            await component.InvokeAsync(() => component.Instance.GoToPage(2));
            component.Render();

            Assert.Contains(@$"rz-pager-summary", component.Markup); 
            Assert.Contains(@$"Page 3 of 10 (100 items)", component.Markup); 
            
            component.SetParametersAndRender(parameters => {
                parameters.Add<bool>(p => p.ShowPagingSummary, false);
            });
            Assert.DoesNotContain(@$"rz-pager-summary", component.Markup);
        }

        [Fact]
        public void RadzenPager_Renders_PagerDensityDefault()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPager>(parameters =>
            {
                parameters.Add<int>(p => p.PageSize, 20);
                parameters.Add<int>(p => p.Count, 100);
                parameters.Add<Density>(p => p.Density, Density.Default);
            });

            Assert.DoesNotContain(@$"rz-density-compact", component.Markup);
        }

        [Fact]
        public void RadzenPager_Renders_PagerDensityCompact()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPager>(parameters =>
            {
                parameters.Add<int>(p => p.PageSize, 20);
                parameters.Add<int>(p => p.Count, 100);
                parameters.Add<Density>(p => p.Density, Density.Compact);
            });

            Assert.Contains(@$"rz-density-compact", component.Markup);
        }

        [Fact]
        public async Task RadzenPager_First_And_Prev_Buttons_Are_Disabled_When_On_The_First_Page()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPager>(parameters => {
                parameters.Add<int>(p => p.PageSize, 10);
                parameters.Add<int>(p => p.Count, 100);
                parameters.Add<bool>(p => p.ShowPagingSummary, true);
            });

            await component.InvokeAsync(() => component.Instance.GoToPage(0));
            component.Render();

            var firstPageButton = component.Find("button.rz-pager-first");
            Assert.True(firstPageButton.HasAttribute("disabled"));

            var prevPageButton = component.Find("button.rz-pager-prev");
            Assert.True(prevPageButton.HasAttribute("disabled"));
        }

        [Fact]
        public async Task RadzenPager_Last_And_Next_Buttons_Are_Disabled_When_On_The_Last_Page()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPager>(parameters => {
                parameters.Add<int>(p => p.PageSize, 10);
                parameters.Add<int>(p => p.Count, 100);
                parameters.Add<bool>(p => p.ShowPagingSummary, true);
            });

            await component.InvokeAsync(() => component.Instance.GoToPage(9));
            component.Render();

            var lastPageButton = component.Find("button.rz-pager-last");
            Assert.True(lastPageButton.HasAttribute("disabled"));

            var nextPageButton = component.Find("button.rz-pager-next");
            Assert.True(nextPageButton.HasAttribute("disabled"));
        }

        [Fact]
        public void RadzenPager_Does_Not_Render_Reload_Button_By_Default()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPager>(parameters =>
            {
                parameters.Add<int>(p => p.PageSize, 10);
                parameters.Add<int>(p => p.Count, 100);
            });

            Assert.DoesNotContain("rz-pager-reload", component.Markup);
        }

        [Fact]
        public void RadzenPager_Renders_Reload_Button_When_AllowReload_Is_True()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPager>(parameters =>
            {
                parameters.Add<int>(p => p.PageSize, 10);
                parameters.Add<int>(p => p.Count, 100);
                parameters.Add<bool>(p => p.AllowReload, true);
            });

            Assert.Contains("rz-pager-reload", component.Markup);
            var reloadButton = component.Find("button.rz-pager-reload");
            Assert.Equal("Reload", reloadButton.GetAttribute("title"));
            Assert.Equal("Reload current page.", reloadButton.GetAttribute("aria-label"));
        }

        [Fact]
        public async Task RadzenPager_Reload_Button_Fires_PageReload_And_PageChanged()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var reloadFired = false;
            PagerEventArgs pageChangedArgs = null;

            var component = ctx.RenderComponent<RadzenPager>(parameters =>
            {
                parameters.Add<int>(p => p.PageSize, 10);
                parameters.Add<int>(p => p.Count, 100);
                parameters.Add<bool>(p => p.AllowReload, true);
                parameters.Add(p => p.PageReload, () => { reloadFired = true; });
                parameters.Add(p => p.PageChanged, (PagerEventArgs args) => { pageChangedArgs = args; });
            });

            await component.InvokeAsync(() => component.Instance.GoToPage(2));

            var reloadButton = component.Find("button.rz-pager-reload");
            await component.InvokeAsync(() => reloadButton.Click());

            Assert.True(reloadFired);
            Assert.NotNull(pageChangedArgs);
            Assert.Equal(20, pageChangedArgs.Skip);
            Assert.Equal(10, pageChangedArgs.Top);
            Assert.Equal(2, pageChangedArgs.PageIndex);
        }
    }
}
