using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDataFilter component.
    /// </summary>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    [CascadingTypeParameter(nameof(TItem))]
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2067, Justification = TrimMessages.DataTypePreserved)]
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2070, Justification = TrimMessages.DataTypePreserved)]
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2072, Justification = TrimMessages.DataTypePreserved)]
    public partial class RadzenDataFilter<TItem> : RadzenComponent
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-datafilter";
        }

        /// <summary>
        /// Gets or sets the properties.
        /// </summary>
        /// <value>The properties.</value>
        [Parameter]
        public RenderFragment? Properties { get; set; }

        /// <summary>
        /// The data
        /// </summary>
        IEnumerable<TItem>? _data;

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        [Parameter]
        public IEnumerable<TItem>? Data
        {
            get
            {
                return _data;
            }
            set
            {
                if (_data != value)
                {
                    _data = value;
                    StateHasChanged();
                }
            }
        }

        IQueryable<TItem>? _view;

        /// <summary>
        /// Gets the view.
        /// </summary>
        /// <value>The view.</value>
        public virtual IQueryable<TItem> View
        {
            get
            {
                if (_view == null)
                {
                    _view = Data != null ? Data.AsQueryable().Where<TItem>(this) : Enumerable.Empty<TItem>().AsQueryable();
                }

                return _view;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this filter is automatic.
        /// </summary>
        /// <value><c>true</c> if filter automatic; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Auto { get; set; } = true;

        /// <summary>
        /// Gets or sets the filters.
        /// </summary>
        /// <value>The filters.</value>
        public IEnumerable<CompositeFilterDescriptor> Filters { get; set; } = Enumerable.Empty<CompositeFilterDescriptor>();

        /// <summary>
        /// Gets or sets the logical filter operator.
        /// </summary>
        /// <value>The logical filter operator.</value>
        [Parameter]
        public LogicalFilterOperator LogicalFilterOperator { get; set; } = LogicalFilterOperator.And;

        /// <summary>
        /// Gets or sets the filter case sensitivity.
        /// </summary>
        /// <value>The filter case sensitivity.</value>
        [Parameter]
        public FilterCaseSensitivity FilterCaseSensitivity { get; set; } = FilterCaseSensitivity.Default;


        private string? filterText;

        /// <summary>
        /// Gets or sets the filter text.
        /// </summary>
        /// <value>The filter text.</value>
        [Parameter]
        public string FilterText { get => filterText ?? Localize(nameof(RadzenStrings.DataFilter_FilterText)); set => filterText = value; }

        private string? enumFilterSelectText;

        /// <summary>
        /// Gets or sets the enum filter select text.
        /// </summary>
        /// <value>The enum filter select text.</value>
        [Parameter]
        public string EnumFilterSelectText { get => enumFilterSelectText ?? Localize(nameof(RadzenStrings.DataFilter_EnumFilterSelectText)); set => enumFilterSelectText = value; }

        private string? andOperatorText;

        /// <summary>
        /// Gets or sets the and operator text.
        /// </summary>
        /// <value>The and operator text.</value>
        [Parameter]
        public string AndOperatorText { get => andOperatorText ?? Localize(nameof(RadzenStrings.DataFilter_AndOperatorText)); set => andOperatorText = value; }

        private string? orOperatorText;

        /// <summary>
        /// Gets or sets the or operator text.
        /// </summary>
        /// <value>The or operator text.</value>
        [Parameter]
        public string OrOperatorText { get => orOperatorText ?? Localize(nameof(RadzenStrings.DataFilter_OrOperatorText)); set => orOperatorText = value; }

        private string? applyFilterText;

        /// <summary>
        /// Gets or sets the apply filter text.
        /// </summary>
        /// <value>The apply filter text.</value>
        [Parameter]
        public string ApplyFilterText { get => applyFilterText ?? Localize(nameof(RadzenStrings.DataFilter_ApplyFilterText)); set => applyFilterText = value; }

        private string? clearFilterText;

        /// <summary>
        /// Gets or sets the clear filter text.
        /// </summary>
        /// <value>The clear filter text.</value>
        [Parameter]
        public string ClearFilterText { get => clearFilterText ?? Localize(nameof(RadzenStrings.DataFilter_ClearFilterText)); set => clearFilterText = value; }

        private string? addFilterText;

        /// <summary>
        /// Gets or sets the add filter text.
        /// </summary>
        /// <value>The add filter text.</value>
        [Parameter]
        public string AddFilterText { get => addFilterText ?? Localize(nameof(RadzenStrings.DataFilter_AddFilterText)); set => addFilterText = value; }

        private string? removeFilterText;

        /// <summary>
        /// Gets or sets the remove filter text.
        /// </summary>
        /// <value>The remove filter text.</value>
        [Parameter]
        public string RemoveFilterText { get => removeFilterText ?? Localize(nameof(RadzenStrings.DataFilter_RemoveFilterText)); set => removeFilterText = value; }

        private string? addFilterGroupText;

        /// <summary>
        /// Gets or sets the add filter group text.
        /// </summary>
        /// <value>The add filter group text.</value>
        [Parameter]
        public string AddFilterGroupText { get => addFilterGroupText ?? Localize(nameof(RadzenStrings.DataFilter_AddFilterGroupText)); set => addFilterGroupText = value; }

        private string? equalsText;

        /// <summary>
        /// Gets or sets the equals text.
        /// </summary>
        /// <value>The equals text.</value>
        [Parameter]
        public string EqualsText { get => equalsText ?? Localize(nameof(RadzenStrings.DataFilter_EqualsText)); set => equalsText = value; }

        private string? notEqualsText;

        /// <summary>
        /// Gets or sets the not equals text.
        /// </summary>
        /// <value>The not equals text.</value>
        [Parameter]
        public string NotEqualsText { get => notEqualsText ?? Localize(nameof(RadzenStrings.DataFilter_NotEqualsText)); set => notEqualsText = value; }

        private string? lessThanText;

        /// <summary>
        /// Gets or sets the less than text.
        /// </summary>
        /// <value>The less than text.</value>
        [Parameter]
        public string LessThanText { get => lessThanText ?? Localize(nameof(RadzenStrings.DataFilter_LessThanText)); set => lessThanText = value; }

        private string? lessThanOrEqualsText;

        /// <summary>
        /// Gets or sets the less than or equals text.
        /// </summary>
        /// <value>The less than or equals text.</value>
        [Parameter]
        public string LessThanOrEqualsText { get => lessThanOrEqualsText ?? Localize(nameof(RadzenStrings.DataFilter_LessThanOrEqualsText)); set => lessThanOrEqualsText = value; }

        private string? greaterThanText;

        /// <summary>
        /// Gets or sets the greater than text.
        /// </summary>
        /// <value>The greater than text.</value>
        [Parameter]
        public string GreaterThanText { get => greaterThanText ?? Localize(nameof(RadzenStrings.DataFilter_GreaterThanText)); set => greaterThanText = value; }

        private string? greaterThanOrEqualsText;

        /// <summary>
        /// Gets or sets the greater than or equals text.
        /// </summary>
        /// <value>The greater than or equals text.</value>
        [Parameter]
        public string GreaterThanOrEqualsText { get => greaterThanOrEqualsText ?? Localize(nameof(RadzenStrings.DataFilter_GreaterThanOrEqualsText)); set => greaterThanOrEqualsText = value; }

        private string? endsWithText;

        /// <summary>
        /// Gets or sets the ends with text.
        /// </summary>
        /// <value>The ends with text.</value>
        [Parameter]
        public string EndsWithText { get => endsWithText ?? Localize(nameof(RadzenStrings.DataFilter_EndsWithText)); set => endsWithText = value; }

        private string? containsText;

        /// <summary>
        /// Gets or sets the contains text.
        /// </summary>
        /// <value>The contains text.</value>
        [Parameter]
        public string ContainsText { get => containsText ?? Localize(nameof(RadzenStrings.DataFilter_ContainsText)); set => containsText = value; }

        private string? doesNotContainText;

        /// <summary>
        /// Gets or sets the does not contain text.
        /// </summary>
        /// <value>The does not contain text.</value>
        [Parameter]
        public string DoesNotContainText { get => doesNotContainText ?? Localize(nameof(RadzenStrings.DataFilter_DoesNotContainText)); set => doesNotContainText = value; }

        private string? inText;

        /// <summary>
        /// Gets or sets the in operator text.
        /// </summary>
        /// <value>The in operator text.</value>
        [Parameter]
        public string InText { get => inText ?? Localize(nameof(RadzenStrings.DataFilter_InText)); set => inText = value; }

        private string? notInText;

        /// <summary>
        /// Gets or sets the not in operator text.
        /// </summary>
        /// <value>The not in operator text.</value>
        [Parameter]
        public string NotInText { get => notInText ?? Localize(nameof(RadzenStrings.DataFilter_NotInText)); set => notInText = value; }

        private string? startsWithText;

        /// <summary>
        /// Gets or sets the starts with text.
        /// </summary>
        /// <value>The starts with text.</value>
        [Parameter]
        public string StartsWithText { get => startsWithText ?? Localize(nameof(RadzenStrings.DataFilter_StartsWithText)); set => startsWithText = value; }

        private string? isNotNullText;

        /// <summary>
        /// Gets or sets the not null text.
        /// </summary>
        /// <value>The not null text.</value>
        [Parameter]
        public string IsNotNullText { get => isNotNullText ?? Localize(nameof(RadzenStrings.DataFilter_IsNotNullText)); set => isNotNullText = value; }

        private string? isNullText;

        /// <summary>
        /// Gets or sets the is null text.
        /// </summary>
        /// <value>The null text.</value>
        [Parameter]
        public string IsNullText { get => isNullText ?? Localize(nameof(RadzenStrings.DataFilter_IsNullText)); set => isNullText = value; }

        private string? isEmptyText;

        /// <summary>
        /// Gets or sets the is empty text.
        /// </summary>
        /// <value>The empty text.</value>
        [Parameter]
        public string IsEmptyText { get => isEmptyText ?? Localize(nameof(RadzenStrings.DataFilter_IsEmptyText)); set => isEmptyText = value; }

        private string? isNotEmptyText;

        /// <summary>
        /// Gets or sets the is not empty text.
        /// </summary>
        /// <value>The not empty text.</value>
        [Parameter]
        public string IsNotEmptyText { get => isNotEmptyText ?? Localize(nameof(RadzenStrings.DataFilter_IsNotEmptyText)); set => isNotEmptyText = value; }

        private string? customText;

        /// <summary>
        /// Gets or sets the custom filter operator text.
        /// </summary>
        /// <value>The custom filter operator text.</value>
        [Parameter]
        public string CustomText { get => customText ?? Localize(nameof(RadzenStrings.DataFilter_CustomText)); set => customText = value; }

        /// <summary>
        /// Gets or sets a value indicating whether the columns can be filtered.
        /// </summary>
        /// <value><c>true</c> if columns can be filtered; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowColumnFiltering { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether properties can be reused in the filter.
        /// </summary>
        /// <value><c>true</c>, if there is only one filter by property; otherwise <c>false</c>.</value>
        [Parameter]
        public bool UniqueFilters { get; set; }

        /// <summary>
        /// Gets the properties collection.
        /// </summary>
        /// <value>The properties collection.</value>
        public IList<RadzenDataFilterProperty<TItem>> PropertiesCollection
        {
            get
            {
                return properties;
            }
        }
        
        internal List<RadzenDataFilterProperty<TItem>> properties = new List<RadzenDataFilterProperty<TItem>>();
        internal void AddProperty(RadzenDataFilterProperty<TItem> property)
        {
            if (!properties.Contains(property))
            {
                properties.Add(property);
            }

            StateHasChanged();
        }

        internal void RemoveProperty(RadzenDataFilterProperty<TItem> property)
        {
            properties.Remove(property);

            if (!disposed)
            {
                try { InvokeAsync(StateHasChanged); } catch { }
            }
        }

        /// <summary>
        /// Recreates View using current Filters.
        /// </summary>
        public async Task Filter()
        {
            _view = null;

            await ViewChanged.InvokeAsync(View);
        }

        /// <summary>
        /// Gets or sets the view changed callback.
        /// </summary>
        /// <value>The view changed callback.</value>
        [Parameter]
        public EventCallback<IQueryable<TItem>> ViewChanged { get; set; }

        internal async Task ChangeState()
        {
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Gets or sets the filter date format.
        /// </summary>
        /// <value>The filter date format.</value>
        [Parameter]
        public string FilterDateFormat { get; set; } = string.Empty;

        internal async Task AddFilter(bool isGroup)
        {
            if (UniqueFilters && properties.All(f => f.IsSelected))
            {
                return;
            }
            if (isGroup)
            {
                Filters = Filters.Concat(new CompositeFilterDescriptor[]
                    {
                        new CompositeFilterDescriptor()
                        {
                            Filters = Enumerable.Empty<CompositeFilterDescriptor>()
                        }
                    }
                );
            }
            else
            {
                Filters = Filters.Concat(new CompositeFilterDescriptor[] { new CompositeFilterDescriptor() });
            }

            if (Auto)
            {
                await Filter();
            }
        }

        /// <summary>
        /// Clear filters.
        /// </summary>
        public async Task ClearFilters()
        {
            Filters = Enumerable.Empty<CompositeFilterDescriptor>();
            
            properties.ForEach(p => p.IsSelected = false);
            
            if (Auto)
            {
                await Filter();
            }
        }

        /// <summary>
        /// Add filter.
        /// </summary>
        public async Task AddFilter(CompositeFilterDescriptor filter)
        {
            Filters = Filters.Concat(new CompositeFilterDescriptor[] { filter });

            if (Auto)
            {
                await Filter();
            }
        }

        /// <summary>
        /// Remove filter.
        /// </summary>
        public async Task RemoveFilter(CompositeFilterDescriptor filter)
        {
            Filters = Filters.Where(f => f != filter);

            if (Auto)
            {
                await Filter();
            }
        }
    }
}
