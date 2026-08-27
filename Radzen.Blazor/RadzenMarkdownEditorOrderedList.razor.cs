using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor;

/// <summary>
/// A <see cref="RadzenMarkdownEditor" /> tool which converts the selected lines to a numbered list.
/// </summary>
public partial class RadzenMarkdownEditorOrderedList : RadzenMarkdownEditorButtonBase
{
    /// <inheritdoc />
    protected override string CommandName => MarkdownEditorCommands.OrderedList;

    private string? title;

    /// <summary>
    /// The tooltip of the tool. Localized by default.
    /// </summary>
    [Parameter]
    public string Title { get => title ?? Localize(nameof(RadzenStrings.MarkdownEditorOrderedList_Title)); set => title = value; }
}
