using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

#nullable enable
using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Renders a validation error popup near the selected cell when it has validation errors.
/// </summary>
public partial class ValidationError : IDisposable
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
    private string? message;
    private string? style;
    private string? className;
    private CellRef cell = CellRef.Invalid;

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (Worksheet is not null)
        {
            Worksheet.Selection.Changed -= OnSelectionChanged;
        }

        if (Context is not null)
        {
            Context.Scrolled -= OnScroll;
        }

        await base.SetParametersAsync(parameters);

        if (Worksheet is not null)
        {
            Worksheet.Selection.Changed += OnSelectionChanged;
        }

        if (Context is not null)
        {
            Context.Scrolled += OnScroll;
        }
    }

    private void OnSelectionChanged()
    {
        visible = false;
        message = null;
        cell = CellRef.Invalid;

        if (Worksheet is not null)
        {
            var address = Worksheet.Selection.Cell;

            if (address != CellRef.Invalid
                && Worksheet.Cells.TryGet(address.Row, address.Column, out var cellData)
                && cellData.HasValidationErrors)
            {
                message = string.Join(Environment.NewLine, cellData.ValidationErrors);
                visible = true;
                cell = address;
                Render();
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

        className = ClassList.Create("rz-spreadsheet-validation-error")
            .Add("rz-spreadsheet-frozen-column", Worksheet is not null && cell.Column < Worksheet.Columns.Frozen)
            .Add("rz-spreadsheet-frozen-row", Worksheet is not null && cell.Row < Worksheet.Rows.Frozen)
            .ToString();
    }

    void IDisposable.Dispose()
    {
        if (Worksheet is not null)
        {
            Worksheet.Selection.Changed -= OnSelectionChanged;
        }

        if (Context is not null)
        {
            Context.Scrolled -= OnScroll;
        }
    }
}
