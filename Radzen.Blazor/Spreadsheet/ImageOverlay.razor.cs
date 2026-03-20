using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System.Threading.Tasks;

#nullable enable
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Renders floating images on a spreadsheet sheet.
/// </summary>
public partial class ImageOverlay : ComponentBase, IDisposable
{
    private const double EmuPerPixel = 9525.0;

    /// <summary>
    /// Gets or sets the sheet.
    /// </summary>
    [Parameter]
    public Sheet Sheet { get; set; } = default!;

    /// <summary>
    /// Gets or sets the virtual grid context.
    /// </summary>
    [Parameter]
    public IVirtualGridContext Context { get; set; } = default!;

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (Sheet != null)
        {
            Sheet.Commands.Changed -= OnCommandsChanged;
        }

        await base.SetParametersAsync(parameters);

        if (Sheet != null)
        {
            Sheet.Commands.Changed += OnCommandsChanged;
        }
    }

    private void OnCommandsChanged()
    {
        StateHasChanged();
    }

    void IDisposable.Dispose()
    {
        if (Sheet != null)
        {
            Sheet.Commands.Changed -= OnCommandsChanged;
        }
    }

    private RangeRef GetImageRange(SheetImage image)
    {
        if (image.AnchorMode == ImageAnchorMode.TwoCellAnchor && image.To != null)
        {
            return new RangeRef(
                new CellRef(image.From.Row, image.From.Column),
                new CellRef(image.To.Row, image.To.Column));
        }

        // For OneCellAnchor, find the end cell by walking columns/rows until the pixel extent is covered
        var widthPx = image.Width / EmuPerPixel;
        var heightPx = image.Height / EmuPerPixel;

        var endCol = image.From.Column;
        var remaining = widthPx - (Context.GetRectangle(image.From.Row, image.From.Column).Width - image.From.ColumnOffset / EmuPerPixel);
        while (remaining > 0 && endCol < Sheet.ColumnCount - 1)
        {
            endCol++;
            remaining -= Context.GetRectangle(image.From.Row, endCol).Width;
        }

        var endRow = image.From.Row;
        remaining = heightPx - (Context.GetRectangle(image.From.Row, image.From.Column).Height - image.From.RowOffset / EmuPerPixel);
        while (remaining > 0 && endRow < Sheet.RowCount - 1)
        {
            endRow++;
            remaining -= Context.GetRectangle(endRow, image.From.Column).Height;
        }

        return new RangeRef(
            new CellRef(image.From.Row, image.From.Column),
            new CellRef(endRow, endCol));
    }

    private string GetZoneStyle(SheetImage image, RangeRef imageRange, RangeInfo zone)
    {
        var rect = Context.GetRectangle(zone.Range.Start.Row, zone.Range.Start.Column, zone.Range.End.Row, zone.Range.End.Column);
        return $"transform: translate({rect.Left.ToPx()}, {rect.Top.ToPx()}); width: {rect.Width.ToPx()}; height: {rect.Height.ToPx()};";
    }

    private string GetImgStyle(SheetImage image, RangeRef imageRange, RangeInfo zone)
    {
        double imgWidth, imgHeight;

        if (image.AnchorMode == ImageAnchorMode.TwoCellAnchor && image.To != null)
        {
            var fullRect = Context.GetRectangle(imageRange.Start.Row, imageRange.Start.Column, imageRange.End.Row, imageRange.End.Column);
            imgWidth = fullRect.Width + image.From.ColumnOffset / EmuPerPixel + image.To.ColumnOffset / EmuPerPixel;
            imgHeight = fullRect.Height + image.From.RowOffset / EmuPerPixel + image.To.RowOffset / EmuPerPixel;
        }
        else
        {
            imgWidth = image.Width / EmuPerPixel;
            imgHeight = image.Height / EmuPerPixel;
        }

        // Calculate pixel offset from image start to zone start using column/row sizes (scroll-independent)
        double offsetX = image.From.ColumnOffset / EmuPerPixel;
        double offsetY = image.From.RowOffset / EmuPerPixel;

        for (var col = image.From.Column; col < zone.Range.Start.Column; col++)
        {
            offsetX -= Sheet.Columns[col];
        }

        for (var row = image.From.Row; row < zone.Range.Start.Row; row++)
        {
            offsetY -= Sheet.Rows[row];
        }

        return $"position: absolute; left: {offsetX.ToPx()}; top: {offsetY.ToPx()}; width: {imgWidth.ToPx()}; height: {imgHeight.ToPx()};";
    }

    private string GetClass(SheetImage image, bool frozenRow, bool frozenColumn)
    {
        return ClassList.Create("rz-spreadsheet-image")
            .Add("rz-spreadsheet-frozen-column", frozenColumn)
            .Add("rz-spreadsheet-frozen-row", frozenRow)
            .Add("rz-spreadsheet-image-selected", Sheet.SelectedImage == image)
            .ToString();
    }

    private static string GetDataUri(SheetImage image)
    {
        return image.DataUri;
    }

    private void OnImagePointerDown(PointerEventArgs e, SheetImage image)
    {
        Sheet.SelectedImage = image;
        StateHasChanged();
    }
}
