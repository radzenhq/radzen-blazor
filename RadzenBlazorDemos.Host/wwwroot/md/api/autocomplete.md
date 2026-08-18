# RadzenAutoComplete API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Data | `IEnumerable?` | Gets or sets the data. |
| Disabled | `bool` | Gets or sets a value indicating whether this is disabled. |
| EmptyTemplate | `RenderFragment?` | Gets or sets the empty template shown when there are no suggestions to display. |
| FieldIdentifier | `FieldIdentifier` | Gets the field identifier. |
| FilterCaseSensitivity | `FilterCaseSensitivity` | Gets or sets the filter case sensitivity. |
| FilterDelay | `int` | Gets or sets the filter delay. |
| FilterOperator | `StringFilterOperator` | Gets or sets the filter operator. |
| InputAttributes | `IReadOnlyDictionary<string, object>?` | Specifies additional custom attributes that will be rendered by the input. |
| InputSize | `InputSize` | Gets or sets the size of the component. |
| InputType | `string` | Gets or sets the underlying input type. This does not apply when is true. |
| IsLoading | `bool` |  |
| LoadingTemplate | `RenderFragment?` |  |
| MaxLength | `long?` | Gets or sets the underlying max length. |
| MinLength | `int` | Gets or sets the minimum length. |
| Multiline | `bool` | Gets or sets a value indicating whether this is multiline. |
| Name | `string?` | Gets or sets the name. |
| OpenOnFocus | `bool` | Gets or sets a value indicating whether popup should open on focus. Set to false by default. |
| Placeholder | `string?` | Gets or sets the placeholder. |
| PopupStyle | `string` | Gets or sets the Popup height. |
| SearchText | `string?` | Gets or sets the search text |
| SelectedItem | `object?` | Gets or sets the selected item. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the index of the tab. |
| Template | `RenderFragment<dynamic>?` | Gets or sets the template. |
| TextProperty | `string?` | Gets or sets the text property. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Value | `T?` | Gets or sets the value. |
| ValueExpression | `Expression<Func<T>>?` | Gets or sets the value expression. |
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

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| OnPopupClose() | `Task` | Invoked from client-side code when the suggestion popup closes. |
| OnPopupOpen() | `Task` | Invoked from client-side code when the suggestion popup opens. |

