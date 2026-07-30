#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class RepairTests
{
    private static byte[] ValidDocument()
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

        writer.Trailer["Root"] = catalogRef;
        writer.Close();
        return ms.ToArray();
    }

    private static byte[] Replace(byte[] bytes, string find, string with)
    {
        var text = Encoding.Latin1.GetString(bytes);
        var at = text.IndexOf(find, StringComparison.Ordinal);
        Assert.True(at >= 0, $"Anchor '{find}' not found.");
        var patched = text.Substring(0, at) + with + text.Substring(at + find.Length);
        return Encoding.Latin1.GetBytes(patched);
    }

    [Fact]
    public void CorruptStartxrefOffset_ScansAndResolvesRoot()
    {
        var bytes = Replace(ValidDocument(), "startxref\n", "startxref\n0000000001\n");
        var reader = DocumentReader.Parse(TruncateAfter(bytes, "%%EOF"));

        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
        Assert.Equal("Page", Assert.IsType<NameObject>(
            Assert.IsType<DictionaryObject>(reader.GetObject(3))["Type"]).Value);
    }

    [Fact]
    public void CorruptSingleEntryOffset_GetObjectFallsBackToScan()
    {
        var bytes = ValidDocument();
        var text = Encoding.Latin1.GetString(bytes);

        var header = "xref\n0 4\n";
        var xi = text.IndexOf(header, StringComparison.Ordinal);
        Assert.True(xi >= 0);
        var obj3EntryOffset = xi + header.Length + 20 * 3;

        var patched = text.Substring(0, obj3EntryOffset) + "0000000030"
            + text.Substring(obj3EntryOffset + 10);
        var reader = DocumentReader.Parse(Encoding.Latin1.GetBytes(patched));

        var page = Assert.IsType<DictionaryObject>(reader.GetObject(3));
        Assert.Equal("Page", Assert.IsType<NameObject>(page["Type"]).Value);
    }

    [Fact]
    public void TruncatedXref_ReconstructsTrailerFromCatalog()
    {
        var bytes = ValidDocument();
        var text = Encoding.Latin1.GetString(bytes);
        var xi = text.IndexOf("xref", StringComparison.Ordinal);
        Assert.True(xi >= 0);
        var truncated = Encoding.Latin1.GetBytes(text.Substring(0, xi));

        var reader = DocumentReader.Parse(truncated);

        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);

        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        Assert.Equal("Pages", Assert.IsType<NameObject>(pages["Type"]).Value);
    }

    [Fact]
    public void ReconstructedRootMatchesTheCatalogWithPages()
    {
        var data = Encoding.ASCII.GetBytes(
            "%PDF-1.7\n"
            + "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"
            + "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n"
            + "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n"
            + "5 0 obj\n<< /Type /Catalog >>\nendobj\n");

        var document = Document.LoadFromStream(new MemoryStream(data));
        var reader = document.Loaded!.Source!;
        var root = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));

        Assert.True(root.TryGetValue("Pages", out var pages) && pages is not null);
        var pagesDictionary = Assert.IsType<DictionaryObject>(reader.Resolve(pages!));
        Assert.Equal("Pages", Assert.IsType<NameObject>(pagesDictionary["Type"]).Value);
    }

    [Fact]
    public void TotalGarbage_ThrowsDocumentParseException()
    {
        var garbage = new byte[256];
        for (var i = 0; i < garbage.Length; i++)
        {
            garbage[i] = (byte)((i * 7 + 3) & 0x7F);
        }

        Assert.Throws<DocumentParseException>(() => DocumentReader.Parse(garbage));
    }

    [Fact]
    public void EmptyInput_ThrowsDocumentParseException()
    {
        Assert.Throws<DocumentParseException>(() => DocumentReader.Parse(Array.Empty<byte>()));
    }

    [Fact]
    public void ObjectHeaderInsideStreamDoesNotReplaceRealObjectOffset()
    {
        var data = Encoding.ASCII.GetBytes(
            "1 0 obj\n<< /Type /Catalog >>\nendobj\n"
            + "2 0 obj\n<< /Length 36 >>\nstream\n"
            + "1 0 obj\n<< /Type /Fake >>\nendobj\n"
            + "endstream\nendobj\n");

        var offsets = new DocumentRepairer(data, ReaderLimits.Default).ScannedOffsets();

        Assert.Equal(0, offsets[1]);
    }

    [Fact]
    public void IncrementalUpdate_RepairKeepsLatestObjectRevision()
    {
        var text = "1 0 obj\n<< /Type /Catalog /V 1 >>\nendobj\n"
            + "1 0 obj\n<< /Type /Catalog /V 2 >>\nendobj\n";
        var data = Encoding.ASCII.GetBytes(text);
        var latest = text.IndexOf("1 0 obj", text.IndexOf("1 0 obj", StringComparison.Ordinal) + 1, StringComparison.Ordinal);

        var offsets = new DocumentRepairer(data, ReaderLimits.Default).ScannedOffsets();

        Assert.Equal(latest, offsets[1]);
    }

    [Fact]
    public void ObjectHeaderInsideStringDoesNotReplaceRealObjectOffset()
    {
        var data = Encoding.ASCII.GetBytes(
            "1 0 obj\n<< /Type /Catalog >>\nendobj\n"
            + "2 0 obj\n<< /Note (see 1 0 obj here) >>\nendobj\n");

        var offsets = new DocumentRepairer(data, ReaderLimits.Default).ScannedOffsets();

        Assert.Equal(0, offsets[1]);
    }

    [Fact]
    public void StreamWordInsideStringDoesNotHideLaterObjects()
    {
        var data = Encoding.ASCII.GetBytes(
            "1 0 obj\n<< /Note (a stream\nhere) >>\nendobj\n"
            + "2 0 obj\n<< /Type /Catalog >>\nendobj\n");

        var offsets = new DocumentRepairer(data, ReaderLimits.Default).ScannedOffsets();

        Assert.True(offsets.ContainsKey(2));
    }

    [Fact]
    public void UnbalancedStringDoesNotHideLaterObjects()
    {
        var data = Encoding.ASCII.GetBytes(
            "garbage ( truncated string never closed\n"
            + "1 0 obj\n<< /Type /Catalog >>\nendobj\n"
            + "2 0 obj\n<< /Type /Page >>\nendobj\n");

        var offsets = new DocumentRepairer(data, ReaderLimits.Default).ScannedOffsets();

        Assert.True(offsets.ContainsKey(1));
        Assert.True(offsets.ContainsKey(2));
    }

    [Fact]
    public void UnterminatedStreamDoesNotHideLaterObjects()
    {
        var data = Encoding.ASCII.GetBytes(
            "1 0 obj\n<< /Length 20 >>\nstream\nsome binary no end marker\n"
            + "2 0 obj\n<< /Type /Page >>\nendobj\n");

        var offsets = new DocumentRepairer(data, ReaderLimits.Default).ScannedOffsets();

        Assert.True(offsets.ContainsKey(2));
    }

    [Fact]
    public void GarbageObjectHeaderInCorruptTailDoesNotClobberRealObject()
    {
        var data = Encoding.ASCII.GetBytes(
            "3 0 obj\n<< /Type /Page >>\nendobj\n"
            + "5 0 obj\n<< /Type /Catalog >>\nendobj\n"
            + "6 0 obj (unterminated note mentioning 3 0 obj at the end");

        var offsets = new DocumentRepairer(data, ReaderLimits.Default).ScannedOffsets();

        Assert.Equal(0, offsets[3]);
    }


    private static byte[] TruncateAfter(byte[] bytes, string marker)
    {
        var text = Encoding.Latin1.GetString(bytes);
        var at = text.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(at >= 0);
        return Encoding.Latin1.GetBytes(text.Substring(0, at + marker.Length) + "\n");
    }
}
