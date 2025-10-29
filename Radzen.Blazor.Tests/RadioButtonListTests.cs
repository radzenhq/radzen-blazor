using Bunit;
using System.Collections.Generic;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class RadioButtonListTests
    {
        [Fact]
        public void RadioButtonList_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>();

            Assert.Contains(@"rz-radio-button-list", component.Markup);
        }

        [Fact]
        public void RadioButtonList_Renders_Orientation()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>(parameters =>
            {
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenRadioButtonListItem<int>>(0);
                    builder.AddAttribute(1, "Text", "Option 1");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();
                });
            });

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Orientation, Orientation.Horizontal));
            // Orientation is applied via RadzenStack which uses flex-direction
            Assert.Contains("rz-flex-row", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Orientation, Orientation.Vertical));
            Assert.Contains("rz-flex-column", component.Markup);
        }

        [Fact]
        public void RadioButtonList_Renders_Disabled()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenRadioButtonList<int>>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenRadioButtonListItem<int>>(0);
                    builder.AddAttribute(1, "Text", "Option 1");
                    builder.AddAttribute(2, "Value", 1);
                    builder.CloseComponent();
                });
            });

            // Disabled class is on the radio button items
            Assert.Contains("rz-state-disabled", component.Markup);
        }
    }
}

