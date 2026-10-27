using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SpeechToTextButtonTests
    {
        [Fact]
        public void SpeechToTextButton_Renders_Record_Button_When_Visible()
        {
            using var ctx = new BunitContext();

            var component = ctx.Render<RadzenSpeechToTextButton>();

            component.Render();

            var recordButton = component.Find("button.rz-button-icon-only.rz-speech-to-text-button");

            Assert.NotNull(recordButton);
        }

        [Fact]
        public void SpeechToTextButton_Does_Not_Renders_Record_Button_When_Visible_False()
        {
            using var ctx = new BunitContext();

            var component = ctx.Render<RadzenSpeechToTextButton>();

            component.Render(parameters =>
            {
                parameters.Add(p => p.Visible, false);
            });

            Assert.Throws<ElementNotFoundException>(() => component.Find("button.rz-button-icon-only.rz-speech-to-text-button"));
        }

        [Fact]
        public void SpeechToTextButton_Renders_Additional_Css()
        {
            using var ctx = new BunitContext();

            var component =
                ctx.Render<RadzenSpeechToTextButton>(p => p.AddUnmatched("class", "another-class"));

            var recordButton = component.Find("button.rz-button-icon-only.rz-speech-to-text-button.another-class");

            Assert.NotNull(recordButton);
        }

        [Fact]
        public void SpeechToTextButton_Can_Override_Default_Title_And_Aria_Label()
        {
            using var ctx = new BunitContext();

            var component =
                ctx.Render<RadzenSpeechToTextButton>(p => p
                    .AddUnmatched("title", "title override")
                    .AddUnmatched("aria-label", "aria-label override"));

            var recordButton = component.Find("button.rz-button-icon-only.rz-speech-to-text-button");

            Assert.NotNull(recordButton);
            Assert.Equal("title override", recordButton.GetAttribute("title"));
            Assert.Equal("aria-label override", recordButton.GetAttribute("aria-label"));
        }

        [Fact]
        public void SpeechToTextButton_Sets_Record_Button_Css_When_Record_Button_Clicked()
        {
            using var ctx = new BunitContext();

            var component = ctx.Render<RadzenSpeechToTextButton>();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            component.Render();

            var recordButton = component.Find("button.rz-button-icon-only.rz-speech-to-text-button");

            Assert.NotNull(recordButton);

            recordButton.Click();

            component.Render();

            var blinkingRecordButton = component.Find("button.rz-button-icon-only.rz-speech-to-text-button-recording");

            Assert.NotNull(blinkingRecordButton);
        }

        [Fact]
        public void SpeechToTextButton_Sets_StopTitle_When_Record_Button_Clicked()
        {
            using var ctx = new BunitContext();

            var component = ctx.Render<RadzenSpeechToTextButton>();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            component.Render();

            var recordButton = component.Find("button.rz-button-icon-only.rz-speech-to-text-button");

            Assert.NotNull(recordButton);

            recordButton.Click();

            component.Render();

            var blinkingRecordButton = component.Find("button.rz-button-icon-only.rz-speech-to-text-button-recording");

            Assert.Equal(component.Instance.StopTitle, blinkingRecordButton.GetAttribute("title"));
        }

        [Fact]
        public void SpeechToTextButton_ChangesIconWhen_When_Record_Button_Clicked()
        {
            using var ctx = new BunitContext();

            var component = ctx.Render<RadzenSpeechToTextButton>();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            component.Render();

            var recordButton = component.Find("button.rz-button-icon-only.rz-speech-to-text-button");

            Assert.NotNull(recordButton);

            recordButton.Click();

            component.Render();

            var blinkingRecordButton = component.Find("button span");

            Assert.Contains("stop", blinkingRecordButton.TextContent);
            Assert.DoesNotContain("mic", blinkingRecordButton.TextContent);
        }

        [Fact]
        public void SpeechToTextButton_UnSets_Record_Button_Css_When_Record_Button_Clicked_Twice()
        {
            using var ctx = new BunitContext();

            var component = ctx.Render<RadzenSpeechToTextButton>();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            component.Render();

            var recordButton = component.Find("button.rz-button-icon-only.rz-speech-to-text-button");

            Assert.NotNull(recordButton);

            recordButton.Click();

            component.Render();

            const string blinkingRecordButtonSelector = "button.rz-button-icon-only.rz-speech-to-text-button-recording";
            var blinkingRecordButton = component.Find(blinkingRecordButtonSelector);

            Assert.NotNull(blinkingRecordButton);

            blinkingRecordButton.Click();

            component.Render();

            Assert.Throws<ElementNotFoundException>(() => component.Find(blinkingRecordButtonSelector));
        }

        [Fact]
        public void SpeechToTextButton_Invokes_OnResult_FromJs()
        {
            using var ctx = new BunitContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.Render<RadzenSpeechToTextButton>();

            string resultsFromJs = null;

            component.Render(parameters => parameters.Add(p => p.Change, r => resultsFromJs = r));

            var recordButton = component.Find("button.rz-button-icon-only.rz-speech-to-text-button");

            Assert.NotNull(recordButton);

            recordButton.Click();

            const string speechResults = "results from js";

            component.InvokeAsync(() => component.Instance.OnResult(speechResults));

            Assert.Equal(speechResults, resultsFromJs);
        }
    }
}
