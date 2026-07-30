#nullable enable
using System.Linq;
using Radzen.Documents.Pdf.Render;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class PdfPaintOrderTests
{
    [Fact]
    public void PageContentFinalizer_MatchesThePdfPaintOrderContract()
        => Assert.Equal(PdfPaintOrder.Phases, PageContentFinalizer.PaintOrder());

    [Fact]
    public void PdfPaintOrder_PutsBodyBeforeBands()
        => Assert.Equal(
            new[] { PageLayerKind.Body, PageLayerKind.Header, PageLayerKind.Footer },
            PdfPaintOrder.Layers.ToArray());

    [Fact]
    public void PdfPaintOrder_PaintsBodyLinesThenTablesAndBoxesThenImagesAndCodeSymbols()
        => Assert.Equal(
            new[] { PaintContent.Lines, PaintContent.TablesAndBoxesByOrder, PaintContent.ImagesAndCodeSymbols },
            PdfPaintOrder.BodyContent.ToArray());

    [Fact]
    public void PdfPaintOrder_PaintsBandLinesThenImagesAndCodeSymbolsThenTablesAndBoxes()
        => Assert.Equal(
            new[] { PaintContent.Lines, PaintContent.ImagesAndCodeSymbols, PaintContent.TablesAndBoxesByOrder },
            PdfPaintOrder.BandContent.ToArray());

    [Fact]
    public void PdfPaintOrder_BandsSwapTablesAndBoxesWithImagesAndCodeSymbolsComparedToBody()
    {
        Assert.Equal(PdfPaintOrder.BodyContent[0], PdfPaintOrder.BandContent[0]);
        Assert.Equal(
            PdfPaintOrder.BodyContent.Skip(1).Reverse().ToArray(),
            PdfPaintOrder.BandContent.Skip(1).ToArray());
    }

    [Fact]
    public void PdfPaintOrder_ContentOfEachLayer_IsTheBodyOrBandContent()
    {
        Assert.Equal(PdfPaintOrder.BodyContent, PdfPaintOrder.ContentOf(PageLayerKind.Body));
        Assert.Equal(PdfPaintOrder.BandContent, PdfPaintOrder.ContentOf(PageLayerKind.Header));
        Assert.Equal(PdfPaintOrder.BandContent, PdfPaintOrder.ContentOf(PageLayerKind.Footer));
    }
}
