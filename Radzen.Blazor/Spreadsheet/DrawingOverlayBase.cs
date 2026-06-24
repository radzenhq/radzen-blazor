using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using Radzen.Documents.Spreadsheet;

#nullable enable
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Shared rendering and geometry logic for floating drawings (charts, images) on a worksheet.
/// Provides the outer container, frozen-pane splitting, selection chrome, and 8-direction resize handles;
/// subclasses contribute only the inner content (e.g. SVG chart, <c>img</c> element).
/// </summary>
/// <typeparam name="TDrawing">The drawing model type (e.g. <see cref="SheetChart"/> or <see cref="SheetImage"/>).</typeparam>
public abstract class DrawingOverlayBase<TDrawing> : ComponentBase, IDisposable where TDrawing : class, IAnchoredDrawing
{
    /// <summary>
    /// Gets or sets the worksheet whose drawings are rendered.
    /// </summary>
    [Parameter]
    public Worksheet Worksheet { get; set; } = default!;

    /// <summary>
    /// Gets or sets the virtual grid context used to translate cell ranges to pixel rectangles.
    /// </summary>
    [Parameter]
    public IVirtualGridContext Context { get; set; } = default!;

    /// <summary>Resize handle direction names and the zone-edge predicates that must hold for each to appear.</summary>
    private static readonly (string Direction, Func<RangeInfo, bool> ShouldShow)[] Handles =
    {
        ("nw", z => z.Top && z.Left),
        ("n",  z => z.Top),
        ("ne", z => z.Top && z.Right),
        ("e",  z => z.Right),
        ("se", z => z.Bottom && z.Right),
        ("s",  z => z.Bottom),
        ("sw", z => z.Bottom && z.Left),
        ("w",  z => z.Left),
    };

    /// <summary>The drawings to render.</summary>
    protected abstract IEnumerable<TDrawing> Drawings { get; }

    /// <summary>Whether the supplied drawing is the currently selected one.</summary>
    protected abstract bool IsSelected(TDrawing drawing);

    /// <summary>The base CSS class for this drawing kind, e.g. <c>rz-spreadsheet-chart</c>.</summary>
    protected abstract string BaseCssClass { get; }

    /// <summary>Renders the drawing-specific inner content into the container.</summary>
    protected abstract void RenderInner(RenderTreeBuilder builder, TDrawing drawing, RangeRef range);

    /// <summary>Handles pointer-down on the drawing's outer element. Default selects the drawing and clears the cell selection.</summary>
    protected virtual void OnDrawingPointerDown(PointerEventArgs e, TDrawing drawing)
    {
        SelectDrawing(drawing);
        Worksheet.Selection.Clear();
        StateHasChanged();
    }

    /// <summary>Sets the worksheet's selected drawing to this one.</summary>
    protected abstract void SelectDrawing(TDrawing drawing);

    /// <summary>Repaints this overlay when a drawing of its own kind changes geometry during a drag.</summary>
    protected void OnDrawingGeometryChanged(IAnchoredDrawing drawing)
    {
        if (drawing is TDrawing)
        {
            StateHasChanged();
        }
    }

    /// <summary>Optional context-menu handler. No-op by default.</summary>
    protected virtual void OnDrawingContextMenu(MouseEventArgs e, TDrawing drawing)
    {
    }

