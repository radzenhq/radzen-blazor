#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

internal static class ExtractionSupport
{
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
