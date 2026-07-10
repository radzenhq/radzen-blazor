using Bunit;
using Radzen.Blazor.Rendering;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SplitterTests
    {
        static IRenderedComponent<RadzenSplitter> RenderSplitter(TestContext ctx, Orientation orientation = Orientation.Horizontal)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            return ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.Orientation, orientation);
                parameters.AddChildContent<RadzenSplitterPane>(pane =>
                {
                    pane.Add(p => p.Size, "30%");
                    pane.Add(p => p.Min, "10%");
                    pane.Add(p => p.Max, "80%");
                });
                parameters.AddChildContent<RadzenSplitterPane>();
            });
        }

        [Fact]
        public void Splitter_Separator_HasRoleSeparator()
        {
            using var ctx = new TestContext();
            var component = RenderSplitter(ctx);

            var separator = component.Find("span.rz-resize");

            Assert.Equal("separator", separator.GetAttribute("role"));
        }

        [Fact]
        public void Splitter_Separator_HasAriaValues()
        {
            using var ctx = new TestContext();
            var component = RenderSplitter(ctx);

            var separator = component.Find("span.rz-resize");

            Assert.Equal("30", separator.GetAttribute("aria-valuenow"));
            Assert.Equal("10", separator.GetAttribute("aria-valuemin"));
            Assert.Equal("80", separator.GetAttribute("aria-valuemax"));
        }

        [Fact]
        public void Splitter_Separator_DefaultAriaValues()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.AddChildContent<RadzenSplitterPane>(pane => pane.Add(p => p.Size, "40%"));
                parameters.AddChildContent<RadzenSplitterPane>();
            });

            var separator = component.Find("span.rz-resize");

            Assert.Equal("0", separator.GetAttribute("aria-valuemin"));
            Assert.Equal("100", separator.GetAttribute("aria-valuemax"));
            Assert.Equal("40", separator.GetAttribute("aria-valuenow"));
        }

        [Fact]
        public void Splitter_Separator_HasAriaControls()
        {
            using var ctx = new TestContext();
            var component = RenderSplitter(ctx);

            var separator = component.Find("span.rz-resize");
            var controls = separator.GetAttribute("aria-controls");

            Assert.False(string.IsNullOrEmpty(controls));
            component.Find($"[id=\"{controls}\"]");
        }

        [Fact]
        public void Splitter_Separator_HasAriaLabel()
        {
            using var ctx = new TestContext();
            var component = RenderSplitter(ctx);

            var separator = component.Find("span.rz-resize");

            Assert.False(string.IsNullOrEmpty(separator.GetAttribute("aria-label")));
        }

        [Fact]
        public void Splitter_Separator_IsFocusable()
        {
            using var ctx = new TestContext();
            var component = RenderSplitter(ctx);

            var separator = component.Find("span.rz-resize");

            Assert.Equal("0", separator.GetAttribute("tabindex"));
        }

        [Fact]
        public void Splitter_Horizontal_Separator_AriaOrientationIsVertical()
        {
            using var ctx = new TestContext();
            var component = RenderSplitter(ctx, Orientation.Horizontal);

            var separator = component.Find("span.rz-resize");

            Assert.Equal("vertical", separator.GetAttribute("aria-orientation"));
        }

        [Fact]
        public void Splitter_Vertical_Separator_AriaOrientationIsHorizontal()
        {
            using var ctx = new TestContext();
            var component = RenderSplitter(ctx, Orientation.Vertical);

            var separator = component.Find("span.rz-resize");

            Assert.Equal("horizontal", separator.GetAttribute("aria-orientation"));
        }

        [Fact]
        public void Splitter_Separator_ArrowKey_StartsResize()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.Setup<Rect>("Radzen.clientRect", _ => true).SetResult(new Rect());

            var component = RenderSplitter(ctx);

            var separator = component.Find("span.rz-resize");
            separator.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowRight" });

            ctx.JSInterop.VerifyInvoke("Radzen.startSplitterResize");
            ctx.JSInterop.VerifyInvoke("Radzen.resizeSplitter");
        }

        [Fact]
        public void Splitter_Separator_HomeKey_StartsResize()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.Setup<Rect>("Radzen.clientRect", _ => true).SetResult(new Rect());

            var component = RenderSplitter(ctx);

            var separator = component.Find("span.rz-resize");
            separator.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "Home" });

            ctx.JSInterop.VerifyInvoke("Radzen.resizeSplitter");
        }

        [Fact]
        public void Splitter_Separator_EnterKey_CollapsesPane()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            RadzenSplitterEventArgs collapsed = null;

            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.Collapse, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<RadzenSplitterEventArgs>(this, args => collapsed = args));
                parameters.AddChildContent<RadzenSplitterPane>(pane =>
                {
                    pane.Add(p => p.Size, "30%");
                    pane.Add(p => p.Collapsible, true);
                });
                parameters.AddChildContent<RadzenSplitterPane>();
            });

            var separator = component.Find("span.rz-resize");
            separator.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "Enter" });

            Assert.NotNull(collapsed);
            Assert.Equal(0, collapsed.PaneIndex);
        }

        [Fact]
        public void Splitter_CollapseButton_HasRoleAndLabel()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.AddChildContent<RadzenSplitterPane>(pane =>
                {
                    pane.Add(p => p.Size, "30%");
                    pane.Add(p => p.Collapsible, true);
                });
                parameters.AddChildContent<RadzenSplitterPane>();
            });

            var collapse = component.Find("span.rz-collapse");

            Assert.Equal("button", collapse.GetAttribute("role"));
            Assert.False(string.IsNullOrEmpty(collapse.GetAttribute("aria-label")));
            Assert.Equal("true", collapse.GetAttribute("aria-expanded"));
        }

        [Fact]
        public void Splitter_ExpandButton_HasRoleAndLabel()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.AddChildContent<RadzenSplitterPane>(pane =>
                {
                    pane.Add(p => p.Size, "30%");
                    pane.Add(p => p.Collapsible, true);
                    pane.Add(p => p.Collapsed, true);
                });
                parameters.AddChildContent<RadzenSplitterPane>();
            });

            var expand = component.Find("span.rz-expand");

            Assert.Equal("button", expand.GetAttribute("role"));
            Assert.False(string.IsNullOrEmpty(expand.GetAttribute("aria-label")));
            Assert.Equal("false", expand.GetAttribute("aria-expanded"));
        }

        [Fact]
        public void Splitter_Separator_AriaLabelledBy_TakesPrecedence()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.AddChildContent<RadzenSplitterPane>(pane =>
                {
                    pane.Add(p => p.Size, "30%");
                    pane.Add(p => p.AriaLabelledBy, "pane-label");
                });
                parameters.AddChildContent<RadzenSplitterPane>();
            });

            var separator = component.Find("span.rz-resize");

            Assert.Equal("pane-label", separator.GetAttribute("aria-labelledby"));
            Assert.True(string.IsNullOrEmpty(separator.GetAttribute("aria-label")));
        }

        [Fact]
        public void Splitter_Bar_PointerDown_StartsResize()
        {
            using var ctx = new TestContext();
            var component = RenderSplitter(ctx);

            var bar = component.Find("div.rz-splitter-bar");
            bar.PointerDown(new Microsoft.AspNetCore.Components.Web.PointerEventArgs());

            ctx.JSInterop.VerifyInvoke("Radzen.startSplitterResize");
        }

        [Fact]
        public void Splitter_CollapseButton_PointerDown_DoesNotStartResize()
        {
            using var ctx = new TestContext();
            var component = RenderSplitter(ctx);

            var collapse = component.Find("span.rz-collapse");

            Assert.Throws<MissingEventHandlerException>(() => collapse.PointerDown(new Microsoft.AspNetCore.Components.Web.PointerEventArgs()));
            Assert.DoesNotContain(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.startSplitterResize");
        }

        [Fact]
        public void Splitter_ExpandButton_PointerDown_DoesNotStartResize()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.AddChildContent<RadzenSplitterPane>(pane =>
                {
                    pane.Add(p => p.Size, "30%");
                    pane.Add(p => p.Collapsible, true);
                    pane.Add(p => p.Collapsed, true);
                });
                parameters.AddChildContent<RadzenSplitterPane>();
            });

            var expand = component.Find("span.rz-expand");

            Assert.Throws<MissingEventHandlerException>(() => expand.PointerDown(new Microsoft.AspNetCore.Components.Web.PointerEventArgs()));
            Assert.DoesNotContain(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.startSplitterResize");
        }

        [Fact]
        public void Splitter_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>();

            Assert.Contains(@"rz-splitter", component.Markup);
        }

        [Fact]
        public void Splitter_Renders_Orientation_Horizontal()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.Orientation, Orientation.Horizontal);
            });

            Assert.Contains("rz-splitter-horizontal", component.Markup);
        }

        [Fact]
        public void Splitter_Renders_Orientation_Vertical()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.Orientation, Orientation.Vertical);
            });

            Assert.Contains("rz-splitter-vertical", component.Markup);
        }

        [Fact]
        public void Splitter_DefaultOrientation_IsHorizontal()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>();

            Assert.Contains("rz-splitter-horizontal", component.Markup);
        }

        [Fact]
        public void Splitter_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.Style, "height:400px");
            });

            Assert.Contains("height:400px", component.Markup);
        }

        [Fact]
        public void Splitter_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-splitter", component.Markup);
        }

        [Fact]
        public void Splitter_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.AddChildContent("<div>Pane Content</div>");
            });

            Assert.Contains("Pane Content", component.Markup);
        }

        [Fact]
        public void Splitter_Renders_UnmatchedAttributes()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.AddUnmatched("data-testid", "my-splitter");
            });

            Assert.Contains(@"data-testid=""my-splitter""", component.Markup);
        }
    }
}

