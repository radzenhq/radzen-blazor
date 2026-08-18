# RadzenSpreadsheet API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowAutofill | `bool` | Allows drag-to-fill (autofill) gestures. |
| AllowCellFormatting | `bool` | Allows font, color, alignment, and border formatting commands. |
| AllowCharts | `bool` | Allows inserting, editing, moving, resizing, and deleting charts. |
| AllowClipboard | `bool` | Allows cut, copy, and paste through the system clipboard. Independent of , so view-only users can still copy unless this is set to false. Cut and paste also require . |
| AllowConditionalFormatting | `bool` | Allows adding and clearing conditional formatting rules. |
| AllowDataValidation | `bool` | Allows adding and clearing data-validation rules. |
| AllowEditing | `bool` | Allows direct cell editing (type-to-edit, double-click, paste-into-cell, delete-key, autoaccept). |
| AllowFiltering | `bool` | Allows filter and auto-filter commands and the filter UI affordances. |
| AllowHyperlinks | `bool` | Allows inserting, editing, and following hyperlinks. |
| AllowImages | `bool` | Allows inserting, moving, resizing, and deleting images. |
| AllowMerge | `bool` | Allows cell merge and unmerge commands. |
| AllowResizing | `bool` | Allows row and column resize gestures. |
| AllowSorting | `bool` | Allows single- and multi-key sort commands. |
| AllowTables | `bool` | Allows creating, editing, and removing structured tables. |
| AllowUndoRedo | `bool` | Allows undo and redo of previously executed commands. |
| AriaLabel | `string?` | Gets or sets the accessible label (aria-label) announced for the spreadsheet's role="application" region. Defaults to a localized "Spreadsheet". |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| CellTypes | `Dictionary<string, Spreadsheet.SpreadsheetCellType>?` | Gets or sets the custom cell type definitions. Maps cell type names to their renderer and editor component types. |
| ChildContent | `RenderFragment?` | Replaces the built-in toolsets. When set, the supplied content sits inside the toolbar's slot — each child should be a . Add to keep the contextual "Table Design" toolset. |
| CsvExportOptions | `CsvExportOptions?` | Options applied when the user exports the workbook as CSV. When null, defaults are used (comma separator, UTF-8 with BOM, CRLF line endings, RFC 4180 minimal quoting, active sheet only). |
| CsvImportOptions | `CsvImportOptions?` | Options applied when the user opens a CSV file. When null, defaults are used (comma separator, UTF-8, value and formula auto-detection on). |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| ExportFileName | `string` | The name of the file to export the workbook to when using the export functionality. When the user picks "Save as CSV" the extension is replaced with .csv. |
| ReadOnly | `bool` | When true, the spreadsheet rejects every command that mutates the workbook. The user can still select cells, scroll, and copy. Defaults to false. |
| SelectedSheetIndex | `int` | The zero-based index of the active sheet. Supports two-way binding via @bind-SelectedSheetIndex. Values outside the sheet range are clamped. Setting it selects the matching sheet even when is false; loading a different resets it to the bound value (or 0 when unbound). |
| SelectedToolsetIndex | `int` | The index of the active toolset in the toolbar. Defaults to 1 so the Home toolset is shown first. Supports two-way binding via @bind-SelectedToolsetIndex. |
| ShowFormulaBar | `bool` | When true (the default) the formula bar is rendered between the toolbar and the grid. |
| ShowSheetTabs | `bool` | When true (the default) the sheet tab strip is rendered below the grid. |
| ShowToolbar | `bool` | When true (the default) the toolbar is rendered above the grid. Set to false for kiosk or view-only embeds. |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |
| Workbook | `Workbook?` | The workbook to display in the spreadsheet. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| CommandExecuting | `EventCallback<SpreadsheetCommandEventArgs>` | Fires before a command is pushed onto the undo stack. Call from the handler to veto the command. Fires after , the matching Allow* flag, and the sheet's Protection have already approved the command. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| SelectedSheetIndexChanged | `EventCallback<int>` | Fired when the active sheet changes. Used by @bind-SelectedSheetIndex. |
| SelectedToolsetIndexChanged | `EventCallback<int>` | Fired when the active toolset changes. Used by @bind-SelectedToolsetIndex. |
| WorkbookChanged | `EventCallback<Workbook?>` | Event callback that is invoked when the workbook changes. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| AcceptAsync() | `Task<bool>` | Accepts the current edit in the spreadsheet. |
| ExecuteAsync(ICommand command) | `Task<bool>` |  |
| IsFeatureAllowed(SpreadsheetFeature feature) | `bool` | Returns true when the given feature is enabled. forces every feature off except , which stays governed solely by so view-only users can still copy data unless the host explicitly opts out. |
| LoadWorkbookAsync(Workbook workbook) | `Task` |  |
| OnAutofillPointerDownAsync(PointerEventArgs args) | `void` | Invoked by JS interop when the autofill handle is pressed. |
| OnAutofillPointerMoveAsync(PointerEventArgs args) | `void` | Invoked by JS interop when the pointer moves during an autofill drag. |
| OnAutofillPointerUpAsync(PointerEventArgs args) | `Task` | Invoked by JS interop when the pointer is released after an autofill drag. |
| OnCellContextMenuAsync(CellEventArgs args) | `Task` | Invoked by JS interop when a cell is right-clicked. |
| OnCellDoubleClickAsync(CellEventArgs args) | `Task` | Invoked by JS interop when a cell is double-clicked with the pointer. |
| OnCellPointerDownAsync(CellEventArgs args) | `Task<bool>` | Invoked by JS interop when a cell is clicked with the pointer. |
| OnCellPointerMoveAsync(PointerEventArgs args) | `Task` | Invoked by JS interop when the pointer moves over a cell. |
| OnColumnContextMenuAsync(CellEventArgs args) | `Task` | Invoked by JS interop when a column header is right-clicked. |
| OnColumnPointerDownAsync(CellEventArgs args) | `Task<bool>` | Invoked by JS interop when a column header is clicked with the pointer. |
| OnColumnPointerMoveAsync(PointerEventArgs args) | `Task` | Invoked by JS interop when the pointer moves over a column header. |
| OnColumnResizeDoubleClickAsync(CellEventArgs args) | `Task` | Invoked by JS interop when a column resize handle is double-clicked. Auto fits the column width to its displayed content. |
| OnColumnResizePointerDownAsync(CellEventArgs args) | `Task<bool>` | Invoked by JS interop when the column resize handle is pressed. |
| OnColumnResizePointerMoveAsync(PointerEventArgs args) | `Task` | Invoked by JS interop when the pointer moves while resizing a column. |
| OnColumnResizePointerUpAsync(PointerEventArgs args) | `Task` | Invoked by JS interop when the pointer is released after resizing a column. |
| OnCopyAsync() | `Task` | Invoked by JS interop to copy the current selection to the clipboard. |
| OnDrawingMovePointerDownAsync(PointerEventArgs args) | `Task` | Invoked by JS interop when a drawing body is pressed to start a move. |
| OnDrawingMovePointerMoveAsync(PointerEventArgs args) | `Task` | Invoked by JS interop when the pointer moves while dragging a drawing. |
| OnDrawingMovePointerUpAsync(PointerEventArgs args) | `Task` | Invoked by JS interop when the pointer is released after moving a drawing. |
| OnDrawingResizePointerDownAsync(ImageResizeEventArgs args) | `Task<bool>` | Invoked by JS interop when a drawing resize handle is pressed. |
| OnDrawingResizePointerMoveAsync(PointerEventArgs args) | `Task` | Invoked by JS interop when the pointer moves while resizing a drawing. |
| OnDrawingResizePointerUpAsync(PointerEventArgs args) | `Task` | Invoked by JS interop when the pointer is released after resizing a drawing. |
| OnKeyDownAsync(KeyboardEventArgs args, bool isGridContext) | `Task` | Invoked by JS interop when a key is pressed down. |
| OnPasteAsync(string text) | `Task` | Invoked by JS interop to paste text from the clipboard into the current selection. |
| OnRowContextMenuAsync(CellEventArgs args) | `Task` | Invoked by JS interop when a row header is right-clicked. |
| OnRowPointerDownAsync(CellEventArgs args) | `Task<bool>` | Invoked by JS interop when a row header is clicked with the pointer. |
| OnRowPointerMoveAsync(PointerEventArgs args) | `Task` | Invoked by JS interop when the pointer moves over a row header. |
| OnRowResizePointerDownAsync(CellEventArgs args) | `Task<bool>` | Invoked by JS interop when the row resize handle is pressed. |
| OnRowResizePointerMoveAsync(PointerEventArgs args) | `Task` | Invoked by JS interop when the pointer moves while resizing a row. |
| OnRowResizePointerUpAsync(PointerEventArgs args) | `Task` | Invoked by JS interop when the pointer is released after resizing a row. |
| OnSelectionPointerUpAsync() | `Task` | Invoked by JS interop when the user releases the pointer after a cell, row, or column selection gesture. Fires so subscribers (such as the range picker) can commit the user's pick. |
| Redo() | `void` |  |
| ScrollToAsync(CellRef address) | `Task` | Scrolls the spreadsheet to the specified cell address. |
| Undo() | `void` |  |

