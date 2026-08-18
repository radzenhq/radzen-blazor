# RadzenHtmlEditor API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| ChildContent | `RenderFragment?` | Gets or sets the child content. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| Disabled | `bool` | Gets or sets a value indicating whether this is disabled. |
| FieldIdentifier | `FieldIdentifier` | Gets the field identifier. |
| Immediate | `bool` | Gets or sets a value indicating whether the source editor should update the value on every keystroke. When true, typing in the HTML source textarea invokes change immediately instead of on blur. Set to false by default. |
| Mode | `HtmlEditorMode` | Gets or sets the editor mode determining whether users see the visual editor or HTML source code. Design mode shows WYSIWYG editing, Source mode shows raw HTML for advanced users. |
| Name | `string?` | Gets or sets the unique name identifier for this form component. Used for validation association (linking with validators and labels) and for identifying the field in form submission. This name should be unique within the form and match the Component property of associated validators/labels. |
| Placeholder | `string?` | Gets or sets the placeholder. |
| ShowToolbar | `bool` | Gets or sets whether to display the formatting toolbar above the editor. When false, hides the toolbar but editing is still possible. Useful for read-only or simplified views. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabIndex | `int` | Gets or sets the tab order index for keyboard navigation. Controls the order in which fields receive focus when the user presses the Tab key. Lower values receive focus first. Use -1 to exclude from tab navigation. |
| TableStrings | `HtmlEditorTableStrings` | Gets or sets localizable strings used by the table tools. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| UploadHeaders | `IDictionary<string, string>?` | Specifies custom headers that will be submit during uploads. |
| UploadUrl | `string?` | Specifies the URL to which RadzenHtmlEditor will submit files. |
| Value | `T?` | Gets or sets the value. |
| ValueExpression | `Expression<Func<T>>?` | Gets or sets the value expression. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<T>` | Gets or sets the change. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| Execute | `EventCallback<HtmlEditorExecuteEventArgs>` | A callback that will be invoked when the user executes a command of the editor (e.g. by clicking one of the tools). |
| Input | `EventCallback<string>` | Gets or sets the input. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| Paste | `EventCallback<HtmlEditorPasteEventArgs>` | A callback that will be invoked when the user pastes content in the editor. Commonly used to filter unwanted HTML. |
| UploadComplete | `EventCallback<UploadCompleteEventArgs>` | Gets or sets the callback which when a file is uploaded. |
| UploadError | `EventCallback<UploadErrorEventArgs>` | A callback that will be invoked when there is an error during upload. |
| ValueChanged | `EventCallback<T>` | Gets or sets the value changed. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| ExecuteCommandAsync(string name, string? value) | `Task` | Executes the requested command with the provided value. Check for the list of supported commands. |
| ExecuteShortcutAsync(string shortcut) | `Task` | Executes the action associated with the specified shortcut. Used internally by RadzenHtmlEditor. |
| ExecuteTableCommandAsync(string name, HtmlEditorTableCommandArgs? value) | `Task` | Executes a table command for the currently selected table. |
| GetHeaders() | `IDictionary<string, string>` | Invoked by interop during uploads. Provides the custom headers. |
| GetMode() | `HtmlEditorMode` | Returns the current mode of the editor. |
| GetSelectionAttributes(string selector, string[] attributes) | `ValueTask<T>` | Retrieves the specified attributes of a selection within the content editable area. |
| GetTableSelectionAsync() | `ValueTask<HtmlEditorTableSelection>` | Gets information about the currently selected table, if any. |
| OnChange(string html) | `void` | Invoked via interop when the value of RadzenHtmlEditor changes. |
| OnContextMenu(double clientX, double clientY) | `Task` | Invoked by interop when the user opens the context menu inside the editor. |
| OnError(string error) | `Task` | Called on upload error. |
| OnPaste(string html) | `Task<string>` | Invoked via interop when the user pastes content in RadzenHtmlEditor. Invokes . |
| OnSelectionChange() | `Task` | Invoked by interop when the RadzenHtmlEditor selection changes. |
| OnUploadComplete(string response) | `Task` | Invoked by interop when the upload is complete. |
| RegisterShortcut(string key, Func<Task> action) | `void` | Registers a shortcut for the specified action. |
| RestoreSelectionAsync() | `Task` | Restores the last saved selection. |
| SaveSelectionAsync() | `Task` | Saves the current selection. RadzenHtmlEditor will lose its selection when it loses focus. Use this method to persist the current selection. |
| UnregisterShortcut(string key) | `void` | Unregisters the specified shortcut. |

