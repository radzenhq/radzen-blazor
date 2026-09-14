using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Radzen.Blazor;

/// <summary>
/// A Markdown editor component with a toolbar, keyboard shortcuts, and a Design/Source mode switcher.
/// </summary>
/// <remarks>
/// The Markdown text is the value in both modes. Design mode edits a parsed document and writes it back to Markdown after
/// every edit; blocks that were not edited are kept as written. Text typed in Design mode is inserted literally:
/// characters with a Markdown meaning are escaped.
/// </remarks>
/// <example>
/// <code>
/// &lt;RadzenMarkdownEditor @bind-Value=@markdown @bind-Mode=@mode /&gt;
/// @code {
///   string markdown = "# Hello";
///   MarkdownEditorMode mode = MarkdownEditorMode.Design;
/// }
/// </code>
/// </example>
public partial class RadzenMarkdownEditor : FormComponent<string>
{
    [Inject] private DialogService DialogService { get; set; } = null!;

    [Inject] private ContextMenuService? ContextMenuService { get; set; }

    private ElementReference editable;
    private ElementReference textarea;
    private IJSObjectReference? jsRef;
    private readonly Dictionary<string, Func<Task>> shortcuts = new();
    private readonly MarkdownEditorEngine engine = new(null);

    private MarkdownEditorMode mode;
    private bool visibleChanged;
    private bool valueChangedExternally;
    private bool modeChanged;
    private bool initialized;
    private bool lastInsertEndedWithWhitespace = true;
    private bool focusOnModeChange;
    private string? valueAtFocus;
    private (int Start, int End) selection;
    private MarkdownEditorMode selectionMode;

    /// <summary>
    /// Gets or sets the mode of the editor. Two-way bindable.
    /// </summary>
    [Parameter]
    public MarkdownEditorMode Mode { get; set; }

    /// <summary>
    /// A callback invoked when the user switches the mode.
    /// </summary>
    [Parameter]
    public EventCallback<MarkdownEditorMode> ModeChanged { get; set; }

    /// <summary>
    /// Specifies whether the toolbar is shown. Set to <c>true</c> by default.
    /// </summary>
    [Parameter]
    public bool ShowToolbar { get; set; } = true;

    /// <summary>
    /// Custom toolbar content. When set, it replaces the default toolbar tools.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Specifies whether <see cref="Input" /> is raised on every keystroke. Set to <c>false</c> by default.
    /// Unlike <see cref="RadzenTextArea.Immediate" />, <see cref="FormComponent{T}.Value" /> is always updated on input; this only controls whether <see cref="Input" /> is raised per keystroke.
    /// </summary>
    [Parameter]
    public bool Immediate { get; set; }

    /// <summary>
    /// A callback invoked on every keystroke when <see cref="Immediate" /> is <c>true</c>.
    /// </summary>
    [Parameter]
    public EventCallback<string> Input { get; set; }

    /// <summary>
    /// A callback invoked after a command is executed, either by a built-in tool, a shortcut, <see cref="ExecuteCommandAsync" /> or a <see cref="RadzenMarkdownEditorCustomTool" />.
    /// </summary>
    [Parameter]
    public EventCallback<MarkdownEditorExecuteEventArgs> Execute { get; set; }

    /// <summary>
    /// The number of visible text rows of the textarea. Set to <c>10</c> by default.
    /// </summary>
    [Parameter]
    public int Rows { get; set; } = 10;

    private MarkdownEditorToolState toolState = new() { Block = string.Empty };

    internal event Action? ToolStateChanged;

    /// <summary>
    /// Whether the editor's history has a state to undo to.
    /// </summary>
    public bool CanUndo => toolState.CanUndo;

    /// <summary>
    /// Whether the editor's history has a state to redo to.
    /// </summary>
    public bool CanRedo => toolState.CanRedo;

    /// <summary>
    /// Returns whether the current selection has the format of <paramref name="commandName" /> applied.
    /// </summary>
    public bool IsActive(string commandName) => Array.IndexOf(toolState.Formats ?? [], commandName) >= 0;

