#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Document-level defensive-hardening contract: cyclic/over-deep page trees, an
// unbounded xref stream, an over-declared object stream, and stream-filter bombs
// must all be handled quickly and safely instead of overflowing the stack,
// hanging, or exhausting memory. Each guard is paired with a positive control.
public class DocumentHardeningTests
{
    // --- Item 2: page-tree cycle and depth ----------------------------------

    [Fact]
    public void CyclicPageTree_Throws()
    {
        var bytes = ClassicXref(
            (1, "<< /Type /Catalog /Pages 2 0 R >>"),
            (2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            (3, "<< /Type /Pages /Kids [2 0 R] /Count 1 >>"));

        Assert.Throws<DocumentParseException>(() => Document.LoadFromStream(new MemoryStream(bytes)));
    }

    [Fact]
    public void OverDeepPageTree_Throws()
    {
        var bytes = ClassicXref(
            (1, "<< /Type /Catalog /Pages 2 0 R >>"),
            (2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            (3, "<< /Type /Pages /Kids [4 0 R] /Count 1 >>"),
            (4, "<< /Type /Page /MediaBox [0 0 200 200] >>"));

        var limits = new ReaderLimits { MaxPageTreeDepth = 1 };
        Assert.Throws<DocumentParseException>(() => Document.LoadFromStream(new MemoryStream(bytes), limits));
    }

    [Fact]
    public void MultiPageDocument_EnumeratesAllPages_PositiveControl()
    {
        var bytes = ClassicXref(
            (1, "<< /Type /Catalog /Pages 2 0 R >>"),
            (2, "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>"),
            (3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>"),
            (4, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>"));

        var document = Document.LoadFromStream(new MemoryStream(bytes));
        Assert.Equal(2, document.Pages.Count);
    }

    // --- Item 3: unbounded xref stream --------------------------------------

    // /W [0 0 0] makes each entry zero-length so a huge /Size never advances the
    // read cursor. The guard rejects the widths; the existing repair fallback then
    // rebuilds from the object scan, so the reader recovers quickly instead of
    // exhausting memory building a hundred-million-entry table.
    [Fact]
    public void XrefStreamZeroWidthHugeSize_RecoversWithoutOom()
    {
        var reader = DocumentReader.Parse(ZeroWidthXrefStreamFile());
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }

    [Fact]
    public void ValidXrefStream_Parses_PositiveControl()
    {
        var reader = DocumentReader.Parse(RawXrefStreamFile());
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }

    // --- Item 4: over-declared object stream --------------------------------

    // /N 2000000000 would size the member list (and the fill loop) from attacker
    // input. The clamp caps it to what the payload can hold, so resolving the one
    // real member succeeds quickly instead of trying to allocate ~16 GB.
    [Fact]
    public void ObjectStreamHugeCount_ResolvesWithoutOom()
    {
        var reader = DocumentReader.Parse(HugeCountObjStmFile());
        var catalog = Assert.IsType<DictionaryObject>(reader.GetObject(1));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }

    [Fact]
    public void ValidObjectStream_ResolvesMembers_PositiveControl()
    {
        var reader = DocumentReader.Parse(ValidObjStmFile());
        var catalog = Assert.IsType<DictionaryObject>(reader.GetObject(1));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
        var pages = Assert.IsType<DictionaryObject>(reader.GetObject(2));
        Assert.Equal("Pages", Assert.IsType<NameObject>(pages["Type"]).Value);
    }

    // --- Item 5: stream-filter bombs and chain length -----------------------

    [Fact]
    public void FilterChainBomb_ExceedsCap_Throws()
    {
        // Filter chain [/ASCII85Decode /FlateDecode]: the inner Flate expands 1 MB
        // of zeros far past the tiny per-stream cap.
        var inner = FlateFilter.Encode(new byte[1024 * 1024]);
        var payload = Ascii85Filter.Encode(inner);
        var bytes = StreamObjectFile("<< /Length LEN /Filter [/ASCII85Decode /FlateDecode] >>", payload);

        var reader = DocumentReader.Parse(bytes, null, new ReaderLimits { MaxDecodedStreamBytes = 4096 });
        var stream = Assert.IsType<StreamObject>(reader.GetObject(4));
        Assert.Throws<DocumentParseException>(() => reader.DecodeStream(stream));
    }

    [Fact]
    public void ValidFilterChain_Decodes_PositiveControl()
    {
        var expected = Encoding.ASCII.GetBytes("Hello filter chain world");
        var payload = Ascii85Filter.Encode(FlateFilter.Encode(expected));
        var bytes = StreamObjectFile("<< /Length LEN /Filter [/ASCII85Decode /FlateDecode] >>", payload);

        var reader = DocumentReader.Parse(bytes);
        var stream = Assert.IsType<StreamObject>(reader.GetObject(4));
        Assert.Equal(expected, reader.DecodeStream(stream));
    }

    [Fact]
    public void OverLongFilterChain_Throws()
    {
        var filters = new StringBuilder("[");
        for (var i = 0; i < ReaderLimits.Default.MaxFilterChainLength + 1; i++)
        {
            filters.Append(" /ASCII85Decode");
        }

        filters.Append(" ]");

        var bytes = StreamObjectFile($"<< /Length LEN /Filter {filters} >>", Array.Empty<byte>());
        var reader = DocumentReader.Parse(bytes);
        var stream = Assert.IsType<StreamObject>(reader.GetObject(4));
        Assert.Throws<DocumentParseException>(() => reader.DecodeStream(stream));
    }

    // --- fixtures -----------------------------------------------------------

    // Assembles a classic cross-reference file from contiguous objects 1..k.
    private static byte[] ClassicXref(params (int Number, string Body)[] objects)
    {
        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        foreach (var (number, body) in objects)
        {
            pdf.Object(number, $"{number} 0 obj\n{body}\nendobj\n");
        }

        return AppendClassicTrailer(pdf, objects.Length, root: 1);
    }

    private static byte[] StreamObjectFile(string dictionary, byte[] payload)
    {
        var dict = dictionary.Replace("LEN", payload.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>\nendobj\n");
        pdf.Mark(4).Append($"4 0 obj\n{dict}\nstream\n").Append(payload).Append("\nendstream\nendobj\n");
        return AppendClassicTrailer(pdf, count: 4, root: 1);
    }

    private static byte[] AppendClassicTrailer(FixturePdf pdf, int count, int root)
    {
        var xref = pdf.Position;
        pdf.Append($"xref\n0 {count + 1}\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= count; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append($"trailer\n<< /Size {count + 1} /Root {root} 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    // Newest section is a /Type /XRef stream with /W [0 0 0] and a huge /Size, plus
    // a valid catalog/pages/page reachable by the repair scan.
    private static byte[] ZeroWidthXrefStreamFile()
    {
        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>\nendobj\n");

        var offset4 = pdf.Position;
        pdf.Append("4 0 obj\n<< /Type /XRef /Size 100000000 /Index [1 100000000] /W [0 0 0] /Root 1 0 R /Length 0 >>\nstream\n")
            .Append("\nendstream\nendobj\n")
            .Append("startxref\n" + offset4 + "\n%%EOF\n");
        return pdf.ToArray();
    }

    // Hand-built minimal xref stream (/W [1 2 1], no filter/predictor).
    private static byte[] RawXrefStreamFile()
    {
        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 0 /Kids [] >>\nendobj\n");

        var offset3 = pdf.Position;
        var payload = new byte[12];
        Copy(payload, 0, FixturePdf.XrefStreamEntry(1, (int)pdf.OffsetOf(1), 0));
        Copy(payload, 4, FixturePdf.XrefStreamEntry(1, (int)pdf.OffsetOf(2), 0));
        Copy(payload, 8, FixturePdf.XrefStreamEntry(1, (int)offset3, 0));

        pdf.Append("3 0 obj\n<< /Type /XRef /Size 4 /Index [1 3] /W [1 2 1] /Root 1 0 R /Length 12 >>\nstream\n")
            .Append(payload)
            .Append("\nendstream\nendobj\n")
            .Append("startxref\n" + offset3 + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static byte[] HugeCountObjStmFile() => ObjStmFile(declaredCount: 2000000000);

    private static byte[] ValidObjStmFile() => ObjStmFile(declaredCount: 2);

    // ObjStm holding objects 1 (Catalog) and 2 (Pages), referenced by an xref
    // stream; /N is set to declaredCount so a hostile value can be exercised.
    private static byte[] ObjStmFile(long declaredCount)
    {
        var b1 = "<< /Type /Catalog /Pages 2 0 R >>";
        var b2 = "<< /Type /Pages /Count 0 /Kids [] >>";
        var o1 = 0;
        var o2 = (b1 + " ").Length;
        var header = $"1 {o1} 2 {o2} ";
        var body = b1 + " " + b2;
        var stmData = header + body;
        var first = header.Length;

        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        var offset3 = pdf.Position;
        pdf.Append($"3 0 obj\n<< /Type /ObjStm /N {declaredCount} /First {first} /Length {stmData.Length} >>\nstream\n")
            .Append(stmData)
            .Append("\nendstream\nendobj\n");

        var offset4 = pdf.Position;
        var payload = new byte[16];
        Copy(payload, 0, FixturePdf.XrefStreamEntry(2, 3, 0));
        Copy(payload, 4, FixturePdf.XrefStreamEntry(2, 3, 1));
        Copy(payload, 8, FixturePdf.XrefStreamEntry(1, (int)offset3, 0));
        Copy(payload, 12, FixturePdf.XrefStreamEntry(1, (int)offset4, 0));

        pdf.Append("4 0 obj\n<< /Type /XRef /Size 5 /Index [1 4] /W [1 2 1] /Root 1 0 R /Length 16 >>\nstream\n")
            .Append(payload)
            .Append("\nendstream\nendobj\n")
            .Append("startxref\n" + offset4 + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static void Copy(byte[] target, int at, byte[] source)
    {
        for (var i = 0; i < source.Length; i++)
        {
            target[at + i] = source[i];
        }
    }
}
