# RadzenDataFilter API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AddFilterGroupText | `string` | Gets or sets the add filter group text. |
| AddFilterText | `string` | Gets or sets the add filter text. |
| AllowColumnFiltering | `bool` | Gets or sets a value indicating whether the columns can be filtered. |
| AndOperatorText | `string` | Gets or sets the and operator text. |
| ApplyFilterText | `string` | Gets or sets the apply filter text. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Auto | `bool` | Gets or sets a value indicating whether this filter is automatic. |
| ClearFilterText | `string` | Gets or sets the clear filter text. |
| ContainsText | `string` | Gets or sets the contains text. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| CustomText | `string` | Gets or sets the custom filter operator text. |
| Data | `IEnumerable<TItem>?` | Gets or sets the data. |
| DoesNotContainText | `string` | Gets or sets the does not contain text. |
| EndsWithText | `string` | Gets or sets the ends with text. |
| EnumFilterSelectText | `string` | Gets or sets the enum filter select text. |
| EqualsText | `string` | Gets or sets the equals text. |
| FilterCaseSensitivity | `FilterCaseSensitivity` | Gets or sets the filter case sensitivity. |
| FilterDateFormat | `string` | Gets or sets the filter date format. |
| FilterOperatorAriaLabel | `string` | Gets or sets the aria-label of the filter operator dropdown. |
| FilterText | `string` | Gets or sets the filter text. |
| FilterValueAriaLabel | `string` | Gets or sets the aria-label of the filter value editor. |
| GreaterThanOrEqualsText | `string` | Gets or sets the greater than or equals text. |
| GreaterThanText | `string` | Gets or sets the greater than text. |
| InText | `string` | Gets or sets the in operator text. |
| IsEmptyText | `string` | Gets or sets the is empty text. |
| IsNotEmptyText | `string` | Gets or sets the is not empty text. |
| IsNotNullText | `string` | Gets or sets the not null text. |
| IsNullText | `string` | Gets or sets the is null text. |
| LessThanOrEqualsText | `string` | Gets or sets the less than or equals text. |
| LessThanText | `string` | Gets or sets the less than text. |
| LogicalFilterOperator | `LogicalFilterOperator` | Gets or sets the logical filter operator. |
| NotEqualsText | `string` | Gets or sets the not equals text. |
| NotInText | `string` | Gets or sets the not in operator text. |
| OrOperatorText | `string` | Gets or sets the or operator text. |
| Properties | `RenderFragment?` | Gets or sets the properties. |
| PropertyAriaLabel | `string` | Gets or sets the aria-label of the filter property dropdown. |
| RemoveFilterText | `string` | Gets or sets the remove filter text. |
| StartsWithText | `string` | Gets or sets the starts with text. |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| UniqueFilters | `bool` | Gets or sets a value indicating whether properties can be reused in the filter. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| ViewChanged | `EventCallback<IQueryable<TItem>>` | Gets or sets the view changed callback. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| AddFilter(CompositeFilterDescriptor filter) | `Task` | Add filter. |
| ClearFilters() | `Task` | Clear filters. |
| Filter() | `Task` | Recreates View using current Filters. |
| RemoveFilter(CompositeFilterDescriptor filter) | `Task` | Remove filter. |

