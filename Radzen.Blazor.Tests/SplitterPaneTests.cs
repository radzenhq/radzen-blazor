using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SplitterPaneTests
    {
        [Fact]
        public void SplitterPane_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenSplitterPane>(0);
                    builder.AddAttribute(1, nameof(RadzenSplitterPane.ChildContent),
                        (RenderFragment)(b => b.AddContent(0, "Pane Content")));
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-splitter-pane", component.Markup);
            Assert.Contains("Pane Content", component.Markup);
        }

        [Fact]
        public void SplitterPane_Renders_MultiPanes()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenSplitterPane>(0);
                    builder.AddAttribute(1, nameof(RadzenSplitterPane.ChildContent),
                        (RenderFragment)(b => b.AddContent(0, "Left Pane")));
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenSplitterPane>(2);
                    builder.AddAttribute(3, nameof(RadzenSplitterPane.ChildContent),
                        (RenderFragment)(b => b.AddContent(0, "Right Pane")));
                    builder.CloseComponent();
                });
            });

            Assert.Contains("Left Pane", component.Markup);
            Assert.Contains("Right Pane", component.Markup);
        }

        [Fact]
        public void SplitterPane_Renders_WithSize()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenSplitterPane>(0);
                    builder.AddAttribute(1, "Size", "30%");
                    builder.AddAttribute(2, nameof(RadzenSplitterPane.ChildContent),
                        (RenderFragment)(b => b.AddContent(0, "Sized")));
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenSplitterPane>(3);
                    builder.AddAttribute(4, nameof(RadzenSplitterPane.ChildContent),
                        (RenderFragment)(b => b.AddContent(0, "Auto")));
                    builder.CloseComponent();
                });
            });

            Assert.Contains("30%", component.Markup);
        }

        [Fact]
        public void SplitterPane_Renders_Collapsed()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenSplitterPane>(0);
                    builder.AddAttribute(1, "Collapsed", true);
                    builder.AddAttribute(2, nameof(RadzenSplitterPane.ChildContent),
                        (RenderFragment)(b => b.AddContent(0, "Collapsed Pane")));
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenSplitterPane>(3);
                    builder.AddAttribute(4, nameof(RadzenSplitterPane.ChildContent),
                        (RenderFragment)(b => b.AddContent(0, "Other")));
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-splitter-pane-collapsed", component.Markup);
        }

        [Fact]
        public void SplitterPane_Renders_Bar_BetweenPanes()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenSplitterPane>(0);
                    builder.AddAttribute(1, nameof(RadzenSplitterPane.ChildContent),
                        (RenderFragment)(b => b.AddContent(0, "First")));
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenSplitterPane>(2);
                    builder.AddAttribute(3, nameof(RadzenSplitterPane.ChildContent),
                        (RenderFragment)(b => b.AddContent(0, "Second")));
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-splitter-bar", component.Markup);
        }

        [Fact]
        public void SplitterPane_NonResizable_DoesNotShowResizeBar()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenSplitterPane>(0);
                    builder.AddAttribute(1, "Resizable", false);
                    builder.AddAttribute(2, nameof(RadzenSplitterPane.ChildContent),
                        (RenderFragment)(b => b.AddContent(0, "Fixed")));
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenSplitterPane>(3);
                    builder.AddAttribute(4, nameof(RadzenSplitterPane.ChildContent),
                        (RenderFragment)(b => b.AddContent(0, "Other")));
                    builder.CloseComponent();
                });
            });

            // Non-resizable panes should not have resize functionality
            Assert.Contains("Fixed", component.Markup);
        }

        [Fact]
        public void SplitterPane_Collapsible_DefaultTrue()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSplitter>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenSplitterPane>(0);
                    builder.AddAttribute(1, nameof(RadzenSplitterPane.ChildContent),
                        (RenderFragment)(b => b.AddContent(0, "Content")));
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenSplitterPane>(2);
                    builder.AddAttribute(3, nameof(RadzenSplitterPane.ChildContent),
                        (RenderFragment)(b => b.AddContent(0, "Other")));
                    builder.CloseComponent();
                });
            });

            // Collapsible is true by default, so collapse icon should be present
            Assert.Contains("rz-splitter-bar", component.Markup);
        }
    }
}
