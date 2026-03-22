using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Base class for row and column header components. Manages selection state tracking,
/// event subscription lifecycle, and CSS class generation.
/// Subclasses provide the axis-specific index, address creation, and CSS class name.
/// </summary>
public abstract class HeaderBase : CellBase, IDisposable
{
    /// <summary>
    /// Gets or sets the worksheet that contains this header.
    /// </summary>
    [Parameter]
    public Worksheet? Worksheet { get; set; }

    private bool active;

    /// <summary>
    /// Gets whether the header is in the active selection range.
    /// </summary>
    protected bool Active => active;

    private bool selected;

    /// <summary>
    /// Gets whether the header's entire row/column is selected.
    /// </summary>
    protected bool Selected => selected;

    /// <summary>
    /// Gets the axis index (row or column) for this header.
    /// </summary>
    protected abstract int Index { get; }

    /// <summary>
    /// Gets the parameter name to watch for changes (e.g. "Row" or "Column").
    /// </summary>
    protected abstract string IndexParameterName { get; }

    /// <summary>
    /// Returns whether the selection is active for this header's axis position.
    /// Called only after validating that <paramref name="selection"/> is non-null.
    /// </summary>
    protected abstract bool CheckIsActive(Selection selection);

    /// <summary>
    /// Returns whether the selection covers this header's entire row/column.
    /// Called only after validating that <paramref name="selection"/> is non-null.
    /// </summary>
    protected abstract bool CheckIsSelected(Selection selection);

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        UpdateState();
    }

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (Worksheet is not null)
        {
            Worksheet.Selection.Changed -= OnSelectionChanged;
        }

        var didIndexChange = parameters.TryGetValue<int>(IndexParameterName, out var index) && Index != index;

        await base.SetParametersAsync(parameters);

        if (Worksheet is not null)
        {
            Worksheet.Selection.Changed += OnSelectionChanged;
        }

        if (didIndexChange)
        {
            UpdateState();
        }
    }

    private void OnSelectionChanged()
    {
        if (UpdateState())
        {
            StateHasChanged();
        }
    }

    private bool UpdateState()
    {
        var dirty = false;

        if (Worksheet is not null)
        {
            var selection = Worksheet.Selection;

            if (CheckIsActive(selection) != active)
            {
                active = !active;
                dirty = true;
            }

            if (CheckIsSelected(selection) != selected)
            {
                selected = !selected;
                dirty = true;
            }
        }

        return dirty;
    }

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
        if (Worksheet is not null)
        {
            Worksheet.Selection.Changed -= OnSelectionChanged;
        }
    }
}
