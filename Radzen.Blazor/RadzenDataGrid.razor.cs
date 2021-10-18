using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenDataGrid.
    /// Implements the <see cref="Radzen.PagedDataBoundComponent{TItem}" />
    /// </summary>
    /// <typeparam name="TItem">The type of the t item.</typeparam>
    /// <seealso cref="Radzen.PagedDataBoundComponent{TItem}" />
    public partial class RadzenDataGrid<TItem> : PagedDataBoundComponent<TItem>
    {
#if NET5
        internal void SetAllowVirtualization(bool allowVirtualization)
        {
            AllowVirtualization = allowVirtualization;
        }

        internal Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<TItem> virtualize;

        public Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<TItem> Virtualize
        {
            get
            {
                return virtualize;
            }
        }

        List<TItem> virtualDataItems = new List<TItem>();

        private async ValueTask<Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<TItem>> LoadItems(Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderRequest request)
        {
            var view = AllowPaging ? PagedView : View;
            var totalItemsCount = LoadData.HasDelegate ? Count : view.Count();
            var top = totalItemsCount > request.Count ? Math.Min(request.Count, totalItemsCount - request.StartIndex) : PageSize;

            if(top <= 0)
            {
                top = PageSize;
            }

            if (LoadData.HasDelegate)
            {
                var orderBy = GetOrderBy();

                Query.Skip = request.StartIndex;
                Query.Top = top;
                Query.OrderBy = orderBy;

                var filterString = columns.ToFilterString<TItem>();
                Query.Filter = filterString;

                await LoadData.InvokeAsync(new Radzen.LoadDataArgs() { Skip = request.StartIndex, Top = top, OrderBy = orderBy, Filter = IsOData() ? columns.ToODataFilterString<TItem>() : filterString });
            }

            virtualDataItems = (LoadData.HasDelegate ? Data : itemToInsert != null ? (new[] { itemToInsert }).Concat(view.Skip(request.StartIndex).Take(top)) : view.Skip(request.StartIndex).Take(top)).ToList();

            return new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderResult<TItem>(virtualDataItems, totalItemsCount);
        }
#endif
        /// <summary>
        /// Draws the rows.
        /// </summary>
        /// <param name="visibleColumns">The visible columns.</param>
        /// <returns>RenderFragment.</returns>
        RenderFragment DrawRows(IList<RadzenDataGridColumn<TItem>> visibleColumns)
        {
            return new RenderFragment(builder =>
            {
    #if NET5
                if (AllowVirtualization)
                {
                    builder.OpenComponent(0, typeof(Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<TItem>));
                    builder.AddAttribute(1, "ItemsProvider", new Microsoft.AspNetCore.Components.Web.Virtualization.ItemsProviderDelegate<TItem>(LoadItems));
                    builder.AddAttribute(2, "ChildContent", (RenderFragment<TItem>)((context) =>
                    {
                        return (RenderFragment)((b) =>
                        {
                            b.OpenComponent<RadzenDataGridRow<TItem>>(3);
                            b.AddAttribute(4, "Columns", visibleColumns);
                            b.AddAttribute(5, "Grid", this);
                            b.AddAttribute(6, "TItem", typeof(TItem));
                            b.AddAttribute(7, "Item", context);
                            b.AddAttribute(8, "InEditMode", IsRowInEditMode(context));
                            b.AddAttribute(9, "Index", virtualDataItems.IndexOf(context));

                            if (editContexts.ContainsKey(context))
                            {
                                b.AddAttribute(10, nameof(RadzenDataGridRow<TItem>.EditContext), editContexts[context]);
                            }

                            b.SetKey(context);
                            b.CloseComponent();
                        });
                    }));

                    builder.AddComponentReferenceCapture(8, c => { virtualize = (Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize<TItem>)c; });

                    builder.CloseComponent();
                }
                else
                {
                    DrawGroupOrDataRows(builder, visibleColumns);
                }
    #else
                DrawGroupOrDataRows(builder, visibleColumns);
    #endif
            });
        }

        /// <summary>
        /// Draws the group or data rows.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="visibleColumns">The visible columns.</param>
        internal void DrawGroupOrDataRows(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, IList<RadzenDataGridColumn<TItem>> visibleColumns)
        {
            if (groups.Any())
            {
                foreach (var group in GroupedPagedView)
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridGroupRow<TItem>));
                    builder.AddAttribute(1, "Columns", visibleColumns);
                    builder.AddAttribute(3, "Grid", this);
                    builder.AddAttribute(5, "GroupResult", group);
                    builder.AddAttribute(6, "Builder", builder);
                    builder.CloseComponent();
                }
            }
            else
            {
                int i = 0;
                foreach (var item in PagedView)
                {
                    builder.OpenComponent<RadzenDataGridRow<TItem>>(0);
                    builder.AddAttribute(1, "Columns", visibleColumns);
                    builder.AddAttribute(2, "Index", i);
                    builder.AddAttribute(3, "Grid", this);
                    builder.AddAttribute(4, "TItem", typeof(TItem));
                    builder.AddAttribute(5, "Item", item);
                    builder.AddAttribute(6, "InEditMode", IsRowInEditMode(item));

                    if (editContexts.ContainsKey(item))
                    {
                        builder.AddAttribute(7, nameof(RadzenDataGridRow<TItem>.EditContext), editContexts[item]);
                    }

                    builder.CloseComponent();
                    i++;
                }
            }
        }

        /// <summary>
        /// The grouped paged view
        /// </summary>
        IEnumerable<GroupResult> _groupedPagedView;
        /// <summary>
        /// Gets the grouped paged view.
        /// </summary>
        /// <value>The grouped paged view.</value>
        public IEnumerable<GroupResult> GroupedPagedView
        {
            get
            {
                if(_groupedPagedView == null)
                {
                    _groupedPagedView = PagedView.GroupByMany(groups.Select(g => $"np({g.Property})").ToArray()).ToList();
                 }
                return _groupedPagedView;
            }
        }

        /// <summary>
        /// Gets the frozen column class.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <param name="visibleColumns">The visible columns.</param>
        /// <returns>System.String.</returns>
        internal string getFrozenColumnClass(RadzenDataGridColumn<TItem> column, IList<RadzenDataGridColumn<TItem>> visibleColumns)
        {
            return column.Frozen ? "rz-frozen-cell" : "";
        }

        /// <summary>
        /// Dates the filter operator style.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <param name="value">The value.</param>
        /// <returns>System.String.</returns>
        protected string DateFilterOperatorStyle(RadzenDataGridColumn<TItem> column, FilterOperator value)
        {
            return column.GetFilterOperator() == value ?
                "rz-listbox-item  rz-state-highlight" :
                "rz-listbox-item ";
        }

        /// <summary>
        /// Called when [filter key press].
        /// </summary>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <param name="column">The column.</param>
        protected void OnFilterKeyPress(EventArgs args, RadzenDataGridColumn<TItem> column)
        {
            Debounce(() => DebounceFilter(column), FilterDelay);
        }

        /// <summary>
        /// Debounces the filter.
        /// </summary>
        /// <param name="column">The column.</param>
        async Task DebounceFilter(RadzenDataGridColumn<TItem> column)
        {
            var inputValue = await JSRuntime.InvokeAsync<string>("Radzen.getInputValue", getFilterInputId(column));
            if (!object.Equals(column.GetFilterValue(), inputValue))
            {
                await InvokeAsync(() => { OnFilter(new ChangeEventArgs() { Value = inputValue }, column); });
            }
        }

        /// <summary>
        /// Applies the date filter by filter operator.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <param name="filterOperator">The filter operator.</param>
        protected void ApplyDateFilterByFilterOperator(RadzenDataGridColumn<TItem> column, FilterOperator filterOperator)
        {
            column.SetFilterOperator(filterOperator);
        }

        /// <summary>
        /// The columns
        /// </summary>
        private readonly List<RadzenDataGridColumn<TItem>> columns = new List<RadzenDataGridColumn<TItem>>();

        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        [Parameter]
        public RenderFragment Columns { get; set; }

        /// <summary>
        /// Adds the column.
        /// </summary>
        /// <param name="column">The column.</param>
        internal void AddColumn(RadzenDataGridColumn<TItem> column)
        {
            if (!columns.Contains(column))
            {
                columns.Add(column);

                var descriptor = sorts.Where(d => d.Property == column?.GetSortProperty()).FirstOrDefault();
                if (descriptor == null && column.SortOrder.HasValue)
                {
                    descriptor = new SortDescriptor() { Property = column.Property, SortOrder = column.SortOrder.Value };
                    sorts.Add(descriptor);
                }
            }
        }

        /// <summary>
        /// Removes the column.
        /// </summary>
        /// <param name="column">The column.</param>
        internal void RemoveColumn(RadzenDataGridColumn<TItem> column)
        {
            if (columns.Contains(column))
            {
                columns.Remove(column);
                if (!disposed)
                {
                    try { InvokeAsync(StateHasChanged); } catch { }
                }
            }
        }

        /// <summary>
        /// Gets the filter input identifier.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <returns>System.String.</returns>
        string getFilterInputId(RadzenDataGridColumn<TItem> column)
        {
            return string.Join("", $"{UniqueID}".Split('.')) + column.GetFilterProperty();
        }

        /// <summary>
        /// Gets the filter date format.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <returns>System.String.</returns>
        string getFilterDateFormat(RadzenDataGridColumn<TItem> column)
        {
            if (column != null && !string.IsNullOrEmpty(column.FormatString))
            {
                var formats = column.FormatString.Split(new char[] { '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
                if (formats.Length > 0)
                {
                    var format = formats[0].Trim().Split(':');
                    if (format.Length > 1)
                    {
                        return format[1].Trim();
                    }
                }
            }

            return FilterDateFormat;
        }

        /// <summary>
        /// Draws the numeric filter.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="isFirst">if set to <c>true</c> [is first].</param>
        /// <returns>RenderFragment.</returns>
        RenderFragment DrawNumericFilter(RadzenDataGridColumn<TItem> column, bool force = true, bool isFirst = true)
        {
            return new RenderFragment(builder =>
            {
                var type = Nullable.GetUnderlyingType(column.FilterPropertyType) != null ?
                    column.FilterPropertyType : typeof(Nullable<>).MakeGenericType(column.FilterPropertyType);

                var numericType = typeof(RadzenNumeric<>).MakeGenericType(type);

                builder.OpenComponent(0, numericType);

                builder.AddAttribute(1, "Value", isFirst ? column.GetFilterValue() : column.GetSecondFilterValue());
                builder.AddAttribute(2, "Style", "width:100%");

                Action<object> action;
                if (force)
                {
                    action = args => OnFilter(new ChangeEventArgs() { Value = args }, column, isFirst);
                }
                else
                {
                    action = args => column.SetFilterValue(args, isFirst);
                }

                var eventCallbackGenericCreate = typeof(NumericFilterEventCallback).GetMethod("Create").MakeGenericMethod(type);
                var eventCallbackGenericAction = typeof(NumericFilterEventCallback).GetMethod("Action").MakeGenericMethod(type);

                builder.AddAttribute(3, "Change", eventCallbackGenericCreate.Invoke(this,
                    new object[] { this, eventCallbackGenericAction.Invoke(this, new object[] { action }) }));

                if(FilterMode == FilterMode.Advanced)
                {
                    builder.AddAttribute(4, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, args => {
                        var value = $"{args.Value}";
                        column.SetFilterValue(!string.IsNullOrWhiteSpace(value) ? Convert.ChangeType(value, Nullable.GetUnderlyingType(type)) : null, isFirst);
                    } ));
                }

                builder.CloseComponent();
            });
        }

        /// <summary>
        /// Called when [filter].
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        /// <param name="column">The column.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="isFirst">if set to <c>true</c> [is first].</param>
        protected void OnFilter(ChangeEventArgs args, RadzenDataGridColumn<TItem> column, bool force = false, bool isFirst = true)
        {
            string property = column.GetFilterProperty();
            if (AllowFiltering && column.Filterable)
            {
                if (!object.Equals(isFirst ? column.GetFilterValue() : column.GetSecondFilterValue(), args.Value) || force)
                {
                    column.SetFilterValue(args.Value, isFirst);
                    skip = 0;
                    CurrentPage = 0;

                    if (LoadData.HasDelegate && IsVirtualizationAllowed())
                    {
                        Data = null;
                    }

                    InvokeAsync(Reload);
                }
            }
        }


        /// <summary>
        /// Gets the columns collection.
        /// </summary>
        /// <value>The columns collection.</value>
        public IList<RadzenDataGridColumn<TItem>> ColumnsCollection
        {
            get
            {
                return columns;
            }
        }

        /// <summary>
        /// Gets the filter icon CSS.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <returns>System.String.</returns>
        private string getFilterIconCss(RadzenDataGridColumn<TItem> column)
        {
            var additionalStyle = column.GetFilterValue() != null || column.GetSecondFilterValue() != null ? "rz-grid-filter-active" : "";
            return $"rzi rz-grid-filter-icon {additionalStyle}";
        }

        /// <summary>
        /// Called when [sort].
        /// </summary>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <param name="column">The column.</param>
        protected void OnSort(EventArgs args, RadzenDataGridColumn<TItem> column)
        {
            if (AllowSorting && column.Sortable)
            {
                var property = column.GetSortProperty();
                if (!string.IsNullOrEmpty(property))
                {
                    OrderBy(property);
                }
                else
                {
                    SetColumnSortOrder(column);

                    if (LoadData.HasDelegate && IsVirtualizationAllowed())
                    {
                        Data = null;
                    }

                    InvokeAsync(Reload);
                }
            }
        }

        /// <summary>
        /// Clears the filter.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <param name="closePopup">if set to <c>true</c> [close popup].</param>
        protected async Task ClearFilter(RadzenDataGridColumn<TItem> column, bool closePopup = false)
        {
            if (closePopup)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", $"{PopupID}{column.GetFilterProperty()}");
            }
            column.SetFilterValue(null);
            column.SetFilterValue(null, false);
            column.SetFilterOperator(null);
            column.SetSecondFilterOperator(null);

            skip = 0;
            CurrentPage = 0;

            if (LoadData.HasDelegate && IsVirtualizationAllowed())
            {
                Data = null;
            }

            await InvokeAsync(Reload);
        }

        /// <summary>
        /// Applies the filter.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <param name="closePopup">if set to <c>true</c> [close popup].</param>
        protected async Task ApplyFilter(RadzenDataGridColumn<TItem> column, bool closePopup = false)
        {
            if (closePopup)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", $"{PopupID}{column.GetFilterProperty()}");
            }
            OnFilter(new ChangeEventArgs() { Value = column.GetFilterValue() }, column, true);
        }

        /// <summary>
        /// Cells the attributes.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="column">The column.</param>
        /// <returns>IReadOnlyDictionary&lt;System.String, System.Object&gt;.</returns>
        internal IReadOnlyDictionary<string, object> CellAttributes(TItem item, RadzenDataGridColumn<TItem> column)
        {
            var args = new Radzen.DataGridCellRenderEventArgs<TItem>() { Data = item, Column = column };

            if (CellRender != null)
            {
                CellRender(args);
            }

            return new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(args.Attributes);
        }

        /// <summary>
        /// The row spans
        /// </summary>
        internal Dictionary<int, int> rowSpans = new Dictionary<int, int>();

        /// <summary>
        /// Gets or sets the logical filter operator.
        /// </summary>
        /// <value>The logical filter operator.</value>
        [Parameter]
        public LogicalFilterOperator LogicalFilterOperator { get; set; } = LogicalFilterOperator.And;

        /// <summary>
        /// Gets or sets the filter mode.
        /// </summary>
        /// <value>The filter mode.</value>
        [Parameter]
        public FilterMode FilterMode { get; set; } = FilterMode.Advanced;

        /// <summary>
        /// Gets or sets the expand mode.
        /// </summary>
        /// <value>The expand mode.</value>
        [Parameter]
        public DataGridExpandMode ExpandMode { get; set; } = DataGridExpandMode.Multiple;

        /// <summary>
        /// Gets or sets the edit mode.
        /// </summary>
        /// <value>The edit mode.</value>
        [Parameter]
        public DataGridEditMode EditMode { get; set; } = DataGridEditMode.Multiple;

        /// <summary>
        /// Gets or sets the filter text.
        /// </summary>
        /// <value>The filter text.</value>
        [Parameter]
        public string FilterText { get; set; } = "Filter";

        /// <summary>
        /// Gets or sets the and operator text.
        /// </summary>
        /// <value>The and operator text.</value>
        [Parameter]
        public string AndOperatorText { get; set; } = "And";

        /// <summary>
        /// Gets or sets the or operator text.
        /// </summary>
        /// <value>The or operator text.</value>
        [Parameter]
        public string OrOperatorText { get; set; } = "Or";

        /// <summary>
        /// Gets or sets the apply filter text.
        /// </summary>
        /// <value>The apply filter text.</value>
        [Parameter]
        public string ApplyFilterText { get; set; } = "Apply";

        /// <summary>
        /// Gets or sets the clear filter text.
        /// </summary>
        /// <value>The clear filter text.</value>
        [Parameter]
        public string ClearFilterText { get; set; } = "Clear";

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
        /// Gets or sets the starts with text.
        /// </summary>
        /// <value>The starts with text.</value>
        [Parameter]
        public string StartsWithText { get; set; } = "Starts with";

        /// <summary>
        /// Class NumericFilterEventCallback.
        /// </summary>
        internal class NumericFilterEventCallback
        {
            /// <summary>
            /// Creates the specified receiver.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="receiver">The receiver.</param>
            /// <param name="action">The action.</param>
            /// <returns>EventCallback&lt;T&gt;.</returns>
            public static EventCallback<T> Create<T>(object receiver, Action<T> action)
            {
                return EventCallback.Factory.Create<T>(receiver, action);
            }

            /// <summary>
            /// Actions the specified action.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="action">The action.</param>
            /// <returns>Action&lt;T&gt;.</returns>
            public static Action<T> Action<T>(Action<object> action)
            {
                return args => action(args);
            }
        }

        /// <summary>
        /// Gets or sets the filter case sensitivity.
        /// </summary>
        /// <value>The filter case sensitivity.</value>
        [Parameter]
        public FilterCaseSensitivity FilterCaseSensitivity { get; set; } = FilterCaseSensitivity.Default;

        /// <summary>
        /// Gets or sets the filter delay.
        /// </summary>
        /// <value>The filter delay.</value>
        [Parameter]
        public int FilterDelay { get; set; } = 500;

        /// <summary>
        /// Gets or sets the filter date format.
        /// </summary>
        /// <value>The filter date format.</value>
        [Parameter]
        public string FilterDateFormat { get; set; }

        /// <summary>
        /// Gets or sets the width of the column.
        /// </summary>
        /// <value>The width of the column.</value>
        [Parameter]
        public string ColumnWidth { get; set; }

        /// <summary>
        /// The empty text
        /// </summary>
        private string _emptyText = "No records to display.";
        /// <summary>
        /// Gets or sets the empty text.
        /// </summary>
        /// <value>The empty text.</value>
        [Parameter]
        public string EmptyText
        {
            get { return _emptyText; }
            set
            {
                if (value != _emptyText)
                {
                    _emptyText = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the empty template.
        /// </summary>
        /// <value>The empty template.</value>
        [Parameter]
        public RenderFragment EmptyTemplate { get; set; }
#if NET5
        [Parameter]
        public bool AllowVirtualization { get; set; }
#endif
        /// <summary>
        /// Gets or sets a value indicating whether this instance is loading.
        /// </summary>
        /// <value><c>true</c> if this instance is loading; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool IsLoading { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [allow sorting].
        /// </summary>
        /// <value><c>true</c> if [allow sorting]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowSorting { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [allow multi column sorting].
        /// </summary>
        /// <value><c>true</c> if [allow multi column sorting]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowMultiColumnSorting { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [allow filtering].
        /// </summary>
        /// <value><c>true</c> if [allow filtering]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowFiltering { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [allow column resize].
        /// </summary>
        /// <value><c>true</c> if [allow column resize]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowColumnResize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [allow column reorder].
        /// </summary>
        /// <value><c>true</c> if [allow column reorder]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowColumnReorder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [allow grouping].
        /// </summary>
        /// <value><c>true</c> if [allow grouping]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowGrouping { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether group footers are visible even when the group is collapsed.
        /// </summary>
        /// <value><c>true</c> if group footers are visible when the group is collapsed otherwise, <c>false</c>.</value>
        [Parameter]
        public bool GroupFootersAlwaysVisible { get; set; }

        /// <summary>
        /// Gets or sets the group header template.
        /// </summary>
        /// <value>The group header template.</value>
        [Parameter]
        public RenderFragment<Group> GroupHeaderTemplate { get; set; }

        /// <summary>
        /// Gets or sets the group panel text.
        /// </summary>
        /// <value>The group panel text.</value>
        [Parameter]
        public string GroupPanelText { get; set; } = "Drag a column header here and drop it to group by that column";

        /// <summary>
        /// Gets the column resizer identifier.
        /// </summary>
        /// <param name="columnIndex">Index of the column.</param>
        /// <returns>System.String.</returns>
        internal string getColumnResizerId(int columnIndex)
        {
            return string.Join("", $"{UniqueID}".Split('.')) + columnIndex;
        }

        /// <summary>
        /// Starts the column resize.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        /// <param name="columnIndex">Index of the column.</param>
        internal async Task StartColumnResize(MouseEventArgs args, int columnIndex)
        {
            await JSRuntime.InvokeVoidAsync("Radzen.startColumnResize", getColumnResizerId(columnIndex), Reference, columnIndex, args.ClientX);
        }

        /// <summary>
        /// The index of column to reoder
        /// </summary>
        int? indexOfColumnToReoder;

        /// <summary>
        /// Starts the column reorder.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        /// <param name="columnIndex">Index of the column.</param>
        internal async Task StartColumnReorder(MouseEventArgs args, int columnIndex)
        {
            indexOfColumnToReoder = columnIndex;
            await JSRuntime.InvokeVoidAsync("Radzen.startColumnReorder", getColumnResizerId(columnIndex));
        }

        /// <summary>
        /// Ends the column reorder.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        /// <param name="columnIndex">Index of the column.</param>
        internal async Task EndColumnReorder(MouseEventArgs args, int columnIndex)
        {
            if (indexOfColumnToReoder != null)
            {
                var visibleColumns = columns.Where(c => c.Visible).ToList();
                var columnToReorder = visibleColumns.ElementAtOrDefault(indexOfColumnToReoder.Value);
                var columnToReorderTo = visibleColumns.ElementAtOrDefault(columnIndex);

                if (columnToReorder != null && columnToReorderTo != null)
                {
                    var actualColumnIndex = columns.IndexOf(columnToReorderTo);
                    columns.Remove(columnToReorder);
                    columns.Insert(actualColumnIndex, columnToReorder);

                    await ColumnReordered.InvokeAsync(new DataGridColumnReorderedEventArgs<TItem>
                    {
                        Column = columnToReorder,
                        OldIndex = indexOfColumnToReoder.Value,
                        NewIndex = actualColumnIndex
                    });
                }

                indexOfColumnToReoder = null;
            }
        }

        /// <summary>
        /// Called when [column resized].
        /// </summary>
        /// <param name="columnIndex">Index of the column.</param>
        /// <param name="value">The value.</param>
        [JSInvokable("RadzenGrid.OnColumnResized")]
        public async Task OnColumnResized(int columnIndex, double value)
        {
            var column = columns.Where(c => c.Visible).ToList()[columnIndex];
            column.SetWidth($"{value}px");
            await ColumnResized.InvokeAsync(new DataGridColumnResizedEventArgs<TItem>
            {
                Column = column,
                Width = value,
            });
        }

        /// <summary>
        /// Gets the order by.
        /// </summary>
        /// <returns>System.String.</returns>
        internal string GetOrderBy()
        {
            return string.Join(",", sorts.Select(d => columns.Where(c => c.GetSortProperty() == d.Property).FirstOrDefault()).Where(c => c != null).Select(c => c.GetSortOrderAsString(IsOData())));
        }

        /// <summary>
        /// Gets or sets the column resized.
        /// </summary>
        /// <value>The column resized.</value>
        [Parameter]
        public EventCallback<DataGridColumnResizedEventArgs<TItem>> ColumnResized { get; set; }

        /// <summary>
        /// Gets or sets the column reordered.
        /// </summary>
        /// <value>The column reordered.</value>
        [Parameter]
        public EventCallback<DataGridColumnReorderedEventArgs<TItem>> ColumnReordered { get; set; }

        /// <summary>
        /// Gets the view.
        /// </summary>
        /// <value>The view.</value>
        public override IQueryable<TItem> View
        {
            get
            {
                if(LoadData.HasDelegate)
                {
                    return base.View;
                }

                var view = base.View.Where<TItem>(columns);
                var orderBy = GetOrderBy();

                if (!string.IsNullOrEmpty(orderBy))
                {
                    if (typeof(TItem) == typeof(object))
                    {
                        var firstItem = view.FirstOrDefault();
                        if (firstItem != null)
                        {
                            view = view.Cast(firstItem.GetType()).AsQueryable().OrderBy(orderBy).Cast<TItem>();
                        }
                    }
                    else
                    {
                        view = view.OrderBy(orderBy);
                    }
                }

                if (!IsVirtualizationAllowed() || AllowPaging)
                {
                    var count = view.Count();
                    if (count != Count)
                    {
                        Count = count;

                        if (skip >= Count && Count > PageSize)
                        {
                            skip = Count - PageSize;
                        }

                        StateHasChanged();
                    }
                }

                return view;
            }
        }

        /// <summary>
        /// Determines whether [is virtualization allowed].
        /// </summary>
        /// <returns><c>true</c> if [is virtualization allowed]; otherwise, <c>false</c>.</returns>
        internal bool IsVirtualizationAllowed()
        {
    #if NET5
            return AllowVirtualization;
    #else
            return false;
    #endif
        }


        /// <summary>
        /// The value
        /// </summary>
        IList<TItem> _value;

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public IList<TItem> Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        /// <summary>
        /// Gets or sets the value changed.
        /// </summary>
        /// <value>The value changed.</value>
        [Parameter]
        public EventCallback<IList<TItem>> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the row select.
        /// </summary>
        /// <value>The row select.</value>
        [Parameter]
        public EventCallback<TItem> RowSelect { get; set; }

        /// <summary>
        /// Gets or sets the row deselect.
        /// </summary>
        /// <value>The row deselect.</value>
        [Parameter]
        public EventCallback<TItem> RowDeselect { get; set; }

        /// <summary>
        /// Gets or sets the row click.
        /// </summary>
        /// <value>The row click.</value>
        [Parameter]
        public EventCallback<DataGridRowMouseEventArgs<TItem>> RowClick { get; set; }

        /// <summary>
        /// Gets or sets the row double click.
        /// </summary>
        /// <value>The row double click.</value>
        [Parameter]
        public EventCallback<DataGridRowMouseEventArgs<TItem>> RowDoubleClick { get; set; }

        /// <summary>
        /// Gets or sets the row expand.
        /// </summary>
        /// <value>The row expand.</value>
        [Parameter]
        public EventCallback<TItem> RowExpand { get; set; }

        /// <summary>
        /// Gets or sets the row collapse.
        /// </summary>
        /// <value>The row collapse.</value>
        [Parameter]
        public EventCallback<TItem> RowCollapse { get; set; }

        /// <summary>
        /// Gets or sets the row render.
        /// </summary>
        /// <value>The row render.</value>
        [Parameter]
        public Action<RowRenderEventArgs<TItem>> RowRender { get; set; }

        /// <summary>
        /// Gets or sets the cell render.
        /// </summary>
        /// <value>The cell render.</value>
        [Parameter]
        public Action<DataGridCellRenderEventArgs<TItem>> CellRender { get; set; }

        /// <summary>
        /// Gets or sets the render.
        /// </summary>
        /// <value>The render.</value>
        [Parameter]
        public Action<DataGridRenderEventArgs<TItem>> Render { get; set; }

        /// <summary>
        /// Called when [data changed].
        /// </summary>
        protected override void OnDataChanged()
        {
            Reset(!IsOData() && !LoadData.HasDelegate);
        }

        /// <summary>
        /// Resets the specified reset column state.
        /// </summary>
        /// <param name="resetColumnState">if set to <c>true</c> [reset column state].</param>
        /// <param name="resetRowState">if set to <c>true</c> [reset row state].</param>
        public void Reset(bool resetColumnState = true, bool resetRowState = false)
        {
            _groupedPagedView = null;
            _view = null;
            _value = new List<TItem>();

            if (resetRowState)
            {
                selectedItems.Clear();
                expandedItems.Clear();
            }

            if (resetColumnState)
            {
                columns.ForEach(c => { c.SetFilterValue(null); c.SetSecondFilterOperator(FilterOperator.Equals); });
                columns.ForEach(c => { c.ResetSortOrder(); });
                sorts.Clear();
           }
        }

        /// <summary>
        /// Reloads this instance.
        /// </summary>
        public async override Task Reload()
        {
            _groupedPagedView = null;
            _view = null;

            if (Data != null && !LoadData.HasDelegate)
            {
                Count = 1;
            }
    #if NET5
            if (AllowVirtualization && virtualize != null)
            {
                if(!LoadData.HasDelegate)
                {
                    await virtualize.RefreshDataAsync();
                }
                else
                {
                    Data = null;
                }
            }
    #endif
            var orderBy = GetOrderBy();

            Query.Skip = skip;
            Query.Top = PageSize;
            Query.OrderBy = orderBy;

            var filterString = columns.ToFilterString<TItem>();
            Query.Filter = filterString;

            if (LoadData.HasDelegate)
            {
                await LoadData.InvokeAsync(new Radzen.LoadDataArgs()
                {
                    Skip = skip,
                    Top = PageSize,
                    OrderBy = orderBy,
                    Filter = IsOData() ? columns.ToODataFilterString<TItem>() : filterString,
                    Filters = columns.Where(c => c.Filterable && c.Visible && c.GetFilterValue() != null).Select(c => new FilterDescriptor()
                    {
                        Property = c.GetFilterProperty(),
                        FilterValue = c.GetFilterValue(),
                        FilterOperator = c.GetFilterOperator(),
                        SecondFilterValue = c.GetSecondFilterValue(),
                        SecondFilterOperator = c.GetSecondFilterOperator(),
                        LogicalFilterOperator = c.GetLogicalFilterOperator()
                    }),
                    Sorts = sorts
                }); ;
            }

            CalculatePager();

            if (!LoadData.HasDelegate)
            {
                StateHasChanged();
            }
            else
            {
    #if NET5
                if (AllowVirtualization && virtualize != null)
                {
                    await virtualize.RefreshDataAsync();
                }
    #endif        
            } 
       }

        /// <summary>
        /// Changes the state.
        /// </summary>
        internal async Task ChangeState()
        {
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Called when [parameters set asynchronous].
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task OnParametersSetAsync()
        {
            if (Visible && !LoadData.HasDelegate && _view == null)
            {
                InvokeAsync(Reload);
            }
            else
            {
                CalculatePager();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// The collapsed group items
        /// </summary>
        internal Dictionary<RadzenDataGridGroupRow<TItem>, bool> collapsedGroupItems = new Dictionary<RadzenDataGridGroupRow<TItem>, bool>();
        /// <summary>
        /// Expandeds the group item style.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        internal string ExpandedGroupItemStyle(RadzenDataGridGroupRow<TItem> item)
        {
            return collapsedGroupItems.Keys.Contains(item) ? "rz-row-toggler rzi-grid-sort  rzi-chevron-circle-right" : "rz-row-toggler rzi-grid-sort  rzi-chevron-circle-down";
        }

        /// <summary>
        /// Determines whether [is group item expanded] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [is group item expanded] [the specified item]; otherwise, <c>false</c>.</returns>
        internal bool IsGroupItemExpanded(RadzenDataGridGroupRow<TItem> item)
        {
            return !collapsedGroupItems.Keys.Contains(item) ;
        }

        /// <summary>
        /// Expands the group item.
        /// </summary>
        /// <param name="item">The item.</param>
        internal async System.Threading.Tasks.Task ExpandGroupItem(RadzenDataGridGroupRow<TItem> item)
        {
            if (!collapsedGroupItems.Keys.Contains(item))
            {
                collapsedGroupItems.Add(item, true);
            }
            else
            {
                collapsedGroupItems.Remove(item);
            }

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// The expanded items
        /// </summary>
        internal Dictionary<TItem, bool> expandedItems = new Dictionary<TItem, bool>();
        /// <summary>
        /// Expandeds the item style.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        internal string ExpandedItemStyle(TItem item)
        {
            return expandedItems.Keys.Contains(item) ? "rz-row-toggler rzi-grid-sort  rzi-chevron-circle-down" : "rz-row-toggler rzi-grid-sort  rzi-chevron-circle-right";
        }

        /// <summary>
        /// The selected items
        /// </summary>
        internal Dictionary<TItem, bool> selectedItems = new Dictionary<TItem, bool>();
        /// <summary>
        /// Rows the style.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="index">The index.</param>
        /// <returns>System.String.</returns>
        internal string RowStyle(TItem item, int index)
        {
            var evenOrOdd = index % 2 == 0 ? "rz-datatable-even" : "rz-datatable-odd";

            return (RowSelect.HasDelegate || ValueChanged.HasDelegate || SelectionMode == DataGridSelectionMode.Multiple) && selectedItems.Keys.Contains(item) ? $"rz-state-highlight {evenOrOdd} " : $"{evenOrOdd} ";
        }

        /// <summary>
        /// Rows the attributes.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Tuple&lt;Radzen.RowRenderEventArgs&lt;TItem&gt;, IReadOnlyDictionary&lt;System.String, System.Object&gt;&gt;.</returns>
        internal Tuple<Radzen.RowRenderEventArgs<TItem>, IReadOnlyDictionary<string, object>> RowAttributes(TItem item)
        {
            var args = new Radzen.RowRenderEventArgs<TItem>() { Data = item, Expandable = Template != null };

            if (RowRender != null)
            {
                RowRender(args);
            }

            return new Tuple<Radzen.RowRenderEventArgs<TItem>, IReadOnlyDictionary<string, object>>(args, new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(args.Attributes));
        }

        /// <summary>
        /// The visible changed
        /// </summary>
        private bool visibleChanged = false;
        /// <summary>
        /// The first render
        /// </summary>
        private bool firstRender = true;
        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var emptyTextChanged = parameters.DidParameterChange(nameof(EmptyText), EmptyText);
            if (emptyTextChanged)
            {
                await ChangeState();
            }

            visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            bool valueChanged = parameters.DidParameterChange(nameof(Value), Value);

            await base.SetParametersAsync(parameters);

            if (valueChanged)
            {
                selectedItems.Clear();

                if (Value != null)
                {
                    Value.Where(v => v != null).ToList().ForEach(v => selectedItems.Add(v, true));
                }
            }

            if (visibleChanged && !firstRender)
            {
                if (Visible == false)
                {
                    Dispose();
                }
            }
        }

        /// <summary>
        /// Called when [after render].
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> [first render].</param>
        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                StateHasChanged();
            }
        }

        /// <summary>
        /// On after render as an asynchronous operation.
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> [first render].</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (Visible)
            {
                var args = new Radzen.DataGridRenderEventArgs<TItem>() { Grid = this, FirstRender = firstRender };

                if (Render != null)
                {
                    Render(args);
                }
            }

            this.firstRender = firstRender;

            if (firstRender || visibleChanged)
            {
                visibleChanged = false;
            }
        }

        /// <summary>
        /// Expands the row.
        /// </summary>
        /// <param name="item">The item.</param>
        public async System.Threading.Tasks.Task ExpandRow(TItem item)
        {
            await ExpandItem(item);
        }

        /// <summary>
        /// Expands the item.
        /// </summary>
        /// <param name="item">The item.</param>
        internal async System.Threading.Tasks.Task ExpandItem(TItem item)
        {
            if (ExpandMode == DataGridExpandMode.Single && expandedItems.Keys.Any())
            {
                var itemToCollapse = expandedItems.Keys.FirstOrDefault();
                if (itemToCollapse != null)
                {
                    expandedItems.Remove(itemToCollapse);
                    await RowCollapse.InvokeAsync(itemToCollapse);

                    if (object.Equals(item, itemToCollapse))
                    {
                        return;
                    }

                }
            }

            if (!expandedItems.Keys.Contains(item))
            {
                expandedItems.Add(item, true);
                await RowExpand.InvokeAsync(item);
            }
            else
            {
                expandedItems.Remove(item);
                await RowCollapse.InvokeAsync(item);
            }

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Gets or sets the selection mode.
        /// </summary>
        /// <value>The selection mode.</value>
        [Parameter]
        public DataGridSelectionMode SelectionMode { get; set; } = DataGridSelectionMode.Single;

        /// <summary>
        /// Called when [row click].
        /// </summary>
        /// <param name="args">The arguments.</param>
        internal async Task OnRowClick(DataGridRowMouseEventArgs<TItem> args)
        {
            await RowClick.InvokeAsync(args);
            await OnRowSelect(args.Data);
        }

        /// <summary>
        /// Called when [row select].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="raiseChange">if set to <c>true</c> [raise change].</param>
        internal async System.Threading.Tasks.Task OnRowSelect(object item, bool raiseChange = true)
        {
            if (SelectionMode == DataGridSelectionMode.Single && item != null && selectedItems.Keys.Contains((TItem)item))
            {
                // Legacy RowSelect raise
                if (raiseChange)
                {
                    await RowSelect.InvokeAsync((TItem)item);
                }
                return;
            }

            if (SelectionMode == DataGridSelectionMode.Single && selectedItems.Keys.Any())
            {
                var itemToDeselect = selectedItems.Keys.FirstOrDefault();
                if (itemToDeselect != null)
                {
                    selectedItems.Remove(itemToDeselect);
                    await RowDeselect.InvokeAsync(itemToDeselect);
                }
            }

            if (item != null)
            {
                if (!selectedItems.Keys.Contains((TItem)item))
                {
                    selectedItems.Add((TItem)item, true);
                    if (raiseChange)
                    {
                        await RowSelect.InvokeAsync((TItem)item);
                    }
                }
                else
                {
                    selectedItems.Remove((TItem)item);
                    await RowDeselect.InvokeAsync((TItem)item);
                }
            }
            else
            {
                if (raiseChange)
                {
                    await RowSelect.InvokeAsync((TItem)item);
                }
            }

            var value = selectedItems.Keys;

            _value = SelectionMode == DataGridSelectionMode.Multiple ? new List<TItem>(value) : new List<TItem>() { value.FirstOrDefault() };

            await ValueChanged.InvokeAsync(_value);

            StateHasChanged();
        }

        /// <summary>
        /// Selects the row.
        /// </summary>
        /// <param name="item">The item.</param>
        public async System.Threading.Tasks.Task SelectRow(TItem item)
        {
            await OnRowSelect(item, true);
        }

        /// <summary>
        /// Called when [row double click].
        /// </summary>
        /// <param name="args">The arguments.</param>
        internal async System.Threading.Tasks.Task OnRowDblClick(DataGridRowMouseEventArgs<TItem> args)
        {
            await RowDoubleClick.InvokeAsync(args);
        }

        /// <summary>
        /// Gets or sets the row edit.
        /// </summary>
        /// <value>The row edit.</value>
        [Parameter]
        public EventCallback<TItem> RowEdit { get; set; }

        /// <summary>
        /// Gets or sets the row update.
        /// </summary>
        /// <value>The row update.</value>
        [Parameter]
        public EventCallback<TItem> RowUpdate { get; set; }

        /// <summary>
        /// Gets or sets the row create.
        /// </summary>
        /// <value>The row create.</value>
        [Parameter]
        public EventCallback<TItem> RowCreate { get; set; }

        /// <summary>
        /// The edited items
        /// </summary>
        internal Dictionary<TItem, bool> editedItems = new Dictionary<TItem, bool>();
        /// <summary>
        /// The edit contexts
        /// </summary>
        internal Dictionary<TItem, EditContext> editContexts = new Dictionary<TItem, EditContext>();

        /// <summary>
        /// Edits the row.
        /// </summary>
        /// <param name="item">The item.</param>
        public async System.Threading.Tasks.Task EditRow(TItem item)
        {
            if(itemToInsert != null)
            {
                CancelEditRow(itemToInsert);
            }

            await EditRowInternal(item);
        }

        /// <summary>
        /// Edits the row internal.
        /// </summary>
        /// <param name="item">The item.</param>
        async System.Threading.Tasks.Task EditRowInternal(TItem item)
        {
            if (EditMode == DataGridEditMode.Single && editedItems.Keys.Any())
            {
                var itemToCancel = editedItems.Keys.FirstOrDefault();
                if (itemToCancel != null)
                {
                    editedItems.Remove(itemToCancel);
                    editContexts.Remove(itemToCancel);
                }
            }

            if (!editedItems.Keys.Contains(item))
            {
                editedItems.Add(item, true);

                var editContext = new EditContext(item);
                editContexts.Add(item, editContext);

                await RowEdit.InvokeAsync(item);

                StateHasChanged();
            }
        }

        /// <summary>
        /// Updates the row.
        /// </summary>
        /// <param name="item">The item.</param>
        public async System.Threading.Tasks.Task UpdateRow(TItem item)
        {
            if (editedItems.Keys.Contains(item))
            {
                var editContext = editContexts[item];

                if (editContext.Validate())
                {
                    editedItems.Remove(item);
                    editContexts.Remove(item);

                    if (object.Equals(itemToInsert, item))
                    {
                        await RowCreate.InvokeAsync(item);
                        itemToInsert = default(TItem);
                    }
                    else
                    {
                        await RowUpdate.InvokeAsync(item);
                    }
                }

                StateHasChanged();
            }
        }

        /// <summary>
        /// Cancels the edit row.
        /// </summary>
        /// <param name="item">The item.</param>
        public void CancelEditRow(TItem item)
        {
            if (object.Equals(itemToInsert, item))
            {
                if(!IsVirtualizationAllowed())
                {
                    var list = this.PagedView.ToList();
                    list.Remove(item);
                    this._view = list.AsQueryable();
                    this.Count--;
                    itemToInsert = default(TItem);
                    StateHasChanged();
                }
                else
                {
        #if NET5
                    itemToInsert = default(TItem);
                    if (virtualize != null)
                    {
                        virtualize.RefreshDataAsync();
                    }
        #endif
                }
            }
            else
            {
                int hash = item.GetHashCode();

                if (editedItems.Keys.Contains(item))
                {
                    editedItems.Remove(item);
                    editContexts.Remove(item);

                    StateHasChanged();
                }
            }
        }

        /// <summary>
        /// Determines whether [is row in edit mode] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [is row in edit mode] [the specified item]; otherwise, <c>false</c>.</returns>
        public bool IsRowInEditMode(TItem item)
        {
            return editedItems.Keys.Contains(item);
        }

        /// <summary>
        /// The item to insert
        /// </summary>
        TItem itemToInsert;
        /// <summary>
        /// Inserts the row.
        /// </summary>
        /// <param name="item">The item.</param>
        public async System.Threading.Tasks.Task InsertRow(TItem item)
        {
            itemToInsert = item;
            if(!IsVirtualizationAllowed())
            {
                var list = this.PagedView.ToList();
                list.Insert(0, item);
                this._view = list.AsQueryable();
                this.Count++;
            }
            else
            {
    #if NET5
                if (virtualize != null)
                {
                    await virtualize.RefreshDataAsync();
                }
    #endif
            }
            
           await EditRowInternal(item);
        }

        /// <summary>
        /// The is o data
        /// </summary>
        bool? isOData;
        /// <summary>
        /// Determines whether [is o data].
        /// </summary>
        /// <returns><c>true</c> if [is o data]; otherwise, <c>false</c>.</returns>
        internal bool IsOData()
        {
            if(isOData == null && Data != null)
            {
                isOData = typeof(ODataEnumerable<TItem>).IsAssignableFrom(Data.GetType());
            }

            return isOData != null ? isOData.Value : false;
        }

        /// <summary>
        /// The sorts
        /// </summary>
        internal List<SortDescriptor> sorts = new List<SortDescriptor>();
        /// <summary>
        /// Sets the column sort order.
        /// </summary>
        /// <param name="column">The column.</param>
        internal void SetColumnSortOrder(RadzenDataGridColumn<TItem> column)
        {
            if (!AllowMultiColumnSorting)
            {
                foreach (var c in columns.Where(c => c != column))
                {
                    c.SetSortOrder(null);
                }
                sorts.Clear();
            }

            var descriptor = sorts.Where(d => d.Property == column?.GetSortProperty()).FirstOrDefault();
            if (descriptor == null)
            {
                descriptor = new SortDescriptor() { Property = column.GetSortProperty() };
            }

            if (column.GetSortOrder() == null)
            {
                column.SetSortOrder(SortOrder.Ascending);
                descriptor.SortOrder = SortOrder.Ascending;
            }
            else if (column.GetSortOrder() == SortOrder.Ascending)
            {
                column.SetSortOrder(SortOrder.Descending);
                descriptor.SortOrder = SortOrder.Descending;
            }
            else if (column.GetSortOrder() == SortOrder.Descending)
            {
                column.SetSortOrder(null);
                if (sorts.Where(d => d.Property == column?.GetSortProperty()).Any())
                {
                    sorts.Remove(descriptor);
                }
                descriptor = null;
            }

            if (descriptor != null && !sorts.Where(d => d.Property == column?.GetSortProperty()).Any())
            {
                sorts.Add(descriptor);
            }
        }

        /// <summary>
        /// Gets or sets the groups.
        /// </summary>
        /// <value>The groups.</value>
        public List<GroupDescriptor> Groups 
        { 
            get
            {
                return groups;
            }
            set
            {
                groups = value;
            }
 
         }

        /// <summary>
        /// The groups
        /// </summary>
        internal List<GroupDescriptor> groups = new List<GroupDescriptor>();
        /// <summary>
        /// Ends the column drop to group.
        /// </summary>
        internal void EndColumnDropToGroup()
        {
            if(indexOfColumnToReoder != null)
            {
                var column = columns.Where(c => c.Visible).ElementAtOrDefault(indexOfColumnToReoder.Value);

                if(column != null && column.Groupable && !string.IsNullOrEmpty(column.GetGroupProperty()))
                {
                    var descriptor = groups.Where(d => d.Property == column.GetGroupProperty()).FirstOrDefault();
                    if (descriptor == null)
                    {
                        descriptor = new GroupDescriptor() { Property = column.GetGroupProperty(), Title = column.Title };
                        groups.Add(descriptor);
                        _groupedPagedView = null;
                    }
                }

                indexOfColumnToReoder = null;
            }  
        }

        /// <summary>
        /// Orders the by.
        /// </summary>
        /// <param name="property">The property.</param>
        public void OrderBy(string property)
        {
            var p = IsOData() ? property.Replace('.', '/') : PropertyAccess.GetProperty(property);

            var column = columns.Where(c => c.GetSortProperty() == property).FirstOrDefault();
            if (column != null)
            {
                SetColumnSortOrder(column);
            }

            if (LoadData.HasDelegate && IsVirtualizationAllowed())
            {
                Data = null;
            }

            InvokeAsync(Reload);
        }

        /// <summary>
        /// Orders the by descending.
        /// </summary>
        /// <param name="property">The property.</param>
        public void OrderByDescending(string property)
        {
            var column = columns.Where(c => c.GetSortProperty() == property).FirstOrDefault();
            if (column != null)
            {
                column.SetSortOrder(SortOrder.Descending);
            }
            InvokeAsync(Reload);
        }

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            var additionalClasses = new List<string>();

            if (CurrentStyle.ContainsKey("height"))
            {
                additionalClasses.Add("rz-has-height");
            }

            if (RowSelect.HasDelegate || ValueChanged.HasDelegate || SelectionMode == DataGridSelectionMode.Multiple)
            {
                additionalClasses.Add("rz-selectable");
            }

            return $"rz-has-paginator rz-datatable  rz-datatable-scrollable {String.Join(" ", additionalClasses)}";
        }

        /// <summary>
        /// Gets the header style.
        /// </summary>
        /// <returns>System.String.</returns>
        internal string getHeaderStyle()
        {
            var additionalStyle = Style != null && Style.IndexOf("height:") != -1 ? "padding-right: 17px;" : "";
            return $"margin-left:0px;{additionalStyle}";
        }

        /// <summary>
        /// Gets the query.
        /// </summary>
        /// <value>The query.</value>
        public Query Query { get; private set; } = new Query();

        /// <summary>
        /// Gets the popup identifier.
        /// </summary>
        /// <value>The popup identifier.</value>
        internal string PopupID
        {
            get
            {
                return $"popup{UniqueID}";
            }
        }

        /// <summary>
        /// The disposed
        /// </summary>
        internal bool disposed = false;

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            disposed = true;

            if (IsJSRuntimeAvailable)
            {
                foreach (var column in columns.Where(c => c.Visible))
                {
                    JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", $"{PopupID}{column.GetFilterProperty()}");
                }
            }
        }
    }
}