using Radzen;
using Radzen.Blazor.Rendering;
using System.Collections;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenAutoComplete component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenAutoComplete Data=@customers TextProperty="CompanyName" Change=@(args => Console.WriteLine($"Selected text: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenAutoComplete : DataBoundFormComponent<string>
    {
        /// <summary>
        /// Specifies additional custom attributes that will be rendered by the input.
        /// </summary>
        /// <value>The attributes.</value>
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenAutoComplete"/> is multiline.
        /// </summary>
        /// <value><c>true</c> if multiline; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Multiline { get; set; }

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
            else if (key == "Enter")
            {
                if (selectedIndex >= 0 && selectedIndex <= items.Count() - 1)
                {
                    await OnSelectItem(items.ElementAt(selectedIndex));
                    selectedIndex = -1;
                }
            }
            else if (key == "Escape" || key == "Tab")
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

            if (value.Length < MinLength)
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
                return Data != null && !string.IsNullOrEmpty(searchText) ? Data.AsQueryable() : null;
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
                    string filterCaseSensitivityOperator = FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? ".ToLower()" : "";

                    string textProperty = string.IsNullOrEmpty(TextProperty) ? string.Empty : $".{TextProperty}";

                    return Query.Where($"o=>o{textProperty}{filterCaseSensitivityOperator}.{Enum.GetName(typeof(StringFilterOperator), FilterOperator)}(@0)",
                        FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? searchText.ToLower() : searchText);
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
            Value = args.Value;

            await ValueChanged.InvokeAsync($"{Value}");
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);
        }

        async System.Threading.Tasks.Task SelectItem(object item)
        {
            if (!string.IsNullOrEmpty(TextProperty))
            {
                Value = PropertyAccess.GetItemOrValueFromProperty(item, TextProperty);
            }
            else
            {
                Value = item;
            }

            await ValueChanged.InvokeAsync($"{Value}");
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

            StateHasChanged();
        }

        ClassList InputClassList => ClassList.Create("rz-inputtext rz-autocomplete-input")
                                             .AddDisabled(Disabled);

        private string OpenScript()
        {
            if (Disabled)
            {
                return string.Empty;
            }

            return $"Radzen.openPopup(this.parentNode, '{PopupID}', true)";
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-autocomplete").ToString();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (IsJSRuntimeAvailable)
            {
                JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", PopupID);
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

            await base.SetParametersAsync(parameters);

            if (shouldClose && !firstRender)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", PopupID);
            }
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Sets the focus on the input element.
        /// </summary>
        public override async ValueTask FocusAsync()
        {
            await search.FocusAsync();
        }
#endif
    }
}
