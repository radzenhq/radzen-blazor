#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Repair contract: when the cross-reference machinery is unusable (bad startxref
// offset, a wrong entry offset, or a truncated table), DocumentReader.Parse falls
// back to scanning the file for "N G obj" object headers rather than throwing. A
// missing trailer is reconstructed by locating the /Type /Catalog object.
public class RepairTests
{
    // A valid classic PDF produced by the library under test. Objects: 1 Catalog,
    // 2 Pages, 3 Page. Root => 1 0 R.
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

    // (a) The startxref offset is garbage but the xref table and trailer are
    // intact. The reader cannot use the stated offset and must scan; because the
    // real trailer survives, /Root still resolves.
    [Fact]
    public void CorruptStartxrefOffset_ScansAndResolvesRoot()
    {
        var bytes = Replace(ValidDocument(), "startxref\n", "startxref\n0000000001\n");
        // The digits after our injected offset belong to the original number and
        // parse as trailing bytes, so cut the file right after our %%EOF anchor.
        var reader = DocumentReader.Parse(TruncateAfter(bytes, "%%EOF"));

        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
        Assert.Equal("Page", Assert.IsType<NameObject>(
            Assert.IsType<DictionaryObject>(reader.GetObject(3))["Type"]).Value);
    }

    // (b) A single xref entry points into the middle of the file. The table is
    // structurally valid, so parsing succeeds, but GetObject for that object finds
    // no "N G obj" at the stated offset and must locate it by scanning.
    [Fact]
    public void CorruptSingleEntryOffset_GetObjectFallsBackToScan()
    {
        var bytes = ValidDocument();
        var text = Encoding.Latin1.GetString(bytes);

        // "xref\n0 4\n" then a 20-byte free entry then 20-byte entries for 1,2,3.
        var header = "xref\n0 4\n";
        var xi = text.IndexOf(header, StringComparison.Ordinal);
        Assert.True(xi >= 0);
        var obj3EntryOffset = xi + header.Length + 20 * 3; // free + obj1 + obj2

        // Overwrite the 10-digit offset of object 3 with a mid-file value.
        var patched = text.Substring(0, obj3EntryOffset) + "0000000030"
            + text.Substring(obj3EntryOffset + 10);
        var reader = DocumentReader.Parse(Encoding.Latin1.GetBytes(patched));

        var page = Assert.IsType<DictionaryObject>(reader.GetObject(3));
        Assert.Equal("Page", Assert.IsType<NameObject>(page["Type"]).Value);
    }

    // (c) Everything from "xref" onward is truncated: no table, no trailer, no
    // startxref. The reader scans objects and synthesizes a trailer whose /Root
    // points at the /Type /Catalog object it discovers.
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

    private static byte[] TruncateAfter(byte[] bytes, string marker)
    {
        var text = Encoding.Latin1.GetString(bytes);
        var at = text.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(at >= 0);
        return Encoding.Latin1.GetBytes(text.Substring(0, at + marker.Length) + "\n");
    }
}
