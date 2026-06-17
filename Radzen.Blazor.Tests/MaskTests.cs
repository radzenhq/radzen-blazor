using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class MaskTests
    {
        [Fact]
        public void Mask_Renders_CssClass()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMask>();

            Assert.Contains(@$"rz-textbox", component.Markup);
        }

        [Fact]
        public void Mask_Renders_ValueParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMask>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Value, value));

            Assert.Contains(@$"value=""{value}""", component.Markup);
        }

        [Fact]
        public void Mask_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMask>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void Mask_Renders_NameParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMask>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Name, value));

            Assert.Contains(@$"name=""{value}""", component.Markup);
        }

        [Fact]
        public void Mask_Renders_IdFromNameParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMask>();

            var value = "DateOfBirth";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Name, value));

            Assert.Contains(@$"id=""{value}""", component.Markup);
        }

        [Fact]
        public void Mask_Renders_TabIndexParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMask>();

            var value = 1;

            component.SetParametersAndRender(parameters => parameters.Add<int>(p => p.TabIndex, value));

            Assert.Contains(@$"tabindex=""{value}""", component.Markup);
        }

        [Fact]
        public void Mask_Renders_PlaceholderParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMask>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Placeholder, value));

            Assert.Contains(@$"placeholder=""{value}""", component.Markup);
        }

        [Fact]
        public void Mask_Renders_DisabledParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMask>();

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.Disabled, true));

            Assert.Contains(@$"disabled", component.Markup);
            Assert.Contains(@$"rz-state-disabled", component.Markup);
        }

        [Fact]
        public void Mask_Renders_ReadOnlyParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMask>();

            var value = true;

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.ReadOnly, value));

            Assert.Contains(@$"readonly", component.Markup);
        }

        [Fact]
        public void Mask_Renders_AutoCompleteParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMask>();

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
        public void Mask_Renders_TypedAutoCompleteParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMask>();

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
            component.SetParametersAndRender(parameters => parameters.Add<AutoCompleteType>(p => p.AutoCompleteType, AutoCompleteType.Email));

            Assert.Contains(@$"autocomplete=""{AutoCompleteType.Email.GetAutoCompleteValue()}""", component.Markup);
        }

        [Fact]
        public void Mask_Renders_MaxLengthParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMask>();

            var value = 10;

            component.SetParametersAndRender(parameters => parameters.Add<long?>(p => p.MaxLength, value));

            Assert.Contains(@$"maxlength=""{value}""", component.Markup);
        }

        [Fact]
        public void Mask_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMask>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }
        
        [Fact]
        public void Mask_Raises_ChangedEvent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMask>();

            var raised = false;
            var value = "Test";
            object newValue = null;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Change, args => { raised = true; newValue = args; }));

            component.Find("input").Change(value);

            Assert.True(raised);
            Assert.True(object.Equals(value, newValue));
        }

        [Fact]
        public void Mask_Raises_ValueChangedEvent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMask>();

            var raised = false;
            var value = "Test";
            object newValue = null;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ValueChanged, args => { raised = true; newValue = args; }));

            component.Find("input").Change(value);

            Assert.True(raised);
            Assert.True(object.Equals(value, newValue));
        }

        [Fact]
        public void Mask_KeepsTypedValue_WhenUsedWithoutTwoWayBinding()
        {
            // Demo-style usage: no @bind-Value or ValueChanged. The user-typed value
            // must survive the post-handler @bind:get/:set sync, otherwise the input
            // clears itself on every blur.
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenMask>();

            component.Find("input").Change("user-typed");

            Assert.Equal("user-typed", component.Instance.Value);
            Assert.Equal("user-typed", component.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void Mask_SyncsDomValue_WhenParentRejectsInput()
        {
            using var ctx = new TestContext();

            // Render through a wrapper that holds Value at a fixed string (parent rejects
            // the user-typed text by simply not updating its variable). Verifies that
            // when the bound Value parameter is re-rendered unchanged after the user
            // typed something different, Blazor still syncs the DOM input back to the
            // bound value — the @bind:get/:set contract.
            var wrapper = ctx.RenderComponent<RadzenMaskWrapper>();
            int beforeCount = wrapper.RenderCount;

            wrapper.Find("input").Change("user-typed");

            int afterCount = wrapper.RenderCount;
            System.Console.Error.WriteLine($"Wrapper render count: {beforeCount} -> {afterCount}");
            System.Console.Error.WriteLine($"Wrapper HeldValue: {wrapper.Instance.HeldValue}");
            System.Console.Error.WriteLine("Markup AFTER change:  " + wrapper.Markup);

            Assert.Equal("fixed", wrapper.Instance.HeldValue);
            Assert.Equal("fixed", wrapper.Find("input").GetAttribute("value"));
        }

        private sealed class RadzenMaskWrapper : Microsoft.AspNetCore.Components.ComponentBase
        {
            public string HeldValue { get; private set; } = "fixed";

            protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
            {
                builder.OpenComponent<RadzenMask>(0);
                builder.AddAttribute(1, nameof(RadzenMask.Value), HeldValue);
                builder.AddAttribute(2, nameof(RadzenMask.ValueChanged),
                    Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string>(this, _ => StateHasChanged()));
                builder.CloseComponent();
            }
        }
    }
}
