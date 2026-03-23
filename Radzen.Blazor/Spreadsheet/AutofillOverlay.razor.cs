using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Renders the autofill handle and drag preview overlay for a spreadsheet.
/// </summary>
public partial class AutofillOverlay : IDisposable
{
    /// <summary>
    /// Gets or sets the worksheet.
    /// </summary>
    [Parameter]
    public Worksheet Worksheet { get; set; } = default!;

    /// <summary>
    /// Gets or sets the context for the virtual grid.
    /// </summary>
    [Parameter]
    public IVirtualGridContext Context { get; set; } = default!;

    /// <summary>
    /// Gets or sets the autofill preview range. Null when no drag is in progress.
    /// </summary>
    [Parameter]
    public RangeRef? AutofillRange { get; set; }

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (Worksheet is not null)
        {
            Worksheet.Selection.Changed -= OnSelectionChanged;
        }

        await base.SetParametersAsync(parameters);

        if (Worksheet is not null)
        {
            Worksheet.Selection.Changed += OnSelectionChanged;
        }
    }

    private void OnSelectionChanged()
    {
        StateHasChanged();
    }

    private static string HandleClass(bool frozenRow, bool frozenColumn)
    {
        return ClassList.Create("rz-spreadsheet-autofill-handle")
            .Add("rz-spreadsheet-frozen-row", frozenRow)
            .Add("rz-spreadsheet-frozen-column", frozenColumn)
            .ToString();
    }

    void IDisposable.Dispose()
    {
        if (Worksheet is not null)
        {
            Worksheet.Selection.Changed -= OnSelectionChanged;
        }
    }
}
