using Bunit;
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
    }
}
