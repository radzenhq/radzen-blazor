#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

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
        var document = PortableDocument.LoadFromStream(new MemoryStream(LoadedLaunchLinkPdf()));

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
        var document = PortableDocument.LoadFromStream(new MemoryStream(LoadedHighlightPdf()));
        var markup = Assert.IsType<HighlightAnnotation>(document.Pages[0].Annotations[0]);
        markup.Areas.Clear();

        Assert.Throws<InvalidOperationException>(document.Flatten);
    }

    private static string FlattenedContent(PortableDocument document)
    {
        document.Flatten();
        var emission = Emit(document);
        var contents = Shaped("page", @"/Contents (\d+) 0 R", Line(emission, "/Type /Page "));
        return IndirectObject(emission, contents.Groups[1].Value);
    }

    private static PortableDocument InvalidMarkup()
    {
        var document = new PortableDocument();
        var markup = document.Pages.Add().Annotations.Add(new HighlightAnnotation(PdfRect.FromSize(40, 50, 100, 12)));
        markup.Areas.Add(PdfRect.FromSize(30, 50, 20, 12));
        return document;
    }

    [Fact]
    public void VisibleAnnotation_IsPainted()
    {
        var document = new PortableDocument();
        document.Pages.Add().Annotations.Add(
            new HighlightAnnotation(PdfRect.FromSize(20, 30, 100, 15)) { Color = Color.Yellow });

        Carries("flattened content", "1 1 0 rg", FlattenedContent(document));
    }

    [Fact]
    public void TranslucentAnnotation_FlattensWithItsOpacity()
    {
        var opaque = new PortableDocument();
        opaque.Pages.Add().Annotations.Add(
            new HighlightAnnotation(PdfRect.FromSize(20, 30, 100, 15)) { Color = Color.Yellow });

        var translucent = new PortableDocument();
        translucent.Pages.Add().Annotations.Add(
            new HighlightAnnotation(PdfRect.FromSize(20, 30, 100, 15)) { Color = Color.Yellow, Opacity = 0.4 });

        Lacks("opaque flattened content", " gs\n", FlattenedContent(opaque));
        Carries("translucent flattened content", " gs\n", FlattenedContent(translucent));
    }

    [Fact]
    public void HiddenAnnotation_IsClearedButNotPainted()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Annotations.Add(new HighlightAnnotation(PdfRect.FromSize(20, 30, 100, 15))
        {
            Color = Color.Yellow,
            Flags = AnnotationFlags.Hidden,
        });

        document.Flatten();
        var emission = Emit(document);

        Assert.Empty(page.Annotations);
        Lacks("emission", "1 1 0 rg", emission);
    }

    [Fact]
    public void InvalidMarkupArea_IsRejectedOnFlattenLikeOnSave()
    {
        var flatten = Assert.Throws<InvalidOperationException>(() => InvalidMarkup().Flatten());
        var save = Assert.Throws<InvalidOperationException>(() => InvalidMarkup().ToArray());

        Assert.Contains("Markup areas", flatten.Message, StringComparison.Ordinal);
        Assert.Contains("Markup areas", save.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyStampName_IsRejectedOnFlattenLikeOnSave()
    {
        var document = new PortableDocument();
        document.Pages.Add().Annotations.Add(new StampAnnotation(PdfRect.FromSize(40, 160, 80, 30)) { Name = "  " });

        var exception = Assert.Throws<InvalidOperationException>(document.Flatten);
        Assert.Contains("non-empty name", exception.Message, StringComparison.Ordinal);
    }
}
