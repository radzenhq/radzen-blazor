using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Radzen.Blazor;

/// <summary>
/// A markdown editor component with a toolbar, keyboard shortcuts and a live preview rendered by <see cref="RadzenMarkdown" />.
/// </summary>
/// <example>
/// <code>
/// &lt;RadzenMarkdownEditor @bind-Value=@markdown @bind-Mode=@mode /&gt;
/// @code {
///   string markdown = "# Hello";
///   MarkdownEditorMode mode = MarkdownEditorMode.Split;
/// }
/// </code>
/// </example>
public partial class RadzenMarkdownEditor : FormComponent<string>
{
    [Inject]
    DialogService DialogService { get; set; } = default!;

    ElementReference textarea;
    IJSObjectReference? jsRef;
    readonly Dictionary<string, Func<Task>> shortcuts = new();

    /// <summary>
    /// Gets or sets the mode of the editor. Two-way bindable.
    /// </summary>
    [Parameter]
    public MarkdownEditorMode Mode { get; set; } = MarkdownEditorMode.Edit;

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
    /// Custom toolbar content. When set it replaces the default toolbar tools.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Specifies whether <see cref="Input" /> is raised on every keystroke. Set to <c>false</c> by default.
    /// </summary>
    [Parameter]
    public bool Immediate { get; set; }

    /// <summary>
    /// A callback invoked on every keystroke when <see cref="Immediate" /> is <c>true</c>.
    /// </summary>
    [Parameter]
    public EventCallback<string> Input { get; set; }

    /// <summary>
    /// A callback invoked after a command is executed, either by a built-in tool, a shortcut, <see cref="ExecuteCommandAsync" /> or a <c>RadzenMarkdownEditorCustomTool</c>.
    /// </summary>
    [Parameter]
    public EventCallback<MarkdownEditorExecuteEventArgs> Execute { get; set; }

    /// <summary>
    /// The number of visible text rows of the textarea. Set to <c>10</c> by default.
    /// </summary>
    [Parameter]
    public int Rows { get; set; } = 10;

    /// <summary>
    /// Specifies whether HTML in the markdown is rendered in the preview. Forwarded to <see cref="RadzenMarkdown.AllowHtml" />.
    /// </summary>
    [Parameter]
    public bool AllowHtml { get; set; } = true;

    /// <summary>
    /// Forwarded to <see cref="RadzenMarkdown.AllowedHtmlTags" />.
    /// </summary>
    [Parameter]
    public IEnumerable<string>? AllowedHtmlTags { get; set; }

    /// <summary>
    /// Forwarded to <see cref="RadzenMarkdown.AllowedHtmlAttributes" />.
    /// </summary>
    [Parameter]
    public IEnumerable<string>? AllowedHtmlAttributes { get; set; }

    string WriteText => Localize(nameof(RadzenStrings.MarkdownEditor_WriteText));
    string PreviewText => Localize(nameof(RadzenStrings.MarkdownEditor_PreviewText));
    string SplitText => Localize(nameof(RadzenStrings.MarkdownEditor_SplitText));
    string NothingToPreviewText => Localize(nameof(RadzenStrings.MarkdownEditor_NothingToPreviewText));
    string UrlText => Localize(nameof(RadzenStrings.MarkdownEditorLink_UrlText));
    string LinkText => Localize(nameof(RadzenStrings.MarkdownEditorLink_LinkText));
    string ImageUrlText => Localize(nameof(RadzenStrings.MarkdownEditorImage_UrlText));
    string ImageAltText => Localize(nameof(RadzenStrings.MarkdownEditorImage_AltText));
    string OkText => Localize(nameof(RadzenStrings.HtmlEditorLink_OkText));
    string CancelText => Localize(nameof(RadzenStrings.HtmlEditorLink_CancelText));

    string ContentClass => Mode == MarkdownEditorMode.Split
        ? "rz-markdown-editor-content rz-markdown-editor-content-split"
        : "rz-markdown-editor-content";

    /// <inheritdoc />
    protected override string GetComponentCssClass() => GetClassList("rz-markdown-editor").ToString();

    async Task SetModeAsync(MarkdownEditorMode mode)
    {
        Mode = mode;
        await ModeChanged.InvokeAsync(mode);
    }

    async Task OnInput(ChangeEventArgs args)
    {
        var newValue = $"{args.Value}";
        Value = newValue;
        await ValueChanged.InvokeAsync(newValue);
        NotifyFieldChanged(newValue);

        if (Immediate)
        {
            await Input.InvokeAsync(newValue);
        }
    }

    async Task OnChange(ChangeEventArgs args)
    {
        await Change.InvokeAsync($"{args.Value}");
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
    /// Focuses the textarea.
    /// </summary>
    public override ValueTask FocusAsync() => textarea.FocusAsync();

    /// <summary>
    /// Executes a command. Built-in commands (see <see cref="MarkdownEditorCommands" />) modify the text; unknown command names only raise <see cref="Execute" />.
    /// </summary>
    /// <param name="name">The command name.</param>
    /// <param name="value">The command value: the URL for <see cref="MarkdownEditorCommands.Link" /> and <see cref="MarkdownEditorCommands.Image" /> (a dialog is opened when <c>null</c>), the text for <see cref="MarkdownEditorCommands.InsertText" />.</param>
    public async Task ExecuteCommandAsync(string name, string? value = null)
    {
        var (start, end) = await GetSelectionAsync();
        string? label = null;

        if (value == null && name is MarkdownEditorCommands.Link or MarkdownEditorCommands.Image)
        {
            var model = new LinkDialogModel();
            var title = Localize(name == MarkdownEditorCommands.Image ? nameof(RadzenStrings.MarkdownEditorImage_Title) : nameof(RadzenStrings.MarkdownEditorLink_Title));
            var result = await DialogService.OpenAsync(title, LinkDialog(model, name == MarkdownEditorCommands.Image, end > start));

            if (result is not true || string.IsNullOrWhiteSpace(model.Url))
            {
                return;
            }

            value = model.Url;
            label = model.Text;
        }

        var edit = MarkdownFormatter.Apply(Value ?? string.Empty, start, end, name, value, label);

        if (edit is { } e && JSRuntime != null)
        {
            await JSRuntime.InvokeVoidAsync("Radzen.markdownEditorApply", textarea, e.Start, e.End, e.Replacement, e.SelectionStart, e.SelectionEnd);
        }

        await Execute.InvokeAsync(new MarkdownEditorExecuteEventArgs(this) { CommandName = name });
    }

    async Task<(int Start, int End)> GetSelectionAsync()
    {
        var length = Value?.Length ?? 0;

        if (JSRuntime == null)
        {
            return (length, length);
        }

        var range = await JSRuntime.InvokeAsync<int[]?>("Radzen.getSelectionRange", textarea);

        return range is { Length: 2 }
            ? (Math.Clamp(range[0], 0, length), Math.Clamp(range[1], 0, length))
            : (length, length);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender && Visible && JSRuntime != null)
        {
            jsRef = await JSRuntime.InvokeAsync<IJSObjectReference>("Radzen.createMarkdownEditor", textarea, Reference, shortcuts.Keys);
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();

        if (jsRef != null)
        {
            jsRef.InvokeVoid("dispose");
            jsRef.DisposeFireAndForget();
            jsRef = null;
        }

        GC.SuppressFinalize(this);
    }

    class LinkDialogModel
    {
        public string? Url { get; set; }
        public string? Text { get; set; }
    }
}
