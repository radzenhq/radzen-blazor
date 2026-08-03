#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class AnnotationChangeTrackingTests
{
    private static PortableDocument Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return PortableDocument.LoadFromStream(stream);
    }

    private static PortableDocument Loaded(Annotation annotation)
    {
        var source = new PortableDocument();
        source.Pages.Add().Annotations.Add(annotation);
        return Load(source.ToArray());
    }

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

    [Fact]
    public void AssigningAPropertyToItsOwnValueLeavesTheAnnotationUnmodified()
    {
        var loaded = Loaded(new SquareAnnotation(PdfRect.FromSize(20, 30, 100, 40)));
        var annotation = loaded.Pages[0].Annotations[0];

        annotation.Color = annotation.Color;

        Assert.False(annotation.IsModified);
    }
}
