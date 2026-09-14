using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor;

/// <summary>
/// A <see cref="RadzenMarkdownEditor" /> tool which switches between the Design and Source views.
/// </summary>
/// <example>
/// <code>
/// &lt;RadzenMarkdownEditor @bind-Value=@markdown&gt;
///   &lt;RadzenMarkdownEditorSource /&gt;
/// &lt;/RadzenMarkdownEditor&gt;
/// </code>
/// </example>
public partial class RadzenMarkdownEditorSource : RadzenMarkdownEditorButtonBase
{
    private string? title;

    /// <summary>
    /// The tooltip of the tool. Localized by default.
    /// </summary>
    [Parameter]
    public string Title { get => title ?? Localize(nameof(RadzenStrings.MarkdownEditorSource_Title)); set => title = value; }

    /// <inheritdoc />
    protected override async Task OnClick()
    {
        if (Editor != null)
        {
            await Editor.SetModeAsync(Editor.CurrentMode == MarkdownEditorMode.Design ? MarkdownEditorMode.Source : MarkdownEditorMode.Design);
        }
    }
}
