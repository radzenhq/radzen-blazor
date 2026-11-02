using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class FormFieldTests
    {
        [Fact]
        public void FormField_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>();

            Assert.Contains(@"rz-form-field", component.Markup);
        }

        [Fact]
        public void FormField_Renders_Text()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters =>
            {
                parameters.Add(p => p.Text, "Email Address");
            });

            Assert.Contains("Email Address", component.Markup);
            Assert.Contains("rz-form-field-label", component.Markup);
        }

        [Fact]
        public void FormField_Renders_Variant_Outlined()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters =>
            {
                parameters.Add(p => p.Variant, Variant.Outlined);
            });

            Assert.Contains("rz-variant-outlined", component.Markup);
        }

        [Fact]
        public void FormField_Renders_Variant_Filled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters =>
            {
                parameters.Add(p => p.Variant, Variant.Filled);
            });

            Assert.Contains("rz-variant-filled", component.Markup);
        }

        [Fact]
        public void FormField_Renders_Variant_Flat()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters =>
            {
                parameters.Add(p => p.Variant, Variant.Flat);
            });

            Assert.Contains("rz-variant-flat", component.Markup);
        }

        [Fact]
        public void FormField_Renders_Variant_Text()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters =>
            {
                parameters.Add(p => p.Variant, Variant.Text);
            });

            Assert.Contains("rz-variant-text", component.Markup);
        }

        [Fact]
        public void FormField_Renders_AllowFloatingLabel_True()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters =>
            {
                parameters.Add(p => p.AllowFloatingLabel, true);
            });

            Assert.Contains("rz-floating-label", component.Markup);
        }

        [Fact]
        public void FormField_Renders_AllowFloatingLabel_False()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters =>
            {
                parameters.Add(p => p.AllowFloatingLabel, false);
            });

            Assert.DoesNotContain("rz-floating-label", component.Markup);
        }

        [Fact]
        public void FormField_Renders_FocusOnClick_True()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters =>
            {
                parameters.Add(p => p.FocusOnClick, true);
            });

            Assert.Equal("true", component.Find(".rz-form-field").GetAttribute("data-focus-on-click"));
        }

        [Fact]
        public void FormField_DoesNotRender_FocusOnClick_ByDefault()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>();

            Assert.False(component.Find(".rz-form-field").HasAttribute("data-focus-on-click"));
        }

        [Fact]
        public void FormField_Removes_FocusOnClick_WhenToggledOff()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters =>
            {
                parameters.Add(p => p.FocusOnClick, true);
            });

            component.SetParametersAndRender(parameters => parameters.Add(p => p.FocusOnClick, false));

            Assert.False(component.Find(".rz-form-field").HasAttribute("data-focus-on-click"));
        }

        [Fact]
        public void FormField_Renders_Component_Attribute()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters =>
            {
                parameters.Add(p => p.Component, "email-input");
            });

            Assert.Contains("for=\"email-input\"", component.Markup);
        }

        [Fact]
        public void FormField_Renders_Helper()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters =>
            {
                parameters.Add(p => p.Helper, builder => builder.AddContent(0, "Enter your email address"));
            });

            Assert.Contains("rz-form-field-helper", component.Markup);
            Assert.Contains("Enter your email address", component.Markup);
        }

        [Fact]
        public void FormField_Renders_Start()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters =>
            {
                parameters.Add(p => p.Start, builder => builder.AddMarkupContent(0, "<span>Start</span>"));
            });

            Assert.Contains("rz-form-field-start", component.Markup);
            Assert.Contains("Start", component.Markup);
        }

        [Fact]
        public void FormField_Renders_End()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters =>
            {
                parameters.Add(p => p.End, builder => builder.AddMarkupContent(0, "<span>End</span>"));
            });

            Assert.Contains("rz-form-field-end", component.Markup);
            Assert.Contains("End", component.Markup);
        }

        [Fact]
        public void FormField_Renders_FormFieldContent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>();

            Assert.Contains("rz-form-field-content", component.Markup);
        }
    

        [Fact]
        public void FormField_Label_PointsToTextBox_WithoutComponent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters => parameters
                .Add(p => p.Text, "Plain name")
                .AddChildContent<RadzenTextBox>(tb => tb.Add(x => x.Name, "pname")));

            var label = component.Find("label");
            var input = component.Find("input");

            Assert.Equal("pname", input.Id);
            Assert.Equal("pname", label.GetAttribute("for"));
            Assert.False(string.IsNullOrEmpty(label.Id));
        }

        [Fact]
        public void FormField_Label_PointsToTextBox_WithGeneratedId()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters => parameters
                .Add(p => p.Text, "Plain name")
                .AddChildContent<RadzenTextBox>());

            var label = component.Find("label");
            var input = component.Find("input");

            Assert.False(string.IsNullOrEmpty(input.Id));
            Assert.Equal(input.Id, label.GetAttribute("for"));
        }

        [Fact]
        public void FormField_Component_TakesPrecedenceOverChild()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters => parameters
                .Add(p => p.Component, "other")
                .AddChildContent<RadzenTextBox>(tb => tb.Add(x => x.Name, "pname")));

            Assert.Equal("other", component.Find("label").GetAttribute("for"));
        }

        [Fact]
        public void FormField_Label_FollowsChildNameChange()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters => parameters
                .AddChildContent<RadzenTextBox>(tb => tb.Add(x => x.Name, "first")));

            Assert.Equal("first", component.Find("label").GetAttribute("for"));

            component.SetParametersAndRender(parameters => parameters
                .AddChildContent<RadzenTextBox>(tb => tb.Add(x => x.Name, "second")));

            Assert.Equal("second", component.Find("input").Id);
            Assert.Equal("second", component.Find("label").GetAttribute("for"));
        }

        [Fact]
        public void FormField_Label_PointsToNumericInput()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters => parameters
                .Add(p => p.Text, "Amount")
                .AddChildContent<RadzenNumeric<int>>());

            var label = component.Find("label");
            var input = component.Find("input");

            Assert.False(string.IsNullOrEmpty(input.Id));
            Assert.Equal(input.Id, label.GetAttribute("for"));
            Assert.False(input.HasAttribute("aria-label"));
        }

        [Fact]
        public void FormField_Label_PointsToDatePickerInput()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters => parameters
                .Add(p => p.Text, "Date")
                .AddChildContent<RadzenDatePicker<System.DateTime?>>());

            var label = component.Find("label");
            var input = component.Find("input.rz-inputtext");

            Assert.False(string.IsNullOrEmpty(input.Id));
            Assert.Equal(input.Id, label.GetAttribute("for"));
        }

        [Fact]
        public void FormField_Label_PointsToSwitchInput()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters => parameters
                .Add(p => p.Text, "Enabled")
                .AddChildContent<RadzenSwitch>());

            var label = component.Find("label");
            var input = component.Find("input[role=switch]");

            Assert.False(string.IsNullOrEmpty(input.Id));
            Assert.Equal(input.Id, label.GetAttribute("for"));
        }

        [Fact]
        public void FormField_DropDown_IsLabelledByTheLabel()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters => parameters
                .Add(p => p.Text, "Country")
                .AddChildContent<RadzenDropDown<string>>(dd => dd.Add(x => x.Data, new[] { "Bulgaria" })));

            var label = component.Find("label");
            var combobox = component.Find("[role=combobox]");

            Assert.False(string.IsNullOrEmpty(label.Id));
            Assert.Equal(label.Id, combobox.GetAttribute("aria-labelledby"));
        }

        [Fact]
        public void FormField_DropDown_KeepsConsumerAriaLabel()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters => parameters
                .Add(p => p.Text, "Country")
                .AddChildContent<RadzenDropDown<string>>(dd => dd
                    .Add(x => x.Data, new[] { "Bulgaria" })
                    .AddUnmatched("aria-label", "Pick a country")));

            var combobox = component.Find("[role=combobox]");

            Assert.Equal("Pick a country", combobox.GetAttribute("aria-label"));
            Assert.False(combobox.HasAttribute("aria-labelledby"));
        }

        [Fact]
        public void FormField_CheckBox_IsLabelledByTheLabel()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters => parameters
                .Add(p => p.Text, "Accept")
                .AddChildContent<RadzenCheckBox<bool>>());

            var label = component.Find("label");
            var checkbox = component.Find("[role=checkbox]");

            Assert.False(string.IsNullOrEmpty(label.Id));
            Assert.Equal(label.Id, checkbox.GetAttribute("aria-labelledby"));
        }

        [Fact]
        public void FormField_SecurityCode_KeepsLabelOnOuterComponent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters => parameters
                .Add(p => p.Text, "Code")
                .AddChildContent<RadzenSecurityCode>(sc => sc.Add(x => x.Count, 3)));

            var label = component.Find("label");
            var group = component.Find("[role=group]");

            Assert.False(string.IsNullOrEmpty(group.Id));
            Assert.Equal(group.Id, label.GetAttribute("for"));
            Assert.Equal(label.Id, group.GetAttribute("aria-labelledby"));
        }
    

        [Fact]
        public void FormField_Label_SkipsHiddenChild()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters => parameters
                .Add(p => p.Text, "Password")
                .AddChildContent<RadzenPassword>(pw => pw.Add(x => x.Visible, false).Add(x => x.Name, "hidden"))
                .AddChildContent<RadzenTextBox>(tb => tb.Add(x => x.Name, "shown")));

            Assert.Equal("shown", component.Find("label").GetAttribute("for"));
        }

        [Fact]
        public void FormField_Label_PointsToTimeSpanPickerInput()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenFormField>(parameters => parameters
                .Add(p => p.Text, "Duration")
                .AddChildContent<RadzenTimeSpanPicker<System.TimeSpan?>>());

            var label = component.Find(".rz-form-field > .rz-form-field-content > label");
            var input = component.Find("input.rz-inputtext");

            Assert.False(string.IsNullOrEmpty(input.Id));
            Assert.Equal(input.Id, label.GetAttribute("for"));
            Assert.Empty(component.FindAll($"[aria-labelledby='{label.Id}']"));
        }
    }
}
