using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Threading.Tasks;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable
/// <summary>
/// Renders a column header in a spreadsheet.
/// </summary>
public partial class ColumnHeader : CellBase, IDisposable
{
    /// <summary>
    /// Gets or sets the column index of the column header.
    /// </summary>
    [Parameter]
    public int Column { get; set; }

    /// <summary>
    /// Gets or sets the sheet that contains the column header.
    /// </summary>
    [Parameter]
    public Worksheet? Worksheet { get; set; }

    private bool active;
    private bool selected;

    private string Class => ClassList.Create("rz-spreadsheet-column-header")
                                     .Add("rz-spreadsheet-frozen-row", FrozenState.HasFlag(FrozenState.Row))
                                     .Add("rz-spreadsheet-frozen-column", FrozenState.HasFlag(FrozenState.Column))
                                     .Add("rz-spreadsheet-header-active", active)
                                     .Add("rz-spreadsheet-header-selected", selected)
                                     .ToString();

    private string ResizeHandleStyle => $"left: {Rect.Right.ToPx()}; top: {Rect.Top.ToPx()}; height: {Rect.Height.ToPx()};";


    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        UpdateState();
    }

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (Worksheet != null)
        {
            Worksheet.Selection.Changed -= OnSelectionChanged;
        }

        var didColumnChange = parameters.TryGetValue<int>(nameof(Column), out var column) && Column != column;

        await base.SetParametersAsync(parameters);

        if (Worksheet != null)
        {
            Worksheet.Selection.Changed += OnSelectionChanged;
        }

        if (didColumnChange)
        {
            UpdateState();
        }
    }

    private void OnSelectionChanged()
    {
        var dirty = UpdateState();

        if (dirty)
        {
            StateHasChanged();
        }
    }

    private bool UpdateState()
    {
        var dirty = false;

        if (Worksheet is not null)
        {
            var address = new ColumnRef(Column);

            if (Worksheet.Selection.IsActive(address) != active)
            {
                active = !active;

                dirty = true;
            }

            if (Worksheet.Selection.IsSelected(address) != selected)
            {
                selected = !selected;

                dirty = true;
            }
        }

        return dirty;
    }

    void IDisposable.Dispose()
    {
        if (Worksheet != null)
        {
            Worksheet.Selection.Changed -= OnSelectionChanged;
        }
    }
}