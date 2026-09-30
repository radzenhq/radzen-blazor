using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor;

/// <summary>
/// A <see cref="RadzenMarkdownEditor" /> tool which executes a table command at the caret. Enabled only when the caret is inside a table.
/// </summary>
/// <example>
/// <code>
/// &lt;RadzenMarkdownEditor @bind-Value=@markdown&gt;
///   &lt;RadzenMarkdownEditorTableCommandButton TableCommand="@MarkdownEditorCommands.TableRowAfter" Icon="south" Title="Insert row below" /&gt;
/// &lt;/RadzenMarkdownEditor&gt;
/// </code>
/// </example>
public partial class RadzenMarkdownEditorTableCommandButton : RadzenMarkdownEditorButtonBase
{
    /// <summary>
    /// The table command to execute. One of the <c>Table</c> commands of <see cref="MarkdownEditorCommands" />.
    /// </summary>
    [Parameter]
    public string? TableCommand { get; set; }

    /// <summary>
    /// The command value. The alignment (<c>left</c>, <c>center</c>, <c>right</c> or <c>none</c>) for <see cref="MarkdownEditorCommands.TableAlign" />.
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>
    /// The tooltip of the tool.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// The icon of the tool.
    /// </summary>
    [Parameter]
    public string? Icon { get; set; }

    /// <inheritdoc />
    protected override string? CommandName => TableCommand;

    private bool IsDisabled => Editor == null || Editor.Disabled || !Editor.InTable || (TableCommand == MarkdownEditorCommands.TableDeleteRow && Editor.TableRow == 0);

    private bool IsSelected => TableCommand == MarkdownEditorCommands.TableAlign && Editor?.InTable == true && Editor.TableAlignment == Value;

    /// <inheritdoc />
    protected override async Task OnClick()
    {
        if (Editor != null && !string.IsNullOrEmpty(TableCommand))
        {
            await Editor.ExecuteCommandAsync(TableCommand, Value);
        }
    }
}
