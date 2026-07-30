#nullable enable
using System;
using System.Collections;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Render;
using Xunit;
using Radzen.Documents.Codes;
using Radzen.Documents.Geometry;

namespace Radzen.Blazor.Pdf.Tests;

public class LaidOutContractTests
{
    private static Section Page(Document document, double width = 400, double height = 300)
    {
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(width), Unit.FromPoint(height));
        section.Margins.SetAll(Unit.FromPoint(20));
        return section;
    }

    private static LineFragment FirstFragment(LaidOutPage page)
        => page.Body.Lines[0].Line.Fragments[0];

    [Fact]
    public void LaidOutFragment_CarriesResolvedRunPaint()
    {
        var document = new Document();
        var section = Page(document);
        var run = section.Blocks.AddParagraph().Inlines.Add("Radzen");
        run.Opacity = 0.5;
        run.LetterSpacing = Unit.FromPoint(3);
        run.WordSpacing = Unit.FromPoint(2);
        run.HorizontalScale = 0.8;
        run.Invisible = true;
        run.Link = "https://www.radzen.com/";
        run.Font.Color = Color.FromRgb(10, 20, 30);
        run.Font.Underline = true;

        var paint = FirstFragment(Assert.Single(DocumentLayouter.Layout(document).Pages)).Paint;

        Assert.Equal(0.5, paint.Opacity, 6);
        Assert.Equal(3, paint.LetterSpacing, 6);
        Assert.Equal(2, paint.WordSpacing, 6);
        Assert.Equal(0.8, paint.HorizontalScale, 6);
        Assert.True(paint.Invisible);
        Assert.True(paint.Font.Underline);
        Assert.Equal("https://www.radzen.com/", paint.LinkTarget);
        Assert.Equal(Color.FromRgb(10, 20, 30), paint.Font.Color);
    }

    [Fact]
    public void MutatingARunAfterLayout_DoesNotChangeTheLaidOutDocument()
    {
        var document = new Document();
        var section = Page(document);
        var run = section.Blocks.AddParagraph().Inlines.Add("Radzen");
        run.Opacity = 0.5;
        run.Font.Color = Color.FromRgb(1, 2, 3);
        run.Link = "https://www.radzen.com/";

        var page = Assert.Single(DocumentLayouter.Layout(document).Pages);
        var before = FirstFragment(page).Paint;

        run.Opacity = 0.1;
        run.Invisible = true;
        run.Link = "https://example.com/";
        run.Text = "Mutated";
        run.Font.Color = Color.FromRgb(9, 9, 9);

        Assert.Equal(before, FirstFragment(page).Paint);
        Assert.Equal("https://www.radzen.com/", FirstFragment(page).Paint.LinkTarget);
        Assert.Equal("Radzen", FirstFragment(page).GlyphRun.Text);
    }

    [Fact]
    public void LaidOutFragment_CapturesFallbackFacesAndLogicalClusters()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        BuildTestSupport.RegisterCjk(document);
        document.Fonts.SetFallback(BuildTestSupport.Cjk);
        var section = Page(document);
        var run = section.Blocks.AddParagraph().Inlines.Add("A\u4E2DB");
        run.Font.Family = BuildTestSupport.Latin;

        var laidOut = DocumentLayouter.Layout(document);
        var fragment = FirstFragment(Assert.Single(laidOut.Pages));
        var spans = fragment.GlyphRun.Spans;

        Assert.Equal("A\u4E2DB", fragment.GlyphRun.Text);
        Assert.Equal(3, spans.Length);
        Assert.Same(fragment.Face, spans[0].Face.Sfnt);
        Assert.NotSame(spans[0].Face.Sfnt, spans[1].Face.Sfnt);
        Assert.Same(spans[0].Face.Sfnt, spans[2].Face.Sfnt);
        Assert.Equal(0, spans[0].XOffset);
        Assert.Equal(spans[0].Advance, spans[1].XOffset);
        Assert.Equal(spans[0].Advance + spans[1].Advance, spans[2].XOffset);
        Assert.All(
            spans.Where(span => span.IsSfnt),
            span => Assert.Contains(
                laidOut.Fonts.Faces,
                registered => ReferenceEquals(registered.Face, span.Face.Sfnt)));
        Assert.Equal(new[] { 0, 1, 2 }, spans.SelectMany(span => span.SfntGlyphs).Select(glyph => glyph.Cluster));
    }

    [Fact]
    public void LayoutCapture_CapturesExactPerSpanPenOffsets()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        BuildTestSupport.RegisterCjk(document);
        document.Fonts.SetFallback(BuildTestSupport.Cjk);
        var run = new Run("A \u4E2DB")
        {
            LetterSpacing = Unit.FromPoint(1.25),
            WordSpacing = Unit.FromPoint(0.75),
            HorizontalScale = 0.83,
        };
        run.Font.Family = BuildTestSupport.Latin;
        var capture = new LayoutCaptureContext();
        var paint = GeometryCapture.Fragment(run, run.Font, capture);
        var positioned = GeometryCapture.PositionSpans(
            document.Fonts.CaptureGlyphRun(run.Text, run.Font),
            paint);

        Assert.Equal(3, positioned.Spans.Length);
        var expectedOffset = 0.0;
        foreach (var span in positioned.Spans)
        {
            Assert.Equal(expectedOffset, span.XOffset);
            expectedOffset += (span.Advance * paint.ScriptScale
                + (Math.Max(0, span.GlyphCount - 1) + 1) * paint.LetterSpacing
                + span.WordSpaceCount * paint.WordSpacing)
                * paint.HorizontalScale;
        }
    }

    [Fact]
    public void LaidOutBuiltInFragment_CapturesUnicodeAndAdvances()
    {
        var document = new Document();
        var section = Page(document);
        section.Blocks.AddParagraph("A\u20AC\uFB01");

        var fragment = FirstFragment(Assert.Single(DocumentLayouter.Layout(document).Pages));
        var span = Assert.Single(fragment.GlyphRun.Spans);

        Assert.Equal(CapturedFontFaceKind.BuiltIn, span.Face.Kind);
        Assert.Equal("Helvetica", span.Face.BuiltIn.PostScriptName);
        Assert.Empty(span.SfntGlyphs);
        Assert.Equal(new[] { 'A', 0x20AC, 0xFB01 }, span.BuiltInGlyphs.Select(glyph => glyph.Codepoint));
        Assert.Equal(new[] { 0, 1, 2 }, span.BuiltInGlyphs.Select(glyph => glyph.Cluster));
        Assert.All(span.BuiltInGlyphs, glyph => Assert.True(glyph.Advance > 0));
        Assert.DoesNotContain(
            typeof(CapturedBuiltInGlyph).GetProperties(),
            property => property.PropertyType == typeof(byte));
    }

    [Theory]
    [InlineData("Helvetica", false, false, "Helvetica")]
    [InlineData("Helvetica", true, true, "Helvetica-BoldOblique")]
    [InlineData("Times", false, true, "Times-Italic")]
    [InlineData("Courier", true, false, "Courier-Bold")]
    public void LaidOutBuiltInFragment_CapturesResolvedFace(
        string family,
        bool bold,
        bool italic,
        string postScriptName)
    {
        var document = new Document();
        var run = Page(document).Blocks.AddParagraph("Text").Inlines[0];
        run.Font.Family = family;
        run.Font.Bold = bold;
        run.Font.Italic = italic;

        var span = Assert.Single(
            FirstFragment(Assert.Single(DocumentLayouter.Layout(document).Pages))
                .GlyphRun.Spans);

        Assert.Equal(CapturedFontFaceKind.BuiltIn, span.Face.Kind);
        Assert.Equal(postScriptName, span.Face.BuiltIn.PostScriptName);
    }

    [Fact]
    public void CapturedBuiltInAdjustment_IsInPointsUntilPdfEmission()
    {
        var document = new Document { Fonts = { EnableKerning = true } };
        var run = Page(document).Blocks.AddParagraph("AV").Inlines[0];
        run.Font.Family = "Helvetica";
        run.Font.Size = 11.3;

        var glyph = Assert.Single(
            FirstFragment(Assert.Single(DocumentLayouter.Layout(document).Pages))
                .GlyphRun.Spans)
            .BuiltInGlyphs[0];
        var metrics = BuiltInFontMetrics.Resolve(run.Font)!;
        var kern = metrics.GetRunKerning('A', 'V');
        var expectedPoints = -FontMetric.Scale(kern, run.Font.EffectiveSize.Point, 1000);

        Assert.Equal(expectedPoints, glyph.TextAdjustmentPoints);
        Assert.Equal(
            -kern,
            SfntRunBuilder.PdfTextAdjustment(
                glyph.TextAdjustmentPoints,
                run.Font.EffectiveSize.Point));
    }

    [Fact]
    public void AllowUnsupportedCharacters_IsCapturedAtLayout()
    {
        var document = new Document();
        var section = Page(document);
        section.Blocks.AddParagraph("A\uFB01B");

        var laidOut = DocumentLayouter.Layout(document);
        var glyph = Assert.Single(
            FirstFragment(Assert.Single(laidOut.Pages)).GlyphRun.Spans)
            .BuiltInGlyphs[1];

        Assert.Equal(0xFB01, glyph.Codepoint);
        var error = Assert.Throws<InvalidOperationException>(() => Render(laidOut, document));
        Assert.Equal(
            "The built-in font 'Helvetica' cannot draw 'ﬁ' (U+FB01): a base-14 font is limited "
            + "to the WinAnsi character set. Register a font that covers these characters with "
            + "FontCollection.Register, add such a font to the SetFallback chain, or set "
            + "FontCollection.AllowUnsupportedCharacters to true to draw '?' in their place.",
            error.Message);

        document.Fonts.AllowUnsupportedCharacters = true;
        Assert.Throws<InvalidOperationException>(() => Render(laidOut, document));

        var recaptured = DocumentLayouter.Layout(document);
        var rendered = Render(recaptured, document);

        using var buffer = new MemoryStream(rendered);
        var reloaded = PortableDocument.LoadFromStream(buffer);

        Assert.Equal("A?B", reloaded.Pages[0].ExtractText());
    }

    [Fact]
    public void ReplacingARegisteredFamilyAfterLayout_DoesNotChangeTheCapturedLaidOutPdf()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = Page(document);
        var run = section.Blocks.AddParagraph().Inlines.Add("AVATAR");
        run.Font.Family = BuildTestSupport.Latin;

        var laidOut = DocumentLayouter.Layout(document);
        var fragment = FirstFragment(Assert.Single(laidOut.Pages));
        var capturedFace = Assert.Single(fragment.GlyphRun.Spans).Face.Sfnt;
        var registered = Assert.Single(document.Fonts.RegisteredFaces());
        var asset = Assert.Single(laidOut.Fonts.Faces);
        var before = Render(laidOut, document);

        Assert.Same(registered.Source, asset.Source);
        Assert.Equal(registered.FaceIndex, asset.FaceIndex);
        Assert.Same(capturedFace, asset.Face);

        document.Fonts.Register(
            BuildTestSupport.Latin,
            new MemoryStream(PdfTestResources.ReadAllBytes("Fonts/LiberationSerif-Regular.ttf")));

        Assert.NotSame(capturedFace, document.Fonts.ResolveFace(run.Font));
        Assert.Equal(before, Render(laidOut, document));
    }

    private static Table BorderedTable()
    {
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(100));
        table.Borders.Width = 1;
        table.Borders.Style = BorderStyle.Solid;
        table.Borders.Color = Color.FromRgb(1, 1, 1);

        var header = table.Rows.Add();
        header.RepeatOnEveryPage = true;
        header.Background = Color.FromRgb(200, 200, 200);
        header.Borders.Bottom.Width = 3;
        header.Borders.Bottom.Color = Color.FromRgb(2, 2, 2);
        header.Cells[0].Blocks.AddParagraph("head");

        var body = table.Rows.Add();
        body.Cells[0].Borders.Left.Width = 5;
        body.Cells[0].Borders.Left.Color = Color.FromRgb(3, 3, 3);
        body.Cells[0].Blocks.AddParagraph("body");
        return table;
    }

    [Fact]
    public void LaidOutCell_CarriesPerEdgeBordersResolvedByPrecedence()
    {
        var document = new Document();
        var section = Page(document);
        var table = section.Blocks.Add(BorderedTable());

        var page = Assert.Single(DocumentLayouter.Layout(document).Pages);
        var layout = page.Body.Tables[0].Layout;
        var head = layout.Cells.Single(cell => cell.Row == 0);
        var body = layout.Cells.Single(cell => cell.Row == 1);

        Assert.Equal(new ResolvedEdge(Color.FromRgb(2, 2, 2), 3, BorderStyle.Solid), head.Decoration.Bottom);
        Assert.Equal(new ResolvedEdge(Color.FromRgb(1, 1, 1), 1, BorderStyle.Solid), head.Decoration.Top);
        Assert.Equal(new ResolvedEdge(Color.FromRgb(3, 3, 3), 5, BorderStyle.Solid), body.Decoration.Left);
        Assert.Equal(Color.FromRgb(200, 200, 200), layout.Decoration.RowBackground(0));
        Assert.Null(layout.Decoration.RowBackground(1));

        table.Borders.Width = 9;
        table.Rows[0].Background = Color.FromRgb(7, 7, 7);

        Assert.Equal(new ResolvedEdge(Color.FromRgb(1, 1, 1), 1, BorderStyle.Solid), head.Decoration.Top);
        Assert.Equal(Color.FromRgb(200, 200, 200), layout.Decoration.RowBackground(0));
    }

    [Fact]
    public void LaidOutTableFragments_CarryPositionedCellsIncludingRepeatedHeaders()
    {
        var document = new Document();
        var section = Page(document, height: 140);
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(100));
        var header = table.Rows.Add();
        header.RepeatOnEveryPage = true;
        header.Cells[0].Blocks.AddParagraph("head");
        for (var i = 0; i < 12; i++)
        {
            table.Rows.Add().Cells[0].Blocks.AddParagraph($"row {i}");
        }

        section.Blocks.Add(table);

        var pages = DocumentLayouter.Layout(document).Pages;
        Assert.True(pages.Length > 1);

        foreach (var page in pages)
        {
            var fragment = Assert.Single(page.Body.Tables);
            var first = fragment.Rows[0];

            Assert.True(first.IsHeader);
            Assert.Equal(0, first.SourceRow);
            var placed = Assert.Single(first.Cells);
            Assert.Equal(first.Y, placed.Cell.Bounds.Y + placed.Delta, 6);
            Assert.All(fragment.Rows, row => Assert.All(
                row.Cells,
                cell => Assert.Equal(row.Y, cell.Cell.Bounds.Y + cell.Delta, 6)));
        }
    }

    [Fact]
    public void LaidOutPage_CarriesPositionedWatermark()
    {
        var document = new Document();
        var section = Page(document, 400, 300);
        section.Watermark = new Watermark { Text = "DRAFT", Rotation = 30, Opacity = 0.25 };
        section.Blocks.AddParagraph("body");

        var watermark = Assert.Single(DocumentLayouter.Layout(document).Pages).Watermark;

        Assert.NotNull(watermark);
        Assert.Equal(200, watermark!.CenterX, 6);
        Assert.Equal(150, watermark.CenterY, 6);
        Assert.Equal(30, watermark.Rotation, 6);
        Assert.Equal(0.25, watermark.Opacity, 6);

        var text = watermark.Text;
        Assert.NotNull(text);
        Assert.Equal("DRAFT", text!.Value.Text);
        Assert.Equal(text.Value.Font.Size * 0.35, text.Value.Baseline, 6);
    }

    [Fact]
    public void LaidOutCodes_CarryEncodedModules()
    {
        var document = new Document();
        var section = Page(document);
        section.Blocks.AddQrCode("RADZEN", Unit.FromPoint(120));
        section.Blocks.AddBarcode(BarcodeType.Code128, "RADZEN", Unit.FromPoint(200), Unit.FromPoint(40));

        var codes = Assert.Single(DocumentLayouter.Layout(document).Pages).Body.CodeSymbols;

        Assert.Equal(2, codes.Length);
        foreach (var code in codes)
        {
            Assert.NotEmpty(code.Modules);
            Assert.All(code.Modules, module =>
            {
                Assert.True(module.Width > 0 && module.Height > 0);
                Assert.True(module.X >= 0 && module.Y >= 0);
                Assert.True(module.X + module.Width <= code.Width + 0.001);
            });
        }
    }

    [Fact]
    public void LaidOutLinksAndAnchors_AreTopOrigin()
    {
        var document = new Document();
        var section = Page(document, 400, 300);
        var paragraph = section.Blocks.AddParagraph();
        paragraph.Inlines.Add("Radzen").Link = "https://www.radzen.com/";
        paragraph.Inlines.Add(" here").Anchor = "here";

        var page = Assert.Single(DocumentLayouter.Layout(document).Pages);
        var link = Assert.Single(page.Links);
        var anchor = Assert.Single(page.Anchors);

        Assert.True(link.Bottom > link.Top);
        Assert.True(link.Right > link.Left);
        Assert.True(link.Top < page.Size.Height.Point / 2, "a link on the first line sits in the top half");
        Assert.True(anchor.Top < page.Size.Height.Point / 2, "an anchor on the first line sits in the top half");
        Assert.True(anchor.Top >= page.ContentBox.Y);
    }

    [Fact]
    public void PdfPaintOrder_MatchesTheLaidOutPaintOrderContract()
        => Assert.Equal(LaidOutPaintOrder.Phases, PageContentFinalizer.PaintOrder());

    [Fact]
    public void LaidOutPaintOrder_PutsBodyBeforeBands()
        => Assert.Equal(
            new[] { PageLayerKind.Body, PageLayerKind.Header, PageLayerKind.Footer },
            LaidOutPaintOrder.Layers.ToArray());

    [Fact]
    public void LaidOutPaintOrder_PaintsBodyLinesThenTablesAndBoxesThenImagesAndCodeSymbols()
        => Assert.Equal(
            new[] { PaintContent.Lines, PaintContent.TablesAndBoxesByOrder, PaintContent.ImagesAndCodeSymbols },
            LaidOutPaintOrder.BodyContent.ToArray());

    [Fact]
    public void LaidOutPaintOrder_PaintsBandLinesThenImagesAndCodeSymbolsThenTablesAndBoxes()
        => Assert.Equal(
            new[] { PaintContent.Lines, PaintContent.ImagesAndCodeSymbols, PaintContent.TablesAndBoxesByOrder },
            LaidOutPaintOrder.BandContent.ToArray());

    [Fact]
    public void LaidOutPaintOrder_BandsSwapTablesAndBoxesWithImagesAndCodeSymbolsComparedToBody()
    {
        Assert.Equal(LaidOutPaintOrder.BodyContent[0], LaidOutPaintOrder.BandContent[0]);
        Assert.Equal(
            LaidOutPaintOrder.BodyContent.Skip(1).Reverse().ToArray(),
            LaidOutPaintOrder.BandContent.Skip(1).ToArray());
    }

    [Fact]
    public void LaidOutPaintOrder_ContentOfEachLayer_IsTheBodyOrBandContent()
    {
        Assert.Equal(LaidOutPaintOrder.BodyContent, LaidOutPaintOrder.ContentOf(PageLayerKind.Body));
        Assert.Equal(LaidOutPaintOrder.BandContent, LaidOutPaintOrder.ContentOf(PageLayerKind.Header));
        Assert.Equal(LaidOutPaintOrder.BandContent, LaidOutPaintOrder.ContentOf(PageLayerKind.Footer));
    }

    private static IEnumerable<PropertyInfo> SettableProperties(System.Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.SetMethod is { ReturnParameter: var returned }
                && !returned.GetRequiredCustomModifiers().Any(modifier => modifier.Name == "IsExternalInit"));

    [Fact]
    public void LaidOutTypes_HaveNoSettableProperties()
    {
        var offenders =
            from type in LaidOutTypes()
            from property in SettableProperties(type)
            select $"{type.Name}.{property.Name}";

        Assert.Empty(offenders);
    }

    [Fact]
    public void LaidOutCollections_AreImmutableArrays()
    {
        var offenders =
            from type in LaidOutTypes()
            from property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            where !Opaque(property)
            let declared = Unwrap(property.PropertyType)
            where IsCollection(declared) && !IsImmutableArray(declared)
            select $"{type.Name}.{property.Name} is {property.PropertyType}";

        Assert.Empty(offenders);
    }

    [Fact]
    public void LaidOutReachableGraph_HasNoMutableModelObjectsOrCollections()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = Page(document, 500, 700);
        var paragraph = section.Blocks.AddParagraph();
        paragraph.Inlines.Add("Body").Font.Family = BuildTestSupport.Latin;
        paragraph.Inlines.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(100));
        table.Rows.Add().Cells[0].Blocks.AddParagraph("Cell");

        var offenders = ReachableGraphOffenders(DocumentLayouter.Layout(document));

        Assert.Empty(offenders);
    }

    private static PropertyInfo[] BehaviorFlags()
        => typeof(FontCollection)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.PropertyType == typeof(bool) && property.CanRead && property.CanWrite)
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();

    private static FontCollectionSnapshot SnapshotWithFlags(bool value)
    {
        var document = new Document();

        foreach (var flag in BehaviorFlags())
        {
            flag.SetValue(document.Fonts, value);
        }

        Page(document).Blocks.AddParagraph("Body");

        return DocumentLayouter.Layout(document).Fonts;
    }

    [Fact]
    public void FontCollectionSnapshot_CapturesEveryBehaviorFlag()
    {
        var flags = BehaviorFlags();
        Assert.NotEmpty(flags);

        var raised = SnapshotWithFlags(true);
        var cleared = SnapshotWithFlags(false);

        foreach (var flag in flags)
        {
            var captured = typeof(FontCollectionSnapshot).GetProperty(
                flag.Name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.True(captured is not null, $"FontCollectionSnapshot does not capture FontCollection.{flag.Name}");
            Assert.Equal(typeof(bool), captured!.PropertyType);
            Assert.Equal(true, captured.GetValue(raised));
            Assert.Equal(false, captured.GetValue(cleared));
        }
    }

    [Fact]
    public void FormAppearanceFontResolution_UsesTheSceneSnapshot()
    {
        const string LateFamily = "LateRegistered";
        var document = new Document();
        Page(document).Blocks.AddParagraph("Body");
        var renderer = new DocumentRenderer();
        renderer.FormFields.Add(new TextFieldDefinition("name")
        {
            Value = "Ada",
            X = 20,
            Y = 20,
            Width = 100,
            Height = 20,
            Font = new Font { Family = LateFamily },
        });

        var laidOut = DocumentLayouter.Layout(document);
        var settings = CapturedRendererSettings.Capture(renderer);
        Assert.Null(settings.Document.Fonts);

        document.Fonts.Register(
            LateFamily,
            new MemoryStream(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));

        var capturedError = Assert.Throws<NotSupportedException>(
            () => DocumentGenerator.Generate(settings, laidOut));
        Assert.Contains(
            "is not one of the base-14 families",
            capturedError.Message,
            StringComparison.Ordinal);

        var recapturedError = Assert.Throws<NotSupportedException>(
            () => Render(DocumentLayouter.Layout(document), document, renderer));
        Assert.Contains(
            "is registered as an embeddable font file",
            recapturedError.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReusedImageArray_IsCopiedOncePerScene()
    {
        var document = new Document();
        var section = Page(document, 500, 700);
        var data = PdfTestResources.ReadAllBytes("Images/rgb.jpg");
        section.Blocks.Add(new Image(data));
        section.Blocks.Add(new Image(data));

        var paints = Assert.Single(DocumentLayouter.Layout(document).Pages).Body.Images;

        Assert.Equal(2, paints.Length);
        Assert.Same(paints[0].Paint.Data, paints[1].Paint.Data);
    }

    [Fact]
    public void MutatingPaintModelAfterLayout_DoesNotChangeTheLaidOutDocumentOrItsPdf()
    {
        var document = new Document();
        var section = Page(document, 500, 700);
        var run = section.Blocks.AddParagraph().Inlines.Add("AVATAR");
        run.Font.Size = 16;
        run.Font.Bold = true;
        run.Font.Color = Color.FromRgb(10, 20, 30);
        run.Opacity = 0.8;
        var inlineImage = section.Blocks[0] is Paragraph paragraph
            ? paragraph.Inlines.AddImage(PdfTestResources.Open("Images/rgb.jpg"))
            : throw new InvalidOperationException();

        var gradient = new LinearGradient(
            0, 0, 120, 0,
            new GradientStop(0, Color.FromRgb(255, 0, 0)),
            new GradientStop(1, Color.FromRgb(0, 0, 255)));
        var shadow = new BoxShadow
        {
            Color = Color.FromArgb(120, 1, 2, 3),
            BlurRadius = Unit.FromPoint(3),
            OffsetX = Unit.FromPoint(2),
            OffsetY = Unit.FromPoint(1),
            Spread = Unit.FromPoint(1),
        };
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(5),
            BackgroundGradient = gradient,
            Shadow = shadow,
        });
        container.Blocks.AddParagraph("box");

        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Opacity = 0.7;
        image.Interpolate = true;

        var watermark = new Watermark { Text = "DRAFT", Opacity = 0.2, Rotation = 30 };
        watermark.Font.Family = "Courier";
        watermark.Font.Size = 40;
        var watermarkImage = watermark.SetImage(PdfTestResources.Open("Images/gray.jpg"));
        watermarkImage.Opacity = 0.6;
        watermarkImage.Interpolate = true;
        section.Watermark = watermark;

        var laidOut = DocumentLayouter.Layout(document);
        var page = Assert.Single(laidOut.Pages);
        var fragmentPaint = page.Body.Lines[0].Line.Fragments[0].Paint;
        var imagePaint = Assert.Single(page.Body.Images).Paint;
        var boxStyle = Assert.Single(page.Body.Boxes).Style;
        var watermarkPaint = page.Watermark;
        Assert.NotNull(watermarkPaint);
        var before = Render(laidOut, document);

        run.Text = "Changed";
        run.Opacity = 0.1;
        run.Font.Family = "Times-Roman";
        run.Font.Size = 8;
        run.Font.Bold = false;
        run.Font.Italic = true;
        run.Font.Color = Color.FromRgb(200, 201, 202);
        image.Opacity = 0.1;
        image.Interpolate = false;
        gradient.ExtendStart = false;
        gradient.ExtendEnd = false;
        shadow.Color = Color.FromArgb(20, 9, 8, 7);
        shadow.BlurRadius = Unit.FromPoint(20);
        shadow.OffsetX = Unit.FromPoint(12);
        shadow.OffsetY = Unit.FromPoint(13);
        shadow.Spread = Unit.FromPoint(14);
        watermark.Text = "CHANGED";
        watermark.Opacity = 0.9;
        watermark.Rotation = 75;
        watermark.Font.Family = "Helvetica";
        watermark.Font.Size = 12;
        watermark.Font.Color = Color.FromRgb(4, 5, 6);
        watermarkImage.Opacity = 0.2;
        watermarkImage.Interpolate = false;
        image.Data[image.Data.Length / 2] ^= 0x5A;
        inlineImage.Data[inlineImage.Data.Length / 2] ^= 0x3C;
        watermarkImage.Data[watermarkImage.Data.Length / 2] ^= 0xA5;
        document.Fonts.EnableKerning = true;

        Assert.Equal(fragmentPaint, page.Body.Lines[0].Line.Fragments[0].Paint);
        Assert.Equal(imagePaint, Assert.Single(page.Body.Images).Paint);
        Assert.Equal(boxStyle, Assert.Single(page.Body.Boxes).Style);
        Assert.Equal(watermarkPaint, page.Watermark);
        Assert.Equal(before, Render(laidOut, document));
    }

    [Fact]
    public void StructurallyMutatingModelAfterLayout_DoesNotChangeTheLaidOutPdfUaOutput()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Captured semantics";
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Add("Lead");
        var section = Page(document, 500, 700);

        var lead = BuildTestSupport.AddText(section, "Lead", BuildTestSupport.Latin);
        lead.StyleName = "Lead";
        var body = BuildTestSupport.AddText(section, "Body", BuildTestSupport.Latin);
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.AlternateText = "Original alternate text";
        var list = section.Blocks.AddList();
        list.Font.Family = BuildTestSupport.Latin;
        list.AddItem("First").Font.Family = BuildTestSupport.Latin;

        var renderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        renderer.RoleMap.Add("Lead", "P");
        var laidOut = DocumentLayouter.Layout(document);
        var immediate = Render(laidOut, document, renderer);

        section.Blocks.Move(3, 0);
        Assert.True(section.Blocks.Remove(lead));
        Assert.True(section.Blocks.Remove(image));
        body.StyleName = "Heading1";
        lead.StyleName = "Heading2";
        image.AlternateText = "Mutated alternate text";
        image.ActualText = "Mutated actual text";
        document.Language = "fr";

        Assert.Equal(immediate, Render(laidOut, document, renderer));
    }

    [Fact]
    public void MutatingDocumentInfoAfterLayout_DoesNotChangeTheLaidOutPdf()
    {
        var document = new Document();
        var section = Page(document, 500, 700);
        BuildTestSupport.RegisterLatin(document);
        BuildTestSupport.AddText(section, "Body", BuildTestSupport.Latin);
        document.Info.Title = "Captured title";
        document.Info.Author = "Captured author";
        document.Info.Subject = "Captured subject";
        document.Info.Keywords = "captured";
        document.Info.Creator = "Captured creator";
        document.Info.CreationDate = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);
        document.Info.ModificationDate = new DateTimeOffset(2026, 3, 16, 12, 0, 0, TimeSpan.Zero);

        var laidOut = DocumentLayouter.Layout(document);
        var before = Render(laidOut, document);
        Assert.Contains("Captured title", System.Text.Encoding.Latin1.GetString(before), StringComparison.Ordinal);

        document.Info.Title = "Mutated title";
        document.Info.Author = "Mutated author";
        document.Info.Subject = "Mutated subject";
        document.Info.Keywords = "mutated";
        document.Info.Creator = "Mutated creator";
        document.Info.CreationDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        document.Info.ModificationDate = new DateTimeOffset(2020, 1, 2, 0, 0, 0, TimeSpan.Zero);

        Assert.Equal(before, Render(laidOut, document));
    }

    private static byte[] Render(
        LaidOutDocument laidOut,
        Document document,
        DocumentRenderer? renderer = null)
    {
        var settings = CapturedRendererSettings.Capture(renderer ?? new DocumentRenderer());
        return DocumentGenerator.Generate(settings, laidOut).ToArray();
    }

    private static bool Opaque(PropertyInfo property)
        => property.GetIndexParameters().Length != 0;

    private static System.Type Unwrap(System.Type type)
        => Nullable.GetUnderlyingType(type) ?? type;

    private static bool IsCollection(System.Type type)
        => type != typeof(string) && type != typeof(byte[]) && typeof(IEnumerable).IsAssignableFrom(type);

    private static bool IsImmutableArray(System.Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>);

    private static IReadOnlyList<string> ReachableGraphOffenders(object root)
    {
        var offenders = new List<string>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);

        void Visit(object? value, System.Type? owner, string path)
        {
            if (value is null)
            {
                return;
            }

            var type = value.GetType();
            if (type == typeof(string)
                || type.IsPrimitive
                || type.IsEnum
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(TimeSpan)
                || type == typeof(Guid)
                || type == typeof(System.Type))
            {
                return;
            }

            if (value is byte[])
            {
                if (owner != typeof(SceneImageData) && owner != typeof(FontSourceData))
                {
                    offenders.Add($"{path} is a byte[] outside a scene-owned wrapper");
                }

                return;
            }

            if (type == typeof(Radzen.Documents.Fonts.Sfnt.SfntFont))
            {
                Radzen.Blazor.Documents.Tests.SharedSfntFontConcurrencyTests.AssertNamedLazyCacheInvariant();
                return;
            }

            if (value is Block
                || value is Run
                || value is Cell
                || value is Row
                || value is Section
                || value is Watermark
                || value is Document
                || value is FontCollection)
            {
                offenders.Add($"{path} reaches mutable model type {type.FullName}");
                return;
            }

            if (!type.IsValueType && !seen.Add(value))
            {
                return;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
            {
                var index = 0;
                foreach (var item in (IEnumerable)value)
                {
                    Visit(item, type, $"{path}[{index++}]");
                }

                return;
            }

            if (value is IEnumerable)
            {
                offenders.Add($"{path} reaches mutable collection {type.FullName}");
                return;
            }

            if (type.Assembly != typeof(LaidOutDocument).Assembly)
            {
                return;
            }

            foreach (var field in type.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                Visit(field.GetValue(value), type, $"{path}.{field.Name}");
            }
        }

        Visit(root, null, nameof(LaidOutDocument));
        return offenders;
    }

    private static IEnumerable<System.Type> LaidOutTypes()
    {
        var pending = new Queue<System.Type>([typeof(LaidOutDocument)]);
        var seen = new HashSet<System.Type> { typeof(LaidOutDocument) };
        while (pending.Count > 0)
        {
            var type = pending.Dequeue();
            yield return type;

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (Opaque(property))
                {
                    continue;
                }

                var declared = Unwrap(property.PropertyType);
                var candidate = IsImmutableArray(declared) ? declared.GetGenericArguments()[0] : declared;
                if (IsLaidOutType(candidate) && seen.Add(candidate))
                {
                    pending.Enqueue(candidate);
                }
            }
        }
    }

    private static bool IsLaidOutType(System.Type type)
        => type.Namespace == typeof(LaidOutDocument).Namespace
            || type == typeof(FontCollectionSnapshot)
            || type == typeof(RegisteredFace);
}
