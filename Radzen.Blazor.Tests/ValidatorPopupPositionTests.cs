using System;
using System.Linq;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ValidatorPopupPositionTests
    {
        sealed class Model
        {
            public string Text { get; set; }
        }

        sealed class FormWrapper : ComponentBase
        {
            readonly Model model = new Model();

            [Parameter]
            public bool Popup { get; set; }

            [Parameter]
            public TooltipPosition PopupPosition { get; set; } = TooltipPosition.Bottom;

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<RadzenTemplateForm<Model>>(0);
                builder.AddAttribute(1, "Data", model);
                builder.AddAttribute(2, "ChildContent", (RenderFragment<EditContext>)(context => childBuilder =>
                {
                    childBuilder.OpenComponent<RadzenTextBox>(0);
                    childBuilder.AddAttribute(1, "Name", "Text");
                    childBuilder.AddAttribute(2, "Value", model.Text);
                    childBuilder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<string>(this, v => model.Text = v));
                    childBuilder.AddAttribute(4, "ValueExpression", (Expression<Func<string>>)(() => model.Text));
                    childBuilder.CloseComponent();

                    childBuilder.OpenComponent<RadzenRequiredValidator>(5);
                    childBuilder.AddAttribute(6, nameof(RadzenRequiredValidator.Component), "Text");
                    childBuilder.AddAttribute(7, nameof(RadzenRequiredValidator.Text), "Required");
                    childBuilder.AddAttribute(8, nameof(RadzenRequiredValidator.Popup), Popup);
                    childBuilder.AddAttribute(9, nameof(RadzenRequiredValidator.PopupPosition), PopupPosition);
                    childBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }

        static IRenderedComponent<FormWrapper> RenderInvalid(TestContext ctx, bool popup, TooltipPosition position)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var wrapper = ctx.RenderComponent<FormWrapper>(p => p.Add(x => x.Popup, popup).Add(x => x.PopupPosition, position));

            wrapper.Find("input").Change("some value");
            wrapper.Find("input").Change("");

            return wrapper;
        }

        [Fact]
        public void BottomPopup_IsTheDefault_AndIsNotPositionedByScript()
        {
            using var ctx = new TestContext();

            var wrapper = RenderInvalid(ctx, popup: true, TooltipPosition.Bottom);

            Assert.Equal("rz-message rz-messages-error rz-message-popup", wrapper.Find(".rz-message").ClassName);
            Assert.True(string.IsNullOrEmpty(wrapper.Find(".rz-message").Id));
            Assert.DoesNotContain(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.positionValidator");
        }

        [Theory]
        [InlineData(TooltipPosition.Top, "top")]
        [InlineData(TooltipPosition.Left, "left")]
        [InlineData(TooltipPosition.Right, "right")]
        public void PopupOnAnotherSide_GetsItsClass_AndIsPositionedByScript(TooltipPosition position, string side)
        {
            using var ctx = new TestContext();

            var wrapper = RenderInvalid(ctx, popup: true, position);

            var validator = wrapper.Find(".rz-message");
            Assert.Contains($"rz-message-popup-{side}", validator.ClassName);

            var call = Assert.Single(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.positionValidator");
            Assert.Equal(validator.Id, call.Arguments[0]);
            Assert.Equal("Text", call.Arguments[1]);
            Assert.Equal(side, call.Arguments[2]);
        }

        [Fact]
        public void ChangingBackToBottom_UndoesThePositionSetByScript()
        {
            using var ctx = new TestContext();

            var wrapper = RenderInvalid(ctx, popup: true, TooltipPosition.Top);
            wrapper.SetParametersAndRender(p => p.Add(x => x.PopupPosition, TooltipPosition.Bottom));

            var calls = ctx.JSInterop.Invocations.Where(i => i.Identifier == "Radzen.positionValidator").ToList();
            Assert.Equal("top", calls[0].Arguments[2]);
            Assert.Equal("bottom", calls[^1].Arguments[2]);
        }

        [Fact]
        public void PopupPosition_IsIgnored_WhenTheValidatorIsNotAPopup()
        {
            using var ctx = new TestContext();

            var wrapper = RenderInvalid(ctx, popup: false, TooltipPosition.Top);

            Assert.Equal("rz-message rz-messages-error ", wrapper.Find(".rz-message").ClassName);
            Assert.True(string.IsNullOrEmpty(wrapper.Find(".rz-message").Id));
            Assert.DoesNotContain(ctx.JSInterop.Invocations, i => i.Identifier == "Radzen.positionValidator");
        }
    }
}
