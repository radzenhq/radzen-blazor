using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Radzen.Blazor;

/// <summary>
/// A <see cref="RadzenMarkdownEditor" /> tool which makes the selected paragraphs headings of a chosen level or normal text.
/// </summary>
/// <example>
/// <code>
/// &lt;RadzenMarkdownEditor @bind-Value=@markdown&gt;
///   &lt;RadzenMarkdownEditorFormatBlock /&gt;
/// &lt;/RadzenMarkdownEditor&gt;
/// </code>
/// </example>
public partial class RadzenMarkdownEditorFormatBlock : ComponentBase
{
    /// <summary>
    /// The <see cref="RadzenMarkdownEditor" /> this tool belongs to.
    /// </summary>
    [CascadingParameter]
    public RadzenMarkdownEditor? Editor { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private Localizer? localizer;

    private Localizer Localizer => localizer ??= Services.GetService<Localizer>() ?? Localizer.Default;

    private string Localize(string key) => Localizer.Get(key, Editor?.UICulture ?? CultureInfo.CurrentUICulture);

    private string? placeholder;

    /// <summary>
    /// The placeholder displayed when no paragraph or heading is selected. Set to <c>"Format block"</c> by default.
    /// </summary>
    [Parameter]
    public string? Placeholder { get => placeholder ?? Localize(nameof(RadzenStrings.HtmlEditorFormatBlock_Placeholder)); set => placeholder = value; }

    private string? title;

    /// <summary>
    /// The tooltip of the tool. Set to <c>"Text style"</c> by default.
    /// </summary>
    [Parameter]
    public string? Title { get => title ?? Localize(nameof(RadzenStrings.HtmlEditorFormatBlock_Title)); set => title = value; }

    private string? normalText;

    /// <summary>
    /// The text of the normal paragraph item. Set to <c>"Normal"</c> by default.
    /// </summary>
    [Parameter]
    public string? NormalText { get => normalText ?? Localize(nameof(RadzenStrings.HtmlEditorFormatBlock_NormalText)); set => normalText = value; }

    private string? heading1Text;

    /// <summary>
    /// The text of the level 1 heading item. Set to <c>"Heading 1"</c> by default.
    /// </summary>
    [Parameter]
    public string? Heading1Text { get => heading1Text ?? Localize(nameof(RadzenStrings.HtmlEditorFormatBlock_Heading1Text)); set => heading1Text = value; }

    private string? heading2Text;

    /// <summary>
    /// The text of the level 2 heading item. Set to <c>"Heading 2"</c> by default.
    /// </summary>
    [Parameter]
    public string? Heading2Text { get => heading2Text ?? Localize(nameof(RadzenStrings.HtmlEditorFormatBlock_Heading2Text)); set => heading2Text = value; }

    private string? heading3Text;

    /// <summary>
    /// The text of the level 3 heading item. Set to <c>"Heading 3"</c> by default.
    /// </summary>
    [Parameter]
    public string? Heading3Text { get => heading3Text ?? Localize(nameof(RadzenStrings.HtmlEditorFormatBlock_Heading3Text)); set => heading3Text = value; }

    private string? heading4Text;

    /// <summary>
    /// The text of the level 4 heading item. Set to <c>"Heading 4"</c> by default.
    /// </summary>
    [Parameter]
    public string? Heading4Text { get => heading4Text ?? Localize(nameof(RadzenStrings.HtmlEditorFormatBlock_Heading4Text)); set => heading4Text = value; }

    private string? heading5Text;

    /// <summary>
    /// The text of the level 5 heading item. Set to <c>"Heading 5"</c> by default.
    /// </summary>
    [Parameter]
    public string? Heading5Text { get => heading5Text ?? Localize(nameof(RadzenStrings.HtmlEditorFormatBlock_Heading5Text)); set => heading5Text = value; }

    private string? heading6Text;

    /// <summary>
    /// The text of the level 6 heading item. Set to <c>"Heading 6"</c> by default.
    /// </summary>
    [Parameter]
    public string? Heading6Text { get => heading6Text ?? Localize(nameof(RadzenStrings.HtmlEditorFormatBlock_Heading6Text)); set => heading6Text = value; }

    private async Task OnChange(string value)
    {
        if (Editor != null)
        {
            await Editor.ExecuteCommandAsync(MarkdownEditorCommands.FormatBlock, value);
        }
    }
}
