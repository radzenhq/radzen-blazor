#nullable enable

using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

internal static class ContentTestHelpers
{
    public static DocumentReader Reload(Document document) => DocumentReader.Parse(document.ToArray());

    public static DictionaryObject Catalog(DocumentReader reader)
        => Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]!));

    public static DictionaryObject PagesNode(DocumentReader reader)
        => Assert.IsType<DictionaryObject>(reader.Resolve(Catalog(reader)["Pages"]));

    public static DictionaryObject Kid(DocumentReader reader, int index)
    {
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(PagesNode(reader)["Kids"]));
        return Assert.IsType<DictionaryObject>(reader.Resolve(kids[index]));
    }

    public static byte[] PageContent(DocumentReader reader, int index)
    {
        var page = Kid(reader, index);
        var contents = reader.Resolve(page["Contents"]);
        var bytes = new List<byte>();

        void Append(StreamObject stream)
        {
            var raw = stream.Data;
            if (stream.Dictionary.TryGetValue("Filter", out var filter)
                && filter is NameObject name && name.Value == "FlateDecode")
            {
                raw = FlateFilter.Decode(raw);
            }

            bytes.AddRange(raw);
            bytes.Add((byte)'\n');
        }

        if (contents is ArrayObject array)
        {
            for (var i = 0; i < array.Count; i++)
            {
                Append(Assert.IsType<StreamObject>(reader.Resolve(array[i])));
            }
        }
        else
        {
            Append(Assert.IsType<StreamObject>(contents));
        }

        return [.. bytes];
    }

    public static DictionaryObject FontResource(DocumentReader reader, int pageIndex, string resourceName)
    {
        var page = Kid(reader, pageIndex);
        var resources = Assert.IsType<DictionaryObject>(reader.Resolve(page["Resources"]));
        var fonts = Assert.IsType<DictionaryObject>(reader.Resolve(resources["Font"]));
        var key = resourceName.StartsWith('/') ? resourceName[1..] : resourceName;
        return Assert.IsType<DictionaryObject>(reader.Resolve(fonts[key]));
    }
}
