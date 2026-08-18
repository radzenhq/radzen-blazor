# RadzenDropDown API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowClear | `bool` | Gets or sets a value indicating whether the user can clear the value. Set to false by default. |
| AllowFiltering | `bool` | Gets or sets a value indicating whether filtering is allowed. Set to false by default. |
| AllowSelectAll | `bool` | Gets or sets a value indicating whether the user can select all values in multiple selection. Set to true by default. |
| AllowVirtualization | `bool` | Specifies wether virtualization is enabled. Set to false by default. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Chips | `bool` | Gets or sets whether selected items should be displayed as removable chips in the input area. When enabled in multiple selection mode, each selected item appears as a chip with an X button for quick removal. Requires to be set to true. |
| ClearAriaLabel | `string` | Gets or sets the clear button aria label text. |
| ClearSearchAfterSelection | `bool` | Gets or sets whether the filter search text should be cleared after an item is selected. When true, selecting an item will reset the filter, showing all items again on the next open. |
| Count | `int` | Specifies the total number of items in the data source. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Data | `IEnumerable?` | Gets or sets the data. |
| Disabled | `bool` | Gets or sets a value indicating whether this is disabled. |
| DisabledProperty | `string?` | Gets or sets the disabled property. |
| EmptyAriaLabel | `string` | Gets or sets the empty value aria label text. |
| EmptyTemplate | `RenderFragment?` | Gets or sets the template displayed when the dropdown data source is empty or no items match the filter. Use this to show a custom "No items found" or "Empty list" message. |
| FieldIdentifier | `FieldIdentifier` | Gets the field identifier. |
| FilterAsYouType | `bool` | Gets or sets a value indicating whether filtering is allowed as you type. Set to true by default. |
| FilterAutoCompleteType | `AutoCompleteType` | Gets or sets the HTML autocomplete attribute value for the filter search input. Controls whether the browser should provide autocomplete suggestions for the filter field. |
| FilterCaseSensitivity | `FilterCaseSensitivity` | Gets or sets the filter case sensitivity. |
| FilterDelay | `int` | Gets or sets the filter delay. |
| FilterOperator | `StringFilterOperator` | Gets or sets the filter operator. |
| FilterPlaceholder | `string` | Gets or sets the placeholder text displayed in the filter search box within the dropdown popup. This helps users understand they can filter the list by typing. |
| FooterTemplate | `RenderFragment?` | Gets or sets the footer template. |
| HeaderTemplate | `RenderFragment?` | Gets or sets the header template. |
| InputAttributes | `IReadOnlyDictionary<string, object>?` | Gets or sets additional HTML attributes to be applied to the underlying input element. This allows passing custom attributes like data-* attributes, aria-* attributes, or other HTML attributes directly to the input. |
| InputSize | `InputSize` | Gets or sets the size of the component. |
| ItemComparer | `IEqualityComparer<object>?` | For lists of objects, an IEqualityComparer to control how selected items are determined |
| ItemRender | `Action<DropDownItemRenderEventArgs<TValue>>?` | Gets or sets a callback invoked when rendering each dropdown item. Use this to customize item attributes, such as adding CSS classes or data attributes based on item properties. |
| MaxSelectedLabels | `int` | Gets or sets the maximum number of selected item labels to display in the input before showing a count summary. When multiple selection is enabled and more items are selected than this value, the input will show "N items selected" instead of listing all labels. Only applicable when is true. |
| Multiple | `bool` | Gets or sets a value indicating whether this is multiple. |
| Name | `string?` | Gets or sets the name. |
| OpenOnFocus | `bool` | Gets or sets whether the dropdown popup should automatically open when the input receives focus. Useful for improving user experience by reducing clicks needed to interact with the dropdown. |
| OpenPopupKey | `string` | Gets or sets the keyboard key that triggers opening the popup when is enabled. Default is "Enter". |
| PageSize | `int` | Specifies the default page size. Set to 5 by default. |
| Placeholder | `string?` | Gets or sets the placeholder. |
| PopupStyle | `string` | Gets or sets the CSS style applied to the dropdown popup container. Use this to control the popup dimensions, especially max-height to limit scrollable area. |
| ReadOnly | `bool` | Gets or sets whether the dropdown is read-only and cannot be changed by user interaction. When true, the dropdown displays the selected value but prevents changing the selection. |
| RemoveChipTitle | `string` | Gets or sets the remove chip button title. |
| ResetSelectedIndexOnFilter | `bool` | Gets or sets a value indicating the selected index should reset to the top item when filtering, resulting in a down arrow action will start moving from the top. |
| SearchAriaLabel | `string` | Gets or sets the search aria label text. |
| SearchText | `string?` | Gets or sets the search text |
| SelectAllText | `string` | Gets or sets the select all text. |
| SelectedItem | `object?` | Gets or sets the selected item. |
| SelectedItemsText | `string` | Gets or sets the selected items text. |
| Separator | `string` | Gets or sets the item separator for Multiple dropdown. |
| ShowValueTemplateOnEmpty | `bool` | Gets or sets whether is rendered even when there is no selected item. When true, the template is invoked with a null context so it can render an editor (e.g. a text box) for an empty value. Templates must handle a null context. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the index of the tab. |
| Template | `RenderFragment<dynamic>?` | Gets or sets the template. |
| TextProperty | `string?` | Gets or sets the text property. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Value | `T?` | Gets or sets the value. |
| ValueExpression | `Expression<Func<T>>?` | Gets or sets the value expression. |
| ValueProperty | `string?` | Gets or sets the value property. |
| ValueTemplate | `RenderFragment<dynamic>?` | Gets or sets the template used to render the currently selected value in the dropdown input. This allows custom formatting or layout for the displayed selection. The template receives the selected item as context. |
| VirtualizationOverscanCount | `int` | Gets or sets a value that determines how many additional items will be rendered before and after the visible region. This help to reduce the frequency of rendering during scrolling. However, higher values mean that more elements will be present in the page. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<object>` | Gets or sets the change. |
| Close | `EventCallback` | Callback for when a dropdown is closed. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| LoadData | `EventCallback<Radzen.LoadDataArgs>` | Gets or sets the load data. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| Open | `EventCallback` | Callback for when a dropdown is opened. |
| SearchTextChanged | `EventCallback<string>` | Gets or sets the search text changed. |
| SelectedItemChanged | `EventCallback<object>` | Gets or sets the selected item changed. |
| ValueChanged | `EventCallback<T>` | Gets or sets the value changed. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| ClosePopup() | `Task` | Closes the dropdown popup programmatically. |
| OnClose() | `Task` | Called when popup is closed. |
| OpenPopup() | `Task` | Opens the popup. |
| TogglePopup() | `Task` | Toggles the dropdown popup, opening it if it is closed and closing it if it is open. |

