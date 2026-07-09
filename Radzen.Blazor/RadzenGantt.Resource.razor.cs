using Microsoft.AspNetCore.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenGantt<TItem>
    {
        #region Resource View Parameters

        /// <summary>
        /// Gets or sets the view mode. Defaults to <see cref="GanttViewMode.Task"/>.
        /// When set to <see cref="GanttViewMode.Resource"/>, rows represent resources and
        /// task bars are grouped by their assigned resource.
        /// </summary>
        [Parameter]
        public GanttViewMode ViewMode { get; set; } = GanttViewMode.Task;

        /// <summary>
        /// Collection of resource items. Each item can be any POCO — properties are accessed
        /// via <see cref="ResourceIdProperty"/> and <see cref="ResourceTextProperty"/>.
        /// Only used when <see cref="ViewMode"/> is <see cref="GanttViewMode.Resource"/>.
        /// </summary>
        [Parameter]
        public IEnumerable? ResourceData { get; set; }

        /// <summary>
        /// Property name on <see cref="ResourceData"/> items that holds the unique resource identifier.
        /// Must match values of <see cref="TaskResourceIdProperty"/> on the task items.
        /// </summary>
        [Parameter]
        public string? ResourceIdProperty { get; set; }

        /// <summary>
        /// Property name on <see cref="ResourceData"/> items used for hierarchical parent-child relationships.
        /// When set, resources are displayed in a tree structure. Tasks of collapsed descendant resources
        /// are rolled up onto the nearest visible ancestor row.
        /// </summary>
        [Parameter]
        public string? ResourceParentIdProperty { get; set; }

        /// <summary>
        /// Property name on <see cref="ResourceData"/> items used for the resource display text.
        /// Used as the default column when <see cref="ResourceColumns"/> is not set.
        /// </summary>
        [Parameter]
        public string? ResourceTextProperty { get; set; }

        /// <summary>
        /// Property name on task items (<typeparamref name="TItem"/>) that holds the assigned resource identifier.
        /// The value must match <see cref="ResourceIdProperty"/> values on <see cref="ResourceData"/> items.
        /// </summary>
        [Parameter]
        public string? TaskResourceIdProperty { get; set; }

        /// <summary>
        /// Optional columns definition for the resource grid in the left pane.
        /// Use <see cref="RadzenDataGridColumn{TItem}"/> with <c>TItem="object"</c>.
        /// Only used when <see cref="ViewMode"/> is <see cref="GanttViewMode.Resource"/>.
        /// </summary>
        [Parameter]
        public RenderFragment? ResourceColumns { get; set; }

        /// <summary>
        /// When enabled, tasks whose resource identifier is <c>null</c> or does not match any
        /// <see cref="ResourceData"/> item are shown in an "Unassigned" row at the bottom.
        /// The row item is a <see cref="GanttUnassignedResource"/> instance — custom
        /// <see cref="ResourceColumns"/> templates can type-check for it.
        /// Only used when <see cref="ViewMode"/> is <see cref="GanttViewMode.Resource"/>.
        /// </summary>
        [Parameter]
        public bool ShowUnassignedTasks { get; set; }

        private string? unassignedText;

        /// <summary>
        /// Gets or sets the display text of the unassigned tasks row. Defaults to "Unassigned".
        /// </summary>
        [Parameter]
        public string UnassignedText { get => unassignedText ?? Localize(nameof(RadzenStrings.Gantt_UnassignedText)); set => unassignedText = value; }

        /// <summary>
        /// When enabled, an accumulated workload histogram is displayed below the timeline,
        /// synchronized with its horizontal scroll. Each timeline column shows the total of
        /// <see cref="HistogramValueProperty"/> over the tasks active in that time unit,
        /// or the task count when no property is set.
        /// </summary>
        [Parameter]
        public bool ShowHistogram { get; set; }

        /// <summary>
        /// Optional numeric property name on task items accumulated by the histogram.
        /// When not set, the histogram counts active tasks per time unit.
        /// </summary>
        [Parameter]
        public string? HistogramValueProperty { get; set; }

        /// <summary>
        /// Optional capacity threshold for the histogram. A capacity line is drawn at this value
        /// and columns exceeding it are highlighted as overallocated.
        /// </summary>
        [Parameter]
        public double? HistogramCapacity { get; set; }

        private string? histogramLabel;

        /// <summary>
        /// Gets or sets the accessible label of the histogram. Defaults to "Workload".
        /// </summary>
        [Parameter]
        public string HistogramLabel { get => histogramLabel ?? Localize(nameof(RadzenStrings.Gantt_HistogramLabel)); set => histogramLabel = value; }

        #endregion

        #region Resource View State

        internal RadzenDataGrid<object>? resourceGrid;
        private IEnumerable<object>? resourcePagedData;
        private int resourceGridCount;
        private List<TItem>? resourceModeTasks;

        private Func<object, object>? resourceIdGetter;
        private Func<object, object>? resourceParentIdGetter;
        private Func<object, object>? resourceTextGetter;
        private Func<TItem, object>? taskResourceIdGetter;
        private Func<TItem, object>? histogramTaskValueGetter;
        private Func<object, double>? timelineHistogramValueGetter;

        internal bool IsResourceMode => ViewMode == GanttViewMode.Resource;

        internal Dictionary<object, int>? LaneByItem { get; private set; }
        internal Dictionary<int, int>? LaneCountByRow { get; private set; }

        private static readonly object UnassignedKey = new object();
        private GanttUnassignedResource? unassignedResource;
        private bool hasUnassignedRow;
        private readonly HashSet<object> expandedResourceIds = new();

        private Dictionary<object, List<TItem>>? tasksByResourceId;
        private List<TItem> unassignedTasks = new();
        private Dictionary<object, object>? resourceById;
        private Dictionary<object, object>? resourceParentById;
        private Dictionary<object, List<object>>? childResourcesByParentId;
        private HashSet<object>? allResources;
        private List<object>? resourceDisplayRows;

        internal string GridPaneStyle => ShowHistogram
            ? "height:calc(100% - var(--rz-gantt-histogram-height, 3.5rem));width:100%;"
            : "height:100%;width:100%;";

        internal Func<object, double>? TimelineHistogramValueGetter
        {
            get
            {
                if (histogramTaskValueGetter == null || string.IsNullOrWhiteSpace(HistogramValueProperty))
                {
                    return null;
                }

                timelineHistogramValueGetter ??= item =>
                {
                    if (item is TItem task)
                    {
                        var value = histogramTaskValueGetter(task);
                        if (value != null)
                        {
                            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
                        }
                    }
                    return 0;
                };

                return timelineHistogramValueGetter;
            }
        }

        private void InitResourceGetters()
        {
            if (ResourceData != null)
            {
                foreach (var item in ResourceData)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    if (resourceIdGetter == null && !string.IsNullOrEmpty(ResourceIdProperty))
                    {
                        resourceIdGetter = PropertyAccess.Getter<object>(item, ResourceIdProperty);
                    }

                    if (resourceParentIdGetter == null && !string.IsNullOrEmpty(ResourceParentIdProperty))
                    {
                        resourceParentIdGetter = PropertyAccess.Getter<object>(item, ResourceParentIdProperty);
                    }

                    if (resourceTextGetter == null && !string.IsNullOrEmpty(ResourceTextProperty))
                    {
                        resourceTextGetter = PropertyAccess.Getter<object>(item, ResourceTextProperty);
                    }

                    break;
                }
            }

            if (taskResourceIdGetter == null && !string.IsNullOrEmpty(TaskResourceIdProperty))
            {
                taskResourceIdGetter = PropertyAccess.Getter<TItem, object>(TaskResourceIdProperty);
            }

            if (histogramTaskValueGetter == null && !string.IsNullOrEmpty(HistogramValueProperty))
            {
                histogramTaskValueGetter = PropertyAccess.Getter<TItem, object>(HistogramValueProperty);
            }
        }

        private void BuildResourceMaps()
        {
            resourceById = new Dictionary<object, object>();
            resourceParentById = new Dictionary<object, object>();
            childResourcesByParentId = new Dictionary<object, List<object>>();
            allResources = new HashSet<object>(ReferenceEqualityComparer.Instance);

            if (ResourceData == null || resourceIdGetter == null)
            {
                return;
            }

            foreach (var resource in ResourceData)
            {
                if (resource == null)
                {
                    continue;
                }

                allResources.Add(resource);

                var id = resourceIdGetter(resource);
                if (id == null)
                {
                    continue;
                }

                resourceById[id] = resource;

                if (resourceParentIdGetter != null && !string.IsNullOrWhiteSpace(ResourceParentIdProperty))
                {
                    var parentId = resourceParentIdGetter(resource);
                    if (parentId != null)
                    {
                        resourceParentById[id] = parentId;
                        if (!childResourcesByParentId.TryGetValue(parentId, out var children))
                        {
                            children = new List<object>();
                            childResourcesByParentId[parentId] = children;
                        }
                        children.Add(resource);
                    }
                }
            }
        }

        internal IEnumerable<TItem>? GetResourceModeTasks()
        {
            return resourceModeTasks;
        }

        private void ComputeResourceModeTasks()
        {
            unassignedTasks = new List<TItem>();

            if (taskResourceIdGetter == null || resourceIdGetter == null || resourcePagedData == null)
            {
                resourceModeTasks = null;
                tasksByResourceId = null;
                return;
            }

            var visibleRootIds = new HashSet<object>();
            foreach (var resource in resourcePagedData)
            {
                if (resource == null || resource is GanttUnassignedResource || allResources == null || !allResources.Contains(resource))
                {
                    continue;
                }

                var id = resourceIdGetter(resource);
                if (id != null)
                {
                    visibleRootIds.Add(id);
                }
            }

            tasksByResourceId = new Dictionary<object, List<TItem>>();
            resourceModeTasks = new List<TItem>();

            foreach (var task in Data ?? Enumerable.Empty<TItem>())
            {
                var resId = taskResourceIdGetter(task);
                if (resId == null || resourceById == null || !resourceById.ContainsKey(resId))
                {
                    if (ShowUnassignedTasks)
                    {
                        unassignedTasks.Add(task);
                        resourceModeTasks.Add(task);
                    }
                    continue;
                }

                if (!tasksByResourceId.TryGetValue(resId, out var list))
                {
                    list = new List<TItem>();
                    tasksByResourceId[resId] = list;
                }
                list.Add(task);

                var rootId = resId;
                while (resourceParentById != null && resourceParentById.TryGetValue(rootId, out var parentId))
                {
                    rootId = parentId;
                }

                if (visibleRootIds.Contains(rootId))
                {
                    resourceModeTasks.Add(task);
                }
            }
        }

        private List<TItem> GetTasksForResource(object resourceId)
        {
            if (tasksByResourceId != null && tasksByResourceId.TryGetValue(resourceId, out var list))
            {
                return list;
            }
            return new List<TItem>();
        }

        private List<object> GetChildResources(object resourceId)
        {
            if (childResourcesByParentId != null && childResourcesByParentId.TryGetValue(resourceId, out var children))
            {
                return children;
            }
            return new List<object>();
        }

        private IReadOnlyDictionary<object, int> BuildResourceModeRowIndex()
        {
            var map = new Dictionary<object, int>();
            var rows = new List<object>();
            resourceDisplayRows = rows;

            if (resourceIdGetter == null || taskResourceIdGetter == null)
            {
                foreach (var resource in resourcePagedData ?? Enumerable.Empty<object>())
                {
                    rows.Add(resource ?? new object());
                }
                return map;
            }

            var rowByResourceId = new Dictionary<object, int>();

            void Walk(object resource)
            {
                rows.Add(resource);

                var id = resourceIdGetter(resource);
                if (id == null)
                {
                    return;
                }

                rowByResourceId[id] = rows.Count - 1;

                if (!expandedResourceIds.Contains(id))
                {
                    return;
                }

                foreach (var task in GetTasksForResource(id))
                {
                    if (task != null)
                    {
                        map[task] = rows.Count;
                    }
                    rows.Add(task!);
                }

                foreach (var child in GetChildResources(id))
                {
                    Walk(child);
                }
            }

            foreach (var resource in resourcePagedData ?? Enumerable.Empty<object>())
            {
                if (resource == null)
                {
                    rows.Add(new object());
                    continue;
                }

                if (resource is GanttUnassignedResource)
                {
                    continue;
                }

                Walk(resource);
            }

            if (hasUnassignedRow && unassignedResource != null)
            {
                rows.Add(unassignedResource);
                var unassignedRow = rows.Count - 1;

                if (expandedResourceIds.Contains(UnassignedKey))
                {
                    foreach (var task in unassignedTasks)
                    {
                        if (task != null)
                        {
                            map[task] = rows.Count;
                        }
                        rows.Add(task!);
                    }
                }
                else
                {
                    foreach (var task in unassignedTasks)
                    {
                        if (task != null)
                        {
                            map[task] = unassignedRow;
                        }
                    }
                }
            }

            foreach (var task in resourceModeTasks ?? Enumerable.Empty<TItem>())
            {
                if (task == null || map.ContainsKey(task))
                {
                    continue;
                }

                var current = taskResourceIdGetter(task);
                while (current != null && !rowByResourceId.ContainsKey(current))
                {
                    current = resourceParentById != null && resourceParentById.TryGetValue(current, out var parentId) ? parentId : null;
                }

                if (current != null)
                {
                    map[task] = rowByResourceId[current];
                }
            }

            return map;
        }

        private int ComputeResourceModeRowCount()
        {
            _ = TimelineRowIndexByItem;
            return resourceDisplayRows?.Count ?? (resourcePagedData?.Count() ?? resourceGridCount);
        }

        internal void ComputeResourceLanes()
        {
            if (!IsResourceMode)
            {
                LaneByItem = null;
                LaneCountByRow = null;
                return;
            }

            var sg = GetStartGetter();
            var eg = GetEndGetter();
            if (sg == null || eg == null || resourceModeTasks == null)
            {
                LaneByItem = null;
                LaneCountByRow = null;
                return;
            }

            var rowIndex = TimelineRowIndexByItem;
            var appointmentsByRow = new Dictionary<int, List<AppointmentData>>();

            foreach (var task in resourceModeTasks)
            {
                if (task == null || !rowIndex.TryGetValue(task, out var row))
                {
                    continue;
                }

                var s = ToDateTime(sg(task));
                var e = ToDateTime(eg(task));
                if (!s.HasValue || !e.HasValue)
                {
                    continue;
                }

                if (!appointmentsByRow.TryGetValue(row, out var list))
                {
                    list = new List<AppointmentData>();
                    appointmentsByRow[row] = list;
                }
                list.Add(new AppointmentData { Data = task, Start = s.Value, End = e.Value });
            }

            GanttResourceRowLayout.ComputeLanes(appointmentsByRow, out var laneByItem, out var laneCountByRow);
            LaneByItem = laneByItem;
            LaneCountByRow = laneCountByRow;
        }

        #endregion

        #region Resource View Public Methods

        private bool isBatchExpanding;

        /// <summary>
        /// Expands the specified resource rows to show their assigned tasks and child resources.
        /// Only applicable when <see cref="ViewMode"/> is <see cref="GanttViewMode.Resource"/>.
        /// </summary>
        /// <param name="resources">The resource items to expand.</param>
        public async System.Threading.Tasks.Task ExpandResourceRows(IEnumerable resources)
        {
            ArgumentNullException.ThrowIfNull(resources);
            if (!IsResourceMode || resourceGrid == null)
            {
                return;
            }

            var list = resources.Cast<object>().ToList();

            foreach (var resource in list)
            {
                var key = GetExpansionKey(resource);
                if (key != null)
                {
                    expandedResourceIds.Add(key);
                }
            }

            isBatchExpanding = true;
            await resourceGrid.ExpandRows(list);
            isBatchExpanding = false;

            InvalidateTimelineCache();
            ComputeResourceLanes();
            await InvokeAsync(StateHasChanged);
        }

        private async Task ExpandResourceRowsForItems(IEnumerable<TItem> items)
        {
            if (taskResourceIdGetter == null || resourceById == null)
            {
                return;
            }

            var resources = new List<object>();

            foreach (var item in items)
            {
                if (item == null)
                {
                    continue;
                }

                var resId = taskResourceIdGetter(item);
                var chain = new List<object>();

                while (resId != null && resourceById.TryGetValue(resId, out var resource))
                {
                    chain.Add(resource);
                    resId = resourceParentById != null && resourceParentById.TryGetValue(resId, out var parentId) ? parentId : null;
                }

                chain.Reverse();

                foreach (var resource in chain)
                {
                    if (!resources.Contains(resource))
                    {
                        resources.Add(resource);
                    }
                }
            }

            if (resources.Count > 0)
            {
                await ExpandResourceRows(resources);
            }
        }

        #endregion

        #region Resource Grid Event Handlers

        private Type? resourceElementType;

        private Type GetResourceElementType()
        {
            if (resourceElementType != null)
            {
                return resourceElementType;
            }

            if (ResourceData == null)
            {
                return typeof(object);
            }

            var dataType = ResourceData.GetType();
            var elementType = dataType.GetInterfaces()
                .Concat(new[] { dataType })
                .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IEnumerable<>) || i.GetGenericTypeDefinition() == typeof(IQueryable<>)))
                .Select(i => i.GetGenericArguments()[0])
                .FirstOrDefault(t => t != typeof(object));

            resourceElementType = elementType ?? typeof(object);
            return resourceElementType;
        }

        internal async Task OnResourceGridLoadData(LoadDataArgs args)
        {
            InitResourceGetters();
            BuildResourceMaps();

            var elementType = GetResourceElementType();
            if (elementType != typeof(object))
            {
                var method = GetType().GetMethod(nameof(ExecuteResourceQuery), BindingFlags.NonPublic | BindingFlags.Instance)!;
                var genericMethod = method.MakeGenericMethod(elementType);
                var result = genericMethod.Invoke(this, new object[] { args });
                resourcePagedData = (IEnumerable<object>?)result;
            }
            else
            {
                resourcePagedData = ExecuteResourceQuery<object>(args);
            }

            ComputeResourceModeTasks();
            AppendUnassignedRow();
            InvalidateTimelineCache();
            ComputeResourceLanes();
            await InvokeAsync(StateHasChanged);
        }

        private void AppendUnassignedRow()
        {
            hasUnassignedRow = false;

            if (!ShowUnassignedTasks || unassignedTasks.Count == 0 || resourcePagedData == null)
            {
                return;
            }

            unassignedResource ??= new GanttUnassignedResource();
            unassignedResource.Text = UnassignedText;

            resourcePagedData = resourcePagedData
                .Where(r => r is not GanttUnassignedResource)
                .Concat(new object[] { unassignedResource })
                .ToList();

            hasUnassignedRow = true;
        }

        private IEnumerable<object> ExecuteResourceQuery<TResource>(LoadDataArgs args)
        {
            IQueryable<TResource> query;

            if (ResourceData is IQueryable<TResource> existingQueryable)
            {
                query = existingQueryable;
            }
            else
            {
                query = (ResourceData?.Cast<TResource>() ?? Enumerable.Empty<TResource>()).AsQueryable();
            }

            if (!string.IsNullOrWhiteSpace(ResourceParentIdProperty))
            {
                query = query.Where($"it => it.{ResourceParentIdProperty} == null");
            }

            if (!string.IsNullOrWhiteSpace(args.Filter))
            {
                query = query.Where(args.Filter);
            }

            if (!string.IsNullOrWhiteSpace(args.OrderBy))
            {
                query = query.OrderBy(args.OrderBy);
            }

            resourceGridCount = query.Count();

            if (args.Skip.HasValue)
            {
                query = query.Skip(args.Skip.Value);
            }

            if (args.Top.HasValue)
            {
                query = query.Take(args.Top.Value);
            }

            return query.Cast<object>().ToList();
        }

        internal Task OnResourceGridLoadChildData(DataGridLoadChildDataEventArgs<object> args)
        {
            if (args.Item is GanttUnassignedResource)
            {
                args.Data = unassignedTasks.Cast<object>().ToList();
                return Task.CompletedTask;
            }

            if (resourceIdGetter == null || args.Item == null || !IsResourceItem(args.Item))
            {
                return Task.CompletedTask;
            }

            var resId = resourceIdGetter(args.Item);
            if (resId == null)
            {
                return Task.CompletedTask;
            }

            args.Data = GetTasksForResource(resId).Cast<object>().Concat(GetChildResources(resId)).ToList();
            return Task.CompletedTask;
        }

        internal void OnResourceGridRowRender(RowRenderEventArgs<object> args)
        {
            if (args.Data is GanttUnassignedResource)
            {
                var isExpanded = expandedResourceIds.Contains(UnassignedKey);
                args.Attributes["style"] = ResourceRowStyle(GetResourceRowHeight(args.Data, isExpanded));
                args.Expandable = unassignedTasks.Count > 0;
                return;
            }

            if (args.Data != null && IsResourceItem(args.Data) && resourceIdGetter != null)
            {
                var resId = resourceIdGetter(args.Data);
                var isExpanded = resId != null && expandedResourceIds.Contains(resId);
                args.Attributes["style"] = ResourceRowStyle(GetResourceRowHeight(args.Data, isExpanded));
                args.Expandable = resId != null && (GetTasksForResource(resId).Count > 0 || GetChildResources(resId).Count > 0);
                return;
            }

            args.Attributes["style"] = ResourceRowStyle(RowHeightPx);
            args.Expandable = false;
        }

        private static string ResourceRowStyle(int height) => $"height:{height}px;--rz-gantt-row-height:{height}px;";

        private int GetResourceRowHeight(object rowItem, bool isExpanded)
        {
            if (isExpanded)
            {
                return RowHeightPx;
            }

            var rowIndex = FindResourceRowIndex(rowItem);
            if (rowIndex >= 0 && LaneCountByRow != null && LaneCountByRow.TryGetValue(rowIndex, out var laneCount) && laneCount > 1)
            {
                return RowHeightPx * laneCount;
            }
            return RowHeightPx;
        }

        private bool IsResourceItem(object item)
        {
            return allResources != null && allResources.Contains(item);
        }

        private int FindResourceRowIndex(object resource)
        {
            _ = TimelineRowIndexByItem;
            var rows = resourceDisplayRows;
            if (rows == null)
            {
                return -1;
            }

            for (var i = 0; i < rows.Count; i++)
            {
                if (ReferenceEquals(rows[i], resource))
                {
                    return i;
                }
            }
            return -1;
        }

        private object? GetExpansionKey(object item)
        {
            if (item is GanttUnassignedResource)
            {
                return UnassignedKey;
            }

            if (IsResourceItem(item) && resourceIdGetter != null)
            {
                return resourceIdGetter(item);
            }

            return null;
        }

        internal async Task OnResourceGridRowExpand(object item)
        {
            var key = GetExpansionKey(item);
            if (key != null)
            {
                expandedResourceIds.Add(key);
            }

            if (!isBatchExpanding)
            {
                InvalidateTimelineCache();
                ComputeResourceLanes();
                await InvokeAsync(StateHasChanged);
            }
        }

        internal async Task OnResourceGridRowCollapse(object item)
        {
            var key = GetExpansionKey(item);
            if (key != null)
            {
                expandedResourceIds.Remove(key);
            }

            InvalidateTimelineCache();
            ComputeResourceLanes();
            await InvokeAsync(StateHasChanged);
        }

        internal string GetResourceCellText(object? item)
        {
            if (item is GanttUnassignedResource unassigned)
            {
                return unassigned.Text;
            }

            if (item != null && IsResourceItem(item) && resourceTextGetter != null)
            {
                return resourceTextGetter(item)?.ToString() ?? "";
            }

            if (item is TItem task && textGetter != null)
            {
                return textGetter(task)?.ToString() ?? "";
            }

            return "";
        }

        #endregion
    }
}
