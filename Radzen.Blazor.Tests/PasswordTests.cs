using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class PasswordTests
    {
        [Fact]
        public void Password_Renders_CssClass()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPassword>();

            Assert.Contains(@$"rz-textbox", component.Markup);
        }

        [Fact]
        public void Password_Renders_ValueParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPassword>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Value, value));

            Assert.Contains(@$"value=""{value}""", component.Markup);
        }

        [Fact]
        public void Password_Renders_StyleParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPassword>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void Password_Renders_NameParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPassword>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Name, value));

            Assert.Contains(@$"name=""{value}""", component.Markup);
        }

        [Fact]
        public void Password_Renders_TabIndexParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPassword>();

            var value = 1;

            component.SetParametersAndRender(parameters => parameters.Add<int>(p => p.TabIndex, value));

            Assert.Contains(@$"tabindex=""{value}""", component.Markup);
        }

        [Fact]
        public void Password_Renders_PlaceholderParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPassword>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Placeholder, value));

            Assert.Contains(@$"placeholder=""{value}""", component.Markup);
        }

        [Fact]
        public void Password_Renders_DisabledParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPassword>();

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.Disabled, true));

            Assert.Contains(@$"disabled", component.Markup);
            Assert.Contains(@$"rz-state-disabled", component.Markup);
        }

        [Fact]
        public void Password_Renders_ReadOnlyParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPassword>();

            var value = true;

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.ReadOnly, value));

            Assert.Contains(@$"readonly", component.Markup);
        }

        [Fact]
        public void Password_Renders_AutoCompleteParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPassword>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("AutoComplete", false));

            Assert.Contains(@$"autocomplete=""new-password""", component.Markup);

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("AutoComplete", true));

            Assert.Contains(@$"autocomplete=""on""", component.Markup);

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autocomplete", "custom"));

            Assert.Contains(@$"autocomplete=""custom""", component.Markup);
        }

        [Fact]
        public void Password_Renders_TypedAutoCompleteParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPassword>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("AutoComplete", false));
            component.SetParametersAndRender(parameters => parameters.Add<AutoCompleteType>(p => p.AutoCompleteType, AutoCompleteType.On));

            Assert.Contains(@$"autocomplete=""new-password""", component.Markup);

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("AutoComplete", true));
            component.SetParametersAndRender(parameters => parameters.Add<AutoCompleteType>(p => p.AutoCompleteType, AutoCompleteType.Off));

            Assert.Contains(@$"autocomplete=""off""", component.Markup);

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("AutoComplete", true));
            component.SetParametersAndRender(parameters => parameters.Add<AutoCompleteType>(p => p.AutoCompleteType, AutoCompleteType.CurrentPassword));

            Assert.Contains(@$"autocomplete=""{AutoCompleteType.CurrentPassword.GetAutoCompleteValue()}""", component.Markup);

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("AutoComplete", true));
            component.SetParametersAndRender(parameters => parameters.Add<AutoCompleteType>(p => p.AutoCompleteType, AutoCompleteType.NewPassword));

            Assert.Contains(@$"autocomplete=""{AutoCompleteType.NewPassword.GetAutoCompleteValue()}""", component.Markup);
        }

        [Fact]
        public void Password_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPassword>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }

        [Fact]
        public void Password_Raises_ChangedEvent()
        {
            using var ctx = new TestContext();
            var hasRaised = false;
            var value = "Test";
            object newValue = null;

            var component = ctx.RenderComponent<RadzenPassword>(parameters =>
            {
                parameters.Add(p => p.Change, args => { hasRaised = true; newValue = args; });
                parameters.Add(p => p.Immediate, false);
            });

            var inputElement = component.Find("input");
            inputElement.Change(value);

            Assert.DoesNotContain("oninput", inputElement.ToMarkup());
            Assert.True(hasRaised);
            Assert.Equal(value, newValue);
        }

        [Fact]
        public void Password_Raises_InputEvent()
        {
            using var ctx = new TestContext();
            var hasRaised = false;
            var value = "Test";
            object newValue = null;

            var component = ctx.RenderComponent<RadzenPassword>(parameters =>
            {
                parameters.Add(p => p.Change, args => { hasRaised = true; newValue = args; });
                parameters.Add(p => p.Immediate, true);
            });

            var inputElement = component.Find("input");
            inputElement.Input(value);

            Assert.DoesNotContain("onchange", inputElement.ToMarkup());
            Assert.True(hasRaised);
            Assert.Equal(value, newValue);
        }

        [Fact]
        public void Password_Raises_ValueChangedEvent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPassword>();

            var raised = false;
            var value = "Test";
            object newValue = null;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ValueChanged, args => { raised = true; newValue = args; }));

            component.Find("input").Change(value);

            Assert.True(raised);
            Assert.True(object.Equals(value, newValue));
        }

        [Fact]
        public void Password_KeepsTypedValue_WhenUsedWithoutTwoWayBinding()
        {
            // Demo-style usage: no @bind-Value or ValueChanged. The user-typed value
            // must survive the post-handler @bind:get/:set sync, otherwise the input
            // clears itself on every blur.
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPassword>();

            component.Find("input").Change("user-typed");

            Assert.Equal("user-typed", component.Instance.Value);
            Assert.Equal("user-typed", component.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void Password_KeepsTypedValue_WhenBoundWithoutParameterReflow()
        {
            using var ctx = new TestContext();

            var boundValue = "original";
            var component = ctx.RenderComponent<RadzenPassword>(parameters =>
            {
                parameters.Add(p => p.Value, boundValue);
                parameters.Add(p => p.ValueChanged, v => boundValue = v);
            });

            component.Find("input").Change("user-typed");

            Assert.Equal("user-typed", boundValue);
            Assert.Equal("user-typed", component.Instance.Value);
            Assert.Equal("user-typed", component.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void Password_SyncsDomValue_WhenParentTransformsInput()
        {
            using var ctx = new TestContext();

            var wrapper = ctx.RenderComponent<RadzenPasswordTransformWrapper>();

            wrapper.Find("input").Change("user-typed");

            Assert.Equal("USER-TYPED", wrapper.FindComponent<RadzenPassword>().Instance.Value);
            Assert.Equal("USER-TYPED", wrapper.Find("input").GetAttribute("value"));
        }

        private sealed class RadzenPasswordTransformWrapper : Microsoft.AspNetCore.Components.ComponentBase
        {
            public string HeldValue { get; private set; } = "original";

            protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
            {
                builder.OpenComponent<RadzenPassword>(0);
                builder.AddAttribute(1, nameof(RadzenPassword.Value), HeldValue);
                builder.AddAttribute(2, nameof(RadzenPassword.ValueChanged),
                    Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string>(this, v => { HeldValue = v.ToUpperInvariant(); StateHasChanged(); }));
                builder.CloseComponent();
            }
        }
    }
}
