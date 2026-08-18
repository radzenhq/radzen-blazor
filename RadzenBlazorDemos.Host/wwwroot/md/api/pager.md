# RadzenPager API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowReload | `bool` | Gets or sets a value indicating whether the reload button is shown. |
| AlwaysVisible | `bool` | Gets or sets a value indicating whether pager is visible even when not enough data for paging. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Count | `int` | Gets or sets the total items count. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Density | `Density` | Gets or sets a value indicating Pager density. |
| FirstPageAriaLabel | `string` | Gets or sets the pager's first page button's aria-label attribute. |
| FirstPageTitle | `string` | Gets or sets the pager's first page button's title attribute. |
| HorizontalAlign | `HorizontalAlign` | Gets or sets the horizontal align. |
| LastPageAriaLabel | `string` | Gets or sets the pager's last page button's aria-label attribute. |
| LastPageTitle | `string` | Gets or sets the pager's last page button's title attribute. |
| NavigationAriaLabel | `string` | Gets or sets the navigation aria-label. |
| NextPageAriaLabel | `string` | Gets or sets the pager's next page button's aria-label attribute. |
| NextPageLabel | `string?` | Gets or sets the pager's optional next page button's label text. |
| NextPageTitle | `string` | Gets or sets the pager's next page button's title attribute. |
| PageAriaLabelFormat | `string` | Gets or sets the pager's numeric page number buttons' aria-label attributes. |
| PageNumbersCount | `int` | Gets or sets the page numbers count. |
| PageSize | `int` | Gets or sets the page size. |
| PageSizeOptions | `IEnumerable<int>?` | Gets or sets the page size options. |
| PageSizeText | `string` | Gets or sets the page size description text. |
| PageTitleFormat | `string` | Gets or sets the pager's numeric page number buttons' title attributes. |
| PagingSummaryFormat | `string` | Gets or sets the pager summary format. has preference over this property. |
| PagingSummaryTemplate | `RenderFragment<PagingInformation>?` | Gets or sets the pager summary template. Has preference over . |
| PrevPageAriaLabel | `string` | Gets or sets the pager's previous page button's aria-label attribute. |
| PrevPageLabel | `string?` | Gets or sets the pager's optional previous page button's label text. |
| PrevPageTitle | `string` | Gets or sets the pager's previous page button's title attribute. |
| ReloadAriaLabel | `string` | Gets or sets the pager's reload button's aria-label attribute. |
| ReloadTitle | `string` | Gets or sets the pager's reload button's title attribute. |
| ShowPagingSummary | `bool` | Gets or sets the pager summary visibility. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the tabindex applied to the currently active pager button. All other pager buttons get tabindex -1 following the roving tabindex pattern. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| PageChanged | `EventCallback<PagerEventArgs>` | Gets or sets the page changed callback. |
| PageReload | `EventCallback` | Gets or sets the reload callback. |
| PageSizeChanged | `EventCallback<int>` | Gets or sets the page size changed callback. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| FirstPage(bool forceReload) | `Task` | Goes to first page. |
| GoToPage(int page, bool forceReload) | `Task` | Goes to specified page. |
| LastPage() | `Task` | Goes to last page. |
| NextPage() | `Task` | Goes to next page. |
| PrevPage() | `Task` | Goes to previous page. |
| Reload() | `Task` | Reloads this instance. |

