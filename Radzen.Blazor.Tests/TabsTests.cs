using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
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
        public void Tabs_Nav_IsFocusTarget_WithRoleTablist()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>();

            var nav = component.Find("ul.rz-tabview-nav");
            Assert.Equal("tablist", nav.GetAttribute("role"));
            Assert.Equal("0", nav.GetAttribute("tabindex"));
        }

        [Fact]
        public void Tabs_Wrapper_HasNoRole_ToAvoidTablistOwningPanels()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>();

            var wrapper = component.Find("div.rz-tabview");
            Assert.Null(wrapper.GetAttribute("role"));
            Assert.Null(wrapper.GetAttribute("tabindex"));
            Assert.Null(wrapper.GetAttribute("aria-activedescendant"));
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

            var wrapper = component.Find("ul.rz-tabview-nav");
            var active = wrapper.GetAttribute("aria-activedescendant");
            Assert.False(string.IsNullOrEmpty(active));

            var activeTab = component.Find($"[id='{active}']");
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
        public void Tabs_KeyDownInPanelContent_DoesNotReRenderContent()
        {
            using var ctx = new TestContext();

            var contentRenderCount = 0;
            RenderFragment content = b => { contentRenderCount++; b.AddContent(0, "Panel-Content"); };

            RenderFragment tabs = builder =>
            {
                builder.OpenComponent(0, typeof(RadzenTabsItem));
                builder.AddAttribute(1, "Text", "First");
                builder.AddAttribute(2, "ChildContent", content);
                builder.CloseComponent();
            };

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.Tabs, tabs)
            );

            Assert.Contains("Panel-Content", component.Markup);

            var renderCountBefore = contentRenderCount;

            var panel = component.Find("div.rz-tabview-panel");
            Assert.Throws<MissingEventHandlerException>(() => panel.KeyDown(new KeyboardEventArgs { Key = "a", Code = "KeyA" }));
            Assert.Throws<MissingEventHandlerException>(() => panel.KeyDown(new KeyboardEventArgs { Key = "b", Code = "KeyB" }));

            Assert.Equal(renderCountBefore, contentRenderCount);
        }

        [Fact]
        public void Tabs_KeyDownInPanelContent_DoesNotDispatchToTabs()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.Tabs, TabsFragmentWithContent(("First", "First-Content")))
            );

            var renderCountBefore = component.RenderCount;

            var panel = component.Find("div.rz-tabview-panel");
            Assert.Throws<MissingEventHandlerException>(() => panel.KeyDown(new KeyboardEventArgs { Key = "Escape", Code = "Escape" }));

            Assert.Equal(renderCountBefore, component.RenderCount);
        }

        [Fact]
        public void Tabs_ChangeHandlerThrows_StillRendersSelectedTab()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.Change, (int _) => throw new System.InvalidOperationException("boom"))
                .Add(p => p.Tabs, TabsFragmentWithContent(("First", "First-Content"), ("Second", "Second-Content")))
            );

            Assert.Contains("First-Content", component.Markup);

            var secondTab = component.FindAll("button[role='tab']")[1];
            var ex = Record.Exception(() => secondTab.Click());

            Assert.NotNull(ex);
            Assert.Contains("Second-Content", component.Markup);
            Assert.DoesNotContain("First-Content", component.Markup);
        }

        [Fact]
        public void Tabs_FocusAsyncThrowsJSException_DoesNotPropagate()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop
                .SetupVoid("Blazor._internal.domWrapper.focus", _ => true)
                .SetException(new JSException("Unable to focus an invalid element."));

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.Tabs, TabsFragmentWithContent(("First", "First-Content"), ("Second", "Second-Content")))
            );

            var secondTab = component.FindAll("button[role='tab']")[1];
            var ex = Record.Exception(() => secondTab.Click());

            Assert.Null(ex);
            Assert.Contains("Second-Content", component.Markup);
            Assert.DoesNotContain("First-Content", component.Markup);
        }

        [Fact]
        public void Tabs_ClientRender_FocusAsyncThrowsJSException_DoesNotPropagate()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop
                .SetupVoid("Blazor._internal.domWrapper.focus", _ => true)
                .SetException(new JSException("Unable to focus an invalid element."));

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.RenderMode, TabRenderMode.Client)
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.Tabs, TabsFragmentWithContent(("First", "First-Content"), ("Second", "Second-Content")))
            );

            var secondTab = component.FindAll("button[role='tab']")[1];
            var ex = Record.Exception(() => secondTab.Click());

            Assert.Null(ex);
        }

        [Fact]
        public void Tabs_NonNavigationKeyDown_DoesNotBlockSubsequentRenders()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.Tabs, TabsFragmentWithContent(("First", "First-Content"), ("Second", "Second-Content")))
            );

            Assert.Contains("First-Content", component.Markup);

            var wrapper = component.Find("ul.rz-tabview-nav");
            // Simulate non-navigation keystrokes (e.g. a barcode scanner) on the focused tablist.
            // Pre-fix: the second keypress latched shouldRender = false, blocking all later renders.
            wrapper.KeyDown(new KeyboardEventArgs { Key = "a", Code = "KeyA" });
            wrapper.KeyDown(new KeyboardEventArgs { Key = "b", Code = "KeyB" });

            component.SetParametersAndRender(parameters => parameters.Add(p => p.SelectedIndex, 1));

            Assert.Contains("Second-Content", component.Markup);
            Assert.DoesNotContain("First-Content", component.Markup);
        }

        [Theory]
        [InlineData(TabPosition.Top, "horizontal")]
        [InlineData(TabPosition.Bottom, "horizontal")]
        [InlineData(TabPosition.TopRight, "horizontal")]
        [InlineData(TabPosition.BottomRight, "horizontal")]
        [InlineData(TabPosition.Left, "vertical")]
        [InlineData(TabPosition.Right, "vertical")]
        public void Tabs_Wrapper_HasAriaOrientation_MatchingTabPosition(TabPosition position, string expected)
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.TabPosition, position)
                .Add(p => p.Tabs, TabsFragment("One", "Two", "Three"))
            );

            var wrapper = component.Find("ul.rz-tabview-nav");
            Assert.Equal(expected, wrapper.GetAttribute("aria-orientation"));
        }

        static string SelectedTabText(IRenderedComponent<RadzenTabs> component)
        {
            return component.Find("[role='tab'][aria-selected='true']").TextContent;
        }

        static string ActiveDescendantText(IRenderedComponent<RadzenTabs> component)
        {
            var wrapper = component.Find("ul.rz-tabview-nav");
            var active = wrapper.GetAttribute("aria-activedescendant");
            return component.Find($"[id='{active}']").TextContent;
        }

        [Fact]
        public void Tabs_VerticalOrientation_ArrowDown_MovesActiveDescendantToNextTab()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.TabPosition, TabPosition.Left)
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.Tabs, TabsFragment("One", "Two", "Three"))
            );

            var wrapper = component.Find("ul.rz-tabview-nav");
            wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowDown", Code = "ArrowDown" });

            Assert.Equal("Two", ActiveDescendantText(component));
        }

        [Fact]
        public void Tabs_VerticalOrientation_ArrowUp_MovesActiveDescendantToPreviousTab()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.TabPosition, TabPosition.Right)
                .Add(p => p.SelectedIndex, 2)
                .Add(p => p.Tabs, TabsFragment("One", "Two", "Three"))
            );

            var wrapper = component.Find("ul.rz-tabview-nav");
            wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowUp", Code = "ArrowUp" });

            Assert.Equal("Two", ActiveDescendantText(component));
        }

        [Fact]
        public void Tabs_VerticalOrientation_ArrowLeftRight_DoNotMoveActiveDescendant()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.TabPosition, TabPosition.Left)
                .Add(p => p.SelectedIndex, 1)
                .Add(p => p.Tabs, TabsFragment("One", "Two", "Three"))
            );

            var wrapper = component.Find("ul.rz-tabview-nav");
            wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowRight", Code = "ArrowRight" });
            Assert.Equal("Two", ActiveDescendantText(component));

            wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", Code = "ArrowLeft" });
            Assert.Equal("Two", ActiveDescendantText(component));
        }

        [Fact]
        public void Tabs_HorizontalOrientation_ArrowRight_MovesActiveDescendantToNextTab()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.TabPosition, TabPosition.Top)
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.Tabs, TabsFragment("One", "Two", "Three"))
            );

            var wrapper = component.Find("ul.rz-tabview-nav");
            wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowRight", Code = "ArrowRight" });

            Assert.Equal("Two", ActiveDescendantText(component));
        }

        [Fact]
        public void Tabs_HorizontalOrientation_ArrowUpDown_DoNotMoveActiveDescendant()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.TabPosition, TabPosition.Top)
                .Add(p => p.SelectedIndex, 1)
                .Add(p => p.Tabs, TabsFragment("One", "Two", "Three"))
            );

            var wrapper = component.Find("ul.rz-tabview-nav");
            wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowDown", Code = "ArrowDown" });
            Assert.Equal("Two", ActiveDescendantText(component));

            wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowUp", Code = "ArrowUp" });
            Assert.Equal("Two", ActiveDescendantText(component));
        }

        [Fact]
        public void Tabs_VerticalOrientation_HomeAndEnd_MoveActiveDescendantToEnds()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.TabPosition, TabPosition.Left)
                .Add(p => p.SelectedIndex, 1)
                .Add(p => p.Tabs, TabsFragment("One", "Two", "Three"))
            );

            var wrapper = component.Find("ul.rz-tabview-nav");

            wrapper.KeyDown(new KeyboardEventArgs { Key = "End", Code = "End" });
            Assert.Equal("Three", ActiveDescendantText(component));

            wrapper.KeyDown(new KeyboardEventArgs { Key = "Home", Code = "Home" });
            Assert.Equal("One", ActiveDescendantText(component));
        }

        [Fact]
        public void Tabs_VerticalOrientation_EnterActivatesFocusedTab()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.TabPosition, TabPosition.Left)
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.Tabs, TabsFragmentWithContent(("One", "One-Content"), ("Two", "Two-Content"), ("Three", "Three-Content")))
            );

            var wrapper = component.Find("ul.rz-tabview-nav");
            wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowDown", Code = "ArrowDown" });
            wrapper.KeyDown(new KeyboardEventArgs { Key = "Enter", Code = "Enter" });

            Assert.Equal("Two", SelectedTabText(component));
            Assert.Contains("Two-Content", component.Markup);
        }

        [Fact]
        public void Tabs_AriaLabel_AppliedToTabList()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.AriaLabel, "Sections")
                .Add(p => p.Tabs, TabsFragment("One", "Two"))
            );

            var wrapper = component.Find("ul.rz-tabview-nav");
            Assert.Equal("Sections", wrapper.GetAttribute("aria-label"));
        }

        [Fact]
        public void Tabs_AriaLabelledBy_AppliedToTabList()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.AriaLabelledBy, "heading-id")
                .Add(p => p.Tabs, TabsFragment("One", "Two"))
            );

            var wrapper = component.Find("ul.rz-tabview-nav");
            Assert.Equal("heading-id", wrapper.GetAttribute("aria-labelledby"));
        }

        [Fact]
        public void Tabs_SelectedPanel_HasNoTabIndex()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.Tabs, TabsFragmentWithContent(("First", "First-Content"), ("Second", "Second-Content")))
            );

            var panel = component.Find("div.rz-tabview-panel");
            Assert.Null(panel.GetAttribute("tabindex"));
        }

        [Fact]
        public void Tabs_HorizontalOrientation_ArrowRight_WrapsFromLastToFirst()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.TabPosition, TabPosition.Top)
                .Add(p => p.SelectedIndex, 2)
                .Add(p => p.Tabs, TabsFragment("One", "Two", "Three"))
            );

            var wrapper = component.Find("ul.rz-tabview-nav");
            wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowRight", Code = "ArrowRight" });

            Assert.Equal("One", ActiveDescendantText(component));
        }

        [Fact]
        public void Tabs_HorizontalOrientation_ArrowLeft_WrapsFromFirstToLast()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.TabPosition, TabPosition.Top)
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.Tabs, TabsFragment("One", "Two", "Three"))
            );

            var wrapper = component.Find("ul.rz-tabview-nav");
            wrapper.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft", Code = "ArrowLeft" });

            Assert.Equal("Three", ActiveDescendantText(component));
        }

        [Fact]
        public void Tabs_CanChangePreventDefault_KeepsCurrentTab_AndDoesNotRaiseChange()
        {
            using var ctx = new TestContext();

            var changeRaised = false;
            TabsCanChangeEventArgs canChangeArgs = null;

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.CanChange, (TabsCanChangeEventArgs args) => { canChangeArgs = args; args.PreventDefault(); })
                .Add(p => p.Change, (int _) => changeRaised = true)
                .Add(p => p.Tabs, TabsFragmentWithContent(("First", "First-Content"), ("Second", "Second-Content")))
            );

            var secondTab = component.FindAll("button[role='tab']")[1];
            secondTab.Click();

            Assert.NotNull(canChangeArgs);
            Assert.Equal(0, canChangeArgs.SelectedIndex);
            Assert.Equal(1, canChangeArgs.NewIndex);
            Assert.False(changeRaised);
            Assert.Contains("First-Content", component.Markup);
            Assert.DoesNotContain("Second-Content", component.Markup);
        }

        [Fact]
        public void Tabs_CanChangeWithoutPreventDefault_ChangesTab()
        {
            using var ctx = new TestContext();

            var changeRaised = false;

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.CanChange, (TabsCanChangeEventArgs _) => { })
                .Add(p => p.Change, (int _) => changeRaised = true)
                .Add(p => p.Tabs, TabsFragmentWithContent(("First", "First-Content"), ("Second", "Second-Content")))
            );

            var secondTab = component.FindAll("button[role='tab']")[1];
            secondTab.Click();

            Assert.True(changeRaised);
            Assert.Contains("Second-Content", component.Markup);
            Assert.DoesNotContain("First-Content", component.Markup);
        }

        [Fact]
        public void Tabs_ClientRender_CanChangePreventDefault_KeepsCurrentTab()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var changeRaised = false;

            var component = ctx.RenderComponent<RadzenTabs>(parameters => parameters
                .Add(p => p.RenderMode, TabRenderMode.Client)
                .Add(p => p.SelectedIndex, 0)
                .Add(p => p.CanChange, (TabsCanChangeEventArgs args) => args.PreventDefault())
                .Add(p => p.Change, (int _) => changeRaised = true)
                .Add(p => p.Tabs, TabsFragmentWithContent(("First", "First-Content"), ("Second", "Second-Content")))
            );

            var secondTab = component.FindAll("button[role='tab']")[1];
            secondTab.Click();

            Assert.False(changeRaised);
            var selected = component.Find("li.rz-tabview-selected");
            Assert.Contains("First", selected.TextContent);
        }
    }
}

