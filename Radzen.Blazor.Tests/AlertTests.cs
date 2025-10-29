using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class AlertTests
    {
        [Fact]
        public void Alert_Renders_CssClasses()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenAlert>();

            Assert.Contains(@"rz-alert", component.Markup);
        }

        [Fact]
        public void Alert_Renders_AlertStyle()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenAlert>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.AlertStyle, AlertStyle.Danger));
            Assert.Contains("rz-danger", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.AlertStyle, AlertStyle.Success));
            Assert.Contains("rz-success", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.AlertStyle, AlertStyle.Warning));
            Assert.Contains("rz-warning", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.AlertStyle, AlertStyle.Info));
            Assert.Contains("rz-info", component.Markup);
        }

        [Fact]
        public void Alert_Renders_Shade()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenAlert>();

            component.SetParametersAndRender(parameters => parameters
                .Add(p => p.AlertStyle, AlertStyle.Primary)
                .Add(p => p.Shade, Shade.Lighter));

            Assert.Contains("rz-shade-lighter", component.Markup);

            component.SetParametersAndRender(parameters => parameters
                .Add(p => p.AlertStyle, AlertStyle.Primary)
                .Add(p => p.Shade, Shade.Darker));

            Assert.Contains("rz-shade-darker", component.Markup);
        }

        [Fact]
        public void Alert_Renders_Variant()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenAlert>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Variant, Variant.Outlined));
            Assert.Contains("rz-variant-outlined", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Variant, Variant.Flat));
            Assert.Contains("rz-variant-flat", component.Markup);
        }

        [Fact]
        public void Alert_Renders_Title()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenAlert>();

            var title = "Alert Title";
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Title, title));

            Assert.Contains(title, component.Markup);
            Assert.Contains("rz-alert-title", component.Markup);
        }

        [Fact]
        public void Alert_Renders_Text()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenAlert>();

            var text = "This is an alert message";
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Text, text));

            Assert.Contains(text, component.Markup);
        }

        [Fact]
        public void Alert_Renders_ChildContent()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenAlert>(parameters =>
            {
                parameters.AddChildContent("Custom alert content");
            });

            Assert.Contains("Custom alert content", component.Markup);
        }

        [Fact]
        public void Alert_ShowIcon_DisplaysIcon()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenAlert>();

            // Default should show icon
            Assert.Contains("rz-alert-icon", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ShowIcon, false));
            Assert.DoesNotContain("rz-alert-icon", component.Markup);
        }

        [Fact]
        public void Alert_AllowClose_DisplaysCloseButton()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenAlert>();

            // Default AllowClose is true - should contain a button with close icon
            component.SetParametersAndRender(parameters => parameters.Add(p => p.AllowClose, true));
            Assert.Contains("close", component.Markup);
            Assert.Contains("rz-button", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.AllowClose, false));
            // When AllowClose is false, should not have close button
            var buttonCount = System.Text.RegularExpressions.Regex.Matches(component.Markup, "rz-button").Count;
            Assert.Equal(0, buttonCount);
        }

        [Fact]
        public void Alert_CloseButton_RaisesCloseEvent()
        {
            using var ctx = new TestContext();
            
            var closeRaised = false;

            var component = ctx.RenderComponent<RadzenAlert>(parameters =>
            {
                parameters.Add(p => p.AllowClose, true);
                parameters.Add(p => p.Close, () => closeRaised = true);
            });

            var closeButton = component.Find("button.rz-button");
            closeButton.Click();

            Assert.True(closeRaised);
        }

        [Fact]
        public void Alert_Visible_ControlsDisplay()
        {
            using var ctx = new TestContext();
            
            var component = ctx.RenderComponent<RadzenAlert>(parameters =>
            {
                parameters.Add(p => p.Visible, true);
                parameters.Add(p => p.Text, "Visible Alert");
            });

            Assert.Contains("Visible Alert", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, false));

            // When not visible, component should not render
            Assert.DoesNotContain("Visible Alert", component.Markup);
        }

        [Fact]
        public void Alert_CloseButton_SetsVisibleToFalse()
        {
            using var ctx = new TestContext();
            
            var visibleValue = true;

            var component = ctx.RenderComponent<RadzenAlert>(parameters =>
            {
                parameters.Add(p => p.Visible, visibleValue);
                parameters.Add(p => p.AllowClose, true);
                parameters.Add(p => p.VisibleChanged, (bool value) => visibleValue = value);
            });

            var closeButton = component.Find("button.rz-button");
            closeButton.Click();

            Assert.False(visibleValue);
        }
    }
}


