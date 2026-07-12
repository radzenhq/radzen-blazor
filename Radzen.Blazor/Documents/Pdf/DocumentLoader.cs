using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf;

// Parses a PDF file into the Document model: reads the info dictionary, walks the
// page tree collecting each leaf page's inherited attributes and raw content, and
// captures the source catalog and AcroForm for a faithful round-trip on save.
internal static class DocumentLoader
{
    public static Document Load(Stream stream, ReaderLimits limits, LoadOptions? options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(limits);

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var reader = DocumentReader.Parse(buffer.ToArray(), options?.Password, limits);

        var document = new Document { source = reader };
        ReadInfo(reader, document.Info);

        var catalog = reader.Trailer.TryGetValue("Root", out var root) && reader.Resolve(root!) is DictionaryObject c
            ? c
            : null;
        document.sourceCatalog = catalog;
        if (catalog is not null && catalog.TryGetValue("Pages", out var pagesRef)
            && reader.Resolve(pagesRef!) is DictionaryObject pagesNode)
        {
            var visited = new HashSet<int>();
            if (pagesRef is ReferenceObject pagesReference)
            {
                visited.Add(pagesReference.ObjectNumber);
            }

            CollectPages(reader, pagesNode, new InheritedAttributes(), document, limits, visited, 0);
        }

        if (catalog is not null && catalog.TryGetValue("AcroForm", out var formObject)
            && reader.Resolve(formObject!) is DictionaryObject form)
        {
            document.sourceAcroForm = form;
            document.AcroForm = new AcroForm(reader, form);
        }

        return document;
    }

    // The inheritable page attributes (ISO 32000-1 Table 30) threaded down the
    // page tree so a leaf without its own entry re-saves the ancestor's value.
    private readonly struct InheritedAttributes
    {
        public ArrayObject? Box { get; init; }

        public DictionaryObject? Resources { get; init; }

        public ArrayObject? CropBox { get; init; }

        public int? Rotate { get; init; }
    }

    // A visited-set of page-node object numbers is the primary guard against a
    // cyclic /Kids graph; MaxPageTreeDepth is a backstop for a deep acyclic tree.
    private static void CollectPages(DocumentReader reader, DictionaryObject node, InheritedAttributes inherited, Document document, ReaderLimits limits, HashSet<int> visited, int depth)
    {
        if (depth > limits.MaxPageTreeDepth)
        {
            throw new DocumentParseException("Maximum page tree depth exceeded.", -1);
        }

        var box = node.TryGetValue("MediaBox", out var mediaBox) && reader.Resolve(mediaBox!) is ArrayObject own
            ? own
            : inherited.Box;

        var resources = node.TryGetValue("Resources", out var resourcesObject) && reader.Resolve(resourcesObject!) is DictionaryObject ownResources
            ? ownResources
            : inherited.Resources;

        var cropBox = node.TryGetValue("CropBox", out var cropObject) && reader.Resolve(cropObject!) is ArrayObject ownCrop
            ? ownCrop
            : inherited.CropBox;

        var rotate = node.TryGetValue("Rotate", out var rotateObject) && reader.Resolve(rotateObject!) is NumberObject ownRotate
            ? ownRotate.IntValue
            : inherited.Rotate;

        var childInherited = new InheritedAttributes { Box = box, Resources = resources, CropBox = cropBox, Rotate = rotate };

        if (node.TryGetValue("Kids", out var kidsObject) && reader.Resolve(kidsObject!) is ArrayObject kids)
        {
            foreach (var kid in kids)
            {
                if (kid is ReferenceObject reference && !visited.Add(reference.ObjectNumber))
                {
                    throw new DocumentParseException("Cyclic page tree reference.", -1);
                }

                if (reader.Resolve(kid) is DictionaryObject child)
                {
                    CollectPages(reader, child, childInherited, document, limits, visited, depth + 1);
                }
            }

            return;
        }

        var (width, height) = Dimensions(box);
        var page = new Page(width, height);
        var content = ReadContent(reader, node);
        if (content is not null)
        {
            page.SetContent(content);
        }

        page.SetTextFonts(BuildTextFonts(reader, resources));
        document.Pages.Insert(document.Pages.Count, page);
        document.sourcePages[page] = node;
        if (resources is not null)
        {
            document.sourceResources[page] = resources;
        }

        if (box is not null && box.Count >= 4)
        {
            document.sourceBoxes[page] = box;
        }

        if (cropBox is not null && cropBox.Count >= 4)
        {
            document.sourceCropBoxes[page] = cropBox;
        }

        // Only a rotation the viewer would actually apply is worth re-emitting.
        if (rotate is { } degrees && degrees % 360 != 0)
        {
            document.sourceRotations[page] = degrees;
        }
    }

