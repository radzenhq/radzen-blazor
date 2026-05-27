using System;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

#nullable enable
using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Renders a data validation input prompt near the selected cell.
/// </summary>
public partial class InputPrompt : IDisposable
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

    private readonly EventBinding<Selection> selectionBinding;
    private readonly EventBinding<IVirtualGridContext> contextBinding;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputPrompt"/> class.
    /// </summary>
    public InputPrompt()
    {
        selectionBinding = new EventBinding<Selection>(
            s => s.Changed += OnSelectionChanged,
            s => s.Changed -= OnSelectionChanged);

        contextBinding = new EventBinding<IVirtualGridContext>(
            c => c.Scrolled += OnScroll,
            c => c.Scrolled -= OnScroll);
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        selectionBinding.Bind(Worksheet?.Selection);
        contextBinding.Bind(Context);
    }

    private void OnSelectionChanged()
    {
        visible = false;
        title = null;
        message = null;
        cell = CellRef.Invalid;

        if (Worksheet is not null)
        {
            var address = Worksheet.Selection.Cell;

            if (address != CellRef.Invalid
                && !(Worksheet.Cells.TryGet(address.Row, address.Column, out var cellData) && cellData.HasValidationErrors))
            {
                var validators = Worksheet.Validation.GetValidatorsForCell(address);

                foreach (var v in validators)
                {
                    if (v is DataValidationRule rule && rule.ShowInputMessage)
                    {
                        title = rule.InputTitle;
                        message = rule.InputMessage;
                        visible = true;
                        cell = address;
                        Render();
                        break;
                    }
                }
            }
        }

        StateHasChanged();
    }

    private void OnScroll()
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
            return;
        }

        var rect = Context.GetRectangle(cell.Row, cell.Column);
        var left = rect.Left + 4;
        var top = rect.Top + rect.Height + 2;
        style = $"transform: translate({left.ToPx()}, {top.ToPx()});";

        className = ClassList.Create("rz-spreadsheet-input-prompt")
            .Add("rz-spreadsheet-frozen-column", Worksheet is not null && cell.Column < Worksheet.Columns.Frozen)
            .Add("rz-spreadsheet-frozen-row", Worksheet is not null && cell.Row < Worksheet.Rows.Frozen)
            .ToString();
    }

    void IDisposable.Dispose()
    {
        selectionBinding.Dispose();
        contextBinding.Dispose();
    }
}
