using System;
using Radzen.Documents.Fonts;

namespace Radzen.Documents.Layout;

internal sealed class BandLayout(LayoutCaptureContext capture)
{
    public PageLayerBuilder Content { get; } = new(capture);

    public double Height { get; set; }
}

internal static class BandLayouter
{
    public static BandLayout Layout(
        HeaderFooter band,
        double width,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution,
        LayoutCaptureContext capture)
    {
        var result = new BandLayout(capture);
        var engine = FlowPlacementEngine.ForBand(
            result,
            width,
            fonts,
            measureImage,
            resolution,
            capture);
        foreach (var block in BlockExpander.ExpandBlocks(band.Blocks, width, resolution))
        {
            engine.Place(block, 0);
        }

        result.Height = engine.Cursor;
        return result;
    }

    public static (double Width, double Height) EffectiveSize(Section section)
    {
        var (width, height) = section.PageSize.Effective(section.Orientation);
        return (width.Point, height.Point);
    }
}
