using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenPivotDataGrid component for creating pivot tables with cross-tabulation functionality.
    /// </summary>
    /// <typeparam name="TItem">The type of the PivotDataGrid data item.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenPivotDataGrid @data=@orders TItem="Order" AllowSorting="true" AllowPaging="true" AllowFiltering="true"&gt;
    ///     &lt;Columns&gt;
    ///         &lt;RadzenPivotColumn TItem="Order" Property="Category" Title="Category" /&gt;
    ///         &lt;RadzenPivotColumn TItem="Order" Property="Region" Title="Region" /&gt;
    ///     &lt;/Columns&gt;
    ///     &lt;Rows&gt;
    ///         &lt;RadzenPivotRow TItem="Order" Property="Product" Title="Product" /&gt;
    ///         &lt;RadzenPivotRow TItem="Order" Property="Year" Title="Year" /&gt;
    ///     &lt;/Rows&gt;
    ///     &lt;Aggregates&gt;
    ///         &lt;RadzenPivotAggregate TItem="Order" Property="Amount" Title="Amount" Aggregate="AggregateFunction.Sum" /&gt;
    ///         &lt;RadzenPivotAggregate TItem="Order" Property="Quantity" Title="Quantity" Aggregate="AggregateFunction.Count" /&gt;
    ///     &lt;/Aggregates&gt;
    /// &lt;/RadzenPivotDataGrid&gt;
    /// </code>
    /// </example>
#if NET6_0_OR_GREATER
    [CascadingTypeParameter(nameof(TItem))]
