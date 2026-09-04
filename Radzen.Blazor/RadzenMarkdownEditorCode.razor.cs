using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor;

/// <summary>
/// A <see cref="RadzenMarkdownEditor" /> tool which wraps the selection in inline code.
/// </summary>
/// <example>
/// <code>
/// &lt;RadzenMarkdownEditor @bind-Value=@markdown&gt;
///   &lt;RadzenMarkdownEditorCode /&gt;
/// &lt;/RadzenMarkdownEditor&gt;
/// </code>
/// </example>
public partial class RadzenMarkdownEditorCode : RadzenMarkdownEditorButtonBase
{
    /// <inheritdoc />
    protected override string CommandName => MarkdownEditorCommands.Code;

    private string? title;

    /// <summary>
    /// The tooltip of the tool. Localized by default.
    /// </summary>
    [Parameter]
    public string Title { get => title ?? Localize(nameof(RadzenStrings.MarkdownEditorCode_Title)); set => title = value; }
}
