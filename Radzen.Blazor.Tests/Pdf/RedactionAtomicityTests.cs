#nullable enable

using System;
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class RedactionAtomicityTests
{
    private static readonly PdfRect TextArea = new(50, 700, 56.67, 710);

    private static PortableDocument TextThenShadingDocument()
    {
        const string stream = "BT /F1 10 Tf 10 700 Td (WWWWX) Tj ET /Sh0 sh";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Font << /F1 5 0 R >> /Shading << /Sh0 6 0 R >> >> /Contents 4 0 R >>\nendobj\n")
            .Object(4, $"4 0 obj\n<< /Length {stream.Length} >>\nstream\n{stream}\nendstream\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Font /Subtype /TrueType /BaseFont /Sample /Encoding /WinAnsiEncoding "
                + "/FirstChar 87 /LastChar 88 /Widths [1000 667] >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /ShadingType 2 /ColorSpace /DeviceGray /Coords [0 0 1 1] "
                + "/Function << /FunctionType 2 /Domain [0 1] /C0 [0] /C1 [1] /N 1 >> >>\nendobj\n");

        return Finish(pdf, 6);
    }

    private static PortableDocument TwoPageDocument()
    {
        const string first = "10 10 50 50 re f";
        const string second = "20 20 50 50 re f";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 5 0 R >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 6 0 R >>\nendobj\n")
            .Object(5, $"5 0 obj\n<< /Length {first.Length} >>\nstream\n{first}\nendstream\nendobj\n")
            .Object(6, $"6 0 obj\n<< /Length {second.Length} >>\nstream\n{second}\nendstream\nendobj\n");

        return Finish(pdf, 6);
    }

    private static PortableDocument Finish(FixturePdf pdf, int last)
    {
        var xref = pdf.Position;
        pdf.Append($"xref\n0 {last + 1}\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= last; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append($"trailer\n<< /Size {last + 1} /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        using var input = new MemoryStream(pdf.ToArray());
        return PortableDocument.LoadFromStream(input);
    }

    private static byte[] SavedPageContent(PortableDocument document, int pageIndex)
        => InterpreterTestSupport.PageContentBytes(document.ToArray(), pageIndex);

    [Fact]
    public void Redact_UnsupportedConstructAfterRedactableText_LeavesTheContentUnchanged()
    {
        var document = TextThenShadingDocument();
        var page = document.Pages[0];
        var before = page.GetContent();
        var savedBefore = SavedPageContent(document, 0);

        Assert.Throws<NotSupportedException>(() => page.Redact([TextArea]));

        Assert.Equal(before, page.GetContent());
        Assert.Equal(savedBefore, SavedPageContent(document, 0));
    }

    [Fact]
    public void RedactText_UnsupportedConstructOnTheMatchedPage_LeavesTheContentUnchanged()
    {
        var document = TextThenShadingDocument();
        var page = document.Pages[0];
        var before = page.GetContent();

        Assert.Throws<NotSupportedException>(() => document.RedactText("X"));

        Assert.Equal(before, page.GetContent());
    }

    [Fact]
    public void Redact_TrailingInvalidPageIndex_LeavesEveryPageUnchanged()
    {
        var document = TwoPageDocument();
        var before = new[] { document.Pages[0].GetContent(), document.Pages[1].GetContent() };
        var savedBefore = new[] { SavedPageContent(document, 0), SavedPageContent(document, 1) };

        Assert.Throws<ArgumentOutOfRangeException>(() => document.Redact(
        [
            new PageRedaction(0, PdfRect.FromSize(10, 10, 50, 50)),
            new PageRedaction(1, PdfRect.FromSize(20, 20, 50, 50)),
            new PageRedaction(7, PdfRect.FromSize(10, 10, 50, 50)),
        ]));

        Assert.Equal(before[0], document.Pages[0].GetContent());
        Assert.Equal(before[1], document.Pages[1].GetContent());
        Assert.Equal(savedBefore[0], SavedPageContent(document, 0));
        Assert.Equal(savedBefore[1], SavedPageContent(document, 1));
    }

    [Fact]
    public void Redact_ValidPageIndexes_RemovesContentOnEveryPage()
    {
        var document = TwoPageDocument();

        document.Redact(
        [
            new PageRedaction(0, PdfRect.FromSize(10, 10, 50, 50)),
            new PageRedaction(1, PdfRect.FromSize(20, 20, 50, 50)),
        ]);

        Assert.DoesNotContain("re", ContentOperationTestHelpers.Operators(SavedPageContent(document, 0)));
        Assert.DoesNotContain("re", ContentOperationTestHelpers.Operators(SavedPageContent(document, 1)));
    }
}
