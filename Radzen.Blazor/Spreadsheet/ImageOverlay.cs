using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Rendering;
using Radzen.Documents.Spreadsheet;

#nullable enable
namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Renders floating images on a spreadsheet sheet.
/// </summary>
public class ImageOverlay : DrawingOverlayBase<SheetImage>
{
    private readonly EventBinding<Worksheet> worksheetBinding;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageOverlay"/> class.
    /// </summary>
    public ImageOverlay()
    {
        worksheetBinding = new EventBinding<Worksheet>(
            w =>
            {
                w.ImagesChanged += OnChanged;
                w.SelectedImageChanged += OnChanged;
                w.DrawingGeometryChanged += OnDrawingGeometryChanged;
            },
            w =>
            {
                w.ImagesChanged -= OnChanged;
                w.SelectedImageChanged -= OnChanged;
                w.DrawingGeometryChanged -= OnDrawingGeometryChanged;
            });
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        worksheetBinding.Bind(Worksheet);
    }

    private void OnChanged()
    {
        StateHasChanged();
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        worksheetBinding.Dispose();
        base.Dispose();
    }

    /// <inheritdoc/>
    protected override IEnumerable<SheetImage> Drawings => Worksheet.Images;

    /// <inheritdoc/>
    protected override bool IsSelected(SheetImage drawing) => Worksheet.SelectedImage == drawing;

    /// <inheritdoc/>
    protected override string BaseCssClass => "rz-spreadsheet-image";

    /// <inheritdoc/>
    protected override void SelectDrawing(SheetImage drawing)
    {
        Worksheet.SelectedImage = drawing;
    }

    /// <inheritdoc/>
    protected override void RenderInner(RenderTreeBuilder builder, SheetImage drawing, RangeRef range)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(drawing);

        builder.OpenElement(0, "img");
        builder.AddAttribute(1, "src", drawing.DataUri);
        builder.AddAttribute(2, "alt", drawing.Description ?? drawing.Name ?? "");
        builder.CloseElement();
    }
}
