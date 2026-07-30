using System.Globalization;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

// Cross-reference table + trailer per ISO 32000-1 section 7.5
public class XrefReaderTests
{
    private static string Entry19(long offset)
        => offset.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n\n";

    private static byte[] StandardFile()
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
    public void ObjectCount_ExcludesFreeEntry()
    {
        Assert.Equal(3, DocumentReader.Parse(StandardFile()).ObjectCount);
    }

    [Fact]
    public void GetObject_ReturnsCorrectTypesAndValues()
    {
        var reader = DocumentReader.Parse(StandardFile());

        var catalog = Assert.IsType<DictionaryObject>(reader.GetObject(1));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
        Assert.Equal(2, Assert.IsType<ReferenceObject>(catalog["Pages"]).ObjectNumber);

        var pages = Assert.IsType<DictionaryObject>(reader.GetObject(2));
        Assert.Equal(1, Assert.IsType<NumberObject>(pages["Count"]).IntValue);

        Assert.Equal("a string", Assert.IsType<StringObject>(reader.GetObject(3)).Value);
    }

    [Fact]
    public void Trailer_RootAndSize()
    {
        var reader = DocumentReader.Parse(StandardFile());
        Assert.Equal(4, Assert.IsType<NumberObject>(reader.Trailer["Size"]).IntValue);
        var root = Assert.IsType<ReferenceObject>(reader.Trailer["Root"]);
        Assert.Equal(1, root.ObjectNumber);
    }

    [Fact]
    public void Resolve_FollowsReference()
    {
        var reader = DocumentReader.Parse(StandardFile());
        var resolved = reader.Resolve(reader.Trailer["Root"]);
        var catalog = Assert.IsType<DictionaryObject>(resolved);
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }

    [Fact]
    public void Resolve_NonReferenceReturnsItself()
    {
        var reader = DocumentReader.Parse(StandardFile());
        var number = new NumberObject(99);
        Assert.Same(number, reader.Resolve(number));
    }

    [Fact]
    public void Parse_StreamOverload()
    {
        using var stream = new MemoryStream(StandardFile());
        var reader = DocumentReader.Parse(stream);
        Assert.Equal(3, reader.ObjectCount);
        Assert.Equal("a string", Assert.IsType<StringObject>(reader.GetObject(3)).Value);
    }

    [Fact]
    public void FreeEntries_SkippedAndOthersResolve()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog >>\nendobj\n")
            .Object(3, "3 0 obj\n(third)\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 4\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(0, 0, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(3)))
            .Append("trailer\n<< /Size 4 /Root 1 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");
        var reader = DocumentReader.Parse(pdf.ToArray());

        Assert.Equal(2, reader.ObjectCount);
        Assert.Equal("Catalog", Assert.IsType<NameObject>(
            Assert.IsType<DictionaryObject>(reader.GetObject(1))["Type"]).Value);
        Assert.Equal("third", Assert.IsType<StringObject>(reader.GetObject(3)).Value);
    }

    [Fact]
    public void Generation_NonZeroResolves()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Ref 2 3 R >>\nendobj\n")
            .Object(2, "2 3 obj\n(gen three)\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 3\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2), 3))
            .Append("trailer\n<< /Size 3 /Root 1 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");
        var reader = DocumentReader.Parse(pdf.ToArray());

        Assert.Equal("gen three", Assert.IsType<StringObject>(reader.GetObject(2)).Value);

        var catalog = Assert.IsType<DictionaryObject>(reader.GetObject(1));
        var reference = Assert.IsType<ReferenceObject>(catalog["Ref"]);
        Assert.Equal(3, reference.Generation);
        Assert.Equal("gen three", Assert.IsType<StringObject>(reader.Resolve(reference)).Value);
    }

    [Fact]
    public void ShortEntries_NineteenByteLoneLfAccepted()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n(x)\nendobj\n")
            .Object(2, "2 0 obj\n(y)\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 3\n")
            .Append("0000000000 65535 f\n")
            .Append(Entry19(pdf.OffsetOf(1)))
            .Append(Entry19(pdf.OffsetOf(2)))
            .Append("trailer\n<< /Size 3 /Root 1 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");
        var reader = DocumentReader.Parse(pdf.ToArray());

        Assert.Equal("x", Assert.IsType<StringObject>(reader.GetObject(1)).Value);
        Assert.Equal("y", Assert.IsType<StringObject>(reader.GetObject(2)).Value);
    }

    [Fact]
    public void StartxrefScannedBackwardPastTrailingBytes()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n(only)\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 2\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append("trailer\n<< /Size 2 /Root 1 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n")
            .Append("\n   \n");
        var reader = DocumentReader.Parse(pdf.ToArray());

        Assert.Equal("only", Assert.IsType<StringObject>(reader.GetObject(1)).Value);
    }

    [Fact]
    public void IncrementalUpdate_PrevChained_UpdatedObjectWins()
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");

        var offset1 = pdf.Position;
        pdf.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        var offset2 = pdf.Position;
        pdf.Append("2 0 obj\n<< /V 100 >>\nendobj\n");
        var offset3Original = pdf.Position;
        pdf.Append("3 0 obj\n(original)\nendobj\n");

        var xref1 = pdf.Position;
        pdf.Append("xref\n0 4\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(offset1))
            .Append(FixturePdf.Entry20(offset2))
            .Append(FixturePdf.Entry20(offset3Original))
            .Append("trailer\n<< /Size 4 /Root 1 0 R >>\n")
            .Append("startxref\n" + xref1 + "\n%%EOF\n");

        var offset3Updated = pdf.Position;
        pdf.Append("3 0 obj\n(updated)\nendobj\n");

        var xref2 = pdf.Position;
        pdf.Append("xref\n3 1\n")
            .Append(FixturePdf.Entry20(offset3Updated))
            .Append("trailer\n<< /Size 4 /Root 1 0 R /Prev " + xref1 + " >>\n")
            .Append("startxref\n" + xref2 + "\n%%EOF\n");
        var reader = DocumentReader.Parse(pdf.ToArray());

        Assert.Equal("updated", Assert.IsType<StringObject>(reader.GetObject(3)).Value);
        Assert.Equal("Catalog", Assert.IsType<NameObject>(
            Assert.IsType<DictionaryObject>(reader.GetObject(1))["Type"]).Value);
        Assert.Equal(100, Assert.IsType<NumberObject>(
            Assert.IsType<DictionaryObject>(reader.GetObject(2))["V"]).IntValue);
        Assert.Equal(4, Assert.IsType<NumberObject>(reader.Trailer["Size"]).IntValue);
    }
}
