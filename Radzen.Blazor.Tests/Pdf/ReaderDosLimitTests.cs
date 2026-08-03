#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ReaderDosLimitTests
{
    private static byte[] FlateBomb(int decodedLength)
        => FlateFilter.Encode(new byte[decodedLength]);

    [Fact]
    public void DecodeExpansionRatioBelowTheFloor_IsNotEnforced()
    {
        var limits = new ReaderLimits
        {
            MaxDecodeExpansionRatio = 1,
            ExpansionRatioFloorBytes = 1024 * 1024,
        };
        var dictionary = new DictionaryObject { ["Filter"] = new NameObject("FlateDecode") };

        var decoded = new StreamDecoder(limits, value => value).Decode(dictionary, FlateBomb(64 * 1024));

        Assert.Equal(64 * 1024, decoded.Length);
    }

    [Fact]
    public void DecodeExpansionRatioAboveTheFloor_ThrowsDocumentParseException()
    {
        var limits = new ReaderLimits
        {
            MaxDecodeExpansionRatio = 1,
            ExpansionRatioFloorBytes = 1024,
        };
        var dictionary = new DictionaryObject { ["Filter"] = new NameObject("FlateDecode") };

        var error = Assert.Throws<DocumentParseException>(
            () => new StreamDecoder(limits, value => value).Decode(dictionary, FlateBomb(64 * 1024)));

        Assert.Contains("expansion ratio", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DecodeExpansionRatioIsAppliedOnlyWhenBothTheFloorAndTheRatioAreExceeded()
    {
        var limits = new ReaderLimits
        {
            MaxDecodeExpansionRatio = 1_000_000,
            ExpansionRatioFloorBytes = 1024,
        };
        var dictionary = new DictionaryObject { ["Filter"] = new NameObject("FlateDecode") };

        var decoded = new StreamDecoder(limits, value => value).Decode(dictionary, FlateBomb(64 * 1024));

        Assert.Equal(64 * 1024, decoded.Length);
    }

    [Fact]
    public void ObjectStreamMemberCountBeyondTheLimit_IsClampedInsteadOfAllocated()
    {
        var file = ObjectStreamFile();

        var unbounded = DocumentReader.Parse(file, null, new ReaderLimits { MaxObjectStreamCount = 3 });
        var clamped = DocumentReader.Parse(file, null, new ReaderLimits { MaxObjectStreamCount = 1 });

        Assert.Equal(42, Assert.IsType<NumberObject>(unbounded.GetObject(1)).IntValue);
        Assert.Equal("Catalog", Assert.IsType<NameObject>(
            Assert.IsType<DictionaryObject>(unbounded.GetObject(3))["Type"]).Value);

        Assert.Equal(42, Assert.IsType<NumberObject>(clamped.GetObject(1)).IntValue);

        var error = Assert.Throws<DocumentParseException>(() => clamped.GetObject(3));

        Assert.Contains("Object stream index out of range", error.Message, StringComparison.Ordinal);
    }

    private static byte[] ObjectStreamFile()
    {
        const string first = "42";
        const string second = "<< /A 1 /B (x) >>";
        const string third = "<< /Type /Catalog >>";
        var body = first + "\n" + second + "\n" + third;
        var header = $"1 0 2 {(first + "\n").Length} 3 {(first + "\n" + second + "\n").Length} ";
        var data = header + body;

        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        var objStm = pdf.Position;
        pdf.Append($"4 0 obj\n<< /Type /ObjStm /N 3 /First {header.Length} /Length {data.Length} >>\nstream\n")
            .Append(data)
            .Append("\nendstream\nendobj\n");

        var xref = pdf.Position;
        var payload = new byte[20];
        Copy(payload, 0, FixturePdf.XrefStreamEntry(2, 4, 0));
        Copy(payload, 4, FixturePdf.XrefStreamEntry(2, 4, 1));
        Copy(payload, 8, FixturePdf.XrefStreamEntry(2, 4, 2));
        Copy(payload, 12, FixturePdf.XrefStreamEntry(1, (int)objStm, 0));
        Copy(payload, 16, FixturePdf.XrefStreamEntry(1, (int)xref, 0));

        pdf.Append("5 0 obj\n<< /Type /XRef /Size 6 /Index [1 5] /W [1 2 1] /Root 3 0 R /Length 20 >>\nstream\n")
            .Append(payload)
            .Append("\nendstream\nendobj\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");

        return pdf.ToArray();
    }

    private static void Copy(byte[] target, int at, byte[] source)
    {
        for (var i = 0; i < source.Length; i++)
        {
            target[at + i] = source[i];
        }
    }

    private static string StreamBody(string content)
        => $"<< /Length {content.Length} >>\nstream\n{content}\nendstream";

    private static byte[] ClassicFile(params (int Number, string Body)[] objects)
    {
        var pdf = new FixturePdf().Append("%PDF-1.4\n");
        foreach (var (number, body) in objects)
        {
            pdf.Object(number, $"{number} 0 obj\n{body}\nendobj\n");
        }

        var xref = pdf.Position;
        var max = objects[^1].Number;
        pdf.Append($"xref\n0 {max + 1}\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= max; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        return pdf.Append($"trailer\n<< /Size {max + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n").ToArray();
    }

    [Fact]
    public void ContentsArrayExceedingAggregateBudget_Throws()
    {
        var bytes = ClassicFile(
            (1, "<< /Type /Catalog /Pages 2 0 R >>"),
            (2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            (3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents [4 0 R 5 0 R 6 0 R] >>"),
            (4, StreamBody("1234")),
            (5, StreamBody("5678")),
            (6, StreamBody("90ab")));
        var limits = new ReaderLimits { MaxDecodedStreamBytes = 8, MaxAggregateDecodedBytes = 10 };

        Assert.Throws<DocumentParseException>(
            () => PortableDocument.LoadFromStream(new MemoryStream(bytes), limits));
    }

    [Fact]
    public void RepairScanExceedingXrefBudget_Throws()
    {
        var pdf = new FixturePdf().Append("%PDF-1.4\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>\nendobj\n");
        pdf.Append("startxref\n0\n%%EOF\n");

        Assert.Throws<DocumentParseException>(
            () => DocumentReader.Parse(pdf.ToArray(), null, new ReaderLimits { MaxXrefEntries = 2 }));
    }

    [Fact]
    public void ByteArrayOverFileCap_Throws()
    {
        var bytes = ClassicFile((1, "<< /Type /Catalog /Pages 2 0 R >>"), (2, "<< /Type /Pages /Kids [] /Count 0 >>"));

        Assert.Throws<DocumentParseException>(
            () => DocumentReader.Parse(bytes, null, new ReaderLimits { MaxFileBytes = bytes.Length - 1 }));
    }

    [Fact]
    public void NonSeekableStreamOverFileCap_Throws()
    {
        var bytes = ClassicFile((1, "<< /Type /Catalog /Pages 2 0 R >>"), (2, "<< /Type /Pages /Kids [] /Count 0 >>"));
        using var stream = new NonSeekableStream(bytes);

        Assert.Throws<DocumentParseException>(
            () => DocumentReader.Parse(stream, null, new ReaderLimits { MaxFileBytes = bytes.Length - 1 }));
    }

    private static byte[] ClassicXrefFile()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n(a string)\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 4\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(3)))
            .Append("trailer\n<< /Size 4 /Root 1 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void ClassicXref_TightenedMaxXrefEntries_Throws()
    {
        var file = ClassicXrefFile();
        var tight = new ReaderLimits { MaxXrefEntries = 2 };

        Assert.Throws<DocumentParseException>(() => DocumentReader.Parse(file, null, tight));
    }

    [Fact]
    public void ClassicXref_DefaultLimits_ParseViaXref()
    {
        Assert.Equal(3, DocumentReader.Parse(ClassicXrefFile()).ObjectCount);
    }

    [Fact]
    public void ObjectParser_HonorsTightenedNestingDepth()
    {
        var bytes = Encoding.Latin1.GetBytes("[[[1]]]");
        var tight = new ReaderLimits { MaxObjectNestingDepth = 2 };
        Assert.Throws<DocumentParseException>(() => ObjectParser.Parse(bytes, 0, tight));
        Assert.NotNull(ObjectParser.Parse(bytes, 0));
    }
}
