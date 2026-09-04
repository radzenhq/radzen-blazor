using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor;

/// <summary>
/// A <see cref="RadzenMarkdownEditor" /> tool which converts the selected lines to a bulleted list.
/// </summary>
/// <example>
/// <code>
/// &lt;RadzenMarkdownEditor @bind-Value=@markdown&gt;
///   &lt;RadzenMarkdownEditorUnorderedList /&gt;
/// &lt;/RadzenMarkdownEditor&gt;
/// </code>
/// </example>
public partial class RadzenMarkdownEditorUnorderedList : RadzenMarkdownEditorButtonBase
{
    /// <inheritdoc />
    protected override string CommandName => MarkdownEditorCommands.UnorderedList;

    private string? title;

    /// <summary>
    /// The tooltip of the tool. Localized by default.
    /// </summary>
    [Parameter]
    public string Title { get => title ?? Localize(nameof(RadzenStrings.MarkdownEditorUnorderedList_Title)); set => title = value; }
}
