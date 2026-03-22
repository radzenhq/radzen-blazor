using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System.Threading.Tasks;

#nullable enable
using Radzen.Documents.Spreadsheet;
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
    public Worksheet Worksheet { get; set; } = default!;

    /// <summary>
    /// Gets or sets the virtual grid context.
    /// </summary>
    [Parameter]
    public IVirtualGridContext Context { get; set; } = default!;

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (Worksheet != null)
        {
            Worksheet.ImagesChanged -= OnChanged;
            Worksheet.SelectedImageChanged -= OnChanged;
        }

        await base.SetParametersAsync(parameters);

        if (Worksheet != null)
        {
            Worksheet.ImagesChanged += OnChanged;
            Worksheet.SelectedImageChanged += OnChanged;
        }
    }

    private void OnChanged()
    {
        StateHasChanged();
    }

    void IDisposable.Dispose()
    {
        if (Worksheet != null)
        {
            Worksheet.ImagesChanged -= OnChanged;
            Worksheet.SelectedImageChanged -= OnChanged;
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
        while (remaining > 0 && endCol < Worksheet.ColumnCount - 1)
        {
            endCol++;
            remaining -= Context.GetRectangle(image.From.Row, endCol).Width;
        }

        var endRow = image.From.Row;
        remaining = heightPx - (Context.GetRectangle(image.From.Row, image.From.Column).Height - image.From.RowOffset / EmuPerPixel);
        while (remaining > 0 && endRow < Worksheet.RowCount - 1)
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

    private (double width, double height) GetImageDimensions(SheetImage image, RangeRef imageRange)
    {
        if (image.AnchorMode == ImageAnchorMode.TwoCellAnchor && image.To != null)
        {
            var fullRect = Context.GetRectangle(imageRange.Start.Row, imageRange.Start.Column, imageRange.End.Row, imageRange.End.Column);
            return (
                fullRect.Width + image.From.ColumnOffset / EmuPerPixel + image.To.ColumnOffset / EmuPerPixel,
                fullRect.Height + image.From.RowOffset / EmuPerPixel + image.To.RowOffset / EmuPerPixel);
        }

        return (image.Width / EmuPerPixel, image.Height / EmuPerPixel);
    }

    private (double x, double y) GetZoneOffset(SheetImage image, RangeInfo zone)
    {
        double offsetX = image.From.ColumnOffset / EmuPerPixel;
        double offsetY = image.From.RowOffset / EmuPerPixel;

        for (var col = image.From.Column; col < zone.Range.Start.Column; col++)
        {
            offsetX -= Worksheet.Columns[col];
        }

        for (var row = image.From.Row; row < zone.Range.Start.Row; row++)
        {
            offsetY -= Worksheet.Rows[row];
        }

        return (offsetX, offsetY);
    }

    private string GetImgStyle(SheetImage image, RangeRef imageRange, RangeInfo zone)
    {
        var (imgWidth, imgHeight) = GetImageDimensions(image, imageRange);
        var (offsetX, offsetY) = GetZoneOffset(image, zone);

        return $"position: absolute; left: {offsetX.ToPx()}; top: {offsetY.ToPx()}; width: {imgWidth.ToPx()}; height: {imgHeight.ToPx()};";
    }

    private static string GetClass(bool frozenRow, bool frozenColumn)
    {
        return ClassList.Create("rz-spreadsheet-image")
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

    private string GetHandleStyle(SheetImage image, RangeRef imageRange, RangeInfo zone, string direction)
    {
        var (imgWidth, imgHeight) = GetImageDimensions(image, imageRange);
        var (offsetX, offsetY) = GetZoneOffset(image, zone);

        const double half = 4; // half of 8px handle
        double x = 0, y = 0;

        switch (direction)
        {
            case "nw": x = offsetX - half; y = offsetY - half; break;
            case "n": x = offsetX + imgWidth / 2 - half; y = offsetY - half; break;
            case "ne": x = offsetX + imgWidth - half; y = offsetY - half; break;
            case "e": x = offsetX + imgWidth - half; y = offsetY + imgHeight / 2 - half; break;
            case "se": x = offsetX + imgWidth - half; y = offsetY + imgHeight - half; break;
            case "s": x = offsetX + imgWidth / 2 - half; y = offsetY + imgHeight - half; break;
            case "sw": x = offsetX - half; y = offsetY + imgHeight - half; break;
            case "w": x = offsetX - half; y = offsetY + imgHeight / 2 - half; break;
        }

        return $"position: absolute; left: {x.ToPx()}; top: {y.ToPx()};";
    }

    private PixelRectangle GetImageRect(SheetImage image, RangeRef imageRange)
    {
        var (imgWidth, imgHeight) = GetImageDimensions(image, imageRange);

        var startRect = Context.GetRectangle(image.From.Row, image.From.Column);
        var left = startRect.Left + image.From.ColumnOffset / EmuPerPixel;
        var top = startRect.Top + image.From.RowOffset / EmuPerPixel;

        return new PixelRectangle(top, left, top + imgHeight, left + imgWidth);
    }

    private static string GetDataUri(SheetImage image)
    {
        return image.DataUri;
    }

    private void OnImagePointerDown(PointerEventArgs e, SheetImage image)
    {
        Worksheet.SelectedImage = image;
        Worksheet.Selection.Clear();
        StateHasChanged();
    }
}
