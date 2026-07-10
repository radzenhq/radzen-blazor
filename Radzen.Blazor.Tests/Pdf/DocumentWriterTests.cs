using System.Globalization;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

// Whole-file structural contract for DocumentWriter: header, indirect object
// bodies, classic cross-reference table, and trailer (ISO 32000-1 section 7.5).
// Assertions are crude ASCII/byte scans - offsets are verified arithmetically
// by locating each "N 0 obj" in the output and matching the parsed xref entry.
public class DocumentWriterTests
{
    private sealed class WrittenPdf
    {
        public byte[] Bytes = System.Array.Empty<byte>();
        public string Text = "";
    }

    private static WrittenPdf WriteMinimalDocument()
    {
        using var ms = new MemoryStream();
        var writer = new DocumentWriter(ms);

        var catalog = new DictionaryObject();
        var pages = new DictionaryObject();
        var page = new DictionaryObject();

        var catalogRef = writer.Add(catalog);
        var pagesRef = writer.Add(pages);
        var pageRef = writer.Add(page);

        catalog["Type"] = new NameObject("Catalog");
        catalog["Pages"] = pagesRef;

        pages["Type"] = new NameObject("Pages");
        var kids = new ArrayObject();
        kids.Add(pageRef);
        pages["Kids"] = kids;
        pages["Count"] = new NumberObject(1);

        page["Type"] = new NameObject("Page");
        page["Parent"] = pagesRef;
        var box = new ArrayObject();
        box.Add(new NumberObject(0));
        box.Add(new NumberObject(0));
        box.Add(new NumberObject(612));
        box.Add(new NumberObject(792));
        page["MediaBox"] = box;

        writer.Trailer["Root"] = catalogRef;
        writer.Close();

        var bytes = ms.ToArray();
        return new WrittenPdf { Bytes = bytes, Text = Encoding.Latin1.GetString(bytes) };
    }

    [Fact]
    public void Add_ReturnsSequentialReferencesFromOne()
    {
        using var ms = new MemoryStream();
        var writer = new DocumentWriter(ms);

        var first = writer.Add(new DictionaryObject());
        var second = writer.Add(new DictionaryObject());
        var third = writer.Add(new DictionaryObject());

        Assert.Equal("1 0 R", Serialize(first));
        Assert.Equal("2 0 R", Serialize(second));
        Assert.Equal("3 0 R", Serialize(third));
    }

    private static string Serialize(DocumentObject obj)
    {
        using var ms = new MemoryStream();
        obj.Write(ms);
        return Encoding.Latin1.GetString(ms.ToArray());
    }

    [Fact]
    public void Header_StartsWithVersionAndBinaryComment()
    {
        var pdf = WriteMinimalDocument();
        Assert.StartsWith("%PDF-1.7\n", pdf.Text);

        Assert.Equal((byte)'%', pdf.Bytes[9]);
        for (var i = 10; i < 14; i++)
        {
            Assert.True(pdf.Bytes[i] >= 0x80, $"binary comment byte {i} = 0x{pdf.Bytes[i]:X2}");
        }
        Assert.Equal((byte)'\n', pdf.Bytes[14]);
    }

    [Fact]
    public void Body_WritesIndirectObjectDefinitions()
    {
        var pdf = WriteMinimalDocument();
        Assert.Contains("\n1 0 obj\n", pdf.Text);
        Assert.Contains("\n2 0 obj\n", pdf.Text);
        Assert.Contains("\n3 0 obj\n", pdf.Text);
        Assert.Contains("\nendobj\n", pdf.Text);
        Assert.Equal(3, CountOccurrences(pdf.Text, "endobj"));
    }

    [Fact]
    public void Xref_HasHeaderWithObjectCount()
    {
        var pdf = WriteMinimalDocument();
        Assert.Contains("xref\n0 4\n", pdf.Text);
    }

    [Fact]
    public void Xref_FirstEntryIsFreeHead()
    {
        var pdf = WriteMinimalDocument();
        var start = pdf.Text.IndexOf("xref\n0 4\n", System.StringComparison.Ordinal) + "xref\n0 4\n".Length;
        var firstEntry = pdf.Text.Substring(start, 20);
        Assert.Equal("0000000000 65535 f \n", firstEntry);
    }

    [Fact]
    public void Xref_EntriesAreTwentyBytesAndPointAtObjects()
    {
        var pdf = WriteMinimalDocument();
        var dataStart = pdf.Text.IndexOf("xref\n0 4\n", System.StringComparison.Ordinal) + "xref\n0 4\n".Length;

        for (var objNumber = 1; objNumber <= 3; objNumber++)
        {
            var entry = pdf.Text.Substring(dataStart + objNumber * 20, 20);
            Assert.Equal(20, entry.Length);
            Assert.Equal(" 00000 n \n", entry.Substring(10));

            var parsedOffset = int.Parse(entry.Substring(0, 10), CultureInfo.InvariantCulture);
            var actualOffset = pdf.Text.IndexOf($"\n{objNumber} 0 obj\n", System.StringComparison.Ordinal) + 1;
            Assert.Equal(actualOffset, parsedOffset);
        }
    }

    [Fact]
    public void Trailer_ContainsSizeAndRoot()
    {
        var pdf = WriteMinimalDocument();
        Assert.Contains("trailer\n", pdf.Text);
        Assert.Contains("/Size 4", pdf.Text);
        Assert.Contains("/Root 1 0 R", pdf.Text);
    }

    [Fact]
    public void StartXref_PointsAtXrefTableAndEndsWithEof()
    {
        var pdf = WriteMinimalDocument();
        var xrefOffset = pdf.Text.IndexOf("xref\n0 4\n", System.StringComparison.Ordinal);

        var marker = "startxref\n";
        var markerIndex = pdf.Text.IndexOf(marker, System.StringComparison.Ordinal);
        Assert.True(markerIndex > xrefOffset, "startxref must follow the xref table");

        var rest = pdf.Text.Substring(markerIndex + marker.Length);
        var newline = rest.IndexOf('\n');
        var parsedOffset = int.Parse(rest.Substring(0, newline), CultureInfo.InvariantCulture);
        Assert.Equal(xrefOffset, parsedOffset);

        Assert.EndsWith("%%EOF", pdf.Text.TrimEnd('\n'));
    }

    [Fact]
    public void MinimalDocument_ScansPassEndToEnd()
    {
        var pdf = WriteMinimalDocument();
        Assert.StartsWith("%PDF-1.7\n", pdf.Text);
        Assert.Contains("/Type /Catalog", pdf.Text);
        Assert.Contains("/Type /Pages", pdf.Text);
        Assert.Contains("/Type /Page", pdf.Text);
        Assert.Contains("/MediaBox [0 0 612 792]", pdf.Text);
        Assert.Contains("/Kids [3 0 R]", pdf.Text);
        Assert.Contains("xref\n0 4\n", pdf.Text);
        Assert.Contains("%%EOF", pdf.Text);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}
