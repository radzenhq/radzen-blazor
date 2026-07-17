using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

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
        limits = limits.Snapshot();

        var bytes = ReadAll(stream, limits);
        var reader = DocumentReader.Parse(bytes, options?.Password, limits);

        var state = new LoadedState(reader, bytes);
        var document = Document.CreateLoaded(state);
        state.SourceInfo = ReadInfo(reader, document.Info);

        var catalog = reader.GetDictionary(reader.Trailer, "Root");
        if (catalog is null || !catalog.TryGetValue("Pages", out var candidatePages)
            || candidatePages is null || reader.Resolve(candidatePages) is not DictionaryObject)
        {
            catalog = reader.ReconstructCatalogWithPages()
                ?? throw new DocumentParseException("A catalog with a valid /Pages dictionary could not be reconstructed.", -1);
        }

        state.SourceCatalog = catalog;
        if (catalog.TryGetValue("Pages", out var pagesRef)
            && reader.Resolve(pagesRef!) is DictionaryObject pagesNode)
        {
            var visited = new HashSet<int>();
            if (pagesRef is ReferenceObject pagesReference)
            {
                visited.Add(pagesReference.ObjectNumber);
            }

            CollectPages(reader, pagesNode, new InheritedAttributes(), document, state, limits, visited, 0);
        }

        foreach (var page in document.Pages)
        {
            AnnotationReader.Read(page, reader, state.SourcePages[page], document.Pages, state.SourcePages);
        }

        if (reader.GetDictionary(catalog, "AcroForm") is { } form)
        {
            state.SourceAcroForm = form;
            document.AcroForm = new AcroForm(reader, form, document);
        }

        ReadAttachments(reader, catalog, document, limits);
        ReadOutline(reader, catalog, document, state, limits);
        ReadPageLabels(reader, catalog, document, state, limits);
        ReadXmp(reader, catalog, document);

        // Reading built the metadata model through its tracked setters; freeze it so only the
        // caller's later edits read as changes.
        document.AcceptMetadataChanges();

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
            if (length > limits.MaxFileBytes || length > int.MaxValue)
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

        // A MemoryStream grows by doubling, so the old and new buffers are both live across
        // each grow and ToArray() adds a third full-size copy - roughly 3x the file, all on
        // the LOH for a large one. Chunks stay just under the 85 KB LOH threshold and are
        // copied out once into an exactly-sized array.
        const int ChunkSize = 81920;
        var chunks = new List<(byte[] Buffer, int Length)>();
        var chunk = new byte[ChunkSize];
        var filled = 0;
        var total = 0L;
        int count;
        while ((count = stream.Read(chunk, filled, chunk.Length - filled)) > 0)
        {
            filled += count;
            total += count;
            if (total > limits.MaxFileBytes || total > int.MaxValue)
            {
                throw new DocumentParseException("Maximum file size exceeded.", -1);
            }

            if (filled == chunk.Length)
            {
                chunks.Add((chunk, filled));
                chunk = new byte[ChunkSize];
                filled = 0;
            }
        }

        if (filled > 0)
        {
            chunks.Add((chunk, filled));
        }

        var result = new byte[total];
        var written = 0;
        foreach (var (buffer, length) in chunks)
        {
            Array.Copy(buffer, 0, result, written, length);
            written += length;
        }

        return result;
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
    private static void CollectPages(DocumentReader reader, DictionaryObject node, InheritedAttributes inherited, Document document, LoadedState state, ReaderLimits limits, HashSet<int> visited, int depth)
    {
        if (depth > limits.MaxPageTreeDepth)
        {
            throw new DocumentParseException("Maximum page tree depth exceeded.", -1);
        }

        var box = reader.GetArray(node, "MediaBox") ?? inherited.Box;

        var resources = reader.GetDictionary(node, "Resources") ?? inherited.Resources;

        var cropBox = reader.GetArray(node, "CropBox") ?? inherited.CropBox;

        var rotate = reader.GetInt(node, "Rotate") ?? inherited.Rotate;

        var childInherited = new InheritedAttributes { Box = box, Resources = resources, CropBox = cropBox, Rotate = rotate };

        if (reader.TryGet<ArrayObject>(node, "Kids", out var kids))
        {
            foreach (var kid in kids)
            {
                if (kid is ReferenceObject reference && !visited.Add(reference.ObjectNumber))
                {
                    throw new DocumentParseException("Cyclic page tree reference.", -1);
                }

                if (reader.AsDictionary(kid) is { } child)
                {
                    CollectPages(reader, child, childInherited, document, state, limits, visited, depth + 1);
                }
            }

            return;
        }

        var mediaCorners = ResolveCorners(reader, box);
        var cropCorners = ResolveCorners(reader, cropBox);
        var mediaBox = ToRect(mediaCorners);
        var cropRect = ToRect(cropCorners);
        var (width, height) = Dimensions(mediaBox);
        var page = new Page(width, height);
        if (mediaBox is { } preservedMediaBox)
        {
            page.SetPreservedMediaBox(preservedMediaBox);
        }

        if (cropRect is { } preservedCropBox)
        {
            page.SetPreservedCropBox(preservedCropBox);
        }

        page.BleedBox = ReadBox(reader, node, "BleedBox");
        page.TrimBox = ReadBox(reader, node, "TrimBox");
        page.ArtBox = ReadBox(reader, node, "ArtBox");
        if (rotate is { } loadedRotation)
        {
            page.SetLoadedRotate(loadedRotation);
        }

        var content = ReadContent(reader, node);
        if (content is not null)
        {
            page.SetLoadedContent(content);
        }

        page.SetTextFonts(BuildTextFonts(reader, resources));
        if (resources is not null)
        {
            page.SetReservedResourceNames(PageResourceBuilder.ResourceNames(reader, resources));
        }

        document.Pages.Insert(document.Pages.Count, page);
        state.SourcePages[page] = node;
        state.LoadedPages.Add(page);
        state.LoadedPageSettings[page] = (page.BleedBox, page.TrimBox, page.ArtBox, page.Rotate);
        if (resources is not null)
        {
            state.SourceResources[page] = resources;
        }

        // The resolved boxes rather than the source arrays: an element held as an indirect
        // reference re-emits as 0 unless it is resolved here.
        if (mediaCorners is not null)
        {
            state.SourceBoxes[page] = BoxArray(mediaCorners);
        }

        if (cropCorners is not null)
        {
            state.SourceCropBoxes[page] = BoxArray(cropCorners);
        }

        // Carries the rotation onto a page appended from this document, which copies the
        // page rather than sharing it. Only a rotation the viewer would apply is worth it.
        if (page.Rotate != 0)
        {
            state.SourceRotations[page] = page.Rotate;
        }
    }

    // Building a ReverseFont decodes and parses the font's whole /ToUnicode CMap, and the
    // same font dictionary is typically shared by every page (commonly through /Resources
    // inherited from the /Pages node), so each one is reversed once per reader. ReverseFont
    // is immutable, and the cache dies with the reader it is keyed by.
    private static readonly ConditionalWeakTable<DocumentReader, Dictionary<DictionaryObject, Fonts.ReverseFont>> ReverseFonts = [];

    public static Dictionary<string, Fonts.ReverseFont> BuildTextFonts(DocumentReader reader, DictionaryObject? resources)
    {
        var fonts = new Dictionary<string, Fonts.ReverseFont>(StringComparer.Ordinal);
        if (resources is null || reader.GetDictionary(resources, "Font") is not { } fontDictionary)
        {
            return fonts;
        }

        var cache = ReverseFonts.GetOrCreateValue(reader);
        foreach (var key in fontDictionary.Keys)
        {
            if (reader.AsDictionary(fontDictionary[key]) is not { } font)
            {
                continue;
            }

            lock (cache)
            {
                if (!cache.TryGetValue(font, out var reversed))
                {
                    reversed = Fonts.ReverseFont.Build(reader, font);
                    cache[font] = reversed;
                }

                fonts[key] = reversed;
            }
        }

        return fonts;
    }

    private static (Unit Width, Unit Height) Dimensions(PdfRect? box)
        => box is { } rect
            ? (Unit.FromPoint(rect.Width), Unit.FromPoint(rect.Height))
            : (PageSizes.A4.Width, PageSizes.A4.Height);

    // A box coordinate may legally be an indirect reference (ISO 32000-1 7.3.10), so each
    // element is resolved; a box with a coordinate that is not a number at all is unusable
    // and reads as absent rather than collapsing the page to a degenerate size.
    private static double[]? ResolveCorners(DocumentReader reader, ArrayObject? box)
    {
        if (box is null || box.Count < 4)
        {
            return null;
        }

        var corners = new double[4];
        for (var i = 0; i < corners.Length; i++)
        {
            if (reader.AsNumber(box[i]) is not { } corner)
            {
                return null;
            }

            corners[i] = corner;
        }

        return corners;
    }

    private static PdfRect? ToRect(double[]? corners)
        => corners is not null ? new PdfRect(corners[0], corners[1], corners[2], corners[3]) : null;

    // The re-emitted box keeps the source coordinates rather than the corners a PdfRect
    // recomputes, so a fractional origin cannot shift the far edge by a rounding step.
    private static ArrayObject BoxArray(double[] corners) =>
    [
        new NumberObject(corners[0]),
        new NumberObject(corners[1]),
        new NumberObject(corners[2]),
        new NumberObject(corners[3]),
    ];

    private static PdfRect? ReadBox(DocumentReader reader, DictionaryObject page, string key)
        => ToRect(ResolveCorners(reader, reader.GetArray(page, key)));

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
            long total = 0;
            for (var i = 0; i < array.Count; i++)
            {
                if (reader.AsStream(array[i]) is { } part)
                {
                    var decoded = reader.DecodeStream(part);
                    var separator = i > 0 ? 1 : 0;
                    try
                    {
                        total = checked(total + separator + decoded.LongLength);
                    }
                    catch (OverflowException)
                    {
                        throw new DocumentParseException("Aggregate decoded content exceeds the maximum allowed size.", -1);
                    }

                    if (total > reader.Limits.MaxAggregateDecodedBytes)
                    {
                        throw new DocumentParseException("Aggregate decoded content exceeds the maximum allowed size.", -1);
                    }

                    if (separator != 0)
                    {
                        joined.WriteByte((byte)'\n');
                    }

                    joined.Write(decoded, 0, decoded.Length);
                }
            }

            return joined.ToArray();
        }

        return null;
    }

    private static DictionaryObject? ReadInfo(DocumentReader reader, DocumentInfo target)
    {
        if (reader.GetDictionary(reader.Trailer, "Info") is not { } info)
        {
            return null;
        }

        target.Title = Text(reader, info, "Title");
        target.Author = Text(reader, info, "Author");
        target.Subject = Text(reader, info, "Subject");
        target.Keywords = Text(reader, info, "Keywords");
        target.Creator = Text(reader, info, "Creator");
        target.Producer = Text(reader, info, "Producer");
        target.CreationDate = Date(reader, info, "CreationDate");
        target.ModificationDate = Date(reader, info, "ModDate");
        return info;
    }

    private static DateTimeOffset? Date(DocumentReader reader, DictionaryObject dictionary, string key)
        => reader.GetString(dictionary, key) is { } text
            ? ParseDate(FormField.DecodeTextString(text))
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

        if (reader.GetDictionary(catalog, "Names") is { } names
            && reader.GetDictionary(names, "EmbeddedFiles") is { } tree)
        {
            CollectEmbeddedFiles(reader, tree, document, seen, limits, 0);
        }

        if (reader.GetArray(catalog, "AF") is { } af)
        {
            foreach (var entry in af)
            {
                if (reader.AsDictionary(entry) is { } filespec)
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

        if (reader.TryGet<ArrayObject>(node, "Kids", out var kids))
        {
            foreach (var kid in kids)
            {
                if (reader.AsDictionary(kid) is { } child)
                {
                    CollectEmbeddedFiles(reader, child, document, seen, limits, depth + 1);
                }
            }

            return;
        }

        if (reader.GetArray(node, "Names") is not { } pairs)
        {
            return;
        }

        for (var i = 1; i < pairs.Count; i += 2)
        {
            if (reader.AsDictionary(pairs[i]) is { } filespec)
            {
                AddAttachment(reader, filespec, document, seen);
            }
        }
    }

    private static void AddAttachment(DocumentReader reader, DictionaryObject filespec, Document document, HashSet<DictionaryObject> seen)
    {
        if (!seen.Add(filespec) || reader.GetDictionary(filespec, "EF") is not { } ef)
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

        var mime = reader.GetName(stream.Dictionary, "Subtype") ?? "application/octet-stream";

        var attachment = new Attachment(name, reader.DecodeStream(stream), Relationship(reader, filespec), mime)
        {
            Description = Text(reader, filespec, "Desc"),
        };

        if (reader.GetDictionary(stream.Dictionary, "Params") is { } parameters
            && Date(reader, parameters, "ModDate") is { } modified)
        {
            attachment.ModificationDate = modified;
        }

        document.Attachments.Add(attachment);
    }

    private static string? FileName(DocumentReader reader, DictionaryObject filespec)
        => Text(reader, filespec, "UF") ?? Text(reader, filespec, "F");

    private static AttachmentRelationship Relationship(DocumentReader reader, DictionaryObject filespec)
        => reader.GetName(filespec, "AFRelationship") is { } name
            && Enum.TryParse<AttachmentRelationship>(name, out var relationship)
            ? relationship
            : AttachmentRelationship.Unspecified;

    private static string? Text(DocumentReader reader, DictionaryObject dictionary, string key)
        => reader.GetString(dictionary, key) is { } text
            ? FormField.DecodeTextString(text)
            : null;

    private static void ReadOutline(DocumentReader reader, DictionaryObject catalog, Document document, LoadedState state, ReaderLimits limits)
    {
        if (reader.GetDictionary(catalog, "Outlines") is { } root
            && root.TryGetValue("First", out var first))
        {
            var pageIndexes = new Dictionary<DictionaryObject, int>();
            for (var i = 0; i < document.Pages.Count; i++)
            {
                pageIndexes[state.SourcePages[document.Pages[i]]] = i;
            }

            var destinations = ReadNamedDestinations(reader, catalog, limits);
            var requiresRewrite = false;
            ReadOutlineLevel(reader, first!, document.Outline, pageIndexes, destinations, [], limits, 0, ref requiresRewrite);
            state.OutlineRequiresRewrite = requiresRewrite;
        }
    }

    private static void ReadOutlineLevel(
        DocumentReader reader,
        DocumentObject first,
        IList<OutlineItem> target,
        Dictionary<DictionaryObject, int> pageIndexes,
        Dictionary<string, DocumentObject> destinations,
        HashSet<DictionaryObject> visited,
        ReaderLimits limits,
        int depth,
        ref bool requiresRewrite)
    {
        if (depth > limits.MaxPageTreeDepth)
        {
            throw new DocumentParseException("Maximum outline depth exceeded.", -1);
        }

        DocumentObject? current = first;
        while (current is not null)
        {
            if (reader.Resolve(current) is not DictionaryObject node)
            {
                throw new DocumentParseException("An outline item is not a dictionary.", -1);
            }

            if (!visited.Add(node))
            {
                throw new DocumentParseException("Cyclic outline item reference.", -1);
            }

            var title = Text(reader, node, "Title")
                ?? throw new DocumentParseException("An outline item is missing its /Title string.", -1);
            var item = new OutlineItem(title, ReadOutlineTarget(reader, node, pageIndexes, destinations, out var namedDestination));
            requiresRewrite |= namedDestination;
            if (reader.GetArray(node, "C") is { Count: >= 3 } color)
            {
                item.Color = Color.FromRgb(ColorChannel(reader, color[0]), ColorChannel(reader, color[1]), ColorChannel(reader, color[2]));
            }

            var flags = reader.GetInt(node, "F") ?? 0;
            item.Italic = (flags & 1) != 0;
            item.Bold = (flags & 2) != 0;
            item.Collapsed = (reader.GetInt(node, "Count") ?? 0) < 0;
            if (node.TryGetValue("First", out var child))
            {
                ReadOutlineLevel(reader, child!, item.Children, pageIndexes, destinations, visited, limits, depth + 1, ref requiresRewrite);
            }

            target.Add(item);
            current = node.TryGetValue("Next", out var next) ? next : null;
        }
    }

    private static byte ColorChannel(DocumentReader reader, DocumentObject value)
    {
        var number = reader.AsNumber(value)
            ?? throw new DocumentParseException("An outline /C entry contains a non-number.", -1);
        if (number < 0 || number > 1)
        {
            throw new DocumentParseException("An outline /C component is outside the 0 to 1 range.", -1);
        }

        return (byte)Math.Round(number * 255, MidpointRounding.AwayFromZero);
    }

    private static OutlineTarget? ReadOutlineTarget(
        DocumentReader reader,
        DictionaryObject node,
        Dictionary<DictionaryObject, int> pageIndexes,
        Dictionary<string, DocumentObject> destinations,
        out bool namedDestination)
    {
        namedDestination = false;
        DocumentObject destination;
        if (node.TryGetValue("Dest", out var direct))
        {
            destination = direct!;
        }
        else if (reader.GetDictionary(node, "A") is { } action
            && reader.GetName(action, "S") == "GoTo"
            && action.TryGetValue("D", out var actionDestination))
        {
            destination = actionDestination!;
        }
        else
        {
            return null;
        }

        var resolved = reader.Resolve(destination);
        if (resolved is StringObject text)
        {
            var target = ResolveNamedOutlineTarget(reader, text.Value, destinations, pageIndexes);
            namedDestination = target is not null;
            return target;
        }

        if (resolved is NameObject name)
        {
            var target = ResolveNamedOutlineTarget(reader, name.Value, destinations, pageIndexes);
            namedDestination = target is not null;
            return target;
        }

        return ExplicitOutlineTarget(reader, resolved, pageIndexes);
    }

    private static OutlineTarget? ResolveNamedOutlineTarget(
        DocumentReader reader,
        string name,
        Dictionary<string, DocumentObject> destinations,
        Dictionary<DictionaryObject, int> pageIndexes)
    {
        if (!destinations.TryGetValue(name, out var destination))
        {
            return null;
        }

        var resolved = reader.Resolve(destination);
        if (resolved is DictionaryObject dictionary && dictionary.TryGetValue("D", out var nested))
        {
            resolved = reader.Resolve(nested!);
        }

        return ExplicitOutlineTarget(reader, resolved, pageIndexes);
    }

    private static OutlineTarget? ExplicitOutlineTarget(
        DocumentReader reader,
        DocumentObject destination,
        Dictionary<DictionaryObject, int> pageIndexes)
    {
        if (destination is not ArrayObject array || array.Count < 2
            || reader.Resolve(array[0]) is not DictionaryObject page
            || !pageIndexes.TryGetValue(page, out var pageIndex)
            || reader.AsName(array[1]) is not { } fit)
        {
            return null;
        }

        // A null or absent coordinate is legal and common (e.g. /XYZ null null 0 means
        // "retain the current value"); treat it as 0 rather than failing the whole load.
        double Argument(int index)
            => index < array.Count && reader.AsNumber(array[index]) is { } number ? number : 0;

        return fit switch
        {
            "Fit" => OutlineTarget.ToPageFit(pageIndex),
            "FitH" => OutlineTarget.ToPageFitHorizontal(pageIndex, Argument(2)),
            "FitR" => OutlineTarget.ToPageRectangle(pageIndex, Argument(2), Argument(3), Argument(4), Argument(5)),
            "XYZ" => OutlineTarget.ToPageXYZ(pageIndex, Argument(2), Argument(3), Argument(4)),
            _ => null,
        };
    }

    private static Dictionary<string, DocumentObject> ReadNamedDestinations(DocumentReader reader, DictionaryObject catalog, ReaderLimits limits)
    {
        var result = new Dictionary<string, DocumentObject>(StringComparer.Ordinal);
        if (reader.GetDictionary(catalog, "Dests") is { } legacy)
        {
            foreach (var key in legacy.Keys)
            {
                result[key] = legacy[key];
            }
        }

        if (reader.GetDictionary(catalog, "Names") is { } names
            && reader.GetDictionary(names, "Dests") is { } tree)
        {
            ReadNameTree(reader, tree, result, [], limits, 0);
        }

        return result;
    }

    private static void ReadNameTree(
        DocumentReader reader,
        DictionaryObject node,
        Dictionary<string, DocumentObject> target,
        HashSet<DictionaryObject> visited,
        ReaderLimits limits,
        int depth)
    {
        if (depth > limits.MaxPageTreeDepth || !visited.Add(node))
        {
            throw new DocumentParseException("Cyclic or excessively deep destination name tree.", -1);
        }

        if (reader.GetArray(node, "Kids") is { } kids)
        {
            foreach (var kid in kids)
            {
                if (reader.AsDictionary(kid) is not { } child)
                {
                    throw new DocumentParseException("A destination name-tree child is not a dictionary.", -1);
                }

                ReadNameTree(reader, child, target, visited, limits, depth + 1);
            }
        }

        if (reader.GetArray(node, "Names") is { } pairs)
        {
            if (pairs.Count % 2 != 0)
            {
                throw new DocumentParseException("A destination name tree has an odd /Names array.", -1);
            }

            for (var i = 0; i < pairs.Count; i += 2)
            {
                var key = reader.AsString(pairs[i])
                    ?? throw new DocumentParseException("A destination name-tree key is not a string.", -1);
                target[key] = pairs[i + 1];
            }
        }
    }

    private static void ReadPageLabels(DocumentReader reader, DictionaryObject catalog, Document document, LoadedState state, ReaderLimits limits)
    {
        if (reader.GetDictionary(catalog, "PageLabels") is { } tree)
        {
            ReadPageLabelTree(reader, tree, document.PageLabels, [], limits, 0);
        }
    }

    private static void ReadPageLabelTree(
        DocumentReader reader,
        DictionaryObject node,
        IList<PageLabel> target,
        HashSet<DictionaryObject> visited,
        ReaderLimits limits,
        int depth)
    {
        if (depth > limits.MaxPageTreeDepth || !visited.Add(node))
        {
            throw new DocumentParseException("Cyclic or excessively deep page-label number tree.", -1);
        }

        if (reader.GetArray(node, "Kids") is { } kids)
        {
            foreach (var kid in kids)
            {
                if (reader.AsDictionary(kid) is not { } child)
                {
                    throw new DocumentParseException("A page-label number-tree child is not a dictionary.", -1);
                }

                ReadPageLabelTree(reader, child, target, visited, limits, depth + 1);
            }
        }

        if (reader.GetArray(node, "Nums") is not { } pairs)
        {
            return;
        }

        if (pairs.Count % 2 != 0)
        {
            throw new DocumentParseException("A page-label number tree has an odd /Nums array.", -1);
        }

        for (var i = 0; i < pairs.Count; i += 2)
        {
            var startPage = reader.AsInt(pairs[i])
                ?? throw new DocumentParseException("A page-label range key is not an integer.", -1);
            var dictionary = reader.AsDictionary(pairs[i + 1])
                ?? throw new DocumentParseException("A page-label range value is not a dictionary.", -1);
            var label = new PageLabel(startPage)
            {
                Style = ReadPageLabelStyle(reader.GetName(dictionary, "S")),
                Prefix = Text(reader, dictionary, "P"),
                Start = reader.GetInt(dictionary, "St") ?? 1,
            };
            target.Add(label);
        }
    }

    private static PageLabelStyle? ReadPageLabelStyle(string? style) => style switch
    {
        null => null,
        "D" => PageLabelStyle.Decimal,
        "R" => PageLabelStyle.UppercaseRoman,
        "r" => PageLabelStyle.LowercaseRoman,
        "A" => PageLabelStyle.UppercaseLetters,
        "a" => PageLabelStyle.LowercaseLetters,
        _ => throw new DocumentParseException($"Page-label style /{style} is not supported.", -1),
    };

    private static void ReadXmp(DocumentReader reader, DictionaryObject catalog, Document document)
    {
        if (reader.GetStream(catalog, "Metadata") is { } metadata)
        {
            document.Xmp.LoadPacket(reader.DecodeStream(metadata));
        }
    }
}
