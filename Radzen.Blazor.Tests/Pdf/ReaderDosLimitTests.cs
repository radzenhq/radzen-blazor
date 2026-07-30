#nullable enable
using System;
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
}
