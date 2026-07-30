#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ConformanceInspectabilityOrderTests
{
    private static byte[] LoadableFileWithFontlessText()
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
            + "/Resources << >> /Contents 4 0 R >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Length 20 >>\nstream\nBT (Hello) Tj ET\nendstream\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 5\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(3)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(4)))
            .Append("trailer\n<< /Size 5 /Root 1 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static PortableDocument LoadedFontlessDocument()
    {
        using var stream = new MemoryStream(LoadableFileWithFontlessText());
        return PortableDocument.LoadFromStream(stream);
    }

    private static (Document Document, DocumentRenderer Renderer) ConformingBuilder()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "Conformance";
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Hello", BuildTestSupport.Latin);
        var builderRenderer = new DocumentRenderer();
        builderRenderer.Conformance = PdfAConformance.PdfA2B;
        return (document, builderRenderer);
    }

    [Fact]
    public void SaveToStream_UninspectablePageWithFontError_ReportsInspectabilityFirst()
    {
        var conforming = ConformingBuilder();
        var document = conforming.Renderer.Render(conforming.Document);
        document.Append(LoadedFontlessDocument());

        using var stream = new MemoryStream();
        var error = Assert.Throws<InvalidOperationException>(() => document.SaveToStream(stream));

        Assert.Contains("cannot be inspected", error.Message, StringComparison.Ordinal);
    }
}
