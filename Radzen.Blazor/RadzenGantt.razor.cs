using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Displays tasks in a split view: hierarchical rows on the left and a timeline with task bars on the right.
    /// </summary>
    /// <typeparam name="TItem">Task item type.</typeparam>
#if NET6_0_OR_GREATER
    [CascadingTypeParameter(nameof(TItem))]
#endif
    public partial class RadzenGantt<TItem> where TItem : notnull
    {
        private RadzenScheduler<TItem>? scheduler;
        private RadzenGanttDayView<TItem>? dayView;
        private RadzenGanttWeekView<TItem>? weekView;
        private RadzenGanttMonthView<TItem>? monthView;
        private RadzenGanttYearView<TItem>? yearView;
        private GanttZoomLevel? pendingZoomLevel;
        private bool scrollToFirstEvent = true;

        #region Data & Columns

        /// <summary>
        /// Task items.
        /// </summary>
        [Parameter]
        public IEnumerable<TItem>? Data { get; set; }

        /// <summary>
        /// Optional task dependencies to draw connecting lines using object references.
        /// For database scenarios, prefer <see cref="DependencyData"/> with property-name-based binding.
        /// </summary>
        [Parameter]
        public IEnumerable<GanttDependency<TItem>>? Dependencies { get; set; }

        /// <summary>
        /// Collection of dependency items (any POCO with predecessor/successor ID properties).
        /// Use together with <see cref="DependencyFromProperty"/>, <see cref="DependencyToProperty"/>,
        /// and optionally <see cref="DependencyTypeProperty"/>.
        /// When set, takes priority over <see cref="Dependencies"/>.
        /// </summary>
        [Parameter]
        public IEnumerable<object>? DependencyData { get; set; }

        /// <summary>
        /// Property name on <see cref="DependencyData"/> items that holds the predecessor task ID
        /// (must match values of <see cref="IdProperty"/> on the task items).
        /// </summary>
        [Parameter]
        public string? DependencyFromProperty { get; set; }

        /// <summary>
        /// Property name on <see cref="DependencyData"/> items that holds the successor task ID
        /// (must match values of <see cref="IdProperty"/> on the task items).
        /// </summary>
        [Parameter]
        public string? DependencyToProperty { get; set; }

        /// <summary>
        /// Optional property name on <see cref="DependencyData"/> items that holds the dependency type.
        /// When not set, all dependencies default to <see cref="GanttDependencyType.FinishToStart"/>.
        /// </summary>
        [Parameter]
        public string? DependencyTypeProperty { get; set; }

        /// <summary>
        /// Optional columns definition for the left pane.
        /// </summary>
        [Parameter]
        public RenderFragment? Columns { get; set; }

        #endregion

        Func<TItem, object>? idGetter;
        Func<TItem, object?>? parentIdGetter;
        Func<TItem, object>? textGetter;
        Func<TItem, dynamic>? progressGetter;
        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            if (pendingZoomLevel != ZoomLevel)
            {
                pendingZoomLevel = ZoomLevel;
            }

            InvalidateTimelineCache();
            base.OnParametersSet();

            if (idGetter == null && !string.IsNullOrEmpty(IdProperty))
            {
                idGetter = PropertyAccess.Getter<TItem, object>(IdProperty);
            }

            if (parentIdGetter == null && !string.IsNullOrEmpty(ParentIdProperty))
            {
                parentIdGetter = PropertyAccess.Getter<TItem, object>(ParentIdProperty);
            }

            if (textGetter == null && !string.IsNullOrEmpty(TextProperty))
            {
                textGetter = PropertyAccess.Getter<TItem, object>(TextProperty);
            }

            if (progressGetter == null && !string.IsNullOrEmpty(ProgressProperty))
            {
                progressGetter = PropertyAccess.Getter<TItem, object>(ProgressProperty);
            }
        }
        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (Visible && JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.ganttSyncScroll", GetId());
            }

            if (pendingZoomLevel.HasValue && scheduler != null)
            {
                var view = GetViewForZoom(pendingZoomLevel.Value);
                if (view != null)
                {
                    await scheduler.SelectView(view);
                }
                pendingZoomLevel = null;
                InvalidateTimelineCache();
                scrollToFirstEvent = true;
                await InvokeAsync(StateHasChanged);
            }

            if (scrollToFirstEvent && Visible && JSRuntime != null)
            {
                var scrolled = await JSRuntime.InvokeAsync<bool>("Radzen.ganttScrollToFirstEvent", GetId());
                if (scrolled)
                {
                    scrollToFirstEvent = false;
                }
            }
        }
        /// <inheritdoc />
        public override void Dispose()
        {
            if (IsJSRuntimeAvailable && JSRuntime != null)
            {
                JSRuntime.InvokeVoid("Radzen.ganttSyncScrollDispose", GetId()!);
            }

            base.Dispose();
        }

        #region Gantt-specific properties

        /// <summary>
        /// Property name used as unique task id.
        /// </summary>
        [Parameter]
        public string? IdProperty { get; set; }

        /// <summary>
        /// Property name used as parent task id.
        /// </summary>
        [Parameter]
        public string? ParentIdProperty { get; set; }

        /// <summary>
        /// Property name used for task title (shown in the first column when no template is provided).
        /// </summary>
        [Parameter]
        public string? TextProperty { get; set; }

        /// <summary>
        /// Property name used for task start date.
        /// </summary>
        [Parameter]
        public string? StartProperty { get; set; }

        /// <summary>
        /// Property name used for task end date.
        /// </summary>
        [Parameter]
        public string? EndProperty { get; set; }

        /// <summary>
        /// Property name used for task progress (0..100). Optional.
        /// </summary>
        [Parameter]
        public string? ProgressProperty { get; set; }

        /// <summary>
        /// Optional explicit timeline start.
        /// </summary>
        [Parameter]
        public DateTime? ViewStart { get; set; }

        /// <summary>
        /// Optional explicit timeline end.
        /// </summary>
        [Parameter]
        public DateTime? ViewEnd { get; set; }

        /// <summary>
        /// Timeline zoom.
        /// </summary>
        [Parameter]
        public GanttZoomLevel ZoomLevel { get; set; } = GanttZoomLevel.Week;

        /// <summary>
        /// Width of the left pane (CSS length).
        /// </summary>
        [Parameter]
        public string LeftPaneWidth { get; set; } = "50%";

        /// <summary>
        /// Shows the unified Gantt navigation header.
        /// </summary>
        [Parameter]
        public bool ShowNavigation { get; set; } = true;

        /// <summary>
        /// Row height in pixels.
        /// </summary>
        [Parameter]
        public int RowHeightPx { get; set; } = 36;

        /// <summary>
        /// Day cell width in pixels.
        /// </summary>
        [Parameter]
        public int DayWidthPx { get; set; } = 80;

        /// <summary>
        /// Number of weeks to render in Week view.
        /// </summary>
        [Parameter]
        public int WeeksInView { get; set; } = 4;

        /// <summary>
        /// Date format for header cells.
        /// </summary>
        [Parameter]
        public string HeaderDateFormat { get; set; } = "dd MMM";

        /// <summary>
        /// Raised when a task bar is clicked.
        /// </summary>
        [Parameter]
        public EventCallback<TItem> TaskClick { get; set; }

        /// <summary>
        /// Raised when the mouse enters a task bar. Commonly used to show a tooltip.
        /// </summary>
        [Parameter]
        public EventCallback<GanttTaskMouseEventArgs<TItem>> TaskMouseEnter { get; set; }

        /// <summary>
        /// Raised when the mouse leaves a task bar. Commonly used to close a tooltip.
        /// </summary>
        [Parameter]
        public EventCallback<GanttTaskMouseEventArgs<TItem>> TaskMouseLeave { get; set; }

        /// <summary>
        /// Raised when a task bar is dragged to a new position on the timeline.
        /// The consumer should update the data item's start and end dates.
        /// </summary>
        [Parameter]
        public EventCallback<GanttTaskMovedEventArgs<TItem>> TaskMove { get; set; }

        /// <summary>
        /// Raised when a task bar edge is dragged to resize the task.
        /// The consumer should update the data item's start and/or end date.
        /// </summary>
        [Parameter]
        public EventCallback<GanttTaskMovedEventArgs<TItem>> TaskResize { get; set; }

        /// <summary>
        /// Property name for the baseline (planned) start date. When set together with <see cref="BaselineEndProperty"/>,
        /// a secondary bar is rendered behind the actual bar showing planned vs actual.
        /// </summary>
        [Parameter]
        public string? BaselineStartProperty { get; set; }

        /// <summary>
        /// Property name for the baseline (planned) end date.
        /// </summary>
        [Parameter]
        public string? BaselineEndProperty { get; set; }

        /// <summary>
        /// When <c>true</c>, highlights the critical path — the longest chain of dependent tasks
        /// that determines the project end date. Requires <see cref="Dependencies"/> to be set.
        /// </summary>
        [Parameter]
        public bool ShowCriticalPath { get; set; }

        /// <summary>
        /// When <c>true</c>, draws a vertical line on the timeline at the current date/time.
        /// </summary>
        [Parameter]
        public bool ShowTodayLine { get; set; } = true;

        /// <summary>
        /// When <c>true</c>, shades non-working days (Saturday and Sunday by default) on the timeline.
        /// </summary>
        [Parameter]
        public bool ShowWeekends { get; set; } = true;

        /// <summary>
        /// The days of the week considered non-working. Used when <see cref="ShowWeekends"/> is <c>true</c>.
        /// Defaults to Saturday and Sunday.
        /// </summary>
        [Parameter]
        public IEnumerable<DayOfWeek> NonWorkingDays { get; set; } = new[] { DayOfWeek.Saturday, DayOfWeek.Sunday };

        /// <summary>
        /// Optional vertical date markers rendered on the timeline (e.g. deadlines, milestones, releases).
        /// </summary>
        [Parameter]
        public IEnumerable<GanttMarker>? Markers { get; set; }

        /// <summary>
        /// Callback to customize the appearance of each task bar. Set <see cref="GanttBarRenderEventArgs{TItem}.CssClass"/>
        /// or <see cref="GanttBarRenderEventArgs{TItem}.Attributes"/> to change colors or styles per task.
        /// </summary>
        [Parameter]
        public Action<GanttBarRenderEventArgs<TItem>>? TaskRender { get; set; }

        /// <summary>
        /// Custom template for the content rendered inside each task bar. When set, replaces the
        /// default progress bar and label. Receives the task data item as context.
        /// </summary>
        [Parameter]
        public RenderFragment<TItem>? TaskTemplate { get; set; }

        /// <summary>
        /// Text for the "Today" navigation button. Default is <c>"Today"</c>.
        /// </summary>
        [Parameter]
        public string TodayText { get; set; } = "Today";

        /// <summary>
        /// Tooltip for the "Previous" navigation button. Default is <c>"Previous"</c>.
        /// </summary>
        [Parameter]
        public string PrevText { get; set; } = "Previous";

        /// <summary>
        /// Tooltip for the "Next" navigation button. Default is <c>"Next"</c>.
        /// </summary>
        [Parameter]
        public string NextText { get; set; } = "Next";

        /// <summary>
        /// Tooltip for the "Zoom to fit" navigation button. Default is <c>"Zoom to fit"</c>.
        /// </summary>
        [Parameter]
        public string ZoomToFitText { get; set; } = "Zoom to fit";

        #endregion

        #region DataGrid Proxied Properties

        /// <summary>
        /// Enables sorting in the left pane grid. Default is <c>true</c>.
        /// </summary>
        [Parameter]
        public bool AllowSorting { get; set; } = true;

        /// <summary>
        /// Enables multi-column sorting. Default is <c>false</c>.
        /// </summary>
        [Parameter]
        public bool AllowMultiColumnSorting { get; set; }

        /// <summary>
        /// Shows multi-column sorting index. Default is <c>false</c>.
        /// </summary>
        [Parameter]
        public bool ShowMultiColumnSortingIndex { get; set; }

        /// <summary>
        /// Whether to go to the first page on sort. Default is <c>false</c>.
        /// </summary>
        [Parameter]
        public bool GotoFirstPageOnSort { get; set; }

        /// <summary>
        /// Enables filtering in the left pane grid. Default is <c>true</c>.
        /// </summary>
        [Parameter]
        public bool AllowFiltering { get; set; } = true;

        /// <summary>
        /// Gets or sets the filter mode.
        /// </summary>
        [Parameter]
        public FilterMode FilterMode { get; set; } = FilterMode.Advanced;

        /// <summary>
        /// Gets or sets the logical filter operator.
        /// </summary>
        [Parameter]
        public LogicalFilterOperator LogicalFilterOperator { get; set; } = LogicalFilterOperator.And;

        /// <summary>
        /// Gets or sets the filter popup render mode.
        /// </summary>
        [Parameter]
        public PopupRenderMode FilterPopupRenderMode { get; set; } = PopupRenderMode.Initial;

        /// <summary>
        /// Gets or sets the filter case sensitivity.
        /// </summary>
        [Parameter]
        public FilterCaseSensitivity FilterCaseSensitivity { get; set; } = FilterCaseSensitivity.Default;

        /// <summary>
        /// Enables paging in the left pane grid. Default is <c>false</c>.
        /// </summary>
        [Parameter]
        public bool AllowPaging { get; set; }

        /// <summary>
        /// Page size when <see cref="AllowPaging"/> is enabled.
        /// </summary>
        [Parameter]
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// Enables column resizing. Default is <c>false</c>.
        /// </summary>
        [Parameter]
        public bool AllowColumnResize { get; set; }

        /// <summary>
        /// Enables column reordering. Default is <c>false</c>.
        /// </summary>
        [Parameter]
        public bool AllowColumnReorder { get; set; }

        /// <summary>
        /// Enables column picking. Default is <c>false</c>.
        /// </summary>
        [Parameter]
        public bool AllowColumnPicking { get; set; }

        /// <summary>
        /// Enables row virtualization. Default is <c>false</c>.
        /// </summary>
        [Parameter]
        public bool AllowVirtualization { get; set; }

        /// <summary>
        /// Gets or sets the virtualization overscan count.
        /// </summary>
        [Parameter]
        public int VirtualizationOverscanCount { get; set; }

        /// <summary>
        /// Gets or sets the grid lines.
        /// </summary>
        [Parameter]
        public DataGridGridLines GridLines { get; set; } = DataGridGridLines.Default;

        /// <summary>
        /// Gets or sets the density.
        /// </summary>
        [Parameter]
        public Density Density { get; set; } = Density.Default;

        /// <summary>
        /// Gets or sets whether empty message is shown.
        /// </summary>
        [Parameter]
        public bool ShowEmptyMessage { get; set; } = true;

        /// <summary>
        /// Gets or sets the empty text.
        /// </summary>
        [Parameter]
        public string EmptyText { get; set; } = "No records to display.";

        /// <summary>
        /// Gets or sets whether loading indicator is shown.
        /// </summary>
        [Parameter]
        public bool IsLoading { get; set; }

        /// <summary>
        /// Gets or sets whether grid is responsive.
        /// </summary>
        [Parameter]
        public bool Responsive { get; set; }

        /// <summary>
        /// Gets or sets whether header is shown.
        /// </summary>
        [Parameter]
        public bool ShowHeader { get; set; } = true;

        /// <summary>
        /// Gets or sets the edit mode.
        /// </summary>
        [Parameter]
        public DataGridEditMode EditMode { get; set; } = DataGridEditMode.Multiple;

        /// <summary>
        /// Gets or sets the expand mode for hierarchical data.
        /// </summary>
        [Parameter]
        public DataGridExpandMode ExpandMode { get; set; } = DataGridExpandMode.Multiple;

        /// <summary>
        /// Gets or sets whether alternating rows are shown.
        /// </summary>
        [Parameter]
        public bool AllowAlternatingRows { get; set; } = true;

        /// <summary>
        /// Gets or sets whether row selection is allowed on row click.
        /// </summary>
        [Parameter]
        public bool AllowRowSelectOnRowClick { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show cell data as tooltip.
        /// </summary>
        [Parameter]
        public bool ShowCellDataAsTooltip { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show column title as tooltip.
        /// </summary>
        [Parameter]
        public bool ShowColumnTitleAsTooltip { get; set; } = true;

        /// <summary>
        /// Gets or sets whether filter date input is allowed.
        /// </summary>
        [Parameter]
        public bool AllowFilterDateInput { get; set; }

        #endregion

        #region DataGrid Proxied Filter Text

        /// <summary>
        /// Filter text.
        /// </summary>
        [Parameter]
        public string FilterText { get; set; } = "Filter";

        /// <summary>
        /// And operator text.
        /// </summary>
        [Parameter]
        public string AndOperatorText { get; set; } = "And";

        /// <summary>
        /// Or operator text.
        /// </summary>
        [Parameter]
        public string OrOperatorText { get; set; } = "Or";

        /// <summary>
        /// Apply filter text.
        /// </summary>
        [Parameter]
        public string ApplyFilterText { get; set; } = "Apply";

        /// <summary>
        /// Clear filter text.
        /// </summary>
        [Parameter]
        public string ClearFilterText { get; set; } = "Clear";

        /// <summary>
        /// Equals text.
        /// </summary>
        [Parameter]
        public string EqualsText { get; set; } = "Equals";

        /// <summary>
        /// Not equals text.
        /// </summary>
        [Parameter]
        public string NotEqualsText { get; set; } = "Not equals";

        /// <summary>
        /// Less than text.
        /// </summary>
        [Parameter]
        public string LessThanText { get; set; } = "Less than";

        /// <summary>
        /// Less than or equals text.
        /// </summary>
        [Parameter]
        public string LessThanOrEqualsText { get; set; } = "Less than or equals";

        /// <summary>
        /// Greater than text.
        /// </summary>
        [Parameter]
        public string GreaterThanText { get; set; } = "Greater than";

        /// <summary>
        /// Greater than or equals text.
        /// </summary>
        [Parameter]
        public string GreaterThanOrEqualsText { get; set; } = "Greater than or equals";

        /// <summary>
        /// Contains text.
        /// </summary>
        [Parameter]
        public string ContainsText { get; set; } = "Contains";

        /// <summary>
        /// Does not contain text.
        /// </summary>
        [Parameter]
        public string DoesNotContainText { get; set; } = "Does not contain";

        /// <summary>
        /// Starts with text.
        /// </summary>
        [Parameter]
        public string StartsWithText { get; set; } = "Starts with";

        /// <summary>
        /// Ends with text.
        /// </summary>
        [Parameter]
        public string EndsWithText { get; set; } = "Ends with";

        /// <summary>
        /// Is null text.
        /// </summary>
        [Parameter]
        public string IsNullText { get; set; } = "Is null";

        /// <summary>
        /// Is not null text.
        /// </summary>
        [Parameter]
        public string IsNotNullText { get; set; } = "Is not null";

        /// <summary>
        /// Is empty text.
        /// </summary>
        [Parameter]
        public string IsEmptyText { get; set; } = "Is empty";

        /// <summary>
        /// Is not empty text.
        /// </summary>
        [Parameter]
        public string IsNotEmptyText { get; set; } = "Is not empty";

        #endregion

        #region DataGrid Proxied Events

        /// <summary>
        /// Column sort callback.
        /// </summary>
        [Parameter]
        public EventCallback<DataGridColumnSortEventArgs<TItem>> Sort { get; set; }

        /// <summary>
        /// Column filter callback.
        /// </summary>
        [Parameter]
        public EventCallback<DataGridColumnFilterEventArgs<TItem>> Filter { get; set; }

        /// <summary>
        /// Column filter cleared callback.
        /// </summary>
        [Parameter]
        public EventCallback<DataGridColumnFilterEventArgs<TItem>> FilterCleared { get; set; }

        /// <summary>
        /// Column resized callback.
        /// </summary>
        [Parameter]
        public EventCallback<DataGridColumnResizedEventArgs<TItem>> ColumnResized { get; set; }

        /// <summary>
        /// Column reordering callback.
        /// </summary>
        [Parameter]
        public EventCallback<DataGridColumnReorderingEventArgs<TItem>> ColumnReordering { get; set; }

        /// <summary>
        /// Column reordered callback.
        /// </summary>
        [Parameter]
        public EventCallback<DataGridColumnReorderedEventArgs<TItem>> ColumnReordered { get; set; }

        /// <summary>
        /// Row click callback.
        /// </summary>
        [Parameter]
        public EventCallback<DataGridRowMouseEventArgs<TItem>> RowClick { get; set; }

        /// <summary>
        /// Row double click callback.
        /// </summary>
        [Parameter]
        public EventCallback<DataGridRowMouseEventArgs<TItem>> RowDoubleClick { get; set; }

        /// <summary>
        /// Cell click callback.
        /// </summary>
        [Parameter]
        public EventCallback<DataGridCellMouseEventArgs<TItem>> CellClick { get; set; }

        /// <summary>
        /// Cell double click callback.
        /// </summary>
        [Parameter]
        public EventCallback<DataGridCellMouseEventArgs<TItem>> CellDoubleClick { get; set; }

        /// <summary>
        /// Cell context menu callback.
        /// </summary>
        [Parameter]
        public EventCallback<DataGridCellMouseEventArgs<TItem>> CellContextMenu { get; set; }

        /// <summary>
        /// Row select callback.
        /// </summary>
        [Parameter]
        public EventCallback<TItem> RowSelect { get; set; }

        /// <summary>
        /// Row deselect callback.
        /// </summary>
        [Parameter]
        public EventCallback<TItem> RowDeselect { get; set; }

        /// <summary>
        /// Row expand callback.
        /// </summary>
        [Parameter]
        public EventCallback<TItem> RowExpand { get; set; }

        /// <summary>
        /// Row collapse callback.
        /// </summary>
        [Parameter]
        public EventCallback<TItem> RowCollapse { get; set; }

        /// <summary>
        /// Row edit callback.
        /// </summary>
        [Parameter]
        public EventCallback<TItem> RowEdit { get; set; }

        /// <summary>
        /// Row update callback.
        /// </summary>
        [Parameter]
        public EventCallback<TItem> RowUpdate { get; set; }

        /// <summary>
        /// Row create callback.
        /// </summary>
        [Parameter]
        public EventCallback<TItem> RowCreate { get; set; }

        /// <summary>
        /// Key down callback.
        /// </summary>
        [Parameter]
        public EventCallback<KeyboardEventArgs> KeyDown { get; set; }

        /// <summary>
        /// Page size changed callback.
        /// </summary>
        [Parameter]
        public EventCallback<int> PageSizeChanged { get; set; }

        /// <summary>
        /// Picked columns changed callback.
        /// </summary>
        [Parameter]
        public EventCallback<DataGridPickedColumnsChangedEventArgs<TItem>> PickedColumnsChanged { get; set; }

        /// <summary>
        /// Load settings callback.
        /// </summary>
        [Parameter]
        public Action<DataGridLoadSettingsEventArgs>? LoadSettings { get; set; }

        /// <summary>
        /// Settings changed callback.
        /// </summary>
        [Parameter]
        public EventCallback<DataGridSettings> SettingsChanged { get; set; }

        #endregion

        #region DataGrid Proxied Action Parameters

        /// <summary>
        /// Row render callback.
        /// </summary>
        [Parameter]
        public Action<RowRenderEventArgs<TItem>>? RowRender { get; set; }

        /// <summary>
        /// Cell render callback.
        /// </summary>
        [Parameter]
        public Action<DataGridCellRenderEventArgs<TItem>>? CellRender { get; set; }

        /// <summary>
        /// Header cell render callback.
        /// </summary>
        [Parameter]
        public Action<DataGridCellRenderEventArgs<TItem>>? HeaderCellRender { get; set; }

        /// <summary>
        /// Footer cell render callback.
        /// </summary>
        [Parameter]
        public Action<DataGridCellRenderEventArgs<TItem>>? FooterCellRender { get; set; }

        /// <summary>
        /// Render callback.
        /// </summary>
        [Parameter]
        public Action<DataGridRenderEventArgs<TItem>>? Render { get; set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Returns whether the underlying grid has a valid edit state.
        /// </summary>
        public bool IsValid => grid?.IsValid ?? true;

        /// <summary>
        /// Puts the specified item in edit mode.
        /// </summary>
        /// <param name="item">The item to edit.</param>
        public async System.Threading.Tasks.Task EditRow(TItem item)
        {
            if (grid != null)
            {
                await grid.EditRow(item);
            }
        }

        /// <summary>
        /// Updates the specified item and exits edit mode.
        /// </summary>
        /// <param name="item">The item to update.</param>
        public async System.Threading.Tasks.Task UpdateRow(TItem item)
        {
            if (grid != null)
            {
                await grid.UpdateRow(item);
            }
        }

        /// <summary>
        /// Cancels edit mode for the specified item.
        /// </summary>
        /// <param name="item">The item to cancel editing for.</param>
        public void CancelEditRow(TItem item)
        {
            grid?.CancelEditRow(item);
        }

        /// <summary>
        /// Inserts a new item into the grid in edit mode.
        /// </summary>
        /// <param name="item">The item to insert.</param>
        public async System.Threading.Tasks.Task InsertRow(TItem item)
        {
            if (grid != null)
            {
                await grid.InsertRow(item);
            }
        }

        /// <summary>
        /// Inserts a new item after the specified row in edit mode.
        /// </summary>
        /// <param name="item">The new item to insert.</param>
        /// <param name="afterItem">The item after which to insert.</param>
        public async System.Threading.Tasks.Task InsertAfterRow(TItem item, TItem afterItem)
        {
            if (grid != null)
            {
                await grid.InsertAfterRow(item, afterItem);
            }
        }

        /// <summary>
        /// Reloads the underlying grid data.
        /// </summary>
        public async System.Threading.Tasks.Task Reload()
        {
            if (grid != null)
            {
                await grid.Reload();
            }
        }

        #endregion

        #region Internal state

        internal readonly List<RadzenGanttColumn<TItem>> GanttColumns = new();

        internal void AddColumn(RadzenGanttColumn<TItem> column)
        {
            if (!GanttColumns.Contains(column))
            {
                GanttColumns.Add(column);
            }
        }

        internal void RemoveColumn(RadzenGanttColumn<TItem> column)
        {
            GanttColumns.Remove(column);
        }

        internal bool IsTreeColumn(RadzenGanttColumn<TItem> column)
        {
            // First declared RadzenGanttColumn is the tree column.
            return GanttColumns.Count > 0 && ReferenceEquals(GanttColumns[0], column);
        }

        internal sealed record GanttRow(TItem Item, int Level, bool HasChildren, bool IsExpanded);

        internal RadzenDataGrid<TItem>? grid;

        private IReadOnlyList<DateTime>? cachedTimelineDays;
        private int? cachedTimelineRowCount;
        private IReadOnlyDictionary<object, int>? cachedTimelineRowIndex;
        private DateTime? cachedSchedulerDate;
        private Func<TItem, object>? cachedStartGetter;
        private Func<TItem, object>? cachedEndGetter;

        private void InvalidateTimelineCache()
        {
            cachedTimelineDays = null;
            cachedTimelineRowCount = null;
            cachedTimelineRowIndex = null;
            cachedSchedulerDate = null;
        }

        private Func<TItem, object>? GetStartGetter()
        {
            if (cachedStartGetter == null && !string.IsNullOrWhiteSpace(StartProperty))
            {
                cachedStartGetter = PropertyAccess.Getter<TItem, object>(StartProperty);
            }
            return cachedStartGetter;
        }

        private Func<TItem, object>? GetEndGetter()
        {
            if (cachedEndGetter == null && !string.IsNullOrWhiteSpace(EndProperty))
            {
                cachedEndGetter = PropertyAccess.Getter<TItem, object>(EndProperty);
            }
            return cachedEndGetter;
        }

        internal IReadOnlyList<DateTime> TimelineDays => cachedTimelineDays ??= BuildTimelineDays();

        internal int TimelineWidthPx => TimelineDays.Count * DayWidthPx;

        internal int TimelineRowCount => cachedTimelineRowCount ??= (TimelineRowIndexByItem.Count > 0 ? TimelineRowIndexByItem.Count : (grid?.View?.Count() ?? gridCount));

        internal IReadOnlyDictionary<object, int> TimelineRowIndexByItem => cachedTimelineRowIndex ??= BuildTimelineRowIndex();

        internal DateTime SchedulerDate => cachedSchedulerDate ??= GetSchedulerDate();

        internal IEnumerable<GanttDependency<TItem>>? TimelineDependencies => ResolveDependencies();

        internal string? TimelineBaselineStartProperty => BaselineStartProperty;
        internal string? TimelineBaselineEndProperty => BaselineEndProperty;
        internal bool TimelineShowTodayLine => ShowTodayLine;
        internal bool TimelineShowWeekends => ShowWeekends;
        internal IEnumerable<DayOfWeek> TimelineNonWorkingDays => NonWorkingDays;
        internal IEnumerable<GanttMarker>? TimelineMarkers => Markers;
        internal Action<GanttBarRenderEventArgs<TItem>>? TimelineTaskRender => TaskRender;
        internal RenderFragment<TItem>? TimelineTaskTemplate => TaskTemplate;

        internal string RootStyle
        {
            get
            {
                var rowHeight = RowHeightPx.ToString(CultureInfo.InvariantCulture);
                var customVar = $"--rz-gantt-row-height: {rowHeight}px;";
                if (string.IsNullOrWhiteSpace(Style))
                {
                    return customVar;
                }

                return $"{Style}; {customVar}";
            }
        }

        #endregion

        /// <inheritdoc />
        protected override string GetComponentCssClass() => "rz-gantt";

        #region Timeline Building

        private IReadOnlyList<DateTime> BuildTimelineDays()
        {
            var data = pagedData?.ToList() ?? new List<TItem>();
            var sg = GetStartGetter();
            var eg = GetEndGetter();
            if (data.Count == 0 || sg == null || eg == null)
            {
                return Array.Empty<DateTime>();
            }

            DateTime? min = ViewStart;
            DateTime? max = ViewEnd;

            if (min == null || max == null)
            {
                foreach (var item in data)
                {
                    var s = ToDateTime(sg(item));
                    var e = ToDateTime(eg(item));
                    if (s.HasValue)
                    {
                        min = min.HasValue ? (s.Value < min.Value ? s.Value : min.Value) : s.Value;
                    }
                    if (e.HasValue)
                    {
                        max = max.HasValue ? (e.Value > max.Value ? e.Value : max.Value) : e.Value;
                    }
                }
            }

            if (!min.HasValue || !max.HasValue)
            {
                return Array.Empty<DateTime>();
            }

            var start = min.Value.Date;
            var end = max.Value.Date;
            if (end < start) (start, end) = (end, start);

            var pad = ZoomLevel switch
            {
                GanttZoomLevel.Day => 2,
                GanttZoomLevel.Week => 7,
                GanttZoomLevel.Month => 14,
                GanttZoomLevel.Year => 30,
                _ => 14
            };
            start = start.AddDays(-pad);
            end = end.AddDays(pad);

            var days = new List<DateTime>();
            for (var d = start; d <= end; d = d.AddDays(1))
            {
                days.Add(d);
            }
            return days;
        }

        private IReadOnlyDictionary<object, int> BuildTimelineRowIndex()
        {
            var map = new Dictionary<object, int>();
            var index = 0;
            foreach (var item in grid?.View ?? Enumerable.Empty<TItem>())
            {
                if (item != null)
                {
                    map[item] = index;
                }
                index++;
            }
            return map;
        }

        private DateTime GetSchedulerDate()
        {
            if (ViewStart.HasValue)
            {
                return ViewStart.Value.Date;
            }

            var sg = GetStartGetter();
            if (Data == null || sg == null)
            {
                return DateTime.Today;
            }

            DateTime? min = null;
            foreach (var item in pagedData ?? Enumerable.Empty<TItem>())
            {
                var start = ToDateTime(sg(item));
                if (start.HasValue)
                {
                    min = min.HasValue ? (start.Value < min.Value ? start.Value : min.Value) : start.Value;
                }
            }

            return (min ?? DateTime.Today).Date;
        }

        private ISchedulerView? GetViewForZoom(GanttZoomLevel zoomLevel)
        {
            return zoomLevel switch
            {
                GanttZoomLevel.Day => dayView,
                GanttZoomLevel.Month => monthView,
                GanttZoomLevel.Year => yearView,
                _ => weekView
            };
        }

        private static DateTime? ToDateTime(object? value)
        {
            if (value == null) return null;
            if (value is DateTime dt) return dt;
            if (value is DateTimeOffset dto) return dto.DateTime;
            if (value is string s && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            {
                return parsed;
            }
            return null;
        }

        #endregion

        #region Dependencies

        private IEnumerable<GanttDependency<TItem>> ResolveDependencies()
        {
            if (DependencyData != null && !string.IsNullOrEmpty(DependencyFromProperty) && !string.IsNullOrEmpty(DependencyToProperty))
            {
                return ResolveDependencyData();
            }

            return Dependencies ?? BuildDefaultDependencies();
        }

        private IEnumerable<GanttDependency<TItem>> ResolveDependencyData()
        {
            if (idGetter == null || Data == null || DependencyData == null)
            {
                return Enumerable.Empty<GanttDependency<TItem>>();
            }

            var taskById = new Dictionary<object, TItem>();
            foreach (var item in Data)
            {
                var id = idGetter(item);
                if (id != null)
                {
                    taskById[id] = item;
                }
            }

            Func<object, object>? fromGetter = null;
            Func<object, object>? toGetter = null;
            Func<object, object>? typeGetter = null;

            var links = new List<GanttDependency<TItem>>();
            foreach (var dep in DependencyData)
            {
                if (dep == null) continue;

                fromGetter ??= PropertyAccess.Getter<object>(dep, DependencyFromProperty!);
                toGetter ??= PropertyAccess.Getter<object>(dep, DependencyToProperty!);
                if (!string.IsNullOrEmpty(DependencyTypeProperty))
                {
                    typeGetter ??= PropertyAccess.Getter<object>(dep, DependencyTypeProperty);
                }

                var fromId = fromGetter(dep);
                var toId = toGetter(dep);
                if (fromId == null || toId == null) continue;

                if (!taskById.TryGetValue(fromId, out var fromTask) || !taskById.TryGetValue(toId, out var toTask))
                {
                    continue;
                }

                var type = GanttDependencyType.FinishToStart;
                if (typeGetter != null)
                {
                    var rawType = typeGetter(dep);
                    if (rawType is GanttDependencyType gdt)
                    {
                        type = gdt;
                    }
                    else if (rawType != null && Enum.TryParse<GanttDependencyType>(rawType.ToString(), out var parsed))
                    {
                        type = parsed;
                    }
                }

                links.Add(new GanttDependency<TItem> { From = fromTask, To = toTask, Type = type });
            }

            return links;
        }

        private IEnumerable<GanttDependency<TItem>> BuildDefaultDependencies()
        {
            if (idGetter == null || parentIdGetter == null || Data == null)
            {
                return Enumerable.Empty<GanttDependency<TItem>>();
            }

            var map = new Dictionary<object, TItem>();
            foreach (var item in Data)
            {
                var id = idGetter(item);
                if (id != null)
                {
                    map[id] = item;
                }
            }

            var links = new List<GanttDependency<TItem>>();
            foreach (var item in Data)
            {
                var parentId = parentIdGetter(item);
                if (parentId == null)
                {
                    continue;
                }

                if (map.TryGetValue(parentId, out var parent))
                {
                    links.Add(new GanttDependency<TItem> { From = parent, To = item });
                }
            }

            return links;
        }

        #endregion

        #region Critical Path

        internal HashSet<object>? CriticalPathItems { get; private set; }

        internal void ComputeCriticalPath(IEnumerable<AppointmentData>? appointments, IReadOnlyDictionary<object, int>? rowIndexByItem)
        {
            CriticalPathItems = null;

            var startGet = GetStartGetter();
            var endGet = GetEndGetter();

            var resolvedDeps = ResolveDependencies();
            if (!ShowCriticalPath || resolvedDeps == null || !resolvedDeps.Any() || appointments == null || startGet == null || endGet == null)
            {
                return;
            }

            var items = new List<TItem>();
            var itemSet = new HashSet<object>();
            foreach (var a in appointments)
            {
                if (a.Data is TItem t)
                {
                    items.Add(t);
                    itemSet.Add(t);
                }
            }

            if (items.Count == 0) return;

            var deps = resolvedDeps
                .Where(d => d != null && itemSet.Contains(d.From) && itemSet.Contains(d.To))
                .ToList();

            var successors = new Dictionary<object, List<(TItem To, GanttDependencyType Type)>>();
            var predecessors = new Dictionary<object, List<(TItem From, GanttDependencyType Type)>>();
            foreach (var item in items)
            {
                successors[item] = new List<(TItem, GanttDependencyType)>();
                predecessors[item] = new List<(TItem, GanttDependencyType)>();
            }
            foreach (var d in deps)
            {
                successors[d.From].Add((d.To, d.Type));
                predecessors[d.To].Add((d.From, d.Type));
            }

            var es = new Dictionary<object, double>();
            var ef = new Dictionary<object, double>();
            var ls = new Dictionary<object, double>();
            var lf = new Dictionary<object, double>();
            var dur = new Dictionary<object, double>();

            DateTime refDate = DateTime.MaxValue;
            foreach (var t in items)
            {
                var s = ToDateTime(startGet(t));
                if (s.HasValue && s.Value < refDate) refDate = s.Value;
            }

            foreach (var t in items)
            {
                var s = ToDateTime(startGet(t)) ?? refDate;
                var e = ToDateTime(endGet(t)) ?? s;
                dur[t] = Math.Max(0, (e - s).TotalDays);
                es[t] = (s - refDate).TotalDays;
                ef[t] = es[t] + dur[t];
            }

            // Forward pass
            bool changed = true;
            for (int iter = 0; iter < items.Count && changed; iter++)
            {
                changed = false;
                foreach (var t in items)
                {
                    foreach (var (from, type) in predecessors[t])
                    {
                        double constraint = type switch
                        {
                            GanttDependencyType.FinishToStart => ef[from],
                            GanttDependencyType.StartToStart => es[from],
                            GanttDependencyType.FinishToFinish => ef[from] - dur[t],
                            GanttDependencyType.StartToFinish => es[from] - dur[t],
                            _ => ef[from]
                        };
                        if (constraint > es[t])
                        {
                            es[t] = constraint;
                            ef[t] = es[t] + dur[t];
                            changed = true;
                        }
                    }
                }
            }

            double projectEnd = items.Max(t => ef[t]);

            // Backward pass
            foreach (var t in items)
            {
                lf[t] = projectEnd;
                ls[t] = lf[t] - dur[t];
            }

            changed = true;
            for (int iter = 0; iter < items.Count && changed; iter++)
            {
                changed = false;
                foreach (var t in items)
                {
                    foreach (var (to, type) in successors[t])
                    {
                        double constraint = type switch
                        {
                            GanttDependencyType.FinishToStart => ls[to],
                            GanttDependencyType.StartToStart => ls[to] + dur[t],
                            GanttDependencyType.FinishToFinish => lf[to],
                            GanttDependencyType.StartToFinish => lf[to] + dur[t],
                            _ => ls[to]
                        };
                        if (type == GanttDependencyType.FinishToStart || type == GanttDependencyType.FinishToFinish)
                        {
                            if (constraint < lf[t])
                            {
                                lf[t] = constraint;
                                ls[t] = lf[t] - dur[t];
                                changed = true;
                            }
                        }
                        else
                        {
                            if (constraint - dur[t] < ls[t])
                            {
                                ls[t] = constraint - dur[t];
                                lf[t] = ls[t] + dur[t];
                                changed = true;
                            }
                        }
                    }
                }
            }

            const double tolerance = 0.001;
            CriticalPathItems = new HashSet<object>();
            foreach (var t in items)
            {
                var slack = ls[t] - es[t];
                if (Math.Abs(slack) < tolerance)
                {
                    CriticalPathItems.Add(t);
                }
            }
        }

        #endregion

        #region Task Bar Rendering

        internal RenderFragment RenderTaskBar(TItem item) => builder =>
        {
            var sg = GetStartGetter();
            var eg = GetEndGetter();
            if (sg == null || eg == null || TimelineDays.Count == 0)
            {
                return;
            }

            var start = ToDateTime(sg(item));
            var end = ToDateTime(eg(item));

            if (!start.HasValue || !end.HasValue)
            {
                return;
            }

            var rangeStart = TimelineDays[0];
            var rangeEnd = TimelineDays[^1];

            var s = start.Value.Date;
            var e = end.Value.Date;
            if (e < s) (s, e) = (e, s);

            if (e < rangeStart || s > rangeEnd)
            {
                return;
            }

            var clampedStart = s < rangeStart ? rangeStart : s;
            var clampedEnd = e > rangeEnd ? rangeEnd : e;

            var leftDays = (clampedStart - rangeStart).TotalDays;
            var widthDays = Math.Max(1, (clampedEnd - clampedStart).TotalDays + 1);

            var leftPx = (int)(leftDays * DayWidthPx);
            var widthPx = (int)(widthDays * DayWidthPx);

            var progress = progressGetter != null ? progressGetter(item) : null;
            var progressWidth = progress != null ? Math.Max(0, Math.Min(100, progress)) : (double?)null;

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "rz-gantt-bar");
            builder.AddAttribute(2, "style", $"left:{leftPx}px;width:{widthPx}px;");
            builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, () => TaskClick.InvokeAsync(item)));

            if (progressWidth != null)
            {
                builder.OpenElement(4, "div");
                builder.AddAttribute(5, "class", "rz-gantt-bar-progress");
                builder.AddAttribute(6, "style", $"width:{progressWidth.ToString(CultureInfo.InvariantCulture)}%;");
                builder.CloseElement();
            }

            builder.OpenElement(7, "div");
            builder.AddAttribute(8, "class", "rz-gantt-bar-label");
            builder.AddContent(9, textGetter != null ? textGetter(item) : "");
            builder.CloseElement();

            builder.CloseElement();
        };

        #endregion

        #region Navigation helpers

        internal IEnumerable<ISchedulerView> GanttViews
        {
            get
            {
                if (dayView != null) yield return dayView;
                if (weekView != null) yield return weekView;
                if (monthView != null) yield return monthView;
                if (yearView != null) yield return yearView;
            }
        }

        internal string SchedulerTitle => scheduler?.SelectedView?.Title ?? string.Empty;
        internal string SchedulerTodayText => TodayText;
        internal string SchedulerPrevText => PrevText;
        internal string SchedulerNextText => NextText;
        internal bool IsNavigationDisabled => scheduler?.SelectedView == null;

        internal string GetViewButtonCssClass(ISchedulerView view)
        {
            var isActive = scheduler?.SelectedView == view;
            return isActive ? " rz-state-active" : string.Empty;
        }

        internal async Task OnGanttChangeView(ISchedulerView view)
        {
            if (scheduler == null)
            {
                return;
            }

            InvalidateTimelineCache();
            scrollToFirstEvent = true;
            await scheduler.SelectView(view);
            await InvokeAsync(StateHasChanged);
        }

        internal async Task OnGanttPrev()
        {
            if (scheduler?.SelectedView == null)
            {
                return;
            }

            InvalidateTimelineCache();
            scheduler.CurrentDate = scheduler.SelectedView.Prev();
            await scheduler.Reload();
            await InvokeAsync(StateHasChanged);
        }

        internal async Task OnGanttNext()
        {
            if (scheduler?.SelectedView == null)
            {
                return;
            }

            InvalidateTimelineCache();
            scheduler.CurrentDate = scheduler.SelectedView.Next();
            await scheduler.Reload();
            await InvokeAsync(StateHasChanged);
        }

        internal async Task OnGanttToday()
        {
            if (scheduler == null)
            {
                return;
            }

            InvalidateTimelineCache();
            scheduler.CurrentDate = DateTime.Today;
            await scheduler.Reload();
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Auto-calculates <see cref="ViewStart"/> and <see cref="ViewEnd"/> and selects the best zoom level
        /// so that all tasks fit in the visible timeline.
        /// </summary>
        public async Task ZoomToFit()
        {
            if (scheduler == null) return;

            var data = (IEnumerable<TItem>?)grid?.View ?? Data ?? Enumerable.Empty<TItem>();
            DateTime? min = null, max = null;

            var sg = GetStartGetter();
            var eg = GetEndGetter();
            if (sg == null || eg == null) return;

            foreach (var item in data)
            {
                var s = ToDateTime(sg(item));
                var e = ToDateTime(eg(item));
                if (s.HasValue)
                {
                    min = min.HasValue ? (s.Value < min.Value ? s.Value : min.Value) : s.Value;
                }
                if (e.HasValue)
                {
                    max = max.HasValue ? (e.Value > max.Value ? e.Value : max.Value) : e.Value;
                }
            }

            if (!min.HasValue || !max.HasValue) return;

            var span = max.Value - min.Value;
            GanttZoomLevel bestZoom;
            if (span.TotalDays <= 2)
                bestZoom = GanttZoomLevel.Day;
            else if (span.TotalDays <= 60)
                bestZoom = GanttZoomLevel.Week;
            else if (span.TotalDays <= 180)
                bestZoom = GanttZoomLevel.Month;
            else
                bestZoom = GanttZoomLevel.Year;

            var pad = bestZoom switch
            {
                GanttZoomLevel.Day => 1,
                GanttZoomLevel.Week => 3,
                GanttZoomLevel.Month => 7,
                GanttZoomLevel.Year => 14,
                _ => 3
            };

            ViewStart = min.Value.Date.AddDays(-pad);
            ViewEnd = max.Value.Date.AddDays(pad);
            ZoomLevel = bestZoom;
            InvalidateTimelineCache();

            var view = GetViewForZoom(bestZoom);
            if (view != null)
            {
                scheduler.CurrentDate = min.Value.Date;
                await scheduler.SelectView(view);
            }
            await scheduler.Reload();
            await InvokeAsync(StateHasChanged);
        }

        internal async Task OnGanttZoomToFit()
        {
            await ZoomToFit();
        }

        #endregion

        #region Task Mouse Event Handlers

        internal string? GanttElementId => GetId();

        internal async Task HandleTaskBarInteraction(AppointmentData appointment, string mode, DateTime newStart, DateTime newEnd)
        {
            if (appointment.Data is TItem item)
            {
                var args = new GanttTaskMovedEventArgs<TItem> { Data = item, NewStart = newStart, NewEnd = newEnd };
                if (mode == "move")
                {
                    await TaskMove.InvokeAsync(args);
                }
                else
                {
                    await TaskResize.InvokeAsync(args);
                }

                InvalidateTimelineCache();
                await InvokeAsync(StateHasChanged);
            }
        }

        internal void OnTaskMouseEnter(SchedulerAppointmentMouseEventArgs<TItem> args)
        {
            TaskMouseEnter.InvokeAsync(new GanttTaskMouseEventArgs<TItem> { Element = args.Element, Data = args.Data, ClientX = args.ClientX, ClientY = args.ClientY });
        }

        internal void OnTaskMouseLeave(SchedulerAppointmentMouseEventArgs<TItem> args)
        {
            TaskMouseLeave.InvokeAsync(new GanttTaskMouseEventArgs<TItem> { Element = args.Element, Data = args.Data, ClientX = args.ClientX, ClientY = args.ClientY });
        }

        #endregion

        #region DataGrid Event Handlers

        private void OnGridRowRender(RowRenderEventArgs<TItem> args)
        {
            if (!string.IsNullOrWhiteSpace(IdProperty) && !string.IsNullOrWhiteSpace(ParentIdProperty))
            {
                if (idGetter != null)
                {
                    var query = (Data ?? Enumerable.Empty<TItem>()).AsQueryable()
                        .Where($"it => it.{ParentIdProperty} == @0", new object[] { idGetter(args.Data!) });

                    if (visibleIds != null)
                    {
                        query = query.Where(item => visibleIds.Contains(idGetter(item)));
                    }

                    args.Expandable = query.Any();
                }
            }

            // Set row height
            args.Attributes["style"] = $"height:{RowHeightPx}px;";
            
            // Also invoke user's RowRender if provided
            RowRender?.Invoke(args);
        }

        private async Task OnGridRowExpand(TItem item)
        {
            InvalidateTimelineCache();
            await RowExpand.InvokeAsync(item);
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnGridRowCollapse(TItem item)
        {
            InvalidateTimelineCache();
            await RowCollapse.InvokeAsync(item);
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnGridSort(DataGridColumnSortEventArgs<TItem> args)
        {
            await Sort.InvokeAsync(args);
            InvalidateTimelineCache();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnGridFilter(DataGridColumnFilterEventArgs<TItem> args)
        {
            await Filter.InvokeAsync(args);
            InvalidateTimelineCache();
            await InvokeAsync(StateHasChanged);
        }

        private async Task OnGridFilterCleared(DataGridColumnFilterEventArgs<TItem> args)
        {
            await FilterCleared.InvokeAsync(args);
            InvalidateTimelineCache();
            await InvokeAsync(StateHasChanged);
        }

        IEnumerable<TItem>? pagedData;
        int gridCount;
        string? currentFilter;
        HashSet<object>? visibleIds;

        private void ComputeVisibleIds(string filter)
        {
            if (idGetter == null || parentIdGetter == null)
            {
                visibleIds = null;
                return;
            }

            var allData = (Data ?? Enumerable.Empty<TItem>()).AsQueryable();

            var parentLookup = new Dictionary<object, object?>();
            foreach (var item in allData)
            {
                var id = idGetter(item);
                if (id != null)
                {
                    parentLookup[id] = parentIdGetter(item);
                }
            }

            var matchingItems = allData.Where(filter);

            visibleIds = new HashSet<object>();
            foreach (var item in matchingItems)
            {
                var currentId = idGetter(item);
                while (currentId != null && visibleIds.Add(currentId))
                {
                    if (parentLookup.TryGetValue(currentId, out var parentId) && parentId != null)
                    {
                        currentId = parentId;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private async Task OnGridLoadData(LoadDataArgs args)
        {
            currentFilter = args.Filter;

            var query = (Data ?? Enumerable.Empty<TItem>()).AsQueryable();

            if (!string.IsNullOrWhiteSpace(ParentIdProperty))
            {
                query = query.Where($"it => it.{ParentIdProperty} == null");
            }

            if (!string.IsNullOrWhiteSpace(args.Filter))
            {
                ComputeVisibleIds(args.Filter);

                if (visibleIds != null)
                {
                    query = query.Where(item => visibleIds.Contains(idGetter!(item)));
                }
            }
            else
            {
                visibleIds = null;
            }

            if (!string.IsNullOrWhiteSpace(args.OrderBy))
            {
                query = query.OrderBy(args.OrderBy);
            }

            gridCount = query.Count();

            if (args.Skip.HasValue)
            {
                query = query.Skip(args.Skip.Value);
            }

            if (args.Top.HasValue)
            {
                query = query.Take(args.Top.Value);
            }

            pagedData = query.ToList();

            InvalidateTimelineCache();
            await InvokeAsync(StateHasChanged);
        }

        async Task OnGridLoadChildData(DataGridLoadChildDataEventArgs<TItem> args)
        {
            var query = (Data ?? Enumerable.Empty<TItem>()).AsQueryable();

            if (!string.IsNullOrWhiteSpace(IdProperty) && !string.IsNullOrWhiteSpace(ParentIdProperty))
            {
                if (idGetter != null)
                {
                    query = query.Where($"it => it.{ParentIdProperty} == @0", new object[] { idGetter(args.Item!)! });
                }
            }

            if (visibleIds != null && idGetter != null)
            {
                query = query.Where(item => visibleIds.Contains(idGetter(item)));
            }

            args.Data = query;
        }

        /// <summary>
        /// Expands a range of rows.
        /// </summary>
        /// <param name="items">The range of rows.</param>
        public async Task ExpandRows(IEnumerable<TItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            if (grid != null)
            {
                await grid.ExpandRows(items);
            }
        }

        #endregion
    }
}
