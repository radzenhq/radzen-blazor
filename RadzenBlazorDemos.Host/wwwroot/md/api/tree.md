# RadzenTree API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowCheckBoxes | `bool` | Specifies whether RadzenTree displays check boxes. Set to false by default. |
| AllowCheckChildren | `bool` | Specifies what happens when a parent item is checked. If set to true checking parent items also checks all of its children. |
| AllowCheckParents | `bool` | Specifies what happens with a parent item when one of its children is checked. If set to true checking a child item will affect the checked state of its parents. |
| AriaLabel | `string?` | Gets or sets the accessible name of the tree, exposed via the aria-label attribute on the role="tree" container. |
| AriaLabelledBy | `string?` | Gets or sets the id of the element that labels the tree, exposed via the aria-labelledby attribute on the role="tree" container. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| CheckedValues | `IEnumerable<object>?` | Gets or sets the checked values. Use with @bind-CheckedValues to sync it with a property. |
| ChildContent | `RenderFragment?` | Gets or sets the child content. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Data | `IEnumerable?` | Specifies the collection of data items which RadzenTree will create its items from. |
| ItemContentCssClass | `string?` | Gets or sets the CSS classes added to the item content. |
| ItemIconCssClass | `string?` | Gets or sets the CSS classes added to the item icon. |
| ItemLabelCssClass | `string?` | Gets or sets the CSS classes added to the item label. |
| ItemRender | `Action<TreeItemRenderEventArgs>?` | A callback that will be invoked when item is rendered. |
| SelectItemAriaLabel | `string?` | Gets or sets the open button aria-label attribute. |
| SingleExpand | `bool` | Specifies whether siblings items are collapsed. Set to false by default. |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Value | `object?` | Specifies the selected value. Use with @bind-Value to sync it with a property. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<TreeEventArgs>` | A callback that will be invoked when the user selects an item. |
| CheckedValuesChanged | `EventCallback<IEnumerable<object>>` | A callback which will be invoked when changes. |
| Collapse | `EventCallback<TreeEventArgs>` | A callback that will be invoked when the user collapse an item. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| Expand | `EventCallback<TreeExpandEventArgs>` | A callback that will be invoked when the user expands an item. |
| ItemContextMenu | `EventCallback<TreeItemContextMenuEventArgs>` | Gets or sets the context menu callback. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| ValueChanged | `EventCallback<object>` | A callback which will be invoked when changes. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| ClearSelection() | `void` | Clear the current selection to allow re-selection by mouse click |
| Reload(RadzenTreeItem? item) | `Task` | Forces the specified or, if is null, all items in the tree to be re-evaluated such that items lazily created via are realised if the underlying data model has been changed from somewhere else. |

