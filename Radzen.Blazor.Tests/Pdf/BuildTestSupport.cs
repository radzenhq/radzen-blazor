#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;

namespace Radzen.Blazor.Pdf.Tests;

// Shared plumbing for the L5 end-to-end DocumentBuilder.Build() tests. The PINNED
// PUBLIC API on DocumentBuilder is:
//
//   Document Build();
//   void SaveToStream(Stream stream);   // == Build().SaveToStream(stream)
//   byte[] ToArray();                   // == Build().ToArray()
//
// Build() runs the layout engine over builder.Sections and emits one physical Page
// per laid-out page; registered sfnt fonts embed as Type0/CID, base-14 families embed
// by name, and Image blocks decode to XObjects. These helpers reload the produced
// bytes (through the merged reader/extractor) and walk the page tree so the tests can
// assert on the real PDF rather than on intermediate objects.
internal static class BuildTestSupport
{
    public const string Latin = "Liberation Sans";
    public const string Cjk = "Noto Sans SC";

    public static void RegisterLatin(DocumentBuilder builder)
        => builder.Fonts.Register(Latin, new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));

    public static void RegisterCjk(DocumentBuilder builder)
        => builder.Fonts.Register(Cjk, new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf")));

    public static Paragraph AddText(Section section, string text, string family, double size = 12)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add(text);
        run.Font.Name = family;
        run.Font.Size = size;
        return section.Blocks.Add(paragraph);
    }

    public static Document Reload(DocumentBuilder builder)
    {
        using var buffer = new MemoryStream(builder.ToArray());
        return Document.LoadFromStream(buffer);
    }

    public static DocumentReader Read(DocumentBuilder builder)
        => DocumentReader.Parse(builder.ToArray());

    public static List<(DictionaryObject Page, DictionaryObject? Resources)> PageLeaves(DocumentReader reader)
    {
        var acc = new List<(DictionaryObject, DictionaryObject?)>();
        if (reader.Trailer.TryGetValue("Root", out var rootObject)
            && reader.Resolve(rootObject!) is DictionaryObject catalog
            && catalog.TryGetValue("Pages", out var pagesObject)
            && reader.Resolve(pagesObject!) is DictionaryObject pages)
        {
            Collect(reader, pages, null, acc);
        }

        return acc;
    }

    private static void Collect(
        DocumentReader reader,
        DictionaryObject node,
        DictionaryObject? inherited,
        List<(DictionaryObject, DictionaryObject?)> acc)
    {
        var resources = node.TryGetValue("Resources", out var ro) && reader.Resolve(ro!) is DictionaryObject own
            ? own
            : inherited;

        if (node.TryGetValue("Kids", out var ko) && reader.Resolve(ko!) is ArrayObject kids)
        {
            foreach (var kid in kids)
            {
                if (reader.Resolve(kid) is DictionaryObject child)
                {
                    Collect(reader, child, resources, acc);
                }
            }

            return;
        }

        acc.Add((node, resources));
    }

    public static List<DictionaryObject> Fonts(DocumentReader reader)
    {
        var seen = new HashSet<DictionaryObject>();
        var result = new List<DictionaryObject>();
        foreach (var (_, resources) in PageLeaves(reader))
        {
            if (resources is null
                || !resources.TryGetValue("Font", out var fontObject)
                || reader.Resolve(fontObject!) is not DictionaryObject fonts)
            {
                continue;
            }

            foreach (var key in fonts.Keys)
            {
                if (reader.Resolve(fonts[key]) is DictionaryObject font && seen.Add(font))
                {
                    result.Add(font);
                }
            }
        }

        return result;
    }

    public static List<DictionaryObject> Type0Fonts(DocumentReader reader)
    {
        var result = new List<DictionaryObject>();
        foreach (var font in Fonts(reader))
        {
            if (font.TryGetValue("Subtype", out var subtype)
                && reader.Resolve(subtype!) is NameObject name
                && name.Value == "Type0")
            {
                result.Add(font);
            }
        }

        return result;
    }

    public static List<StreamObject> ImageXObjects(DocumentReader reader)
    {
        var seen = new HashSet<StreamObject>();
        var result = new List<StreamObject>();
        foreach (var (_, resources) in PageLeaves(reader))
        {
            if (resources is null
                || !resources.TryGetValue("XObject", out var xo)
                || reader.Resolve(xo!) is not DictionaryObject xobjects)
            {
                continue;
            }

            foreach (var key in xobjects.Keys)
            {
                if (reader.Resolve(xobjects[key]) is StreamObject stream
                    && stream.Dictionary.TryGetValue("Subtype", out var subtype)
                    && subtype is NameObject name && name.Value == "Image"
                    && seen.Add(stream))
                {
                    result.Add(stream);
                }
            }
        }

        return result;
    }

    public static string Name(DocumentReader reader, DictionaryObject dict, string key)
        => ((NameObject)reader.Resolve(dict[key])).Value;

    public static int Int(DictionaryObject dict, string key)
        => ((NumberObject)dict[key]).IntValue;

    public static byte[] Content(DocumentReader reader, DictionaryObject page)
    {
        if (!page.TryGetValue("Contents", out var contents))
        {
            return Array.Empty<byte>();
        }

        var resolved = reader.Resolve(contents!);
        if (resolved is StreamObject stream)
        {
            return Decode(stream);
        }

        if (resolved is ArrayObject array)
        {
            using var joined = new MemoryStream();
            foreach (var part in array)
            {
                if (reader.Resolve(part) is StreamObject piece)
                {
                    var bytes = Decode(piece);
                    joined.Write(bytes, 0, bytes.Length);
                    joined.WriteByte((byte)'\n');
                }
            }

            return joined.ToArray();
        }

        return Array.Empty<byte>();
    }

    private static byte[] Decode(StreamObject stream)
    {
        if (!stream.Dictionary.TryGetValue("Filter", out var filter) || filter is null)
        {
            return stream.Data;
        }

        if (filter is NameObject name && name.Value == "FlateDecode")
        {
            return FlateFilter.Decode(stream.Data);
        }

        return stream.Data;
    }

    public static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
