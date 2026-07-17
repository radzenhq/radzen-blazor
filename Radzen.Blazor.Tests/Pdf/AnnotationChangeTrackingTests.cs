#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Annotations used to answer "did the caller change this since load" by serialising every
// property into a pipe-delimited string and comparing it. These pin the dirty bit that
// replaced it, including the blind spots the string had.
public class AnnotationChangeTrackingTests
{
    private static Document Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return Document.LoadFromStream(stream);
    }

    private static Document Loaded(Annotation annotation)
    {
        var source = new Document();
        source.Pages.Add().Annotations.Add(annotation);
        return Load(source.ToArray());
    }

    [Fact]
    public void LoadedAnnotationStartsClean()
    {
        var loaded = Loaded(new SquareAnnotation(PdfRect.FromSize(20, 30, 100, 40)) { Color = Color.Red });

        Assert.False(loaded.Pages[0].Annotations[0].IsModified);
        Assert.False(loaded.Pages[0].Annotations.HasChanges);
    }

    [Fact]
    public void EditingALoadedAnnotationMarksItModified()
    {
        var loaded = Loaded(new SquareAnnotation(PdfRect.FromSize(20, 30, 100, 40)));

        loaded.Pages[0].Annotations[0].Color = Color.Blue;

        Assert.True(loaded.Pages[0].Annotations[0].IsModified);
        Assert.True(loaded.Pages[0].Annotations.HasChanges);
    }

    // The string compared Appearance.Content.Count, not the content, so an element edit that
    // left the count alone was invisible and the save was silently dropped. The element's own
    // flag is what closes that.
    [Fact]
    public void EditingAnAppearanceElementMarksTheAnnotationModified()
    {
        var annotation = new SquareAnnotation(PdfRect.FromSize(20, 30, 100, 40));
        var appearance = new AnnotationAppearance();
        var path = appearance.Content.Add(new PathContent { FillColor = Color.Red });
        annotation.Appearance = appearance;
        annotation.AcceptChanges();
        Assert.False(annotation.IsModified);

        path.FillColor = Color.Blue;

        Assert.True(annotation.IsModified);
    }

    // The container-level truth: removing an element leaves every survivor clean.
    [Fact]
    public void RemovingAnAppearanceElementMarksTheAnnotationModified()
    {
        var annotation = new SquareAnnotation(PdfRect.FromSize(20, 30, 100, 40));
        var appearance = new AnnotationAppearance();
        var path = appearance.Content.Add(new PathContent { FillColor = Color.Red });
        appearance.Content.Add(new PathContent { FillColor = Color.Green });
        annotation.Appearance = appearance;
        annotation.AcceptChanges();

        appearance.Content.Remove(path);

        Assert.True(annotation.IsModified);
    }

    // A removed annotation is a container change: no surviving annotation is touched by it.
    [Fact]
    public void RemovingALoadedAnnotationIsAChange()
    {
        var loaded = Loaded(new SquareAnnotation(PdfRect.FromSize(20, 30, 100, 40)));
        var annotations = loaded.Pages[0].Annotations;
        Assert.False(annotations.HasChanges);

        annotations.RemoveAt(0);

        Assert.True(annotations.HasChanges);
    }

    [Fact]
    public void EditingAMarkupAreaMarksTheAnnotationModified()
    {
        var loaded = Loaded(new HighlightAnnotation(PdfRect.FromSize(40, 100, 100, 12)));
        var annotation = (HighlightAnnotation)loaded.Pages[0].Annotations[0];
        Assert.False(annotation.IsModified);

        annotation.Areas.Add(PdfRect.FromSize(40, 80, 60, 12));

        Assert.True(annotation.IsModified);
    }

    [Fact]
    public void EditingAnInkStrokeMarksTheAnnotationModified()
    {
        var ink = new InkAnnotation(PdfRect.FromSize(10, 10, 100, 100)) { StrokeWidth = 2 };
        var stroke = new InkStroke { new AnnotationPoint(10, 10), new AnnotationPoint(20, 20) };
        ink.Strokes.Add(stroke);
        var loaded = Loaded(ink);
        var annotation = (InkAnnotation)loaded.Pages[0].Annotations[0];
        Assert.False(annotation.IsModified);

        annotation.Strokes[0].Add(new AnnotationPoint(30, 30));

        Assert.True(annotation.IsModified);
    }

    // The deliberate cost of the unconditional Set<T>, and the one behavior change of the fold:
    // the string compare found a self-assignment identical and preserved the original bytes,
    // where the bit re-encodes. It fails in the safe direction - a spurious re-encode, never a
    // dropped edit - and matches what ContentElement has always done.
    [Fact]
    public void AssigningAPropertyToItsOwnValueMarksTheAnnotationModified()
    {
        var loaded = Loaded(new SquareAnnotation(PdfRect.FromSize(20, 30, 100, 40)));
        var annotation = loaded.Pages[0].Annotations[0];

        annotation.Color = annotation.Color;

        Assert.True(annotation.IsModified);
    }

    [Fact]
    public void EditingAFreeTextFontThroughTheInstanceMarksTheAnnotationModified()
    {
        var loaded = Loaded(new FreeTextAnnotation(PdfRect.FromSize(20, 30, 100, 40)) { Contents = "hi" });
        var annotation = (FreeTextAnnotation)loaded.Pages[0].Annotations[0];
        Assert.False(annotation.IsModified);

        annotation.FontSize = 22;

        Assert.True(annotation.IsModified);
    }
}
