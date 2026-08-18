# RadzenColumn API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChildContent | `RenderFragment?` | Gets or sets the child content |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Offset | `int?` | Gets or sets the number of columns to skip before this column (left margin spacing). Creates empty space to the left by pushing the column to the right. |
| OffsetLG | `int?` | Gets or sets the offset for large screens (breakpoint ≥ 1024px). |
| OffsetMD | `int?` | Gets or sets the offset for medium screens (breakpoint ≥ 768px). |
| OffsetSM | `int?` | Gets or sets the offset for small screens (breakpoint ≥ 576px). |
| OffsetXL | `int?` | Gets or sets the offset for extra large screens (breakpoint ≥ 1280px). |
| OffsetXS | `int?` | Gets or sets the offset for extra small screens (breakpoint < 576px). |
| OffsetXX | `int?` | Gets or sets the offset for extra extra large screens (breakpoint ≥ 1536px). |
| Order | `string?` | Gets or sets the visual display order of this column within its row. Allows reordering columns without changing their position in markup. Values can be 0-12 or "first"/"last". |
| OrderLG | `string?` | Gets or sets the column order for large screens (breakpoint ≥ 1024px). |
| OrderMD | `string?` | Gets or sets the column order for medium screens (breakpoint ≥ 768px). |
| OrderSM | `string?` | Gets or sets the column order for small screens (breakpoint ≥ 576px). |
| OrderXL | `string?` | Gets or sets the column order for extra large screens (breakpoint ≥ 1280px). |
| OrderXS | `string?` | Gets or sets the column order for extra small screens (breakpoint < 576px). |
| OrderXX | `string?` | Gets or sets the column order for extra extra large screens (breakpoint ≥ 1536px). |
| Size | `int?` | Gets or sets the default column width as a value from 1-12 in the grid system. If not specified, the column will automatically expand to fill available space. |
| SizeLG | `int?` | Gets or sets the column width for large screens (breakpoint ≥ 1024px). Overrides the default Size on desktops and larger devices. |
| SizeMD | `int?` | Gets or sets the column width for medium screens (breakpoint ≥ 768px). Overrides the default Size on tablets and larger devices. |
| SizeSM | `int?` | Gets or sets the column width for small screens (breakpoint ≥ 576px). Overrides the default Size on small tablets and larger devices. |
| SizeXL | `int?` | Gets or sets the column width for extra large screens (breakpoint ≥ 1280px). Overrides the default Size on large desktops and larger devices. |
| SizeXS | `int?` | Gets or sets the column width for extra small screens (breakpoint < 576px). Overrides the default Size on mobile devices. |
| SizeXX | `int?` | Gets or sets the column width for extra extra large screens (breakpoint ≥ 1536px). Overrides the default Size on very large displays. |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |

