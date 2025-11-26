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
using System.Data.Common;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Threading.Tasks;

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
            public bool HasChildren { get; set; }
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
        /// Gets or sets a value indicating whether this instance loading indicator is shown.
        /// </summary>
        /// <value><c>true</c> if this instance loading indicator is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool IsLoading { get; set; }

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
        /// Gets or sets a value indicating whether picking of fields runtime is allowed. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if picking of fields runtime is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowFieldsPicking { get; set; } = true;

        /// <summary>
        /// Gets or sets the fields picker header template.
        /// </summary>
        /// <value>The fields picker header template.</value>
        [Parameter]
        public RenderFragment FieldsPickerHeaderTemplate { get; set; }

        /// <summary>
        /// Gets or sets the fields picker header text.
        /// </summary>
        /// <value>The fields picker header text.</value>
        [Parameter]
        public string FieldsPickerHeaderText { get; set; } = "Settings";

        /// <summary>
        /// Gets or sets value indicating if the fields picker is expanded.
        /// </summary>
        /// <value>The fields picker expanded.</value>
        [Parameter]
        public bool FieldsPickerExpanded { get; set; }

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

        /// <summary>
        /// Gets or sets a value indicating whether sorting is enabled.
        /// </summary>
        [Parameter]
        public bool AllowSorting { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether filtering is enabled.
        /// </summary>
        [Parameter]
        public bool AllowFiltering { get; set; } = true;

        /// <summary>
        /// Gets or sets the filter case sensitivity.
        /// </summary>
        [Parameter]
        public FilterCaseSensitivity FilterCaseSensitivity { get; set; } = FilterCaseSensitivity.Default;

        /// <summary>
        /// Gets or sets the logical filter operator.
        /// </summary>
        [Parameter]
        public LogicalFilterOperator LogicalFilterOperator { get; set; } = LogicalFilterOperator.And;

        /// <summary>
        /// Gets or sets the Rows text.
        /// </summary>
        [Parameter]
        public string RowsText { get; set; } = "Rows";

        /// <summary>
        /// Gets or sets the Columns text.
        /// </summary>
        [Parameter]
        public string ColumnsText { get; set; } = "Columns";

        /// <summary>
        /// Gets or sets the Aggregates text.
        /// </summary>
        [Parameter]
        public string AggregatesText { get; set; } = "Aggregates";

        /// <summary>
        /// Gets or set the filter icon to use.
        /// </summary>
        [Parameter]
        public string FilterIcon { get; set; } = "filter_alt";

        /// <summary>
        /// Gets or sets the filter text.
        /// </summary>
        [Parameter]
        public string FilterText { get; set; } = "Filter";

        /// <summary>
        /// Gets or sets the enum filter select text.
        /// </summary>
        [Parameter]
        public string EnumFilterSelectText { get; set; } = "Select...";

        /// <summary>
        /// Gets or sets the apply text.
        /// </summary>
        [Parameter]
        public string ApplyText { get; set; } = "Apply";

        /// <summary>
        /// Gets or sets the clear text.
        /// </summary>
        [Parameter]
        public string ClearText { get; set; } = "Clear";

        /// <summary>
        /// Gets or sets the filter operator aria label.
        /// </summary>
        [Parameter]
        public string FilterOperatorAriaLabel { get; set; } = "Filter operator";

        /// <summary>
        /// Gets or sets the filter value aria label.
        /// </summary>
        [Parameter]
        public string FilterValueAriaLabel { get; set; } = "Filter value";

        /// <summary>
        /// Gets or sets the second filter operator aria label.
        /// </summary>
        [Parameter]
        public string SecondFilterOperatorAriaLabel { get; set; } = "Second filter operator";

        /// <summary>
        /// Gets or sets the second filter value aria label.
        /// </summary>
        [Parameter]
        public string SecondFilterValueAriaLabel { get; set; } = "Second filter value";

        /// <summary>
        /// Gets or sets the logical operator aria label.
        /// </summary>
        [Parameter]
        public string LogicalOperatorAriaLabel { get; set; } = "Logical operator";

        /// <summary>
        /// Gets or sets the and operator text.
        /// </summary>
        [Parameter]
        public string AndOperatorText { get; set; } = "And";

        /// <summary>
        /// Gets or sets the or operator text.
        /// </summary>
        [Parameter]
        public string OrOperatorText { get; set; } = "Or";

        /// <summary>
        /// Gets or sets the equals text.
        /// </summary>
        /// <value>The equals text.</value>
        [Parameter]
        public string EqualsText { get; set; } = "Equals";

        /// <summary>
        /// Gets or sets the not equals text.
        /// </summary>
        /// <value>The not equals text.</value>
        [Parameter]
        public string NotEqualsText { get; set; } = "Not equals";

        /// <summary>
        /// Gets or sets the less than text.
        /// </summary>
        /// <value>The less than text.</value>
        [Parameter]
        public string LessThanText { get; set; } = "Less than";

        /// <summary>
        /// Gets or sets the less than or equals text.
        /// </summary>
        /// <value>The less than or equals text.</value>
        [Parameter]
        public string LessThanOrEqualsText { get; set; } = "Less than or equals";

        /// <summary>
        /// Gets or sets the greater than text.
        /// </summary>
        /// <value>The greater than text.</value>
        [Parameter]
        public string GreaterThanText { get; set; } = "Greater than";

        /// <summary>
        /// Gets or sets the greater than or equals text.
        /// </summary>
        /// <value>The greater than or equals text.</value>
        [Parameter]
        public string GreaterThanOrEqualsText { get; set; } = "Greater than or equals";

        /// <summary>
        /// Gets or sets the ends with text.
        /// </summary>
        /// <value>The ends with text.</value>
        [Parameter]
        public string EndsWithText { get; set; } = "Ends with";

        /// <summary>
        /// Gets or sets the contains text.
        /// </summary>
        /// <value>The contains text.</value>
        [Parameter]
        public string ContainsText { get; set; } = "Contains";

        /// <summary>
        /// Gets or sets the does not contain text.
        /// </summary>
        /// <value>The does not contain text.</value>
        [Parameter]
        public string DoesNotContainText { get; set; } = "Does not contain";

        /// <summary>
        /// Gets or sets the in operator text.
        /// </summary>
        /// <value>The in operator text.</value>
        [Parameter]
        public string InText { get; set; } = "In";

        /// <summary>
        /// Gets or sets the not in operator text.
        /// </summary>
        /// <value>The not in operator text.</value>
        [Parameter]
        public string NotInText { get; set; } = "Not in";

        /// <summary>
        /// Gets or sets the starts with text.
        /// </summary>
        /// <value>The starts with text.</value>
        [Parameter]
        public string StartsWithText { get; set; } = "Starts with";

        /// <summary>
        /// Gets or sets the not null text.
        /// </summary>
        /// <value>The not null text.</value>
        [Parameter]
        public string IsNotNullText { get; set; } = "Is not null";

        /// <summary>
        /// Gets or sets the is null text.
        /// </summary>
        /// <value>The null text.</value>
        [Parameter]
        public string IsNullText { get; set; } = "Is null";

        /// <summary>
        /// Gets or sets the is empty text.
        /// </summary>
        /// <value>The empty text.</value>
        [Parameter]
        public string IsEmptyText { get; set; } = "Is empty";

        /// <summary>
        /// Gets or sets the is not empty text.
        /// </summary>
        /// <value>The not empty text.</value>
        [Parameter]
        public string IsNotEmptyText { get; set; } = "Is not empty";

        /// <summary>
        /// Gets or sets the custom filter operator text.
        /// </summary>
        /// <value>The custom filter operator text.</value>
        [Parameter]
        public string CustomText { get; set; } = "Custom";

        /// <summary>
        /// Gets or sets the enum filter translation function.
        /// </summary>
        [Parameter]
        public Func<object, string> EnumFilterTranslationFunc { get; set; }

        /// <summary>
        /// Gets or sets whether to allow filter date input.
        /// </summary>
        [Parameter]
        public bool AllowFilterDateInput { get; set; } = true;

        List<RadzenPivotField<TItem>> pivotFields = new List<RadzenPivotField<TItem>>();
        List<RadzenPivotColumn<TItem>> allPivotColumns = new List<RadzenPivotColumn<TItem>>();
        List<RadzenPivotColumn<TItem>> pivotColumns = new List<RadzenPivotColumn<TItem>>();
        List<RadzenPivotRow<TItem>> allPivotRows = new List<RadzenPivotRow<TItem>>();
        List<RadzenPivotRow<TItem>> pivotRows = new List<RadzenPivotRow<TItem>>();
        List<RadzenPivotAggregate<TItem>> allPivotAggregates = new List<RadzenPivotAggregate<TItem>>();
        List<RadzenPivotAggregate<TItem>> pivotAggregates = new List<RadzenPivotAggregate<TItem>>();

        // Drill down state management
        private Dictionary<string, bool> _collapsedColumnGroups = new Dictionary<string, bool>();
        private Dictionary<string, bool> _collapsedRowGroups = new Dictionary<string, bool>();

        private List<List<ColumnHeaderCell>> _columnHeaderRows;
        private List<PivotBodyRow> _cachedPivotRows;
        private List<List<object>> _cachedColumnLeaves;

        // Filter functionality
        private RadzenPivotField<TItem> currentFilterField;
        private Popup filterPopup;

        /// <summary>
        /// Gets the columns collection.
        /// </summary>
        /// <value>The columns collection.</value>
        public IList<RadzenPivotColumn<TItem>> ColumnsCollection
        {
            get
            {
                return pivotColumns;
            }
        }

        /// <summary>
        /// Gets the rows collection.
        /// </summary>
        /// <value>The rows collection.</value>
        public IList<RadzenPivotRow<TItem>> RowsCollection
        {
            get
            {
                return pivotRows;
            }
        }

        /// <summary>
        /// Gets the aggregates collection.
        /// </summary>
        /// <value>The aggregates collection.</value>
        public IList<RadzenPivotAggregate<TItem>> AggregatesCollection
        {
            get
            {
                return pivotAggregates;
            }
        }

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
                    PathKey = pathKey,
                    HeaderTemplate = col.HeaderTemplate,
                    Group = group
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
                    PathKey = node.PathKey,
                    HasChildren = level < maxLevel,
                    HeaderTemplate = node.HeaderTemplate,
                    Group = node.Group
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
            _view = null;

            if (Data != null && !LoadData.HasDelegate)
            {
                Count = View.Count();
            }

            await InvokeLoadData(skip, PageSize);

            CalculatePager();

            StateHasChanged();
        }

        bool? isOData;

        internal bool IsOData()
        {
            if (isOData == null && Data != null)
            {
                isOData = typeof(ODataEnumerable<TItem>).IsAssignableFrom(Data.GetType());
            }

            return isOData != null ? isOData.Value : false;
        }

        internal async Task InvokeLoadData(int start, int top)
        {
            if (LoadData.HasDelegate)
            {
                var filters = GetFilters();
                var sorts = GetOrderBy();

                await LoadData.InvokeAsync(new Radzen.LoadDataArgs()
                {
                    Skip = start,
                    Top = top,
                    OrderBy = sorts.Item1,
                    GetFilter = () => IsOData() ? filters.ToODataFilterString<TItem>() : filters.ToFilterString<TItem>(),
                    Filters = filters.Concat(filters.SelectManyRecursive(f => f.Filters ?? Enumerable.Empty<CompositeFilterDescriptor>()))
                        .Where(f => f.FilterValue != null
                            || f.FilterOperator == FilterOperator.IsNotNull || f.FilterOperator == FilterOperator.IsNull
                            || f.FilterOperator == FilterOperator.IsEmpty | f.FilterOperator == FilterOperator.IsNotEmpty)
                        .Select(f => new FilterDescriptor() 
                        { 
                            Property = f.Property,
                            FilterProperty = f.FilterProperty,
                            FilterOperator = f.FilterOperator ?? FilterOperator.Equals,
                            FilterValue = f.FilterValue,
                            LogicalFilterOperator = f.LogicalFilterOperator,
                            Type = f.Type
                        }),
                    Sorts = sorts.Item2
                });
            }
        }

        /// <summary>
        /// Adds a pivot field to the pivot grid if it does not already exist.
        /// </summary>
        /// <param name="field">The pivot field to add.</param>
        public void AddPivotField(RadzenPivotField<TItem> field)
        {
            if (!pivotFields.Any(f => f.Property == field.Property))
            {
                pivotFields.Add(field);
                _columnHeaderRows = null;
                _cachedPivotRows = null;
                _cachedColumnLeaves = null;

                UpdateSelected();

                StateHasChanged();
            }
        }

        /// <summary>
        /// Adds a pivot column to the pivot grid if it does not already exist.
        /// </summary>
        /// <param name="column">The pivot column to add.</param>
        public void AddPivotColumn(RadzenPivotColumn<TItem> column)
        {
            if (!allPivotColumns.Contains(column))
            {
                allPivotColumns.Add(column);

                pivotColumns.Add(column);

                _columnHeaderRows = null;
                _cachedPivotRows = null;
                _cachedColumnLeaves = null;

                UpdateSelected();

                StateHasChanged();
            }
        }

        /// <summary>
        /// Adds a pivot row to the pivot grid if it does not already exist.
        /// </summary>
        /// <param name="row">The pivot row to add.</param>
        public void AddPivotRow(RadzenPivotRow<TItem> row)
        {
            if (!allPivotRows.Contains(row))
            {
                allPivotRows.Add(row);

                pivotRows.Add(row);

                _columnHeaderRows = null;
                _cachedPivotRows = null;
                _cachedColumnLeaves = null;

                UpdateSelected();

                StateHasChanged();
            }
        }
        
        /// <summary>
        /// Adds a pivot aggregate to the pivot grid if it does not already exist.
        /// </summary>
        /// <param name="aggregate">The pivot aggregate to add.</param>
        public void AddPivotAggregate(RadzenPivotAggregate<TItem> aggregate)
        {
            if (!allPivotAggregates.Contains(aggregate))
            {
                allPivotAggregates.Add(aggregate);

                pivotAggregates.Add(aggregate);

                _columnHeaderRows = null;
                _cachedPivotRows = null;
                _cachedColumnLeaves = null;

                UpdateSelected();

                StateHasChanged();
            }
        }

        internal void RemovePivotColumn(RadzenPivotColumn<TItem> column)
        {
            if (allPivotColumns.Contains(column))
            {
                allPivotColumns.Remove(column);
                pivotColumns.Remove(column);
                _columnHeaderRows = null;
                _cachedPivotRows = null;
                _cachedColumnLeaves = null;
                StateHasChanged();
            }
        }

        internal void RemovePivotRow(RadzenPivotRow<TItem> row)
        {
            if (allPivotRows.Contains(row))
            {
                allPivotRows.Remove(row);
                pivotRows.Remove(row);
                _columnHeaderRows = null;
                _cachedPivotRows = null;
                _cachedColumnLeaves = null;
                StateHasChanged();
            }
        }

        internal void RemovePivotField(RadzenPivotField<TItem> row)
        {
            if (pivotFields.Contains(row))
            {
                pivotFields.Remove(row);
                _columnHeaderRows = null;
                _cachedPivotRows = null;
                _cachedColumnLeaves = null;
                StateHasChanged();
            }
        }

        internal void RemovePivotAggregate(RadzenPivotAggregate<TItem> aggregate)
        {
            if (allPivotAggregates.Contains(aggregate))
            {
                allPivotAggregates.Remove(aggregate);
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
                        if (AllowDrillDown && !string.IsNullOrEmpty(rowHeaderCell.PathKey) && rowHeaderCell.HasChildren)
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
                            var items = GetRowItems(pivotRow);
                            var rowTotal = GetAggregateValue(items, aggregate);

                            if (aggregate.RowTotalTemplate != null)
                            {
                                var context = new RadzenPivotAggreateContext<TItem>()
                                {
                                    View = items,
                                    Aggregate = aggregate,
                                    Value = rowTotal,
                                    Index = rowIndex
                                };

                                builder.AddContent(15, aggregate.RowTotalTemplate(context));
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
                            var property = pivotColumns[i].Property;
                            var value = ExpressionSerializer.FormatValue(colPath[i]);
                            items = property.Contains("it[") ? 
                                items.Where($"it => {property} == {value}") : items.Where($"i => i.{property} == {value}");

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
                            var context = new RadzenPivotAggreateContext<TItem>() 
                            { 
                                View = View,
                                Aggregate = aggregate, 
                                Value= total
                            };

                            builder.AddContent(seq++, aggregate.RowTotalTemplate(context));
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

            // Filter items based on row header values. Ignore padded row header cells (they have null PathKey).
            for (int i = 0; i < pivotRow.RowHeaderCells.Count && i < pivotRows.Count; i++)
            {
                var cell = pivotRow.RowHeaderCells[i];
                if (cell.PathKey == null)
                {
                    continue; // Skip padding cells added to align depths
                }

                var property = pivotRows[i].Property;
                var value = ExpressionSerializer.FormatValue(cell.Value);
                items = property.Contains("it[") ?
                       items.Where($@"it => {property} == {value}") : items.Where($@"i => i.{property} == {value}");
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
        public object GetAggregateValue(IQueryable<TItem> items, RadzenPivotAggregate<TItem> aggregate)
        {
            if (items == null || !items.Any())
                return null;

            try
            {
                var propertyType = PropertyAccess.GetPropertyType(typeof(TItem), aggregate.Property);
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
                    values = propertyType != null ? items.Select(aggregate.Property).Cast(propertyType) : items.Select(aggregate.Property);
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
                            var property = pivotColumns[i].Property;
                            var value = ExpressionSerializer.FormatValue(colPath[i]);
                            items = property.Contains("it[") ?
                                items.Where($"it => {property} == {value}") : items.Where($"i => i.{property} == {value}");
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
                        Value = child.Value,
                        Title = child.Title,
                        RowSpan = GetLeafCount(child),
                        IsCollapsed = child.IsCollapsed,
                        PathKey = child.PathKey,
                        HasChildren = child.Level < pivotRows.Count
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
                                    var property = pivotRows[i].Property;
                                    var value = ExpressionSerializer.FormatValue(newPrefix[i].Value);
                                    items = property.Contains("it[") ?
                                        items.Where($@"it => {property} == {value}") : items.Where($@"i => i.{property} == {value}");
                                }
                                // Filter by column path
                                for (int i = 0; i < colPath.Count; i++)
                                {
                                    var property = pivotColumns[i].Property;
                                    var value = ExpressionSerializer.FormatValue(colPath[i]);
                                    items = property.Contains("it[") ?
                                        items.Where($"it => {property} == {value}") : items.Where($"i => i.{property} == {value}");
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
            public bool HasChildren { get; set; }
            public RenderFragment<GroupResult> HeaderTemplate { get; set; }
            public GroupResult Group { get; set; }
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
            public bool HasChildren { get; set; }
            public RenderFragment<GroupResult> HeaderTemplate { get; set; }
            public GroupResult Group { get; set; }
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
            public bool HasChildren { get; set; }
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

        /// <summary>
        /// Handles aggregate click for sorting.
        /// </summary>
        internal async Task OnAggregateSort(EventArgs args, RadzenPivotAggregate<TItem> aggregate)
        {
            await HandleFieldSort(pivotAggregates, aggregate);
        }

        /// <summary>
        /// Handles column header click for sorting.
        /// </summary>
        internal async Task OnColumnSort(EventArgs args, RadzenPivotColumn<TItem> column)
        {
            await HandleFieldSort(pivotColumns, column);
        }

        /// <summary>
        /// Handles row header click for sorting.
        /// </summary>
        internal async Task OnRowSort(EventArgs args, RadzenPivotRow<TItem> row)
        {
            await HandleFieldSort(pivotRows, row);
        }

        private async Task HandleFieldSort<T>(List<T> allFields, T sortedField)
            where T : RadzenPivotField<TItem>
        {
            if (AllowSorting && sortedField.Sortable)
            {
                // Toggle sort order
                var sequence = sortedField.SortOrderSequence;

                var pos = Array.IndexOf(sequence, sortedField.GetSortOrder());

                var nextSortOrder = pos == -1 || pos + 1 >= sequence.Length
                    ? sequence.FirstOrDefault()
                    : sequence[pos + 1];

                sortedField.SetSortOrderInternal(nextSortOrder);

                // Clear other column sorts if single column sorting
                if (sortedField.GetSortOrder() != null)
                {
                    foreach (var col in allFields.Where(c => c != sortedField))
                    {
                        col.SetSortOrderInternal(null);
                    }
                }

                await Reload();
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets the column for a header cell.
        /// </summary>
        private RadzenPivotColumn<TItem> GetColumnForHeaderCell(ColumnHeaderCell cell)
        {
            if (cell.Level < pivotColumns.Count)
            {
                return pivotColumns[cell.Level];
            }
            return null;
        }

        /// <summary>
        /// Handles keyboard events for sorting.
        /// </summary>
        private async Task OnSortKeyPressed(KeyboardEventArgs args)
        {
            var key = args.Code ?? args.Key;
            if (key == "Enter" || key == " ")
            {
                // The actual sorting will be handled by the onclick event
                // This is just for keyboard accessibility
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets the filter icon CSS class for a field.
        /// </summary>
        private string GetFilterIconCss(RadzenPivotField<TItem> field)
        {
            var additionalStyle = field?.HasActiveFilter() == true ? "rz-grid-filter-active" : "";
            return $"notranslate rzi rz-grid-filter-icon {additionalStyle}";
        }

        /// <summary>
        /// Gets or sets the current filter icon reference.
        /// </summary>
        private Dictionary<string, ElementReference> FilterIconRef = new();

        /// <summary>
        /// Toggles the filter popup for a field.
        /// </summary>
        private async Task ToggleFilter(RadzenPivotField<TItem> field, string filterIconRefKey)
        {
            if (field == null || !AllowFiltering || !field.Filterable)
                return;

            currentFilterField = field;
            StateHasChanged();

            await Task.Yield();

            if (filterPopup != null)
            {
                await filterPopup.ToggleAsync(FilterIconRef[filterIconRefKey]);
            }
        }

        /// <summary>
        /// Gets the filter property type for a field.
        /// </summary>
        private Type GetFilterPropertyType(RadzenPivotField<TItem> field)
        {
            if (field == null || string.IsNullOrEmpty(field.Property))
                return typeof(string);

            return PropertyAccess.GetPropertyType(typeof(TItem), field.Property) ?? typeof(string);
        }

        /// <summary>
        /// Gets the filter operators for a field.
        /// </summary>
        private IEnumerable<FilterOperator> GetFilterOperators(RadzenPivotField<TItem> field)
        {
            var propertyType = GetFilterPropertyType(field);
            var operators = new List<FilterOperator>();

            if (PropertyAccess.IsNumeric(propertyType))
            {
                operators.AddRange(new[] { FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.LessThan, FilterOperator.LessThanOrEquals, FilterOperator.GreaterThan, FilterOperator.GreaterThanOrEquals, FilterOperator.IsNull, FilterOperator.IsNotNull });
            }
            else if (PropertyAccess.IsDate(propertyType))
            {
                operators.AddRange(new[] { FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.LessThan, FilterOperator.LessThanOrEquals, FilterOperator.GreaterThan, FilterOperator.GreaterThanOrEquals, FilterOperator.IsNull, FilterOperator.IsNotNull });
            }
            else if (propertyType == typeof(bool) || propertyType == typeof(bool?))
            {
                operators.AddRange(new[] { FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.IsNull, FilterOperator.IsNotNull });
            }
            else if (PropertyAccess.IsNullableEnum(propertyType) || PropertyAccess.IsEnum(propertyType))
            {
                operators.AddRange(new[] { FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.In, FilterOperator.NotIn, FilterOperator.IsNull, FilterOperator.IsNotNull });
            }
            else
            {
                operators.AddRange(new[] { FilterOperator.Contains, FilterOperator.DoesNotContain, FilterOperator.StartsWith, FilterOperator.EndsWith, FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.In, FilterOperator.NotIn, FilterOperator.IsNull, FilterOperator.IsNotNull });
            }

            return operators;
        }

        /// <summary>
        /// Gets the filter operator text.
        /// </summary>
        public string GetFilterOperatorText(FilterOperator? filterOperator)
        {
            switch (filterOperator)
            {
                case FilterOperator.Custom:
                    return CustomText;
                case FilterOperator.Contains:
                    return ContainsText;
                case FilterOperator.DoesNotContain:
                    return DoesNotContainText;
                case FilterOperator.EndsWith:
                    return EndsWithText;
                case FilterOperator.Equals:
                    return EqualsText;
                case FilterOperator.GreaterThan:
                    return GreaterThanText;
                case FilterOperator.GreaterThanOrEquals:
                    return GreaterThanOrEqualsText;
                case FilterOperator.LessThan:
                    return LessThanText;
                case FilterOperator.LessThanOrEquals:
                    return LessThanOrEqualsText;
                case FilterOperator.StartsWith:
                    return StartsWithText;
                case FilterOperator.NotEquals:
                    return NotEqualsText;
                case FilterOperator.IsNull:
                    return IsNullText;
                case FilterOperator.IsEmpty:
                    return IsEmptyText;
                case FilterOperator.IsNotNull:
                    return IsNotNullText;
                case FilterOperator.IsNotEmpty:
                    return IsNotEmptyText;
                default:
                    return $"{filterOperator}";
            }
        }

        /// <summary>
        /// Checks if a field can set filter value.
        /// </summary>
        private bool CanSetFilterValue(RadzenPivotField<TItem> field, bool isFirst = true)
        {
            if (field == null)
                return false;

            var filterOperator = isFirst ? field.GetFilterOperator() : field.GetSecondFilterOperator();
            return filterOperator != FilterOperator.IsNull && filterOperator != FilterOperator.IsNotNull;
        }

        /// <summary>
        /// Sets the filter operator for a field.
        /// </summary>
        private void SetFilterOperator(RadzenPivotField<TItem> field, FilterOperator filterOperator)
        {
            if (field != null)
            {
                field.SetFilterOperatorInternal(filterOperator);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Sets the second filter operator for a field.
        /// </summary>
        private void SetSecondFilterOperator(RadzenPivotField<TItem> field, FilterOperator filterOperator)
        {
            if (field != null)
            {
                field.SetSecondFilterOperatorInternal(filterOperator);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Sets the logical filter operator for a field.
        /// </summary>
        private void SetLogicalFilterOperator(RadzenPivotField<TItem> field, LogicalFilterOperator logicalOperator)
        {
            if (field != null)
            {
                field.SetLogicalFilterOperatorInternal(logicalOperator);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Sets the filter value for a field.
        /// </summary>
        private void SetFilterValue(RadzenPivotField<TItem> field, object value, bool isFirst = true)
        {
            if (field != null)
            {
                if (isFirst)
                {
                    field.SetFilterValueInternal(value);
                }
                else
                {
                    field.SetSecondFilterValueInternal(value);
                }
                InvokeAsync(OnFilterChanged);
            }
        }

        /// <summary>
        /// Applies the current filter.
        /// </summary>
        private async Task ApplyFilter(string filterIconRefKey)
        {
            if (currentFilterField != null)
            {
                // Trigger filter change event
                await OnFilterChanged();
                
                if (filterPopup != null)
                {
                    await filterPopup.CloseAsync(FilterIconRef[filterIconRefKey]);
                }
            }
        }

        /// <summary>
        /// Clears the current filter.
        /// </summary>
        private async Task ClearFilter(string filterIconRefKey)
        {
            if (currentFilterField != null)
            {
                currentFilterField.ClearFilterValues();
                StateHasChanged();
                
                // Trigger filter change event
                await OnFilterChanged();
                
                if (filterPopup != null)
                {
                    await filterPopup.CloseAsync(FilterIconRef[filterIconRefKey]);
                }
            }
        }

        /// <summary>
        /// Handles filter popup key pressed events.
        /// </summary>
        private async Task OnFilterPopupKeyPressed(KeyboardEventArgs args, string filterIconRefKey)
        {
            var key = args.Code ?? args.Key;
            if (key == "Escape")
            {
                if (filterPopup != null && currentFilterField != null)
                {
                    await filterPopup.CloseAsync(FilterIconRef[filterIconRefKey]);
                }
            }
        }
        
        async Task OnFilterChanged()
        {
            await InvokeAsync(Reload);
        }

        async Task SetFilterValueAndReload(RadzenPivotField<TItem> field, object value, bool isFirst = true)
        {
            SetFilterValue(field, value, isFirst);
            await InvokeAsync(Reload);
        }

        /// <summary>
        /// Draws numeric filter UI.
        /// </summary>
        private RenderFragment DrawNumericFilter(RadzenPivotField<TItem> field, bool force = true, bool isFirst = true)
        {
            return builder =>
            {
                var propertyType = field.FilterPropertyType ?? GetFilterPropertyType(field);

                var type = Nullable.GetUnderlyingType(propertyType) != null ?
                   propertyType : typeof(Nullable<>).MakeGenericType(propertyType);

                var numericType = typeof(RadzenNumeric<>).MakeGenericType(type);

                builder.OpenComponent(0, numericType);

                builder.AddAttribute(1, "Value", isFirst ? field.GetFilterValue() : field.GetSecondFilterValue());
                builder.AddAttribute(2, "ShowUpDown", field.ShowUpDownForNumericFilter());
                builder.AddAttribute(3, "Style", "width:100%");
                builder.AddAttribute(4, "InputAttributes", new Dictionary<string, object>() { { "aria-label", field.Title + $"{(!isFirst ? " second " : " ")}filter value " + (isFirst ? field.GetFilterValue() : field.GetSecondFilterValue()) } });
                builder.AddAttribute(5, "id", getFilterInputId(field) + (isFirst ? "f" : "s"));

                Action<object> action;
                if (force)
                {
                    action = args => InvokeAsync(() => SetFilterValueAndReload(field, args, isFirst));
                }
                else
                {
                    action = args => { SetFilterValue(field, args, isFirst); };
                }

                var eventCallbackGenericCreate = typeof(NumericFilterEventCallback).GetMethod("Create").MakeGenericMethod(type);
                var eventCallbackGenericAction = typeof(NumericFilterEventCallback).GetMethod("Action").MakeGenericMethod(type);

                builder.AddAttribute(3, "Change", eventCallbackGenericCreate.Invoke(this,
                    new object[] { this, eventCallbackGenericAction.Invoke(this, new object[] { action }) }));

                builder.AddAttribute(4, "Disabled", !field.CanSetFilterValue());

                builder.CloseComponent();
            };
        }

        internal string getFilterInputId(RadzenPivotField<TItem> field)
        {
            return string.Join("", $"{UniqueID}".Split('.')) + field.GetFilterProperty();
        }

        internal class NumericFilterEventCallback
        {
            public static EventCallback<T> Create<T>(object receiver, Action<T> action)
            {
                return EventCallback.Factory.Create<T>(receiver, action);
            }

            public static Action<T> Action<T>(Action<object> action)
            {
                return args => action(args);
            }
        }

        /// <summary>
        /// Checks if field should show time for datetime filter.
        /// </summary>
        private bool ShowTimeForDateTimeFilter(RadzenPivotField<TItem> field)
        {
            var propertyType = GetFilterPropertyType(field);
            return PropertyAccess.IsDate(propertyType) && !PropertyAccess.IsDateOnly(propertyType);
        }

        /// <summary>
        /// Gets the filter date format for a field.
        /// </summary>
        private string GetFilterDateFormat(RadzenPivotField<TItem> field)
        {
            var propertyType = GetFilterPropertyType(field);
            if (PropertyAccess.IsDateOnly(propertyType))
                return "yyyy-MM-dd";
            else if (PropertyAccess.IsDate(propertyType))
                return "yyyy-MM-dd HH:mm";
            else
                return "yyyy-MM-dd";
        }

        /// <summary>
        /// Gets the order by string for sorting.
        /// </summary>
        internal Tuple<string, IEnumerable<SortDescriptor>> GetOrderBy()
        {
            var sortExpressions = new List<string>();
            var sortDescriptors = new List<SortDescriptor>();

            // Add column sorts
            foreach (var column in pivotColumns.Cast<RadzenPivotField<TItem>>().Concat(pivotAggregates).Where(c => c.GetSortOrder() != null))
            {
                var property = column.Property;
                if (!string.IsNullOrEmpty(property))
                {
                    var sortOrder = column.GetSortOrder() == SortOrder.Ascending ? "asc" : "desc";
                    sortExpressions.Add($"{property} {sortOrder}");
                    sortDescriptors.Add(new SortDescriptor { Property = property, SortOrder = column.GetSortOrder() });
                }
            }

            // Add row sorts
            foreach (var row in pivotRows.Where(r => r.GetSortOrder() != null))
            {
                var property = row.Property;
                if (!string.IsNullOrEmpty(property))
                {
                    var sortOrder = row.GetSortOrder() == SortOrder.Ascending ? "asc" : "desc";
                    sortExpressions.Add($"{property} {sortOrder}");
                    sortDescriptors.Add(new SortDescriptor { Property = property, SortOrder = row.GetSortOrder() });
                }
            }

            return new Tuple<string, IEnumerable<SortDescriptor>>(string.Join(",", sortExpressions), sortDescriptors);
        }

        /// <summary>
        /// Gets the filter string for filtering.
        /// </summary>
        internal List<CompositeFilterDescriptor> GetFilters()
        {
            var filterExpressions = new List<CompositeFilterDescriptor>();

            // Add column filters
            foreach (var column in pivotColumns.Cast<RadzenPivotField<TItem>>().Concat(pivotAggregates).Where(c => c.HasActiveFilter()))
            {
                var property = column.GetFilterProperty();
                if (!string.IsNullOrEmpty(property))
                {
                    var filterValue = column.GetFilterValue();
                    var secondFilterValue = column.GetSecondFilterValue();
                    var filterOperator = column.GetFilterOperator();
                    var secondFilterOperator = column.GetSecondFilterOperator();
                    var logicalOperator = column.GetLogicalFilterOperator();

                    var innerFilterExpressions = new List<CompositeFilterDescriptor>();

                    var filterExpression = BuildFilterExpression(property, filterValue, filterOperator, column.Type);
                    innerFilterExpressions.Add(filterExpression);

                    if (secondFilterValue != null)
                    {
                        var secondFilterExpression = BuildFilterExpression(property, secondFilterValue, secondFilterOperator, column.Type);
                        innerFilterExpressions.Add(secondFilterExpression);
                    }

                    filterExpressions.Add(new CompositeFilterDescriptor() 
                    { 
                        Property = property,
                        Filters = innerFilterExpressions,  
                        LogicalFilterOperator = logicalOperator
                    });
                }
            }

            // Add row filters
            foreach (var row in pivotRows.Where(r => r.HasActiveFilter()))
            {
                var property = row.GetFilterProperty();
                if (!string.IsNullOrEmpty(property))
                {
                    var filterValue = row.GetFilterValue();
                    var secondFilterValue = row.GetSecondFilterValue();
                    var filterOperator = row.GetFilterOperator();
                    var secondFilterOperator = row.GetSecondFilterOperator();
                    var logicalOperator = row.GetLogicalFilterOperator();

                    var innerFilterExpressions = new List<CompositeFilterDescriptor>();

                    var filterExpression = BuildFilterExpression(property, filterValue, filterOperator, row.Type);
                    innerFilterExpressions.Add(filterExpression);

                    if (secondFilterValue != null)
                    {
                        var secondFilterExpression = BuildFilterExpression(property, secondFilterValue, secondFilterOperator, row.Type);
                        innerFilterExpressions.Add(secondFilterExpression);
                    }

                    filterExpressions.Add(new CompositeFilterDescriptor()
                    {
                        Property = property,
                        Filters = innerFilterExpressions,
                        LogicalFilterOperator = logicalOperator
                    });
                }
            }

            return filterExpressions;
        }

        /// <summary>
        /// Builds a filter expression for a property and value.
        /// </summary>
        private CompositeFilterDescriptor BuildFilterExpression(string property, object value, FilterOperator filterOperator, Type type)
        {
            return new CompositeFilterDescriptor
            {
                Property = property,
                FilterValue = value,
                FilterOperator = filterOperator,
                Type = type
            };
        }

        /// <summary>
        /// Override the View property to apply sorting and filtering.
        /// </summary>
        public override IQueryable<TItem> View
        {
            get
            {
                var baseView = base.View;

                var filters = GetFilters();
                var orderBy = GetOrderBy();
                
                if (filters.Any())
                {
                    baseView = baseView.Where(filters, LogicalFilterOperator, FilterCaseSensitivity);
                }

                if (!string.IsNullOrEmpty(orderBy.Item1))
                {
                    baseView = baseView.OrderBy(orderBy.Item1);
                }

                return baseView;
            }
        }

        List<string> selectedRows = new();
        List<string> selectedColumns = new();
        List<RadzenPivotAggregate<TItem>> selectedAggregates = new();

        IEnumerable<AggregateFunction> allAggregates =>
            Enum.GetValues(typeof(AggregateFunction)).Cast<AggregateFunction>();

        IEnumerable<AggregateFunction> numericAggregates = new[]
        {
            AggregateFunction.Sum,
            AggregateFunction.Average,
            AggregateFunction.Min,
            AggregateFunction.Max
        };

        IEnumerable<AggregateFunction> GetAggregates(string propertyName)
        {
            return PropertyAccess.IsNumeric(PropertyAccess.GetPropertyType(typeof(TItem), propertyName)) ?
                allAggregates : allAggregates.Except(numericAggregates);
        }

        async Task UpdateAggregates(object value)
        {
            selectedAggregates = ((IEnumerable<string>)value).Select(property =>
            {
                return selectedAggregates.FirstOrDefault(a => a.Property == property) ??
                    new RadzenPivotAggregate<TItem>
                    {
                        Property = property,
                        Title = pivotAggregates.FirstOrDefault(a => a.Property == property)?.Title,
                        Aggregate = AggregateFunction.Count
                    };
            }).ToList();

            await UpdateFieldsFromSelected();
        }

        async Task UpdateAggregate(string property, object value)
        {
            var aggregate = selectedAggregates.FirstOrDefault(a => a.Property == property);
            if (aggregate != null)
            {
                aggregate.Aggregate = (AggregateFunction?)value ?? default(AggregateFunction);
            }

            aggregate = pivotAggregates.FirstOrDefault(a => a.Property == property);
            if (aggregate != null)
            {
                aggregate.Aggregate = (AggregateFunction?)value ?? default(AggregateFunction);
            }

            await Reload();
        }

        void UpdateSelected()
        {
            selectedRows = allPivotRows.Select(r => r.Property).ToList();
            selectedColumns = allPivotColumns.Select(c => c.Property).ToList();
            selectedAggregates = allPivotAggregates.ToList();
        }

        async Task UpdateFieldsFromSelected()
        {
            pivotRows = selectedRows.Select(sr =>
            {
                var row = allPivotRows.FirstOrDefault(r => r.Property == sr);
                if (row != null)
                {
                    return row;
                }

                var field = pivotFields.FirstOrDefault(r => r.Property == sr);
                return new RadzenPivotRow<TItem>
                {
                    Property = field.Property,
                    Title = field.Title
                };
            }).ToList();

            pivotColumns = selectedColumns.Select(sc =>
            {
                var column = allPivotColumns.FirstOrDefault(c => c.Property == sc);
                if (column != null)
                {
                    return column;
                }
                var field = pivotFields.FirstOrDefault(c => c.Property == sc);
                return new RadzenPivotColumn<TItem>
                {
                    Property = field.Property,
                    Title = field.Title
                };
            }).ToList();

            pivotAggregates = selectedAggregates.Select(sa =>
            {
                var aggregate = allPivotAggregates.FirstOrDefault(a => a.Property == sa.Property);
                if (aggregate != null)
                {
                    return aggregate;
                }

                var field = pivotFields.FirstOrDefault(c => c.Property == sa.Property);
                return new RadzenPivotAggregate<TItem>
                {
                    Property = field.Property,
                    Title = field.Title,
                    Aggregate = GetAggregates(field.Property).FirstOrDefault()
                };
            }).ToList();

            await Reload();
        }
    }
}
