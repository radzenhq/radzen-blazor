# RadzenFileInput API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Accept | `string` | Gets or sets the comma-separated accepted MIME types. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChooseText | `string` | Gets or sets the choose button text. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| DeleteText | `string` | Gets or sets the delete button text. |
| Disabled | `bool` | Gets or sets a value indicating whether this is disabled. |
| FieldIdentifier | `FieldIdentifier` | Gets the field identifier. |
| FileName | `string?` | Gets or sets the image file name. |
| FileSize | `long?` | Gets or sets the image file size. |
| ImageAlternateText | `string` | Gets or sets the text. |
| ImageStyle | `string` | Gets or sets the image style. |
| InputAttributes | `IReadOnlyDictionary<string, object>?` | Specifies additional custom attributes that will be rendered by the input. |
| MaxFileSize | `int` | Gets or sets the maximum size of the file. |
| MaxHeight | `int` | Gets or sets the maximum height of the file, keeping aspect ratio. |
| MaxWidth | `int` | Gets or sets the maximum width of the file, keeping aspect ratio. |
| Name | `string?` | Gets or sets the unique name identifier for this form component. Used for validation association (linking with validators and labels) and for identifying the field in form submission. This name should be unique within the form and match the Component property of associated validators/labels. |
| Placeholder | `string?` | Gets or sets the placeholder. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the tab order index for keyboard navigation. Controls the order in which fields receive focus when the user presses the Tab key. Lower values receive focus first. Use -1 to exclude from tab navigation. |
| Title | `string?` | Gets or sets the title. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Value | `T?` | Gets or sets the value. |
| ValueExpression | `Expression<Func<T>>?` | Gets or sets the value expression. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<T>` | Gets or sets the change. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| Error | `EventCallback<UploadErrorEventArgs>` | Gets or sets the error callback. |
| FileNameChanged | `EventCallback<string>` | Gets or sets the FileName changed. |
| FileSizeChanged | `EventCallback<long?>` | Gets or sets the FileSize changed. |
| ImageClick | `EventCallback<MouseEventArgs>` | Gets or sets the image click callback. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| ValueChanged | `EventCallback<T>` | Gets or sets the value changed. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| OnChange(IEnumerable<PreviewFileInfo> files) | `System.Threading.Tasks.Task` | Called on file change. |
| OnImageClick(MouseEventArgs args) | `Task` | Handles the image click event. |

