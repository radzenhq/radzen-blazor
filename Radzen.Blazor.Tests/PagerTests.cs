using Bunit;
using Bunit.JSInterop;
using System;
using System.Collections.Generic;
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

            Assert.Contains(@$"rz-paginator", component.Markup);

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<int>(p => p.PageSize, 101);
                parameters.Add<int>(p => p.Count, 100);
            });
            Assert.DoesNotContain(@$"rz-paginator", component.Markup);
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

            Assert.Contains(@$"rz-paginator", component.Markup);
            Assert.Contains(@$"rz-dropdown-trigger", component.Markup);
        }

        [Fact]
        public async void RadzenPager_Renders_Summary() {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenPager>(parameters => {
                parameters.Add<int>(p => p.PageSize, 10);
                parameters.Add<int>(p => p.Count, 100);
                parameters.Add<bool>(p => p.ShowPagingSummary, true);
            });
            await component.Instance.GoToPage(2);
            component.Render();

            Assert.Contains(@$"rz-paginator-summary", component.Markup); 
            Assert.Contains(@$"Page 3 of 10 (100 items)", component.Markup); 
            
            component.SetParametersAndRender(parameters => {
                parameters.Add<bool>(p => p.ShowPagingSummary, false);
            });
            Assert.DoesNotContain(@$"rz-paginator-summary", component.Markup);
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
    }
}