    /// <summary>
    /// The block at the current selection: <c>p</c> or <c>h1</c> to <c>h6</c>, an empty string when the selection spans several kinds of blocks,
    /// or <c>null</c> when it contains no paragraph or heading at all.
    /// </summary>
    public string? FormatBlock => toolState.Block;

    /// <summary>
    /// The selection: positions in the rendered document in Design mode, offsets in the Markdown text in Source mode.
    /// </summary>
    public (int Start, int End) Selection => selection;

    /// <summary>
    /// Whether the caret is inside a table.
    /// </summary>
    public bool InTable => toolState.TableRow >= 0;

    /// <summary>
    /// The index of the table row at the caret (<c>0</c> is the header row), or <c>-1</c> outside a table.
    /// </summary>
    public int TableRow => toolState.TableRow;

    /// <summary>
    /// The index of the table column at the caret, or <c>-1</c> outside a table.
    /// </summary>
    public int TableColumn => toolState.TableColumn;

    /// <summary>
    /// The alignment of the table column at the caret: <c>none</c>, <c>left</c>, <c>center</c> or <c>right</c>. <c>null</c> outside a table.
    /// </summary>
    public string? TableAlignment => toolState.TableRow >= 0 ? toolState.TableAlignment : null;

    private string UrlText => Localize(nameof(RadzenStrings.MarkdownEditorLink_UrlText));
    private string LinkText => Localize(nameof(RadzenStrings.MarkdownEditorLink_LinkText));
    private string RowsText => Localize(nameof(RadzenStrings.MarkdownEditorTable_RowsText));
    private string ColumnsText => Localize(nameof(RadzenStrings.MarkdownEditorTable_ColumnsText));
    private string ImageUrlText => Localize(nameof(RadzenStrings.MarkdownEditorImage_UrlText));
    private string ImageAltText => Localize(nameof(RadzenStrings.MarkdownEditorImage_AltText));
    private string OkText => Localize(nameof(RadzenStrings.HtmlEditorLink_OkText));
    private string CancelText => Localize(nameof(RadzenStrings.HtmlEditorLink_CancelText));

    private string ContentEditable => Disabled ? "false" : "true";

    /// <inheritdoc />
    protected override string GetComponentCssClass() => GetClassList("rz-markdown-editor").ToString();

    /// <summary>
    /// The mode the editor is currently in.
    /// </summary>
    public MarkdownEditorMode CurrentMode => mode;

    internal async Task SetModeAsync(MarkdownEditorMode value)
    {
        mode = value;
        modeChanged = true;
        focusOnModeChange = true;
        await ModeChanged.InvokeAsync(value);
        ToolStateChanged?.Invoke();
        StateHasChanged();
    }

    private async Task<MarkdownEditorUpdate> PublishAsync(MarkdownEditorUpdate update, int version, bool raiseInput)
    {
        engine.RenderHtml = mode == MarkdownEditorMode.Design;
        update.Version = version;
        selection = (update.SelectionStart, update.SelectionEnd);
        selectionMode = mode;
        toolState = update.State ?? toolState;
        ToolStateChanged?.Invoke();

        if (mode == MarkdownEditorMode.Source)
        {
            update.Html = null;
            update.Segments = null;
        }

        if (Value != engine.Text)
        {
            Value = engine.Text;
            await ValueChanged.InvokeAsync(Value);
            NotifyFieldChanged(Value);

            if (raiseInput && Immediate)
            {
                await Input.InvokeAsync(Value);
            }
        }

        StateHasChanged();

        return update;
    }

    private async Task PushAsync(MarkdownEditorUpdate? update, bool includeText, bool focus = false)
    {
        if (update == null)
        {
            return;
        }

        if (includeText)
        {
            update.Text = engine.Text;
        }

        await PublishAsync(update, await GetVersionAsync(), raiseInput: false);

        if (jsRef != null)
        {
            await jsRef.InvokeVoidAsync("update", update, focus);
        }
    }

