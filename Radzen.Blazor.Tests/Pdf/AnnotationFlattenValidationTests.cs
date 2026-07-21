#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class AnnotationFlattenValidationTests
{
    private static byte[] LoadedLaunchLinkPdf()
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [4 0 R] >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Type /Annot /Subtype /Link /Rect [10 10 100 30] "
            + "/A << /S /Launch /F (x.pdf) >> >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 5\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 5; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 5 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void LoadedLinkWithUnsupportedAction_FlattensWithoutThrowing()
    {
        var document = Document.LoadFromStream(new MemoryStream(LoadedLaunchLinkPdf()));

        document.Flatten();

        Assert.Empty(document.Pages[0].Annotations);
    }

    private static byte[] LoadedHighlightPdf()
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [4 0 R] >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Type /Annot /Subtype /Highlight /Rect [10 10 110 30] "
            + "/QuadPoints [10 30 110 30 10 10 110 10] /C [1 1 0] >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 5\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 5; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 5 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void LoadedModifiedInvalidAnnotation_IsRejectedOnFlattenLikeOnSave()
    {
        var document = Document.LoadFromStream(new MemoryStream(LoadedHighlightPdf()));
        var markup = Assert.IsType<HighlightAnnotation>(document.Pages[0].Annotations[0]);
        markup.Areas.Clear();

        Assert.Throws<InvalidOperationException>(document.Flatten);
    }

    private static string FlattenedContent(Document document)
    {
        document.Flatten();
        var reader = DocumentReader.Parse(document.ToArray());
        var page = DocumentLoadTests.Kid(reader, 0);
        return page.ContainsKey("Contents")
            ? Encoding.ASCII.GetString(DocumentLoadTests.KidContent(reader, 0))
            : string.Empty;
    }

    private static Document InvalidMarkup()
    {
        var document = new Document();
        var markup = document.Pages.Add().Annotations.Add(new HighlightAnnotation(PdfRect.FromSize(40, 50, 100, 12)));
        markup.Areas.Add(PdfRect.FromSize(30, 50, 20, 12));
        return document;
    }

    [Fact]
    public void VisibleAnnotation_IsPainted()
    {
        var document = new Document();
        document.Pages.Add().Annotations.Add(
            new HighlightAnnotation(PdfRect.FromSize(20, 30, 100, 15)) { Color = Color.Yellow });

        Assert.Contains("1 1 0 rg", FlattenedContent(document), StringComparison.Ordinal);
    }

    [Fact]
    public void TranslucentAnnotation_FlattensWithItsOpacity()
    {
        var opaque = new Document();
        opaque.Pages.Add().Annotations.Add(
            new HighlightAnnotation(PdfRect.FromSize(20, 30, 100, 15)) { Color = Color.Yellow });

        var translucent = new Document();
        translucent.Pages.Add().Annotations.Add(
            new HighlightAnnotation(PdfRect.FromSize(20, 30, 100, 15)) { Color = Color.Yellow, Opacity = 0.4 });

        Assert.DoesNotContain(" gs", FlattenedContent(opaque), StringComparison.Ordinal);
        Assert.Contains(" gs", FlattenedContent(translucent), StringComparison.Ordinal);
    }

    [Fact]
    public void HiddenAnnotation_IsClearedButNotPainted()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Annotations.Add(new HighlightAnnotation(PdfRect.FromSize(20, 30, 100, 15))
        {
            Color = Color.Yellow,
            Flags = AnnotationFlags.Hidden,
        });

        var content = FlattenedContent(document);

        Assert.Empty(page.Annotations);
        Assert.DoesNotContain("1 1 0 rg", content, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidMarkupArea_IsRejectedOnFlattenLikeOnSave()
    {
        var flatten = Assert.Throws<InvalidOperationException>(() => InvalidMarkup().Flatten());
        var save = Assert.Throws<InvalidOperationException>(() => InvalidMarkup().ToArray());

        Assert.Equal("Markup areas must be contained within the annotation bounds.", flatten.Message);
        Assert.Equal(flatten.Message, save.Message);
    }

    [Fact]
    public void EmptyStampName_IsRejectedOnFlattenLikeOnSave()
    {
        var document = new Document();
        document.Pages.Add().Annotations.Add(new StampAnnotation(PdfRect.FromSize(40, 160, 80, 30)) { Name = "  " });

        var exception = Assert.Throws<InvalidOperationException>(document.Flatten);
        Assert.Equal("A stamp annotation requires a non-empty name.", exception.Message);
    }
}
