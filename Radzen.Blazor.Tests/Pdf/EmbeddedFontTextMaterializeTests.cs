#nullable enable

using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// A run drawn in a loaded Type0/embedded font carries 2-byte codes. Materialization must
// decode its Text through the font's /ToUnicode map (as text extraction does) rather than
// pushing raw bytes through WinAnsi, which drops the 0x00 high bytes and yields empty text.
// The original show bytes are preserved so an unrelated edit survives the load-edit-save
// cycle with the embedded glyphs intact.
public class EmbeddedFontTextMaterializeTests
{
    // Codes 0x0001..0x0003 are undefined in WinAnsi (both bytes drop), so a per-byte
    // WinAnsi decode yields "" while the /ToUnicode map resolves them to "ABC".
    private const string StreamData = "BT /F0 12 Tf 72 700 Td <000100020003> Tj ET";

    private const string ToUnicode =
        "/CIDInit /ProcSet findresource begin\n" +
        "12 dict begin\nbegincmap\n" +
        "1 begincodespacerange\n<0000> <FFFF>\nendcodespacerange\n" +
        "3 beginbfchar\n<0001> <0041>\n<0002> <0042>\n<0003> <0043>\nendbfchar\n" +
        "endcmap\nCMapName currentdict /CMap defineresource pop\nend\nend\n";

    private static byte[] Fixture()
    {
        var obj4 = $"4 0 obj\n<< /Length {StreamData.Length} >>\nstream\n{StreamData}\nendstream\nendobj\n";
        var obj6 = $"6 0 obj\n<< /Length {ToUnicode.Length} >>\nstream\n{ToUnicode}endstream\nendobj\n";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Font << /F0 5 0 R >> >> /Contents 4 0 R >>\nendobj\n")
            .Object(4, obj4)
            .Object(5, "5 0 obj\n<< /Type /Font /Subtype /Type0 /BaseFont /EMBEDDED "
                + "/Encoding /Identity-H /ToUnicode 6 0 R >>\nendobj\n")
            .Object(6, obj6);

        var xref = pdf.Position;
        pdf.Append("xref\n0 7\n");
        pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 6; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 7 /Root 1 0 R >>\n");
        pdf.Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static Document Load()
    {
        using var stream = new MemoryStream(Fixture());
        return Document.LoadFromStream(stream);
    }

    [Fact]
    public void EmbeddedRun_MaterializesUnicodeTextThroughToUnicode()
    {
        var document = Load();

        var text = document.Pages[0].Content.OfType<TextContent>().First();

        Assert.Equal("ABC", text.Text);
    }

    [Fact]
    public void EmbeddedRun_UnrelatedEdit_PreservesGlyphsAcrossReload()
    {
        var document = Load();
        var text = document.Pages[0].Content.OfType<TextContent>().First();
        text.Color = Color.Red; // unrelated edit forces a full re-encode

        using var stream = new MemoryStream(document.ToArray());
        var reloaded = Document.LoadFromStream(stream);

        Assert.Contains("ABC", reloaded.Pages[0].ExtractText());
    }
}
