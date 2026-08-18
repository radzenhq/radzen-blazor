# RadzenChipList API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowDelete | `bool` | Gets or sets whether chips can be removed. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChipStyle | `BadgeStyle` | Gets or sets the default style applied to chips. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Data | `IEnumerable?` | Gets or sets the data source used to generate chip items. |
| Disabled | `bool` | Gets or sets a value indicating whether this is disabled. |
| DisabledProperty | `string?` | Gets or sets the name of the data property used for item disabled state. |
| FieldIdentifier | `FieldIdentifier` | Gets the field identifier. |
| Items | `RenderFragment?` | Gets or sets declarative chip items. |
| Multiple | `bool` | Gets or sets whether multiple items can be selected. |
| Name | `string?` | Gets or sets the unique name identifier for this form component. Used for validation association (linking with validators and labels) and for identifying the field in form submission. This name should be unique within the form and match the Component property of associated validators/labels. |
| Orientation | `Orientation` | Gets or sets the chip list orientation. |
| Placeholder | `string?` | Gets or sets the placeholder. |
| RemoveChipTitle | `string` | Gets or sets the close button accessible title. |
| Shade | `Shade` | Gets or sets the default shade applied to chips. |
| Size | `ChipSize` | Gets or sets the default chip size. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the tab order index for keyboard navigation. Controls the order in which fields receive focus when the user presses the Tab key. Lower values receive focus first. Use -1 to exclude from tab navigation. |
| Template | `RenderFragment<RadzenChipItem>?` | Gets or sets a template for custom chip content. |
| TextProperty | `string?` | Gets or sets the name of the data property used as chip text. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Value | `T?` | Gets or sets the value. |
| ValueExpression | `Expression<Func<T>>?` | Gets or sets the value expression. |
| ValueProperty | `string?` | Gets or sets the name of the data property used as chip value. |
| Variant | `Variant` | Gets or sets the default variant applied to chips. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |
| Wrap | `FlexWrap` | Gets or sets the wrapping behavior. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<T>` | Gets or sets the change. |
| ChipRemoved | `EventCallback<object?>` | Gets or sets the callback invoked when a chip remove action is requested. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| ValueChanged | `EventCallback<T>` | Gets or sets the value changed. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| AddItem(RadzenChipItem item) | `void` | Adds the specified item to the chip list. |
| Refresh() | `void` | Refreshes this instance. |
| RemoveItem(RadzenChipItem item) | `void` | Removes the specified item from the chip list. |

