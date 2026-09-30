using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class FormFieldValueUpdateTests
    {
        class RecordingNumeric : RadzenNumeric<int>
        {
            public List<int> ReceivedValues { get; } = new();

            public override Task SetParametersAsync(ParameterView parameters)
            {
                if (parameters.TryGetValue<int>(nameof(Value), out var value))
                {
                    ReceivedValues.Add(value);
                }

                return base.SetParametersAsync(parameters);
            }
        }

        class Host : ComponentBase
        {
            public int Value { get; set; }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<RadzenFormField>(0);
                builder.AddAttribute(1, nameof(RadzenFormField.Text), "Test");
                builder.AddAttribute(2, nameof(RadzenFormField.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenComponent<RecordingNumeric>(0);
                    b.AddAttribute(1, nameof(RecordingNumeric.Value), Value);
                    b.AddAttribute(2, nameof(RecordingNumeric.ValueChanged), EventCallback.Factory.Create<int>(this, v => Value = v));
                    b.CloseComponent();
                }));
                builder.CloseComponent();
            }
        }

        [Fact]
        public void ComponentInsideFormFieldReceivesEachNewValueOnce()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var host = ctx.RenderComponent<Host>();
            var numeric = host.FindComponent<RecordingNumeric>().Instance;

            host.Find("input").Change("5");

            Assert.Equal(new[] { 0, 5 }, numeric.ReceivedValues);
            Assert.Equal(5, host.Instance.Value);
        }

        [Fact]
        public void FormFieldStillReflectsDisabledChangesOfTheWrappedComponent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenFormField>(parameters =>
            {
                parameters.Add(p => p.ChildContent, b =>
                {
                    b.OpenComponent<RadzenTextBox>(0);
                    b.AddAttribute(1, nameof(RadzenTextBox.Disabled), true);
                    b.CloseComponent();
                });
            });

            Assert.Contains("rz-state-disabled", component.Find(".rz-form-field").ClassName);
        }
    }
}
