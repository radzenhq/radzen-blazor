using Bunit;
using Bunit.JSInterop;
using System;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class CheckBoxTests
    {
        [Fact]
        public void CheckBox_HiddenInput_IsInert()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenCheckBox<bool>>(parameters => parameters.Add(p => p.Name, "Accept"));

            var input = component.Find("[role=checkbox] input[type=checkbox]");
            Assert.True(input.HasAttribute("inert"));
            Assert.Equal("true", input.GetAttribute("aria-hidden"));
            Assert.Equal("-1", input.GetAttribute("tabindex"));
            Assert.Equal("Accept", input.GetAttribute("id"));
        }

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
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

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
    

        [Fact]
        public void CheckBox_WithName_LinksLabelOnClient()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.RenderComponent<RadzenCheckBox<bool>>(parameters => parameters.Add(p => p.Name, "pcheck"));

            var invocation = ctx.JSInterop.VerifyInvoke("Radzen.linkLabel");

            Assert.Equal("pcheck", invocation.Arguments[1]);
        }

        [Fact]
        public void CheckBox_WithAriaLabel_DoesNotLinkLabelOnClient()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.RenderComponent<RadzenCheckBox<bool>>(parameters => parameters
                .Add(p => p.Name, "pcheck")
                .Add(p => p.AriaLabel, "Plain check"));

            Assert.DoesNotContain(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.linkLabel");
        }

        [Fact]
        public void CheckBox_InsideFormField_IsLabelledByTheFieldAndDoesNotLinkOnClient()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters => parameters
                .Add(p => p.Text, "Plain check")
                .AddChildContent<RadzenCheckBox<bool>>(cb => cb.Add(p => p.Name, "pcheck")));

            Assert.DoesNotContain(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.linkLabel");
            Assert.Equal(component.Find("label").Id, component.Find("[role=checkbox]").GetAttribute("aria-labelledby"));
        }

        [Fact]
        public void CheckBox_LinksLabelAgain_WhenShownAgain()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenCheckBox<bool>>(parameters => parameters.Add(p => p.Name, "pcheck"));

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, false));
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Visible, true));

            Assert.Equal(2, ctx.JSInterop.Invocations.Count(i => i.Identifier == "Radzen.linkLabel"));
        }
    }
}
