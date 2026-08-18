# RadzenDropDownDataGrid API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AddAriaLabel | `string` | Gets or sets the add button aria-label attribute. |
| AllowClear | `bool` | Gets or sets a value indicating whether the user can clear the value. Set to false by default. |
| AllowColumnPicking | `bool` | Gets or sets a value indicating whether column picking is allowed. |
| AllowColumnReorder | `bool` | Gets or sets a value indicating whether column reorder is allowed. |
| AllowColumnResize | `bool` | Gets or sets a value indicating whether column resizing is allowed. |
| AllowFiltering | `bool` | Gets or sets a value indicating whether filtering is allowed. |
| AllowFilteringByAllStringColumns | `bool` | Gets or sets a value indicating whether filtering by all string columns is allowed. |
| AllowFilteringByWord | `bool` | Gets or sets a value indicating whether filtering by each entered word in the search term, sperated by a space, is allowed. |
| AllowFilteringByWordCount | `int` | Gets or sets the AllowFilteringByWord max count. |
| AllowRowSelectOnRowClick | `bool` | Gets or sets a value indicating whether DataGrid row can be selected on row click. |
| AllowSelectAll | `bool` | Gets or sets a value indicating whether the user can select all values in multiple selection. Set to true by default. |
| AllowSorting | `bool` | Gets or sets a value indicating whether sorting is allowed. |
| AllowVirtualization | `bool` | Specifies wether virtualization is enabled. Set to false by default. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| CellRender | `Action<DataGridCellRenderEventArgs<object>>?` | Gets or sets the cell render callback. Use it to set cell attributes. |
| Chips | `bool` | Gets or sets a value indicating whether the selected items will be displayed as chips. Set to false by default. Requires to be set to true. |
| ClearAriaLabel | `string` | Gets or sets the clear button aria label text. |
| ColumnWidth | `string` | Gets or sets the width of all columns. |
| Columns | `RenderFragment?` | Gets or sets the columns. |
| Count | `int` | Specifies the total number of items in the data source. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Data | `IEnumerable?` | Gets or sets the data. |
| Density | `Density` | Gets or sets a value indicating DataGrid density. |
| Disabled | `bool` | Gets or sets a value indicating whether this is disabled. |
| DisabledProperty | `string?` | Gets or sets the disabled property. |
| EmptyAriaLabel | `string` | Gets or sets the empty value aria label text. |
| EmptyTemplate | `RenderFragment?` | Gets or sets the empty template shown when Data is empty collection. |
| EmptyText | `string` | Gets or sets the empty text. |
| FieldIdentifier | `FieldIdentifier` | Gets the field identifier. |
| FilterAsYouType | `bool` | Gets or sets a value indicating whether filtering is allowed as you type. Set to true by default. |
| FilterCaseSensitivity | `FilterCaseSensitivity` | Gets or sets the filter case sensitivity. |
| FilterDelay | `int` | Gets or sets the filter delay. |
| FilterOperator | `StringFilterOperator` | Gets or sets the filter operator. |
| FirstPageAriaLabel | `string` | Gets or sets the pager's first page button's aria-label attribute. |
| FirstPageTitle | `string` | Gets or sets the pager's first page button's title attribute. |
| FocusFilterOnPopup | `bool` | Gets or sets whether popup automatically focuses on filter input. |
| FooterTemplate | `RenderFragment?` | Gets or sets the footer template. |
| GridLines | `DataGridGridLines` | Gets or sets the grid lines. |
| HeaderTemplate | `RenderFragment?` | Gets or sets the header template. |
| InputAttributes | `IReadOnlyDictionary<string, object>?` | Specifies additional custom attributes that will be rendered by the input. |
| InputSize | `InputSize` | Gets or sets the size of the component. |
| IsLoading | `bool` |  |
| ItemComparer | `IEqualityComparer<object>?` | For lists of objects, an IEqualityComparer to control how selected items are determined |
| LastPageAriaLabel | `string` | Gets or sets the pager's last page button's aria-label attribute. |
| LastPageTitle | `string` | Gets or sets the pager's last page button's title attribute. |
| MaxSelectedLabels | `int` | Gets or sets the number of maximum selected labels. |
| Multiple | `bool` | Gets or sets a value indicating whether this is multiple. |
| Name | `string?` | Gets or sets the name. |
| NextPageAriaLabel | `string` | Gets or sets the pager's next page button's aria-label attribute. |
| NextPageTitle | `string` | Gets or sets the pager's next page button's title attribute. |
| OpenOnFocus | `bool` | Gets or sets a value indicating whether popup should open on focus. Set to false by default. |
| OpenPopupKey | `string` | Gets or sets the keyboard key that triggers opening the popup when is enabled. Default is "Enter". |
| PageAriaLabelFormat | `string` | Gets or sets the pager's numeric page number buttons' aria-label attributes. |
| PageNumbersCount | `int` | Gets or sets the page numbers count. |
| PageSize | `int` | Specifies the default page size. Set to 5 by default. |
| PageSizeOptions | `IEnumerable<int>` | Gets or sets the page size options. |
| PageTitleFormat | `string` | Gets or sets the pager's numeric page number buttons' title attributes. |
| PagerAlwaysVisible | `bool` | Gets or sets a value indicating whether pager is visible even when not enough data for paging. |
| PagerHorizontalAlign | `HorizontalAlign` | Gets or sets the horizontal align. |
| PagingSummaryFormat | `string` | Gets or sets the pager summary format. |
| Placeholder | `string?` | Gets or sets the placeholder. |
| PopupStyle | `string` | Gets or sets the Popup style. |
| PreserveRowSelectionOnPaging | `bool` | Gets or sets preserving the selected row index on pageing. |
| PrevPageAriaLabel | `string` | Gets or sets the pager's previous page button's aria-label attribute. |
| PrevPageTitle | `string` | Gets or sets the pager's previous page button's title attribute. |
| RemoveChipTitle | `string` | Gets or sets the remove chip button title. |
| ResetSelectedIndexOnFilter | `bool` | Gets or sets a value indicating the selected index should reset to the top item when filtering, resulting in a down arrow action will start moving from the top. |
| Responsive | `bool` | Gets or sets a value indicating whether this is responsive. |
| RowRender | `Action<RowRenderEventArgs<object>>?` | Gets or sets the row render callback. Use it to set row attributes. |
| SearchAriaLabel | `string` | Gets or sets the search aria label text. |
| SearchText | `string?` | Gets or sets the search text |
| SearchTextPlaceholder | `string` | Gets or sets the search input placeholder text. |
| SelectedItem | `object?` | Gets or sets the selected item. |
| SelectedItemsText | `string` | Gets or sets the selected items text. |
| SelectedValue | `object?` | Gets or sets the selected value. |
| Separator | `string` | Gets or sets the item separator for Multiple dropdown. |
| ShowAdd | `bool` | Gets or sets a value indicating whether the create button is shown. |
| ShowPagingSummary | `bool` | Gets or sets the pager summary visibility. |
| ShowSearch | `bool` | Gets or sets a value indicating whether search button is shown. |
| ShowValueTemplateOnEmpty | `bool` | Gets or sets whether is rendered even when there is no selected item. When true, the template is invoked with a null context so it can render an editor (e.g. a text box) for an empty value. Templates must handle a null context. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the index of the tab. |
| Template | `RenderFragment<dynamic>?` | Gets or sets the template. |
| TextProperty | `string?` | Gets or sets the text property. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Value | `T?` | Gets or sets the value. |
| ValueExpression | `Expression<Func<T>>?` | Gets or sets the value expression. |
| ValueProperty | `string?` | Gets or sets the value property. |
| ValueTemplate | `RenderFragment<dynamic>?` | Gets or sets the value template. |
| VirtualizationOverscanCount | `int` | Gets or sets a value that determines how many additional items will be rendered before and after the visible region. This help to reduce the frequency of rendering during scrolling. However, higher values mean that more elements will be present in the page. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Add | `EventCallback<MouseEventArgs>` | Gets or sets the action to be executed when the Add button is clicked. |
| Change | `EventCallback<object>` | Gets or sets the change. |
| ColumnReordered | `EventCallback<DataGridColumnReorderedEventArgs<object>>` | Gets or sets the column reordered callback. |
| ColumnReordering | `EventCallback<DataGridColumnReorderingEventArgs<object>>` | Gets or sets the column reordering callback. |
| ColumnResized | `EventCallback<DataGridColumnResizedEventArgs<object>>` | Gets or sets the column resized callback. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| ContextMenuDataGrid | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| LoadChildData | `EventCallback<Radzen.DataGridLoadChildDataEventArgs<object>>` | Gets or sets the load child data callback. |
| LoadData | `EventCallback<Radzen.LoadDataArgs>` | Gets or sets the load data. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| SearchTextChanged | `EventCallback<string>` | Gets or sets the search text changed. |
| SelectedItemChanged | `EventCallback<object>` | Gets or sets the selected item changed. |
| ValueChanged | `EventCallback<T>` | Gets or sets the value changed. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| CloseAndFocus() | `Task` | Closes the dropdown popup and sets focus to the input element. |
| ClosePopup() | `Task` | Closes the dropdown popup programmatically. |
| OnAddClick(MouseEventArgs args) | `Task` | Handles the click event. |
| OnClose() | `Task` | Called when popup is closed. |
| OpenPopup() | `Task` | Opens the popup. |
| Reload() | `Task` | Reloads this instance. |
| Reset() | `Task` | Resets component and deselects row |
| TogglePopup() | `Task` | Toggles the dropdown popup, opening it if it is closed and closing it if it is open. |

