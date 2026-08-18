# RadzenPivotDataGrid API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Aggregates | `RenderFragment?` | Gets or sets the aggregates collection for pivot aggregates/measures. |
| AggregatesText | `string` | Gets or sets the Aggregates text. |
| AllowAlternatingRows | `bool` | Gets or sets a value indicating whether RadzenPivotDataGrid should use alternating row styles. |
| AllowDrillDown | `bool` | Gets or sets a value indicating whether drill down functionality is enabled. |
| AllowFieldsPicking | `bool` | Gets or sets a value indicating whether picking of fields runtime is allowed. Set to false by default. |
| AllowFilterDateInput | `bool` | Gets or sets whether to allow filter date input. |
| AllowFiltering | `bool` | Gets or sets a value indicating whether filtering is enabled. |
| AllowPaging | `bool` | Gets or sets a value indicating whether paging is allowed. Set to false by default. |
| AllowSorting | `bool` | Gets or sets a value indicating whether sorting is enabled. |
| AndOperatorText | `string` | Gets or sets the and operator text. |
| ApplyText | `string` | Gets or sets the apply text. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ClearText | `string` | Gets or sets the clear text. |
| Columns | `RenderFragment?` | Gets or sets the columns collection for pivot columns. |
| ColumnsText | `string` | Gets or sets the Columns text. |
| ContainsText | `string` | Gets or sets the contains text. |
| Count | `int` | Gets or sets the count. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| CustomText | `string` | Gets or sets the custom filter operator text. |
| Data | `IEnumerable<T>?` | Gets or sets the data. |
| Density | `Density` | Gets or sets a value indicating pager density. |
| DoesNotContainText | `string` | Gets or sets the does not contain text. |
| EmptyTemplate | `RenderFragment?` | Gets or sets the empty template shown when Data is empty collection. |
| EmptyText | `string` | Gets or sets the empty text shown when Data is empty collection. |
| EndsWithText | `string` | Gets or sets the ends with text. |
| EnumFilterSelectText | `string` | Gets or sets the enum filter select text. |
| EnumFilterTranslationFunc | `Func<object, string>?` | Gets or sets the enum filter translation function. |
| EqualsText | `string` | Gets or sets the equals text. |
| FieldsPickerExpanded | `bool` | Gets or sets value indicating if the fields picker is expanded. |
| FieldsPickerHeaderTemplate | `RenderFragment?` | Gets or sets the fields picker header template. |
| FieldsPickerHeaderText | `string` | Gets or sets the fields picker header text. |
| FilterCaseSensitivity | `FilterCaseSensitivity` | Gets or sets the filter case sensitivity. |
| FilterIcon | `string` | Gets or set the filter icon to use. |
| FilterOperatorAriaLabel | `string` | Gets or sets the filter operator aria label. |
| FilterText | `string` | Gets or sets the filter text. |
| FilterValueAriaLabel | `string` | Gets or sets the filter value aria label. |
| FirstPageAriaLabel | `string` | Gets or sets the pager's first page button's aria-label attribute. |
| FirstPageTitle | `string` | Gets or sets the pager's first page button's title attribute. |
| GreaterThanOrEqualsText | `string` | Gets or sets the greater than or equals text. |
| GreaterThanText | `string` | Gets or sets the greater than text. |
| GridLines | `DataGridGridLines` | Gets or sets the grid lines style. |
| InText | `string` | Gets or sets the in operator text. |
| IsEmptyText | `string` | Gets or sets the is empty text. |
| IsLoading | `bool` | Gets or sets a value indicating whether this instance loading indicator is shown. |
| IsNotEmptyText | `string` | Gets or sets the is not empty text. |
| IsNotNullText | `string` | Gets or sets the not null text. |
| IsNullText | `string` | Gets or sets the is null text. |
| LastPageAriaLabel | `string` | Gets or sets the pager's last page button's aria-label attribute. |
| LastPageTitle | `string` | Gets or sets the pager's last page button's title attribute. |
| LessThanOrEqualsText | `string` | Gets or sets the less than or equals text. |
| LessThanText | `string` | Gets or sets the less than text. |
| LoadingTemplate | `RenderFragment?` | Gets or sets the loading template. |
| LogicalFilterOperator | `LogicalFilterOperator` | Gets or sets the logical filter operator. |
| LogicalOperatorAriaLabel | `string` | Gets or sets the logical operator aria label. |
| NextPageAriaLabel | `string` | Gets or sets the pager's next page button's aria-label attribute. |
| NextPageLabel | `string?` | Gets or sets the pager's optional next page button's label text. |
| NextPageTitle | `string` | Gets or sets the pager's next page button's title attribute. |
| NotEqualsText | `string` | Gets or sets the not equals text. |
| NotInText | `string` | Gets or sets the not in operator text. |
| OrOperatorText | `string` | Gets or sets the or operator text. |
| PageAriaLabelFormat | `string` | Gets or sets the pager's numeric page number buttons' aria-label attributes. |
| PageNumbersCount | `int` | Gets or sets the page numbers count. |
| PageSize | `int` | Gets or sets the size of the page. |
| PageSizeOptions | `IEnumerable<int>?` | Gets or sets the page size options. |
| PageSizeText | `string` | Gets or sets the page size description text. |
| PageTitleFormat | `string` | Gets or sets the pager's numeric page number buttons' title attributes. |
| PagerAlwaysVisible | `bool` | Gets or sets a value indicating whether pager is visible even when not enough data for paging. |
| PagerHorizontalAlign | `HorizontalAlign` | Gets or sets the horizontal align. |
| PagerPosition | `PagerPosition` | Gets or sets the pager position. Set to PagerPosition.Bottom by default. |
| PagingSummaryFormat | `string` | Gets or sets the pager summary format. has preference over this property. |
| PagingSummaryTemplate | `RenderFragment<PagingInformation>?` | Gets or sets the pager summary template. Has preference over . |
| PrevPageAriaLabel | `string` | Gets or sets the pager's previous page button's aria-label attribute. |
| PrevPageLabel | `string?` | Gets or sets the pager's optional previous page button's label text. |
| PrevPageTitle | `string` | Gets or sets the pager's previous page button's title attribute. |
| Rows | `RenderFragment?` | Gets or sets the rows collection for pivot rows. |
| RowsText | `string` | Gets or sets the Rows text. |
| SecondFilterOperatorAriaLabel | `string` | Gets or sets the second filter operator aria label. |
| SecondFilterValueAriaLabel | `string` | Gets or sets the second filter value aria label. |
| ShowColumnsTotals | `bool` | Gets or sets a value indicating whether to show column totals. |
| ShowPagingSummary | `bool` | Gets or sets the pager summary visibility. |
| ShowRowsTotals | `bool` | Gets or sets a value indicating whether to show row totals. |
| SortAriaLabelFormat | `string` | Gets or sets the sort aria label format. |
| StartsWithText | `string` | Gets or sets the starts with text. |
| Style | `string?` | Gets or sets the inline CSS style. |
| Template | `RenderFragment<T>?` | Gets or sets the template. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| LoadData | `EventCallback<Radzen.LoadDataArgs>` | Gets or sets the load data. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| Page | `EventCallback<PagerEventArgs>` | Gets or sets the page callback. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| AddPivotAggregate(RadzenPivotAggregate<TItem> aggregate) | `void` | Adds a pivot aggregate to the pivot grid if it does not already exist. |
| AddPivotColumn(RadzenPivotColumn<TItem> column) | `void` | Adds a pivot column to the pivot grid if it does not already exist. |
| AddPivotField(RadzenPivotField<TItem> field) | `void` | Adds a pivot field to the pivot grid if it does not already exist. |
| AddPivotRow(RadzenPivotRow<TItem> row) | `void` | Adds a pivot row to the pivot grid if it does not already exist. |
| GetAggregateValue(IQueryable<TItem> items, RadzenPivotAggregate<TItem> aggregate) | `object?` | Gets the aggregate value for a group, considering collapsed state. |
| GetFilterOperatorText(FilterOperator? filterOperator) | `string` | Gets the filter operator text. |
| ToggleColumnDrillDown(string pathKey) | `Task` | Toggles the drill down state for a column group. |
| ToggleRowDrillDown(string pathKey) | `Task` | Toggles the drill down state for a row group. |

