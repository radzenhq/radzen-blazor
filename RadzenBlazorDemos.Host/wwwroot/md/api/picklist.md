# RadzenPickList API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowFiltering | `bool` | Gets or sets value if filtering is allowed. |
| AllowMoveAll | `bool` | Gets or sets a value indicating whether it is allowed to move all items. |
| AllowMoveAllSourceToTarget | `bool` | Gets or sets a value indicating whether it is allowed to move all items from source to target. |
| AllowMoveAllTargetToSource | `bool` | Gets or sets a value indicating whether it is allowed to move all items from target to source. |
| AllowSelectAll | `bool` | Gets or sets a value indicating whether selecting all items is allowed. |
| AllowVirtualization | `bool` | Gets or sets a value indicating whether virtualization is enabled for the source and target listboxes. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ButtonGap | `string?` | Gets or sets the buttons spacing |
| ButtonJustifyContent | `JustifyContent` | Gets or sets the buttons style |
| ButtonShade | `Shade` | Gets or sets the color shade of the buttons. |
| ButtonSize | `ButtonSize` | Gets or sets the buttons size. |
| ButtonStyle | `ButtonStyle` | Gets or sets the buttons style |
| ButtonVariant | `Variant` | Gets or sets the design variant of the buttons. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Disabled | `bool` | Gets or sets a value indicating whether component is disabled. |
| DisabledProperty | `string?` | Gets or sets the disabled property |
| EmptyTemplate | `RenderFragment?` | Gets or sets the empty template shown when a list has no items. |
| EmptyText | `string` | Gets or sets the empty text shown when a list has no items. |
| ItemRender | `Action<PickListItemRenderEventArgs<TItem>>?` | Gets or sets the row render callback. Use it to set row attributes. |
| MoveFilteredItemsOnlyOnMoveAll | `bool` | Gets or sets a value indicating whether to move all or only avaialable after filter items. |
| Multiple | `bool` | Gets or sets a value indicating whether multiple selection is allowed. |
| Orientation | `Orientation` | Gets or sets the orientation |
| Placeholder | `string?` | Gets or sets the common placeholder |
| SelectAllText | `string?` | Gets or sets the select all text. |
| SelectedSourceToTargetIcon | `string` | Gets or sets the selected source to target icon |
| SelectedSourceToTargetTitle | `string` | Gets or sets the selected source to target title |
| SelectedTargetToSourceIcon | `string` | Gets or sets the selected target to source icon |
| SelectedTargetToSourceTitle | `string` | Gets or sets the selected target to source title |
| ShowHeader | `bool` | Gets or sets value if headers are shown. |
| Source | `IEnumerable<TItem>?` | Gets or sets the source collection. |
| SourceAriaLabel | `string` | Gets or sets the aria-label of the source list. Ignored when is rendered - the source list is labelled by the header instead. |
| SourceEmptyTemplate | `RenderFragment?` | Gets or sets the empty template shown when the source list has no items. Overrides . |
| SourceEmptyText | `string?` | Gets or sets the empty text shown when the source list has no items. Overrides . |
| SourceExpression | `Expression<Func<IEnumerable<TItem>?>>?` | Gets or sets the source expression used to create the FieldIdentifier for source validation. |
| SourceHeader | `RenderFragment?` | Gets or sets the source header |
| SourcePlaceholder | `string?` | Gets or sets the source placeholder |
| SourceTemplate | `RenderFragment<TItem>?` | Gets or sets the source template. Overrides . |
| SourceToTargetIcon | `string` | Gets or sets the source to target icon |
| SourceToTargetTitle | `string` | Gets or sets the source to target title |
| Style | `string?` | Gets or sets the inline CSS style. |
| Target | `IEnumerable<TItem>?` | Gets or sets the target collection. |
| TargetAriaLabel | `string` | Gets or sets the aria-label of the target list. Ignored when is rendered - the target list is labelled by the header instead. |
| TargetEmptyTemplate | `RenderFragment?` | Gets or sets the empty template shown when the target list has no items. Overrides . |
| TargetEmptyText | `string?` | Gets or sets the empty text shown when the target list has no items. Overrides . |
| TargetExpression | `Expression<Func<IEnumerable<TItem>?>>?` | Gets or sets the target expression used to create the FieldIdentifier for target validation. |
| TargetHeader | `RenderFragment?` | Gets or sets the target header |
| TargetPlaceholder | `string?` | Gets or sets the target placeholder |
| TargetTemplate | `RenderFragment<TItem>?` | Gets or sets the target template. Overrides . |
| TargetToSourceIcon | `string` | Gets or sets the target to source icon |
| TargetToSourceTitle | `string` | Gets or sets the target to source title |
| Template | `RenderFragment<TItem>?` | Gets or sets the template. |
| TextProperty | `string?` | Gets or sets the text property |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| ValueProperty | `string?` | Gets or sets the value property |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| Move | `EventCallback<PickListMoveEventArgs<TItem>>` | Gets or sets the callback that is invoked when items are moved between the source and target collections. Fires after and , so the bound collections already reflect the move. |
| SelectedSourceChanged | `EventCallback<IEnumerable<TItem>>` | Gets or sets the callback that is invoked when the selected source items change. |
| SelectedTargetChanged | `EventCallback<IEnumerable<TItem>>` | Gets or sets the callback that is invoked when the selected target items change. |
| SourceChanged | `EventCallback<IEnumerable<TItem>>` | Gets or sets the source changed. |
| TargetChanged | `EventCallback<IEnumerable<TItem>>` | Gets or sets the target changed. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| GetSelectedSources() | `IEnumerable<TItem>` | Returns a collection of TItem that are selected in the source list. |
| GetSelectedTargets() | `IEnumerable<TItem>` | Returns a collection of TItem that are selected in the target list. |

