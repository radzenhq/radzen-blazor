using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A rich text HTML editor component with WYSIWYG editing, formatting toolbar, image upload, and custom tool support.
    /// RadzenHtmlEditor provides a full-featured editor for creating and editing formatted content with a Microsoft Word-like interface.
    /// Allows users to create rich formatted content without knowing HTML.
    /// Features WYSIWYG (what-you-see-is-what-you-get) visual editing interface, formatting tools (bold, italic, underline, font selection, colors, alignment, lists, links, images),
    /// built-in image upload with configurable upload URL, custom toolbar buttons via RadzenHtmlEditorCustomTool, toggle between visual editing and HTML source code view,
    /// paste filtering to remove unwanted HTML when pasting from other sources, and programmatic execution of formatting commands via ExecuteCommandAsync().
    /// The Value property contains HTML markup. Use UploadUrl to configure where images are uploaded. Add custom tools for domain-specific functionality like inserting templates or special content.
    /// </summary>
    /// <example>
    /// Basic HTML editor:
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@htmlContent Style="height: 400px;" /&gt;
    /// @code {
    ///     string htmlContent = "&lt;p&gt;Enter content here...&lt;/p&gt;";
    /// }
    /// </code>
    /// Editor with image upload:
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@content UploadUrl="api/upload/image" UploadHeaders=@uploadHeaders&gt;
    ///     &lt;RadzenHtmlEditorBold /&gt;
    ///     &lt;RadzenHtmlEditorItalic /&gt;
    ///     &lt;RadzenHtmlEditorUnderline /&gt;
    ///     &lt;RadzenHtmlEditorSeparator /&gt;
    ///     &lt;RadzenHtmlEditorImage /&gt;
    /// &lt;/RadzenHtmlEditor&gt;
    /// </code>
    /// Editor with custom tool:
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html Execute=@OnExecute&gt;
    ///     &lt;RadzenHtmlEditorCustomTool CommandName="InsertDate" Icon="calendar_today" Title="Insert Current Date" /&gt;
    /// &lt;/RadzenHtmlEditor&gt;
    /// @code {
    ///     async Task OnExecute(HtmlEditorExecuteEventArgs args)
    ///     {
    ///         if (args.CommandName == "InsertDate")
    ///         {
    ///             await args.Editor.ExecuteCommandAsync(HtmlEditorCommands.InsertHtml, DateTime.Today.ToLongDateString());
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditor : FormComponent<string>
    {
        [Inject] private ContextMenuService? ContextMenuService { get; set; }
        [Inject] private NotificationService? NotificationService { get; set; }

        /// <summary>
        /// Gets or sets whether to display the formatting toolbar above the editor.
        /// When false, hides the toolbar but editing is still possible. Useful for read-only or simplified views.
        /// </summary>
        /// <value><c>true</c> to show the toolbar; <c>false</c> to hide it. Default is <c>true</c>.</value>
        [Parameter]
        public bool ShowToolbar { get; set; } = true;

        /// <summary>
        /// Gets or sets the editor mode determining whether users see the visual editor or HTML source code.
        /// Design mode shows WYSIWYG editing, Source mode shows raw HTML for advanced users.
        /// </summary>
        /// <value>The editor mode. Default is <see cref="HtmlEditorMode.Design"/>.</value>
        [Parameter]
        public HtmlEditorMode Mode { get; set; } = HtmlEditorMode.Design;

        private HtmlEditorMode mode;

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Specifies custom headers that will be submit during uploads.
        /// </summary>
        [Parameter]
        public IDictionary<string, string>? UploadHeaders { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the source editor should update the value on every keystroke.
        /// When <c>true</c>, typing in the HTML source textarea invokes change immediately instead of on blur.
        /// Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> for immediate updates; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Immediate { get; set; }

        /// <summary>
        /// Gets or sets the input.
        /// </summary>
        /// <value>The input.</value>
        [Parameter]
        public EventCallback<string> Input { get; set; }

        /// <summary>
        /// A callback that will be invoked when the user pastes content in the editor. Commonly used to filter unwanted HTML.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenHtmlEditor @bind-Value=@html Paste=@OnPaste /&gt;
        /// @code {
        ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!";
        ///   void OnPaste(HtmlEditorPasteEventArgs args)
        ///   {
        ///     // Set args.Html to filter unwanted tags.
        ///     args.Html = args.Html.Replace("&lt;br&gt;", "");
        ///   }
        /// </code>
        /// </example>
        [Parameter]
        public EventCallback<HtmlEditorPasteEventArgs> Paste { get; set; }

        /// <summary>
        /// A callback that will be invoked when there is an error during upload.
        /// </summary>
        [Parameter]
        public EventCallback<UploadErrorEventArgs> UploadError { get; set; }

        /// <summary>
        /// Called on upload error.
        /// </summary>
        /// <param name="error">The error.</param>
        [JSInvokable("OnError")]
        public async Task OnError(string error)
        {
            await UploadError.InvokeAsync(new UploadErrorEventArgs { Message = error });
        }

        /// <summary>
        /// A callback that will be invoked when the user executes a command of the editor (e.g. by clicking one of the tools).
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenHtmlEditor Execute=@OnExecute&gt;
        ///   &lt;RadzenHtmlEditorCustomTool CommandName="InsertToday" Icon="today" Title="Insert today" /&gt;
        /// &lt;/RadzenHtmlEditor&gt;
        /// @code {
        ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!";
        ///   async Task OnExecute(HtmlEditorExecuteEventArgs args)
        ///   {
        ///     if (args.CommandName == "InsertToday")
        ///     {
        ///       await args.Editor.ExecuteCommandAsync(HtmlEditorCommands.InsertHtml, DateTime.Today.ToLongDateString());
        ///     }
        ///  }
        /// </code>
        /// </example>
        [Parameter]
        public EventCallback<HtmlEditorExecuteEventArgs> Execute { get; set; }

        /// <summary>
        /// Specifies the title used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableDialogTitleText { get; set; } = "Insert table";

        /// <summary>
        /// Specifies the text of the rows label used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableRowsText { get; set; } = "Rows";

        /// <summary>
        /// Specifies the text of the columns label used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableColumnsText { get; set; } = "Columns";

        /// <summary>
        /// Specifies the text of the width label used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableWidthText { get; set; } = "Width";

        /// <summary>
        /// Specifies the text of the border label used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableBorderText { get; set; } = "Border";

        /// <summary>
        /// Specifies the text of the header row toggle used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableHeaderRowText { get; set; } = "Include header row";

        /// <summary>
        /// Specifies the text of the edit section label used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableEditText { get; set; } = "Edit table";

        /// <summary>
        /// Specifies the text of the insert confirmation button used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableOkText { get; set; } = "OK";

        /// <summary>
        /// Specifies the text of the update confirmation button used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableUpdateText { get; set; } = "Update";

        /// <summary>
        /// Specifies the text of the cancel button used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableCancelText { get; set; } = "Cancel";

        /// <summary>
        /// Specifies the text of the column width label used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableColumnWidthText { get; set; } = "Column width";

        /// <summary>
        /// Specifies the text of the cell background label used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableCellBackgroundText { get; set; } = "Cell background";

        /// <summary>
        /// Specifies the text of the cell padding label used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableCellPaddingText { get; set; } = "Cell padding";

        /// <summary>
        /// Specifies the text of the horizontal cell alignment label used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableCellTextAlignText { get; set; } = "Horizontal align";

        /// <summary>
        /// Specifies the text of the vertical cell alignment label used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableCellVerticalAlignText { get; set; } = "Vertical align";

        /// <summary>
        /// Specifies the text of the cell border label used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableCellBorderText { get; set; } = "Cell border";

        /// <summary>
        /// Specifies the text of the column width in pixels label used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableColumnWidthPxText { get; set; } = "Column width (px)";

        /// <summary>
        /// Specifies the text of the cell padding in pixels label used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableCellPaddingPxText { get; set; } = "Cell padding (px)";

        /// <summary>
        /// Specifies the text of the border style label used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableBorderStyleText { get; set; } = "Border style";

        /// <summary>
        /// Specifies the text of the border width in pixels label used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableBorderWidthPxText { get; set; } = "Border width (px)";

        /// <summary>
        /// Specifies the text of the border color label used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableBorderColorText { get; set; } = "Border color";

        /// <summary>
        /// Specifies the text of the top border toggle used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableBorderTopText { get; set; } = "Top";

        /// <summary>
        /// Specifies the text of the right border toggle used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableBorderRightText { get; set; } = "Right";

        /// <summary>
        /// Specifies the text of the bottom border toggle used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableBorderBottomText { get; set; } = "Bottom";

        /// <summary>
        /// Specifies the text of the left border toggle used by the table dialog.
        /// </summary>
        [Parameter]
        public string TableBorderLeftText { get; set; } = "Left";

        /// <summary>
        /// Specifies the notification summary shown when a table action is blocked.
        /// </summary>
        [Parameter]
        public string TableActionBlockedText { get; set; } = "Table action blocked";

        /// <summary>
        /// Specifies the text of the context menu item which copies the selected table cells.
        /// </summary>
        [Parameter]
        public string TableCopyCellsText { get; set; } = "Copy cells";

        /// <summary>
        /// Specifies the text of the context menu item which pastes copied table cells.
        /// </summary>
        [Parameter]
        public string TablePasteCellsText { get; set; } = "Paste cells";

        /// <summary>
        /// Specifies the text of the context menu item which inserts a row above the current row.
        /// </summary>
        [Parameter]
        public string TableInsertRowAboveText { get; set; } = "Insert row above";

        /// <summary>
        /// Specifies the text of the context menu item which inserts a row below the current row.
        /// </summary>
        [Parameter]
        public string TableInsertRowBelowText { get; set; } = "Insert row below";

        /// <summary>
        /// Specifies the text of the context menu item which inserts a column to the left.
        /// </summary>
        [Parameter]
        public string TableInsertColumnLeftText { get; set; } = "Insert column left";

        /// <summary>
        /// Specifies the text of the context menu item which inserts a column to the right.
        /// </summary>
        [Parameter]
        public string TableInsertColumnRightText { get; set; } = "Insert column right";

        /// <summary>
        /// Specifies the text of the context menu item which deletes the current row.
        /// </summary>
        [Parameter]
        public string TableDeleteRowText { get; set; } = "Delete row";

        /// <summary>
        /// Specifies the text of the context menu item which deletes the current column.
        /// </summary>
        [Parameter]
        public string TableDeleteColumnText { get; set; } = "Delete column";

        /// <summary>
        /// Specifies the text of the context menu item which merges the current cell with the cell to the right.
        /// </summary>
        [Parameter]
        public string TableMergeRightText { get; set; } = "Merge right";

        /// <summary>
        /// Specifies the text of the context menu item which merges the current cell with the cell below.
        /// </summary>
        [Parameter]
        public string TableMergeDownText { get; set; } = "Merge down";

        /// <summary>
        /// Specifies the text of the context menu item which splits the current merged cell.
        /// </summary>
        [Parameter]
        public string TableSplitCellText { get; set; } = "Split cell";

        /// <summary>
        /// Specifies the text of the context menu item which deletes the current table.
        /// </summary>
        [Parameter]
        public string TableDeleteText { get; set; } = "Delete table";

        /// <summary>
        /// Specifies the message shown when a table command requires the caret to be inside a table.
        /// </summary>
        [Parameter]
        public string TableActionRequiresTableText { get; set; } = "Place the caret inside a table to use table actions.";

        /// <summary>
        /// Specifies the message shown when the selected cells do not form a rectangular copy range.
        /// </summary>
        [Parameter]
        public string TableCopyInvalidSelectionText { get; set; } = "Select a rectangular range of table cells before copying.";

        /// <summary>
        /// Specifies the message shown when pasted cells would overlap merged cells or an irregular selection.
        /// </summary>
        [Parameter]
        public string TablePasteBlockedText { get; set; } = "The copied cells cannot be pasted over merged cells or an irregular selection.";

        /// <summary>
        /// Specifies the message shown when the selected cells cannot be merged because they are not rectangular.
        /// </summary>
        [Parameter]
        public string TableMergeInvalidSelectionText { get; set; } = "The selected cells must form a valid rectangular range before they can be merged.";

        /// <summary>
        /// Specifies the URL to which RadzenHtmlEditor will submit files.
        /// </summary>
        [Parameter]
        public string? UploadUrl { get; set; }

        ElementReference ContentEditable { get; set; }
        RadzenTextArea? TextArea { get; set; }

        /// <summary>
        /// Focuses the editor.
        /// </summary>
        public override ValueTask FocusAsync()
        {
            if (mode == HtmlEditorMode.Design)
            {
                return ContentEditable.FocusAsync();
            }
            else
            {
                return TextArea != null ? TextArea.Element.FocusAsync() : ValueTask.CompletedTask;
            }
        }

        /// <summary>
        /// Represents the current state of the toolbar commands and other functionalities within the RadzenHtmlEditor component.
        /// Updated dynamically based on user actions or programmatically invoked commands.
        /// </summary>
        public RadzenHtmlEditorCommandState State { get; set; } = new();

        /// <summary>
        /// Gets the current table selection state.
        /// </summary>
        public HtmlEditorTableSelection TableSelection { get; private set; } = new();

        async Task OnFocus()
        {
            await UpdateCommandState();
        }

        private readonly IDictionary<string, Func<Task>> shortcuts = new Dictionary<string, Func<Task>>();

        /// <summary>
        /// Registers a shortcut for the specified action.
        /// </summary>
        /// <param name="key">The shortcut. Can be combination of keys such as <c>CTRL+B</c>.</param>
        /// <param name="action">The action to execute.</param>
        public void RegisterShortcut(string key, Func<Task> action)
        {
            shortcuts[key] = action;
        }

        /// <summary>
        /// Unregisters the specified shortcut.
        /// </summary>
        /// <param name="key"></param>
        public void UnregisterShortcut(string key)
        {
            shortcuts.Remove(key);
        }

        /// <summary>
        /// Invoked by interop when the RadzenHtmlEditor selection changes.
        /// </summary>
        [JSInvokable]
        public async Task OnSelectionChange()
        {
            await UpdateCommandState();
        }

        /// <summary>
        /// Invoked by interop when the user opens the context menu inside the editor.
        /// </summary>
        /// <param name="clientX">The horizontal mouse position.</param>
        /// <param name="clientY">The vertical mouse position.</param>
        [JSInvokable]
        public async Task OnContextMenu(double clientX, double clientY)
        {
            if (ContextMenuService == null || mode != HtmlEditorMode.Design)
            {
                return;
            }

            await UpdateTableSelectionAsync(fromContextMenu: true);

            if (!TableSelection.InTable)
            {
                return;
            }

            var items = new List<ContextMenuItem>
            {
                new ContextMenuItem { Text = TableCopyCellsText, Value = "copyCells", Icon = "content_copy" },
                new ContextMenuItem { Text = TablePasteCellsText, Value = "pasteCells", Icon = "content_paste" },
                new ContextMenuItem { Text = TableInsertRowAboveText, Value = "addRowBefore", Icon = "north" },
                new ContextMenuItem { Text = TableInsertRowBelowText, Value = "addRowAfter", Icon = "south" },
                new ContextMenuItem { Text = TableInsertColumnLeftText, Value = "addColumnBefore", Icon = "west" },
                new ContextMenuItem { Text = TableInsertColumnRightText, Value = "addColumnAfter", Icon = "east" },
                new ContextMenuItem { Text = TableDeleteRowText, Value = "deleteRow", Icon = "horizontal_rule" },
                new ContextMenuItem { Text = TableDeleteColumnText, Value = "deleteColumn", Icon = "vertical_align_center" },
                new ContextMenuItem { Text = TableMergeRightText, Value = "mergeCellRight", Icon = "merge_type", Disabled = !TableSelection.CanMergeRight },
                new ContextMenuItem { Text = TableMergeDownText, Value = "mergeCellDown", Icon = "merge", Disabled = !TableSelection.CanMergeDown },
                new ContextMenuItem { Text = TableSplitCellText, Value = "splitCell", Icon = "call_split", Disabled = !TableSelection.CanSplit },
                new ContextMenuItem { Text = TableDeleteText, Value = "deleteTable", Icon = "delete", IconColor = "var(--rz-danger)", Disabled = !TableSelection.InTable }
            };

            var args = new MouseEventArgs { ClientX = clientX, ClientY = clientY, Button = 2, Type = "contextmenu" };

            ContextMenuService.Open(args, items, async e =>
            {
                if (e.Value is string command)
                {
                    await RestoreSelectionAsync();
                    await ExecuteTableCommandAsync(command);
                    await UpdateCommandState();
                }
            });
        }

        /// <summary>
        /// Invoked by interop during uploads. Provides the custom headers.
        /// </summary>
        [JSInvokable("GetHeaders")]
        public IDictionary<string, string> GetHeaders()
        {
            return UploadHeaders ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Executes the requested command with the provided value. Check <see cref="HtmlEditorCommands" /> for the list of supported commands.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public async Task ExecuteCommandAsync(string name, string? value = null)
        {
            if (JSRuntime == null) return;
            State = await JSRuntime.InvokeAsync<RadzenHtmlEditorCommandState>("Radzen.execCommand", ContentEditable, name, value);

            await OnExecuteAsync(name);

            if (Html != State.Html)
            {
                Html = State.Html;

                htmlChanged = true;

                await OnChange();
            }
        }

        /// <summary>
        /// Executes the action associated with the specified shortcut. Used internally by RadzenHtmlEditor.
        /// </summary>
        /// <param name="shortcut"></param>
        /// <returns></returns>
        [JSInvokable("ExecuteShortcutAsync")]
        public async Task ExecuteShortcutAsync(string shortcut)
        {
            if (shortcuts.TryGetValue(shortcut, out var action))
            {
                await action();
            }
        }

        /// <summary>
        /// Handles changes to the editor's source content.
        /// </summary>
        /// <param name="html">The updated HTML content.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task SourceChanged(string html)
        {
            if (Html != html)
            {
                Html = html;
                htmlChanged = true;
                sourceChanged = true;
            }
            await OnChange();
            StateHasChanged();
        }

        async Task OnChange()
        {
            if (htmlChanged)
            {
                htmlChanged = false;

                _value = Html;

                await ValueChanged.InvokeAsync(Html);

                if (FieldIdentifier.FieldName != null)
                {
                    EditContext?.NotifyFieldChanged(FieldIdentifier);
                }

                await Change.InvokeAsync(Html);
            }
        }

        internal async Task OnExecuteAsync(string name)
        {
            await Execute.InvokeAsync(new HtmlEditorExecuteEventArgs(this) { CommandName = name });

            StateHasChanged();
        }

        /// <summary>
        /// Saves the current selection. RadzenHtmlEditor will lose its selection when it loses focus. Use this method to persist the current selection.
        /// </summary>
        public async Task SaveSelectionAsync()
        {
            if (JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.saveSelection", ContentEditable);
            }
        }

        /// <summary>
        /// Restores the last saved selection.
        /// </summary>
        public async Task RestoreSelectionAsync()
        {
            if (JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.restoreSelection", ContentEditable);
            }
        }

        /// <summary>
        /// Gets information about the currently selected table, if any.
        /// </summary>
        public ValueTask<HtmlEditorTableSelection> GetTableSelectionAsync()
        {
            if (JSRuntime == null) return ValueTask.FromResult(new HtmlEditorTableSelection());
            return JSRuntime.InvokeAsync<HtmlEditorTableSelection>("Radzen.getTableSelection", ContentEditable);
        }

        /// <summary>
        /// Executes a table command for the currently selected table.
        /// </summary>
        /// <param name="name">The table command name.</param>
        /// <param name="value">Optional table command arguments.</param>
        public async Task ExecuteTableCommandAsync(string name, HtmlEditorTableCommandArgs? value = null)
        {
            if (JSRuntime == null) return;

            State = await JSRuntime.InvokeAsync<RadzenHtmlEditorCommandState>("Radzen.execTableCommand", ContentEditable, name, value);

            if (State?.Success == false && !string.IsNullOrEmpty(State.Message))
            {
                NotificationService?.Notify(NotificationSeverity.Warning, TableActionBlockedText, GetTableCommandMessage(State.Message), 4000);
                await UpdateTableSelectionAsync();
                StateHasChanged();
                return;
            }

            await OnExecuteAsync(name);

            if (State != null && Html != State.Html)
            {
                Html = State.Html;

                htmlChanged = true;

                await OnChange();
            }
        }

        async Task UpdateCommandState()
        {
            if (JSRuntime == null) return;
            State = await JSRuntime.InvokeAsync<RadzenHtmlEditorCommandState>("Radzen.queryCommands", ContentEditable);

            await UpdateTableSelectionAsync();

            StateHasChanged();
        }

        async Task UpdateTableSelectionAsync(bool fromContextMenu = false)
        {
            if (JSRuntime == null)
            {
                TableSelection = new HtmlEditorTableSelection();
                return;
            }

            TableSelection = fromContextMenu
                ? await JSRuntime.InvokeAsync<HtmlEditorTableSelection>("Radzen.getContextTableSelection", ContentEditable)
                : await GetTableSelectionAsync();
        }

        string GetTableCommandMessage(string message)
        {
            return message switch
            {
                "table.action.requiresTable" => TableActionRequiresTableText,
                "table.copy.invalidSelection" => TableCopyInvalidSelectionText,
                "table.paste.blocked" => TablePasteBlockedText,
                "table.merge.invalidSelection" => TableMergeInvalidSelectionText,
                _ => message
            };
        }

        async Task OnBlur()
        {
            await OnChange();
        }

        bool htmlChanged;
        bool sourceChanged;

        bool visibleChanged;
        bool firstRender = true;

        /// <summary>
        /// Retrieves the specified attributes of a selection within the content editable area.
        /// </summary>
        /// <typeparam name="T">The type of attributes to retrieve.</typeparam>
        /// <param name="selector">The CSS selector used to target the element.</param>
        /// <param name="attributes">An array of attribute names to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation, returning the attributes as an object of type T.</returns>
        public ValueTask<T> GetSelectionAttributes<T>(string selector, string[] attributes)
        {
            if (JSRuntime == null) return ValueTask.FromResult<T>(default!);
            return JSRuntime.InvokeAsync<T>("Radzen.selectionAttributes", selector, attributes, ContentEditable);
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            this.firstRender = firstRender;

            if (firstRender || visibleChanged)
            {
                if (Visible && JSRuntime != null)
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.createEditor", ContentEditable, UploadUrl, Paste.HasDelegate, Reference, shortcuts.Keys);
                }
            }

            var requiresUpdate = false;

            if (valueChanged || visibleChanged)
            {
                valueChanged = false;
                visibleChanged = false;

                Html = Value;

                if (Visible)
                {
                    requiresUpdate = true;
                }
            }
            else if (sourceChanged)
            {
                sourceChanged = false;

                if (Visible)
                {
                    requiresUpdate = true;
                }
            }

            if (requiresUpdate && JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.innerHTML", ContentEditable, Html);
            }
        }

        internal void SetMode(HtmlEditorMode value)
        {
            mode = value;

            StateHasChanged();
        }

        /// <summary>
        /// Returns the current mode of the editor.
        /// </summary>
        public HtmlEditorMode GetMode()
        {
            return mode;
        }

        string? Html { get; set; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            Html = Value;
            mode = Mode;

            base.OnInitialized();
        }

        /// <summary>
        /// Invoked via interop when the value of RadzenHtmlEditor changes.
        /// </summary>
        /// <param name="html">The HTML.</param>
        [JSInvokable]
        public void OnChange(string html)
        {
            if (Html != html)
            {
                Html = html;
                htmlChanged = true;
            }
            Input.InvokeAsync(html);
        }

        /// <summary>
        /// Invoked via interop when the user pastes content in RadzenHtmlEditor. Invokes <see cref="Paste" />.
        /// </summary>
        /// <param name="html">The HTML.</param>
        [JSInvokable]
        public async Task<string> OnPaste(string html)
        {
            var args = new HtmlEditorPasteEventArgs { Html = html };

            await Paste.InvokeAsync(args);

            return args.Html;
        }

        bool valueChanged;

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Value), Value))
            {
                valueChanged = Html != parameters.GetValueOrDefault<string>(nameof(Value));
            }

            if (parameters.DidParameterChange(nameof(Mode), Mode))
            {
                mode = Mode;
            }

            visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            await base.SetParametersAsync(parameters);

            if (visibleChanged && !firstRender && !Visible && JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.destroyEditor", ContentEditable);
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-html-editor").ToString();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (Visible && IsJSRuntimeAvailable && JSRuntime != null)
            {
                JSRuntime.InvokeVoid("Radzen.destroyEditor", ContentEditable);
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets or sets the callback which when a file is uploaded.
        /// </summary>
        /// <value>The complete callback.</value>
        [Parameter]
        public EventCallback<UploadCompleteEventArgs> UploadComplete { get; set; }


        internal async Task RaiseUploadComplete(UploadCompleteEventArgs args)
        {
            await UploadComplete.InvokeAsync(args);
        }

        /// <summary>
        /// Invoked by interop when the upload is complete.
        /// </summary>
        [JSInvokable("OnUploadComplete")]
        public async Task OnUploadComplete(string response)
        {
            System.Text.Json.JsonDocument? doc = null;

            if (!string.IsNullOrEmpty(response))
            {
                try
                {
                    doc = System.Text.Json.JsonDocument.Parse(response);
                }
                catch (System.Text.Json.JsonException)
                {
                    //
                }
            }

            await UploadComplete.InvokeAsync(new UploadCompleteEventArgs() { RawResponse = response, JsonResponse = doc });
        }
    }
}
