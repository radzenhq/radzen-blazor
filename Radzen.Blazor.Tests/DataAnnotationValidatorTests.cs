using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataAnnotationValidatorTests
    {
        class Model
        {
            [Required(ErrorMessage = "Name is required.")]
            [StringLength(15, MinimumLength = 4, ErrorMessage = "Name must be between 4 and 15 characters.")]
            public string Name { get; set; }
        }

        private static IRenderedComponent<RadzenTemplateForm<Model>> RenderForm(BunitContext ctx, Model model)
        {
            return ctx.Render<RadzenTemplateForm<Model>>(parameters => parameters
                .Add(p => p.Data, model)
                .Add(p => p.ChildContent, editContext => builder =>
                {
                    Expression<Func<string>> valueExpression = () => model.Name;

                    builder.OpenComponent<RadzenTextBox>(0);
                    builder.AddAttribute(1, "Name", "Name");
                    builder.AddAttribute(2, "Value", model.Name);
                    builder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<string>(model, v => model.Name = v));
                    builder.AddAttribute(4, "ValueExpression", valueExpression);
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenDataAnnotationValidator>(5);
                    builder.AddAttribute(6, "Component", "Name");
                    builder.CloseComponent();
                }));
        }

        [Fact]
        public async Task DataAnnotationValidator_Updates_Message_When_Transitioning_Between_Invalid_States()
        {
            using var ctx = new BunitContext();
            var model = new Model();

            var form = RenderForm(ctx, model);

            // Trigger validation with empty value → [Required] fails.
            await form.InvokeAsync(() => form.Instance.EditContext.ValidateAsync());

            Assert.Contains("Name is required.", form.Markup);
            Assert.DoesNotContain("Name must be between 4 and 15 characters.", form.Markup);

            // Change value to a single character → [Required] passes, [StringLength] fails.
            // IsValid remains false, but Text should change and view should re-render.
            model.Name = "a";
            await form.InvokeAsync(() => form.Instance.EditContext.ValidateAsync());

            Assert.Contains("Name must be between 4 and 15 characters.", form.Markup);
            Assert.DoesNotContain("Name is required.", form.Markup);
        }

        [Fact]
        public async Task DataAnnotationValidator_Clears_Message_When_Value_Becomes_Valid()
        {
            using var ctx = new BunitContext();
            var model = new Model();

            var form = RenderForm(ctx, model);

            await form.InvokeAsync(() => form.Instance.EditContext.ValidateAsync());
            Assert.Contains("Name is required.", form.Markup);

            model.Name = "valid name";
            await form.InvokeAsync(() => form.Instance.EditContext.ValidateAsync());

            Assert.DoesNotContain("Name is required.", form.Markup);
            Assert.DoesNotContain("Name must be between 4 and 15 characters.", form.Markup);
        }
    }
}
