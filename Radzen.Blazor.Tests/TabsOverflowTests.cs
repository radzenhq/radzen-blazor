using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TabsOverflowTests
    {
        static RenderFragment TabsFragment(params string[] titles) => builder =>
        {
            for (var i = 0; i < titles.Length; i++)
            {
                builder.OpenComponent(i, typeof(RadzenTabsItem));
                builder.AddAttribute(i + 1, "Text", titles[i]);
                builder.CloseComponent();
            }
        };

        static readonly string[] FiveTabs = new[] { "One", "Two", "Three", "Four", "Five" };

        static IRenderedComponent<RadzenTabs> RenderCollapsible(TestContext ctx, int selectedIndex = 0, System.Action<ComponentParameterCollectionBuilder<RadzenTabs>> configure = null)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            return ctx.RenderComponent<RadzenTabs>(parameters =>
            {
                parameters.Add(p => p.Overflow, TabsOverflow.Collapse)
                          .Add(p => p.SelectedIndex, selectedIndex)
                          .Add(p => p.Tabs, TabsFragment(FiveTabs));
                configure?.Invoke(parameters);
            });
        }

        static Task Measure(IRenderedComponent<RadzenTabs> component, double available, double moreSize = 50, double tabSize = 100)
        {
            var indexes = Enumerable.Range(0, FiveTabs.Length).ToArray();
            var sizes = indexes.Select(_ => tabSize).ToArray();

            return component.InvokeAsync(() => component.Instance.OnTabsOverflow(available, moreSize, indexes, sizes));
        }

        static bool IsCollapsed(IRenderedComponent<RadzenTabs> component, int index)
        {
            return component.FindAll("button[role=tab]")[index].ParentElement.ClassList.Contains("rz-tabview-collapsed");
        }

        [Fact]
        public void Tabs_DoesNotRender_OverflowMarkup_ByDefault()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters.Add(p => p.Tabs, TabsFragment(FiveTabs)));

            Assert.DoesNotContain("rz-tabview-overflow", component.Markup);
            Assert.DoesNotContain("rz-tabview-more", component.Markup);
            Assert.DoesNotContain("rz-tabview-collapsed", component.Markup);
        }

        [Theory]
        [InlineData(TabsOverflow.Wrap, "rz-tabview-overflow-wrap")]
        [InlineData(TabsOverflow.Scroll, "rz-tabview-overflow-scroll")]
        [InlineData(TabsOverflow.Collapse, "rz-tabview-overflow-collapse")]
        public void Tabs_Renders_OverflowClass(TabsOverflow overflow, string expectedClass)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters.Add(p => p.Overflow, overflow).Add(p => p.Tabs, TabsFragment(FiveTabs)));

            Assert.Contains(expectedClass, component.Find(".rz-tabview").ClassList);
        }

        [Theory]
        [InlineData(TabsOverflow.Wrap)]
        [InlineData(TabsOverflow.Scroll)]
        public void Tabs_WrapAndScroll_DoNotRender_MoreButton(TabsOverflow overflow)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters.Add(p => p.Overflow, overflow).Add(p => p.Tabs, TabsFragment(FiveTabs)));

            Assert.Empty(component.FindAll(".rz-tabview-more"));
        }

        [Theory]
        [InlineData(TabPosition.Top, "keyboard_arrow_left", "keyboard_arrow_right")]
        [InlineData(TabPosition.Left, "keyboard_arrow_up", "keyboard_arrow_down")]
        public void Tabs_Scroll_Renders_ScrollButtons(TabPosition position, string backwardIcon, string forwardIcon)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.Overflow, TabsOverflow.Scroll)
                .Add(p => p.TabPosition, position)
                .Add(p => p.Tabs, TabsFragment(FiveTabs)));

            Assert.Equal(FiveTabs.Length, component.FindAll("ul[role=tablist] > li").Count);

            var children = component.Find(".rz-tabview").Children;
            Assert.Contains("rz-tabview-scroll-prev", children[0].ClassList);
            Assert.Equal("tablist", children[1].GetAttribute("role"));
            Assert.Contains("rz-tabview-scroll-next", children[2].ClassList);

            var backward = component.Find("button.rz-tabview-scroll-prev");
            Assert.Equal("Scroll tabs backward", backward.GetAttribute("aria-label"));
            Assert.Equal("-1", backward.GetAttribute("tabindex"));
            Assert.True(backward.HasAttribute("hidden"));
            Assert.Equal(backwardIcon, backward.TextContent.Trim());

            var forward = component.Find("button.rz-tabview-scroll-next");
            Assert.Equal("Scroll tabs forward", forward.GetAttribute("aria-label"));
            Assert.True(forward.HasAttribute("hidden"));
            Assert.Equal(forwardIcon, forward.TextContent.Trim());
        }

        [Theory]
        [InlineData(TabsOverflow.Default)]
        [InlineData(TabsOverflow.Wrap)]
        [InlineData(TabsOverflow.Collapse)]
        public void Tabs_OtherModes_DoNotRender_ScrollButtons(TabsOverflow overflow)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters.Add(p => p.Overflow, overflow).Add(p => p.Tabs, TabsFragment(FiveTabs)));

            Assert.Empty(component.FindAll(".rz-tabview-scroll"));
        }

        [Theory]
        [InlineData(TabsOverflow.Default)]
        [InlineData(TabsOverflow.Wrap)]
        [InlineData(TabsOverflow.Scroll)]
        [InlineData(TabsOverflow.Collapse)]
        public async Task Tabs_Tablist_Contains_Only_Tabs(TabsOverflow overflow)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters.Add(p => p.Overflow, overflow).Add(p => p.Tabs, TabsFragment(FiveTabs)));

            if (overflow == TabsOverflow.Collapse)
            {
                await Measure(component, available: 350);
            }

            var tablist = component.Find("ul[role=tablist]");
            Assert.All(tablist.Children, li => Assert.NotNull(li.QuerySelector("[role=tab]")));
            Assert.All(tablist.QuerySelectorAll("button"), button => Assert.Equal("tab", button.GetAttribute("role")));
            Assert.Equal("tablist", component.Find(".rz-tabview-panels").PreviousElementSibling?.GetAttribute("role") ?? component.Find(".rz-tabview > ul").GetAttribute("role"));
        }

        [Fact]
        public async Task Tabs_Collapse_Sets_AriaSetSize_And_PosInSet_For_Visible_Tabs()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.Overflow, TabsOverflow.Collapse)
                .Add(p => p.RenderMode, TabRenderMode.Client)
                .Add(p => p.Tabs, builder =>
                {
                    for (var i = 0; i < FiveTabs.Length; i++)
                    {
                        builder.OpenComponent(i * 3, typeof(RadzenTabsItem));
                        builder.AddAttribute(i * 3 + 1, "Text", FiveTabs[i]);
                        builder.AddAttribute(i * 3 + 2, "Visible", i != 1);
                        builder.CloseComponent();
                    }
                }));

            await Measure(component, available: 350);

            var tabs = component.FindAll("button[role=tab]");
            Assert.All(tabs.Where(t => t.GetAttribute("aria-setsize") != null), t => Assert.Equal("4", t.GetAttribute("aria-setsize")));
            Assert.Equal("1", tabs[0].GetAttribute("aria-posinset"));
            Assert.Null(tabs[1].GetAttribute("aria-posinset"));
            Assert.Equal("2", tabs[2].GetAttribute("aria-posinset"));
            Assert.Equal("4", tabs[4].GetAttribute("aria-posinset"));
        }

        [Fact]
        public void Tabs_Default_DoesNotRender_AriaSetSize()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters.Add(p => p.Tabs, TabsFragment(FiveTabs)));

            Assert.DoesNotContain("aria-setsize", component.Markup);
            Assert.DoesNotContain("aria-posinset", component.Markup);
        }

        [Fact]
        public void Tabs_Collapse_Renders_HiddenMoreButton_BeforeMeasurement()
        {
            using var ctx = new TestContext();
            var component = RenderCollapsible(ctx);

            var more = component.Find(".rz-tabview-more");
            Assert.Contains("rz-tabview-collapsed", more.ClassList);
            Assert.Equal(".rz-tabview", more.ParentElement.ClassList.Contains("rz-tabview") ? ".rz-tabview" : more.ParentElement.TagName);

            var button = more.QuerySelector("button.rz-tabview-more-button");
            Assert.Equal("menu", button.GetAttribute("aria-haspopup"));
            Assert.Equal("false", button.GetAttribute("aria-expanded"));
            Assert.Equal("More tabs", button.GetAttribute("aria-label"));
            Assert.Equal(button.GetAttribute("aria-controls"), more.QuerySelector(".rz-tabview-more-popup").Id);

            Assert.All(Enumerable.Range(0, FiveTabs.Length), i => Assert.False(IsCollapsed(component, i)));
        }

        [Fact]
        public async Task Tabs_Collapse_CollapsesTabsThatDoNotFit_AndKeepsSelectedVisible()
        {
            using var ctx = new TestContext();
            var component = RenderCollapsible(ctx, selectedIndex: 4);

            await Measure(component, available: 350);

            Assert.False(IsCollapsed(component, 0));
            Assert.False(IsCollapsed(component, 1));
            Assert.True(IsCollapsed(component, 2));
            Assert.True(IsCollapsed(component, 3));
            Assert.False(IsCollapsed(component, 4));

            Assert.DoesNotContain("rz-tabview-collapsed", component.Find(".rz-tabview-more").ClassList);

            var items = component.FindAll(".rz-tabview-more-popup [role=menuitem]");
            Assert.Equal(new[] { "Three", "Four" }, items.Select(item => item.TextContent.Trim()));
        }

        [Fact]
        public async Task Tabs_Collapse_CollapsesNothing_WhenEverythingFits()
        {
            using var ctx = new TestContext();
            var component = RenderCollapsible(ctx);

            await Measure(component, available: 500);

            Assert.All(Enumerable.Range(0, FiveTabs.Length), i => Assert.False(IsCollapsed(component, i)));
            Assert.Contains("rz-tabview-collapsed", component.Find(".rz-tabview-more").ClassList);
            Assert.Empty(component.FindAll(".rz-tabview-more-popup [role=menuitem]"));
        }

        [Fact]
        public async Task Tabs_Collapse_SelectingFromMenu_SelectsTab_AndKeepsItVisible()
        {
            using var ctx = new TestContext();
            var changed = -1;
            var bound = -1;
            var component = RenderCollapsible(ctx, configure: parameters => parameters
                .Add(p => p.Change, index => changed = index)
                .Add(p => p.SelectedIndexChanged, index => bound = index));

            await Measure(component, available: 350);

            Assert.True(IsCollapsed(component, 3));

            var item = component.FindAll(".rz-tabview-more-popup [role=menuitem]").Single(i => i.TextContent.Trim() == "Four");
            await item.ClickAsync(new MouseEventArgs());

            Assert.Equal(3, changed);
            Assert.Equal(3, bound);
            Assert.Equal("true", component.FindAll("button[role=tab]")[3].GetAttribute("aria-selected"));

            Assert.False(IsCollapsed(component, 0));
            Assert.False(IsCollapsed(component, 1));
            Assert.True(IsCollapsed(component, 2));
            Assert.False(IsCollapsed(component, 3));
            Assert.True(IsCollapsed(component, 4));
        }

        [Fact]
        public async Task Tabs_Collapse_SelectingFromMenu_RespectsCanChange()
        {
            using var ctx = new TestContext();
            var component = RenderCollapsible(ctx, configure: parameters => parameters
                .Add(p => p.CanChange, args => args.PreventDefault()));

            await Measure(component, available: 350);

            var item = component.FindAll(".rz-tabview-more-popup [role=menuitem]").Single(i => i.TextContent.Trim() == "Four");
            await item.ClickAsync(new MouseEventArgs());

            Assert.Equal("true", component.FindAll("button[role=tab]")[0].GetAttribute("aria-selected"));
            Assert.True(IsCollapsed(component, 3));
        }

        [Fact]
        public async Task Tabs_Collapse_KeyboardFocus_KeepsFocusedTabVisible()
        {
            using var ctx = new TestContext();
            var component = RenderCollapsible(ctx);

            await Measure(component, available: 350);

            Assert.True(IsCollapsed(component, 3));
            Assert.True(IsCollapsed(component, 4));

            await component.Find("ul[role=tablist]").KeyDownAsync(new KeyboardEventArgs { Code = "End" });

            Assert.False(IsCollapsed(component, 0));
            Assert.False(IsCollapsed(component, 1));
            Assert.True(IsCollapsed(component, 2));
            Assert.True(IsCollapsed(component, 3));
            Assert.False(IsCollapsed(component, 4));
            Assert.Contains("rz-state-focused", component.FindAll("button[role=tab]")[4].ParentElement.ClassList);
        }

        [Fact]
        public async Task Tabs_Collapse_DisabledTab_IsRenderedDisabled_InMenu()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.Overflow, TabsOverflow.Collapse)
                .Add(p => p.Tabs, builder =>
                {
                    for (var i = 0; i < FiveTabs.Length; i++)
                    {
                        builder.OpenComponent(i * 3, typeof(RadzenTabsItem));
                        builder.AddAttribute(i * 3 + 1, "Text", FiveTabs[i]);
                        builder.AddAttribute(i * 3 + 2, "Disabled", i == 4);
                        builder.CloseComponent();
                    }
                }));

            await Measure(component, available: 350);

            var item = component.FindAll(".rz-tabview-more-popup [role=menuitem]").Single(i => i.TextContent.Trim() == "Five");
            Assert.True(item.HasAttribute("disabled"));
            Assert.Contains("rz-state-disabled", item.ClassList);
        }

        [Fact]
        public async Task Tabs_Collapse_ClickingMoreButton_TogglesAriaExpanded()
        {
            using var ctx = new TestContext();
            var component = RenderCollapsible(ctx);

            await Measure(component, available: 350);

            var button = component.Find("button.rz-tabview-more-button");
            await button.ClickAsync(new MouseEventArgs());
            Assert.Equal("true", component.Find("button.rz-tabview-more-button").GetAttribute("aria-expanded"));

            await component.InvokeAsync(() => component.Instance.OnMorePopupClose());
            Assert.Equal("false", component.Find("button.rz-tabview-more-button").GetAttribute("aria-expanded"));
        }

        [Fact]
        public async Task Tabs_Collapse_SwitchingOverflow_RemovesMoreButton_AndCollapsedState()
        {
            using var ctx = new TestContext();
            var component = RenderCollapsible(ctx);

            await Measure(component, available: 350);
            Assert.True(IsCollapsed(component, 3));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Overflow, TabsOverflow.Default));

            Assert.Empty(component.FindAll(".rz-tabview-more"));
            Assert.DoesNotContain("rz-tabview-collapsed", component.Markup);
            Assert.DoesNotContain("rz-tabview-overflow", component.Find(".rz-tabview").ClassName);
        }

        [Fact]
        public async Task Tabs_Collapse_IgnoresInvisibleTabs()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.Overflow, TabsOverflow.Collapse)
                .Add(p => p.RenderMode, TabRenderMode.Client)
                .Add(p => p.Tabs, builder =>
                {
                    for (var i = 0; i < FiveTabs.Length; i++)
                    {
                        builder.OpenComponent(i * 3, typeof(RadzenTabsItem));
                        builder.AddAttribute(i * 3 + 1, "Text", FiveTabs[i]);
                        builder.AddAttribute(i * 3 + 2, "Visible", i != 2);
                        builder.CloseComponent();
                    }
                }));

            await Measure(component, available: 350);

            var items = component.FindAll(".rz-tabview-more-popup [role=menuitem]");
            Assert.DoesNotContain("Three", items.Select(item => item.TextContent.Trim()));
        }
    }
}
