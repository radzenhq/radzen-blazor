#nullable enable
using System;
using System.IO;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class LiveGraphConformanceTests
{
    private static Document WithTextInput(string value)
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Form";
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Normal.Font.Family = BuildTestSupport.Latin;

        var paragraph = document.Sections.Add().Blocks.Add(new Paragraph());
        paragraph.Font.Family = BuildTestSupport.Latin;
        paragraph.Inlines.Add(new TextInput("name") { Value = value, Label = "Name" });
        return document;
    }

    private static byte[] Save(PortableDocument document)
    {
        using var stream = new MemoryStream();
        document.SaveToStream(stream);
        return stream.ToArray();
    }

    private static byte[] DocumentWithAnnotation(string annotation)
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /StructTreeRoot 5 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << >> /Annots [4 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n" + annotation + "\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /StructTreeRoot /K [] >>\nendobj\n");
        return FixturePdf.Wrap(pdf, 6);
    }

    private static PortableDocument Accessible(string annotation)
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(DocumentWithAnnotation(annotation)));
        document.Accessibility = PdfUaConformance.PdfUa1;
        document.Language = "en-US";
        document.Info.Title = "Fixture";
        return document;
    }

    [Fact]
    public void UnmodeledAnnotationWithoutStructureUnderPdfUaFailsAtSave()
    {
        var document = Accessible("<< /Type /Annot /Subtype /Polygon /Rect [10 10 40 40] >>");

        var error = Assert.Throws<InvalidOperationException>(() => Save(document));

        Assert.Contains(
            "PDF/UA requires every annotation to be referenced from the structure tree",
            error.Message,
            StringComparison.Ordinal);
        Assert.Contains("/Polygon annotation", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnmodeledAnnotationWithoutStructureUnderPdfUaFailsAtSaveIncremental()
    {
        var document = Accessible("<< /Type /Annot /Subtype /Polygon /Rect [10 10 40 40] >>");

        using var stream = new MemoryStream();
        var error = Assert.Throws<InvalidOperationException>(() => document.SaveIncremental(stream));

        Assert.Contains("/Polygon annotation", error.Message, StringComparison.Ordinal);
        Assert.Equal(0, stream.Length);
    }

    // ISO 14289-1 7.18.1: an annotation whose Hidden flag is set is not content.
    [Fact]
    public void HiddenAnnotationWithoutStructureUnderPdfUaSaves()
    {
        var document = Accessible("<< /Type /Annot /Subtype /Polygon /Rect [10 10 40 40] /F 2 >>");

        Assert.NotEmpty(Save(document));
    }

    [Fact]
    public void UnmodeledAnnotationWithoutStructureWithoutAClaimSaves()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(DocumentWithAnnotation(
            "<< /Type /Annot /Subtype /Polygon /Rect [10 10 40 40] >>")));

        Assert.NotEmpty(Save(document));
    }

    [Fact]
    public void FieldFilledBeforeTheClaimIsAddedFailsAtSave()
    {
        var document = new DocumentRenderer().Render(WithTextInput(string.Empty));
        document.AcroForm!.FillField("name", "Bob");
        document.Conformance = PdfAConformance.PdfA2B;

        var error = Assert.Throws<InvalidOperationException>(() => Save(document));

        Assert.Contains("PDF/A requires every font to be embedded", error.Message, StringComparison.Ordinal);
        Assert.Contains("Helvetica", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FieldFilledUnderNoClaimStillSaves()
    {
        var document = new DocumentRenderer().Render(WithTextInput(string.Empty));
        document.AcroForm!.FillField("name", "Bob");

        Assert.NotEmpty(Save(document));
    }

    [Fact]
    public void RenderedFormUnderAClaimStillSaves()
    {
        var document = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 }
            .Render(WithTextInput("Ada"));

        Assert.NotEmpty(Save(document));
    }
}
