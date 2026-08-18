# RadzenDataList API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowPaging | `bool` | Gets or sets a value indicating whether paging is allowed. Set to false by default. |
| AllowVirtualization | `bool` | Gets or sets whether the DataList uses virtualization to improve performance with large datasets. When enabled, only visible items are rendered in the DOM, significantly improving performance for long lists. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Count | `int` | Gets or sets the count. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Data | `IEnumerable<T>?` | Gets or sets the data. |
| Density | `Density` | Gets or sets a value indicating pager density. |
| EmptyTemplate | `RenderFragment?` | Gets or sets a custom template for rendering the empty state when the data source has no items. Takes precedence over when both are set. Use this for rich empty states with images, icons, or action buttons. |
| EmptyText | `string` | Gets or sets the text message displayed when the data source is empty. Only shown if is true and no is specified. |
| FirstPageAriaLabel | `string` | Gets or sets the pager's first page button's aria-label attribute. |
| FirstPageTitle | `string` | Gets or sets the pager's first page button's title attribute. |
| IsLoading | `bool` | Gets or sets a value indicating whether this instance loading indicator is shown. |
| LastPageAriaLabel | `string` | Gets or sets the pager's last page button's aria-label attribute. |
| LastPageTitle | `string` | Gets or sets the pager's last page button's title attribute. |
| LoadingTemplate | `RenderFragment?` | Gets or sets the loading template. |
| NextPageAriaLabel | `string` | Gets or sets the pager's next page button's aria-label attribute. |
| NextPageLabel | `string?` | Gets or sets the pager's optional next page button's label text. |
| NextPageTitle | `string` | Gets or sets the pager's next page button's title attribute. |
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
| ShowEmptyMessage | `bool` | Gets or sets whether to display an empty message when the data source has no items. Enable this to show EmptyText or EmptyTemplate when the list is empty, providing user feedback. |
| ShowPagingSummary | `bool` | Gets or sets the pager summary visibility. |
| Style | `string?` | Gets or sets the inline CSS style. |
| Template | `RenderFragment<T>?` | Gets or sets the template. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |
| WrapItems | `bool` | Gets or sets whether items should wrap to multiple rows based on their width and the container size. When true, items flow horizontally and wrap like words in a paragraph. When false, items stack vertically. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| LoadData | `EventCallback<Radzen.LoadDataArgs>` | Gets or sets the load data. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| Page | `EventCallback<PagerEventArgs>` | Gets or sets the page callback. |

