#nullable enable

using System;
using System.Linq;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ContentEditingTests
{
    private static Document LoadedDocumentWithText(string text)
    {
        var document = new Document();
        document.Pages.Add().Content.Add(new TextContent(text, 72, 700) { Font = new Font { Size = 12 } });
        return InterpreterTestSupport.Load(document.ToArray());
    }

    private static Document LoadedSimpleWidthDocument(string streamData = "BT /F0 10 Tf 72 700 Td (AB) Tj (Z) Tj ET")
    {
        var contentObject = $"4 0 obj\n<< /Length {streamData.Length} >>\nstream\n{streamData}\nendstream\nendobj\n";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Font << /F0 5 0 R >> >> /Contents 4 0 R >>\nendobj\n")
            .Object(4, contentObject)
            .Object(5, "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Custom "
                + "/Encoding /WinAnsiEncoding /FirstChar 65 /LastChar 66 /Widths [200 900] >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 5; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        using var input = new MemoryStream(pdf.ToArray());
        return Document.LoadFromStream(input);
    }

    [Fact]
    public void RemoveAt_LoadedPath_RemovesOnlyItsPaintOperators()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(Encoding.ASCII.GetBytes("0 0 m 10 0 l S\n20 0 m 30 0 l S\n"));
        var loaded = InterpreterTestSupport.Load(document.ToArray());

        loaded.Pages[0].Content.RemoveAt(0);
        var saved = InterpreterTestSupport.PageContentBytes(loaded.ToArray(), 0);
        var stream = Encoding.ASCII.GetString(saved);

        Assert.DoesNotContain("0 0 m 10 0 l S", stream);
        Assert.Contains("20 0 m 30 0 l S", stream);
    }

    [Fact]
    public void Insert_LoadedPage_PreservesPaintOrder()
    {
        var loaded = LoadedDocumentWithText("after");
        loaded.Pages[0].Content.Insert(0, new TextContent("before", 72, 720));

        var reloaded = InterpreterTestSupport.Load(loaded.ToArray());

        Assert.Contains("before", reloaded.ExtractText());
        Assert.Contains("after", reloaded.ExtractText());
    }

    [Fact]
    public void ReplaceText_InvoiceNumber_ExtractsReplacementAndPreservesFollowingOrigin()
    {
        var loaded = LoadedDocumentWithText("Invoice INV-1001");
        var before = loaded.Pages[0].ExtractPositionedText().Single().Bounds.Right;

        var count = loaded.ReplaceText("INV-1001", "INV-9");
        var reloaded = InterpreterTestSupport.Load(loaded.ToArray());
        var after = reloaded.Pages[0].ExtractPositionedText().Single().Bounds.Right;

        Assert.Equal(1, count);
        Assert.Contains("Invoice INV-9", reloaded.ExtractText());
        Assert.Equal(before, after, 6);
    }

    [Fact]
    public void ReplaceText_PreserveAdvance_UsesSourceFontWidths()
    {
        var loaded = LoadedSimpleWidthDocument();
        var before = loaded.Pages[0].ExtractPositionedText().Single(run => run.Text == "Z").Bounds.Left;

        var count = loaded.ReplaceText("A", "B");
        var saved = loaded.ToArray();
        var reloaded = InterpreterTestSupport.Load(saved);
        var after = reloaded.Pages[0].ExtractPositionedText().Single(run => run.Text == "Z").Bounds.Left;
        var stream = Encoding.ASCII.GetString(InterpreterTestSupport.PageContentBytes(saved, 0));

        Assert.Equal(1, count);
        Assert.Contains("700", stream);
        Assert.Equal(before, after, 6);
    }

    [Fact]
    public void Insert_ThenReplaceText_PreservesInsertedContent()
    {
        var loaded = LoadedDocumentWithText("original");
        loaded.Pages[0].Content.Insert(0, new TextContent("inserted", 72, 720));

        var count = loaded.ReplaceText("original", "changed");
        var reloaded = InterpreterTestSupport.Load(loaded.ToArray());

        Assert.Equal(1, count);
        Assert.Contains("inserted", reloaded.ExtractText());
        Assert.Contains("changed", reloaded.ExtractText());
    }

    [Fact]
    public void RemoveAt_ThenRedactText_DoesNotRestoreRemovedContent()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("SENSITIVE", 72, 720));
        page.Content.Add(new TextContent("OTHER", 72, 700));
        var loaded = InterpreterTestSupport.Load(document.ToArray());
        var sensitiveIndex = loaded.Pages[0].Content
            .Select((element, index) => (element, index))
            .Single(item => item.element is TextContent text && text.Text.Contains("SENSITIVE", StringComparison.Ordinal)).index;

        loaded.Pages[0].Content.RemoveAt(sensitiveIndex);
        var count = loaded.RedactText("OTHER");
        var saved = loaded.ToArray();
        var reloaded = InterpreterTestSupport.Load(saved);

        Assert.Equal(1, count);
        Assert.DoesNotContain("SENSITIVE", reloaded.ExtractText());
        Assert.DoesNotContain("SENSITIVE", Encoding.Latin1.GetString(InterpreterTestSupport.PageContentBytes(saved, 0)));
    }

    [Fact]
    public void ReplaceText_MissingWinAnsiGlyph_FailsLoud()
    {
        var loaded = LoadedDocumentWithText("Invoice 1001");

        var exception = Assert.Throws<NotSupportedException>(() => loaded.ReplaceText("1001", "猫"));

        Assert.Contains("does not contain every glyph", exception.Message);
    }

    [Fact]
    public void ReplaceText_MissingSubsetGlyph_FailsLoud()
    {
        const string streamData = "BT /F0 12 Tf 72 700 Td <000100020003> Tj ET";
        const string cmap = "/CIDInit /ProcSet findresource begin\n12 dict begin\nbegincmap\n"
            + "1 begincodespacerange\n<0000> <FFFF>\nendcodespacerange\n"
            + "3 beginbfchar\n<0001> <0041>\n<0002> <0042>\n<0003> <0043>\nendbfchar\n"
            + "endcmap\nCMapName currentdict /CMap defineresource pop\nend\nend\n";
        var contentObject = $"4 0 obj\n<< /Length {streamData.Length} >>\nstream\n{streamData}\nendstream\nendobj\n";
        var cmapObject = $"6 0 obj\n<< /Length {cmap.Length} >>\nstream\n{cmap}endstream\nendobj\n";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Font << /F0 5 0 R >> >> /Contents 4 0 R >>\nendobj\n")
            .Object(4, contentObject)
            .Object(5, "5 0 obj\n<< /Type /Font /Subtype /Type0 /BaseFont /SUBSET "
                + "/Encoding /Identity-H /ToUnicode 6 0 R >>\nendobj\n")
            .Object(6, cmapObject);
        var xref = pdf.Position;
        pdf.Append("xref\n0 7\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 6; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 7 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        using var input = new MemoryStream(pdf.ToArray());
        var document = Document.LoadFromStream(input);

        var exception = Assert.Throws<NotSupportedException>(() => document.ReplaceText("A", "D"));

        Assert.Contains("does not contain every glyph", exception.Message);
    }

    [Fact]
    public void RedactText_RemovesUnderlyingTextFromSavedContent()
    {
        var loaded = LoadedDocumentWithText("public SECRET public");

        var count = loaded.RedactText("SECRET", redactionOptions: new RedactionOptions { FillColor = Color.Black });
        var saved = loaded.ToArray();
        var reloaded = InterpreterTestSupport.Load(saved);

        Assert.Equal(1, count);
        Assert.DoesNotContain("SECRET", reloaded.ExtractText());
        Assert.DoesNotContain("SECRET", Encoding.Latin1.GetString(InterpreterTestSupport.PageContentBytes(saved, 0)));
    }

    [Fact]
    public void Redact_PartOfTjRun_PreservesNonIntersectingWords()
    {
        var loaded = LoadedDocumentWithText("before SECRET after");
        var secret = loaded.FindText("SECRET").Single();

        loaded.Pages[0].Redact(new[] { new Rect(secret.Bounds.Left, secret.Bounds.Bottom, secret.Bounds.Width, secret.Bounds.Height) });
        var saved = loaded.ToArray();
        var reloaded = InterpreterTestSupport.Load(saved);
        var text = reloaded.ExtractText();

        Assert.DoesNotContain("SECRET", text);
        Assert.Contains("before", text);
        Assert.Contains("after", text);
    }

    [Fact]
    public void Redact_PartOfTjArray_PreservesNonIntersectingWords()
    {
        var loaded = LoadedSimpleWidthDocument("BT /F0 10 Tf 72 700 Td [(AA) -50 (BB) 75 (A)] TJ ET");
        var secret = loaded.FindText("BB").Single();

        loaded.Pages[0].Redact(new[] { new Rect(secret.Bounds.Left, secret.Bounds.Bottom, secret.Bounds.Width, secret.Bounds.Height) });
        var reloaded = InterpreterTestSupport.Load(loaded.ToArray());
        var text = reloaded.ExtractText();

        Assert.DoesNotContain("B", text);
        Assert.Equal("AAA", text.Replace(" ", string.Empty, StringComparison.Ordinal));
    }
}
