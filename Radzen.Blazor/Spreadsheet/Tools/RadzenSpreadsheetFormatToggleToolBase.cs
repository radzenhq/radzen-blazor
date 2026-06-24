using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

using Radzen.Blazor.Spreadsheet;
using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor;

#nullable enable

/// <summary>
/// Base class for toggle-style format toolbar buttons (Bold, Italic, Underline, Strikethrough, TextWrap).
/// Subclasses only need to override <see cref="GetValue"/> and <see cref="WithValue"/>.
/// </summary>
public abstract class RadzenSpreadsheetFormatToggleToolBase : ComponentBase, IDisposable
{
    /// <summary>
    /// The active worksheet, cascaded from the host <see cref="Radzen.Blazor.RadzenSpreadsheet"/>.
    /// </summary>
    [CascadingParameter]
    public Worksheet? Worksheet { get; set; }

    /// <summary>
    /// The host spreadsheet, cascaded from <see cref="Radzen.Blazor.RadzenSpreadsheet"/>.
    /// </summary>
    [CascadingParameter]
    public ISpreadsheet? Spreadsheet { get; set; }

    /// <summary>
    /// Reads the boolean property from the cell's format (e.g. <c>format.Bold</c>).
    /// </summary>
    protected abstract bool GetValue(Format format);

    /// <summary>
    /// Returns a new format with the boolean property set (e.g. <c>format.WithBold(value)</c>).
    /// </summary>
    protected abstract Format WithValue(Format format, bool value);

    /// <summary>
    /// Derived state: whether the selected cell's format has this property toggled on.
    /// </summary>
    protected bool IsToggled
    {
        get
        {
            if (Worksheet?.Cells.TryGet(Worksheet.Selection.Cell.Row, Worksheet.Selection.Cell.Column, out var cell) == true)
            {
                return GetValue(cell.Format);
            }
            return false;
        }
    }

    /// <summary>
    /// Dispatches a <see cref="FormatCommand"/> to set the toggle to <paramref name="value"/>.
    /// Used as the <c>ValueChanged</c> callback for the underlying toggle button.
    /// </summary>
    protected async Task OnToggledChangedAsync(bool value)
    {
        if (Worksheet is null || Worksheet.Selection.Cell == CellRef.Invalid || Spreadsheet is null)
        {
            return;
        }

        if (!Worksheet.Cells.TryGet(Worksheet.Selection.Cell.Row, Worksheet.Selection.Cell.Column, out var cell))
        {
            return;
        }

        var newFormat = WithValue(cell.Format, value);
        var command = new FormatCommand(Worksheet, Worksheet.Selection.Range, newFormat);
        await Spreadsheet.ExecuteAsync(command);
    }

    /// <summary>
    /// Gets whether the tool should be disabled.
    /// </summary>
    protected bool IsDisabled
        => Worksheet?.Selection.Cell == CellRef.Invalid
        || Spreadsheet?.IsFeatureAllowed(SpreadsheetFeature.CellFormatting) == false;

    private readonly EventBinding<Selection> selectionBinding;

    /// <summary>
    /// Initializes a new instance of the <see cref="RadzenSpreadsheetFormatToggleToolBase"/> class.
    /// </summary>
    protected RadzenSpreadsheetFormatToggleToolBase()
    {
        selectionBinding = new EventBinding<Selection>(
            s => s.Changed += OnSelectionChanged,
            s => s.Changed -= OnSelectionChanged);
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        selectionBinding.Bind(Worksheet?.Selection);
    }

    private void OnSelectionChanged()
    {
        StateHasChanged();
    }

    void IDisposable.Dispose()
    {
        selectionBinding.Dispose();
    }
}
