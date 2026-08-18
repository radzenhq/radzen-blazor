# RadzenCarousel API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowNavigation | `bool` | Gets or sets a value indicating whether previous/next navigation is allowed. Set to true by default. |
| AllowPaging | `bool` | Gets or sets a value indicating whether paging is allowed. Set to true by default. |
| AllowScroll | `bool` | Gets or sets a value indicating whether the user can scroll or swipe through carousel items. Set to true by default. |
| AnimationDuration | `double?` | Gets or sets the slide transition animation duration in milliseconds. When null (default), the browser's native smooth scroll is used. Use 0 for instant transitions with no animation, or a positive value for a custom duration. |
| AriaLabel | `string?` | Gets or sets the aria-label of the carousel container. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Auto | `bool` | Gets or sets a value indicating whether this cycle is automatic. |
| ButtonShade | `Shade` | Gets or sets the color shade of the buttons. |
| ButtonSize | `ButtonSize` | Gets or sets the buttons size. |
| ButtonStyle | `ButtonStyle` | Gets or sets the buttons style |
| ButtonVariant | `Variant` | Gets or sets the design variant of the buttons. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Interval | `double` | Gets or sets the auto-cycle interval in milliseconds. |
| Items | `RenderFragment?` | Gets or sets the items. |
| ItemsPerPage | `int` | Gets or sets the number of items visible at the same time. Set to 1 by default. |
| NextAriaLabel | `string` | Gets or sets the aria-label of the next button. |
| NextIcon | `string` | Gets or sets the next button icon. |
| NextText | `string` | Gets or sets the next button text. |
| PagerButtonAriaLabelFormat | `string` | Gets or sets the pager button aria-label format. Use {0} for the 1-based slide index. |
| PagerOverlay | `bool` | Gets or sets a value indicating whether pager overlays the carousel items. Set to true by default. |
| PagerPosition | `PagerPosition` | Gets or sets the pager position. Set to PagerPosition.Bottom by default. |
| PrevAriaLabel | `string` | Gets or sets the aria-label of the previous button. |
| PrevIcon | `string` | Gets or sets the previous button icon. |
| PrevText | `string` | Gets or sets the previous button text. |
| SelectedIndex | `int` | Gets or sets the selected index. |
| SlideAriaLabelFormat | `string` | Gets or sets the slide aria-label format. Use {0} for the 1-based slide index and {1} for the total slide count. |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<int>` | Gets or sets the change callback. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| SelectedIndexChanged | `EventCallback<int>` | Gets or sets the selected index changed callback. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| AddItem(RadzenCarouselItem item) | `void` | Adds the item. |
| Navigate(int index) | `Task` | Navigates to specific index. |
| OnScroll(int index) | `Task` | Called from JavaScript when the user scrolls the carousel items container. |
| RemoveItem(RadzenCarouselItem item) | `void` | Removes the item. |
| Reset() | `Task` | Resets the auto-cycle timer. |
| Start() | `void` | Starts the auto-cycle timer. |
| Stop() | `void` | Stops the auto-cycle timer. |

