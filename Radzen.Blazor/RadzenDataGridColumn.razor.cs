﻿using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDataGridColumn component.
    /// Must be placed inside a <see cref="RadzenDataGrid{TItem}" />
    /// </summary>
    /// <typeparam name="TItem">The type of the DataGrid item.</typeparam>
    public partial class RadzenDataGridColumn<TItem> : ComponentBase, IDisposable
    {
        /// <summary>
        /// Gets or sets the grid.
        /// </summary>
        /// <value>The grid.</value>
        [CascadingParameter]
        public RadzenDataGrid<TItem> Grid { get; set; }

        /// <summary>
        /// Gets or sets the columns.
        /// </summary>
        /// <value>The columns.</value>
        [Parameter]
        public RenderFragment Columns { get; set; }

        /// <summary>
        /// Gets or sets the parent column.
        /// </summary>
        /// <value>The parent column.</value>
        [CascadingParameter]
        public RadzenDataGridColumn<TItem> Parent { get; set; }

        /// <summary>
        /// Specifies wether CheckBoxList filter list virtualization is enabled. Set to <c>true</c> by default.
        /// </summary>
        [Parameter]
        public bool AllowCheckBoxListVirtualization { get; set; } = true;

        /// <summary>
        /// Gets or sets the column filter mode.
        /// </summary>
        /// <value>The column filter mode.</value>
        [Parameter]
        public FilterMode? FilterMode { get; set; }

        internal void RemoveColumn(RadzenDataGridColumn<TItem> column)
        {
            if (Grid.childColumns.Contains(column))
            {
                Grid.childColumns.Remove(column);
                if (!Grid.disposed)
                {
                    try { InvokeAsync(StateHasChanged); } catch { }
                }
            }
        }

        /// <summary>
        /// Gets the child columns.
        /// </summary>
        /// <value>The child columns.</value>
        public IList<RadzenDataGridColumn<TItem>> ColumnsCollection
        {
            get
            {
                return Grid.childColumns.Where(c => c.Parent == this).ToList();
            }
        }

        internal IEnumerable<RadzenDataGridColumn<TItem>> VisibleColumns => ColumnsCollection.Where(c => c.GetVisible());

        internal int GetLevel()
        {
            int i = 0;
            var p = Parent;
            while (p != null)
            {
                p = p.Parent;
                i++;
            }

            return i;
        }

        internal int GetColSpan(bool isDataCell = false)
        {
            if (!Grid.AllowCompositeDataCells && isDataCell || Columns == null)
                return 1;

            var span = VisibleColumns.Concat(VisibleColumns.SelectManyRecursive(c => c.VisibleColumns)).Sum(c => c.VisibleColumns.Count())
                - VisibleColumns.SelectManyRecursive(c => c.VisibleColumns).Count(c => c.VisibleColumns.Any())
                + VisibleColumns.Where(c => c.VisibleColumns.Count() == 0).Count();

            return span != 0 ? span : VisibleColumns.Count();
        }

        internal int GetRowSpan(bool isDataCell = false)
        {
            if (!Grid.AllowCompositeDataCells && isDataCell)
                return 1;

            if (Columns == null && Parent != null)
            {
                var level = this.GetLevel();
                return level == Grid.deepestChildColumnLevel ? 1 : level < Grid.deepestChildColumnLevel ? Grid.deepestChildColumnLevel : level + 1;
            }

            return Columns == null && Parent == null ? Grid.deepestChildColumnLevel + 1 : 1;
        }

        Type _propertyType;
        internal Type PropertyType => _propertyType;

        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        /// <value>The unique identifier.</value>
        [Parameter]
        public string UniqueID { get; set; }

        /// <summary>
        /// Called when initialized.
        /// </summary>
        protected override void OnInitialized()
        {
            if (Grid != null)
            {
                Grid.AddColumn(this);

                var canSetFilterPropertyType = (FilterMode ?? Grid.FilterMode) == Radzen.FilterMode.CheckBoxList && FilterTemplate == null;

                if (canSetFilterPropertyType)
                {
                    if (Type == null)
                    {
                        var fp = GetFilterProperty();
                        var pt = !string.IsNullOrEmpty(fp) ?
                                PropertyAccess.GetPropertyType(typeof(TItem), fp) : typeof(object);

                        _filterPropertyType = typeof(IEnumerable<>).MakeGenericType(pt);
                    }

                    if (GetFilterOperator() == FilterOperator.Equals)
                    {
                        SetFilterOperator(FilterOperator.Contains);
                    }

                    Grid.FilterPopupRenderMode = PopupRenderMode.OnDemand;
                }

                var property = GetFilterProperty();

                if (!string.IsNullOrEmpty(property))
                {
                    _propertyType = PropertyAccess.GetPropertyType(typeof(TItem), property);
                }

                if (!string.IsNullOrEmpty(property) && Type == null && !canSetFilterPropertyType)
                {
                    _filterPropertyType = _propertyType;
                }

                if (_filterPropertyType == null)
                {
                    _filterPropertyType = Type;
                }
                else if(!string.IsNullOrEmpty(Property))
                {
                    propertyValueGetter = PropertyAccess.Getter<TItem, object>(Property);
                }

                if (_filterPropertyType == typeof(string) && filterOperator != FilterOperator.Custom && filterOperator == null && _filterOperator == null)
                {
                    SetFilterOperator(FilterOperator.Contains);
                }
            }
        }
        
        int? orderIndex;

        /// <summary>
        /// Gets or sets the order index.
        /// </summary>
        /// <value>The order index.</value>
        [Parameter]
        public int? OrderIndex { get; set; }

        /// <summary>
        /// Gets the order index.
        /// </summary>
        public int? GetOrderIndex()
        {
            return orderIndex ?? OrderIndex;
        }

        internal void SetOrderIndex(int? value)
        {
            orderIndex = value;
        }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        /// <value>The sort order.</value>
        [Parameter]
        public SortOrder? SortOrder { get; set; }

        bool visible = true;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDataGridColumn{TItem}"/> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Visible
        {
            get
            {
                return visible;
            }
            set
            {
                if (visible != value)
                {
                    visible = value;
                    _visible = visible;
                    if (Grid != null)
                    {
                        Grid.UpdatePickableColumn(this, visible);
                        InvokeAsync(Grid.ChangeState);
                    }
                }
            }
        }

        bool? _visible;

        /// <summary>
        /// Gets if the column is visible or not.
        /// </summary>
        /// <returns>System.Boolean.</returns>
        public bool GetVisible()
        {
            if (Columns != null && ColumnsCollection.Any() && VisibleColumns.Count() == 0)
            {
                return false;
            }

            return _visible ?? Visible;
        }

        internal void SetVisible(bool? value)
        {
            _visible = value;

            if (Grid != null && Pickable)
            {
                Grid.UpdatePickableColumn(this, _visible == true);
            }
        }

        /// <summary>
        /// Gets or sets the header tooltip.
        /// </summary>
        /// <value>The header tooltip.</value>
        [Parameter]
        public string HeaderTooltip { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [Parameter]
        public string Title { get; set; }

        string _title;

        /// <summary>
        /// Gets the column title.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetTitle()
        {
            return _title ?? Title;
        }

        /// <summary>
        /// Sets the column title.
        /// </summary>
        public void SetTitle(string value)
        {
            _title = value;
        }

        /// <summary>
        /// Gets or sets the title in column picker.
        /// Value of Title is used when ColumnPickerTitle is not set
        /// </summary>
        /// <value>The column picker title.</value>
        [Parameter]
        public string ColumnPickerTitle
        {
            get => _columnPickerTitle ?? Title ?? string.Empty;
            set => _columnPickerTitle = value;
        }

        string _columnPickerTitle;

        /// <summary>
        /// Gets or sets the property name.
        /// </summary>
        /// <value>The property name.</value>
        [Parameter]
        public string Property { get; set; }

        /// <summary>
        /// Gets or sets the sort property name.
        /// </summary>
        /// <value>The sort property name.</value>
        [Parameter]
        public string SortProperty { get; set; }

        /// <summary>
        /// Gets or sets the group property name.
        /// </summary>
        /// <value>The group property name.</value>
        [Parameter]
        public string GroupProperty { get; set; }

        /// <summary>
        /// Gets or sets the filter property name.
        /// </summary>
        /// <value>The filter property name.</value>
        [Parameter]
        public string FilterProperty { get; set; }

        /// <summary>
        /// Gets or sets the filter value.
        /// </summary>
        /// <value>The filter value.</value>
        [Parameter]
        public object FilterValue { get; set; }

        /// <summary>
        /// Gets or sets the filter placeholder.
        /// </summary>
        /// <value>The filter placeholder value.</value>
        [Parameter]
        public string FilterPlaceholder { get; set; }

        /// <summary>
        /// Gets or sets the custom filter dynamic Linq dictionary.
        /// </summary>
        /// <value>The custom filter dynamic Linq dictionary.</value>
        [Parameter]
        public string CustomFilterExpression { get; set; }

        /// <summary>
        /// Gets the filter placeholder.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetFilterPlaceholder()
        {
            return FilterPlaceholder ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the second filter value.
        /// </summary>
        /// <value>The second filter value.</value>
        [Parameter]
        public object SecondFilterValue { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        [Parameter]
        public string Width { get; set; }

        /// <summary>
        /// Gets or sets the min-width.
        /// </summary>
        /// <value>The min-width.</value>
        [Parameter]
        public string MinWidth { get; set; }


        /// <summary>
        /// Gets or sets the max-width.
        /// </summary>
        /// <value>The max-width.</value>
        [Parameter]
        public string MaxWidth { get; set; }

        /// <summary>
        /// Gets or sets the format string.
        /// </summary>
        /// <value>The format string.</value>
        [Parameter]
        public string FormatString { get; set; }

        /// <summary>
        /// Gets or sets the CSS class applied to data cells.
        /// </summary>
        /// <value>The CSS class applied to data cells.</value>
        [Parameter]
        public string CssClass { get; set; }

        /// <summary>
        /// Gets or sets a function that calculates the CSS class based on the <typeparamref name="TItem"/> value.
        /// </summary>
        /// <value>The dynamic CSS class applied to data cells.</value>
        [Parameter]
        public Func<RadzenDataGridColumn<TItem>, TItem, string> CalculatedCssClass { get; set; }

        /// <summary>
        /// Gets or sets the header CSS class applied to header cell.
        /// </summary>
        /// <value>The header CSS class applied to header cell.</value>
        [Parameter]
        public string HeaderCssClass { get; set; }

        /// <summary>
        /// Gets or sets the footer CSS class applied to footer cell.
        /// </summary>
        /// <value>The footer CSS class applied to footer cell.</value>
        [Parameter]
        public string FooterCssClass { get; set; }

        /// <summary>
        /// Gets or sets the group footer CSS class applied to group footer cell.
        /// </summary>
        /// <value>The group footer CSS class applied to group footer cell.</value>
        [Parameter]
        public string GroupFooterCssClass { get; set; }

        /// <summary>
        /// Gets or sets the header white space style.
        /// </summary>
        [Parameter]
        public WhiteSpace HeaderWhiteSpace { get; set; } = WhiteSpace.Truncate;

        /// <summary>
        /// Gets or sets the white space style.
        /// </summary>
        [Parameter]
        public WhiteSpace WhiteSpace { get; set; } = WhiteSpace.Truncate;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDataGridColumn{TItem}"/> is filterable.
        /// </summary>
        /// <value><c>true</c> if filterable; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Filterable { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDataGridColumn{TItem}"/> is sortable.
        /// </summary>
        /// <value><c>true</c> if sortable; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Sortable { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDataGridColumn{TItem}"/> is frozen.
        /// </summary>
        /// <value><c>true</c> if frozen will disable horizontal scroll for the column; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Frozen { get; set; }

        /// <summary>
        /// Gets or sets the frozen position this <see cref="RadzenDataGridColumn{TItem}"/>
        /// </summary>
        /// <value><see cref="FrozenColumnPosition.Left"/> or <see cref="FrozenColumnPosition.Right"/>.</value>
        [Parameter]
        public FrozenColumnPosition FrozenPosition { get; set; } = FrozenColumnPosition.Left;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDataGridColumn{TItem}"/> is resizable.
        /// </summary>
        /// <value><c>true</c> if resizable; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Resizable { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDataGridColumn{TItem}"/> is reorderable.
        /// </summary>
        /// <value><c>true</c> if reorderable; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Reorderable { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDataGridColumn{TItem}"/> is groupable.
        /// </summary>
        /// <value><c>true</c> if groupable; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Groupable { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDataGridColumn{TItem}"/> is pickable - listed when DataGrid AllowColumnPicking is set to true.
        /// </summary>
        /// <value><c>true</c> if pickable; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Pickable { get; set; } = true;

        /// <summary>
        /// Gets or sets the text align.
        /// </summary>
        /// <value>The text align.</value>
        [Parameter]
        public TextAlign TextAlign { get; set; } = TextAlign.Left;

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment<TItem> Template { get; set; }

        /// <summary>
        /// Gets or sets the edit template.
        /// </summary>
        /// <value>The edit template.</value>
        [Parameter]
        public RenderFragment<TItem> EditTemplate { get; set; }

        /// <summary>
        /// Allows the column to override whether or not this column's the <see cref="EditTemplate" /> is visible at runtime.
        /// </summary>
        [Parameter]
        public Func<string, TItem, bool> IsInEditMode { get; set; } = DefaultIsInEditMode;

        internal static Func<string, TItem, bool> DefaultIsInEditMode { get; set; } = (property, item) => false;

        /// <summary>
        /// Gets or sets the header template.
        /// </summary>
        /// <value>The header template.</value>
        [Parameter]
        public RenderFragment HeaderTemplate { get; set; }

        /// <summary>
        /// Gets or sets the footer template.
        /// </summary>
        /// <value>The footer template.</value>
        [Parameter]
        public RenderFragment FooterTemplate { get; set; }

        /// <summary>
        /// Gets or sets the group footer template.
        /// </summary>
        /// <value>The group footer template.</value>
        [Parameter]
        public RenderFragment<Group> GroupFooterTemplate { get; set; }

        /// <summary>
        /// Gets or sets the filter template.
        /// </summary>
        /// <value>The filter template.</value>
        [Parameter]
        public RenderFragment<RadzenDataGridColumn<TItem>> FilterTemplate { get; set; }

        /// <summary>
        /// Gets or sets the filter value template.
        /// </summary>
        /// <value>The filter value template.</value>
        [Parameter]
        public RenderFragment<RadzenDataGridColumn<TItem>> FilterValueTemplate { get; set; }

        /// <summary>
        /// Gets or sets the second filter value template.
        /// </summary>
        /// <value>The second filter value template.</value>
        [Parameter]
        public RenderFragment<RadzenDataGridColumn<TItem>> SecondFilterValueTemplate { get; set; }

        /// <summary>
        /// Gets or sets the logical filter operator.
        /// </summary>
        /// <value>The logical filter operator.</value>
        [Parameter]
        public LogicalFilterOperator LogicalFilterOperator { get; set; } = LogicalFilterOperator.And;

        /// <summary>
        /// Gets or sets the data type.
        /// </summary>
        /// <value>The data type.</value>
        [Parameter]
        public Type Type { get; set; }

        /// <summary>
        /// Gets or sets the IFormatProvider used for FormatString.
        /// </summary>
        /// <value>The IFormatProvider.</value>
        [Parameter]
        public IFormatProvider FormatProvider { get; set; }

        Func<TItem, object> propertyValueGetter;

        /// <summary>
        /// Gets the value for specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.Object.</returns>
        public virtual object GetValue(TItem item)
        {
            var value = propertyValueGetter != null && !string.IsNullOrEmpty(Property) && !Property.Contains('.') ? propertyValueGetter(item) : !string.IsNullOrEmpty(Property) ? PropertyAccess.GetValue(item, Property) : "";


            if ((PropertyAccess.IsEnum(FilterPropertyType) || PropertyAccess.IsNullableEnum(FilterPropertyType) ||
                ((FilterMode ?? Grid.FilterMode) == Radzen.FilterMode.CheckBoxList && (value as Enum) != null)) && value != null)
            {
                var enumValue = value as Enum;
                if (enumValue != null)
                {
                    value = EnumExtensions.GetDisplayDescription(enumValue, Grid.EnumFilterTranslationFunc);
                }
            }

            return !string.IsNullOrEmpty(FormatString) ? string.Format(FormatProvider ?? Grid?.Culture ?? CultureInfo.CurrentCulture, FormatString, value) : Convert.ToString(value, FormatProvider ?? Grid?.Culture ?? CultureInfo.CurrentCulture);
        }

        internal object GetHeader()
        {
            if (HeaderTemplate != null)
            {
                return HeaderTemplate;
            }
            else if (!string.IsNullOrEmpty(GetTitle()))
            {
                return GetTitle();
            }
            else
            {
                return Property;
            }
        }

        /// <summary>
        /// Gets the cell style.
        /// </summary>
        /// <param name="forCell">if set to <c>true</c> [for cell].</param>
        /// <param name="isHeaderOrFooterCell">if set to <c>true</c> [is header or footer cell].</param>
        /// <param name="isForCol">if set to <c>true</c> [is for col element].</param>
        /// <returns>System.String.</returns>
        public virtual string GetStyle(bool forCell = false, bool isHeaderOrFooterCell = false, bool isForCol = false)
        {
            var style = new List<string>();

            var width = GetWidthOrGridSetting()?.Trim();

            if (!string.IsNullOrEmpty(width))
            {
                style.Add($"width:{width}");
            }

            if (forCell && TextAlign != TextAlign.Left)
            {
                style.Add($"text-align:{Enum.GetName(typeof(TextAlign), TextAlign).ToLower()};");
            }

            if (forCell && IsFrozen())
            {
                style.Add(GetStackedStyleForFrozen());
            }

            if (!isHeaderOrFooterCell && IsFrozen() || (isHeaderOrFooterCell && Grid.ColumnsCollection.Where(c => c.GetVisible() && c.IsFrozen()).Any()))
            {
                style.Add($"z-index:{(isHeaderOrFooterCell && IsFrozen() ? 2 : 1)}");
            }

            if (!string.IsNullOrEmpty(MinWidth))
            {
                style.Add($"min-width:{MinWidth}");
            }

            if (!string.IsNullOrEmpty(MaxWidth))
            {
                style.Add($"max-width:{MaxWidth}");
            }

            return string.Join(";", style);
        }

        private string GetStackedStyleForFrozen()
        {
            var visibleFrozenColumns = Grid.ColumnsCollection.Where(c => c.GetVisible() && c.IsFrozen() && c.FrozenPosition == FrozenPosition).ToList();
            if (FrozenPosition == FrozenColumnPosition.Left)
            {
                var stackColumns = visibleFrozenColumns.Where((c, i) => visibleFrozenColumns.IndexOf(this) > i);

                return GetStackedStyleForFrozen(stackColumns, "inset-inline-start");
            }
            else
            {
                var stackColumns = visibleFrozenColumns.Where((c, i) => visibleFrozenColumns.IndexOf(this) < i);

                return GetStackedStyleForFrozen(stackColumns, "inset-inline-end");
            }
        }

        private static string GetStackedStyleForFrozen(IEnumerable<RadzenDataGridColumn<TItem>> stackColumns, string position)
        {
            if (!stackColumns.Any())
            {
                return $"{position}:0";
            }

            var widths = new List<string>();
            foreach (var column in stackColumns)
            {
                var w = column.GetWidthOrGridSetting()?.Trim();

                if (string.IsNullOrEmpty(w))
                {
                    widths.Add("200px");
                    continue;
                }

                if (w.StartsWith("calc(") && w.EndsWith(")"))
                {
                    var calcExpression = w.Remove(w.Length - 1).Substring("calc(".Length);
                    widths.Add(calcExpression);
                    continue;
                }

                widths.Add(w);
            }

            if (widths.Count == 1)
            {
                return $"{position}:{widths.First()}";
            }

            return $"{position}:calc({string.Join(" + ", widths)})";
        }

        internal bool IsFrozen()
        {
            return Frozen && Parent == null && Columns == null;
        }

        /// <summary>
        /// Gets the sort property.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetSortProperty()
        {
            if (!string.IsNullOrEmpty(SortProperty))
            {
                return SortProperty;
            }
            else
            {
                return Property;
            }
        }

        internal void SetSortOrder(SortOrder? order)
        {
            var descriptor = Grid.sorts.Where(d => d.Property == GetSortProperty()).FirstOrDefault();
            if (descriptor == null)
            {
                descriptor = new SortDescriptor() { Property = GetSortProperty() };
            }

            if (order.HasValue)
            {
                SetSortOrderInternal(order.Value);
                descriptor.SortOrder = order.Value;
            }
            else
            {
                SetSortOrderInternal(null);
                if (Grid.sorts.Where(d => d.Property == GetSortProperty()).Any())
                {
                    Grid.sorts.Remove(descriptor);
                }
                descriptor = null;
            }

            if (descriptor != null && !Grid.sorts.Where(d => d.Property == GetSortProperty()).Any())
            {
                Grid.sorts.Add(descriptor);
            }

            sortOrder = new SortOrder?[] { order };
        }

        internal void SetSortOrderInternal(SortOrder? order)
        {
            sortOrder = new SortOrder?[] { order };
        }
        internal void ResetSortOrder()
        {
            sortOrder = Enumerable.Empty<SortOrder?>();
            SortOrder = null;
        }

        /// <summary>
        /// Gets the group property.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetGroupProperty()
        {
            if (!string.IsNullOrEmpty(GroupProperty))
            {
                return GroupProperty;
            }
            else
            {
                return Property;
            }
        }

        /// <summary>
        /// Gets the filter property.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetFilterProperty()
        {
            if (!string.IsNullOrEmpty(FilterProperty))
            {
                return FilterProperty;
            }
            else
            {
                return Property;
            }
        }

        Type _filterPropertyType;

        /// <summary>
        /// Gets the filter property type.
        /// </summary>
        public Type FilterPropertyType
        {
            get
            {
                return _filterPropertyType;
            }
        }

        IEnumerable<SortOrder?> sortOrder = Enumerable.Empty<SortOrder?>();
        object filterValue;
        FilterOperator? filterOperator;
        object secondFilterValue;
        FilterOperator? secondFilterOperator;
        LogicalFilterOperator? logicalFilterOperator;
        string customFilterExpression;

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Visible), Visible) ||
                parameters.DidParameterChange(nameof(Title), Title))
            {
                if (Grid != null)
                {
                    Grid.UpdatePickableColumn(this, parameters.GetValueOrDefault<bool>(nameof(Visible)));
                    await Grid.ChangeState();
                }
            }

            if (parameters.DidParameterChange(nameof(OrderIndex), OrderIndex))
            {
                var newOrderIndex = parameters.GetValueOrDefault<int?>(nameof(OrderIndex));
                if (newOrderIndex != orderIndex)
                {
                    SetOrderIndex(newOrderIndex);

                    if (Grid != null)
                    {
                        Grid.UpdateColumnsOrder();
                        await Grid.ChangeState();
                    }
                }
            }

            if (parameters.DidParameterChange(nameof(Pickable), Pickable))
            {
                var newPickable = parameters.GetValueOrDefault<bool>(nameof(Pickable));

                Pickable = newPickable;

                if (Grid != null)
                {
                    Grid.UpdatePickableColumns();
                    await Grid.ChangeState();
                }
            }

            if (parameters.DidParameterChange(nameof(SortOrder), SortOrder))
            {
                sortOrder = new SortOrder?[] { parameters.GetValueOrDefault<SortOrder?>(nameof(SortOrder)) };

                if (Grid != null)
                {
                    var descriptor = Grid.sorts.Where(d => d.Property == GetSortProperty()).FirstOrDefault();
                    if (descriptor == null)
                    {
                        Grid.sorts.Add(new SortDescriptor() { Property = GetSortProperty(), SortOrder = sortOrder.FirstOrDefault() });
                        Grid._view = null;
                    }
                }
            }

            if (parameters.DidParameterChange(nameof(FilterValue), FilterValue))
            {
                filterValue = parameters.GetValueOrDefault<object>(nameof(FilterValue));

                if (FilterTemplate != null || FilterValueTemplate != null)
                {
                    FilterValue = filterValue;
                    Grid.SaveSettings();
                    if (Grid.IsVirtualizationAllowed())
                    {
                        if (Grid.virtualize != null)
                        {
                            await Grid.virtualize.RefreshDataAsync();
                        }
                    }
                    else
                    {
                        await Grid.Reload();
                    }

                    return;
                }
            }

            if (parameters.DidParameterChange(nameof(SecondFilterValue), SecondFilterValue))
            {
                secondFilterValue = parameters.GetValueOrDefault<object>(nameof(SecondFilterValue));

                if (FilterTemplate != null || SecondFilterValueTemplate != null)
                {
                    SecondFilterValue = secondFilterValue;
                    Grid.SaveSettings();
                    if (Grid.IsVirtualizationAllowed())
                    {
                        if (Grid.virtualize != null)
                        {
                            await Grid.virtualize.RefreshDataAsync();
                        }
                    }
                    else
                    {
                        await Grid.Reload();
                    }

                    return;
                }
            }

            if (parameters.DidParameterChange(nameof(CustomFilterExpression), CustomFilterExpression))
            {
                customFilterExpression = parameters.GetValueOrDefault<string>(nameof(CustomFilterExpression));

                if (CustomFilterExpression != null)
                {
                    CustomFilterExpression = customFilterExpression;
                    Grid.SaveSettings();
                    if (Grid.IsVirtualizationAllowed())
                    {
                        if (Grid.virtualize != null)
                        {
                            await Grid.virtualize.RefreshDataAsync();
                        }
                    }
                    else
                    {
                        await Grid.Reload();
                    }

                    return;
                }
            }

            if (filterOperator == null && (parameters.DidParameterChange(nameof(FilterOperator), FilterOperator) || _filterOperator != null))
            {
                filterOperator = _filterOperator ?? parameters.GetValueOrDefault<FilterOperator>(nameof(FilterOperator));
            }

            if (parameters.DidParameterChange(nameof(SecondFilterValue), SecondFilterValue))
            {
                secondFilterValue = parameters.GetValueOrDefault<object>(nameof(SecondFilterValue));
            }

            if (parameters.DidParameterChange(nameof(SecondFilterOperator), SecondFilterOperator))
            {
                secondFilterOperator = parameters.GetValueOrDefault<FilterOperator>(nameof(SecondFilterOperator));
            }

            if (parameters.DidParameterChange(nameof(LogicalFilterOperator), LogicalFilterOperator))
            {
                logicalFilterOperator = parameters.GetValueOrDefault<LogicalFilterOperator>(nameof(LogicalFilterOperator));
            }

            await base.SetParametersAsync(parameters);
        }

        /// <summary>
        /// Get column sort order.
        /// </summary>
        public SortOrder? GetSortOrder()
        {
            return sortOrder.Any() ? sortOrder.FirstOrDefault() : SortOrder;
        }

        /// <summary>
        /// Get column filter value.
        /// </summary>
        public object GetFilterValue()
        {
            return filterValue ?? FilterValue;
        }

        internal void ClearFilterValues()
        {
            if (headerCell != null)
            {
                headerCell.filterValues = null;
            }
        }

        /// <summary>
        /// Get column filter operator.
        /// </summary>
        public FilterOperator GetFilterOperator()
        {
            return filterOperator ?? FilterOperator;
        }

        /// <summary>
        /// Get column second filter value.
        /// </summary>
        public object GetSecondFilterValue()
        {
            return secondFilterValue ?? SecondFilterValue;
        }

        /// <summary>
        /// Get column second filter operator.
        /// </summary>
        public FilterOperator GetSecondFilterOperator()
        {
            return secondFilterOperator ?? SecondFilterOperator;
        }

        /// <summary>
        /// Get column logical filter operator.
        /// </summary>
        public LogicalFilterOperator GetLogicalFilterOperator()
        {
            return logicalFilterOperator ?? LogicalFilterOperator;
        }

        /// <summary>
        /// Get body column class.
        /// </summary>
        /// <returns></returns>
        internal string GetCellClass()
        {
            return $"rz-cell-data rz-text-{Enum.GetName(typeof(WhiteSpace), WhiteSpace).ToLower()}";
        }

        /// <summary>
        /// Get column header class.
        /// </summary>
        /// <returns></returns>
        internal string GetHeaderClass()
        {
            return $"rz-column-title-content rz-text-{Enum.GetName(typeof(WhiteSpace), HeaderWhiteSpace).ToLower()}";
        }

        /// <summary>
        /// Set column filter value.
        /// </summary>
        public void SetFilterValue(object value, bool isFirst = true)
        {
            if ((FilterPropertyType == typeof(DateTimeOffset) || FilterPropertyType == typeof(DateTimeOffset?)) && value != null && value is DateTime?)
            {
                DateTimeOffset? offset = DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
                value = offset;
            }

            if ((FilterPropertyType == typeof(TimeOnly) || FilterPropertyType == typeof(TimeOnly?)) && value != null && value is string)
            {
                var v = TimeOnly.Parse($"{value}");
                value = FilterPropertyType == typeof(TimeOnly) ? v : (TimeOnly?)v;
            }

            if ((FilterPropertyType == typeof(Guid) || FilterPropertyType == typeof(Guid?)) && value != null && value is string)
            {
                var v = Guid.Parse($"{value}");
                value = FilterPropertyType == typeof(Guid) ? v : (Guid?)v;
            }

            if (!QueryableExtension.IsEnumerable(value?.GetType() ?? typeof(object)) && (PropertyAccess.IsEnum(FilterPropertyType) || (PropertyAccess.IsNullableEnum(FilterPropertyType))))
            {
                Type enumType = Enum.GetUnderlyingType(Nullable.GetUnderlyingType(FilterPropertyType) ?? FilterPropertyType);
                value = value is not null ? Convert.ChangeType(value, enumType) : null;
            }

            if (isFirst)
            {
                filterValue = CanSetCurrentValue(value) ? value : 
                    GetFilterOperator() == FilterOperator.IsEmpty  || GetFilterOperator() == FilterOperator.IsNotEmpty ? string.Empty : null;
            }
            else
            {
                secondFilterValue = CanSetCurrentValue(value, false) ? value :
                    GetSecondFilterOperator() == FilterOperator.IsEmpty || GetSecondFilterOperator() == FilterOperator.IsNotEmpty ? string.Empty : null;
            }
        }

        /// <summary>
        /// Set column filter value and reload grid.
        /// </summary>
        /// <param name="value">Filter value.</param>
        /// <param name="isFirst"><c>true</c> if FilterValue; <c>false</c> for SecondFilterValue</param>
        public async Task SetFilterValueAsync(object value, bool isFirst = true)
        {
            SetFilterValue(value, isFirst);
            Grid.SaveSettings();
            await Grid.FirstPage(true);
        }

        internal bool CanSetFilterValue(bool isFirst = true)
        {
            var fo = isFirst ? GetFilterOperator() : GetSecondFilterOperator();
            return fo != FilterOperator.IsNull
                    && fo != FilterOperator.IsNotNull
                    && fo != FilterOperator.IsEmpty
                    && fo != FilterOperator.IsNotEmpty;
        }

        internal bool CanSetCurrentValue(object value, bool isFirst = true)
        {
            return CanSetFilterValue(isFirst) ? !string.IsNullOrEmpty(value?.ToString()) : false;
        }

        internal bool HasCustomFilter()
        {
            return GetFilterOperator() == FilterOperator.Custom && GetCustomFilterExpression() != null;
        }

        internal bool HasActiveFilter()
        {
            return GetFilterValue() != null
            || GetSecondFilterValue() != null
            || !CanSetFilterValue()
            || HasCustomFilter();
        }

        /// <summary>
        /// Get custom filter linq.
        /// </summary>
        public string GetCustomFilterExpression()
        {
            return customFilterExpression ?? CustomFilterExpression;
        }

        /// <summary>
        /// Set column custom filter linq.
        /// </summary>
        public void SetCustomFilterExpression(string value)
        {
            customFilterExpression = value;
        }

        /// <summary>
        /// Set column custom filter linq and reload grid.
        /// </summary>
        /// <param name="value">Filter value.</param>
        public async Task SetCustomFilterExpressionAsync(string value)
        {
            SetCustomFilterExpression(value);
            Grid.SaveSettings();
            await Grid.FirstPage(true);
        }

        /// <summary>
        /// Sets to default column filter values and operators.
        /// </summary>
        public void ClearFilters()
        {
            var fo = FilterOperator == FilterOperator.Custom
                ? FilterOperator.Custom
                : typeof(System.Collections.IEnumerable).IsAssignableFrom(FilterPropertyType)
                    ? !string.IsNullOrEmpty(FilterProperty) && FilterProperty != Property ? FilterOperator.In : FilterOperator.Contains
                    : default(FilterOperator);

            SetFilterOperator(fo);
            SetSecondFilterOperator(null);

            filterValue = null;
            secondFilterValue = null;

            ClearFilterValues();

            LogicalFilterOperator = default(LogicalFilterOperator);

        }

        FilterOperator? _filterOperator;
        /// <summary>
        /// Gets or sets the filter operator.
        /// </summary>
        /// <value>The filter operator.</value>
        [Parameter]
        public FilterOperator FilterOperator 
        {
            get
            {
                return _filterOperator ?? FilterOperator.Equals;
            }
            set
            {
                _filterOperator = value;
            }
        }

        IEnumerable<FilterOperator> _filterOperators;
        /// <summary>
        /// Gets or sets the filter operators.
        /// </summary>
        /// <value>The filter operators.</value>
        [Parameter]
        public IEnumerable<FilterOperator> FilterOperators
        {
            get
            {
                return _filterOperators;
            }
            set
            {
                _filterOperators = value;
            }
        }

        /// <summary>
        /// Gets or sets the second filter operator.
        /// </summary>
        /// <value>The second filter operator.</value>
        [Parameter]
        public FilterOperator SecondFilterOperator { get; set; }

        /// <summary>
        /// Set column filter operator.
        /// </summary>
        public void SetFilterOperator(FilterOperator? value)
        {
            if (value == FilterOperator.IsEmpty || value == FilterOperator.IsNotEmpty || value == FilterOperator.IsNull || value == FilterOperator.IsNotNull)
            {
                filterValue = value == FilterOperator.IsEmpty || value == FilterOperator.IsNotEmpty ? string.Empty : null;
            }

            filterOperator = value;
        }

        /// <summary>
        /// Set column second filter operator.
        /// </summary>
        public void SetSecondFilterOperator(FilterOperator? value)
        {
            if (value == FilterOperator.IsEmpty || value == FilterOperator.IsNotEmpty || value == FilterOperator.IsNull || value == FilterOperator.IsNotNull)
            {
                secondFilterValue = value == FilterOperator.IsEmpty || value == FilterOperator.IsNotEmpty ? string.Empty : null;
            }

            secondFilterOperator = value;
        }

        /// <summary>
        /// Set column second logical operator.
        /// </summary>
        public void SetLogicalFilterOperator(LogicalFilterOperator value)
        {
            LogicalFilterOperator = value;
        }


        /// <summary>
        /// Closes this column filter popup.
        /// </summary>
        public async Task CloseFilter()
        {
            if (Grid.FilterPopupRenderMode == PopupRenderMode.OnDemand && headerCell != null)
            {
                await headerCell.CloseFilter();
            }
            await Grid.GetJSRuntime().InvokeVoidAsync("Radzen.closePopup", $"{Grid.PopupID}{GetFilterProperty()}");
        }

        string runtimeWidth;

        /// <summary>
        /// Set column width.
        /// </summary>
        public void SetWidth(string value)
        {
            runtimeWidth = value;

            if (IsFrozen())
            {
                InvokeAsync(() => Grid.ChangeState());
            }
        }

        /// <summary>
        /// Get column width.
        /// </summary>
        public string GetWidth()
        {
            return !string.IsNullOrEmpty(runtimeWidth) ? runtimeWidth : Width;
        }

        /// <summary>
        /// Get column width if it's set, otherwise get a column width set on the grid.
        /// </summary>
        internal string GetWidthOrGridSetting()
        {
            var internalWidth = GetWidth();
            return !string.IsNullOrWhiteSpace(internalWidth) ? internalWidth : Grid?.ColumnWidth;
        }

        /// <summary>
        /// Get possible column filter operators.
        /// </summary>
        public virtual IEnumerable<FilterOperator> GetFilterOperators()
        {
            if (FilterOperators != null) return FilterOperators;

            if (PropertyAccess.IsEnum(FilterPropertyType) || FilterPropertyType == typeof(bool))
                return new FilterOperator[] { FilterOperator.Equals, FilterOperator.NotEquals };

            if (PropertyAccess.IsNullableEnum(FilterPropertyType) || FilterPropertyType == typeof(bool?))
                return new FilterOperator[] { FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.IsNull, FilterOperator.IsNotNull };

            return Enum.GetValues(typeof(FilterOperator)).Cast<FilterOperator>().Where(o =>
            {
                var isStringOperator = o == FilterOperator.Contains || o == FilterOperator.DoesNotContain
                    || o == FilterOperator.StartsWith || o == FilterOperator.EndsWith || o == FilterOperator.IsEmpty || o == FilterOperator.IsNotEmpty;

                if ((FilterPropertyType == typeof(string) || !QueryableExtension.IsEnumerable(FilterPropertyType)) &&
                    (o == FilterOperator.In || o == FilterOperator.NotIn)) return false;

                return FilterPropertyType == typeof(string) || QueryableExtension.IsEnumerable(FilterPropertyType) ? isStringOperator
                      || o == FilterOperator.Equals || o == FilterOperator.NotEquals
                      || o == FilterOperator.IsNull || o == FilterOperator.IsNotNull
                    : !isStringOperator;
            });
        }

        /// <summary>
        /// Get filter operator text
        /// </summary>
        public string GetFilterOperatorText(FilterOperator filterOperator)
        {
            switch (filterOperator)
            {
                case FilterOperator.Custom:
                    return Grid?.CustomText;
                case FilterOperator.Contains:
                    return Grid?.ContainsText;
                case FilterOperator.DoesNotContain:
                    return Grid?.DoesNotContainText;
                case FilterOperator.EndsWith:
                    return Grid?.EndsWithText;
                case FilterOperator.Equals:
                    return Grid?.EqualsText;
                case FilterOperator.GreaterThan:
                    return Grid?.GreaterThanText;
                case FilterOperator.GreaterThanOrEquals:
                    return Grid?.GreaterThanOrEqualsText;
                case FilterOperator.LessThan:
                    return Grid?.LessThanText;
                case FilterOperator.LessThanOrEquals:
                    return Grid?.LessThanOrEqualsText;
                case FilterOperator.StartsWith:
                    return Grid?.StartsWithText;
                case FilterOperator.NotEquals:
                    return Grid?.NotEqualsText;
                case FilterOperator.IsNull:
                    return Grid?.IsNullText;
                case FilterOperator.IsEmpty:
                    return Grid?.IsEmptyText;
                case FilterOperator.IsNotNull:
                    return Grid?.IsNotNullText;
                case FilterOperator.IsNotEmpty:
                    return Grid?.IsNotEmptyText;
                default:
                    return $"{filterOperator}";
            }
        }

        internal string GetFilterOperatorSymbol(FilterOperator filterOperator)
        {
            var symbol = Grid.FilterCaseSensitivity == FilterCaseSensitivity.CaseInsensitive ? "a" : "A";
            switch (filterOperator)
            {
                case FilterOperator.Contains:
                    return $"*{symbol}*";
                case FilterOperator.DoesNotContain:
                    return $"*{symbol}*";
                case FilterOperator.StartsWith:
                    return $"{symbol}**";
                case FilterOperator.EndsWith:
                    return $"**{symbol}";
                case FilterOperator.Equals:
                    return "=";
                case FilterOperator.GreaterThan:
                    return ">";
                case FilterOperator.GreaterThanOrEquals:
                    return "≥";
                case FilterOperator.LessThan:
                    return "<";
                case FilterOperator.LessThanOrEquals:
                    return "≤";
                case FilterOperator.NotEquals:
                    return "≠";
                case FilterOperator.IsNull:
                    return "∅";
                case FilterOperator.IsNotNull:
                    return "!∅";
                case FilterOperator.IsEmpty:
                    return "= ''";
                case FilterOperator.IsNotEmpty:
                    return "≠ ''";
                case FilterOperator.In:
                    return "∈";
                case FilterOperator.NotIn:
                    return "∉";
                default:
                    return $"{filterOperator}";
            }
        }

        /// <summary>
        /// Gets value indicating if the user can specify time in DateTime column filter.
        /// </summary>
        public virtual bool ShowTimeForDateTimeFilter()
        {
#if NET6_0_OR_GREATER
            if (FilterPropertyType == typeof(DateOnly))
            {
                return false;
            }
#endif
            return true;
        }

        /// <summary>
        /// Gets value indicating if up and down buttons are displayed in numeric column filter.
        /// </summary>
        public virtual bool ShowUpDownForNumericFilter()
        {
            return true;
        }

        /// <summary>
        /// Gets an OData expression to filter by this column.
        /// </summary>
        /// <param name="second">Whether to use <see cref="SecondFilterValue"/> instead of <see cref="FilterValue"/></param>
        /// <returns>An OData expression to filter by this column.</returns>
        public string GetColumnODataFilter(bool second = false)
        {
            return GetColumnODataFilter(second ? GetSecondFilterValue() : GetFilterValue(), second ? GetSecondFilterOperator() : GetFilterOperator());
        }

        /// <summary>
        /// Gets an OData expression to filter by this column.
        /// </summary>
        /// <param name="filterValue">The specific value to filter by</param>
        /// <param name="filterOperator">The operator used to compare to <paramref name="filterValue"/></param>
        /// <returns>An OData expression to filter by this column.</returns>
        protected virtual string GetColumnODataFilter(object filterValue, FilterOperator filterOperator)
        {
            return QueryableExtension.GetColumnODataFilter(this, filterValue, filterOperator);
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Grid?.RemoveColumn(this);
        }

        /// <summary>
        /// Gets the column sort descriptor index indicating order of applied column sort in case of multiple sorting.
        /// </summary>
        public int? GetSortIndex()
        {
            var descriptor = Grid.sorts.Where(s => s.Property == GetSortProperty()).FirstOrDefault();
            if (descriptor != null)
            {
                return Grid.sorts.IndexOf(descriptor);
            }

            return null;
        }

        internal string getSortIndexAsString()
        {
            var index = GetSortIndex();
            return index != null ? $"{GetSortIndex() + 1}" : "";
        }

        internal RadzenDataGridHeaderCell<TItem> headerCell;
    }
}
