using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

        var bytes = ReadAll(stream, limits);
        var reader = DocumentReader.Parse(bytes, options?.Password, limits);

        var document = new Document { source = reader, sourceBytes = bytes };
        ReadInfo(reader, document.Info);
        document.loadedInfoSnapshot = Document.InfoSnapshot(document.Info);

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

        if (catalog is not null)
        {
            ReadAttachments(reader, catalog, document, limits);
        }

        return document;
    }

    // Buffers the source into a single byte[] with the file-size cap enforced while reading, so a
    // hostile oversized stream throws before exhausting memory rather than after. A seekable stream
    // is read once into a right-sized array (no second full copy); a non-seekable stream grows a
    // capped MemoryStream. ISO 32000-1 places no hard file-size limit, so MaxFileBytes is the guard.
    private static byte[] ReadAll(Stream stream, ReaderLimits limits)
    {
        if (stream.CanSeek)
        {
            var length = stream.Length - stream.Position;
            if (length > limits.MaxFileBytes)
            {
                throw new DocumentParseException("Maximum file size exceeded.", -1);
            }

            var buffer = new byte[length];
            var offset = 0;
            int read;
            while (offset < buffer.Length && (read = stream.Read(buffer, offset, buffer.Length - offset)) > 0)
            {
                offset += read;
            }

            if (offset != buffer.Length)
            {
                Array.Resize(ref buffer, offset);
            }

            return buffer;
        }

        using var accumulator = new MemoryStream();
        var chunk = new byte[81920];
        int count;
        while ((count = stream.Read(chunk, 0, chunk.Length)) > 0)
        {
            if (accumulator.Length + count > limits.MaxFileBytes)
            {
                throw new DocumentParseException("Maximum file size exceeded.", -1);
            }

            accumulator.Write(chunk, 0, count);
        }

        return accumulator.ToArray();
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

    public static Dictionary<string, Fonts.ReverseFont> BuildTextFonts(DocumentReader reader, DictionaryObject? resources)
    {
        var fonts = new Dictionary<string, Fonts.ReverseFont>(StringComparer.Ordinal);
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
        target.Producer = Text(reader, info, "Producer");
        target.CreationDate = Date(reader, info, "CreationDate");
        target.ModificationDate = Date(reader, info, "ModDate");
    }

    private static DateTimeOffset? Date(DocumentReader reader, DictionaryObject dictionary, string key)
        => dictionary.TryGetValue(key, out var value) && reader.Resolve(value!) is StringObject text
            ? ParseDate(DecodeTextString(text.Value))
            : null;

    // ISO 32000-1 7.9.4 date string: D:YYYYMMDDHHmmSSOHH'mm'. Every field after the
    // year is optional; the offset O is +, -, or Z (or absent). A value that does not
    // parse is dropped rather than throwing so a re-save keeps every other Info entry.
    private static DateTimeOffset? ParseDate(string raw)
    {
        var s = raw.StartsWith("D:", StringComparison.Ordinal) ? raw[2..] : raw;
        if (s.Length < 4 || !int.TryParse(s.AsSpan(0, 4), out var year))
        {
            return null;
        }

        int Part(int start, int length, int fallback)
            => start + length <= s.Length && int.TryParse(s.AsSpan(start, length), out var v) ? v : fallback;

        var month = Part(4, 2, 1);
        var day = Part(6, 2, 1);
        var hour = Part(8, 2, 0);
        var minute = Part(10, 2, 0);
        var second = Part(12, 2, 0);

        var offset = TimeSpan.Zero;
        if (s.Length > 14 && s[14] is '+' or '-')
        {
            var sign = s[14] == '-' ? -1 : 1;
            offset = new TimeSpan(sign * Part(15, 2, 0), sign * Part(18, 2, 0), 0);
        }

        try
        {
            return new DateTimeOffset(year, month, day, hour, minute, second, offset);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    // Loads embedded files declared in the catalog /Names EmbeddedFiles name tree and
    // the /AF associated-files array so they (and /AF) survive a re-save; without this a
    // loaded Factur-X invoice loses its XML on save. Payloads are re-emitted by the
    // AttachmentWriter, which runs because these populate document.Attachments.
    private static void ReadAttachments(DocumentReader reader, DictionaryObject catalog, Document document, ReaderLimits limits)
    {
        var seen = new HashSet<DictionaryObject>();

        if (catalog.TryGetValue("Names", out var namesObject)
            && reader.Resolve(namesObject!) is DictionaryObject names
            && names.TryGetValue("EmbeddedFiles", out var treeObject)
            && reader.Resolve(treeObject!) is DictionaryObject tree)
        {
            CollectEmbeddedFiles(reader, tree, document, seen, limits, 0);
        }

        if (catalog.TryGetValue("AF", out var afObject)
            && reader.Resolve(afObject!) is ArrayObject af)
        {
            foreach (var entry in af)
            {
                if (reader.Resolve(entry) is DictionaryObject filespec)
                {
                    AddAttachment(reader, filespec, document, seen);
                }
            }
        }
    }

    private static void CollectEmbeddedFiles(DocumentReader reader, DictionaryObject node, Document document, HashSet<DictionaryObject> seen, ReaderLimits limits, int depth)
    {
        if (depth > limits.MaxPageTreeDepth)
        {
            throw new DocumentParseException("Maximum name tree depth exceeded.", -1);
        }

        if (node.TryGetValue("Kids", out var kidsObject) && reader.Resolve(kidsObject!) is ArrayObject kids)
        {
            foreach (var kid in kids)
            {
                if (reader.Resolve(kid) is DictionaryObject child)
                {
                    CollectEmbeddedFiles(reader, child, document, seen, limits, depth + 1);
                }
            }

            return;
        }

        if (!node.TryGetValue("Names", out var pairsObject) || reader.Resolve(pairsObject!) is not ArrayObject pairs)
        {
            return;
        }

        for (var i = 1; i < pairs.Count; i += 2)
        {
            if (reader.Resolve(pairs[i]) is DictionaryObject filespec)
            {
                AddAttachment(reader, filespec, document, seen);
            }
        }
    }

    private static void AddAttachment(DocumentReader reader, DictionaryObject filespec, Document document, HashSet<DictionaryObject> seen)
    {
        if (!seen.Add(filespec)
            || !filespec.TryGetValue("EF", out var efObject)
            || reader.Resolve(efObject!) is not DictionaryObject ef)
        {
            return;
        }

        var streamObject = ef.TryGetValue("F", out var f) ? reader.Resolve(f!) : null;
        streamObject ??= ef.TryGetValue("UF", out var uf) ? reader.Resolve(uf!) : null;
        if (streamObject is not StreamObject stream)
        {
            return;
        }

        var name = FileName(reader, filespec);
        if (name is null)
        {
            return;
        }

        var mime = stream.Dictionary.TryGetValue("Subtype", out var subtype) && reader.Resolve(subtype!) is NameObject mimeName
            ? mimeName.Value
            : "application/octet-stream";

        var attachment = new Attachment(name, reader.DecodeStream(stream), Relationship(reader, filespec), mime)
        {
            Description = Text(reader, filespec, "Desc"),
        };

        if (stream.Dictionary.TryGetValue("Params", out var paramsObject)
            && reader.Resolve(paramsObject!) is DictionaryObject parameters
            && Date(reader, parameters, "ModDate") is { } modified)
        {
            attachment.ModificationDate = modified;
        }

        document.Attachments.Add(attachment);
    }

    private static string? FileName(DocumentReader reader, DictionaryObject filespec)
        => Text(reader, filespec, "UF") ?? Text(reader, filespec, "F");

    private static AttachmentRelationship Relationship(DocumentReader reader, DictionaryObject filespec)
        => filespec.TryGetValue("AFRelationship", out var value) && reader.Resolve(value!) is NameObject name
            && Enum.TryParse<AttachmentRelationship>(name.Value, out var relationship)
            ? relationship
            : AttachmentRelationship.Unspecified;

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

        return Encoding.BigEndianUnicode.GetString(bytes);
    }
}
