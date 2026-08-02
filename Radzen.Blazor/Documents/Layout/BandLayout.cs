using Radzen.Documents.Fonts;

namespace Radzen.Documents.Layout;

internal sealed class BandLayout
{
    public PageLayerBuilder Content { get; } = new();

    public double Height { get; set; }
}

internal static class BandLayouter
{
    public static BandLayout Layout(
        HeaderFooter band,
        double width,
        FontCollection fonts,
        LoweringResult resolution,
        LayoutCaptureContext capture)
    {
        var result = new BandLayout();
        var engine = FlowPlacementEngine.ForBand(
            result,
            width,
            fonts,
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
