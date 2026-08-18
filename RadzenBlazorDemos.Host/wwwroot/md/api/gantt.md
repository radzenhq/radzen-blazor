# RadzenGantt API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowAlternatingRows | `bool` | Gets or sets whether alternating rows are shown. |
| AllowColumnPicking | `bool` | Enables column picking. Default is false. |
| AllowColumnReorder | `bool` | Enables column reordering. Default is false. |
| AllowColumnResize | `bool` | Enables column resizing. Default is false. |
| AllowFilterDateInput | `bool` | Gets or sets whether filter date input is allowed. |
| AllowFiltering | `bool` | Enables filtering in the left pane grid. Default is true. |
| AllowMultiColumnSorting | `bool` | Enables multi-column sorting. Default is false. |
| AllowPaging | `bool` | Enables paging in the left pane grid. Default is false. |
| AllowRowSelectOnRowClick | `bool` | Gets or sets whether row selection is allowed on row click. |
| AllowSorting | `bool` | Enables sorting in the left pane grid. Default is true. |
| AllowVirtualization | `bool` | Enables row virtualization. Default is false. |
| AndOperatorText | `string` | And operator text. |
| ApplyFilterText | `string` | Apply filter text. |
| BaselineEndProperty | `string?` | Property name for the baseline (planned) end date. |
| BaselineStartProperty | `string?` | Property name for the baseline (planned) start date. When set together with , a secondary bar is rendered behind the actual bar showing planned vs actual. |
| CellRender | `Action<DataGridCellRenderEventArgs<TItem>>?` | Cell render callback. |
| ClearFilterText | `string` | Clear filter text. |
| Columns | `RenderFragment?` | Optional columns definition for the left pane. |
| ContainsText | `string` | Contains text. |
| Data | `IEnumerable<TItem>?` | Task items. |
| DayWidthPx | `int` | Day cell width in pixels. |
| Density | `Density` | Gets or sets the density. |
| Dependencies | `IEnumerable<GanttDependency<TItem>>?` | Optional task dependencies to draw connecting lines using object references. For database scenarios, prefer with property-name-based binding. |
| DependencyData | `IEnumerable<object>?` | Collection of dependency items (any POCO with predecessor/successor ID properties). Use together with , , and optionally . When set, takes priority over . |
| DependencyFromProperty | `string?` | Property name on items that holds the predecessor task ID (must match values of on the task items). |
| DependencyToProperty | `string?` | Property name on items that holds the successor task ID (must match values of on the task items). |
| DependencyTypeProperty | `string?` | Optional property name on items that holds the dependency type. When not set, all dependencies default to . |
| DoesNotContainText | `string` | Does not contain text. |
| EditMode | `DataGridEditMode` | Gets or sets the edit mode. |
| EmptyText | `string` | Gets or sets the empty text. |
| EndProperty | `string?` | Property name used for task end date. |
| EndsWithText | `string` | Ends with text. |
| EqualsText | `string` | Equals text. |
| ExpandMode | `DataGridExpandMode` | Gets or sets the expand mode for hierarchical data. |
| FilterCaseSensitivity | `FilterCaseSensitivity` | Gets or sets the filter case sensitivity. |
| FilterMode | `FilterMode` | Gets or sets the filter mode. |
| FilterPopupRenderMode | `PopupRenderMode` | Gets or sets the filter popup render mode. |
| FilterText | `string` | Filter text. |
| FooterCellRender | `Action<DataGridCellRenderEventArgs<TItem>>?` | Footer cell render callback. |
| GotoFirstPageOnSort | `bool` | Whether to go to the first page on sort. Default is false. |
| GreaterThanOrEqualsText | `string` | Greater than or equals text. |
| GreaterThanText | `string` | Greater than text. |
| GridLines | `DataGridGridLines` | Gets or sets the grid lines. |
| HeaderCellRender | `Action<DataGridCellRenderEventArgs<TItem>>?` | Header cell render callback. |
| HeaderDateFormat | `string` | Date format for header cells. |
| IdProperty | `string?` | Property name used as unique task id. |
| IsEmptyText | `string` | Is empty text. |
| IsLoading | `bool` | Gets or sets whether loading indicator is shown. |
| IsNotEmptyText | `string` | Is not empty text. |
| IsNotNullText | `string` | Is not null text. |
| IsNullText | `string` | Is null text. |
| LeftPaneWidth | `string` | Width of the left pane (CSS length). |
| LessThanOrEqualsText | `string` | Less than or equals text. |
| LessThanText | `string` | Less than text. |
| LoadSettings | `Action<DataGridLoadSettingsEventArgs>?` | Load settings callback. |
| LogicalFilterOperator | `LogicalFilterOperator` | Gets or sets the logical filter operator. |
| Markers | `IEnumerable<GanttMarker>?` | Optional vertical date markers rendered on the timeline (e.g. deadlines, milestones, releases). |
| NextText | `string` | Tooltip for the "Next" navigation button. Default is "Next". |
| NonWorkingDays | `IEnumerable<DayOfWeek>` | The days of the week considered non-working. Used when is true. Defaults to Saturday and Sunday. |
| NotEqualsText | `string` | Not equals text. |
| OrOperatorText | `string` | Or operator text. |
| PageSize | `int` | Page size when is enabled. |
| ParentIdProperty | `string?` | Property name used as parent task id. |
| PrevText | `string` | Tooltip for the "Previous" navigation button. Default is "Previous". |
| ProgressProperty | `string?` | Property name used for task progress (0..100). Optional. |
| Render | `Action<DataGridRenderEventArgs<TItem>>?` | Render callback. |
| Responsive | `bool` | Gets or sets whether grid is responsive. |
| RowHeightPx | `int` | Row height in pixels. |
| RowRender | `Action<RowRenderEventArgs<TItem>>?` | Row render callback. |
| ShowCellDataAsTooltip | `bool` | Gets or sets whether to show cell data as tooltip. |
| ShowColumnTitleAsTooltip | `bool` | Gets or sets whether to show column title as tooltip. |
| ShowCriticalPath | `bool` | When true, highlights the critical path — the longest chain of dependent tasks that determines the project end date. Requires to be set. |
| ShowEmptyMessage | `bool` | Gets or sets whether empty message is shown. |
| ShowHeader | `bool` | Gets or sets whether header is shown. |
| ShowMultiColumnSortingIndex | `bool` | Shows multi-column sorting index. Default is false. |
| ShowNavigation | `bool` | Shows the unified Gantt navigation header. |
| ShowTodayLine | `bool` | When true, draws a vertical line on the timeline at the current date/time. |
| ShowWeekends | `bool` | When true, shades non-working days (Saturday and Sunday by default) on the timeline. |
| StartProperty | `string?` | Property name used for task start date. |
| StartsWithText | `string` | Starts with text. |
| TaskRender | `Action<GanttBarRenderEventArgs<TItem>>?` | Callback to customize the appearance of each task bar. Set or to change colors or styles per task. |
| TaskTemplate | `RenderFragment<TItem>?` | Custom template for the content rendered inside each task bar. When set, replaces the default progress bar and label. Receives the task data item as context. |
| TextProperty | `string?` | Property name used for task title (shown in the first column when no template is provided). |
| TodayText | `string` | Text for the "Today" navigation button. Default is "Today". |
| ViewEnd | `DateTime?` | Optional explicit timeline end. |
| ViewStart | `DateTime?` | Optional explicit timeline start. |
| VirtualizationOverscanCount | `int` | Gets or sets the virtualization overscan count. |
| WeeksInView | `int` | Number of weeks to render in Week view. |
| ZoomLevel | `GanttZoomLevel` | Timeline zoom. |
| ZoomToFitText | `string` | Tooltip for the "Zoom to fit" navigation button. Default is "Zoom to fit". |

