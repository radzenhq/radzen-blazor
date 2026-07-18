using System;

namespace Radzen.Documents.Pdf.Emit;

internal static class FlowContentPlacer
{
    public static (double Width, double Height) MeasureImage(
        Image image,
        double width,
        Func<Image, double, (double Width, double Height)>? measureImage)
        => measureImage is null ? Paginator.MeasureImage(image, width) : measureImage(image, width);

    public static PositionedImage Image(Image image, double width, double y, double imageWidth, double imageHeight)
        => new()
        {
            Source = image,
            Y = y,
            Width = imageWidth,
            Height = imageHeight,
            XOffset = HorizontalAlignmentOffset.Of(image.Alignment, width, imageWidth),
        };

    public static PositionedCode Code(Block block, double width, double y, double codeWidth, double codeHeight)
        => new()
        {
            Source = block,
            Y = y,
            Width = codeWidth,
            Height = codeHeight,
            XOffset = HorizontalAlignmentOffset.Of(Paginator.CodeAlignment(block), width, codeWidth),
        };

    public static PositionedTableFragment Table(LaidOutTable layout, TableFragment fragment, double y, int order)
        => new()
        {
            Layout = layout,
            Fragment = fragment,
            Y = y,
            Order = order,
        };
}
