using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class TextBoxTests
    {
        [Fact]
        public void TextBox_Renders_CssClass()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextBox>();

            Assert.Contains(@$"rz-textbox", component.Markup);
        }

        [Fact]
        public void TextBox_Renders_ValueParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextBox>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Value, value));

            Assert.Contains(@$"value=""{value}""", component.Markup);
        }

        [Fact]
        public void TextboxCanSetFieldIdentifier()
        {
            using var ctx = new TestContext();

            var editContext = new EditContext(ctx);
            var fieldIdentifier = new FieldIdentifier(ctx, nameof(RadzenTextBox.Value));
            ctx.RenderTree.TryAdd<CascadingValue<EditContext>>(parameters =>
            {
                parameters.Add(e => e.Value, editContext);
            });

            var component = ctx.RenderComponent<RadzenTextBox>(parameters =>
            {
                parameters.Add(p => p.FieldIdentifier, fieldIdentifier);
            });

            Assert.Equal(component.Instance.FieldIdentifier, fieldIdentifier);
        }

        [Fact]
        public void TextBox_Renders_StyleParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextBox>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void TextBox_Renders_NameParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextBox>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Name, value));

            Assert.Contains(@$"name=""{value}""", component.Markup);
        }

        [Fact]
        public void TextBox_Renders_TabIndexParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextBox>();

            var value = 1;

            component.SetParametersAndRender(parameters => parameters.Add<int>(p => p.TabIndex, value));

            Assert.Contains(@$"tabindex=""{value}""", component.Markup);
        }

        [Fact]
        public void TextBox_Renders_PlaceholderParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextBox>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Placeholder, value));

            Assert.Contains(@$"placeholder=""{value}""", component.Markup);
        }

        [Fact]
        public void TextBox_Renders_DisabledParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextBox>();

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.Disabled, true));

            Assert.Contains(@$"disabled", component.Markup);
            Assert.Contains(@$"rz-state-disabled", component.Markup);
        }

        [Fact]
        public void TextBox_Renders_ReadOnlyParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextBox>();

            var value = true;

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.ReadOnly, value));

            Assert.Contains(@$"readonly", component.Markup);
        }

        [Fact]
        public void TextBox_Renders_AutoCompleteParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextBox>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("AutoComplete", false));

            Assert.Contains(@$"autocomplete=""off""", component.Markup);
            Assert.Contains(@$"aria-autocomplete=""none""", component.Markup);

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("AutoComplete", true));

            Assert.Contains(@$"autocomplete=""on""", component.Markup);
            Assert.DoesNotContain(@$"aria-autocomplete", component.Markup);

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autocomplete", "custom"));

            Assert.Contains(@$"autocomplete=""custom""", component.Markup);
            Assert.DoesNotContain(@$"aria-autocomplete", component.Markup);

            component.Instance.DefaultAutoCompleteAttribute = "autocomplete-custom";
            component.SetParametersAndRender(parameters => parameters.AddUnmatched("AutoComplete", false));

            Assert.Contains(@$"autocomplete=""autocomplete-custom""", component.Markup);
            Assert.Contains(@$"aria-autocomplete=""none""", component.Markup);
        }

        [Fact]
        public void TextBox_Renders_TypedAutoCompleteParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextBox>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("AutoComplete", false));
            component.SetParametersAndRender(parameters => parameters.Add<AutoCompleteType>(p => p.AutoCompleteType, AutoCompleteType.On));

            Assert.Contains(@$"autocomplete=""off""", component.Markup);

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("AutoComplete", true));
            component.SetParametersAndRender(parameters => parameters.Add<AutoCompleteType>(p => p.AutoCompleteType, AutoCompleteType.Off));

            Assert.Contains(@$"autocomplete=""off""", component.Markup);

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("AutoComplete", true));
            component.SetParametersAndRender(parameters => parameters.Add<AutoCompleteType>(p => p.AutoCompleteType, AutoCompleteType.AdditionalName));

            Assert.Contains(@$"autocomplete=""{AutoCompleteType.AdditionalName.GetAutoCompleteValue()}""", component.Markup);

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("AutoComplete", true));
            component.SetParametersAndRender(parameters => parameters.Add<AutoCompleteType>(p => p.AutoCompleteType, AutoCompleteType.FamilyName));

            Assert.Contains(@$"autocomplete=""{AutoCompleteType.FamilyName.GetAutoCompleteValue()}""", component.Markup);
        }

        [Fact]
        public void TextBox_Renders_MaxLengthParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextBox>();

            var value = 10;

            component.SetParametersAndRender(parameters => parameters.Add<long?>(p => p.MaxLength, value));

            Assert.Contains(@$"maxlength=""{value}""", component.Markup);
        }

        [Fact]
        public void TextBox_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextBox>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }
        
        [Fact]
        public void TextBox_Raises_ChangedEvent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextBox>();

            var raised = false;
            var value = "Test";
            object newValue = null;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Change, args => { raised = true; newValue = args; }));

            component.Find("input").Change(value);

            Assert.True(raised);
            Assert.True(object.Equals(value, newValue));
        }

        [Fact]
        public void TextBox_Raises_ValueChangedEvent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextBox>();

            var raised = false;
            var value = "Test";
            object newValue = null;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ValueChanged, args => { raised = true; newValue = args; }));

            component.Find("input").Change(value);

            Assert.True(raised);
            Assert.True(object.Equals(value, newValue));
        }

        [Fact]
        public void TextBox_Trim_TrimsOnChange()
        {
            using var ctx = new TestContext();

            var wrapper = ctx.RenderComponent<RadzenTextBoxBindWrapper>(parameters =>
            {
                parameters.Add(p => p.Trim, true);
            });

            wrapper.Find("input").Change("  hello  ");

            Assert.Equal("hello", wrapper.Instance.BoundValue);
            Assert.Equal("hello", wrapper.FindComponent<RadzenTextBox>().Instance.Value);
        }

        [Fact]
        public void TextBox_Immediate_Trim_DoesNotTrimOnInput()
        {
            using var ctx = new TestContext();

            int changeCount = 0;
            var wrapper = ctx.RenderComponent<RadzenTextBoxBindWrapper>(parameters =>
            {
                parameters.Add(p => p.Immediate, true);
                parameters.Add(p => p.Trim, true);
                parameters.Add(p => p.OnChange, args => changeCount++);
            });

            wrapper.Find("input").Input("hello ");

            Assert.Equal("hello ", wrapper.Instance.BoundValue);
            Assert.Equal("hello ", wrapper.FindComponent<RadzenTextBox>().Instance.Value);
            Assert.Equal(1, changeCount);
        }

        [Fact]
        public void TextBox_Immediate_Trim_TrimsOnChangeAfterInput()
        {
            using var ctx = new TestContext();

            var wrapper = ctx.RenderComponent<RadzenTextBoxBindWrapper>(parameters =>
            {
                parameters.Add(p => p.Immediate, true);
                parameters.Add(p => p.Trim, true);
            });

            wrapper.Find("input").Input("hello world ");
            wrapper.Find("input").Change("hello world ");

            Assert.Equal("hello world", wrapper.Instance.BoundValue);
            Assert.Equal("hello world", wrapper.FindComponent<RadzenTextBox>().Instance.Value);
        }

        [Fact]
        public void TextBox_Trim_SkipsChangeNotificationWhenTrimDoesNotAlterExistingValue()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextBox>(parameters =>
            {
                parameters.Add(p => p.Trim, true);
                parameters.Add(p => p.Value, "hello");
            });

            int changeCount = 0;
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Change, _ => changeCount++));

            component.Find("input").Change("hello   ");

            Assert.Equal("hello", component.Instance.Value);
            Assert.Equal(0, changeCount);
        }

        [Fact]
        public void TextBox_KeepsTypedValue_WhenUsedWithoutTwoWayBinding()
        {
            // Demo-style usage: <RadzenTextBox Change=... /> with no @bind-Value or ValueChanged.
            // The user-typed value must survive the post-handler @bind:get/:set sync,
            // otherwise the input clears itself on every blur.
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenTextBox>();

            component.Find("input").Change("user-typed");

            Assert.Equal("user-typed", component.Instance.Value);
            Assert.Equal("user-typed", component.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void TextBox_SyncsDomValue_WhenParentRejectsInput()
        {
            using var ctx = new TestContext();

            // Wrapper holds Value at "original" and ignores ValueChanged (parent rejects).
            // After the user types, the DOM input should be synced back to "original".
            var wrapper = ctx.RenderComponent<RadzenTextBoxRejectWrapper>();

            wrapper.Find("input").Change("user-typed");

            Assert.Equal("original", wrapper.FindComponent<RadzenTextBox>().Instance.Value);
            Assert.Equal("original", wrapper.Find("input").GetAttribute("value"));
        }

        private sealed class RadzenTextBoxBindWrapper : Microsoft.AspNetCore.Components.ComponentBase
        {
            public string BoundValue { get; private set; }

            [Microsoft.AspNetCore.Components.Parameter]
            public bool Immediate { get; set; }

            [Microsoft.AspNetCore.Components.Parameter]
            public bool Trim { get; set; }

            [Microsoft.AspNetCore.Components.Parameter]
            public Microsoft.AspNetCore.Components.EventCallback<string> OnChange { get; set; }

            protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
            {
                builder.OpenComponent<RadzenTextBox>(0);
                builder.AddAttribute(1, nameof(RadzenTextBox.Value), BoundValue);
                builder.AddAttribute(2, nameof(RadzenTextBox.ValueChanged),
                    Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string>(this, v => { BoundValue = v; StateHasChanged(); }));
                builder.AddAttribute(3, nameof(RadzenTextBox.Immediate), Immediate);
                builder.AddAttribute(4, nameof(RadzenTextBox.Trim), Trim);
                builder.AddAttribute(5, nameof(RadzenTextBox.Change), OnChange);
                builder.CloseComponent();
            }
        }

        private sealed class RadzenTextBoxRejectWrapper : Microsoft.AspNetCore.Components.ComponentBase
        {
            public string HeldValue { get; private set; } = "original";

            protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
            {
                builder.OpenComponent<RadzenTextBox>(0);
                builder.AddAttribute(1, nameof(RadzenTextBox.Value), HeldValue);
                builder.AddAttribute(2, nameof(RadzenTextBox.ValueChanged),
                    Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string>(this, _ => StateHasChanged()));
                builder.CloseComponent();
            }
        }
    }
}
