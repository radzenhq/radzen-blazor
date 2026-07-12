#nullable enable

using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// A loaded page whose font resource is named /F0 must not have that resource clobbered
// when a full re-encode registers a fresh base-14 face: the emitter's default keys start
// at F0/Im0 and MergeResources lets emitted entries win, so a colliding key would replace
// the loaded font. The re-emit must allocate keys that dodge the loaded page's names.
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

    private static DictionaryObject PageFonts(byte[] file)
    {
        var reader = DocumentReader.Parse(file);
        var page = ContentTestHelpers.Kid(reader, 0);
        var resources = Assert.IsType<DictionaryObject>(reader.Resolve(page["Resources"]));
        return Assert.IsType<DictionaryObject>(reader.Resolve(resources["Font"]));
    }

    private static string BaseFont(DocumentReader reader, DocumentObject fontValue)
        => ((NameObject)reader.Resolve(((DictionaryObject)reader.Resolve(fontValue))["BaseFont"])).Value;

    [Fact]
    public void FullReencode_AddingBase14Text_DoesNotClobberLoadedFont()
    {
        using var stream = new MemoryStream(Fixture());
        var document = Document.LoadFromStream(stream);
        var page = document.Pages[0];

        // Mutate an existing element to force a full re-encode, then add a base-14 run.
        var existing = page.Content.OfType<TextContent>().First();
        existing.Color = Color.Red;
        page.Content.Add(new TextContent("New", 72, 680) { Font = new Font { Name = "Helvetica" } });

        var saved = document.ToArray();

        var reader = DocumentReader.Parse(saved);
        var page0 = ContentTestHelpers.Kid(reader, 0);
        var resources = Assert.IsType<DictionaryObject>(reader.Resolve(page0["Resources"]));
        var fonts = Assert.IsType<DictionaryObject>(reader.Resolve(resources["Font"]));

        Assert.True(fonts.ContainsKey("F0"));
        Assert.Equal("CLOBBERCANARY", BaseFont(reader, fonts["F0"]));

        // The freshly registered base-14 face lives under its own, non-colliding key.
        Assert.Contains(fonts.Keys, k => k != "F0" && BaseFont(reader, fonts[k]) == "Helvetica");
    }
}
