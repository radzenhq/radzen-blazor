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
            var component = ctx.RenderComponent<RadzenFormField>();

            Assert.Contains(@"rz-form-field", component.Markup);
        }

        [Fact]
        public void FormField_Renders_Text()
        {
            using var ctx = new TestContext();
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
            var component = ctx.RenderComponent<RadzenFormField>(parameters =>
            {
                parameters.Add(p => p.AllowFloatingLabel, false);
            });

            Assert.DoesNotContain("rz-floating-label", component.Markup);
        }

        [Fact]
        public void FormField_Renders_Component_Attribute()
        {
            using var ctx = new TestContext();
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
            var component = ctx.RenderComponent<RadzenFormField>();

            Assert.Contains("rz-form-field-content", component.Markup);
        }
    }
}

