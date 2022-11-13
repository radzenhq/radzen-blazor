using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TextAreaTests
    {
        [Fact]
        public void TextArea_Renders_CssClass()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            Assert.Contains(@$"rz-textarea", component.Markup);
        }

        [Fact]
        public void TextArea_Renders_ValueParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Value, value));

            Assert.Contains(@$"value=""{value}""", component.Markup);
        }

        [Fact]
        public void TextArea_Renders_StyleParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void TextArea_Renders_NameParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Name, value));

            Assert.Contains(@$"name=""{value}""", component.Markup);
        }

        [Fact]
        public void TextArea_Renders_TabIndexParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            var value = 1;

            component.SetParametersAndRender(parameters => parameters.Add<int>(p => p.TabIndex, value));

            Assert.Contains(@$"tabindex=""{value}""", component.Markup);
        }

        [Fact]
        public void TextArea_Renders_RowsParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            var value = 10;

            component.SetParametersAndRender(parameters => parameters.Add<int>(p => p.Rows, value));

            Assert.Contains(@$"rows=""{value}""", component.Markup);
        }

        [Fact]
        public void TextArea_Renders_ColsParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            var value = 10;

            component.SetParametersAndRender(parameters => parameters.Add<int>(p => p.Cols, value));

            Assert.Contains(@$"cols=""{value}""", component.Markup);
        }

        [Fact]
        public void TextArea_Renders_PlaceholderParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Placeholder, value));

            Assert.Contains(@$"placeholder=""{value}""", component.Markup);
        }

        [Fact]
        public void TextArea_Renders_DisabledParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.Disabled, true));

            Assert.Contains(@$"disabled", component.Markup);
            Assert.Contains(@$"rz-state-disabled", component.Markup);
        }

        [Fact]
        public void TextArea_Renders_ReadOnlyParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            var value = true;

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.ReadOnly, value));

            Assert.Contains(@$"readonly", component.Markup);
        }

        [Fact]
        public void TextArea_Renders_MaxLengthParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            var value = 10;

            component.SetParametersAndRender(parameters => parameters.Add<long?>(p => p.MaxLength, value));

            Assert.Contains(@$"maxlength=""{value}""", component.Markup);
        }

        [Fact]
        public void TextArea_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }

        [Fact]
        public void TextArea_Raises_ChangedEvent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            var raised = false;
            var value = "Test";
            object newValue = null;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Change, args => { raised = true; newValue = args; }));

            component.Find("textarea").Change(value);

            Assert.True(raised);
            Assert.True(object.Equals(value, newValue));
        }

        [Fact]
        public void TextArea_Raises_ValueChangedEvent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            var raised = false;
            var value = "Test";
            object newValue = null;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ValueChanged, args => { raised = true; newValue = args; }));

            component.Find("textarea").Change(value);

            Assert.True(raised);
            Assert.True(object.Equals(value, newValue));
        }

        [Fact]
        public void TextArea_Renders_Record_Button_When_EnableSpeechToText_Is_True_And_Not_Disabled()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.EnableSpeechToText, true));

            var recordButton = component.Find("button.rz-button-icon-only");

            Assert.NotNull(recordButton);
        }

        [Fact]
        public void TextArea_Does_Not_Render_Record_Button_When_EnableSpeechToText_Is_True_And_Disabled()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.EnableSpeechToText, true);
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Throws<ElementNotFoundException>(() => component.Find("button.rz-button-icon-only"));
        }

        [Fact]
        public void TextArea_Does_Not_Render_Record_Button_When_EnableSpeechToText_Is_False()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            component.Render();

            Assert.Throws<ElementNotFoundException>(() => component.Find("button.rz-button-icon-only"));
        }

        [Fact]
        public void TextArea_Sets_Record_Button_Css_When_Record_Button_Clicked()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.EnableSpeechToText, true));

            var recordButton = component.Find("button.rz-button-icon-only.rz-mic");

            Assert.NotNull(recordButton);

            recordButton.Click();

            component.Render();

            var blinkingRecordButton = component.Find("button.rz-button-icon-only.rz-mic-on");

            Assert.NotNull(blinkingRecordButton);
        }

        [Fact]
        public void TextArea_UnSets_Record_Button_Css_When_Record_Button_Clicked_Twice()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.EnableSpeechToText, true));

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
    }
}