    private async Task<int> GetVersionAsync() => jsRef != null ? await jsRef.InvokeAsync<int>("getVersion") : 0;

    /// <summary>
    /// Invoked from JavaScript when text is inserted, replaced or deleted.
    /// </summary>
    [JSInvokable("OnEditAsync")]
    public async Task<MarkdownEditorUpdate?> OnEditAsync(int start, int end, string text, string inputType, int version)
    {
        text ??= string.Empty;
        inputType ??= string.Empty;
        engine.RenderHtml = mode == MarkdownEditorMode.Design;
        string? key = null;
        var merge = false;
        var selected = inputType.EndsWith(":selection", StringComparison.Ordinal);
        inputType = selected ? inputType[..^":selection".Length] : inputType;
        var right = inputType.EndsWith(":right", StringComparison.Ordinal);
        inputType = right ? inputType[..^":right".Length] : inputType;

        switch (inputType)
        {
            case "insertText":
                key = "insert";
                merge = !(lastInsertEndedWithWhitespace && text.Length > 0 && !char.IsWhiteSpace(text[0]));
                lastInsertEndedWithWhitespace = text.Length > 0 && char.IsWhiteSpace(text[^1]);
                break;
            case "deleteContentBackward":
                key = "delete-backward";
                merge = true;
                break;
            case "deleteContentForward":
                key = "delete-forward";
                merge = true;
                break;
            default:
                lastInsertEndedWithWhitespace = true;
                break;
        }

        if (mode == MarkdownEditorMode.Source)
        {
            var caret = start + text.Length;
            return await PublishAsync(engine.Apply(start, end, text, (caret, caret), key, merge), version, raiseInput: true);
        }

        var paragraphs = inputType is "insertFromPaste" or "insertFromDrop";
        var update = text.Length == 0 && inputType.StartsWith("delete", StringComparison.Ordinal)
            ? engine.Delete(start, end, inputType.Contains("Forward", StringComparison.Ordinal), key, merge, selected)
            : engine.InsertText(start, end, text, literal: true, key, merge, paragraphs, selected, right);

        return update == null ? null : await PublishAsync(update, version, raiseInput: true);
    }

    /// <summary>
    /// Invoked from JavaScript when a task list checkbox is clicked in Design mode.
    /// </summary>
    [JSInvokable("OnToggleCheckAsync")]
    public async Task<MarkdownEditorUpdate?> OnToggleCheckAsync(int position, int version)
    {
        lastInsertEndedWithWhitespace = true;
        var update = engine.ToggleCheck(position);
        return update == null ? null : await PublishAsync(update, version, raiseInput: true);
    }

    /// <summary>
    /// Invoked from JavaScript when Enter is pressed in Design mode.
    /// </summary>
    [JSInvokable("OnInsertParagraphAsync")]
    public async Task<MarkdownEditorUpdate?> OnInsertParagraphAsync(int start, int end, int version)
    {
        lastInsertEndedWithWhitespace = true;
        var update = engine.InsertParagraph(start, end);
        return update == null ? null : await PublishAsync(update, version, raiseInput: true);
    }

    /// <summary>
    /// Invoked from JavaScript when Tab is pressed in the last table cell in Design mode.
    /// </summary>
    [JSInvokable("OnInsertRowAsync")]
    public async Task<MarkdownEditorUpdate?> OnInsertRowAsync(int start, int end, int version)
    {
        lastInsertEndedWithWhitespace = true;
        var update = engine.AppendRow(start);
        return update == null ? null : await PublishAsync(update, version, raiseInput: true);
    }

    /// <summary>
    /// Invoked from JavaScript when Tab or Shift+Tab is pressed in a list item in Design mode.
    /// </summary>
    [JSInvokable("OnIndentAsync")]
    public async Task<MarkdownEditorUpdate?> OnIndentAsync(int start, int end, bool outdent, int version)
    {
        lastInsertEndedWithWhitespace = true;
        var update = engine.Indent(start, end, outdent);
        return update == null ? null : await PublishAsync(update, version, raiseInput: true);
    }

