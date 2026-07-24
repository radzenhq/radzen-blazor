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
        public void TextArea_KeepsTypedValue_WhenUsedWithoutTwoWayBinding()
        {
            // Demo-style usage: no @bind-Value or ValueChanged. The user-typed value
            // must survive the post-handler @bind:get/:set sync, otherwise the textarea
            // clears itself on every blur.
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextArea>();

            component.Find("textarea").Change("user-typed");

            Assert.Equal("user-typed", component.Instance.Value);
        }

        [Fact]
        public void TextArea_KeepsTypedValue_WhenBoundWithoutParameterReflow()
        {
            using var ctx = new TestContext();

            var boundValue = "original";
            var component = ctx.RenderComponent<RadzenTextArea>(parameters =>
            {
                parameters.Add(p => p.Value, boundValue);
                parameters.Add(p => p.ValueChanged, v => boundValue = v);
            });

            component.Find("textarea").Change("user-typed");

            Assert.Equal("user-typed", boundValue);
            Assert.Equal("user-typed", component.Instance.Value);
        }

        [Fact]
        public void TextArea_SyncsDomValue_WhenParentTransformsInput()
        {
            using var ctx = new TestContext();

            var wrapper = ctx.RenderComponent<RadzenTextAreaTransformWrapper>();

            wrapper.Find("textarea").Change("user-typed");

            Assert.Equal("USER-TYPED", wrapper.FindComponent<RadzenTextArea>().Instance.Value);
            Assert.Equal("USER-TYPED", wrapper.Find("textarea").GetAttribute("value"));
        }

        private sealed class RadzenTextAreaTransformWrapper : Microsoft.AspNetCore.Components.ComponentBase
        {
            public string HeldValue { get; private set; } = "original";

            protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
            {
                builder.OpenComponent<RadzenTextArea>(0);
                builder.AddAttribute(1, nameof(RadzenTextArea.Value), HeldValue);
                builder.AddAttribute(2, nameof(RadzenTextArea.ValueChanged),
                    Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string>(this, v => { HeldValue = v.ToUpperInvariant(); StateHasChanged(); }));
                builder.CloseComponent();
            }
        }
    }
}
