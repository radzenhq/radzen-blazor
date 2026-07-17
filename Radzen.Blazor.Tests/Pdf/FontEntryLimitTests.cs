#nullable enable
using System.Diagnostics;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// ReaderLimits caps on font tables materialized from untrusted input. MaxCMapEntries is
// documented to bound a /ToUnicode CMap, but only the scalar bfrange form ever checked it:
// beginbfchar and the array form of bfrange filled the map unbounded. The CID font /W array
// had no ReaderLimits check at all, and its "c_first c_last w" range form is the worst case -
// ~15 input bytes buy an arbitrary span, so <0 2147483647 500> materializes billions of
// width entries. Each cap is paired with a positive control proving valid fonts still map.
public class FontEntryLimitTests
{
    [Fact]
    public void Bfchar_BeyondMaxCMapEntries_Throws()
    {
        var body = new StringBuilder("64 beginbfchar\n");
        for (var i = 1; i <= 64; i++)
        {
            body.Append($"<{i:X4}> <0041>\n");
        }

        body.Append("endbfchar");

        Assert.Throws<DocumentParseException>(
            () => ToUnicodeCMap.Parse(Cmap(body.ToString()), new ReaderLimits { MaxCMapEntries = 8 }));
    }

    [Fact]
    public void Bfchar_WithinMaxCMapEntries_StillMaps()
    {
        var (map, _) = ToUnicodeCMap.Parse(
            Cmap("2 beginbfchar <0003> <0041> <0009> <0062> endbfchar"),
            new ReaderLimits { MaxCMapEntries = 2 });

        Assert.Equal("A", map[0x0003]);
        Assert.Equal("b", map[0x0009]);
    }

    // Redefining a code materializes no new entry, so a CMap sitting exactly at the cap
    // must not be rejected for overwriting codes it already holds.
    [Fact]
    public void Bfchar_RedefiningCodeAtCap_DoesNotThrow()
    {
        var (map, _) = ToUnicodeCMap.Parse(
            Cmap("3 beginbfchar <0003> <0041> <0009> <0062> <0003> <0043> endbfchar"),
            new ReaderLimits { MaxCMapEntries = 2 });

        Assert.Equal("C", map[0x0003]);
        Assert.Equal(2, map.Count);
    }

    [Fact]
    public void ArrayBfrange_BeyondMaxCMapEntries_Throws()
    {
        var body = new StringBuilder("1 beginbfrange\n<0000> <003F> [");
        for (var i = 0; i < 64; i++)
        {
            body.Append("<0041> ");
        }

        body.Append("]\nendbfrange");

        Assert.Throws<DocumentParseException>(
            () => ToUnicodeCMap.Parse(Cmap(body.ToString()), new ReaderLimits { MaxCMapEntries = 8 }));
    }

    [Fact]
    public void ArrayBfrange_WithinMaxCMapEntries_StillMaps()
    {
        var (map, _) = ToUnicodeCMap.Parse(
            Cmap("1 beginbfrange <0003> <0005> [<0041> <0062> <0043>] endbfrange"),
            new ReaderLimits { MaxCMapEntries = 3 });

        Assert.Equal("A", map[0x0003]);
        Assert.Equal("b", map[0x0004]);
        Assert.Equal("C", map[0x0005]);
    }

    // The bomb: a 24-byte /W range asks for 2.1 billion width entries (~168 GB at the
    // ~88 bytes/entry this dictionary costs). It must be refused from the default limits,
    // before the expansion loop runs.
    [Fact]
    public void CidWidthRange_FullIntSpan_ThrowsFastUnderDefaultLimits()
    {
        var reader = DocumentReader.Parse(WidthPdf("[0 2147483647 500]"), null, ReaderLimits.Default);
        var font = FontDict(reader);

        var watch = Stopwatch.StartNew();
        Assert.Throws<DocumentParseException>(() => ReverseFont.Build(reader, font));
        watch.Stop();

        Assert.True(watch.ElapsedMilliseconds < 1000, $"Rejection took {watch.ElapsedMilliseconds}ms.");
    }

    [Fact]
    public void CidWidthRange_BeyondMaxFontWidthEntries_Throws()
    {
        var reader = DocumentReader.Parse(WidthPdf("[0 200000 500]"), null, new ReaderLimits { MaxFontWidthEntries = 8 });

        Assert.Throws<DocumentParseException>(() => ReverseFont.Build(reader, FontDict(reader)));
    }

    [Fact]
    public void CidWidthArray_BeyondMaxFontWidthEntries_Throws()
    {
        var reader = DocumentReader.Parse(
            WidthPdf("[0 [500 500 500 500]]"), null, new ReaderLimits { MaxFontWidthEntries = 2 });

        Assert.Throws<DocumentParseException>(() => ReverseFont.Build(reader, FontDict(reader)));
    }

    [Fact]
    public void CidWidths_WithinLimit_StillResolveWidths()
    {
        var reader = DocumentReader.Parse(
            WidthPdf("[3 5 500 8 [700 900]]"), null, new ReaderLimits { MaxFontWidthEntries = 8 });
        var font = ReverseFont.Build(reader, FontDict(reader));

        Assert.True(font.TryGetWidth(3, out var ranged));
        Assert.Equal(500, ranged);
        Assert.True(font.TryGetWidth(5, out var rangedEnd));
        Assert.Equal(500, rangedEnd);
        Assert.True(font.TryGetWidth(9, out var listed));
        Assert.Equal(900, listed);

        // /DW 1 for any CID the table does not cover.
        Assert.True(font.TryGetWidth(4000, out var fallback));
        Assert.Equal(1, fallback);
    }

    private static DictionaryObject FontDict(DocumentReader reader)
    {
        var catalog = reader.GetDictionary(reader.Trailer, "Root")!;
        var pages = reader.GetDictionary(catalog, "Pages")!;
        var page = reader.AsDictionary(reader.GetArray(pages, "Kids")![0])!;
        var resources = reader.GetDictionary(page, "Resources")!;
        return reader.GetDictionary(reader.GetDictionary(resources, "Font")!, "F0")!;
    }

    // A Type0/Identity-H font whose descendant carries the /W array verbatim, so a hostile
    // span can be injected while the file itself stays a few hundred bytes.
    private static byte[] WidthPdf(string w)
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Font << /F0 4 0 R >> >> >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Type /Font /Subtype /Type0 /BaseFont /SUBSET /Encoding /Identity-H "
                + "/DescendantFonts [5 0 R] >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Font /Subtype /CIDFontType2 /BaseFont /SUBSET "
                + "/CIDSystemInfo << /Registry (Adobe) /Ordering (Identity) /Supplement 0 >> "
                + $"/DW 1 /W {w} >>\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 5; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static byte[] Cmap(string body) => Encoding.ASCII.GetBytes(
        "/CIDInit /ProcSet findresource begin 12 dict begin begincmap\n" +
        "1 begincodespacerange <0000> <FFFF> endcodespacerange\n" +
        body + "\nendcmap end end\n");
}