    public static System.Collections.Generic.Dictionary<string, Fonts.ReverseFont> BuildTextFonts(DocumentReader reader, DictionaryObject? resources)
    {
        var fonts = new System.Collections.Generic.Dictionary<string, Fonts.ReverseFont>(System.StringComparer.Ordinal);
        if (resources is null
            || !resources.TryGetValue("Font", out var fontObject)
            || reader.Resolve(fontObject!) is not DictionaryObject fontDictionary)
        {
            return fonts;
        }

        foreach (var key in fontDictionary.Keys)
        {
            if (reader.Resolve(fontDictionary[key]) is DictionaryObject font)
            {
                fonts[key] = Fonts.ReverseFont.Build(reader, font);
            }
        }

        return fonts;
    }

    private static (Unit Width, Unit Height) Dimensions(ArrayObject? box)
    {
        if (box is null || box.Count < 4)
        {
            return (PageSizes.A4.Width, PageSizes.A4.Height);
        }

        var llx = Number(box[0]);
        var lly = Number(box[1]);
        var urx = Number(box[2]);
        var ury = Number(box[3]);
        return (Unit.FromPoint(urx - llx), Unit.FromPoint(ury - lly));
    }

    public static double Number(DocumentObject value) => value is NumberObject number ? number.DoubleValue : 0.0;

    private static byte[]? ReadContent(DocumentReader reader, DictionaryObject page)
    {
        if (!page.TryGetValue("Contents", out var contents))
        {
            return null;
        }

        var resolved = reader.Resolve(contents!);
        if (resolved is StreamObject stream)
        {
            return reader.DecodeStream(stream);
        }

        if (resolved is ArrayObject array)
        {
            using var joined = new MemoryStream();
            for (var i = 0; i < array.Count; i++)
            {
                if (reader.Resolve(array[i]) is StreamObject part)
                {
                    if (i > 0)
                    {
                        joined.WriteByte((byte)'\n');
                    }

                    var decoded = reader.DecodeStream(part);
                    joined.Write(decoded, 0, decoded.Length);
                }
            }

            return joined.ToArray();
        }

        return null;
    }

    private static void ReadInfo(DocumentReader reader, DocumentInfo target)
    {
        if (!reader.Trailer.TryGetValue("Info", out var infoObject)
            || reader.Resolve(infoObject!) is not DictionaryObject info)
        {
            return;
        }

        target.Title = Text(reader, info, "Title");
        target.Author = Text(reader, info, "Author");
        target.Subject = Text(reader, info, "Subject");
        target.Keywords = Text(reader, info, "Keywords");
        target.Creator = Text(reader, info, "Creator");
    }

    private static string? Text(DocumentReader reader, DictionaryObject dictionary, string key)
        => dictionary.TryGetValue(key, out var value) && reader.Resolve(value!) is StringObject text
            ? DecodeTextString(text.Value)
            : null;

    // A PDF text string (ISO 32000 7.9.2.2) whose raw bytes start with the FE FF
    // byte order mark is UTF-16BE; otherwise the bytes are PDFDocEncoding/Latin1,
    // which StringObject.Value already exposes verbatim as chars 0-255.
    private static string DecodeTextString(string raw)
    {
        if (raw.Length < 2 || raw[0] != 0xFE || raw[1] != 0xFF)
        {
            return raw;
        }

        var bytes = new byte[raw.Length - 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)raw[i + 2];
        }

        return System.Text.Encoding.BigEndianUnicode.GetString(bytes);
    }
}
