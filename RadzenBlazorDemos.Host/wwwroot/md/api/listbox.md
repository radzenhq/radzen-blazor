# RadzenListBox API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowClear | `bool` | Gets or sets a value indicating whether the user can clear the value. Set to false by default. |
| AllowFiltering | `bool` | Gets or sets a value indicating whether filtering is allowed. Set to false by default. |
| AllowSelectAll | `bool` | Gets or sets a value indicating whether the user can select all values in multiple selection. Set to true by default. |
| AllowVirtualization | `bool` | Specifies wether virtualization is enabled. Set to false by default. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ClearAriaLabel | `string` | Gets or sets the clear button aria label text. |
| Count | `int` | Specifies the total number of items in the data source. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Data | `IEnumerable?` | Gets or sets the data. |
| Disabled | `bool` | Gets or sets a value indicating whether this is disabled. |
| DisabledProperty | `string?` | Gets or sets the disabled property. |
| EmptyAriaLabel | `string` | Gets or sets the empty value aria label text. |
| EmptyTemplate | `RenderFragment?` | Gets or sets the empty template shown when Data is empty. |
| EmptyText | `string` | Gets or sets the empty text shown when Data is empty. |
| FieldIdentifier | `FieldIdentifier` | Gets the field identifier. |
| FilterAsYouType | `bool` | Gets or sets a value indicating whether filtering is allowed as you type. Set to true by default. |
| FilterCaseSensitivity | `FilterCaseSensitivity` | Gets or sets the filter case sensitivity. |
| FilterDelay | `int` | Gets or sets the filter delay. |
| FilterOperator | `StringFilterOperator` | Gets or sets the filter operator. |
| HeaderTemplate | `RenderFragment?` | Gets or sets the header template. |
| InputAttributes | `IReadOnlyDictionary<string, object>?` | Specifies additional custom attributes that will be rendered by the input. |
| InputSize | `InputSize` | Gets or sets the input size. |
| ItemComparer | `IEqualityComparer<object>?` | For lists of objects, an IEqualityComparer to control how selected items are determined |
| ItemRender | `Action<ListBoxItemRenderEventArgs<TValue>>?` | Gets or sets the row render callback. Use it to set row attributes. |
| Multiple | `bool` | Gets or sets a value indicating whether this is multiple. |
| Name | `string?` | Gets or sets the name. |
| PageSize | `int` | Specifies the default page size. Set to 5 by default. |
| Placeholder | `string?` | Gets or sets the placeholder. |
| ReadOnly | `bool` | Gets or sets a value indicating whether is read only. |
| RemoveChipTitle | `string` | Gets or sets the remove chip button title. |
| ResetSelectedIndexOnFilter | `bool` | Gets or sets a value indicating the selected index should reset to the top item when filtering, resulting in a down arrow action will start moving from the top. |
| SearchAriaLabel | `string` | Gets or sets the search aria label text. |
| SearchText | `string?` | Gets or sets the search text |
| SelectAllText | `string?` | Gets or sets the select all text. |
| SelectedItem | `object?` | Gets or sets the selected item. |
| Separator | `string` | Gets or sets the item separator for Multiple dropdown. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the index of the tab. |
| Template | `RenderFragment<dynamic>?` | Gets or sets the template. |
| TextProperty | `string?` | Gets or sets the text property. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Value | `T?` | Gets or sets the value. |
| ValueExpression | `Expression<Func<T>>?` | Gets or sets the value expression. |
| ValueProperty | `string?` | Gets or sets the value property. |
| VirtualizationOverscanCount | `int` | Gets or sets a value that determines how many additional items will be rendered before and after the visible region. This help to reduce the frequency of rendering during scrolling. However, higher values mean that more elements will be present in the page. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<object>` | Gets or sets the change. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| LoadData | `EventCallback<Radzen.LoadDataArgs>` | Gets or sets the load data. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| SearchTextChanged | `EventCallback<string>` | Gets or sets the search text changed. |
| SelectedItemChanged | `EventCallback<object>` | Gets or sets the selected item changed. |
| ValueChanged | `EventCallback<T>` | Gets or sets the value changed. |

