#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class AnnotationTests
{
    [Fact]
    public void CreatedAnnotationKinds_SaveAndReloadTheirDeclarativeState()
    {
        var document = new Document();
        var page = document.Pages.Add();
        document.Pages.Add();
        page.Annotations.Add(new TextAnnotation(PdfRect.FromSize(10, 20, 24, 24)) { Contents = "note", Title = "author" });
        page.Annotations.Add(new HighlightAnnotation(PdfRect.FromSize(40, 50, 100, 12)) { Contents = "highlight" });
        page.Annotations.Add(new UnderlineAnnotation(PdfRect.FromSize(40, 70, 100, 12)));
        page.Annotations.Add(new StrikeOutAnnotation(PdfRect.FromSize(40, 90, 100, 12)));
        page.Annotations.Add(new SquigglyAnnotation(PdfRect.FromSize(40, 110, 100, 12)));
        page.Annotations.Add(new LinkAnnotation(PdfRect.FromSize(40, 130, 100, 12)) { Uri = new Uri("https://example.com/") });
        page.Annotations.Add(new LinkAnnotation(PdfRect.FromSize(40, 145, 100, 12)) { TargetPageIndex = 1 });
        page.Annotations.Add(new StampAnnotation(PdfRect.FromSize(40, 160, 80, 30)) { Name = "Approved" });
        var ink = page.Annotations.Add(new InkAnnotation(PdfRect.FromSize(40, 190, 100, 50)));
        ink.Strokes.Add(new InkStroke { new AnnotationPoint(40, 190), new AnnotationPoint(80, 220) });
        page.Annotations.Add(new FreeTextAnnotation(PdfRect.FromSize(40, 250, 120, 30)) { Contents = "free text", FontSize = 11 });
        page.Annotations.Add(new SquareAnnotation(PdfRect.FromSize(40, 290, 50, 50)) { InteriorColor = Color.Yellow });
        page.Annotations.Add(new CircleAnnotation(PdfRect.FromSize(110, 290, 50, 50)) { InteriorColor = Color.LightGray });

        var loaded = Load(document.ToArray());

        Assert.Collection(loaded.Pages[0].Annotations,
            value => Assert.IsType<TextAnnotation>(value),
            value => Assert.Equal(100, Assert.IsType<HighlightAnnotation>(value).Areas[0].Width),
            value => Assert.IsType<UnderlineAnnotation>(value),
            value => Assert.IsType<StrikeOutAnnotation>(value),
            value => Assert.IsType<SquigglyAnnotation>(value),
            value => Assert.Equal("https://example.com/", Assert.IsType<LinkAnnotation>(value).Uri?.AbsoluteUri),
            value => Assert.Equal(1, Assert.IsType<LinkAnnotation>(value).TargetPageIndex),
            value => Assert.Equal("Approved", Assert.IsType<StampAnnotation>(value).Name),
            value => Assert.Single(Assert.IsType<InkAnnotation>(value).Strokes),
            value => Assert.Equal("free text", Assert.IsType<FreeTextAnnotation>(value).Contents),
            value => Assert.Equal(Color.Yellow, Assert.IsType<SquareAnnotation>(value).InteriorColor),
            value => Assert.Equal(Color.LightGray, Assert.IsType<CircleAnnotation>(value).InteriorColor));
    }

    [Fact]
    public void LoadedAnnotations_CanEditAndRemove_WhileUnknownAnnotationIsPreserved()
    {
        var source = new Document();
        var page = source.Pages.Add();
        page.Annotations.Add(new TextAnnotation(PdfRect.FromSize(10, 10, 20, 20)) { Contents = "old" });
        page.Annotations.Add(new SquareAnnotation(PdfRect.FromSize(40, 10, 20, 20)));
        var bytes = AddUnknownAnnotation(source.ToArray());
        var loaded = Load(bytes);

        Assert.Equal(2, loaded.Pages[0].Annotations.Count);
        Assert.IsType<TextAnnotation>(loaded.Pages[0].Annotations[0]).Contents = "edited";
        loaded.Pages[0].Annotations.RemoveAt(1);

        var saved = loaded.ToArray();
        var reloaded = Load(saved);
        Assert.Single(reloaded.Pages[0].Annotations);
        Assert.Equal("edited", Assert.IsType<TextAnnotation>(reloaded.Pages[0].Annotations[0]).Contents);
        var reader = DocumentReader.Parse(saved);
        Assert.Contains(PageAnnotations(reader), value =>
            reader.AsDictionary(value) is { } dictionary
            && reader.GetName(dictionary, "Subtype") == "Caret"
            && reader.GetString(dictionary, "Custom") == "keep-me");
    }

    [Fact]
    public void Flatten_BurnsAnnotationAppearanceAndRemovesInteractivity()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Annotations.Add(new HighlightAnnotation(PdfRect.FromSize(20, 30, 100, 15)) { Color = Color.Yellow });

        document.Flatten();
        var bytes = document.ToArray();

        Assert.Empty(page.Annotations);
        Assert.False(DocumentLoadTests.Kid(DocumentReader.Parse(bytes), 0).ContainsKey("Annots"));
        Assert.Contains(" rg", Encoding.ASCII.GetString(page.GetContent() ?? DocumentLoadTests.KidContent(DocumentReader.Parse(bytes), 0)), StringComparison.Ordinal);
    }

    [Fact]
    public void Flatten_LoadedAnnotationBurnsItsNormalAppearance()
    {
        var source = new Document();
        source.Pages.Add().Annotations.Add(new SquareAnnotation(PdfRect.FromSize(20, 30, 100, 40))
        {
            Color = Color.Red,
            InteriorColor = Color.Yellow,
        });
        var loaded = Load(source.ToArray());

        loaded.Flatten();
        var bytes = loaded.ToArray();
        var reader = DocumentReader.Parse(bytes);
        var page = DocumentLoadTests.Kid(reader, 0);

        Assert.Empty(Load(bytes).Pages[0].Annotations);
        Assert.IsType<NullObject>(page["Annots"]);
        Assert.Contains("Do", Encoding.ASCII.GetString(DocumentLoadTests.KidContent(reader, 0)), StringComparison.Ordinal);
        Assert.NotNull(reader.GetDictionary(Assert.IsType<DictionaryObject>(page["Resources"]), "XObject"));
    }

    [Fact]
    public void Flatten_PagesInheritingOneResources_DoNotShareEachOthersAppearances()
    {
        var loaded = Load(SharedResourcesWithAppearances());

        loaded.Flatten();
        var reader = DocumentReader.Parse(loaded.ToArray());

        Assert.Equal(new[] { "AFlatten" }, XObjectNames(reader, 0));
        Assert.Equal(new[] { "AFlatten" }, XObjectNames(reader, 1));
    }

    [Fact]
    public void Flatten_DoesNotChangeWhatAnotherDocumentHoldingTheSamePagesSaves()
    {
        var first = Load(SharedResourcesWithAppearances());
        var second = new Document();
        second.Append(first);
        var expected = second.ToArray();

        first.Flatten();

        Assert.Equal(expected, second.ToArray());
    }

    private static string[] XObjectNames(DocumentReader reader, int index)
    {
        var resources = Assert.IsType<DictionaryObject>(reader.Resolve(DocumentLoadTests.Kid(reader, index)["Resources"]));
        var xobjects = reader.GetDictionary(resources, "XObject");
        return xobjects is null ? [] : [.. xobjects.Keys];
    }

    private static byte[] SharedResourcesWithAppearances()
    {
        var content = Encoding.ASCII.GetBytes("q Q");
        var appearance = Encoding.ASCII.GetBytes("1 0 0 RG 0 0 20 20 re S");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 2 /Kids [3 0 R 4 0 R] /MediaBox [0 0 612 792] "
            + "/Resources 5 0 R >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /Contents 8 0 R /Annots [6 0 R] >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Type /Page /Parent 2 0 R /Contents 9 0 R /Annots [7 0 R] >>\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< >>\nendobj\n");
        pdf.Object(6, "6 0 obj\n<< /Type /Annot /Subtype /Square /Rect [10 10 30 30] /AP << /N 10 0 R >> >>\nendobj\n");
        pdf.Object(7, "7 0 obj\n<< /Type /Annot /Subtype /Square /Rect [40 40 60 60] /AP << /N 11 0 R >> >>\nendobj\n");
        for (var i = 8; i <= 9; i++)
        {
            pdf.Mark(i);
            pdf.Append(i + " 0 obj\n<< /Length " + content.Length + " >>\nstream\n").Append(content).Append("\nendstream\nendobj\n");
        }

        for (var i = 10; i <= 11; i++)
        {
            pdf.Mark(i);
            pdf.Append(i + " 0 obj\n<< /Type /XObject /Subtype /Form /BBox [0 0 20 20] /Length "
                + appearance.Length + " >>\nstream\n").Append(appearance).Append("\nendstream\nendobj\n");
        }

        var xref = pdf.Position;
        pdf.Append("xref\n0 12\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 12; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 12 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void EmptyAnnotationCollection_DoesNotChangeOutput()
    {
        var first = new Document();
        first.Pages.Add().SetContent(Encoding.ASCII.GetBytes("plain"));
        var expected = first.ToArray();

        var second = new Document();
        var page = second.Pages.Add();
        page.SetContent(Encoding.ASCII.GetBytes("plain"));
        _ = page.Annotations.Count;

        Assert.Equal(expected, second.ToArray());
    }

    [Fact]
    public void TextMarkup_EmitsUpperQuadPointsBeforeLowerQuadPoints()
    {
        var document = new Document();
        document.Pages.Add().Annotations.Add(new HighlightAnnotation(PdfRect.FromSize(40, 50, 100, 12)));

        var reader = DocumentReader.Parse(document.ToArray());
        var dictionary = Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(PageAnnotations(reader))));
        var points = Assert.IsType<ArrayObject>(dictionary["QuadPoints"]);

        Assert.Equal([40, 62, 140, 62, 40, 50, 140, 50], Numbers(reader, points));
    }

    [Fact]
    public void InkAppearance_BoundsEncloseStrokePointsOutsideAnnotationBounds()
    {
        var document = new Document();
        var ink = document.Pages.Add().Annotations.Add(new InkAnnotation(PdfRect.FromSize(40, 190, 100, 50)));
        ink.Strokes.Add(new InkStroke { new AnnotationPoint(20, 180), new AnnotationPoint(160, 260) });

        var reader = DocumentReader.Parse(document.ToArray());
        var dictionary = Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(PageAnnotations(reader))));
        var appearances = reader.GetDictionary(dictionary, "AP")!;
        var appearance = Assert.IsType<StreamObject>(reader.Resolve(appearances["N"]));
        var box = Assert.IsType<ArrayObject>(appearance.Dictionary["BBox"]);

        Assert.Equal([-20, -10, 120, 70], Numbers(reader, box));
    }

    [Fact]
    public void MarkupAreaOutsideBounds_ThrowsInsteadOfClipping()
    {
        var document = new Document();
        var markup = document.Pages.Add().Annotations.Add(new HighlightAnnotation(PdfRect.FromSize(40, 50, 100, 12)));
        markup.Areas.Add(PdfRect.FromSize(30, 50, 20, 12));

        var exception = Assert.Throws<InvalidOperationException>(() => document.ToArray());

        Assert.Equal("Markup areas must be contained within the annotation bounds.", exception.Message);
    }

    [Fact]
    public void EditedLink_PreservesNamedDestinationObjectType()
    {
        var source = new Document();
        source.Pages.Add().Annotations.Add(new LinkAnnotation(PdfRect.FromSize(10, 20, 100, 12)) { Destination = "chapter-one" });
        var bytes = ChangeLinkDestinationToName(source.ToArray());
        var document = Load(bytes);
        Assert.IsType<LinkAnnotation>(document.Pages[0].Annotations[0]).Contents = "edited";

        var reader = DocumentReader.Parse(document.ToArray());
        var dictionary = Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(PageAnnotations(reader))));
        var action = reader.GetDictionary(dictionary, "A")!;

        Assert.Equal("chapter-one", Assert.IsType<NameObject>(action["D"]).Value);
    }

    private static Document Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return Document.LoadFromStream(stream);
    }

    private static ArrayObject PageAnnotations(DocumentReader reader)
        => Assert.IsType<ArrayObject>(reader.Resolve(DocumentLoadTests.Kid(reader, 0)["Annots"]));

    private static double[] Numbers(DocumentReader reader, ArrayObject values)
    {
        var result = new double[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            result[i] = Assert.IsType<NumberObject>(reader.Resolve(values[i])).DoubleValue;
        }

        return result;
    }

    private static byte[] ChangeLinkDestinationToName(byte[] bytes)
    {
        var document = Load(bytes);
        var state = document.Loaded!;
        var reader = state.Source!;
        var sourceNode = state.SourcePages[document.Pages[0]];
        var annotation = reader.AsDictionary(Assert.Single(reader.GetArray(sourceNode, "Annots")!))!;
        var action = reader.GetDictionary(annotation, "A")!;
        action["D"] = new NameObject("chapter-one");
        return document.ToArray();
    }

    private static byte[] AddUnknownAnnotation(byte[] bytes)
    {
        var document = Load(bytes);
        var state = document.Loaded!;
        var page = document.Pages[0];
        var sourceNode = state.SourcePages[page];
        var annotations = state.Source!.GetArray(sourceNode, "Annots")!;
        annotations.Add(new DictionaryObject
        {
            ["Type"] = new NameObject("Annot"),
            ["Subtype"] = new NameObject("Caret"),
            ["Rect"] = new ArrayObject { new NumberObject(70), new NumberObject(10), new NumberObject(90), new NumberObject(30) },
            ["Custom"] = new StringObject("keep-me"),
        });
        return document.ToArray();
    }
}
