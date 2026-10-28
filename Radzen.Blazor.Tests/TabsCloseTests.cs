#nullable enable

using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TabsCloseTests
    {
        static RenderFragment ClosableTabs(IList<string> titles, string? notClosable = null, string? disabled = null) => builder =>
        {
            foreach (var title in titles)
            {
                builder.OpenComponent(0, typeof(RadzenTabsItem));
                builder.SetKey(title);
                builder.AddAttribute(1, "Text", title);
                builder.AddAttribute(2, "Closable", title != notClosable);
                builder.AddAttribute(3, "Disabled", title == disabled);
                builder.CloseComponent();
            }
        };

        static KeyboardEventArgs Key(string key) => new KeyboardEventArgs { Key = key, Code = key };

        [Fact]
        public void Tabs_ClosableTab_RendersMouseOnlyCloseIcon_AndAnnouncesDelete()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.Tabs, ClosableTabs(new[] { "One", "Two" }, notClosable: "One"))
            );

            var tabs = component.FindAll("button[role='tab']");

            Assert.Empty(tabs[0].QuerySelectorAll(".rz-tabview-close"));
            Assert.Null(tabs[0].GetAttribute("aria-keyshortcuts"));

            var close = tabs[1].QuerySelector(".rz-tabview-close");
            Assert.NotNull(close);
            Assert.Equal("true", close!.GetAttribute("aria-hidden"));
            Assert.Null(close!.GetAttribute("tabindex"));
            Assert.Null(close!.GetAttribute("role"));
            Assert.Equal("Close tab", close!.GetAttribute("title"));
            Assert.Equal("Delete", tabs[1].GetAttribute("aria-keyshortcuts"));
        }

        [Fact]
        public void Tabs_CloseTitle_OverridesCloseIconTooltip()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.CloseTitle, "Dismiss")
                .Add(p => p.Tabs, ClosableTabs(new[] { "One" }))
            );

            Assert.Equal("Dismiss", component.Find(".rz-tabview-close").GetAttribute("title"));
        }

        [Fact]
        public void Tabs_CloseIconClick_RaisesClose_WithoutSelectingTheTab()
        {
            using var ctx = new TestContext();

            TabsCloseEventArgs? closeArgs = null;
            var changeRaised = false;

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.Close, (TabsCloseEventArgs args) => closeArgs = args)
                .Add(p => p.Change, (int _) => changeRaised = true)
                .Add(p => p.Tabs, ClosableTabs(new[] { "One", "Two", "Three" }))
            );

            component.FindAll("button[role='tab']")[1].QuerySelector(".rz-tabview-close")!.Click();

            Assert.NotNull(closeArgs);
            Assert.Equal(1, closeArgs!.Index);
            Assert.Equal("Two", closeArgs!.Tab.Text);
            Assert.False(changeRaised);
            Assert.Equal("true", component.FindAll("button[role='tab']")[0].GetAttribute("aria-selected"));
        }

        [Fact]
        public void Tabs_CloseIconClick_OnDisabledTab_DoesNotRaiseClose()
        {
            using var ctx = new TestContext();

            var closeRaised = false;

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.Close, (TabsCloseEventArgs _) => closeRaised = true)
                .Add(p => p.Tabs, ClosableTabs(new[] { "One", "Two" }, disabled: "Two"))
            );

            component.FindAll("button[role='tab']")[1].QuerySelector(".rz-tabview-close")!.Click();

            Assert.False(closeRaised);
        }

        [Fact]
        public void Tabs_Delete_ClosesTheFocusedTab_NotTheSelectedOne()
        {
            using var ctx = new TestContext();

            TabsCloseEventArgs? closeArgs = null;

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.Close, (TabsCloseEventArgs args) => closeArgs = args)
                .Add(p => p.Tabs, ClosableTabs(new[] { "One", "Two", "Three" }))
            );

            var nav = component.Find("ul.rz-tabview-nav");
            nav.KeyDown(Key("ArrowRight"));
            nav.KeyDown(Key("Delete"));

            Assert.NotNull(closeArgs);
            Assert.Equal(1, closeArgs!.Index);
            Assert.Equal("Two", closeArgs!.Tab.Text);
            Assert.Equal("true", component.FindAll("button[role='tab']")[0].GetAttribute("aria-selected"));
        }

        [Fact]
        public void Tabs_Delete_RightAfterArrow_IsNotSwallowed()
        {
            using var ctx = new TestContext();

            var closeCount = 0;

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.Close, (TabsCloseEventArgs _) => closeCount++)
                .Add(p => p.Tabs, ClosableTabs(new[] { "One", "Two" }))
            );

            var nav = component.Find("ul.rz-tabview-nav");
            nav.KeyDown(Key("ArrowRight"));
            nav.KeyDown(Key("Delete"));

            Assert.Equal(1, closeCount);
        }

        [Fact]
        public void Tabs_Delete_OnNonClosableTab_DoesNothing()
        {
            using var ctx = new TestContext();

            var closeRaised = false;

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.Close, (TabsCloseEventArgs _) => closeRaised = true)
                .Add(p => p.Tabs, ClosableTabs(new[] { "One", "Two" }, notClosable: "One"))
            );

            var renderCount = component.RenderCount;
            var nav = component.Find("ul.rz-tabview-nav");
            nav.KeyDown(Key("Delete"));

            Assert.False(closeRaised);
            Assert.Equal(renderCount, component.RenderCount);
        }

        [Fact]
        public void Tabs_HandledKeys_PropagateToAncestors_OnEveryKeystroke()
        {
            using var ctx = new TestContext();

            var keys = new List<string>();

            RenderFragment host = builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, (KeyboardEventArgs args) => keys.Add(args.Key)));
                builder.OpenComponent<RadzenTabs>(2);
                builder.AddAttribute(3, "Tabs", ClosableTabs(new[] { "One", "Two" }));
                builder.CloseComponent();
                builder.CloseElement();
            };

            var component = ctx.Render(host);

            var nav = component.Find("ul.rz-tabview-nav");
            nav.KeyDown(Key("ArrowRight"));
            nav.KeyDown(Key("Escape"));
            nav.KeyDown(Key("Enter"));
            nav.KeyDown(Key("Escape"));

            Assert.Equal(new[] { "ArrowRight", "Escape", "Enter", "Escape" }, keys);
        }

        [Fact]
        public void Tabs_RemovingSelectedLastTab_SelectsPrecedingTab_AndRaisesSelectedIndexChanged()
        {
            using var ctx = new TestContext();

            var titles = new List<string> { "One", "Two", "Three" };
            var selectedIndex = 2;

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, selectedIndex)
                .Add(p => p.SelectedIndexChanged, (int index) => selectedIndex = index)
                .Add(p => p.Tabs, ClosableTabs(titles))
            );

            titles.Remove("Three");
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Tabs, ClosableTabs(titles)));

            Assert.Equal(1, selectedIndex);
            var selected = component.Find("li.rz-tabview-selected");
            Assert.Contains("Two", selected.TextContent);
            Assert.Equal(1, component.Instance.SelectedTab!.Index);
        }

        [Fact]
        public void Tabs_RemovingSelectedMiddleTab_SelectsFollowingTab()
        {
            using var ctx = new TestContext();

            var titles = new List<string> { "One", "Two", "Three" };
            var selectedIndexChangedRaised = false;

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 1)
                .Add(p => p.SelectedIndexChanged, (int _) => selectedIndexChangedRaised = true)
                .Add(p => p.Tabs, ClosableTabs(titles))
            );

            titles.Remove("Two");
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Tabs, ClosableTabs(titles)));

            var selected = component.Find("li.rz-tabview-selected");
            Assert.Contains("Three", selected.TextContent);
            Assert.False(selectedIndexChangedRaised);
        }

        [Fact]
        public void Tabs_RemovingTabBeforeSelected_KeepsTheSameTabSelected()
        {
            using var ctx = new TestContext();

            var titles = new List<string> { "One", "Two", "Three" };
            var selectedIndex = 2;

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, selectedIndex)
                .Add(p => p.SelectedIndexChanged, (int index) => selectedIndex = index)
                .Add(p => p.Tabs, ClosableTabs(titles))
            );

            titles.Remove("One");
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Tabs, ClosableTabs(titles)));

            Assert.Equal(1, selectedIndex);
            var selected = component.Find("li.rz-tabview-selected");
            Assert.Contains("Three", selected.TextContent);
        }

        [Fact]
        public void Tabs_RemovingSelectedTab_SkipsDisabledFollowingTab()
        {
            using var ctx = new TestContext();

            var titles = new List<string> { "One", "Two", "Three", "Four" };
            var selectedIndex = 1;

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, selectedIndex)
                .Add(p => p.SelectedIndexChanged, (int index) => selectedIndex = index)
                .Add(p => p.Tabs, ClosableTabs(titles, disabled: "Three"))
            );

            titles.Remove("Two");
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Tabs, ClosableTabs(titles, disabled: "Three")));

            Assert.Equal(2, selectedIndex);
            var selected = component.Find("li.rz-tabview-selected");
            Assert.Contains("Four", selected.TextContent);
        }

        [Fact]
        public void Tabs_Delete_MovesKeyboardFocusToTheFollowingTab()
        {
            using var ctx = new TestContext();

            var titles = new List<string> { "One", "Two", "Three", "Four" };

            IRenderedComponent<RadzenTabs> component = null!;

            component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.Close, (TabsCloseEventArgs args) =>
                {
                    titles.Remove(args.Tab.Text!);
                    component.SetParametersAndRender(p => p.Add(x => x.Tabs, ClosableTabs(titles)));
                })
                .Add(p => p.Tabs, ClosableTabs(titles))
            );

            var nav = component.Find("ul.rz-tabview-nav");
            nav.KeyDown(Key("ArrowRight"));
            nav.KeyDown(Key("Delete"));

            Assert.Equal(new[] { "One", "Three", "Four" }, titles);

            nav = component.Find("ul.rz-tabview-nav");
            var active = component.Find($"[id='{nav.GetAttribute("aria-activedescendant")}']");
            Assert.Contains("Three", active.TextContent);
            Assert.Equal("true", component.FindAll("button[role='tab']")[0].GetAttribute("aria-selected"));
        }

        [Fact]
        public void Tabs_HidingAndShowingTheComponent_KeepsTheSelectedTab()
        {
            using var ctx = new TestContext();

            var selectedIndexChangedRaised = false;

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 1)
                .Add(p => p.SelectedIndexChanged, (int _) => selectedIndexChangedRaised = true)
                .Add(p => p.Tabs, ClosableTabs(new[] { "One", "Two", "Three" }))
            );

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, false));
            Assert.Empty(component.FindAll("button[role='tab']"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            var selected = component.Find("li.rz-tabview-selected");
            Assert.Contains("Two", selected.TextContent);
            Assert.False(selectedIndexChangedRaised);
        }

        [Fact]
        public void Tabs_ParentReRender_KeepsKeyboardFocusWhereTheArrowsLeftIt()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.Tabs, ClosableTabs(new[] { "One", "Two", "Three" }))
            );

            component.Find("ul.rz-tabview-nav").KeyDown(Key("ArrowRight"));
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Tabs, ClosableTabs(new[] { "One", "Two", "Three" })));

            var nav = component.Find("ul.rz-tabview-nav");
            var active = component.Find($"[id='{nav.GetAttribute("aria-activedescendant")}']");
            Assert.Contains("Two", active.TextContent);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.SelectedIndex, 2));

            nav = component.Find("ul.rz-tabview-nav");
            active = component.Find($"[id='{nav.GetAttribute("aria-activedescendant")}']");
            Assert.Contains("Three", active.TextContent);
        }

        [Fact]
        public void Tabs_RemovingTheOnlyTab_LeavesNoSelection()
        {
            using var ctx = new TestContext();

            var titles = new List<string> { "One" };
            var selectedIndex = 0;

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, selectedIndex)
                .Add(p => p.SelectedIndexChanged, (int index) => selectedIndex = index)
                .Add(p => p.Tabs, ClosableTabs(titles))
            );

            titles.Remove("One");
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Tabs, ClosableTabs(titles)));

            Assert.Equal(-1, selectedIndex);
            Assert.Null(component.Instance.SelectedTab);
            Assert.Empty(component.FindAll("button[role='tab']"));
        }
    }
}
