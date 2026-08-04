#nullable enable

using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class LoadedResourceKeyCollisionTests
{
    private const string StreamData = "BT /F0 12 Tf 72 700 Td (Hi) Tj ET";

    private static byte[] Fixture()
    {
        var obj4 = $"4 0 obj\n<< /Length {StreamData.Length} >>\nstream\n{StreamData}\nendstream\nendobj\n";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Font << /F0 5 0 R >> >> /Contents 4 0 R >>\nendobj\n")
            .Object(4, obj4)
            .Object(5, "5 0 obj\n<< /Type /Font /Subtype /TrueType /BaseFont /CLOBBERCANARY >>\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n");
        pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 5; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 6 /Root 1 0 R >>\n");
        pdf.Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void FullReencode_AddingBase14Text_DoesNotClobberLoadedFont()
    {
        using var stream = new MemoryStream(Fixture());
        var document = PortableDocument.LoadFromStream(stream);
        var page = document.Pages[0];

        var existing = page.Content.OfType<TextContent>().First();
        existing.Color = Color.Red;
        page.Content.Add(new TextContent("New", 72, 680) { Font = new Font { Family = "Helvetica" } });

        var emission = Emit(document);
        var pageObject = Line(emission, "/Type /Page ");

        var loaded = Shaped("page /Resources /Font", @"/Font << /F0 (\d+) 0 R", pageObject);
        Carries("loaded font /F0", "/BaseFont /CLOBBERCANARY", IndirectObject(emission, loaded.Groups[1].Value));

        var added = Shaped(
            "page /Resources /Font",
            @"/(\w+) << /Type /Font /Subtype /Type1 /BaseFont /Helvetica",
            pageObject);
        Assert.True(
            added.Groups[1].Value != "F0",
            $"The added Helvetica font took the loaded /F0 name.\npage:\n{Excerpt(pageObject)}");
    }
}
