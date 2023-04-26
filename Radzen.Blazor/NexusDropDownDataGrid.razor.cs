using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
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
    /// NexusDropDownDataGrid component.
    /// </summary>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    /// <example>
    /// <code>
    /// &lt;NexusDropDownDataGrid @bind-Value=@customerID TValue="string" Data=@customers TextProperty="CompanyName" ValueProperty="CustomerID" Change=@(args => Console.WriteLine($"Selected CustomerID: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class NexusDropDownDataGrid<TValue> : DropDownBase<TValue>
    {


        /// <summary>
        /// Gets or sets a value indicating whether the selected items will be displayed as chips. Set to <c>false</c> by default.
        /// Requires <see cref="DropDownBase{T}.Multiple" /> to be set to <c>true</c>. 
        /// </summary>
        /// <value><c>true</c> to display the selected items as chips; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Chips { get; set; }


        /// <summary>
        /// Gets or sets a value indicating whether popup should open on focus. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if popup should open on focus; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool OpenOnFocus { get; set; }

        private async Task OnFocus(Microsoft.AspNetCore.Components.Web.FocusEventArgs args)
        {
            if (OpenOnFocus)
            {
                await OpenPopup("Enter", false);
            }
        }

        /// <summary>
        /// Opens the popup.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="isFilter">if set to <c>true</c> [is filter].</param>
        /// <param name="isFromClick">if set to <c>true</c> [is from click].</param>
        protected override async System.Threading.Tasks.Task OpenPopup(string key = "ArrowDown", bool isFilter = false, bool isFromClick = false)
        {
            if (Disabled)
                return;

            await JSRuntime.InvokeVoidAsync(OpenOnFocus ? "Radzen.openPopup" : "Radzen.togglePopup", Element, PopupID, true);

            if (FocusFilterOnPopup)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.focusElement", isFilter ? UniqueID : SearchID);
            }

            if (list != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.selectListItem", search, list, selectedIndex);
            }
        }

        /// <summary>
        /// Gets or sets the value template.
        /// </summary>
        /// <value>The value template.</value>
        /// 
        [Parameter]
        public bool AllowGrouping { get; set; }
        [Parameter]
        public bool AllowColumnPicking { get; set; }
        [Parameter]
        public RenderFragment HeaderTemplate { get; set; }
        [Parameter]
        public RenderFragment<dynamic> ValueTemplate { get; set; }
        [Parameter]
        public string AllColumnsText { get; set; } = "All";
        [Parameter]
        public bool AllowPickAllColumns { get; set; } = true;
        [Parameter]
        public string ColumnsShowingText { get; set; } = "columns showing";

        [Parameter]
        public string GroupPanelText { get; set; } = "Drag a column header here and drop it to group by that column";

        /// <summary>
        /// Gets or sets a value indicating DataGrid density.
        /// </summary>
        [Parameter]
        public Density Density { get; set; }

        /// <summary>
        /// Gets or sets the empty template shown when Data is empty collection.
        /// </summary>
        /// <value>The empty template.</value>
        [Parameter]
        public RenderFragment EmptyTemplate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether pager is visible even when not enough data for paging.
        /// </summary>
        /// <value><c>true</c> if pager is visible even when not enough data for paging otherwise, <c>false</c>.</value>
        [Parameter]
        public bool PagerAlwaysVisible { get; set; }

        /// <summary>
        /// Gets or sets the horizontal align.
        /// </summary>
        /// <value>The horizontal align.</value>
        [Parameter]
        public HorizontalAlign PagerHorizontalAlign { get; set; } = HorizontalAlign.Justify;

        /// <summary>
        /// Gets or sets a value indicating whether column resizing is allowed.
        /// </summary>
        /// <value><c>true</c> if column resizing is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowColumnResize { get; set; }

        /// <summary>
        /// Gets or sets the width of all columns.
        /// </summary>
        /// <value>The width of all columns.</value>
        [Parameter]
        public string ColumnWidth { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="NexusDropDownDataGrid{TValue}"/> is responsive.
        /// </summary>
        /// <value><c>true</c> if responsive; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Responsive { get; set; } = false;



        /// <summary>
        /// Gets or sets the empty text.
        /// </summary>
        /// <value>The empty text.</value>
        [Parameter]
        public string EmptyText { get; set; } = "No records to display.";

        /// <summary>
        /// Gets or sets the search input placeholder text.
        /// </summary>
        /// <value>The search input placeholder text.</value>
        [Parameter]
        public string SearchText { get; set; } = "Search...";

        /// <summary>
        /// Gets or sets the selected value.
        /// </summary>
        /// <value>The selected value.</value>
        [Parameter]
        public object SelectedValue { get; set; }

        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        [Parameter]
        public RenderFragment Columns { get; set; }

        NexusDataGrid<TValue> grid;

        /// <summary>
        /// Gets or sets the number of maximum selected labels.
        /// </summary>
        /// <value>The maximum selected labels.</value>
        [Parameter]
        public int MaxSelectedLabels { get; set; } = 4;



        /// <summary>
        /// Gets or sets the selected items text.
        /// </summary>
        /// <value>The selected items text.</value>
        [Parameter]
        public string SelectedItemsText { get; set; } = "items selected";

        /// <summary>
        /// Gets or sets whether popup automatically focuses on filter input.
        /// </summary>
        /// <value><c>true</c> if filter input should auto focus when opened; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool FocusFilterOnPopup { get; set; } = true;

        /// <summary>
        /// Gets popup element reference.
        /// </summary>
        protected ElementReference popup;

        int? indexOfColumnToReoder;
        internal string getColumnResizerId(int columnIndex)
        {
            return string.Join("", $"{UniqueID}".Split('.')) + columnIndex;
        }


        IEnumerable<GroupResult> _groupedPagedView;


        /// <summary>
        /// Selects the item from value.
        /// </summary>
        /// <param name="value">The value.</param>
        protected override void SelectItemFromValue(object value)
        {
            if (value != null && Query != null)
            {
                if (!Multiple)
                {
                    if (!string.IsNullOrEmpty(ValueProperty))
                    {
                        var item = Query.Where($@"{ValueProperty} == @0", value).FirstOrDefault();
                        if (item != null)
                        {
                            SelectedItem = item;
                        }
                    }
                    else
                    {
                        SelectedItem = internalValue;
                    }
                    SelectedItemChanged?.Invoke(SelectedItem);
                    selectedItems.Clear();
                    selectedItems.Add(SelectedItem);
                }
                else
                {
                    var values = value as dynamic;
                    if (values != null)
                    {
                        if (!string.IsNullOrEmpty(ValueProperty))
                        {
                            foreach (object v in values)
                            {
                                var item = Query.Where($@"{ValueProperty} == @0", v).FirstOrDefault();
                                if (item != null && selectedItems.IndexOf(item) == -1)
                                {
                                    selectedItems.Add(item);
                                }
                            }
                        }
                        else
                        {
                            foreach (object v in values)
                            {
                                if (selectedItems.IndexOf(v) == -1)
                                {
                                    selectedItems.Add(v);
                                }
                            }
                        }

                    }
                }
            }
            else
            {
                selectedItem = null;
                selectedItems.Clear();
            }
        }

        async System.Threading.Tasks.Task Clear()
        {
            if (Disabled)
                return;

            internalValue = default(TValue);
            selectedItem = null;

            selectedItems.Clear();

            await ValueChanged.InvokeAsync((TValue)internalValue);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(internalValue);

            await grid.Reload();

            StateHasChanged();
        }
        

       

        ObservableCollection<GroupDescriptor> groups;


       




        //        async Task DebounceFilter()
        //        {
        //            searchText = await JSRuntime.InvokeAsync<string>("Radzen.getInputValue", search);
        //            if (searchText != previousSearch)
        //            {
        //                previousSearch = searchText;
        //                _view = null;

        //                await InvokeAsync(RefreshAfterFilter);
        //            }
        //        }

        //        async Task RefreshAfterFilter()
        //        {
        //#if NET5_0_OR_GREATER
        //            if (grid?.virtualize != null)
        //            {
        //                if(string.IsNullOrEmpty(searchText))
        //                {
        //                    if(LoadData.HasDelegate)
        //                    {
        //                        Data = null;
        //                        await grid.Reload();
        //                    }
        //                    else
        //                    {
        //                        pagedData = null;
        //                        StateHasChanged();
        //                    }
        //                }
        //                await grid.virtualize.RefreshDataAsync();
        //            }
        //#endif
        //            StateHasChanged();

        //            if (!IsVirtualizationAllowed())
        //            {
        //                await grid.FirstPage(true);
        //            }

        //            await JSRuntime.InvokeAsync<string>("Radzen.repositionPopup", Element, PopupID);
        //        }

        //        /// <summary>
        //        /// Handles the <see cref="E:Filter" /> event.
        //        /// </summary>
        //        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        //        protected override async Task OnFilter(ChangeEventArgs args)
        //        {
        //            await DebounceFilter();
        //        }

        /// <summary>
        /// Gets or sets a value indicating whether sorting is allowed.
        /// </summary>
        /// <value><c>true</c> if sorting is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowSorting { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether filtering is allowed.
        /// </summary>
        /// <value><c>true</c> if filtering is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public override bool AllowFiltering { get; set; } = true;

        [Parameter]
        public bool DataGridAllowFiltering { get; set; } = false;
        [Parameter]
        public FilterMode DataGridFilterMode { get; set; } = FilterMode.Advanced;

        [Parameter]
        public bool AllowPaging { get; set; } = false;



        /// <summary>
        /// Gets or sets a value indicating whether DataGrid row can be selected on row click.
        /// </summary>
        /// <value><c>true</c> if DataGrid row can be selected on row click; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowRowSelectOnRowClick { get; set; } = true;

        async Task OnRowSelect(TValue item)
        {
            if (!Disabled && !Multiple)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
            }

            if (AllowRowSelectOnRowClick)
            {
                await SelectItem(item);
            }

        }

        private async Task OnChipRemove(object item)
        {
            if (!Disabled)
            {
                await SelectItem(item);
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-dropdown").Add("rz-dropdown-chips", Chips && selectedItems.Count > 0).Add("rz-clear", AllowClear).ToString();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (IsJSRuntimeAvailable)
            {
                JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", PopupID);
            }
        }
    }
}
