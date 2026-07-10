#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Blazor.Pdf.Tests;

// Shared plumbing for the C4b text-extraction contract. The PINNED public API is
// string Page.ExtractText() and string Document.ExtractText(), returning the
// page/document text in reading order (top-to-bottom, then left-to-right) with
// char codes reversed to Unicode through the font's /ToUnicode, /Differences or
// the standard WinAnsi/base-14 encoding.
//
// L5 (full text emission with embedded fonts) is not merged, so the Type0/CID and
// /Differences fixtures are hand-built at the object level: a real Type0 graph is
// emitted through Type0FontEmbedder, and a content stream showing the raw codes is
// written into a page whose /Resources reference it. The document is reloaded via
// Document.LoadFromStream and extracted.
internal static class ExtractionSupport
{
    // Builds a one-page PDF: the caller supplies the /Font resource entry (an inline
    // dictionary or an indirect reference produced against the same writer) and the
    // raw content-stream bytes. The document is serialized and reloaded.
    public static Document BuildSinglePage(Func<DocumentWriter, DocumentObject> font, byte[] content, string fontKey = "F1", double width = 612, double height = 792)
    {
        using var buffer = new MemoryStream();
        var writer = new DocumentWriter(buffer);

        var fontObject = font(writer);
        var contentRef = writer.Add(new StreamObject(content));

        var resources = new DictionaryObject
        {
            ["Font"] = new DictionaryObject { [fontKey] = fontObject },
        };

        var pageNode = new DictionaryObject
        {
            ["Type"] = new NameObject("Page"),
            ["MediaBox"] = MediaBox(width, height),
            ["Resources"] = resources,
            ["Contents"] = contentRef,
        };
        var pageRef = writer.Add(pageNode);

        var pagesNode = new DictionaryObject
        {
            ["Type"] = new NameObject("Pages"),
            ["Kids"] = new ArrayObject { pageRef },
            ["Count"] = new NumberObject(1),
        };
        var pagesRef = writer.Add(pagesNode);
        pageNode["Parent"] = pagesRef;

        var catalog = new DictionaryObject
        {
            ["Type"] = new NameObject("Catalog"),
            ["Pages"] = pagesRef,
        };
        writer.Trailer["Root"] = writer.Add(catalog);
        writer.Close();

        using var reload = new MemoryStream(buffer.ToArray());
        return Document.LoadFromStream(reload);
    }

    // "BT /<key> <size> Tf <x> <y> Td <hex codes> Tj ET" - a single positioned run.
    public static byte[] TextRun(string fontKey, double size, double x, double y, byte[] codes)
    {
        var sb = new StringBuilder();
        sb.Append("BT /").Append(fontKey).Append(' ')
          .Append(size.ToString(CultureInfo.InvariantCulture)).Append(" Tf ")
          .Append(x.ToString(CultureInfo.InvariantCulture)).Append(' ')
          .Append(y.ToString(CultureInfo.InvariantCulture)).Append(" Td <")
          .Append(Hex(codes)).Append("> Tj ET\n");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    // Encodes a string as Identity-H CID bytes: two big-endian bytes per glyph id,
    // where under Identity-H the CID equals the sfnt glyph id.
    public static byte[] IdentityCodes(Radzen.Documents.Pdf.Fonts.Sfnt.SfntFont font, string text)
    {
        var bytes = new byte[text.Length * 2];
        for (var i = 0; i < text.Length; i++)
        {
            var gid = font.GetGlyphId(text[i]);
            bytes[i * 2] = (byte)(gid >> 8);
            bytes[(i * 2) + 1] = (byte)(gid & 0xFF);
        }

        return bytes;
    }

    public static string Hex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    private static ArrayObject MediaBox(double width, double height) =>
    [
        new NumberObject(0.0),
        new NumberObject(0.0),
        new NumberObject(width),
        new NumberObject(height),
    ];
}
