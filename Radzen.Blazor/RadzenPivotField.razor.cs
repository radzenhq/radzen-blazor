using Microsoft.AspNetCore.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Base component for RadzenPivotDataGrid Rows, Columns and Aggregates.
    /// </summary>
    /// <typeparam name="TItem">The type of the PivotDataGrid item.</typeparam>
    public partial class RadzenPivotField<TItem> : ComponentBase, IDisposable
    {
        /// <summary>
        /// Gets or sets the property name.
        /// </summary>
        [Parameter]
        public string Property { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [Parameter]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        [Parameter]
        public SortOrder? SortOrder { get; set; }

        /// <summary>
        /// Gets or sets the sequence of the <see cref="Radzen.SortOrder"/>s to use when sorting the column interactively.
        /// </summary>
        [Parameter]
        public SortOrder?[] SortOrderSequence { get; set; } = [Radzen.SortOrder.Ascending, Radzen.SortOrder.Descending, null];

        /// <summary>
        /// Gets or sets a value indicating whether this column is sortable.
        /// </summary>
        [Parameter]
        public bool Sortable { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this column is filterable.
        /// </summary>
        [Parameter]
        public bool Filterable { get; set; } = true;

        /// <summary>
        /// Gets or sets the filter value.
        /// </summary>
        [Parameter]
        public object FilterValue { get; set; }

        /// <summary>
        /// Gets or sets the second filter value.
        /// </summary>
        [Parameter]
        public object SecondFilterValue { get; set; }

        /// <summary>
        /// Gets or sets the filter operator.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the second filter operator.
        /// </summary>
        [Parameter]
        public FilterOperator SecondFilterOperator { get; set; }

        /// <summary>
        /// Gets or sets the logical filter operator.
        /// </summary>
        [Parameter]
        public LogicalFilterOperator LogicalFilterOperator { get; set; }

        /// <summary>
        /// Gets or sets the filter template.
        /// </summary>
        [Parameter]
        public RenderFragment<RadzenPivotField<TItem>> FilterTemplate { get; set; }

        /// <summary>
        /// Gets or sets the filter value template.
        /// </summary>
        [Parameter]
        public RenderFragment<RadzenPivotField<TItem>> FilterValueTemplate { get; set; }

        /// <summary>
        /// Gets or sets the second filter value template.
        /// </summary>
        [Parameter]
        public RenderFragment<RadzenPivotField<TItem>> SecondFilterValueTemplate { get; set; }

        private SortOrder? internalSortOrder;

        /// <summary>
        /// Gets the current sort order (internal state).
        /// </summary>
        public SortOrder? GetSortOrder()
        {
            return internalSortOrder ?? SortOrder;
        }

        /// <summary>
        /// Sets the internal sort order.
        /// </summary>
        internal void SetSortOrderInternal(SortOrder? sortOrder)
        {
            internalSortOrder = sortOrder;
        }

        /// <summary>
        /// Resets the sort order.
        /// </summary>
        internal void ResetSortOrder()
        {
            internalSortOrder = null;
        }

        private object internalFilterValue;
        private object internalSecondFilterValue;
        private FilterOperator? internalSecondFilterOperator;
        private LogicalFilterOperator? internalLogicalFilterOperator;

        /// <summary>
        /// Gets the current filter value (internal state).
        /// </summary>
        public object GetFilterValue()
        {
            return internalFilterValue ?? FilterValue;
        }

        /// <summary>
        /// Sets the internal filter value.
        /// </summary>
        internal void SetFilterValueInternal(object filterValue)
        {
            internalFilterValue = filterValue;
        }

        /// <summary>
        /// Gets the current second filter value (internal state).
        /// </summary>
        public object GetSecondFilterValue()
        {
            return internalSecondFilterValue ?? SecondFilterValue;
        }

        /// <summary>
        /// Sets the internal second filter value.
        /// </summary>
        internal void SetSecondFilterValueInternal(object filterValue)
        {
            internalSecondFilterValue = filterValue;
        }

        /// <summary>
        /// Gets the current filter operator (internal state).
        /// </summary>
        public FilterOperator GetFilterOperator()
        {
            return filterOperator ?? FilterOperator;
        }

        /// <summary>
        /// Sets the internal filter operator.
        /// </summary>
        internal void SetFilterOperatorInternal(FilterOperator? filterOperator)
        {
            this.filterOperator = filterOperator;
        }

        /// <summary>
        /// Gets the current second filter operator (internal state).
        /// </summary>
        public FilterOperator GetSecondFilterOperator()
        {
            return internalSecondFilterOperator ?? SecondFilterOperator;
        }

        /// <summary>
        /// Sets the internal second filter operator.
        /// </summary>
        internal void SetSecondFilterOperatorInternal(FilterOperator? filterOperator)
        {
            internalSecondFilterOperator = filterOperator;
        }

        /// <summary>
        /// Gets the current logical filter operator (internal state).
        /// </summary>
        public LogicalFilterOperator GetLogicalFilterOperator()
        {
            return internalLogicalFilterOperator ?? LogicalFilterOperator;
        }

        /// <summary>
        /// Sets the internal logical filter operator.
        /// </summary>
        internal void SetLogicalFilterOperatorInternal(LogicalFilterOperator? logicalFilterOperator)
        {
            internalLogicalFilterOperator = logicalFilterOperator;
        }

        /// <summary>
        /// Gets the filter property name.
        /// </summary>
        public string GetFilterProperty()
        {
            return Property;
        }

        /// <summary>
        /// Checks if the field has an active filter.
        /// </summary>
        public bool HasActiveFilter()
        {
            return GetFilterValue() != null || GetSecondFilterValue() != null;
        }

        /// <summary>
        /// Clears all filter values.
        /// </summary>
        internal void ClearFilterValues()
        {
            internalFilterValue = null;
            internalSecondFilterValue = null;
        }

        /// <summary>
        /// Gets the column title.
        /// </summary>
        public string GetTitle()
        {
            return !string.IsNullOrEmpty(Title) ? Title : Property;
        }

        /// <summary>
        /// Gets or sets the parent pivot data grid component.
        /// </summary>
        [CascadingParameter]
        public RadzenPivotDataGrid<TItem> PivotGrid { get; set; }

        /// <summary>
        /// Called when initialized.
        /// </summary>
        protected override void OnInitialized()
        {
            if (PivotGrid != null)
            {
                PivotGrid.AddPivotField(this);

                var property = GetFilterProperty();

                if (!string.IsNullOrEmpty(property))
                {
                    _propertyType = PropertyAccess.GetPropertyType(typeof(TItem), property);
                }

                if (!string.IsNullOrEmpty(property) && Type == null)
                {
                    _filterPropertyType = _propertyType;
                }

                if (_filterPropertyType == null)
                {
                    _filterPropertyType = Type;
                }
                else if (!string.IsNullOrEmpty(Property))
                {
                    propertyValueGetter = PropertyAccess.Getter<TItem, object>(Property);
                }

                if (!string.IsNullOrEmpty(Property) && (typeof(TItem).IsGenericType && typeof(IDictionary<,>).IsAssignableFrom(typeof(TItem).GetGenericTypeDefinition()) ||
                    typeof(IDictionary).IsAssignableFrom(typeof(TItem)) || typeof(System.Data.DataRow).IsAssignableFrom(typeof(TItem))))
                {
                    propertyValueGetter = PropertyAccess.Getter<TItem, object>(Property);
                }

                if (_filterPropertyType == typeof(string) && filterOperator != FilterOperator.Custom && filterOperator == null && _filterOperator == null)
                {
                    SetFilterOperator(FilterOperator.Contains);
                }
            }
        }

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
        /// Gets value indicating if up and down buttons are displayed in numeric column filter.
        /// </summary>
        public virtual bool ShowUpDownForNumericFilter()
        {
            return true;
        }

        internal bool CanSetFilterValue(bool isFirst = true)
        {
            var fo = isFirst ? GetFilterOperator() : GetSecondFilterOperator();
            return fo != FilterOperator.IsNull
                    && fo != FilterOperator.IsNotNull
                    && fo != FilterOperator.IsEmpty
                    && fo != FilterOperator.IsNotEmpty;
        }

        /// <summary>
        /// Set column filter value.
        /// </summary>
        public void SetFilterValue(object value, bool isFirst = true)
        {
            if ((FilterPropertyType == typeof(DateTimeOffset) || FilterPropertyType == typeof(DateTimeOffset?)) && value != null && value is DateTime?)
            {
                DateTimeOffset? offset = DateTime.SpecifyKind((DateTime)value, ((DateTime?)value).Value.Kind);
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
                    GetFilterOperator() == FilterOperator.IsEmpty || GetFilterOperator() == FilterOperator.IsNotEmpty ? string.Empty : null;
            }
            else
            {
                secondFilterValue = CanSetCurrentValue(value, false) ? value :
                    GetSecondFilterOperator() == FilterOperator.IsEmpty || GetSecondFilterOperator() == FilterOperator.IsNotEmpty ? string.Empty : null;
            }
        }

        internal bool CanSetCurrentValue(object value, bool isFirst = true)
        {
            return CanSetFilterValue(isFirst) ? !string.IsNullOrEmpty(value?.ToString()) : false;
        }

        FilterOperator? _filterOperator;
        Func<TItem, object> propertyValueGetter;
        object filterValue;
        FilterOperator? filterOperator;
        object secondFilterValue;
        FilterOperator? secondFilterOperator;
        LogicalFilterOperator? logicalFilterOperator;
        Type _propertyType;
        internal Type PropertyType => _propertyType;
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

        /// <summary>
        /// Gets or sets the data type.
        /// </summary>
        /// <value>The data type.</value>
        [Parameter]
        public Type Type { get; set; }

        /// <summary>
        /// Disposes the component and removes it from the parent pivot grid.
        /// </summary>
        public virtual void Dispose()
        {
            PivotGrid?.RemovePivotField(this);
        }

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(FilterValue), FilterValue))
            {
                filterValue = parameters.GetValueOrDefault<object>(nameof(FilterValue));

                if (FilterTemplate != null || FilterValueTemplate != null)
                {
                    FilterValue = filterValue;
                    await PivotGrid.Reload();

                    return;
                }
            }

            if (parameters.DidParameterChange(nameof(SecondFilterValue), SecondFilterValue))
            {
                secondFilterValue = parameters.GetValueOrDefault<object>(nameof(SecondFilterValue));

                if (FilterTemplate != null || SecondFilterValueTemplate != null)
                {
                    SecondFilterValue = secondFilterValue;
                    await PivotGrid.Reload();

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
    }
} 