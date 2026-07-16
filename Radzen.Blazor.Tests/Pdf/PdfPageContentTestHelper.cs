#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

internal static class PdfPageContentTestHelper
{
    public static List<(DictionaryObject Page, DictionaryObject? Resources)> PageLeaves(
        DocumentReader reader,
        bool assertStructure)
    {
        var leaves = new List<(DictionaryObject, DictionaryObject?)>();
        if (assertStructure)
        {
            var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]!));
            var pages = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
            Collect(reader, pages, null, leaves);
        }
        else if (reader.Trailer.TryGetValue("Root", out var rootObject)
            && reader.Resolve(rootObject!) is DictionaryObject catalog
            && catalog.TryGetValue("Pages", out var pagesObject)
            && reader.Resolve(pagesObject!) is DictionaryObject pages)
        {
            Collect(reader, pages, null, leaves);
        }

        return leaves;
    }

    public static byte[] Content(
        DocumentReader reader,
        DictionaryObject page,
        bool assertStreams,
        bool appendSeparatorAfterEveryStream)
    {
        if (!page.TryGetValue("Contents", out var contents))
        {
            if (assertStreams)
            {
                Assert.Fail("The page has no content stream.");
            }

            return Array.Empty<byte>();
        }

        var resolved = reader.Resolve(contents!);
        if (resolved is StreamObject stream)
        {
            var bytes = Decode(stream);
            if (!appendSeparatorAfterEveryStream)
            {
                return bytes;
            }

            using var single = new MemoryStream();
            single.Write(bytes, 0, bytes.Length);
            single.WriteByte((byte)'\n');
            return single.ToArray();
        }

        if (resolved is ArrayObject array)
        {
            using var joined = new MemoryStream();
            foreach (var part in array)
            {
                var piece = assertStreams
                    ? Assert.IsType<StreamObject>(reader.Resolve(part))
                    : reader.Resolve(part) as StreamObject;
                if (piece is null)
                {
                    continue;
                }

                var bytes = Decode(piece);
                joined.Write(bytes, 0, bytes.Length);
                joined.WriteByte((byte)'\n');
            }

            return joined.ToArray();
        }

        if (assertStreams)
        {
            Assert.Fail("The page contents are not a stream or stream array.");
        }

        return Array.Empty<byte>();
    }

    private static void Collect(
        DocumentReader reader,
        DictionaryObject node,
        DictionaryObject? inherited,
        List<(DictionaryObject, DictionaryObject?)> leaves)
    {
        var resources = node.TryGetValue("Resources", out var resourceObject)
            && reader.Resolve(resourceObject!) is DictionaryObject own
                ? own
                : inherited;

        if (node.TryGetValue("Kids", out var kidsObject)
            && reader.Resolve(kidsObject!) is ArrayObject kids)
        {
            foreach (var kid in kids)
            {
                if (reader.Resolve(kid) is DictionaryObject child)
                {
                    Collect(reader, child, resources, leaves);
                }
            }

            return;
        }

        leaves.Add((node, resources));
    }

    private static byte[] Decode(StreamObject stream)
    {
        if (stream.Dictionary.TryGetValue("Filter", out var filter)
            && filter is NameObject name
            && name.Value == "FlateDecode")
        {
            return FlateFilter.Decode(stream.Data.ToArray());
        }

        return stream.Data.ToArray();
    }
}
