#nullable enable

using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class TextCompositionAgreementTests
{
    private static PortableDocument Loaded(string streamData)
    {
        var contentObject = $"4 0 obj\n<< /Length {streamData.Length} >>\nstream\n{streamData}\nendstream\nendobj\n";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Font << /F0 5 0 R >> >> /Contents 4 0 R >>\nendobj\n")
            .Object(4, contentObject)
            .Object(5, "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 5; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        using var input = new MemoryStream(pdf.ToArray());
        return PortableDocument.LoadFromStream(input);
    }

    private const string NarrowGlyphRun = "BT /F0 10 Tf 1 0 0 1 72 700 Tm (iiii) Tj 1 0 0 1 87 700 Tm (B) Tj ET";

    [Fact]
    public void ExtractText_NarrowGlyphs_AgreesWithFindTextAboutTheWordBreak()
    {
        var page = Loaded(NarrowGlyphRun).Pages[0];

        Assert.Equal("iiii B", page.ExtractText());
    }

    [Fact]
    public void ExtractText_AfterElementRemoval_ReflectsTheEdit()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("SENSITIVE", 72, 720));
        page.Content.Add(new TextContent("KEPT", 72, 700));
        var loaded = InterpreterTestSupport.Load(document.ToArray());
        var sensitive = loaded.Pages[0].Content
            .Select((element, index) => (element, index))
            .Single(item => item.element is TextContent text && text.Text == "SENSITIVE").index;

        loaded.Pages[0].Content.RemoveAt(sensitive);

        Assert.DoesNotContain("SENSITIVE", loaded.Pages[0].ExtractText());
        Assert.Contains("KEPT", loaded.Pages[0].ExtractText());
    }

    [Fact]
    public void ExtractPositionedText_AfterAppend_IncludesTheAppendedElement()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("ORIGINAL", 72, 720));
        var loaded = InterpreterTestSupport.Load(document.ToArray());

        loaded.Pages[0].Content.Add(new TextContent("ADDED", 72, 700));

        Assert.Contains("ADDED", loaded.Pages[0].ExtractPositionedText().Select(run => run.Text));
    }

    [Fact]
    public void ExtractText_OnUneditedLoadedPage_DoesNotForceReencode()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("STABLE", 72, 720));
        var original = document.ToArray();

        var untouched = InterpreterTestSupport.Load(original).ToArray();
        var read = InterpreterTestSupport.Load(original);
        _ = read.Pages[0].ExtractText();
        _ = read.Pages[0].FindText("STABLE");
        _ = read.Pages[0].ExtractPositionedText();

        Assert.Equal(untouched, read.ToArray());
    }
}
