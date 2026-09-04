using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor;

/// <summary>
/// A <see cref="RadzenMarkdownEditor" /> tool which converts the selected lines to a task list.
/// Not rendered by <see cref="RadzenMarkdown" />, which does not support GitHub-flavoured strikethrough/task lists; therefore not part of the default toolbar.
/// </summary>
/// <example>
/// <code>
/// &lt;RadzenMarkdownEditor @bind-Value=@markdown&gt;
///   &lt;RadzenMarkdownEditorTaskList /&gt;
/// &lt;/RadzenMarkdownEditor&gt;
/// </code>
/// </example>
public partial class RadzenMarkdownEditorTaskList : RadzenMarkdownEditorButtonBase
{
    /// <inheritdoc />
    protected override string CommandName => MarkdownEditorCommands.TaskList;

    private string? title;

    /// <summary>
    /// The tooltip of the tool. Localized by default.
    /// </summary>
    [Parameter]
    public string Title { get => title ?? Localize(nameof(RadzenStrings.MarkdownEditorTaskList_Title)); set => title = value; }
}
