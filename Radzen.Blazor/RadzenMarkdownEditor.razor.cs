using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Documents.Markdown;

namespace Radzen.Blazor;

/// <summary>
/// A Markdown editor component with a toolbar, keyboard shortcuts, and a Design/Source mode switcher.
/// </summary>
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

    private ElementReference editable;
    private ElementReference textarea;
    private IJSObjectReference? jsRef;
    private readonly Dictionary<string, Func<Task>> shortcuts = new();

    private MarkdownEditorMode mode;
    private bool visibleChanged;
    private int jsRefVersion;

    /// <summary>
    /// Gets or sets the mode of the editor. Two-way bindable.
    /// </summary>
    [Parameter]
    public MarkdownEditorMode Mode { get; set; } = MarkdownEditorMode.Design;

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

    private string DesignText => Localize(nameof(RadzenStrings.MarkdownEditor_DesignText));
    private string SourceText => Localize(nameof(RadzenStrings.MarkdownEditor_SourceText));
    private string UrlText => Localize(nameof(RadzenStrings.MarkdownEditorLink_UrlText));
    private string LinkText => Localize(nameof(RadzenStrings.MarkdownEditorLink_LinkText));
    private string ImageUrlText => Localize(nameof(RadzenStrings.MarkdownEditorImage_UrlText));
    private string ImageAltText => Localize(nameof(RadzenStrings.MarkdownEditorImage_AltText));
    private string OkText => Localize(nameof(RadzenStrings.HtmlEditorLink_OkText));
    private string CancelText => Localize(nameof(RadzenStrings.HtmlEditorLink_CancelText));

    /// <inheritdoc />
    protected override string GetComponentCssClass() => GetClassList("rz-markdown-editor").ToString();

    /// <summary>
    /// Returns the current mode of the editor.
    /// </summary>
    public MarkdownEditorMode GetMode() => mode;

    private async Task SetModeAsync(MarkdownEditorMode value)
    {
        mode = value;
        await ModeChanged.InvokeAsync(value);

        if (value == MarkdownEditorMode.Design)
        {
            await SyncDesignContentAsync();
        }
    }

    private string? lastSurfaceValue;
    private bool valueChangedExternally;

    /// <summary>
    /// Invoked from JavaScript when the design surface content changes.
    /// </summary>
    [JSInvokable("OnDesignInputAsync")]
    public async Task OnDesignInputAsync(string markdown)
    {
        lastSurfaceValue = markdown;
        Value = markdown;
        await ValueChanged.InvokeAsync(markdown);
        NotifyFieldChanged(markdown);

        if (Immediate)
        {
            await Input.InvokeAsync(markdown);
        }
    }

    /// <summary>
    /// Invoked from JavaScript when the design surface loses focus.
    /// </summary>
    [JSInvokable("OnDesignChangeAsync")]
    public async Task OnDesignChangeAsync(string markdown)
    {
        await Change.InvokeAsync(markdown);
    }

    private async Task SyncDesignContentAsync()
    {
        if (jsRef != null && mode == MarkdownEditorMode.Design)
        {
            await jsRef.InvokeVoidAsync("setContent", HtmlVisitor.ToHtml(NormalizedValue));
        }
    }

    /// <summary>
    /// Invoked from JavaScript to render markdown as design-surface HTML (paste laundering, undo/redo cross-mode restore).
    /// </summary>
    [JSInvokable("RenderMarkdownAsync")]
    public Task<string> RenderMarkdownAsync(string markdown) => Task.FromResult(HtmlVisitor.ToHtml(markdown ?? string.Empty));

    private async Task OnInputAsync(string? value)
    {
        string newValue = value ?? string.Empty;
        Value = newValue;
        await ValueChanged.InvokeAsync(newValue);
        NotifyFieldChanged(newValue);

        if (Immediate)
        {
            await Input.InvokeAsync(newValue);
        }
    }

    private async Task OnChange(ChangeEventArgs args)
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
    /// Focuses the editable surface in Design mode, or the textarea in Source mode.
    /// </summary>
    public override ValueTask FocusAsync() => mode == MarkdownEditorMode.Design ? editable.FocusAsync() : textarea.FocusAsync();

    /// <summary>
    /// Executes a command. Built-in commands (see <see cref="MarkdownEditorCommands" />) modify the text; unknown command names only raise <see cref="Execute" />.
    /// </summary>
    /// <param name="name">The command name.</param>
    /// <param name="value">The command value: the URL for <see cref="MarkdownEditorCommands.Link" /> and <see cref="MarkdownEditorCommands.Image" /> (a dialog is opened when <c>null</c>), the text for <see cref="MarkdownEditorCommands.InsertText" />.</param>
    public async Task ExecuteCommandAsync(string name, string? value = null)
    {
        (int start, int end) = await GetSelectionAsync();
        string? label = null;

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

        MarkdownEdit? edit = MarkdownFormatter.Apply(NormalizedValue, start, end, name, value, label);

        if (edit is { } e && JSRuntime != null)
        {
            await JSRuntime.InvokeVoidAsync("Radzen.markdownEditorApply", textarea, e.Start, e.End, e.Replacement, e.SelectionStart, e.SelectionEnd);
        }

        await Execute.InvokeAsync(new MarkdownEditorExecuteEventArgs(this) { CommandName = name });
    }

    /// <summary>
    /// <see cref="FormComponent{T}.Value" /> with line endings normalised to <c>\n</c>, matching the offsets
    /// <c>Radzen.getSelectionRange</c> returns for the textarea (the HTML spec normalises the API value to LF).
    /// </summary>
    private string NormalizedValue => (Value ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);

    private async Task<(int Start, int End)> GetSelectionAsync()
    {
        string text = NormalizedValue;
        int length = text.Length;

        if (JSRuntime == null)
        {
            return (length, length);
        }

        int[]? range = await JSRuntime.InvokeAsync<int[]?>("Radzen.getSelectionRange", textarea);

        return range is { Length: 2 }
            ? (Math.Clamp(range[0], 0, length), Math.Clamp(range[1], 0, length))
            : (length, length);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        mode = Mode;

        base.OnInitialized();
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.DidParameterChange(nameof(Mode), Mode))
        {
            mode = parameters.GetValueOrDefault<MarkdownEditorMode>(nameof(Mode));
        }

        if (parameters.DidParameterChange(nameof(Value), Value))
        {
            var incoming = parameters.GetValueOrDefault<string?>(nameof(Value));
            valueChangedExternally = incoming != lastSurfaceValue;
        }

        visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

        await base.SetParametersAsync(parameters);

        if (visibleChanged && !Visible && jsRef != null)
        {
            jsRefVersion++;
            IJSObjectReference stale = jsRef;
            jsRef = null;
            await stale.InvokeVoidAsync("dispose");
            await stale.DisposeAsync();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if ((firstRender || visibleChanged) && Visible && JSRuntime != null)
        {
            int version = ++jsRefVersion;
            IJSObjectReference? stale = jsRef;
            jsRef = null;

            if (stale != null)
            {
                await stale.InvokeVoidAsync("dispose");
                await stale.DisposeAsync();
            }

            if (version == jsRefVersion)
            {
                IJSObjectReference created = await JSRuntime.InvokeAsync<IJSObjectReference>("Radzen.createMarkdownEditor", editable, textarea, Reference, shortcuts.Keys);

                if (version == jsRefVersion)
                {
                    jsRef = created;
                }
                else
                {
                    await created.InvokeVoidAsync("dispose");
                    await created.DisposeAsync();
                }
            }

            await SyncDesignContentAsync();
        }

        if (valueChangedExternally)
        {
            valueChangedExternally = false;
            await SyncDesignContentAsync();
        }

        visibleChanged = false;
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

    private class LinkDialogModel
    {
        public string? Url { get; set; }
        public string? Text { get; set; }
    }
}
