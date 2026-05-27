using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Represents a formula editor component for a spreadsheet.
/// </summary>
public partial class FormulaEditor : ComponentBase, IDisposable
{
    /// <summary>
    /// Gets or sets the sheet that contains the formula editor.
    /// </summary>
    [Parameter]
    public Worksheet Worksheet { get; set; } = default!;

    /// <summary>
    /// Gets or sets the editor.
    /// </summary>
    [Parameter]
    public Editor Editor { get; set; } = default!;

    /// <summary>
    /// Gets or sets the spreadsheet instance that contains the sheet.
    /// </summary>
    [CascadingParameter]
    public ISpreadsheet Spreadsheet { get; set; } = default!;

    private readonly EventBinding<Selection> selectionBinding;
    private readonly EventBinding<Editor> editorBinding;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormulaEditor"/> class.
    /// </summary>
    public FormulaEditor()
    {
        selectionBinding = new EventBinding<Selection>(
            s => s.Changed += OnSelectionChanged,
            s => s.Changed -= OnSelectionChanged);

        editorBinding = new EventBinding<Editor>(
            e => e.ValueChanged += OnEditorValueChanged,
            e => e.ValueChanged -= OnEditorValueChanged);
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        selectionBinding.Bind(Worksheet?.Selection);
        editorBinding.Bind(Worksheet is not null ? Editor : null);

        if (Worksheet is not null)
        {
            Render();
        }
    }

    private void OnFocus()
    {
        if (Worksheet.Selection.Cell != CellRef.Invalid)
        {
            var cell = Worksheet.Cells[Worksheet.Selection.Cell];
            Editor.StartEdit(cell.Address, Editor.Mode != EditMode.None ? Editor.Value : cell.GetValue(), EditMode.Formula);
        }
    }

    private Cell? boundCell;

    private void Render()
    {
        Cell? cell = Worksheet.Selection.Cell != CellRef.Invalid
            ? Worksheet.Cells[Worksheet.Selection.Cell]
            : null;

        // The formula bar is a view of the active cell, so observe that cell's content
        // (mirroring CellView). This keeps the bar correct when the cell changes for any
        // reason other than the selection moving — an undo/redo restore, an edit applied
        // elsewhere, a formula recalculation — none of which raise Selection.Changed.
        if (!ReferenceEquals(cell, boundCell))
        {
            if (boundCell is not null)
            {
                boundCell.Changed -= OnActiveCellChanged;
            }

            boundCell = cell;

            if (boundCell is not null)
            {
                boundCell.Changed += OnActiveCellChanged;
            }
        }

        if (cell is not null)
        {
            Editor.Value = cell.GetValue();
        }
    }

    private void OnActiveCellChanged(Cell cell)
    {
        // Don't clobber what the user is typing into the bar.
        if (Editor.Mode == EditMode.None)
        {
            Editor.Value = cell.GetValue();

            StateHasChanged();
        }
    }

    private void OnSelectionChanged()
    {
        Render();

        StateHasChanged();
    }

    private void OnEditorValueChanged()
    {
        if (Editor.Mode == EditMode.Cell)
        {
            StateHasChanged();
        }
    }

    void IDisposable.Dispose()
    {
        selectionBinding.Dispose();
        editorBinding.Dispose();

        if (boundCell is not null)
        {
            boundCell.Changed -= OnActiveCellChanged;
        }
    }

    private async Task OnBlurAsync()
    {
        if (Editor.Mode == EditMode.Formula)
        {
            await Spreadsheet.AcceptAsync();
        }
    }
}