    /// <summary>
    /// Whether the outer element should attach a context-menu handler. Off by default;
    /// override to <c>true</c> when <see cref="OnDrawingContextMenu(MouseEventArgs, TDrawing)"/> is implemented.
    /// </summary>
    protected virtual bool HasContextMenu => false;

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        foreach (var drawing in Drawings)
        {
            var range = GetDrawingRange(drawing);
            var selected = IsSelected(drawing);

            foreach (var zone in Context.GetRanges(range))
            {
                BuildZone(builder, drawing, range, zone, selected);
            }
        }
    }

    private void BuildZone(RenderTreeBuilder builder, TDrawing drawing, RangeRef range, RangeInfo zone, bool selected)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", GetClass(zone.FrozenRow, zone.FrozenColumn));
        builder.AddAttribute(2, "style", GetZoneStyle(zone));
        builder.AddAttribute(3, "onpointerdown", EventCallback.Factory.Create<PointerEventArgs>(this, e => OnDrawingPointerDown(e, drawing)));
        builder.AddEventStopPropagationAttribute(4, "onpointerdown", true);

        if (HasContextMenu)
        {
            builder.AddAttribute(5, "oncontextmenu", EventCallback.Factory.Create<MouseEventArgs>(this, e => OnDrawingContextMenu(e, drawing)));
            builder.AddEventPreventDefaultAttribute(6, "oncontextmenu", true);
            builder.AddEventStopPropagationAttribute(7, "oncontextmenu", true);
        }

        builder.OpenElement(8, "div");
        builder.AddAttribute(9, "class", $"{BaseCssClass}-clip");

        builder.OpenElement(10, "div");
        builder.AddAttribute(11, "class", $"{BaseCssClass}-container");
        builder.AddAttribute(12, "style", GetContainerStyle(drawing, range, zone));

        RenderInner(builder, drawing, range);

        if (selected)
        {
            builder.OpenElement(13, "div");
            builder.AddAttribute(14, "class", GetSelectionClass(zone));
            builder.CloseElement();
        }

        builder.CloseElement(); // container
        builder.CloseElement(); // clip

        if (selected)
        {
            for (var i = 0; i < Handles.Length; i++)
            {
                var (direction, shouldShow) = Handles[i];
                if (!shouldShow(zone))
                {
                    continue;
                }

                builder.OpenElement(100 + i * 10, "div");
                builder.AddAttribute(101 + i * 10, "class", $"rz-spreadsheet-resize-handle rz-spreadsheet-resize-{direction}");
                builder.AddAttribute(102 + i * 10, "style", GetHandleStyle(drawing, range, zone, direction));
                builder.AddAttribute(103 + i * 10, "data-direction", direction);
                builder.CloseElement();
            }
        }

        builder.CloseElement(); // outer
    }

    /// <summary>Computes the cell range covered by the drawing, including OneCellAnchor walk to find the end cell.</summary>
    protected RangeRef GetDrawingRange(TDrawing drawing)
    {
        ArgumentNullException.ThrowIfNull(drawing);

        var from = drawing.From;

        if (drawing.AnchorMode == DrawingAnchorMode.TwoCellAnchor && drawing.To is { } to)
        {
            return new RangeRef(
                new CellRef(from.Row, from.Column),
                new CellRef(to.Row, to.Column));
        }

        var endCol = from.Column;
        var remaining = drawing.Width - (Context.GetRectangle(from.Row, from.Column).Width - from.ColumnOffset);
        while (remaining > 0 && endCol < Worksheet.ColumnCount - 1)
        {
            endCol++;
            remaining -= Context.GetRectangle(from.Row, endCol).Width;
        }

        var endRow = from.Row;
        remaining = drawing.Height - (Context.GetRectangle(from.Row, from.Column).Height - from.RowOffset);
        while (remaining > 0 && endRow < Worksheet.RowCount - 1)
        {
            endRow++;
            remaining -= Context.GetRectangle(endRow, from.Column).Height;
        }

        return new RangeRef(
            new CellRef(from.Row, from.Column),
            new CellRef(endRow, endCol));
    }

    /// <summary>Computes the drawing's effective pixel dimensions (handles both anchor modes).</summary>
    protected (double width, double height) GetDimensions(TDrawing drawing, RangeRef range)
    {
        ArgumentNullException.ThrowIfNull(drawing);

        if (drawing.AnchorMode == DrawingAnchorMode.TwoCellAnchor && drawing.To is { } to)
        {
            var from = drawing.From;
            var fullRect = Context.GetRectangle(range.Start.Row, range.Start.Column, range.End.Row, range.End.Column);
            return (
                fullRect.Width + from.ColumnOffset + to.ColumnOffset,
                fullRect.Height + from.RowOffset + to.RowOffset);
        }

        return (drawing.Width, drawing.Height);
    }

    private (double x, double y) GetZoneOffset(TDrawing drawing, RangeInfo zone)
    {
        var from = drawing.From;
        double offsetX = from.ColumnOffset;
        double offsetY = from.RowOffset;

        for (var col = from.Column; col < zone.Range.Start.Column; col++)
        {
            offsetX -= Worksheet.Columns[col];
        }

        for (var row = from.Row; row < zone.Range.Start.Row; row++)
        {
            offsetY -= Worksheet.Rows[row];
        }

        return (offsetX, offsetY);
    }

    private string GetZoneStyle(RangeInfo zone)
    {
        var rect = Context.GetRectangle(zone.Range.Start.Row, zone.Range.Start.Column, zone.Range.End.Row, zone.Range.End.Column);
        return $"transform: translate({rect.Left.ToPx()}, {rect.Top.ToPx()}); width: {rect.Width.ToPx()}; height: {rect.Height.ToPx()};";
    }

    private string GetContainerStyle(TDrawing drawing, RangeRef range, RangeInfo zone)
    {
        var (width, height) = GetDimensions(drawing, range);
        var (offsetX, offsetY) = GetZoneOffset(drawing, zone);

        return $"position: absolute; left: {offsetX.ToPx()}; top: {offsetY.ToPx()}; width: {width.ToPx()}; height: {height.ToPx()};";
    }

    private string GetClass(bool frozenRow, bool frozenColumn)
    {
        return ClassList.Create(BaseCssClass)
            .Add("rz-spreadsheet-frozen-column", frozenColumn)
            .Add("rz-spreadsheet-frozen-row", frozenRow)
            .ToString();
    }

    private static string GetSelectionClass(RangeInfo zone)
    {
        return ClassList.Create("rz-spreadsheet-selection-range")
            .Add("rz-spreadsheet-selection-range-top", zone.Top)
            .Add("rz-spreadsheet-selection-range-left", zone.Left)
            .Add("rz-spreadsheet-selection-range-bottom", zone.Bottom)
            .Add("rz-spreadsheet-selection-range-right", zone.Right)
            .Add("rz-spreadsheet-frozen-row", zone.FrozenRow)
            .Add("rz-spreadsheet-frozen-column", zone.FrozenColumn)
            .ToString();
    }

    private string GetHandleStyle(TDrawing drawing, RangeRef range, RangeInfo zone, string direction)
    {
        var (width, height) = GetDimensions(drawing, range);
        var (offsetX, offsetY) = GetZoneOffset(drawing, zone);

        const double half = 4;
        double x = 0, y = 0;

        switch (direction)
        {
            case "nw": x = offsetX - half; y = offsetY - half; break;
            case "n":  x = offsetX + width / 2 - half; y = offsetY - half; break;
            case "ne": x = offsetX + width - half; y = offsetY - half; break;
            case "e":  x = offsetX + width - half; y = offsetY + height / 2 - half; break;
            case "se": x = offsetX + width - half; y = offsetY + height - half; break;
            case "s":  x = offsetX + width / 2 - half; y = offsetY + height - half; break;
            case "sw": x = offsetX - half; y = offsetY + height - half; break;
            case "w":  x = offsetX - half; y = offsetY + height / 2 - half; break;
        }

        return $"position: absolute; left: {x.ToPx()}; top: {y.ToPx()};";
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {
    }
}
