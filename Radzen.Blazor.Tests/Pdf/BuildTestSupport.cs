#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Blazor.Pdf.Tests;

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
        => PdfPageContentTestHelper.PageLeaves(reader, assertStructure: false);

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
        => PdfPageContentTestHelper.Content(
            reader, page, assertStreams: false, appendSeparatorAfterEveryStream: false);

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
