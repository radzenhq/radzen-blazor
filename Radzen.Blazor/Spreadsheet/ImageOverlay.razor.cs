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

    private string GetStyle(SheetImage image)
    {
        double left, top, width, height;

        var fromRect = Context.GetRectangle(image.From.Row, image.From.Column);

        if (image.AnchorMode == ImageAnchorMode.TwoCellAnchor && image.To != null)
        {
            var toRect = Context.GetRectangle(image.To.Row, image.To.Column);
            left = fromRect.Left + image.From.ColumnOffset / EmuPerPixel;
            top = fromRect.Top + image.From.RowOffset / EmuPerPixel;
            var right = toRect.Left + image.To.ColumnOffset / EmuPerPixel;
            var bottom = toRect.Top + image.To.RowOffset / EmuPerPixel;
            width = right - left;
            height = bottom - top;
        }
        else
        {
            left = fromRect.Left + image.From.ColumnOffset / EmuPerPixel;
            top = fromRect.Top + image.From.RowOffset / EmuPerPixel;
            width = image.Width / EmuPerPixel;
            height = image.Height / EmuPerPixel;
        }

        return $"transform: translate({left.ToPx()}, {top.ToPx()}); width: {width.ToPx()}; height: {height.ToPx()};";
    }

    private string GetClass(SheetImage image)
    {
        return ClassList.Create("rz-spreadsheet-image")
            .Add("rz-spreadsheet-frozen-column", image.From.Column < Sheet.Columns.Frozen)
            .Add("rz-spreadsheet-frozen-row", image.From.Row < Sheet.Rows.Frozen)
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
