#nullable enable
using System;
using System.IO;
using System.Linq;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class DocumentAtomicityAndFacadeTests
{
    private static byte[] FormDocumentWithViewerPreferences()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var paragraph = document.Sections.Add().Blocks.Add(new Paragraph());
        paragraph.Font.Family = BuildTestSupport.Latin;
        paragraph.Inlines.Add(new TextInput("Name") { Value = "Ada" });

        var rendered = new DocumentRenderer().Render(document);
        rendered.ViewerPreferences = new ViewerPreferences
        {
            DisplayDocTitle = true,
            HideToolbar = true,
        };
        return rendered.ToArray();
    }

    private static PortableDocument Load(byte[] bytes)
        => PortableDocument.LoadFromStream(new MemoryStream(bytes, writable: false));

    [Fact]
    public void SaveIncremental_LoadedDocumentWithViewerPreferences_FormFillSucceeds()
    {
        var document = Load(FormDocumentWithViewerPreferences());
        Assert.True(document.ViewerPreferences!.DisplayDocTitle);
        document.AcroForm!.FillField("Name", "Radzen");

        using var output = new MemoryStream();
        document.SaveIncremental(output);

        var reloaded = Load(output.ToArray());
        Assert.True(reloaded.ViewerPreferences!.DisplayDocTitle);
    }

    [Fact]
    public void SaveToStream_ClearingOneLoadedViewerPreferenceFlag_IsReflected()
    {
        var document = Load(FormDocumentWithViewerPreferences());
        document.ViewerPreferences!.HideToolbar = false;

        var reloaded = Load(document.ToArray());

        Assert.False(reloaded.ViewerPreferences!.HideToolbar);
        Assert.True(reloaded.ViewerPreferences.DisplayDocTitle);
    }

    [Fact]
    public void SaveToStream_ClearingEveryLoadedViewerPreferenceFlag_IsReflected()
    {
        var document = Load(FormDocumentWithViewerPreferences());
        document.ViewerPreferences!.HideToolbar = false;
        document.ViewerPreferences.DisplayDocTitle = false;

        var reloaded = Load(document.ToArray());

        Assert.True(reloaded.ViewerPreferences is null
            || (!reloaded.ViewerPreferences.HideToolbar && !reloaded.ViewerPreferences.DisplayDocTitle));
    }

    [Fact]
    public void SaveToStream_UntouchedLoadedViewerPreferences_MatchesUnreadSave()
    {
        var bytes = FormDocumentWithViewerPreferences();

        var read = Load(bytes);
        Assert.True(read.ViewerPreferences!.DisplayDocTitle);
        var withRead = read.ToArray();

        var withoutRead = Load(bytes).ToArray();

        Assert.Equal(withoutRead, withRead);
    }

    [Fact]
    public void Append_Self_IntactPages_AppendsACopyOfTheOriginalPages()
    {
        var authored = new PortableDocument();
        authored.Pages.Add().Content.Add(new TextContent("Solo", 72, 700) { Font = new Font { Size = 12 } });
        var document = Load(authored.ToArray());

        document.Append(document);

        Assert.Equal(2, document.Pages.Count);
        var text = Load(document.ToArray()).ExtractText();
        Assert.Equal(2, text.Split("Solo").Length - 1);
    }

    [Fact]
    public void Append_Self_WithEditedPage_AppendsACopyOfTheOriginalPages()
    {
        var authored = new PortableDocument();
        authored.Pages.Add().Content.Add(new TextContent("Solo", 72, 700) { Font = new Font { Size = 12 } });
        var document = Load(authored.ToArray());
        document.Pages[0].Content.Add(new TextContent("Extra", 72, 650) { Font = new Font { Size = 12 } });

        document.Append(document);

        Assert.Equal(2, document.Pages.Count);
    }

    private static PortableDocument TwoPageDocument(string firstStream, string secondStream)
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Font << /F0 7 0 R >> >> /Contents 5 0 R >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Font << /F0 7 0 R >> >> /Contents 6 0 R >>\nendobj\n")
            .Object(5, $"5 0 obj\n<< /Length {firstStream.Length} >>\nstream\n{firstStream}\nendstream\nendobj\n")
            .Object(6, $"6 0 obj\n<< /Length {secondStream.Length} >>\nstream\n{secondStream}\nendstream\nendobj\n")
            .Object(7, "7 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 8\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 7; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 8 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        return Load(pdf.ToArray());
    }

    [Fact]
    public void ReplaceText_UnsupportedConstructOnALaterPage_LeavesEarlierPagesUntouched()
    {
        var document = TwoPageDocument(
            "BT /F0 10 Tf 72 700 Td (Date) Tj ET",
            "BT /F0 10 Tf 72 700 Td [(Date)] TJ ET");
        var before = document.Pages[0].GetContent();

        Assert.Throws<NotSupportedException>(() => document.ReplaceText("Date", "Time"));

        Assert.Equal(before, document.Pages[0].GetContent());
        Assert.DoesNotContain("Time", Load(document.ToArray()).ExtractText());
    }

    [Fact]
    public void ReplaceText_AllPagesSupported_ReplacesEverywhere()
    {
        var document = TwoPageDocument(
            "BT /F0 10 Tf 72 700 Td (Date) Tj ET",
            "BT /F0 10 Tf 72 700 Td (Date) Tj ET");

        var count = document.ReplaceText("Date", "Time");

        Assert.Equal(2, count);
        var text = Load(document.ToArray()).ExtractText();
        Assert.Equal(2, text.Split("Time").Length - 1);
        Assert.DoesNotContain("Date", text);
    }
}
