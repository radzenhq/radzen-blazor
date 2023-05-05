using Bunit;
using Bunit.JSInterop;
using System;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class CheckBoxTests
    {
        [Fact]
        public void CheckBox_Renders_CssClasses()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenCheckBox<bool>>();

            component.Render();

            Assert.Contains(@$"rz-chkbox", component.Markup);
            Assert.Contains(@$"rz-chkbox-box", component.Markup);
        }

        [Fact]
        public void CheckBox_Renders_ValueParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenCheckBox<bool?>>();

            var value = true;

            component.SetParametersAndRender(parameters => parameters.Add<bool?>(p => p.Value, value));

            Assert.Contains(@$"rz-state-active", component.Markup);
            Assert.Contains(@$"rzi-check", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add<bool?>(p => p.Value, !value));

            Assert.DoesNotContain(@$"rz-state-active", component.Markup);
            Assert.DoesNotContain(@$"rzi-check", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add<bool?>(p => p.Value, null));

            Assert.Contains(@$"rz-state-active", component.Markup);
            Assert.Contains(@$"rzi-times", component.Markup);
        }

        [Fact]
        public void CheckBox_Renders_StyleParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenCheckBox<bool>>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void CheckBox_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenCheckBox<bool>>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }


        [Fact]
        public void CheckBox_Renders_NameParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenCheckBox<bool>>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Name, value));

            Assert.Contains(@$"name=""{value}""", component.Markup);
        }

        [Fact]
        public void CheckBox_Renders_DisabledParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenCheckBox<bool>>();

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.Disabled, true));

            Assert.Contains(@$"disabled", component.Markup);
            Assert.Contains(@$"rz-state-disabled", component.Markup);
        }

        [Fact]
        public void CheckBox_Raises_ChangedEvent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenCheckBox<bool>>();

            var raised = false;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Change, args => { raised = true; }));

            component.Find("div.rz-chkbox-box").Click();

            Assert.True(raised);
        }

        [Fact]
        public void CheckBox_Raises_ValueChangedEvent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenCheckBox<bool>>();

            var raised = false;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ValueChanged, args => { raised = true; }));

            component.Find("div.rz-chkbox-box").Click();

            Assert.True(raised);
        }

        [Fact]
        public void CheckBox_Renders_TriStateParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenCheckBox<bool?>>();

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.TriState, true));


            component.Find("div.rz-chkbox-box").Click();

            component.Render();

            Assert.Contains(@$"rz-state-active", component.Markup);
            Assert.Contains(@$"rzi-check", component.Markup);

            component.Find("div.rz-chkbox-box").Click();

            component.Render();

            Assert.DoesNotContain(@$"rz-state-active", component.Markup);
            Assert.DoesNotContain(@$"rzi-check", component.Markup);

            component.Find("div.rz-chkbox-box").Click();

            Assert.Contains(@$"rz-state-active", component.Markup);
            Assert.Contains(@$"rzi-times", component.Markup);
        }

        [Fact]
        public void CheckBox_Renders_ReadonlyParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenCheckBox<bool>>();

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.ReadOnly, true));

            Assert.Contains(@$"readonly", component.Markup);
        }

        [Fact]
        public void CheckBox_DoesNotRaise_ChangedEvent_ReadonlyParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenCheckBox<bool>>();

            var raised = false;

            component.SetParametersAndRender(parameters => parameters
              .Add<bool>(p => p.ReadOnly, true)
              .Add(p => p.Change, args => { raised = true; })
            );

            component.Find("div.rz-chkbox-box").Click();

            Assert.False(raised);
        }

        [Fact]
        public void CheckBox_DoesNotRaise_ValueChangedEvent_ReadonlyParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenCheckBox<bool>>();

            var raised = false;

            component.SetParametersAndRender(parameters => parameters
              .Add<bool>(p => p.ReadOnly, true)
              .Add(p => p.ValueChanged, args => { raised = true; })
            );

            component.Find("div.rz-chkbox-box").Click();

            Assert.False(raised);
        }

        [Fact]
        public void CheckBox_ValueNotChanged_ReadonlyParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenCheckBox<bool>>();

            var value = true;

            component.SetParametersAndRender(parameters => parameters
              .Add<bool>(p => p.ReadOnly, true)
              .Add<bool>(p => p.Value, value)
            );

            component.Find("div.rz-chkbox-box").Click();

            Assert.Contains(@$"rz-state-active", component.Markup);

            component.SetParametersAndRender(parameters => parameters
              .Add<bool>(p => p.ReadOnly, !true)
              .Add<bool>(p => p.Value, value)
            );

            component.Find("div.rz-chkbox-box").Click();

            Assert.DoesNotContain(@$"rz-state-active", component.Markup);
        }
    }
}
