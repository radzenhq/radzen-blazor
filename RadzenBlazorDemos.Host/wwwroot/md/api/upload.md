# RadzenUpload API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Accept | `string?` | Gets or sets the accepted MIME types. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Auto | `bool` | Gets or sets a value indicating whether this upload is automatic. |
| ChildContent | `RenderFragment?` | Gets or sets the child content. |
| ChooseText | `string` | Gets or sets the choose button text. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| DeleteText | `string` | Gets or sets the choose button text. |
| Disabled | `bool` | Gets or sets a value indicating whether this is disabled. |
| Icon | `string?` | Gets or sets the icon. |
| IconColor | `string?` | Gets or sets the icon color. |
| ImageAlternateText | `string` | Gets or sets the text. |
| InputAttributes | `IReadOnlyDictionary<string, object>?` | Specifies additional custom attributes that will be rendered by the input. |
| MaxFileCount | `int` | Gets or sets the maximum number of files. |
| Method | `string` | Specifies the HTTP method used for uploading files to the defined endpoint. Common values are POST (default) and PUT. If the parameter is not set, this property is ignored. Defaults to POST. |
| Multiple | `bool` | Gets or sets a value indicating whether this is multiple. |
| Name | `string?` | Gets or sets the name. |
| ParameterName | `string?` | Gets or sets the parameter name. If not set 'file' parameter name will be used for single file and 'files' for multiple files. |
| Stream | `bool` | Enables streaming upload mode for large files to the specified . When true, files are uploaded as raw binary streams instead of multipart/form-data. Only a single file can be uploaded at a time in streaming mode. When false (default), files are uploaded as multipart/form-data (standard form upload), and multiple files can be uploaded simultaneously if is enabled. This property is ignored if is not set. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the tab order index for keyboard navigation. Controls the order in which the Choose button receives focus when the user presses the Tab key. Lower values receive focus first. Use -1 to exclude from tab navigation. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Url | `string?` | Gets or sets the URL. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<UploadChangeEventArgs>` | Gets or sets the change callback. |
| Complete | `EventCallback<UploadCompleteEventArgs>` | Gets or sets the complete callback. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| Error | `EventCallback<UploadErrorEventArgs>` | Gets or sets the error callback. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| Progress | `EventCallback<UploadProgressArgs>` | Gets or sets the progress callback. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| ClearFiles() | `System.Threading.Tasks.Task` | Clear selected file(s) from the upload selection |
| CreateUploadChangeEventArgs(IEnumerable<FileInfo> files) | `UploadChangeEventArgs` | Creates the upload change event args. |
| GetHeaders() | `IDictionary<string, string>` | Gets the headers. |
| OnChange(IEnumerable<PreviewFileInfo> files) | `System.Threading.Tasks.Task` | Called on file change. |
| OnComplete(string response, bool cancelled) | `System.Threading.Tasks.Task` | Called when upload is complete. |
| OnError(string error) | `System.Threading.Tasks.Task` | Called on upload error. |
| OnProgress(int progress, long loaded, long total, IEnumerable<FileInfo> files, bool cancel) | `System.Threading.Tasks.Task<bool>` | Called on progress. |
| RemoveFile(string fileName, bool ignoreCase) | `System.Threading.Tasks.Task` | Called on file remove. |
| Upload() | `Task` | Uploads this instance selected files. |