#endif
    public partial class RadzenPivotDataGrid<TItem> : PagedDataBoundComponent<TItem>
    {
        private class RowHeaderCell
        {
            public object Value { get; set; }
            public string Title { get; set; }
            public int RowSpan { get; set; } = 1;
            public bool IsCollapsed { get; set; }
            public string PathKey { get; set; }
        }

        /// <summary>
        /// Gets or sets the columns collection for pivot columns.
        /// </summary>
        [Parameter]
        public RenderFragment Columns { get; set; }

        /// <summary>
        /// Gets or sets the rows collection for pivot rows.
        /// </summary>
        [Parameter]
        public RenderFragment Rows { get; set; }

        /// <summary>
        /// Gets or sets the aggregates collection for pivot aggregates/measures.
        /// </summary>
        [Parameter]
        public RenderFragment Aggregates { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show column totals.
        /// </summary>
        [Parameter]
        public bool ShowColumnsTotals { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show row totals.
        /// </summary>
        [Parameter]
        public bool ShowRowsTotals { get; set; }

        /// <summary>
        /// Gets or sets the empty text shown when Data is empty collection.
        /// </summary>
        [Parameter]
        public string EmptyText { get; set; } = "No records to display.";

        /// <summary>
        /// Gets or sets the empty template shown when Data is empty collection.
        /// </summary>
        [Parameter]
        public RenderFragment EmptyTemplate { get; set; }

        /// <summary>
        /// Gets or sets the grid lines style.
        /// </summary>
        [Parameter]
        public DataGridGridLines GridLines { get; set; } = DataGridGridLines.Default;

        /// <summary>
        /// Gets or sets a value indicating whether RadzenPivotDataGrid should use alternating row styles.
        /// </summary>
        [Parameter]
        public bool AllowAlternatingRows { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether drill down functionality is enabled.
        /// </summary>
        [Parameter]
        public bool AllowDrillDown { get; set; } = true;

        readonly List<RadzenPivotColumn<TItem>> pivotColumns = new List<RadzenPivotColumn<TItem>>();
        
        readonly List<RadzenPivotRow<TItem>> pivotRows = new List<RadzenPivotRow<TItem>>();
        
        readonly List<RadzenPivotAggregate<TItem>> pivotAggregates = new List<RadzenPivotAggregate<TItem>>();

        // Drill down state management
        private Dictionary<string, bool> _collapsedColumnGroups = new Dictionary<string, bool>();
        private Dictionary<string, bool> _collapsedRowGroups = new Dictionary<string, bool>();

        private List<List<ColumnHeaderCell>> _columnHeaderRows;
        private List<PivotBodyRow> _cachedPivotRows;
        private List<List<object>> _cachedColumnLeaves;

        /// <summary>
        /// Gets the cached column header rows.
        /// </summary>
        private List<List<ColumnHeaderCell>> ColumnHeaderRows
        {
            get
            {
                if (_columnHeaderRows == null)
                {
                    var root = BuildColumnHeaderTree();
                    _columnHeaderRows = FlattenColumnHeaderTree(root);
                }
                return _columnHeaderRows;
            }
        }

        /// <summary>
        /// Gets the cached pivot rows.
        /// </summary>
        private List<PivotBodyRow> CachedPivotRows
        {
            get
            {
                if (_cachedPivotRows == null)
                {
                    _cachedPivotRows = GetPivotRowHierarchy();
                }
                return _cachedPivotRows;
            }
        }

        /// <summary>
        /// Gets the cached column leaves.
        /// </summary>
        private List<List<object>> CachedColumnLeaves
        {
            get
            {
                if (_cachedColumnLeaves == null)
                {
                    _cachedColumnLeaves = GetColumnHeaderLeaves();
                }
                return _cachedColumnLeaves;
            }
        }

        ColumnHeaderNode BuildColumnHeaderTree()
        {
            var root = new ColumnHeaderNode { Level = 0, Title = null };
            if (!pivotColumns.Any() || Data == null)
                return root;

            BuildColumnHeaderTreeRecursive(root, PagedView, 0, new List<object>());

            return root;
        }

        void BuildColumnHeaderTreeRecursive(ColumnHeaderNode node, IQueryable<TItem> items, int level, List<object> path)
        {
            if (level >= pivotColumns.Count)
                return;
            var col = pivotColumns[level];
            var groups = items.GroupByMany(new string[] { col.Property });
            foreach (var group in groups)
            {
                var currentPath = new List<object>(path) { group.Key };
                var pathKey = string.Join("|", currentPath);
                var isCollapsed = AllowDrillDown && (!_collapsedColumnGroups.ContainsKey(pathKey) || _collapsedColumnGroups[pathKey]);

                var child = new ColumnHeaderNode
                {
                    Value = group.Key,
                    Title = $"{group.Key}",
                    Level = level + 1,
                    Width = col.Width,
                    IsCollapsed = isCollapsed,
                    PathKey = pathKey
                };

                if (!isCollapsed)
                {
                    BuildColumnHeaderTreeRecursive(child, group.Items.Cast<TItem>().AsQueryable(), level + 1, currentPath);
                }
                node.Children.Add(child);
            }
        }

        List<List<ColumnHeaderCell>> FlattenColumnHeaderTree(ColumnHeaderNode root)
        {
            var rows = new List<List<ColumnHeaderCell>>();
            int maxLevel = pivotColumns.Count;
            for (int i = 0; i < maxLevel; i++)
                rows.Add(new List<ColumnHeaderCell>());
            FlattenColumnHeaderTreeRecursive(root, rows, 0, maxLevel);
            return rows;
        }

        void FlattenColumnHeaderTreeRecursive(ColumnHeaderNode node, List<List<ColumnHeaderCell>> rows, int level, int maxLevel)
        {
            if (level > 0 && node.Title != null)
            {
                var cell = new ColumnHeaderCell
                {
                    Value = node.Value,
                    Title = node.Title,
                    Level = level - 1,
                    Width = node.Width,
                    IsCollapsed = node.IsCollapsed,
                    PathKey = node.PathKey
                };

                if (node.Children.Count == 0)
                {
                    cell.RowSpan = maxLevel - (level - 1);
                    cell.ColSpan = 1;
                }
                else
                {
                    cell.RowSpan = 1;
                    cell.ColSpan = GetLeafCount(node);
                }

                rows[level - 1].Add(cell);
            }

            foreach (var child in node.Children)
            {
                FlattenColumnHeaderTreeRecursive(child, rows, level + 1, maxLevel);
            }
        }

        int GetLeafCount(ColumnHeaderNode node)
        {
            if (node.Children.Count == 0) return 1;
            return node.Children.Sum(GetLeafCount);
        }

        List<int> GetColumnHeaderRows()
        {
            return Enumerable.Range(0, ColumnHeaderRows.Count).ToList();
        }

        List<ColumnHeaderCell> GetColumnHeaderCells(int row)
        {
            return ColumnHeaderRows[row];
        }

        /// <inheritdoc />
        protected override void OnDataChanged()
        {
            _columnHeaderRows = null;
            _cachedPivotRows = null;
            _cachedColumnLeaves = null;
            base.OnDataChanged();
        }

        /// <inheritdoc />
        public async override Task Reload()
        {
            _columnHeaderRows = null;
            _cachedPivotRows = null;
            _cachedColumnLeaves = null;
            await base.Reload();
            StateHasChanged();
        }

        internal void AddPivotColumn(RadzenPivotColumn<TItem> column)
        {
            if (!pivotColumns.Contains(column))
            {
                pivotColumns.Add(column);
                _columnHeaderRows = null;
                _cachedPivotRows = null;
                _cachedColumnLeaves = null;
                StateHasChanged();
            }
        }

        internal void AddPivotRow(RadzenPivotRow<TItem> row)
        {
            if (!pivotRows.Contains(row))
            {
                pivotRows.Add(row);
                _columnHeaderRows = null;
                _cachedPivotRows = null;
                _cachedColumnLeaves = null;
                StateHasChanged();
            }
        }

        internal void AddPivotAggregate(RadzenPivotAggregate<TItem> aggregate)
        {
            if (!pivotAggregates.Contains(aggregate))
            {
                pivotAggregates.Add(aggregate);
                _columnHeaderRows = null;
                _cachedPivotRows = null;
                _cachedColumnLeaves = null;
                StateHasChanged();
            }
        }

        internal void RemovePivotColumn(RadzenPivotColumn<TItem> column)
        {
            if (pivotColumns.Contains(column))
            {
                pivotColumns.Remove(column);
                _columnHeaderRows = null;
                _cachedPivotRows = null;
                _cachedColumnLeaves = null;
                StateHasChanged();
            }
        }

        internal void RemovePivotRow(RadzenPivotRow<TItem> row)
        {
            if (pivotRows.Contains(row))
            {
                pivotRows.Remove(row);
                _columnHeaderRows = null;
                _cachedPivotRows = null;
                _cachedColumnLeaves = null;
                StateHasChanged();
            }
        }

        internal void RemovePivotAggregate(RadzenPivotAggregate<TItem> aggregate)
        {
            if (pivotAggregates.Contains(aggregate))
            {
                pivotAggregates.Remove(aggregate);
                _columnHeaderRows = null;
                _cachedPivotRows = null;
                _cachedColumnLeaves = null;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Renders pivot rows with real data aggregation and grouping.
        /// </summary>
        protected virtual RenderFragment RenderPivotRows()
        {
            return new RenderFragment(builder =>
            {
                if (Data == null || !pivotRows.Any() || !pivotAggregates.Any())
                {
                    builder.OpenElement(0, "tr");
                    builder.AddAttribute(1, "class", "rz-pivot-empty-row");
                    builder.OpenElement(2, "td");
                    var totalColumns = ShowRowsTotals ? pivotAggregates.Count : 0;
                    builder.AddAttribute(3, "colspan", pivotRows.Count + (pivotAggregates.Count * CachedColumnLeaves.Count) + totalColumns);
                    builder.AddContent(4, EmptyText);
                    builder.CloseElement();
                    builder.CloseElement();
                    return;
                }

                var pivotRowData = CachedPivotRows;
                PadRowHeaderCells(pivotRowData); // Ensure all rows have the same number of row header cells
                var rowIndex = 0;
                var activeRowSpans = new Dictionary<int, int>(); // Track active rowspans

                foreach (var pivotRow in pivotRowData)
                {
                    builder.OpenElement(0, "tr");
                    builder.AddAttribute(1, "class", $"rz-pivot-row {(rowIndex % 2 == 0 ? "rz-pivot-row-even" : "rz-pivot-row-odd")}");
                    
                    // Render row headers, accounting for active rowspans
                    var colIndex = 0;
                    foreach (var rowHeaderCell in pivotRow.RowHeaderCells)
                    {
                        // Check if this column position has an active rowspan from a previous row
                        if (activeRowSpans.ContainsKey(colIndex))
                        {
                            activeRowSpans[colIndex]--;
                            if (activeRowSpans[colIndex] <= 0)
                            {
                                activeRowSpans.Remove(colIndex);
                            }
                            colIndex++;
                            continue; // Skip rendering this cell as it's covered by rowspan
                        }

                        builder.OpenElement(2, "td");
                        builder.AddAttribute(3, "class", $"rz-pivot-row-header {GetFrozenRowHeaderClass(colIndex)}");
                        builder.AddAttribute(4, "rowspan", rowHeaderCell.RowSpan);
                        builder.AddAttribute(5, "style", $"inset-inline-start: {colIndex * 140}px");
                        
                        // Check if this is a drill down row header
                        if (AllowDrillDown && !string.IsNullOrEmpty(rowHeaderCell.PathKey))
                        {
                            builder.OpenElement(6, "div");
                            builder.AddAttribute(7, "class", "rz-pivot-drill-down-header");
                            
                            builder.OpenElement(8, "span");
                            builder.AddAttribute(9, "class", $"notranslate rz-tree-toggler rzi rzi-caret-{(rowHeaderCell.IsCollapsed ? "right" : "down")}");
                            builder.AddAttribute(10, "onclick", EventCallback.Factory.Create(this, () => ToggleRowDrillDown(rowHeaderCell.PathKey)));
                            builder.AddAttribute(11, "style", "margin-inline-start:0");
                            builder.CloseElement();
                            
                            builder.OpenElement(13, "span");
                            builder.AddAttribute(14, "class", "rz-pivot-header-text");
                            builder.AddContent(15, rowHeaderCell.Title);
                            builder.CloseElement();
                            
                            builder.CloseElement();
                        }
                        else
                        {
                            builder.AddContent(6, rowHeaderCell.Title);
                        }
                        
                        builder.CloseElement();

                        // Track this rowspan for future rows
                        if (rowHeaderCell.RowSpan > 1)
                        {
                            activeRowSpans[colIndex] = rowHeaderCell.RowSpan - 1;
                        }
                        
                        colIndex++;
                    }

                    // Render value cells for each column combination
                    var colLeaves = CachedColumnLeaves;
                    foreach (var colPath in colLeaves)
                    {
                        foreach (var aggregate in pivotAggregates)
                        {
                            builder.OpenElement(8, "td");
                            builder.AddAttribute(9, "class", "rz-pivot-value-cell");
                            builder.AddAttribute(10, "style", $"text-align: {aggregate.TextAlign.ToString().ToLowerInvariant()}");
                            
                            // Find the value for this specific row and column combination
                            var valueIndex = colLeaves.IndexOf(colPath) * pivotAggregates.Count + pivotAggregates.IndexOf(aggregate);
                            if (valueIndex < pivotRow.ValueCells.Count)
                            {
                                var cellValue = pivotRow.ValueCells[valueIndex];

                                if (aggregate.Template != null)
                                {
                                    builder.AddContent(11, aggregate.Template(cellValue));
                                }
                                else
                                {
                                    builder.AddContent(11, aggregate.FormatValue(cellValue));
                                }
                            }
                            
                            builder.CloseElement();
                        }
                    }

                    // Render total cells for each aggregate (only if ShowRowsTotals is true)
                    if (ShowRowsTotals)
                    {
                        foreach (var aggregate in pivotAggregates)
                        {
                            builder.OpenElement(12, "td");
                            builder.AddAttribute(13, "class", $"rz-pivot-total-cell {GetFrozenTotalCellClass()}");
                            builder.AddAttribute(14, "style", $"inset-inline-end: {(pivotAggregates.Count - 1 - pivotAggregates.IndexOf(aggregate)) * 120}px");
                            
                            // Calculate row total for this specific aggregate
                            var rowTotal = GetAggregateValue(GetRowItems(pivotRow), aggregate);

                            if (aggregate.RowTotalTemplate != null)
                            {
                                builder.AddContent(15, aggregate.RowTotalTemplate(rowTotal));
                            }
                            else
                            {
                                builder.AddContent(15, aggregate.FormatValue(rowTotal));
                            }
                            
                            builder.CloseElement();
                        }
                    }
                    
                    builder.CloseElement();
                    rowIndex++;
                }
            });
        }

        /// <summary>
        /// Renders footer with grand totals for each value column.
        /// </summary>
        protected RenderFragment RenderFooter()
        {
            return new RenderFragment(builder =>
            {
                builder.OpenElement(0, "tr");
                builder.AddAttribute(1, "class", "rz-pivot-footer-row");

                // Empty cells for row headers
                int seq = 2;
                var pivotRowData = CachedPivotRows;
                PadRowHeaderCells(pivotRowData); // Ensure all rows have the same number of row header cells
                var count = pivotRowData.FirstOrDefault() != null ?
                    pivotRowData.FirstOrDefault().RowHeaderCells.Count : pivotRows.Count;
                for (int i = 0; i < count; i++)
                {
                    builder.OpenElement(seq++, "td");
                    builder.AddAttribute(seq++, "class", $"rz-pivot-footer-header {GetFrozenRowHeaderClass(i)}");
                    builder.AddAttribute(seq++, "style", $"inset-inline-start: {i * 140}px");
                    builder.CloseElement();
                }

                var colLeaves = CachedColumnLeaves;
                foreach (var colPath in colLeaves)
                {
                    foreach (var aggregate in pivotAggregates)
                    {
                        builder.OpenElement(seq++, "td");
                        builder.AddAttribute(seq++, "class", "rz-pivot-footer-value");
                        builder.AddAttribute(seq++, "style", $"text-align: {aggregate.TextAlign.ToString().ToLowerInvariant()}");

                        var items = PagedView;

                        for (int i = 0; i < colPath.Count; i++)
                        {
                            var col = pivotColumns[i];
                            var value = colPath[i] is string ? $@"""{colPath[i]}""" : colPath[i];
                            items = items.Where($"i => i.{col.Property} == {value}");
                        }

                        var total = GetAggregateValue(items, aggregate);

                        if (aggregate.ColumnTotalTemplate != null)
                        {
                            builder.AddContent(seq++, aggregate.ColumnTotalTemplate(total));
                        }
                        else
                        {
                            builder.AddContent(seq++, aggregate.FormatValue(total));
                        }
                        
                        builder.CloseElement();
                    }
                }

                if(ShowRowsTotals)
                {
                    foreach (var aggregate in pivotAggregates)
                    {
                        builder.OpenElement(seq++, "td");
                        builder.AddAttribute(seq++, "class", $"rz-pivot-footer-total {GetFrozenTotalCellClass()}");
                        builder.AddAttribute(seq++, "style", $"inset-inline-end: {(pivotAggregates.Count - 1 - pivotAggregates.IndexOf(aggregate)) * 120}px");

                        var total = GetAggregateValue(View, aggregate);

                        if (aggregate.RowTotalTemplate != null)
                        {
                            builder.AddContent(seq++, aggregate.RowTotalTemplate(total));
                        }
                        else
                        {
                            builder.AddContent(seq++, aggregate.FormatValue(total));
                        }
                        
                        builder.CloseElement();
                    }
                }

                builder.CloseElement();
            });
        }

        /// <summary>
        /// Gets the items for a specific row.
        /// </summary>
        /// <param name="pivotRow">The pivot row data.</param>
        /// <returns>The items for this row.</returns>
        private IQueryable<TItem> GetRowItems(PivotBodyRow pivotRow)
        {
            if (Data == null || !pivotRows.Any())
                return Enumerable.Empty<TItem>().AsQueryable();

            var items = PagedView;
            
            // Filter items based on row header values
            for (int i = 0; i < pivotRow.RowHeaderCells.Count && i < pivotRows.Count; i++)
            {
                var rowHeaderValue = pivotRow.RowHeaderCells[i].Title;
                var rowProperty = pivotRows[i].Property;
                
                items = items.Where($@"i => i.{rowProperty} == ""{rowHeaderValue}""");
            }
            
            return items;
        }

        /// <summary>
        /// Toggles the drill down state for a column group.
        /// </summary>
        /// <param name="pathKey">The path key identifying the column group.</param>
        public async Task ToggleColumnDrillDown(string pathKey)
        {
            if (!AllowDrillDown) return;

            if (_collapsedColumnGroups.ContainsKey(pathKey))
            {
                _collapsedColumnGroups[pathKey] = !_collapsedColumnGroups[pathKey];
            }
            else
            {
                _collapsedColumnGroups[pathKey] = false;
            }

            _columnHeaderRows = null;
            _cachedPivotRows = null;
            _cachedColumnLeaves = null;
            StateHasChanged();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Toggles the drill down state for a row group.
        /// </summary>
        /// <param name="pathKey">The path key identifying the row group.</param>
        public async Task ToggleRowDrillDown(string pathKey)
        {
            if (!AllowDrillDown) return;

            if (_collapsedRowGroups.ContainsKey(pathKey))
            {
                _collapsedRowGroups[pathKey] = !_collapsedRowGroups[pathKey];
            }
            else
            {
                _collapsedRowGroups[pathKey] = false;
            }

            _columnHeaderRows = null;
            _cachedPivotRows = null;
            _cachedColumnLeaves = null;
            StateHasChanged();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets the aggregate value for a group, considering collapsed state.
        /// </summary>
        /// <param name="items">The items to aggregate.</param>
        /// <param name="aggregate">The aggregate configuration.</param>
        /// <param name="isCollapsed">Whether the group is collapsed.</param>
        /// <returns>The aggregated value.</returns>
        private object GetAggregateValue(IQueryable<TItem> items, RadzenPivotAggregate<TItem> aggregate, bool isCollapsed = false)
        {
            if (items == null || !items.Any())
                return null;

            if (isCollapsed)
            {
                // For collapsed groups, aggregate all items in the group
                // This means we need to get all items that match the current path
                return GetAggregateValue(items, aggregate);
            }

            return GetAggregateValue(items, aggregate);
        }

        /// <summary>
        /// Gets the aggregate value for items.
        /// </summary>
        /// <param name="items">The items to aggregate.</param>
        /// <param name="aggregate">The aggregate configuration.</param>
        /// <returns>The aggregated value.</returns>
        private object GetAggregateValue(IQueryable<TItem> items, RadzenPivotAggregate<TItem> aggregate)
        {
            if (items == null || !items.Any())
                return null;

            try
            {
                var propertyType = typeof(TItem).GetProperty(aggregate.Property)?.PropertyType;
                var isNumeric = PropertyAccess.IsNumeric(propertyType);

                IQueryable values;

                if (propertyType == typeof(short))
                {
                    values = ((IQueryable<short>)items.Select(aggregate.Property)).Select(i => (int)i);
                    propertyType = typeof(int);
                }
                else if (propertyType == typeof(short?))
                {
                    values = ((IQueryable<short?>)items.Select(aggregate.Property)).Select(i => (int?)i);
                    propertyType = typeof(int?);
                }
                else
                {
                    values = items.Select(aggregate.Property).Cast(propertyType);
                }

                switch (aggregate.Aggregate)
                {
                    case AggregateFunction.Sum:
                        return isNumeric ? values.Sum(propertyType) : items.Count();
                    case AggregateFunction.Average:
                        return isNumeric ? values.Average(propertyType) : items.Count();
                    case AggregateFunction.Count:
                        return items.Count();
                    case AggregateFunction.Min:
                        return isNumeric ? values.Min(propertyType) : items.FirstOrDefault();
                    case AggregateFunction.Max:
                        return isNumeric ? values.Max(propertyType) : items.LastOrDefault();
                    case AggregateFunction.First:
                        return values.FirstOrDefault();
                    case AggregateFunction.Last:
                        return values.LastOrDefault();
                    default:
                        return isNumeric ? values.Sum(propertyType) : items.Count();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the table CSS class.
        /// </summary>
        protected virtual string GetTableCssClass()
        {
            return ClassList.Create("rz-grid-table rz-grid-table-fixed")
                .Add("rz-grid-table-striped", AllowAlternatingRows)
                .Add($"rz-grid-gridlines-{Enum.GetName(typeof(DataGridGridLines), GridLines).ToLowerInvariant()}", GridLines != DataGridGridLines.Default)
                .ToString();
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create("rz-has-pager rz-pivot-datatable rz-pivot-datatable-scrollable")
                .Add("rz-has-height", CurrentStyle.ContainsKey("height"))
                .ToString();
        }

        // Build the row header tree from the data
        private RowHeaderNode BuildRowHeaderTree()
        {
            var root = new RowHeaderNode { Level = 0, Title = null };
            if (!pivotRows.Any() || Data == null)
                return root;

            BuildRowHeaderTreeRecursive(root, PagedView, 0, new List<object>());

            return root;
        }
        private void BuildRowHeaderTreeRecursive(RowHeaderNode node, IQueryable<TItem> items, int level, List<object> path)
        {
            if (level >= pivotRows.Count)
            {
                node.Items = items;
                return;
            }
            var row = pivotRows[level];
            var groups = items.GroupByMany(new string[]{ row.Property});
            foreach (var group in groups)
            {
                var currentPath = new List<object>(path) { group.Key };
                var pathKey = string.Join("|", currentPath);
                var isCollapsed = AllowDrillDown && (!_collapsedRowGroups.ContainsKey(pathKey) || _collapsedRowGroups[pathKey]);

                var child = new RowHeaderNode
                {
                    Value = group.Key,
                    Title = $"{group.Key}",
                    Level = level + 1,
                    IsCollapsed = isCollapsed,
                    PathKey = pathKey
                };

                if (!isCollapsed)
                {
                    BuildRowHeaderTreeRecursive(child, group.Items.Cast<TItem>().AsQueryable(), level + 1, currentPath);
                }
                node.Children.Add(child);
            }
        }
        // Flatten the row header tree into a list of rows for rendering
        private List<PivotBodyRow> GetPivotRowHierarchy()
        {
            var result = new List<PivotBodyRow>();
            var tree = BuildRowHeaderTree();
            FlattenRowHeaderTree(tree, new List<RowHeaderCell>(), result);

            return result;
        }
        private void FlattenRowHeaderTree(RowHeaderNode node, List<RowHeaderCell> prefix, List<PivotBodyRow> result)
        {
            if (node.Level == pivotRows.Count)
            {
                // Leaf: render a row
                var row = new PivotBodyRow
                {
                    RowHeaderCells = new List<RowHeaderCell>(prefix)
                };
                // For each column header leaf, aggregate values
                var colLeaves = CachedColumnLeaves;
                foreach (var colPath in colLeaves)
                {
                    foreach (var aggregate in pivotAggregates)
                    {
                        // Find items matching this row and column
                        var items = node.Items;
                        for (int i = 0; i < colPath.Count; i++)
                        {
                            var col = pivotColumns[i];
                            var value = colPath[i] is string ? $@"""{colPath[i]}""" : colPath[i];
                            items = items.Where($"i => i.{col.Property} == {value}");
                        }
                        row.ValueCells.Add(items.Count() > 0 ? GetAggregateValue(items, aggregate) : null);
                    }
                    row.VisibleColumnLeaves.Add(colPath);
                }

                result.Add(row);
            }
            else
            {
                foreach (var child in node.Children)
                {
                    var cell = new RowHeaderCell { 
                        Value = node.Value, 
                        Title = child.Title, 
                        RowSpan = GetLeafCount(child),
                        IsCollapsed = child.IsCollapsed,
                        PathKey = child.PathKey
                    };
                    var newPrefix = new List<RowHeaderCell>(prefix) { cell };
                    
                    if (child.IsCollapsed)
                    {
                        // For collapsed groups, create a single row with aggregated values
                        var collapsedRow = new PivotBodyRow
                        {
                            RowHeaderCells = new List<RowHeaderCell>(newPrefix)
                        };
                        var colLeaves = CachedColumnLeaves;
                        foreach (var colPath in colLeaves)
                        {
                            foreach (var aggregate in pivotAggregates)
                            {
                                // Find items matching this row path and column
                                var items = PagedView;
                                // Filter by row path
                                for (int i = 0; i < newPrefix.Count && i < pivotRows.Count; i++)
                                {
                                    var rowProperty = pivotRows[i].Property;
                                    var rowValue = newPrefix[i].Title;
                                    items = items.Where($@"i => i.{rowProperty} == ""{rowValue}""");
                                }
                                // Filter by column path
                                for (int i = 0; i < colPath.Count; i++)
                                {
                                    var col = pivotColumns[i];
                                    var value = colPath[i] is string ? $@"""{colPath[i]}""" : colPath[i];
                                    items = items.Where($"i => i.{col.Property} == {value}");
                                }
                                collapsedRow.ValueCells.Add(items.Count() > 0 ? GetAggregateValue(items, aggregate) : null);
                            }
                            collapsedRow.VisibleColumnLeaves.Add(colPath);
                        }
                        result.Add(collapsedRow);
                    }
                    else
                    {
                        FlattenRowHeaderTree(child, newPrefix, result);
                    }
                }
            }
        }

        // Get all column header leaf paths (for value cell mapping)
        private List<List<object>> GetColumnHeaderLeaves()
        {
            var tree = BuildColumnHeaderTree();
            var leaves = new List<List<object>>();
            GetColumnHeaderLeavesRecursive(tree, new List<object>(), leaves);
            return leaves;
        }
        private void GetColumnHeaderLeavesRecursive(ColumnHeaderNode node, List<object> path, List<List<object>> leaves)
        {
            if (node.Children.Count == 0 && path.Count > 0)
            {
                leaves.Add(new List<object>(path));
            }
            foreach (var child in node.Children)
            {
                path.Add(child.Value);
                GetColumnHeaderLeavesRecursive(child, path, leaves);
                path.RemoveAt(path.Count - 1);
            }
        }
        // Helper for row header cell rowspan
        private int GetLeafCount(RowHeaderNode node)
        {
            if (node.Children.Count == 0) return 1;
            return node.Children.Sum(GetLeafCount);
        }

        private string GetWidthForColumnPath(List<object> colPath)
        {
            if (colPath.Count == 0 || pivotColumns.Count == 0)
                return null;
            
            // For now, return the width of the first column
            // In a more complex implementation, you might want to calculate based on the path
            return pivotColumns[0].Width;
        }

        /// <summary>
        /// Gets the frozen cell class for row headers based on their position.
        /// </summary>
        /// <param name="columnIndex">The column index of the row header.</param>
        /// <returns>The CSS class string for frozen row headers.</returns>
        private string GetFrozenRowHeaderClass(int columnIndex)
        {
            if (columnIndex == 0)
            {
                return "rz-frozen-cell rz-frozen-cell-left rz-frozen-cell-left-end";
            }
            else
            {
                return "rz-frozen-cell rz-frozen-cell-left";
            }
        }

        /// <summary>
        /// Gets the frozen cell class for total headers.
        /// </summary>
        /// <returns>The CSS class string for frozen total headers.</returns>
        private string GetFrozenTotalHeaderClass()
        {
            return "rz-frozen-cell rz-frozen-cell-right rz-frozen-cell-right-end";
        }

        /// <summary>
        /// Gets the frozen cell class for total cells.
        /// </summary>
        /// <returns>The CSS class string for frozen total cells.</returns>
        private string GetFrozenTotalCellClass()
        {
            return "rz-frozen-cell rz-frozen-cell-right";
        }

        private class ColumnHeaderCell
        {
            public object Value { get; set; }
            public string Title { get; set; }
            public int ColSpan { get; set; } = 1;
            public int RowSpan { get; set; } = 1;
            public int Level { get; set; }
            public string Width { get; set; }
            public bool IsCollapsed { get; set; }
            public string PathKey { get; set; }
        }

        private class ColumnHeaderNode
        {
            public object Value { get; set; }
            public string Title { get; set; }
            public List<ColumnHeaderNode> Children { get; set; } = new List<ColumnHeaderNode>();
            public int Level { get; set; }
            public int ColSpan { get; set; } = 1;
            public int RowSpan { get; set; } = 1;
            public string Width { get; set; }
            public bool IsCollapsed { get; set; }
            public string PathKey { get; set; }
        }

        private class RowHeaderNode
        {
            public object Value { get; set; }
            public string Title { get; set; }
            public List<RowHeaderNode> Children { get; set; } = new List<RowHeaderNode>();
            public IQueryable<TItem> Items { get; set; } = Enumerable.Empty<TItem>().AsQueryable();
            public int Level { get; set; }
            public bool IsCollapsed { get; set; }
            public string PathKey { get; set; }
        }

        private class PivotBodyRow
        {
            public List<RowHeaderCell> RowHeaderCells { get; set; } = new List<RowHeaderCell>();
            public List<object> ValueCells { get; set; } = new List<object>();
            public List<List<object>> VisibleColumnLeaves { get; set; } = new List<List<object>>();
        }

        // Returns the union of all visible column leaves for the current view
        private List<List<object>> GetVisibleColumnLeaves(List<PivotBodyRow> rows)
        {
            var set = new HashSet<string>();
            var result = new List<List<object>>();
            foreach (var row in rows)
            {
                foreach (var colPath in row.VisibleColumnLeaves)
                {
                    var key = string.Join("|", colPath.Select(x => x?.ToString() ?? ""));
                    if (set.Add(key))
                    {
                        result.Add(colPath);
                    }
                }
            }
            return result;
        }

        // Pads all PivotBodyRow.RowHeaderCells to the maximum depth among all rows
        private void PadRowHeaderCells(List<PivotBodyRow> rows)
        {
            int maxDepth = rows.Count > 0 ? rows.Max(r => r.RowHeaderCells.Count) : 0;
            foreach (var row in rows)
            {
                while (row.RowHeaderCells.Count < maxDepth)
                {
                    row.RowHeaderCells.Add(new RowHeaderCell
                    {
                        Value = null,
                        Title = string.Empty,
                        RowSpan = 1,
                        IsCollapsed = false,
                        PathKey = null
                    });
                }
            }
        }
    }
} 