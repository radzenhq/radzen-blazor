using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SelectBarTests
    {
        [Fact]
        public void SelectBar_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSelectBar<int>>();

            Assert.Contains(@"rz-selectbar", component.Markup);
        }

        [Fact]
        public void SelectBar_Renders_Orientation()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSelectBar<int>>(parameters =>
            {
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenSelectBarItem>(0);
                    builder.AddAttribute(1, "Text", "Option 1");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();
                });
            });

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Orientation, Orientation.Horizontal));
            Assert.Contains("rz-selectbar-horizontal", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Orientation, Orientation.Vertical));
            Assert.Contains("rz-selectbar-vertical", component.Markup);
        }

        [Fact]
        public void SelectBar_Renders_Multiple()
        {
            using var ctx = new TestContext();
            // When Multiple is true, TValue should be IEnumerable<T>
            var component = ctx.RenderComponent<RadzenSelectBar<System.Collections.Generic.IEnumerable<int>>>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenSelectBarItem>(0);
                    builder.AddAttribute(1, "Text", "Option 1");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();
                });
            });

            Assert.NotNull(component.Instance);
            Assert.True(component.Instance.Multiple);
        }

        [Fact]
        public void SelectBar_Renders_Size()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSelectBar<int>>(parameters =>
            {
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenSelectBarItem>(0);
                    builder.AddAttribute(1, "Text", "Option 1");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();
                });
            });

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Size, ButtonSize.Small));
            Assert.Contains("rz-button-sm", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Size, ButtonSize.Large));
            Assert.Contains("rz-button-lg", component.Markup);
        }

        [Fact]
        public void SelectBar_Renders_Disabled()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSelectBar<int>>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenSelectBarItem>(0);
                    builder.AddAttribute(1, "Text", "Option 1");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-state-disabled", component.Markup);
        }

        [Fact]
        public void SelectBar_NotVisible_DoesNotRender()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSelectBar<int>>(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.DoesNotContain("rz-selectbar", component.Markup);
        }

        [Fact]
        public void SelectBar_Renders_MultipleItems()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSelectBar<int>>(parameters =>
            {
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenSelectBarItem>(0);
                    builder.AddAttribute(1, "Text", "First");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenSelectBarItem>(3);
                    builder.AddAttribute(4, "Text", "Second");
                    builder.AddAttribute(5, "Value", 2);
                    builder.CloseComponent();
                });
            });

            Assert.Contains("First", component.Markup);
            Assert.Contains("Second", component.Markup);
        }

        [Fact]
        public void SelectBar_SingleSelect_Renders_RadiogroupRole()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSelectBar<int>>(parameters =>
            {
                parameters.Add(p => p.Value, 1);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenSelectBarItem>(0);
                    builder.AddAttribute(1, "Text", "First");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenSelectBarItem>(3);
                    builder.AddAttribute(4, "Text", "Second");
                    builder.AddAttribute(5, "Value", 2);
                    builder.CloseComponent();
                });
            });

            Assert.Contains("role=\"radiogroup\"", component.Markup);
            Assert.Contains("role=\"radio\"", component.Markup);
            Assert.Contains("aria-checked=\"true\"", component.Markup);
            Assert.Contains("aria-checked=\"false\"", component.Markup);
            Assert.DoesNotContain("aria-pressed", component.Markup);
            Assert.DoesNotContain("role=\"toolbar\"", component.Markup);
        }

        [Fact]
        public void SelectBar_Multiple_Renders_ToolbarRole_WithAriaPressed()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSelectBar<System.Collections.Generic.IEnumerable<int>>>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Value, new System.Collections.Generic.List<int> { 1 });
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenSelectBarItem>(0);
                    builder.AddAttribute(1, "Text", "First");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenSelectBarItem>(3);
                    builder.AddAttribute(4, "Text", "Second");
                    builder.AddAttribute(5, "Value", 2);
                    builder.CloseComponent();
                });
            });

            Assert.Contains("role=\"toolbar\"", component.Markup);
            Assert.Contains("aria-pressed=\"true\"", component.Markup);
            Assert.Contains("aria-pressed=\"false\"", component.Markup);
            Assert.DoesNotContain("aria-checked", component.Markup);
            Assert.DoesNotContain("role=\"radio\"", component.Markup);
            Assert.DoesNotContain("role=\"radiogroup\"", component.Markup);
        }

        [Fact]
        public void SelectBar_Renders_AriaLabelledBy()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenSelectBar<int>>(parameters =>
            {
                parameters.Add(p => p.AriaLabelledBy, "label-id");
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenSelectBarItem>(0);
                    builder.AddAttribute(1, "Text", "Option 1");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();
                });
            });

            Assert.Contains("aria-labelledby=\"label-id\"", component.Markup);
        }
    }
}

