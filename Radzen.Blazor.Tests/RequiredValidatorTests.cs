using System;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class RequiredValidatorTests
    {
        class Model
        {
            public string Text { get; set; }
        }

        sealed class ValidatedFormWrapper<TComponent> : ComponentBase where TComponent : IComponent
        {
            public Model ModelInstance { get; } = new Model();

            public int SubmitCount { get; private set; }

            public int InvalidSubmitCount { get; private set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<RadzenTemplateForm<Model>>(0);
                builder.AddAttribute(1, "Data", ModelInstance);
                builder.AddAttribute(2, "Submit", EventCallback.Factory.Create<Model>(this, _ => SubmitCount++));
                builder.AddAttribute(3, "InvalidSubmit", EventCallback.Factory.Create<FormInvalidSubmitEventArgs>(this, _ => InvalidSubmitCount++));
                builder.AddAttribute(4, "ChildContent", (RenderFragment<EditContext>)(context => childBuilder =>
                {
                    childBuilder.OpenComponent<TComponent>(0);
                    childBuilder.AddAttribute(1, "Name", "Text");
                    childBuilder.AddAttribute(2, "Value", ModelInstance.Text);
                    childBuilder.AddAttribute(3, "ValueChanged",
                        EventCallback.Factory.Create<string>(this, v => ModelInstance.Text = v));
                    childBuilder.AddAttribute(4, "ValueExpression",
                        (Expression<Func<string>>)(() => ModelInstance.Text));
                    childBuilder.CloseComponent();

                    childBuilder.OpenComponent<RadzenRequiredValidator>(5);
                    childBuilder.AddAttribute(6, nameof(RadzenRequiredValidator.Component), "Text");
                    childBuilder.AddAttribute(7, nameof(RadzenRequiredValidator.Text), "Required");
                    childBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }

        static IRenderedComponent<ValidatedFormWrapper<TComponent>> RenderForm<TComponent>(TestContext ctx)
            where TComponent : IComponent
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            return ctx.RenderComponent<ValidatedFormWrapper<TComponent>>();
        }

        static void AssertStaysValidAfterChange<TComponent>(string selector) where TComponent : IComponent
        {
            using var ctx = new TestContext();

            var wrapper = RenderForm<TComponent>(ctx);

            wrapper.Find(selector).Change("some value");

            var validator = wrapper.FindComponent<RadzenRequiredValidator>().Instance;

            Assert.Equal("some value", wrapper.Instance.ModelInstance.Text);
            Assert.True(validator.IsValid);
            Assert.DoesNotContain("rz-messages-error", wrapper.Markup);
        }

        [Fact]
        public void RequiredValidator_StaysValid_WhenBoundTextBoxChanges()
        {
            AssertStaysValidAfterChange<RadzenTextBox>("input");
        }

        [Fact]
        public void RequiredValidator_StaysValid_WhenBoundTextAreaChanges()
        {
            AssertStaysValidAfterChange<RadzenTextArea>("textarea");
        }

        [Fact]
        public void RequiredValidator_StaysValid_WhenBoundPasswordChanges()
        {
            AssertStaysValidAfterChange<RadzenPassword>("input");
        }

        [Fact]
        public void RequiredValidator_StaysValid_WhenBoundMaskChanges()
        {
            AssertStaysValidAfterChange<RadzenMask>("input");
        }

        [Fact]
        public void RequiredValidator_StaysValid_WhenBoundAutoCompleteChanges()
        {
            AssertStaysValidAfterChange<RadzenAutoComplete>("input");
        }

        [Fact]
        public void RequiredValidator_BecomesInvalid_WhenBoundTextBoxIsCleared()
        {
            using var ctx = new TestContext();

            var wrapper = RenderForm<RadzenTextBox>(ctx);

            wrapper.Find("input").Change("some value");
            wrapper.Find("input").Change("");

            var validator = wrapper.FindComponent<RadzenRequiredValidator>().Instance;

            Assert.False(validator.IsValid);
            Assert.Contains("rz-messages-error", wrapper.Markup);
        }

        [Fact]
        public void Submit_Fires_WhenBoundTextBoxHasValue()
        {
            using var ctx = new TestContext();

            var wrapper = RenderForm<RadzenTextBox>(ctx);

            wrapper.Find("input").Change("some value");
            wrapper.Find("form").Submit();

            Assert.Equal(1, wrapper.Instance.SubmitCount);
            Assert.Equal(0, wrapper.Instance.InvalidSubmitCount);
        }

        [Fact]
        public void InvalidSubmit_Fires_WhenBoundTextBoxIsEmpty()
        {
            using var ctx = new TestContext();

            var wrapper = RenderForm<RadzenTextBox>(ctx);

            wrapper.Find("form").Submit();

            Assert.Equal(0, wrapper.Instance.SubmitCount);
            Assert.Equal(1, wrapper.Instance.InvalidSubmitCount);
        }
    }
}