    /// <summary>
    /// Invoked from JavaScript when Shift+Enter is pressed in Design mode.
    /// </summary>
    [JSInvokable("OnInsertLineBreakAsync")]
    public async Task<MarkdownEditorUpdate?> OnInsertLineBreakAsync(int start, int end, int version)
    {
        lastInsertEndedWithWhitespace = true;
        var update = engine.InsertLineBreak(start, end);
        return update == null ? null : await PublishAsync(update, version, raiseInput: true);
    }

    /// <summary>
    /// Invoked from JavaScript when the selection changes.
    /// </summary>
    [JSInvokable("OnSelectionAsync")]
    public Task OnSelectionAsync(int start, int end)
    {
        selection = (start, end);
        selectionMode = mode;
        toolState = engine.State(start, end);
        ToolStateChanged?.Invoke();
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked from JavaScript when the context menu is requested inside a table. Opens the table commands.
    /// </summary>
    [JSInvokable("OnContextMenuAsync")]
    public Task OnContextMenuAsync(double clientX, double clientY, int start, int end)
    {
        selection = (start, end);
        selectionMode = mode;
        toolState = engine.State(start, end);
        ToolStateChanged?.Invoke();

        if (ContextMenuService == null || !InTable)
        {
            return Task.CompletedTask;
        }

        var items = new List<ContextMenuItem>
        {
            TableMenuItem(nameof(RadzenStrings.MarkdownEditorTableRowBefore_Title), MarkdownEditorCommands.TableRowBefore, "north"),
            TableMenuItem(nameof(RadzenStrings.MarkdownEditorTableRowAfter_Title), MarkdownEditorCommands.TableRowAfter, "south"),
            TableMenuItem(nameof(RadzenStrings.MarkdownEditorTableColumnBefore_Title), MarkdownEditorCommands.TableColumnBefore, "west"),
            TableMenuItem(nameof(RadzenStrings.MarkdownEditorTableColumnAfter_Title), MarkdownEditorCommands.TableColumnAfter, "east"),
            TableMenuItem(nameof(RadzenStrings.MarkdownEditorTableDeleteRow_Title), MarkdownEditorCommands.TableDeleteRow, "horizontal_rule", disabled: TableRow == 0),
            TableMenuItem(nameof(RadzenStrings.MarkdownEditorTableDeleteColumn_Title), MarkdownEditorCommands.TableDeleteColumn, "vertical_align_center"),
            TableMenuItem(nameof(RadzenStrings.MarkdownEditorTableAlignLeft_Title), MarkdownEditorCommands.TableAlign, "format_align_left", "left"),
            TableMenuItem(nameof(RadzenStrings.MarkdownEditorTableAlignCenter_Title), MarkdownEditorCommands.TableAlign, "format_align_center", "center"),
            TableMenuItem(nameof(RadzenStrings.MarkdownEditorTableAlignRight_Title), MarkdownEditorCommands.TableAlign, "format_align_right", "right"),
            TableMenuItem(nameof(RadzenStrings.MarkdownEditorTableDelete_Title), MarkdownEditorCommands.TableDelete, "delete", iconColor: "var(--rz-danger)")
        };

        var args = new MouseEventArgs { ClientX = clientX, ClientY = clientY, Button = 2, Type = "contextmenu" };

        ContextMenuService.Open(args, items, async e =>
        {
            ContextMenuService.Close();

            if (e.Value is ValueTuple<string, string?> command)
            {
                await ExecuteAsync(command.Item1, command.Item2, (start, end));
            }
        });

        return Task.CompletedTask;
    }

    private ContextMenuItem TableMenuItem(string title, string command, string icon, string? value = null, bool disabled = false, string? iconColor = null) =>
        new() { Text = Localize(title), Value = (command, value), Icon = icon, Disabled = disabled, IconColor = iconColor };

    /// <summary>
    /// Invoked from JavaScript when the undo shortcut is pressed.
    /// </summary>
    [JSInvokable("OnUndoAsync")]
    public async Task<MarkdownEditorUpdate?> OnUndoAsync(int version)
    {
        lastInsertEndedWithWhitespace = true;
        var update = engine.Undo();
        return update == null ? null : await PublishAsync(update, version, raiseInput: true);
    }

    /// <summary>
    /// Invoked from JavaScript when the redo shortcut is pressed.
    /// </summary>
    [JSInvokable("OnRedoAsync")]
    public async Task<MarkdownEditorUpdate?> OnRedoAsync(int version)
    {
        lastInsertEndedWithWhitespace = true;
        var update = engine.Redo();
        return update == null ? null : await PublishAsync(update, version, raiseInput: true);
    }

    /// <summary>
    /// Invoked from JavaScript when the editing surface gains focus.
    /// </summary>
    [JSInvokable("OnFocusAsync")]
    public Task OnFocusAsync()
    {
        valueAtFocus = engine.Text;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Invoked from JavaScript when the editing surface loses focus. Raises <see cref="FormComponent{T}.Change" /> when the text changed.
    /// </summary>
    [JSInvokable("OnBlurAsync")]
    public async Task OnBlurAsync()
    {
        if (valueAtFocus != null && valueAtFocus != engine.Text)
        {
            valueAtFocus = engine.Text;
            await Change.InvokeAsync(engine.Text);
        }
    }

    /// <summary>
    /// Registers a keyboard shortcut. Used by toolbar tools.
    /// </summary>
    public void RegisterShortcut(string key, Func<Task> action) => shortcuts[key] = action;

    /// <summary>
    /// Unregisters a keyboard shortcut. Used by toolbar tools.
    /// </summary>
    public void UnregisterShortcut(string key) => shortcuts.Remove(key);

    /// <summary>
    /// Invoked from JavaScript when a registered shortcut is pressed.
    /// </summary>
    [JSInvokable("ExecuteShortcutAsync")]
    public async Task ExecuteShortcutAsync(string shortcut)
    {
        if (shortcuts.TryGetValue(shortcut, out var action))
        {
            await action();
        }
    }

    /// <summary>
    /// Focuses the editable surface in Design mode, or the textarea in Source mode.
    /// </summary>
    public override ValueTask FocusAsync() => mode == MarkdownEditorMode.Design ? editable.FocusAsync() : textarea.FocusAsync();

    private async Task<(int Start, int End)> GetSelectionAsync()
    {
        if (jsRef != null)
        {
            var range = await jsRef.InvokeAsync<int[]?>("getSelection");

            if (range is { Length: 2 })
            {
                selection = (range[0], range[1]);
            }
        }

        return selection;
    }

    /// <summary>
    /// Executes a command. Built-in commands (see <see cref="MarkdownEditorCommands" />) modify the text; unknown command names only raise <see cref="Execute" />.
    /// </summary>
    /// <param name="name">The command name.</param>
    /// <param name="value">The command value: the URL for <see cref="MarkdownEditorCommands.Link" /> and <see cref="MarkdownEditorCommands.Image" /> (a dialogue is opened when <c>null</c>), the text for <see cref="MarkdownEditorCommands.InsertText" />,
    /// the size as <c>rows x columns</c> for <see cref="MarkdownEditorCommands.InsertTable" /> (a dialogue is opened when <c>null</c>), the alignment for <see cref="MarkdownEditorCommands.TableAlign" />.</param>
    public async Task ExecuteCommandAsync(string name, string? value = null) => await ExecuteAsync(name, value, await GetSelectionAsync());

    private async Task ExecuteAsync(string name, string? value, (int Start, int End) range)
    {
        string? label = null;
        var (start, end) = range;

        if (value == null && name is MarkdownEditorCommands.Link or MarkdownEditorCommands.Image)
        {
            LinkDialogModel model = new();
            string title = Localize(name == MarkdownEditorCommands.Image ? nameof(RadzenStrings.MarkdownEditorImage_Title) : nameof(RadzenStrings.MarkdownEditorLink_Title));
            dynamic? result = await DialogService.OpenAsync(title, LinkDialog(model, name == MarkdownEditorCommands.Image, end > start));

            if (result is not true || string.IsNullOrWhiteSpace(model.Url))
            {
                return;
            }

            value = model.Url;
            label = model.Text;
        }

        if (value == null && name == MarkdownEditorCommands.InsertTable)
        {
            TableDialogModel model = new();
            dynamic? result = await DialogService.OpenAsync(Localize(nameof(RadzenStrings.MarkdownEditorTable_Title)), TableDialog(model));

            if (result is not true)
            {
                return;
            }

            value = FormattableString.Invariant($"{model.Rows}x{model.Columns}");
        }

        lastInsertEndedWithWhitespace = true;

        var update = name switch
        {
            MarkdownEditorCommands.Undo => engine.Undo(),
            MarkdownEditorCommands.Redo => engine.Redo(),
            _ => engine.Command(name, start, end, value, label)
        };

        await PushAsync(update, includeText: true, focus: true);
        await Execute.InvokeAsync(new MarkdownEditorExecuteEventArgs(this) { CommandName = name });
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        mode = Mode;
        engine.Reset(Value);
        base.OnInitialized();
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.DidParameterChange(nameof(Mode), Mode))
        {
            mode = parameters.GetValueOrDefault<MarkdownEditorMode>(nameof(Mode));
            modeChanged = true;
        }

        if (parameters.DidParameterChange(nameof(Value), Value))
        {
            var incoming = parameters.GetValueOrDefault<string?>(nameof(Value));
            valueChangedExternally = MarkdownEditorEngine.Normalize(incoming) != engine.Text;
        }

        visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

        await base.SetParametersAsync(parameters);

        if (visibleChanged && !Visible)
        {
            await DisposeJsAsync();
        }
    }

    private async Task DisposeJsAsync()
    {
        if (jsRef != null)
        {
            var stale = jsRef;
            jsRef = null;
            initialized = false;

            try
            {
                await stale.InvokeVoidAsync("dispose");
                await stale.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    /// <inheritdoc />
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MarkdownEditorToolState))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MarkdownEditorUpdate))]
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        engine.RenderHtml = mode == MarkdownEditorMode.Design;

        if ((firstRender || visibleChanged) && Visible && JSRuntime != null && !initialized)
        {
            initialized = true;
            jsRef = await JSRuntime.InvokeAsync<IJSObjectReference>("Radzen.createMarkdownEditor", editable, textarea, Reference, shortcuts.Keys);
            await PushAsync(engine.Render(0, 0), includeText: true);
        }
        else if (valueChangedExternally || modeChanged)
        {
            if (valueChangedExternally)
            {
                engine.Reset(Value);
            }
            else if (selectionMode != mode)
            {
                selection = mode == MarkdownEditorMode.Source
                    ? (engine.ToSource(selection.Start), engine.ToSource(selection.End))
                    : (engine.ToPosition(selection.Start), engine.ToPosition(selection.End));
                selectionMode = mode;
            }

            engine.RenderHtml = mode == MarkdownEditorMode.Design;
            await PushAsync(engine.Render(selection.Start, selection.End), includeText: true, focus: focusOnModeChange);
        }

        valueChangedExternally = false;
        modeChanged = false;
        focusOnModeChange = false;
        visibleChanged = false;
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();

        if (jsRef != null)
        {
            var stale = jsRef;
            jsRef = null;

            try
            {
                stale.InvokeVoid("dispose");
            }
            catch (JSDisconnectedException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }

            stale.DisposeFireAndForget();
        }

        GC.SuppressFinalize(this);
    }

    private class TableDialogModel
    {
        public int Rows { get; set; } = 3;

        public int Columns { get; set; } = 3;
    }

    private class LinkDialogModel
    {
        public string? Url { get; set; }
        public string? Text { get; set; }
    }
}
