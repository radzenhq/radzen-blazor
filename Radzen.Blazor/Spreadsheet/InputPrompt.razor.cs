using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

#nullable enable
using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Renders a data validation input prompt near the selected cell.
/// </summary>
public partial class InputPrompt : ComponentBase
{
    /// <summary>
    /// Gets or sets the sheet.
    /// </summary>
    [Parameter]
    public Worksheet Worksheet { get; set; } = default!;

    /// <summary>
    /// Gets or sets the virtual grid context.
    /// </summary>
    [Parameter]
    public IVirtualGridContext Context { get; set; } = default!;

    private bool visible;
    private CellRef cell = CellRef.Invalid;
    private string? title;
    private string? message;
    private string? style;
    private string? className;

    /// <summary>
    /// Shows the input prompt for the specified cell if it has a data validation rule with an input message.
    /// </summary>
    public void Show(CellRef address)
    {
        var wasVisible = visible;

        visible = false;
        title = null;
        message = null;
        cell = CellRef.Invalid;

        if (Worksheet != null)
        {
            var validators = Worksheet.Validation.GetValidatorsForCell(address);

            foreach (var v in validators)
            {
                if (v is DataValidationRule rule && rule.ShowInputMessage)
                {
                    title = rule.PromptTitle;
                    message = rule.Prompt;
                    visible = true;
                    cell = address;
                    break;
                }
            }
        }

        if (visible || wasVisible)
        {
            Render();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Hides the input prompt.
    /// </summary>
    public void Hide()
    {
        if (visible)
        {
            visible = false;
            cell = CellRef.Invalid;
            StateHasChanged();
        }
    }

    private void Render()
    {
        if (cell == CellRef.Invalid)
        {
            style = "display: none;";
            className = "rz-spreadsheet-input-prompt";
            return;
        }

        var rect = Context.GetRectangle(cell.Row, cell.Column);
        var left = rect.Left + 4;
        var top = rect.Top + rect.Height + 2;
        style = $"transform: translate({left.ToPx()}, {top.ToPx()});";

        className = ClassList.Create("rz-spreadsheet-input-prompt")
            .Add("rz-spreadsheet-frozen-column", Worksheet != null && cell.Column < Worksheet.Columns.Frozen)
            .Add("rz-spreadsheet-frozen-row", Worksheet != null && cell.Row < Worksheet.Rows.Frozen)
            .ToString();
    }
}
