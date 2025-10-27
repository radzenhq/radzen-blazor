using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// An autocomplete text input component that provides real-time suggestions as users type based on a data source.
    /// RadzenAutoComplete combines a text input with a suggestion dropdown that filters and displays matching items, enabling quick selection without typing complete values.
    /// Features configurable filter operators (Contains, StartsWith, etc.) and case sensitivity, binding to any IEnumerable data source with TextProperty to specify display field,
    /// MinLength to require typing before showing suggestions, FilterDelay for debouncing, custom templates for rendering suggestion items,
    /// LoadData event for on-demand server-side filtering, textarea-style multiline input support, and option to show all items when field gains focus.
    /// Unlike dropdown, allows free-text entry and suggests matching items. The Value is the entered text, while SelectedItem provides access to the selected data object.
    /// </summary>
    /// <example>
    /// Basic autocomplete:
    /// <code>
    /// &lt;RadzenAutoComplete @bind-Value=@customerName Data=@customers TextProperty="CompanyName" /&gt;
    /// </code>
    /// Autocomplete with custom filtering and delay:
    /// <code>
    /// &lt;RadzenAutoComplete @bind-Value=@search Data=@products TextProperty="ProductName"
    ///                      FilterOperator="StringFilterOperator.Contains" FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
    ///                      MinLength="2" FilterDelay="300" Placeholder="Type to search products..." /&gt;
    /// </code>
    /// </example>
    public partial class RadzenAutoComplete : DataBoundFormComponent<string>
    {
        object selectedItem = null;

        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        /// <value>The selected item.</value>
        [Parameter]
        public object SelectedItem
        {
            get
            {
                return selectedItem;
            }
            set
            {
                if (selectedItem != value)
                {
                    selectedItem = object.Equals(value, "null") ? null : value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected item changed.
        /// </summary>
        /// <value>The selected item changed.</value>
        [Parameter]
        public EventCallback<object> SelectedItemChanged { get; set; }

        /// <summary>
        /// Specifies additional custom attributes that will be rendered by the input.
        /// </summary>
        /// <value>The attributes.</value>
        [Parameter]
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenAutoComplete"/> is multiline.
        /// </summary>
        /// <value><c>true</c> if multiline; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Multiline { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether popup should open on focus. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if popup should open on focus; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool OpenOnFocus { get; set; }

        /// <summary>
        /// Gets or sets the Popup height.
        /// </summary>
        /// <value>The number Popup height.</value>
        [Parameter]
        public string PopupStyle { get; set; } = "display:none; transform: none; box-sizing: border-box; max-height: 200px;";

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment<dynamic> Template { get; set; }

        /// <summary>
        /// Gets or sets the minimum length.
        /// </summary>
        /// <value>The minimum length.</value>
        [Parameter]
        public int MinLength { get; set; } = 1;

        /// <summary>
        /// Gets or sets the filter delay.
        /// </summary>
        /// <value>The filter delay.</value>
        [Parameter]
        public int FilterDelay { get; set; } = 500;

        /// <summary>
        /// Gets or sets the underlying input type. This does not apply when <see cref="Multiline"/> is <c>true</c>.
        /// </summary>
        /// <value>The input type.</value>
        [Parameter]
        public string InputType { get; set; } = "text";

        /// <summary>
        /// Gets or sets the underlying max length.
        /// </summary>
        /// <value>The max length value.</value>
        [Parameter]
        public long? MaxLength { get; set; }

        /// <summary>
        /// Gets search input reference.
        /// </summary>
        protected ElementReference search;

        /// <summary>
        /// Gets list element reference.
        /// </summary>
        protected ElementReference list;

        string customSearchText;
        int selectedIndex = -1;

        /// <summary>
        /// Handles the <see cref="E:FilterKeyPress" /> event.
        /// </summary>
        /// <param name="args">The <see cref="KeyboardEventArgs"/> instance containing the event data.</param>
        protected async Task OnFilterKeyPress(KeyboardEventArgs args)
        {
            var items = (LoadData.HasDelegate ? Data != null ? Data : Enumerable.Empty<object>() : (View != null ? View : Enumerable.Empty<object>())).OfType<object>();

            var key = args.Code != null ? args.Code : args.Key;

            if (key == "ArrowDown" || key == "ArrowUp")
            {
                try
                {
                    selectedIndex = await JSRuntime.InvokeAsync<int>("Radzen.focusListItem", search, list, key == "ArrowDown", selectedIndex);
                }
                catch (Exception)
                {
                    //
                }
            }
            else if (key == "Enter" || key == "NumpadEnter" || key == "Tab")
            {
                if (selectedIndex >= 0 && selectedIndex <= items.Count() - 1)
                {
                    await OnSelectItem(items.ElementAt(selectedIndex));
                    selectedIndex = -1;
                }

                if (key == "Tab")
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
                }
            }
            else if (key == "Escape")
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
            }
            else
            {
                selectedIndex = -1;

                Debounce(DebounceFilter, FilterDelay);
            }
        }

        async Task DebounceFilter()
        {
            var value = await JSRuntime.InvokeAsync<string>("Radzen.getInputValue", search);

            value = $"{value}";
            
            if (value.Length < MinLength && !OpenOnFocus)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
                return;
            }

            if (!LoadData.HasDelegate)
            {
                searchText = value;
                await InvokeAsync(() => { StateHasChanged(); });
            }
            else
            {
                customSearchText = value;
                await InvokeAsync(() => { LoadData.InvokeAsync(new Radzen.LoadDataArgs() { Filter = customSearchText }); });
            }
        }

        private string PopupID
        {
            get
            {
                return $"popup{UniqueID}";
            }
        }

        private async Task OnSelectItem(object item)
        {
            await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);

            await SelectItem(item);
        }

        /// <summary>
        /// Gets the IQueryable.
        /// </summary>
        /// <value>The IQueryable.</value>
        protected override IQueryable Query
        {
            get
            {
                return Data != null && (OpenOnFocus || !string.IsNullOrEmpty(searchText)) ? Data.AsQueryable() : null;
            }
        }

        /// <summary>
        /// Gets the view - the Query with filtering applied.
        /// </summary>
        /// <value>The view.</value>
        protected override IEnumerable View
        {
            get
            {
                if (Query != null)
                {
                    return Query.Where(TextProperty, searchText, FilterOperator, FilterCaseSensitivity);
                }

                return null;
            }
        }

        /// <summary>
        /// Handles the <see cref="E:Change" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        protected async System.Threading.Tasks.Task OnChange(ChangeEventArgs args)
        {
            Value = args.Value?.ToString();

            await ValueChanged.InvokeAsync($"{Value}");
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

            await SelectedItemChanged.InvokeAsync(null);
        }

        async System.Threading.Tasks.Task SelectItem(object item)
        {
            if (!string.IsNullOrEmpty(TextProperty))
            {
                Value = PropertyAccess.GetItemOrValueFromProperty(item, TextProperty)?.ToString();
            }
            else
            {
                Value = item?.ToString();
            }

            await ValueChanged.InvokeAsync($"{Value}");
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

            await SelectedItemChanged.InvokeAsync(item);

            StateHasChanged();
        }

        string InputClass => ClassList.Create("rz-inputtext rz-autocomplete-input")
                                      .AddDisabled(Disabled)
                                      .ToString();

        private string OpenScript()
        {
            if (Disabled)
            {
                return string.Empty;
            }

            return $"Radzen.openPopup(this.parentNode, '{PopupID}', true)";
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass() => GetClassList("rz-autocomplete").ToString();

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (IsJSRuntimeAvailable)
            {
                JSRuntime.InvokeVoid("Radzen.destroyPopup", PopupID);
            }
        }

        private bool firstRender = true;

        /// <summary>
        /// Called on after render asynchronous.
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> is first render.</param>
        /// <returns>Task.</returns>
        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            this.firstRender = firstRender;

            return base.OnAfterRenderAsync(firstRender);
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var shouldClose = false;

            if (parameters.DidParameterChange(nameof(Visible), Visible))
            {
                var visible = parameters.GetValueOrDefault<bool>(nameof(Visible));
                shouldClose = !visible;
            }

            if (parameters.DidParameterChange(nameof(SelectedItem), SelectedItem))
            {
                var item = parameters.GetValueOrDefault<object>(nameof(SelectedItem));
                if (item != null)
                {
                    await SelectItem(item);
                }
            }

            await base.SetParametersAsync(parameters);

            if (parameters.DidParameterChange(nameof(Value), Value))
            {
                Value = parameters.GetValueOrDefault<string>(nameof(Value));
            }

            if (shouldClose && !firstRender)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", PopupID);
            }
        }

        /// <summary>
        /// Sets the focus on the input element.
        /// </summary>
        public override async ValueTask FocusAsync()
        {
            await search.FocusAsync();
        }
    }
}