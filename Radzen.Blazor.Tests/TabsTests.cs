using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TabsTests
    {
        [Fact]
        public void Tabs_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>();

            Assert.Contains(@"rz-tabview", component.Markup);
        }

        [Fact]
        public void Tabs_Renders_TabPosition_Top()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters =>
            {
                parameters.Add(p => p.TabPosition, TabPosition.Top);
            });

            Assert.Contains("rz-tabview-top", component.Markup);
        }

        [Fact]
        public void Tabs_Renders_TabPosition_Bottom()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters =>
            {
                parameters.Add(p => p.TabPosition, TabPosition.Bottom);
            });

            Assert.Contains("rz-tabview-bottom", component.Markup);
        }

        [Fact]
        public void Tabs_Renders_TabPosition_Left()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters =>
            {
                parameters.Add(p => p.TabPosition, TabPosition.Left);
            });

            Assert.Contains("rz-tabview-left", component.Markup);
        }

        [Fact]
        public void Tabs_Renders_TabPosition_Right()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters =>
            {
                parameters.Add(p => p.TabPosition, TabPosition.Right);
            });

            Assert.Contains("rz-tabview-right", component.Markup);
        }

        [Fact]
        public void Tabs_Renders_TabPosition_TopRight()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters =>
            {
                parameters.Add(p => p.TabPosition, TabPosition.TopRight);
            });

            Assert.Contains("rz-tabview-top-right", component.Markup);
        }

        [Fact]
        public void Tabs_Renders_TabPosition_BottomRight()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters =>
            {
                parameters.Add(p => p.TabPosition, TabPosition.BottomRight);
            });

            Assert.Contains("rz-tabview-bottom-right", component.Markup);
        }

        [Fact]
        public void Tabs_Renders_TabNav()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>();

            Assert.Contains("rz-tabview-nav", component.Markup);
        }

        [Fact]
        public void Tabs_Renders_TabPanels()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>();

            Assert.Contains("rz-tabview-panels", component.Markup);
        }

        [Fact]
        public void Tabs_Wrapper_IsFocusTarget_WithRoleTablist()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>();

            var wrapper = component.Find("div.rz-tabview");
            Assert.Equal("tablist", wrapper.GetAttribute("role"));
            Assert.Equal("0", wrapper.GetAttribute("tabindex"));
        }

        [Fact]
        public void Tabs_InnerNav_HasRolePresentation_ToAvoidDuplicateTablist()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>();

            var nav = component.Find("ul.rz-tabview-nav");
            Assert.NotEqual("tablist", nav.GetAttribute("role"));
        }

        static RenderFragment TabsFragment(params string[] titles) => builder =>
        {
            for (var i = 0; i < titles.Length; i++)
            {
                builder.OpenComponent(i, typeof(RadzenTabsItem));
                builder.AddAttribute(i + 1, "Text", titles[i]);
                builder.CloseComponent();
            }
        };

        [Fact]
        public void Tabs_Wrapper_HasAriaActiveDescendant_PointingAt_SelectedTab()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 1)
                .Add(p => p.Tabs, TabsFragment("One", "Two"))
            );

            var wrapper = component.Find("div.rz-tabview");
            var active = wrapper.GetAttribute("aria-activedescendant");
            Assert.False(string.IsNullOrEmpty(active));

            var activeTab = component.Find($"#{active}");
            Assert.Equal("tab", activeTab.GetAttribute("role"));
            Assert.Equal("true", activeTab.GetAttribute("aria-selected"));
        }

        [Fact]
        public void Tabs_OnlyOneTab_HasAriaSelectedTrue()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 2)
                .Add(p => p.Tabs, TabsFragment("One", "Two", "Three"))
            );

            var selectedTabs = component.FindAll("[role='tab'][aria-selected='true']");
            Assert.Single(selectedTabs);
        }

        static RenderFragment TabsFragmentWithContent(params (string Title, string Content)[] tabs) => builder =>
        {
            var seq = 0;
            foreach (var (title, content) in tabs)
            {
                builder.OpenComponent(seq++, typeof(RadzenTabsItem));
                builder.AddAttribute(seq++, "Text", title);
                builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b => b.AddContent(0, content)));
                builder.CloseComponent();
            }
        };

        [Fact]
        public void Tabs_NonNavigationKeyDown_DoesNotBlockSubsequentRenders()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.Tabs, TabsFragmentWithContent(("First", "First-Content"), ("Second", "Second-Content")))
            );

            Assert.Contains("First-Content", component.Markup);

            var wrapper = component.Find("div.rz-tabview");
            // Simulate non-navigation keystrokes (e.g. a barcode scanner) on the focused tablist.
            // Pre-fix: the second keypress latched shouldRender = false, blocking all later renders.
            wrapper.KeyDown(new KeyboardEventArgs { Key = "a", Code = "KeyA" });
            wrapper.KeyDown(new KeyboardEventArgs { Key = "b", Code = "KeyB" });

            component.SetParametersAndRender(parameters => parameters.Add(p => p.SelectedIndex, 1));

            Assert.Contains("Second-Content", component.Markup);
            Assert.DoesNotContain("First-Content", component.Markup);
        }
    }
}

