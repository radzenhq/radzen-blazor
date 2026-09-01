using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor;

/// <summary>
/// A <see cref="RadzenMarkdownEditor" /> tool which restores the previous state from the editor's history.
/// </summary>
/// <example>
/// <code>
/// &lt;RadzenMarkdownEditor @bind-Value=@markdown&gt;
///   &lt;RadzenMarkdownEditorUndo /&gt;
/// &lt;/RadzenMarkdownEditor&gt;
/// </code>
/// </example>
public partial class RadzenMarkdownEditorUndo : RadzenMarkdownEditorButtonBase
{
    /// <inheritdoc />
    protected override string CommandName => MarkdownEditorCommands.Undo;

    private string? title;

    /// <summary>
    /// The tooltip of the tool. Localized by default.
    /// </summary>
    [Parameter]
    public string Title { get => title ?? Localize(nameof(RadzenStrings.MarkdownEditorUndo_Title)); set => title = value; }
}
