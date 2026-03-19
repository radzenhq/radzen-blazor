using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SplitButtonItemTests
    {
        [Fact]
        public void SplitButtonItem_Renders_TextParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitButton>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenSplitButtonItem>(0);
                    builder.AddAttribute(1, nameof(RadzenSplitButtonItem.Text), "Option A");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("Option A", component.Markup);
        }

        [Fact]
        public void SplitButtonItem_Renders_IconParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitButton>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenSplitButtonItem>(0);
                    builder.AddAttribute(1, nameof(RadzenSplitButtonItem.Text), "Save");
                    builder.AddAttribute(2, nameof(RadzenSplitButtonItem.Icon), "save");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("save", component.Markup);
        }

        [Fact]
        public void SplitButtonItem_Renders_Disabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitButton>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenSplitButtonItem>(0);
                    builder.AddAttribute(1, nameof(RadzenSplitButtonItem.Text), "Disabled Item");
                    builder.AddAttribute(2, nameof(RadzenSplitButtonItem.Disabled), true);
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-state-disabled", component.Markup);
        }

        [Fact]
        public void SplitButtonItem_Renders_MultipleItems()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitButton>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenSplitButtonItem>(0);
                    builder.AddAttribute(1, nameof(RadzenSplitButtonItem.Text), "First");
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenSplitButtonItem>(2);
                    builder.AddAttribute(3, nameof(RadzenSplitButtonItem.Text), "Second");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("First", component.Markup);
            Assert.Contains("Second", component.Markup);
        }

        [Fact]
        public void SplitButtonItem_Renders_MenuItemClass()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitButton>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenSplitButtonItem>(0);
                    builder.AddAttribute(1, nameof(RadzenSplitButtonItem.Text), "Item");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-menuitem", component.Markup);
        }

        [Fact]
        public void SplitButtonItem_HasValue()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSplitButton>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenSplitButtonItem>(0);
                    builder.AddAttribute(1, nameof(RadzenSplitButtonItem.Text), "Export");
                    builder.AddAttribute(2, nameof(RadzenSplitButtonItem.Value), "export-csv");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("Export", component.Markup);
        }
    }
}
