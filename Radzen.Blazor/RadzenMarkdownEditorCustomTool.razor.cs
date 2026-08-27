using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor;

/// <summary>
/// A custom tool in <see cref="RadzenMarkdownEditor" />. Either renders a button which raises <see cref="RadzenMarkdownEditor.Execute" />
/// with <see cref="CommandName" />, or arbitrary content via <see cref="Template" />.
/// </summary>
/// <example>
/// <code>
/// &lt;RadzenMarkdownEditor @bind-Value=@markdown Execute=@OnExecute&gt;
///   &lt;RadzenMarkdownEditorCustomTool CommandName="InsertToday" Icon="today" Title="Insert today" /&gt;
/// &lt;/RadzenMarkdownEditor&gt;
/// @code {
///   async Task OnExecute(MarkdownEditorExecuteEventArgs args)
///   {
///     if (args.CommandName == "InsertToday")
///     {
///       await args.Editor.ExecuteCommandAsync(MarkdownEditorCommands.InsertText, DateTime.Today.ToLongDateString());
///     }
///   }
/// }
/// </code>
/// </example>
public partial class RadzenMarkdownEditorCustomTool
{
    /// <summary>Determines if the tool is visible.</summary>
    [Parameter]
    public bool Visible { get; set; } = true;

    /// <summary>The icon of the tool. Set to <c>"settings"</c> by default.</summary>
    [Parameter]
    public string Icon { get; set; } = "settings";

    /// <summary>The icon color.</summary>
    [Parameter]
    public string? IconColor { get; set; }

    /// <summary>Custom content rendered instead of the button. Receives the editor.</summary>
    [Parameter]
    public RenderFragment<RadzenMarkdownEditor>? Template { get; set; }

    /// <summary>Specifies whether the tool is rendered as selected.</summary>
    [Parameter]
    public bool Selected { get; set; }

    /// <summary>Specifies whether the tool is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The command name passed to <see cref="RadzenMarkdownEditor.Execute" /> when the tool is clicked.</summary>
    [Parameter]
    public string? CommandName { get; set; }

    /// <summary>The editor this tool belongs to.</summary>
    [CascadingParameter]
    public RadzenMarkdownEditor? Editor { get; set; }

    /// <summary>The tooltip of the tool.</summary>
    [Parameter]
    public string? Title { get; set; }

    async Task OnClick()
    {
        if (Editor != null && CommandName != null)
        {
            await Editor.ExecuteCommandAsync(CommandName);
        }
    }
}
