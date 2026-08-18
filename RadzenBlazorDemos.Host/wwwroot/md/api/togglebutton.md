# RadzenToggleButton API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AriaControls | `string?` | Gets or sets the aria-controls attribute. |
| AriaExpanded | `string?` | Gets or sets the aria-expanded attribute. |
| AriaHasPopup | `string?` | Gets or sets the aria-haspopup attribute. |
| AriaLabel | `string?` | Gets or sets the aria-label attribute. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| BusyText | `string` | Gets or sets the text displayed when the button is in a busy state ( is true). If not set, the button will show a loading indicator without text. |
| ButtonStyle | `ButtonStyle` | Gets or sets the semantic color style of the button. Determines the button's color scheme based on its purpose (e.g., Primary for main actions, Danger for destructive actions). |
| ButtonType | `ButtonType` | Gets or sets the HTML button type attribute. Use for form submissions or for regular clickable buttons. |
| ChildContent | `RenderFragment?` | Gets or sets the custom child content to be rendered inside the button. When set, this content will be displayed instead of the , , or . |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Disabled | `bool` | Gets or sets whether the button is disabled and cannot be clicked. When true, the button will have a disabled appearance and will not respond to user interactions. |
| FieldIdentifier | `FieldIdentifier` | Gets the field identifier. |
| Icon | `string?` | Gets or sets the Material icon name to be displayed in the button. Use Material Symbols icon names (e.g., "save", "delete", "add"). The icon will be rendered using the rzi icon font. |
| IconColor | `string?` | Gets or sets a custom color for the icon. This overrides the default icon color determined by the button's and . Supports any valid CSS color value (e.g., "#FF0000", "rgb(255, 0, 0)", "var(--my-color)"). |
| Image | `string?` | Gets or sets the URL of an image to be displayed in the button. The image will be rendered as an img element. For icon fonts, use the property instead. |
| ImageAlternateText | `string` | Gets or sets the alternate text for the button's image. This is used as the alt attribute when an is specified, improving accessibility. |
| InputAttributes | `IReadOnlyDictionary<string, object>?` | Specifies additional custom attributes that will be rendered by the input. |
| IsBusy | `bool` | Gets or sets whether the button is in a busy/loading state. When true, the button displays a loading indicator, shows the , and becomes disabled. This is useful for indicating asynchronous operations are in progress. |
| Name | `string?` | Gets or sets the name. |
| Placeholder | `string?` | Gets or sets the placeholder. |
| Shade | `Shade` | Gets or sets the color intensity shade for the button. Works in combination with to adjust the color darkness/lightness. |
| Size | `ButtonSize` | Gets or sets the button size. Controls the padding, font size, and overall dimensions of the button. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the tab index for keyboard navigation. Controls the order in which the button receives focus when the user presses the Tab key. |
| Text | `string` | Gets or sets the text label displayed on the button. If both and are set, both will be displayed. |
| ToggleButtonStyle | `ButtonStyle` | Gets or sets the ToggleButton style. |
| ToggleIcon | `string?` | Gets or sets the toggle icon. |
| ToggleShade | `Shade` | Gets or sets the ToggleButton shade. |
| ToggleVariant | `Variant?` | Gets or sets the variant used when the button is toggled. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Value | `bool` | Gets or sets the value. |
| ValueExpression | `Expression<Func<bool>>?` | Gets or sets the value expression. |
| Variant | `Variant` | Gets or sets the design variant that controls the button's visual appearance. Options include Filled (solid background), Flat (subtle background), Outlined (border only), and Text (minimal styling). |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<bool>` | Gets or sets the change. |
| Click | `EventCallback<MouseEventArgs>` | Gets or sets the callback invoked when the button is clicked. This event will not fire if the button is or . |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| ValueChanged | `EventCallback<bool>` | Gets or sets the value changed. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| FocusAsync() | `ValueTask` |  |
| GetValue() | `object` | Gets the value. |

