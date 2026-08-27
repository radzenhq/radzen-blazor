using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor;

/// <summary>
/// A <see cref="RadzenMarkdownEditor" /> tool which inserts a link.
/// </summary>
public partial class RadzenMarkdownEditorLink : RadzenMarkdownEditorButtonBase
{
    /// <inheritdoc />
    protected override string CommandName => MarkdownEditorCommands.Link;

    private string? title;

    /// <summary>
    /// The tooltip of the tool. Localized by default.
    /// </summary>
    [Parameter]
    public string Title { get => title ?? Localize(nameof(RadzenStrings.MarkdownEditorLink_Title)); set => title = value; }

    /// <inheritdoc />
    [Parameter]
    public override string? Shortcut { get; set; } = "Ctrl+K";
}
