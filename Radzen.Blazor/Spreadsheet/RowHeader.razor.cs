
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Renders a row header in a spreadsheet.
/// </summary>
public partial class RowHeader : CellBase, IDisposable
{
    /// <summary>
    /// Gets or sets the row index for the row header.
    /// </summary>
    [Parameter]
    public int Row { get; set; }

    /// <summary>
    /// Gets or sets the sheet that contains the row header.
    /// </summary>
    [Parameter]
    public Sheet? Sheet { get; set; }

    private bool active;
    private bool selected;

    private string Class => ClassList.Create("rz-spreadsheet-row-header")
                                     .Add("rz-spreadsheet-frozen-row", FrozenState.HasFlag(FrozenState.Row))
                                     .Add("rz-spreadsheet-frozen-column", FrozenState.HasFlag(FrozenState.Column))
                                     .Add("rz-spreadsheet-header-selected", selected)
                                     .ToString();

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        UpdateState();
    }

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (Sheet != null)
        {
            Sheet.Selection.Changed -= OnSelectionChanged;
        }
        var didRowChange = parameters.TryGetValue<int>(nameof(Row), out var row) && Row != row;

        await base.SetParametersAsync(parameters);

        if (Sheet != null)
        {
            Sheet.Selection.Changed += OnSelectionChanged;
        }

        if (didRowChange)
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

        if (Sheet is not null)
        {
            var address = new RowRef(Row);

            if (Sheet.Selection.IsActive(address) != active)
            {
                active = !active;

                dirty = true;
            }

            if (Sheet.Selection.IsSelected(address) != selected)
            {
                selected = !selected;

                dirty = true;
            }
        }

        return dirty;
    }

    void IDisposable.Dispose()
    {
        if (Sheet != null)
        {
            Sheet.Selection.Changed -= OnSelectionChanged;
        }
    }
}