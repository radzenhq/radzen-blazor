#nullable enable
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ObjectStreamRetentionTests
{
    private static ConcurrentDictionary<int, ObjectStream> ObjectStreamsOf(DocumentReader reader)
    {
        var store = typeof(DocumentReader)
            .GetField("store", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(reader)!;

        return (ConcurrentDictionary<int, ObjectStream>)store.GetType()
            .GetField("objectStreams", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(store)!;
    }

    private static long RetainedDecodedBytes(DocumentReader reader)
    {
        long total = 0;
        foreach (var pair in ObjectStreamsOf(reader))
        {
            total += pair.Value.Data.Length;
        }

        return total;
    }

    private static byte[] HandCraftedObjStmFile(int extraBogusMembers = 0)
    {
        var b1 = "42";
        var b2 = "<< /A 1 /B (x) >>";
        var b3 = "<< /Type /Catalog >>";
        var body = b1 + "\n" + b2 + "\n" + b3;
        var o2 = (b1 + "\n").Length;
        var o3 = (b1 + "\n" + b2 + "\n").Length;
        var header = $"1 0 2 {o2} 3 {o3} ";
        var stmData = header + body;

        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        var offset4 = pdf.Position;
        pdf.Append($"4 0 obj\n<< /Type /ObjStm /N 3 /First {header.Length} /Length {stmData.Length} >>\nstream\n")
            .Append(stmData)
            .Append("\nendstream\nendobj\n");

        var offset5 = pdf.Position;
        var count = 5 + extraBogusMembers;
        var payload = new byte[count * 4];
        Copy(payload, 0, FixturePdf.XrefStreamEntry(2, 4, 0));
        Copy(payload, 4, FixturePdf.XrefStreamEntry(2, 4, 1));
        Copy(payload, 8, FixturePdf.XrefStreamEntry(2, 4, 2));
        Copy(payload, 12, FixturePdf.XrefStreamEntry(1, (int)offset4, 0));
        Copy(payload, 16, FixturePdf.XrefStreamEntry(1, (int)offset5, 0));
        for (var i = 0; i < extraBogusMembers; i++)
        {
            Copy(payload, 20 + (i * 4), FixturePdf.XrefStreamEntry(2, 4, 99));
        }

        pdf.Append($"5 0 obj\n<< /Type /XRef /Size {count + 1} /Index [1 {count}] /W [1 2 1] /Root 3 0 R /Length {payload.Length} >>\nstream\n")
            .Append(payload)
            .Append("\nendstream\nendobj\n")
            .Append("startxref\n" + offset5 + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static void Copy(byte[] target, int at, byte[] source)
    {
        for (var i = 0; i < source.Length; i++)
        {
            target[at + i] = source[i];
        }
    }

    [Fact]
    public void ObjStm_DecodedPayloadIsHeldWhileMembersAreStillUnresolved()
    {
        var reader = DocumentReader.Parse(HandCraftedObjStmFile());

        reader.GetObject(1);

        Assert.True(RetainedDecodedBytes(reader) > 0);
    }

    [Fact]
    public void ObjStm_DecodedPayloadIsReleasedOnceEveryMemberIsResolved()
    {
        var reader = DocumentReader.Parse(HandCraftedObjStmFile());

        reader.GetObject(1);
        reader.GetObject(2);
        Assert.True(RetainedDecodedBytes(reader) > 0);

        reader.GetObject(3);

        Assert.Equal(0, RetainedDecodedBytes(reader));
    }

    [Fact]
    public void ObjStm_ResolvingTheSameMemberTwiceDoesNotDrainTheContainerEarly()
    {
        var reader = DocumentReader.Parse(HandCraftedObjStmFile());

        reader.GetObject(1);
        reader.GetObject(1);
        reader.GetObject(1);

        Assert.True(RetainedDecodedBytes(reader) > 0);
    }

    [Fact]
    public void ObjStm_EntriesThatCanNeverParseKeepTheContainerCached()
    {
        var reader = DocumentReader.Parse(HandCraftedObjStmFile(extraBogusMembers: 3));

        reader.GetObject(1);
        reader.GetObject(2);
        reader.GetObject(3);

        Assert.True(
            RetainedDecodedBytes(reader) > 0,
            "a container an unparseable entry can still request must not be evicted");
    }

    [Fact]
    public void ObjStm_MembersStayCorrectAndSingleValuedAfterTheContainerIsReleased()
    {
        var reader = DocumentReader.Parse(HandCraftedObjStmFile());

        var first = new List<DocumentObject> { reader.GetObject(1), reader.GetObject(2), reader.GetObject(3) };
        Assert.Equal(0, RetainedDecodedBytes(reader));

        Assert.Equal(42, Assert.IsType<NumberObject>(reader.GetObject(1)).IntValue);
        var dict = Assert.IsType<DictionaryObject>(reader.GetObject(2));
        Assert.Equal(1, Assert.IsType<NumberObject>(dict["A"]).IntValue);
        Assert.Equal("x", Assert.IsType<StringObject>(dict["B"]).Value);
        Assert.Equal("Catalog", Assert.IsType<NameObject>(Assert.IsType<DictionaryObject>(reader.GetObject(3))["Type"]).Value);

        Assert.Same(first[0], reader.GetObject(1));
        Assert.Same(first[1], reader.GetObject(2));
        Assert.Same(first[2], reader.GetObject(3));
    }

    [Fact]
    public void QpdfFixture_FullyResolvedDocumentRetainsNoDecodedObjectStreams()
    {
        var reader = DocumentReader.Parse(PdfTestResources.ReadAllBytes("Documents/xref-stream-objstm.pdf"));

        for (var number = 1; number <= reader.ObjectCount; number++)
        {
            reader.GetObject(number);
        }

        Assert.Equal(0, RetainedDecodedBytes(reader));
    }

    [Fact]
    public void CompressedDocument_HoldsNoDecodedObjectStreamsAfterAFullGraphWalk()
    {
        var document = new Document();
        var section = document.Sections.Add();
        for (var i = 0; i < 2000; i++)
        {
            section.Blocks.Add(new Paragraph($"Compressible line number {i} with repeated filler text."));
        }

        var rendered = new DocumentRenderer().Render(document);
        rendered.CompressOutput = true;

        var reader = DocumentReader.Parse(rendered.ToArray());
        for (var number = 1; number <= reader.ObjectCount; number++)
        {
            reader.GetObject(number);
        }

        Assert.Equal(0, RetainedDecodedBytes(reader));
    }
}
