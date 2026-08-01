#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class AnnotationFidelityTests
{
    private static PortableDocument Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return PortableDocument.LoadFromStream(stream);
    }

    private static ArrayObject PageAnnotations(DocumentReader reader)
        => Assert.IsType<ArrayObject>(reader.Resolve(DocumentLoadTests.Kid(reader, 0)["Annots"]));

    private static string AppearanceText(DocumentReader reader, DictionaryObject annotation)
    {
        var appearance = reader.GetDictionary(annotation, "AP")!;
        return FormTestSupport.Decode(Assert.IsType<StreamObject>(reader.Resolve(appearance["N"])));
    }

    private static T TwoLines<T>(T annotation) where T : MarkupAnnotation
    {
        annotation.Areas.Clear();
        annotation.Areas.Add(PdfRect.FromSize(40, 130, 100, 12));
        annotation.Areas.Add(PdfRect.FromSize(40, 100, 60, 12));
        return annotation;
    }

    private static string MarkupAppearance(MarkupAnnotation annotation)
    {
        var document = new PortableDocument();
        document.Pages.Add().Annotations.Add(annotation);
        var reader = DocumentReader.Parse(document.ToArray());
        return AppearanceText(reader, Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(PageAnnotations(reader)))));
    }

    [Fact]
    public void HighlightAppearancePaintsOneRectanglePerArea()
    {
        var content = MarkupAppearance(TwoLines(new HighlightAnnotation(PdfRect.FromSize(40, 100, 100, 42))));

        Assert.Equal(2, Occurrences(content, "\nf\n"));
        Assert.Contains("0 30 m", content);
        Assert.Contains("0 0 m", content);
        Assert.Contains("60 12 l", content);
    }

    [Fact]
    public void UnderlineAppearanceDrawsOneLinePerArea()
    {
        var content = MarkupAppearance(TwoLines(new UnderlineAnnotation(PdfRect.FromSize(40, 100, 100, 42))));

        Assert.Equal(2, Occurrences(content, "\nS\n"));
        Assert.Contains("0 31 m", content);
        Assert.Contains("0 1 m", content);
        Assert.Contains("60 1 l", content);
    }

    [Fact]
    public void StrikeOutAppearanceDrawsOneLinePerArea()
    {
        var content = MarkupAppearance(TwoLines(new StrikeOutAnnotation(PdfRect.FromSize(40, 100, 100, 42))));

        Assert.Equal(2, Occurrences(content, "\nS\n"));
        Assert.Contains("0 36 m", content);
        Assert.Contains("0 6 m", content);
    }

    [Fact]
    public void SquigglyAppearanceDrawsOnePathPerArea()
    {
        var content = MarkupAppearance(TwoLines(new SquigglyAnnotation(PdfRect.FromSize(40, 100, 100, 42))));

        Assert.Equal(2, Occurrences(content, "\nS\n"));
        Assert.Contains("0 31 m", content);
        Assert.Contains("0 1 m", content);
    }

    [Fact]
    public void SingleAreaMarkupAppearanceIsUnchanged()
    {
        var content = MarkupAppearance(new HighlightAnnotation(PdfRect.FromSize(40, 100, 100, 12)));

        Assert.Equal(1, Occurrences(content, "\nf\n"));
        Assert.Contains("0 0 m", content);
        Assert.Contains("100 12 l", content);
    }

    private static int Occurrences(string value, string token)
    {
        var count = 0;
        for (var i = value.IndexOf(token); i >= 0; i = value.IndexOf(token, i + 1))
        {
            count++;
        }

        return count;
    }


    [Fact]
    public void EditedInkKeepsTheLoadedStrokeWidth()
    {
        var source = new PortableDocument();
        var ink = source.Pages.Add().Annotations.Add(new InkAnnotation(PdfRect.FromSize(40, 190, 100, 50)));
        ink.Strokes.Add(new InkStroke { new AnnotationPoint(40, 190), new AnnotationPoint(80, 220) });
        ink.StrokeWidth = 4;

        var document = Load(source.ToArray());
        var loaded = Assert.IsType<InkAnnotation>(document.Pages[0].Annotations[0]);

        Assert.Equal(4, loaded.StrokeWidth);

        loaded.Contents = "edited";
        var reader = DocumentReader.Parse(document.ToArray());
        var dictionary = Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(PageAnnotations(reader))));

        Assert.Equal(4, reader.GetNumber(reader.GetDictionary(dictionary, "BS")!, "W"));
    }

    [Fact]
    public void EditedFreeTextKeepsTheLoadedDefaultAppearance()
    {
        var source = new PortableDocument();
        source.Pages.Add().Annotations.Add(new FreeTextAnnotation(PdfRect.FromSize(40, 250, 120, 30))
        {
            Contents = "free text",
            Font = new Font { Family = "Courier", Size = 18 },
            TextColor = Color.Red,
        });

        var document = Load(source.ToArray());
        var loaded = Assert.IsType<FreeTextAnnotation>(document.Pages[0].Annotations[0]);

        Assert.Equal(18, loaded.Font.Size);
        Assert.Equal("Courier", loaded.Font.Family);
        Assert.Equal(Color.Red, loaded.TextColor);

        loaded.Contents = "edited";
        var reader = DocumentReader.Parse(document.ToArray());
        var dictionary = Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(PageAnnotations(reader))));

        Assert.Equal("/Courier 18 Tf 1 0 0 rg", reader.GetString(dictionary, "DA"));
    }

    [Fact]
    public void LoadedFreeTextWithoutFontInDefaultAppearanceKeepsTheDefaultFont()
    {
        var document = Load(WithDefaultAppearance("0.5 g"));
        var loaded = Assert.IsType<FreeTextAnnotation>(document.Pages[0].Annotations[0]);

        Assert.Equal(new Font().Size, loaded.Font.Size);
        Assert.Equal(Color.FromRgb(128, 128, 128), loaded.TextColor);
    }

    private static byte[] WithDefaultAppearance(string da)
    {
        var source = new PortableDocument();
        source.Pages.Add().Annotations.Add(new FreeTextAnnotation(PdfRect.FromSize(40, 250, 120, 30)) { Contents = "free text" });
        var document = Load(source.ToArray());
        var state = document.Loaded!;
        var annotation = state.Source!.AsDictionary(
            Assert.Single(state.Source.GetArray(state.SourcePages[document.Pages[0]], "Annots")!))!;
        annotation["DA"] = new StringObject(da);
        return document.ToArray();
    }
}