## Events

| Event | Type | Description |
|-------|------|-------------|
| CellClick | `EventCallback<DataGridCellMouseEventArgs<TItem>>` | Cell click callback. |
| CellContextMenu | `EventCallback<DataGridCellMouseEventArgs<TItem>>` | Cell context menu callback. |
| CellDoubleClick | `EventCallback<DataGridCellMouseEventArgs<TItem>>` | Cell double click callback. |
| ColumnReordered | `EventCallback<DataGridColumnReorderedEventArgs<TItem>>` | Column reordered callback. |
| ColumnReordering | `EventCallback<DataGridColumnReorderingEventArgs<TItem>>` | Column reordering callback. |
| ColumnResized | `EventCallback<DataGridColumnResizedEventArgs<TItem>>` | Column resized callback. |
| Filter | `EventCallback<DataGridColumnFilterEventArgs<TItem>>` | Column filter callback. |
| FilterCleared | `EventCallback<DataGridColumnFilterEventArgs<TItem>>` | Column filter cleared callback. |
| KeyDown | `EventCallback<KeyboardEventArgs>` | Key down callback. |
| PageSizeChanged | `EventCallback<int>` | Page size changed callback. |
| PickedColumnsChanged | `EventCallback<DataGridPickedColumnsChangedEventArgs<TItem>>` | Picked columns changed callback. |
| RowClick | `EventCallback<DataGridRowMouseEventArgs<TItem>>` | Row click callback. |
| RowCollapse | `EventCallback<TItem>` | Row collapse callback. |
| RowCreate | `EventCallback<TItem>` | Row create callback. |
| RowDeselect | `EventCallback<TItem>` | Row deselect callback. |
| RowDoubleClick | `EventCallback<DataGridRowMouseEventArgs<TItem>>` | Row double click callback. |
| RowEdit | `EventCallback<TItem>` | Row edit callback. |
| RowExpand | `EventCallback<TItem>` | Row expand callback. |
| RowSelect | `EventCallback<TItem>` | Row select callback. |
| RowUpdate | `EventCallback<TItem>` | Row update callback. |
| SettingsChanged | `EventCallback<DataGridSettings>` | Settings changed callback. |
| Sort | `EventCallback<DataGridColumnSortEventArgs<TItem>>` | Column sort callback. |
| TaskClick | `EventCallback<TItem>` | Raised when a task bar is clicked. |
| TaskMouseEnter | `EventCallback<GanttTaskMouseEventArgs<TItem>>` | Raised when the mouse enters a task bar. Commonly used to show a tooltip. |
| TaskMouseLeave | `EventCallback<GanttTaskMouseEventArgs<TItem>>` | Raised when the mouse leaves a task bar. Commonly used to close a tooltip. |
| TaskMove | `EventCallback<GanttTaskMovedEventArgs<TItem>>` | Raised when a task bar is dragged to a new position on the timeline. The consumer should update the data item's start and end dates. |
| TaskResize | `EventCallback<GanttTaskMovedEventArgs<TItem>>` | Raised when a task bar edge is dragged to resize the task. The consumer should update the data item's start and/or end date. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| CancelEditRow(TItem item) | `void` | Cancels edit mode for the specified item. |
| EditRow(TItem item) | `System.Threading.Tasks.Task` | Puts the specified item in edit mode. |
| ExpandRows(IEnumerable<TItem> items) | `Task` | Expands a range of rows. |
| InsertAfterRow(TItem item, TItem afterItem) | `System.Threading.Tasks.Task` | Inserts a new item after the specified row in edit mode. |
| InsertRow(TItem item) | `System.Threading.Tasks.Task` | Inserts a new item into the grid in edit mode. |
| Reload() | `System.Threading.Tasks.Task` | Reloads the underlying grid data. |
| UpdateRow(TItem item) | `System.Threading.Tasks.Task` | Updates the specified item and exits edit mode. |
| ZoomToFit() | `Task` | Auto-calculates and and selects the best zoom level so that all tasks fit in the visible timeline. |

