# RadzenDataGrid API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllColumnsText | `string` | Gets or sets the column picker all columns text. |
| AllGroupsExpanded | `bool?` | Gets or sets a value indicating whether all groups should be expanded when DataGrid is grouped. |
| AllowAlternatingRows | `bool` | Gets or sets a value indicating whether DataGrid should use alternating row styles. |
| AllowColumnPicking | `bool` | Gets or sets a value indicating whether column picking is allowed. |
| AllowColumnReorder | `bool` | Gets or sets a value indicating whether column reorder is allowed. |
| AllowColumnResize | `bool` | Gets or sets a value indicating whether column resizing is allowed. |
| AllowCompositeDataCells | `bool` | Gets or sets a value indicating whether DataGrid data cells will follow the header cells structure in composite columns. |
| AllowFilterDateInput | `bool` | Gets or sets a value indicating whether input is allowed in filter DatePicker. |
| AllowFiltering | `bool` | Gets or sets a value indicating whether filtering is allowed. |
| AllowGrouping | `bool` | Gets or sets a value indicating whether grouping is allowed. |
| AllowMultiColumnSorting | `bool` | Gets or sets a value indicating whether multi column sorting is allowed. |
| AllowPaging | `bool` | Gets or sets a value indicating whether paging is allowed. Set to false by default. |
| AllowPickAllColumns | `bool` | Gets or sets a value indicating whether user can pick all columns in column picker. |
| AllowRowSelectOnRowClick | `bool` | Gets or sets a value indicating whether DataGrid row can be selected on row click. |
| AllowSorting | `bool` | Gets or sets a value indicating whether sorting is allowed. |
| AllowSortingColumnPicker | `bool` | Gets or sets a value indicating whether sorting in column picker is allowed. |
| AllowVirtualization | `bool` | Gets or sets whether the DataGrid uses virtualization to improve performance with large datasets. When enabled, only visible rows are rendered in the DOM, with additional rows loaded as the user scrolls. Virtualization significantly reduces memory usage and initial render time for grids with thousands of rows. |
| AndOperatorText | `string` | Gets or sets the and operator text. |
| ApplyFilterText | `string` | Gets or sets the apply filter text. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| AutoApplyCheckBoxListFilter | `bool` | Gets or sets whether CheckBoxList column filters are applied immediately as options are selected, instead of requiring the filter popup's Apply button. Default is false. |
| CellRender | `Action<DataGridCellRenderEventArgs<TItem>>?` | Gets or sets the cell render callback. Use it to set cell attributes. |
| ClearFilterText | `string` | Gets or sets the clear filter text. |
| CollapseAllTitle | `string` | Gets or sets the title attribute of the collapse all button. |
| ColumnWidth | `string` | Gets or sets the width of all columns. |
| Columns | `RenderFragment?` | Gets or sets the columns. |
| ColumnsPickerAllowFiltering | `bool` | Gets or sets a value indicating whether user can filter columns in column picker. |
| ColumnsPickerMaxSelectedLabels | `int` | Gets or sets the column picker max selected labels. |
| ColumnsShowingText | `string` | Gets or sets the column picker columns showing text. |
| ColumnsText | `string` | Gets or sets the column picker columns text. |
| ContainsText | `string` | Gets or sets the contains text. |
| Count | `int` | Gets or sets the count. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| CustomText | `string` | Gets or sets the custom filter operator text. |
| Data | `IEnumerable<T>?` | Gets or sets the data. |
| Density | `Density` | Gets or sets a value indicating pager density. |
| DoesNotContainText | `string` | Gets or sets the does not contain text. |
| EditMode | `DataGridEditMode` | Gets or sets the edit mode. |
| EditTemplate | `RenderFragment<TItem>?` | Gets or sets the edit template. |
| EmptyTemplate | `RenderFragment?` | Gets or sets the empty template shown when Data is empty collection. |
| EmptyText | `string` | Gets or sets the empty text shown when Data is empty collection. |
| EndsWithText | `string` | Gets or sets the ends with text. |
| EnumFilterSelectText | `string` | Gets or sets the enum filter select text. |
| EnumFilterTranslationFunc | `Func<string, string>?` | Allows to define a custom function for enums DisplayAttribute Description property value translation in datagrid Enum filters. |
| EnumNullFilterText | `string` | Gets or sets the nullable enum for null value filter text. |
| EqualsText | `string` | Gets or sets the equals text. |
| ExpandAllTitle | `string` | Gets or sets the title attribute of the expand all button. |
| ExpandChildItemAriaLabel | `string?` | Gets or sets the expand child item aria label text. |
| ExpandGroupAriaLabel | `string?` | Gets or sets the expand group aria label text. |
| ExpandMode | `DataGridExpandMode` | Gets or sets the expand mode. |
| FilterAsYouType | `bool` | Gets or sets a value indicating whether filtering is performed as you type. Set to true by default. When set to false, the filter is only applied when the user presses Enter or leaves the filter input. |
| FilterCaseSensitivity | `FilterCaseSensitivity` | Gets or sets the filter case sensitivity. |
| FilterDateFormat | `string` | Gets or sets the filter date format. |
| FilterDelay | `int` | Gets or sets the filter delay. |
| FilterIcon | `string` | Gets or set the filter icon to use. |
| FilterMode | `FilterMode` | Gets or sets the filter mode. |
| FilterOperatorAriaLabel | `string` | Gets or sets the column filter value aria label text. |
| FilterPopupRenderMode | `PopupRenderMode` | Gets or sets the render mode. |
| FilterText | `string` | Gets or sets the filter text. |
| FilterToggleAriaLabel | `string?` | Gets or sets the date simple filter toggle aria label text. |
| FilterValueAriaLabel | `string` | Gets or sets the column filter value aria label text. |
| FirstPageAriaLabel | `string` | Gets or sets the pager's first page button's aria-label attribute. |
| FirstPageTitle | `string` | Gets or sets the pager's first page button's title attribute. |
| FooterCellRender | `Action<DataGridCellRenderEventArgs<TItem>>?` | Gets or sets the footer cell render callback. Use it to set footer cell attributes. |
| FooterTemplate | `RenderFragment?` | Gives the grid a custom footer, allowing the adding of components to create custom tool bars or custom pagination |
| GotoFirstPageOnSort | `bool` | Gets or sets the ability to automatically goto the first page when sorting is changed. |
| GreaterThanOrEqualsText | `string` | Gets or sets the greater than or equals text. |
| GreaterThanText | `string` | Gets or sets the greater than text. |
| GridLines | `DataGridGridLines` | Gets or sets the grid lines. |
| GroupFootersAlwaysVisible | `bool` | Gets or sets a value indicating whether group footers are visible even when the group is collapsed. |
| GroupHeaderTemplate | `RenderFragment<Group>?` | Gets or sets the group header template. |
| GroupHeaderToggleTemplate | `RenderFragment<(Group Group, RadzenDataGridGroupRow<TItem> GroupHeader)>?` | Gets or sets the group header with option to add custom toggle visibility button template. |
| GroupPanelText | `string` | Gets or sets the group panel text. |
| GroupRowRender | `Action<GroupRowRenderEventArgs>?` | Gets or sets the group row render callback. Use it to set group row attributes. |
| HeaderCellRender | `Action<DataGridCellRenderEventArgs<TItem>>?` | Gets or sets the header cell render callback. Use it to set header cell attributes. |
| HeaderTemplate | `RenderFragment?` | Gives the grid a custom header, allowing the adding of components to create custom tool bars in addtion to column grouping and column picker |
| HideGroupedColumn | `bool` | Gets or sets a value indicating whether grouped column should be hidden. |
| InText | `string` | Gets or sets the in operator text. |
| IsEmptyText | `string` | Gets or sets the is empty text. |
| IsLoading | `bool` | Gets or sets a value indicating whether this instance loading indicator is shown. |
| IsNotEmptyText | `string` | Gets or sets the is not empty text. |
| IsNotNullText | `string` | Gets or sets the not null text. |
| IsNullText | `string` | Gets or sets the is null text. |
| KeyProperty | `string?` | Gets or sets the key property. |
| LastPageAriaLabel | `string` | Gets or sets the pager's last page button's aria-label attribute. |
| LastPageTitle | `string` | Gets or sets the pager's last page button's title attribute. |
| LessThanOrEqualsText | `string` | Gets or sets the less than or equals text. |
| LessThanText | `string` | Gets or sets the less than text. |
| LoadSettings | `Action<DataGridLoadSettingsEventArgs>?` | Gets or sets the load settings callback. |
| LoadingTemplate | `RenderFragment?` | Gets or sets the loading template. |
| LogicalFilterOperator | `LogicalFilterOperator` | Gets or sets the logical filter operator. |
| LogicalOperatorAriaLabel | `string` | Gets or sets the column logical filter value aria label text. |
| NextPageAriaLabel | `string` | Gets or sets the pager's next page button's aria-label attribute. |
| NextPageLabel | `string?` | Gets or sets the pager's optional next page button's label text. |
| NextPageTitle | `string` | Gets or sets the pager's next page button's title attribute. |
| NotEqualsText | `string` | Gets or sets the not equals text. |
| NotInText | `string` | Gets or sets the not in operator text. |
| OrOperatorText | `string` | Gets or sets the or operator text. |
| PageAriaLabelFormat | `string` | Gets or sets the pager's numeric page number buttons' aria-label attributes. |
| PageNumbersCount | `int` | Gets or sets the page numbers count. |
| PageSize | `int` | Gets or sets the size of the page. |
| PageSizeOptions | `IEnumerable<int>?` | Gets or sets the page size options. |
| PageSizeText | `string` | Gets or sets the page size description text. |
| PageTitleFormat | `string` | Gets or sets the pager's numeric page number buttons' title attributes. |
| PagerAlwaysVisible | `bool` | Gets or sets a value indicating whether pager is visible even when not enough data for paging. |
| PagerHorizontalAlign | `HorizontalAlign` | Gets or sets the horizontal align. |
| PagerPosition | `PagerPosition` | Gets or sets the pager position. Set to PagerPosition.Bottom by default. |
| PagingSummaryFormat | `string` | Gets or sets the pager summary format. has preference over this property. |
| PagingSummaryTemplate | `RenderFragment<PagingInformation>?` | Gets or sets the pager summary template. Has preference over . |
| PrevPageAriaLabel | `string` | Gets or sets the pager's previous page button's aria-label attribute. |
| PrevPageLabel | `string?` | Gets or sets the pager's optional previous page button's label text. |
| PrevPageTitle | `string` | Gets or sets the pager's previous page button's title attribute. |
| QueryOnlyVisibleColumns | `bool` | Gets or sets a value indicating whether only visible columns are included in the query. |
| RemoveGroupAriaLabel | `string` | Gets or sets the remove group button aria label text. |
| Render | `Action<DataGridRenderEventArgs<TItem>>?` | Gets or sets the render callback. |
| RenderAsync | `Func<DataGridRenderEventArgs<TItem>, Task>?` | Gets or sets the render async callback. |
| Responsive | `bool` | Gets or sets a value indicating whether DataGrid is responsive. |
| RowRender | `Action<RowRenderEventArgs<TItem>>?` | Gets or sets the row render callback. Use it to set row attributes. |
| SecondFilterOperatorAriaLabel | `string` | Gets or sets the column filter value aria label text. |
| SecondFilterValueAriaLabel | `string` | Gets or sets the column filter value aria label text. |
| SelectVisibleColumnsAriaLabel | `string` | Gets or sets the select visible columns aria label text. |
| SelectionMode | `DataGridSelectionMode` | Gets or sets the selection mode. |
| Settings | `DataGridSettings?` | Gets or sets DataGrid settings. |
| ShowCellDataAsTooltip | `bool` | Gets or sets a value indicating whether cell data should be shown as tooltip. |
| ShowColumnTitleAsTooltip | `bool` | Gets or sets a value indicating whether column title should be shown as tooltip. |
| ShowEmptyMessage | `bool` | Gets or sets a value indicating whether DataGrid data body show empty message. |
| ShowExpandAll | `bool` | Gets or sets a value indicating whether all rows can be expanded at once from the header. Setting is only available when is set to DataGridExpandMode.Multiple. |
| ShowExpandColumn | `bool` | Gets or sets whether the expandable indicator column is visible. |
| ShowGroupExpandColumn | `bool` | Gets or sets a value indicating whether to show group visibility column |
| ShowHeader | `bool` | Gets or sets value if headers are shown. |
| ShowMultiColumnSortingIndex | `bool` | Gets or sets a value indicating whether multi column sorting index is shown. |
| ShowPagingSummary | `bool` | Gets or sets the pager summary visibility. |
| StartsWithText | `string` | Gets or sets the starts with text. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the tabindex applied to the grid element. Set to 0 by default so the grid is a tab stop. Embedding components can set it to -1 to remove the grid from the tab order. |
| Template | `RenderFragment<T>?` | Gets or sets the template. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Value | `IList<TItem>?` | Gets or sets the selected item. |
| VirtualizationOverscanCount | `int` | Gets or sets the number of additional rows to render before and after the visible viewport when virtualization is enabled. A higher overscan count reduces the chance of seeing blank space during fast scrolling, but increases the number of rendered elements. The optimal value depends on row height and typical scroll speed. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| AllGroupsExpandedChanged | `EventCallback<bool?>` | Gets or sets the AllGroupsExpanded changed callback. |
| CellClick | `EventCallback<DataGridCellMouseEventArgs<TItem>>` | Gets or sets the cell click callback. |
| CellContextMenu | `EventCallback<DataGridCellMouseEventArgs<TItem>>` | Gets or sets the row click callback. |
| CellDoubleClick | `EventCallback<DataGridCellMouseEventArgs<TItem>>` | Gets or sets the cell double click callback. |
| ColumnReordered | `EventCallback<DataGridColumnReorderedEventArgs<TItem>>` | Gets or sets the column reordered callback. |
| ColumnReordering | `EventCallback<DataGridColumnReorderingEventArgs<TItem>>` | Gets or sets the column reordering callback. |
| ColumnResized | `EventCallback<DataGridColumnResizedEventArgs<TItem>>` | Gets or sets the column resized callback. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| Filter | `EventCallback<DataGridColumnFilterEventArgs<TItem>>` | Gets or sets the column filter callback. |
| FilterCleared | `EventCallback<DataGridColumnFilterEventArgs<TItem>>` | Gets or sets the column filter cleared callback. |
| Group | `EventCallback<DataGridColumnGroupEventArgs<TItem>>` | Gets or sets the column group callback. |
| GroupRowCollapse | `EventCallback<Group>` | Gets or sets the group row collapse callback. |
| GroupRowExpand | `EventCallback<Group>` | Gets or sets the group row expand callback. |
| KeyDown | `EventCallback<KeyboardEventArgs>` | Gets or sets key down callback. |
| LoadChildData | `EventCallback<Radzen.DataGridLoadChildDataEventArgs<TItem>>` | Gets or sets the load child data callback. |
| LoadColumnFilterData | `EventCallback<DataGridLoadColumnFilterDataEventArgs<TItem>>` | Gets or sets the callback used to load column filter data for DataGrid FilterMode.CheckBoxList filter mode. |
| LoadData | `EventCallback<Radzen.LoadDataArgs>` | Gets or sets the load data. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| Page | `EventCallback<PagerEventArgs>` | Gets or sets the page callback. |
| PageSizeChanged | `EventCallback<int>` | Gets or sets the page size changed callback. |
| PickedColumnsChanged | `EventCallback<DataGridPickedColumnsChangedEventArgs<TItem>>` | Gets or sets the picked columns changed callback. |
| RowClick | `EventCallback<DataGridRowMouseEventArgs<TItem>>` | Gets or sets the row click callback. |
| RowCollapse | `EventCallback<TItem>` | Gets or sets the row collapse callback. |
| RowCreate | `EventCallback<TItem>` | Gets or sets the row create callback. |
| RowDeselect | `EventCallback<TItem>` | Gets or sets the row deselect callback. |
| RowDoubleClick | `EventCallback<DataGridRowMouseEventArgs<TItem>>` | Gets or sets the row double click callback. |
| RowEdit | `EventCallback<TItem>` | Gets or sets the row edit callback. |
| RowExpand | `EventCallback<TItem>` | Gets or sets the row expand callback. |
| RowSelect | `EventCallback<TItem>` | Gets or sets the row select callback. |
| RowUpdate | `EventCallback<TItem>` | Gets or sets the row update callback. |
| SettingsChanged | `EventCallback<DataGridSettings>` | Gets or sets the settings changed callback. |
| Sort | `EventCallback<DataGridColumnSortEventArgs<TItem>>` | Gets or sets the column sort callback. |
| ValueChanged | `EventCallback<IList<TItem>>` | Gets or sets the value changed callback. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| ApplyFilter(RadzenDataGridColumn<TItem> column, bool closePopup) | `Task` | Apply filter to the specified column |
| CancelEditRow(TItem item) | `void` | Cancels the edited row. |
| CancelEditRows(IEnumerable<TItem> items) | `void` | Cancels the edit of a range of rows. |
| ClearFilter(RadzenDataGridColumn<TItem> column, bool closePopup, bool shouldReload) | `Task` | ?lear filter on the specified column |
| CollapseAll() | `System.Threading.Tasks.Task` | Collapse all rows that are expanded |
| CollapseRows(IEnumerable<TItem> items) | `System.Threading.Tasks.Task` | Collapse a range of rows. |
| EditRow(TItem item) | `System.Threading.Tasks.Task` | Edits the row. |
| EditRows(IEnumerable<TItem> items) | `System.Threading.Tasks.Task` | Edits a range of rows. |
| ExpandGroupItem(RadzenDataGridGroupRow<TItem> item, bool? expandedOnLoad) | `System.Threading.Tasks.Task` | Expand group item. |
| ExpandRow(TItem item) | `System.Threading.Tasks.Task` | Expands the row to show the content defined in Template property. |
| ExpandRows(IEnumerable<TItem> items) | `System.Threading.Tasks.Task` | Expands a range of rows. |
| InsertAfterRow(TItem itemToInsert, TItem rowItem) | `System.Threading.Tasks.Task` | Inserts new row after specific row item. |
| InsertRow(TItem item) | `System.Threading.Tasks.Task` | Inserts new row. |
| IsRowExpanded(TItem item) | `bool` | Gets boolean value indicating if the row is expanded or not. |
| IsRowInEditMode(TItem item) | `bool` | Determines whether row in edit mode. |
| OnColumnDropToGroup() | `Task` | Called from JS when a touch-initiated column reorder drop lands on the group panel. |
| OnColumnReorderEnded(int columnIndex) | `Task` | Called from JS when a touch-initiated column reorder drop lands on a target column. |
| OnColumnResized(int columnIndex, double value) | `Task` | Called when column is resized. |
| OrderBy(string property) | `void` | Orders the DataGrid by property name. |
| OrderByDescending(string property) | `void` | Orders descending the DataGrid by property name. |
| RefreshDataAsync() | `Task` | Clears the internal data cache and refreshes the DataGrid, reloading data from the source. When virtualization is enabled, this method refreshes the Virtualize component. Otherwise, it triggers a standard reload. Call this method after external data changes to ensure the grid displays current data. |
| ReloadSettings(bool forceReload) | `Task` | Force load of the DataGrid Settings. This method triggers a reload of the DataGrid settings, optionally forcing a reload even if the settings are already loaded. |
| Reset(bool resetColumnState, bool resetRowState) | `void` | Resets the internal LoadData state, forcing the next data operation to reload from the source. This is useful when you've made changes to the underlying data source and want to ensure the next load operation fetches fresh data instead of using cached arguments. Typically called before RefreshDataAsync or when manually managing data reload. |
| ResetLoadData() | `void` | Resets the internal LoadData state, forcing the next data operation to reload from the source. This is useful when you've made changes to the underlying data source and want to ensure the next load operation fetches fresh data instead of using cached arguments. Typically called before RefreshDataAsync or when manually managing data reload. |
| SaveSettings() | `void` | Saves DataGrid settings as JSON string. |
| SelectRow(TItem item, bool raiseEvent) | `System.Threading.Tasks.Task` | Selects the row. |
| SetColumnFilterValueFromSettings(RadzenDataGridColumn<TItem> gridColumn, DataGridColumnSettings columnSettings, bool isFirst) | `bool` | Override this method to customize how filter values are serialized back typed from the settings. |
| UpdatePickableColumns() | `void` | Updates pickable columns. |
| UpdateRow(TItem item) | `System.Threading.Tasks.Task` | Updates the row. |

