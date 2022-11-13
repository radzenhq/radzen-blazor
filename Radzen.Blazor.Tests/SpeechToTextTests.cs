using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SpeechToTextTests
    {
        [Fact]
        public void SpeechToText_Renders_Record_Button_When_Visible()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSpeechToText>();

            component.Render();

            var recordButton = component.Find("button.rz-button-icon-only.rz-mic");

            Assert.NotNull(recordButton);
        }

        [Fact]
        public void SpeechToText_Does_Not_Renders_Record_Button_When_Visible_False()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSpeechToText>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.Throws<ElementNotFoundException>(() => component.Find("button.rz-button-icon-only.rz-mic"));
        }

        [Fact]
        public void SpeechToText_Renders_Additional_Css()
        {
            using var ctx = new TestContext();

            var component =
                ctx.RenderComponent<RadzenSpeechToText>(ComponentParameter.CreateParameter("class", "another-class"));

            var recordButton = component.Find("button.rz-button-icon-only.rz-mic.another-class");

            Assert.NotNull(recordButton);
        }

        [Fact]
        public void SpeechToText_Sets_Record_Button_Css_When_Record_Button_Clicked()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSpeechToText>();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            component.Render();

            var recordButton = component.Find("button.rz-button-icon-only.rz-mic");

            Assert.NotNull(recordButton);

            recordButton.Click();

            component.Render();

            var blinkingRecordButton = component.Find("button.rz-button-icon-only.rz-mic-on");

            Assert.NotNull(blinkingRecordButton);
        }

        [Fact]
        public void SpeechToText_UnSets_Record_Button_Css_When_Record_Button_Clicked_Twice()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenSpeechToText>();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            component.Render();

            var recordButton = component.Find("button.rz-button-icon-only.rz-mic");

            Assert.NotNull(recordButton);

            recordButton.Click();

            component.Render();

            const string blinkingRecordButtonSelector = "button.rz-button-icon-only.rz-mic-on";
            var blinkingRecordButton = component.Find(blinkingRecordButtonSelector);

            Assert.NotNull(blinkingRecordButton);

            blinkingRecordButton.Click();

            component.Render();

            Assert.Throws<ElementNotFoundException>(() => component.Find(blinkingRecordButtonSelector));
        }

        [Fact]
        public void SpeechToText_Invokes_OnResult_FromJs()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenSpeechToText>();

            string resultsFromJs = null;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.OnResult, r => resultsFromJs = r));

            var recordButton = component.Find("button.rz-button-icon-only.rz-mic");

            Assert.NotNull(recordButton);

            recordButton.Click();

            const string speechResults = "results from js";

            component.InvokeAsync(() => component.Instance.OnResultFromJs(speechResults));

            Assert.Equal(speechResults, resultsFromJs);
        }
    }
}
