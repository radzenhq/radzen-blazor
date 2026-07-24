using System.Collections;
using Bunit;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor.Tests
{
	public class AutoCompleteTests
	{
        [Fact]
        public void AutoComplete_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenAutoComplete>();

            Assert.Contains(@"rz-autocomplete", component.Markup);
        }

        [Fact]
        public void AutoComplete_Renders_InputElement()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenAutoComplete>();

            Assert.Contains("type=\"text\"", component.Markup);
            Assert.Contains("rz-inputtext", component.Markup);
        }

        [Fact]
        public void AutoComplete_Renders_Disabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains("disabled", component.Markup);
            Assert.Contains("rz-state-disabled", component.Markup);
        }

        [Fact]
        public void AutoComplete_Renders_Placeholder()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
            {
                parameters.Add(p => p.Placeholder, "Type to search...");
            });

            Assert.Contains("placeholder=\"Type to search...\"", component.Markup);
        }

        [Fact]
        public void AutoComplete_Renders_WithData()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Apple", "Banana", "Cherry" };

            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
            {
                parameters.Add(p => p.Data, data);
            });

            Assert.Contains("rz-autocomplete-panel", component.Markup);
        }

        [Fact]
        public void AutoComplete_Enum_Converts_To_Attr_Value()
        {
            // Options
            Assert.Equal("off", AutoCompleteType.Off.GetAutoCompleteValue());
            Assert.Equal("on", AutoCompleteType.On.GetAutoCompleteValue());
            Assert.Equal("name", AutoCompleteType.Name.GetAutoCompleteValue());
            Assert.Equal("honorific-prefix", AutoCompleteType.HonorificPrefix.GetAutoCompleteValue());
            Assert.Equal("given-name", AutoCompleteType.GivenName.GetAutoCompleteValue());
            Assert.Equal("additional-name", AutoCompleteType.AdditionalName.GetAutoCompleteValue());
            Assert.Equal("family-name", AutoCompleteType.FamilyName.GetAutoCompleteValue());
            Assert.Equal("honorific-suffix", AutoCompleteType.HonorificSuffix.GetAutoCompleteValue());
            Assert.Equal("nickname", AutoCompleteType.Nickname.GetAutoCompleteValue());
            Assert.Equal("email", AutoCompleteType.Email.GetAutoCompleteValue());
            Assert.Equal("username", AutoCompleteType.Username.GetAutoCompleteValue());
            Assert.Equal("new-password", AutoCompleteType.NewPassword.GetAutoCompleteValue());
            Assert.Equal("current-password", AutoCompleteType.CurrentPassword.GetAutoCompleteValue());
            Assert.Equal("one-time-code", AutoCompleteType.OneTimeCode.GetAutoCompleteValue());
            Assert.Equal("organization-title", AutoCompleteType.OrganizationTitle.GetAutoCompleteValue());
            Assert.Equal("organization", AutoCompleteType.Organization.GetAutoCompleteValue());
            Assert.Equal("street-address", AutoCompleteType.StreetAddress.GetAutoCompleteValue());
            Assert.Equal("address-line1", AutoCompleteType.AddressLine1.GetAutoCompleteValue());
            Assert.Equal("address-line2", AutoCompleteType.AddressLine2.GetAutoCompleteValue());
            Assert.Equal("address-line3", AutoCompleteType.AddressLine3.GetAutoCompleteValue());
            Assert.Equal("address-level1", AutoCompleteType.AddressLevel1.GetAutoCompleteValue());
            Assert.Equal("address-level2", AutoCompleteType.AddressLevel2.GetAutoCompleteValue());
            Assert.Equal("address-level3", AutoCompleteType.AddressLevel3.GetAutoCompleteValue());
            Assert.Equal("address-level4", AutoCompleteType.AddressLevel4.GetAutoCompleteValue());
            Assert.Equal("country", AutoCompleteType.Country.GetAutoCompleteValue());
            Assert.Equal("country-name", AutoCompleteType.CountryName.GetAutoCompleteValue());
            Assert.Equal("postal-code", AutoCompleteType.PostalCode.GetAutoCompleteValue());
            Assert.Equal("cc-name", AutoCompleteType.CcName.GetAutoCompleteValue());
            Assert.Equal("cc-given-name", AutoCompleteType.CcGivenName.GetAutoCompleteValue());
            Assert.Equal("cc-additional-name", AutoCompleteType.CcAdditionalName.GetAutoCompleteValue());
            Assert.Equal("cc-family-name", AutoCompleteType.CcFamilyName.GetAutoCompleteValue());
            Assert.Equal("cc-number", AutoCompleteType.CcNumber.GetAutoCompleteValue());
            Assert.Equal("cc-exp", AutoCompleteType.CcExp.GetAutoCompleteValue());
            Assert.Equal("cc-exp-month", AutoCompleteType.CcExpMonth.GetAutoCompleteValue());
            Assert.Equal("cc-exp-year", AutoCompleteType.CcExpYear.GetAutoCompleteValue());
            Assert.Equal("cc-csc", AutoCompleteType.CcCsc.GetAutoCompleteValue());
            Assert.Equal("cc-type", AutoCompleteType.CcType.GetAutoCompleteValue());
            Assert.Equal("transaction-currency", AutoCompleteType.TransactionCurrency.GetAutoCompleteValue());
            Assert.Equal("transaction-amount", AutoCompleteType.TransactionAmount.GetAutoCompleteValue());
            Assert.Equal("language", AutoCompleteType.Language.GetAutoCompleteValue());
            Assert.Equal("bday", AutoCompleteType.Bday.GetAutoCompleteValue());
            Assert.Equal("bday-day", AutoCompleteType.BdayDay.GetAutoCompleteValue());
            Assert.Equal("bday-month", AutoCompleteType.BdayMonth.GetAutoCompleteValue());
            Assert.Equal("bday-year", AutoCompleteType.BdayYear.GetAutoCompleteValue());
            Assert.Equal("sex", AutoCompleteType.Sex.GetAutoCompleteValue());
            Assert.Equal("tel", AutoCompleteType.Tel.GetAutoCompleteValue());
            Assert.Equal("tel-country-code", AutoCompleteType.TelCountryCode.GetAutoCompleteValue());
            Assert.Equal("tel-national", AutoCompleteType.TelNational.GetAutoCompleteValue());
            Assert.Equal("tel-area-code", AutoCompleteType.TelAreaCode.GetAutoCompleteValue());
            Assert.Equal("tel-local", AutoCompleteType.TelLocal.GetAutoCompleteValue());
            Assert.Equal("tel-extension", AutoCompleteType.TelExtension.GetAutoCompleteValue());
            Assert.Equal("impp", AutoCompleteType.Impp.GetAutoCompleteValue());
            Assert.Equal("url", AutoCompleteType.Url.GetAutoCompleteValue());
            Assert.Equal("photo", AutoCompleteType.Photo.GetAutoCompleteValue());
            // Synonyms
            Assert.Equal("address-level1", AutoCompleteType.State.GetAutoCompleteValue());
            Assert.Equal("address-level1", AutoCompleteType.Province.GetAutoCompleteValue());
            Assert.Equal("postal-code", AutoCompleteType.ZipCode.GetAutoCompleteValue());
            Assert.Equal("given-name", AutoCompleteType.FirstName.GetAutoCompleteValue());
            Assert.Equal("additional-name", AutoCompleteType.MiddleName.GetAutoCompleteValue());
            Assert.Equal("family-name", AutoCompleteType.LastName.GetAutoCompleteValue());
        }

        [Fact]
        public void AutoComplete_Renders_EmptyTemplate_WhenNoData()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string>();

            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
            {
                parameters
                    .Add(p => p.Data, data)
                    .Add(p => p.OpenOnFocus, true)
                    .Add(p => p.EmptyTemplate, b => b.AddMarkupContent(0, "<span class=\"empty-marker\">No results</span>"));
            });

            Assert.Contains("empty-marker", component.Markup);
            Assert.Contains("No results", component.Markup);
        }

        [Fact]
        public void AutoComplete_DoesNotRender_EmptyTemplate_WhenItemsPresent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Apple" };

            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
            {
                parameters
                    .Add(p => p.Data, data)
                    .Add(p => p.OpenOnFocus, true)
                    .Add(p => p.EmptyTemplate, b => b.AddMarkupContent(0, "<span class=\"empty-marker\">No results</span>"));
            });

            Assert.DoesNotContain("empty-marker", component.Markup);
            Assert.Contains("Apple", component.Markup);
        }

        [Fact]
        public void AutoComplete_Renders_LoadingTemplate_WhenIsLoading()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Apple", "Banana" };

            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
            {
                parameters
                    .Add(p => p.Data, data)
                    .Add(p => p.OpenOnFocus, true)
                    .Add(p => p.IsLoading, true)
                    .Add(p => p.LoadingTemplate, b => b.AddMarkupContent(0, "<span class=\"loading-marker\">Loading...</span>"));
            });

            Assert.Contains("loading-marker", component.Markup);
            Assert.DoesNotContain("Apple", component.Markup);
            Assert.DoesNotContain("Banana", component.Markup);
        }

        [Fact]
        public void AutoComplete_DoesNotRender_EmptyMarker_WhenEmptyTemplateNotSet()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string>();

            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
            {
                parameters
                    .Add(p => p.Data, data)
                    .Add(p => p.OpenOnFocus, true);
            });

            Assert.DoesNotContain("rz-state-disabled", component.Markup);
            Assert.DoesNotContain("role=\"presentation\"", component.Markup);
        }

        [Fact]
        public void AutoComplete_DoesNotRender_LoadingMarker_WhenLoadingTemplateNotSet()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Apple" };

            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
            {
                parameters
                    .Add(p => p.Data, data)
                    .Add(p => p.OpenOnFocus, true)
                    .Add(p => p.IsLoading, true);
            });

            Assert.DoesNotContain("role=\"presentation\"", component.Markup);
            Assert.Contains("Apple", component.Markup);
        }

        [Fact]
        public void AutoComplete_LoadingTemplate_HasPrecedence_OverEmptyTemplate()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string>();

            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
            {
                parameters
                    .Add(p => p.Data, data)
                    .Add(p => p.OpenOnFocus, true)
                    .Add(p => p.IsLoading, true)
                    .Add(p => p.LoadingTemplate, b => b.AddMarkupContent(0, "<span class=\"loading-marker\">Loading...</span>"))
                    .Add(p => p.EmptyTemplate, b => b.AddMarkupContent(0, "<span class=\"empty-marker\">No results</span>"));
            });

            Assert.Contains("loading-marker", component.Markup);
            Assert.DoesNotContain("empty-marker", component.Markup);
        }

        [Fact]
        public void AutoComplete_Filters_StringList()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Apple", "Banana", "Cherry" };

            var component = ctx.RenderComponent<AutoCompleteWithAccessibleView>(parameters =>
            {
                parameters
                    .Add(p => p.Data, data)
                    .Add(p => p.SearchText, "Ban")
                    .Add(p => p.OpenOnFocus, true);
            });
            
            Assert.Contains("Banana", component.Instance.CurrentView.OfType<string>());
            Assert.DoesNotContain("Apple", component.Instance.CurrentView.OfType<string>());
            Assert.DoesNotContain("Cherry", component.Instance.CurrentView.OfType<string>());
        }
        
        [Fact]
        public void AutoComplete_Renders_StableOptionIds()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Apple", "Banana", "Cherry" };

            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
            {
                parameters
                    .Add(p => p.Data, data)
                    .Add(p => p.OpenOnFocus, true);
            });

            var options = component.FindAll("li[role=\"option\"]");

            Assert.Equal(3, options.Count);

            for (var i = 0; i < options.Count; i++)
            {
                var id = options[i].Id;
                Assert.False(string.IsNullOrEmpty(id));
                Assert.EndsWith($"-list-{i}", id);
            }
        }

        [Fact]
        public void AutoComplete_DoesNotSet_AriaActiveDescendant_WhenNothingHighlighted()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Apple", "Banana", "Cherry" };

            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
            {
                parameters
                    .Add(p => p.Data, data)
                    .Add(p => p.OpenOnFocus, true);
            });

            var input = component.Find("input[role=\"combobox\"]");

            Assert.False(input.HasAttribute("aria-activedescendant"));
        }

        [Fact]
        public void AutoComplete_Sets_AriaActiveDescendant_OnArrowDown()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Apple", "Banana", "Cherry" };

            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
            {
                parameters
                    .Add(p => p.Data, data)
                    .Add(p => p.OpenOnFocus, true);
            });

            var input = component.Find("input[role=\"combobox\"]");
            input.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowDown" });

            input = component.Find("input[role=\"combobox\"]");
            var activeId = input.GetAttribute("aria-activedescendant");

            Assert.False(string.IsNullOrEmpty(activeId));

            var activeOption = component.FindAll("li[role=\"option\"]").Single(o => o.Id == activeId);

            Assert.NotNull(activeOption);
        }

        [Fact]
        public void AutoComplete_Clears_AriaActiveDescendant_OnEscape()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Apple", "Banana", "Cherry" };

            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
            {
                parameters
                    .Add(p => p.Data, data)
                    .Add(p => p.OpenOnFocus, true);
            });

            var input = component.Find("input[role=\"combobox\"]");
            input.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowDown" });

            input = component.Find("input[role=\"combobox\"]");
            Assert.True(input.HasAttribute("aria-activedescendant"));

            input.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "Escape" });

            input = component.Find("input[role=\"combobox\"]");
            Assert.False(input.HasAttribute("aria-activedescendant"));
        }

        [Fact]
        public async Task AutoComplete_AriaExpanded_TracksPopupState()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Apple", "Banana", "Cherry" };

            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
            {
                parameters
                    .Add(p => p.Data, data)
                    .Add(p => p.OpenOnFocus, true);
            });

            var input = component.Find("input[role=\"combobox\"]");

            Assert.Equal("false", input.GetAttribute("aria-expanded"));

            await component.InvokeAsync(() => component.Instance.OnPopupOpen());

            input = component.Find("input[role=\"combobox\"]");
            Assert.Equal("true", input.GetAttribute("aria-expanded"));

            await component.InvokeAsync(() => component.Instance.OnPopupClose());

            input = component.Find("input[role=\"combobox\"]");
            Assert.Equal("false", input.GetAttribute("aria-expanded"));
        }

        [Fact]
        public async Task AutoComplete_AriaExpanded_IsFalse_AfterEscape()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var data = new List<string> { "Apple", "Banana", "Cherry" };

            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
            {
                parameters
                    .Add(p => p.Data, data)
                    .Add(p => p.OpenOnFocus, true);
            });

            await component.InvokeAsync(() => component.Instance.OnPopupOpen());

            var input = component.Find("input[role=\"combobox\"]");
            Assert.Equal("true", input.GetAttribute("aria-expanded"));

            input.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "Escape" });

            input = component.Find("input[role=\"combobox\"]");
            Assert.Equal("false", input.GetAttribute("aria-expanded"));
        }

        [Fact]
        public void AutoComplete_Sets_AriaSelected_OnHighlightedOption()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.Setup<int>("Radzen.focusListItem", _ => true).SetResult(1);
            var data = new List<string> { "Apple", "Banana", "Cherry" };

            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
            {
                parameters
                    .Add(p => p.Data, data)
                    .Add(p => p.OpenOnFocus, true);
            });

            var input = component.Find("input[role=\"combobox\"]");
            input.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowDown" });

            var options = component.FindAll("li[role=\"option\"]");

            Assert.Equal("false", options[0].GetAttribute("aria-selected"));
            Assert.Equal("true", options[1].GetAttribute("aria-selected"));
            Assert.Equal("false", options[2].GetAttribute("aria-selected"));
        }

        private sealed class AutoCompleteWithAccessibleView : RadzenAutoComplete
        {
            public IEnumerable CurrentView => View;
        }

        [Fact]
        public void AutoComplete_KeepsTypedValue_WhenUsedWithoutTwoWayBinding()
        {
            // Demo-style usage: no @bind-Value or ValueChanged. The user-typed value
            // must survive the post-handler @bind:get/:set sync, otherwise the input
            // clears itself on every blur.
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenAutoComplete>();

            component.Find("input").Change("user-typed");

            Assert.Equal("user-typed", component.Instance.Value);
            Assert.Equal("user-typed", component.Find("input").GetAttribute("value"));
        }

        [Fact]
        public void AutoComplete_KeepsTypedValue_WhenBoundWithoutParameterReflow()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var boundValue = "original";
            var component = ctx.RenderComponent<RadzenAutoComplete>(parameters =>
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
        public void AutoComplete_SyncsDomValue_WhenParentTransformsInput()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var wrapper = ctx.RenderComponent<RadzenAutoCompleteTransformWrapper>();

            wrapper.Find("input").Change("user-typed");

            Assert.Equal("USER-TYPED", wrapper.FindComponent<RadzenAutoComplete>().Instance.Value);
            Assert.Equal("USER-TYPED", wrapper.Find("input").GetAttribute("value"));
        }

        private sealed class RadzenAutoCompleteTransformWrapper : Microsoft.AspNetCore.Components.ComponentBase
        {
            public string HeldValue { get; private set; } = "original";

            protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
            {
                builder.OpenComponent<RadzenAutoComplete>(0);
                builder.AddAttribute(1, nameof(RadzenAutoComplete.Value), HeldValue);
                builder.AddAttribute(2, nameof(RadzenAutoComplete.ValueChanged),
                    Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string>(this, v => { HeldValue = v.ToUpperInvariant(); StateHasChanged(); }));
                builder.CloseComponent();
            }
        }
    }
}
