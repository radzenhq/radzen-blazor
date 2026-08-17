using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Write;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf;

internal static class DocumentLoader
{
    public static PortableDocument Load(Stream stream, ReaderLimits limits, LoadOptions? options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(limits);
        limits = limits.Snapshot();

        var bytes = PdfSourceBytes.ReadFully(stream, limits.MaxFileBytes);
        var reader = DocumentReader.Parse(bytes, options?.Password, options?.AesProvider, limits);
        return BuildSafely(reader, bytes, limits);
    }

    public static async ValueTask<PortableDocument> LoadAsync(Stream stream, ReaderLimits limits, LoadOptions? options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(limits);
        limits = limits.Snapshot();

        var bytes = PdfSourceBytes.ReadFully(stream, limits.MaxFileBytes);
        var reader = await DocumentReader
            .ParseAsync(bytes, options?.Password, options?.AesProvider, limits).ConfigureAwait(false);
        return BuildSafely(reader, bytes, limits);
    }

    private static PortableDocument BuildSafely(DocumentReader reader, byte[] bytes, ReaderLimits limits)
    {
        try
        {
            return Build(reader, bytes, limits);
        }
        catch (Exception exception) when (IsRecoverableBuildFailure(exception))
        {
            throw new DocumentParseException("The PDF document could not be materialized.", exception);
        }
    }

    private static bool IsRecoverableBuildFailure(Exception exception)
        => exception is not DocumentParseException
            && (exception is KeyNotFoundException
                or ArgumentException
                or OverflowException
                or FormatException
                or EndOfStreamException
                or InvalidDataException
                or InvalidCastException
                or IndexOutOfRangeException);

    private static PortableDocument Build(DocumentReader reader, byte[] bytes, ReaderLimits limits)
    {
        var state = new LoadedState(reader, bytes);
        var document = PortableDocument.CreateLoaded(state);
        document.ImageDecoders = ImageDecoders.BuiltIn.WithLimits(limits);
        state.SourceInfo = reader.GetDictionary(reader.Trailer, "Info");

        var catalog = reader.GetDictionary(reader.Trailer, "Root");
        if (catalog is null || !catalog.TryGetValue("Pages", out var candidatePages)
            || candidatePages is null || reader.Resolve(candidatePages) is not DictionaryObject)
        {
            catalog = reader.ReconstructCatalogWithPages()
                ?? throw new DocumentParseException("A catalog with a valid /Pages dictionary could not be reconstructed.", -1);
        }

        state.SourceCatalog = catalog;
        if (catalog.TryGetValue("Pages", out var pagesRef) && pagesRef is not null)
        {
            foreach (var leaf in PageTreeWalker.Enumerate(reader, pagesRef, limits, rejectInvalidKids: false))
            {
                CollectPage(reader, leaf, document, state);
            }
        }

        var namedDestinations = new Lazy<Dictionary<string, DocumentObject>>(
            () => ReadNamedDestinations(reader, catalog, limits));
        foreach (var page in document.Pages)
        {
            AnnotationReader.Read(
                page, reader, state.SourcePages[page], document.Pages, state.SourcePages, namedDestinations);
        }

        if (reader.GetDictionary(catalog, "AcroForm") is { } form)
        {
            state.SourceAcroForm = form;
            _ = new AcroForm(reader, form, document);
        }

        return document;
    }

    // The inheritable page attributes (ISO 32000-1 Table 30).
    private readonly struct InheritedAttributes
    {
        public ArrayObject? Box { get; init; }

        public DictionaryObject? Resources { get; init; }

        public ArrayObject? CropBox { get; init; }

        public int? Rotate { get; init; }
    }

    private static void CollectPage(DocumentReader reader, PageTreeWalker.Leaf leaf, PortableDocument document, LoadedState state)
    {
        var inherited = new InheritedAttributes();
        foreach (var pathNode in leaf.Path)
        {
            var dictionary = pathNode.Dictionary;
            inherited = new InheritedAttributes
            {
                Box = reader.GetArray(dictionary, "MediaBox") ?? inherited.Box,
                Resources = reader.GetDictionary(dictionary, "Resources") ?? inherited.Resources,
                CropBox = reader.GetArray(dictionary, "CropBox") ?? inherited.CropBox,
                Rotate = reader.GetInt(dictionary, "Rotate") ?? inherited.Rotate,
            };
        }

        var node = leaf.Node.Dictionary;
        var box = inherited.Box;
        var resources = inherited.Resources;
        var cropBox = inherited.CropBox;
        var rotate = inherited.Rotate;

        var mediaCorners = RectReader.ResolveCorners(reader, box, RectPolicy.Rejecting);
        var cropCorners = RectReader.ResolveCorners(reader, cropBox, RectPolicy.Rejecting);
        var mediaBox = NormalizedBox(mediaCorners);
        var cropRect = NormalizedBox(cropCorners);
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

        if (mediaCorners is not null)
        {
            state.SourceBoxes[page] = BoxArray(mediaCorners);
        }

        if (cropCorners is not null)
        {
            state.SourceCropBoxes[page] = BoxArray(cropCorners);
        }

        if (page.Rotate != 0)
        {
            state.SourceRotations[page] = page.Rotate;
        }
    }

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
        => box is { } rect && rect.IsFiniteAndPositive
            ? (Unit.FromPoint(rect.Width), Unit.FromPoint(rect.Height))
            : (PageSizes.A4.Width, PageSizes.A4.Height);

    private static PdfRect? NormalizedBox(double[]? corners)
        => corners is not null ? PdfRect.Normalize(corners) : null;

    private static ArrayObject BoxArray(double[] corners) =>
    [
        new NumberObject(corners[0]),
        new NumberObject(corners[1]),
        new NumberObject(corners[2]),
        new NumberObject(corners[3]),
    ];

    private static PdfRect? ReadBox(DocumentReader reader, DictionaryObject page, string key)
        => NormalizedBox(RectReader.ResolveCorners(reader, reader.GetArray(page, key), RectPolicy.Rejecting));

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

    internal static DictionaryObject? ReadInfo(DocumentReader reader, DocumentInfo target)
    {
        if (reader.GetDictionary(reader.Trailer, "Info") is not { } info)
        {
            return null;
        }

        foreach (var field in DocumentInfoFields.All)
        {
            field.Read(reader, info, target);
        }

        return info;
    }

    internal static DateTimeOffset? Date(DocumentReader reader, DictionaryObject dictionary, string key)
        => reader.GetString(dictionary, key) is { } text
            ? ParseDate(FormField.DecodeTextString(text))
            : null;

    // ISO 32000-1 7.9.4 date string: D:YYYYMMDDHHmmSSOHH'mm'; every field after the year is optional, the offset O is +, -, or Z.
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

    internal static void ReadAttachments(DocumentReader reader, DictionaryObject catalog, PortableDocument document, ReaderLimits limits)
    {
        var seen = new HashSet<DictionaryObject>();

        if (reader.GetDictionary(catalog, "Names") is { } names
            && reader.GetDictionary(names, "EmbeddedFiles") is { } tree)
        {
            WalkNameTree(reader, tree, "Names", [], limits, 0, (_, value) =>
            {
                if (reader.AsDictionary(value) is { } filespec)
                {
                    AddAttachment(reader, filespec, document, seen);
                }
            });
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

    // ISO 32000-1 7.9.6 (name trees) and 7.9.7 (number trees).
    private static void WalkNameTree(
        DocumentReader reader,
        DictionaryObject node,
        string leafKey,
        HashSet<DictionaryObject> visited,
        ReaderLimits limits,
        int depth,
        Action<DocumentObject, DocumentObject> leaf)
    {
        if (depth > limits.MaxPageTreeDepth || !visited.Add(node))
        {
            throw new DocumentParseException("Cyclic or excessively deep name tree.", -1);
        }

        if (reader.GetArray(node, "Kids") is { } kids)
        {
            foreach (var kid in kids)
            {
                if (reader.AsDictionary(kid) is not { } child)
                {
                    throw new DocumentParseException("A name-tree child is not a dictionary.", -1);
                }

                WalkNameTree(reader, child, leafKey, visited, limits, depth + 1, leaf);
            }

            return;
        }

        if (reader.GetArray(node, leafKey) is not { } pairs)
        {
            return;
        }

        if (pairs.Count % 2 != 0)
        {
            throw new DocumentParseException("A name tree has an odd-length leaf array.", -1);
        }

        for (var i = 0; i < pairs.Count; i += 2)
        {
            leaf(pairs[i], pairs[i + 1]);
        }
    }

    private static void AddAttachment(DocumentReader reader, DictionaryObject filespec, PortableDocument document, HashSet<DictionaryObject> seen)
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

    internal static string? Text(DocumentReader reader, DictionaryObject dictionary, string key)
        => reader.GetString(dictionary, key) is { } text
            ? FormField.DecodeTextString(text)
            : null;

    internal static void ReadOutline(
        DocumentReader reader,
        DictionaryObject catalog,
        PortableDocument document,
        LoadedState state,
        ReaderLimits limits)
    {
        var namedDestinations = new Lazy<Dictionary<string, DocumentObject>>(
            () => ReadNamedDestinations(reader, catalog, limits));
        if (reader.GetDictionary(catalog, "Outlines") is { } root
            && root.TryGetValue("First", out var first))
        {
            var pageIndexes = new Dictionary<DictionaryObject, int>();
            for (var i = 0; i < document.Pages.Count; i++)
            {
                pageIndexes[state.SourcePages[document.Pages[i]]] = i;
            }

            var requiresRewrite = false;
            ReadOutlineLevel(
                reader, first!, document.Outline, pageIndexes,
                namedDestinations.Value, [], limits, 0, ref requiresRewrite);
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

        return ColorComponent.ToChannel(number);
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

        var result = DestinationReader.Read(
            reader,
            destination,
            page => pageIndexes.TryGetValue(page, out var index) ? index : null,
            destinations,
            retainAllFitTypes: false);
        namedDestination = result.WasNamed && result.Target is not null;
        return result.Target;
    }

    internal static Dictionary<string, DocumentObject> ReadNamedDestinations(DocumentReader reader, DictionaryObject catalog, ReaderLimits limits)
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
            WalkNameTree(reader, tree, "Names", [], limits, 0, (key, value) =>
            {
                var name = reader.AsString(key)
                    ?? throw new DocumentParseException("A destination name-tree key is not a string.", -1);
                result[name] = value;
            });
        }

        return result;
    }

    internal static void ReadPageLabels(DocumentReader reader, DictionaryObject catalog, PortableDocument document, ReaderLimits limits)
    {
        if (reader.GetDictionary(catalog, "PageLabels") is { } tree)
        {
            WalkNameTree(reader, tree, "Nums", [], limits, 0, (key, value) =>
            {
                var startPage = reader.AsInt(key)
                    ?? throw new DocumentParseException("A page-label range key is not an integer.", -1);
                var dictionary = reader.AsDictionary(value)
                    ?? throw new DocumentParseException("A page-label range value is not a dictionary.", -1);
                document.PageLabels.Add(new PageLabel(startPage)
                {
                    Style = ReadPageLabelStyle(reader.GetName(dictionary, "S")),
                    Prefix = Text(reader, dictionary, "P"),
                    Start = reader.GetInt(dictionary, "St") ?? 1,
                });
            });
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

    internal static void ReadXmp(DocumentReader reader, DictionaryObject catalog, DocumentXmpMetadata target)
    {
        if (reader.GetStream(catalog, "Metadata") is { } metadata)
        {
            target.LoadPacket(reader.DecodeStream(metadata));
        }
    }

    internal static ViewerPreferences? ReadViewerPreferences(DocumentReader? reader, DictionaryObject? catalog)
    {
        if (reader is null || catalog is null)
        {
            return null;
        }

        var result = new ViewerPreferences
        {
            PageLayout = Enum.TryParse<PdfPageLayout>(reader.GetName(catalog, "PageLayout"), out var layout) ? layout : null,
            PageMode = Enum.TryParse<PdfPageMode>(reader.GetName(catalog, "PageMode"), out var mode) ? mode : null,
        };

        if (reader.GetDictionary(catalog, "ViewerPreferences") is { } preferences)
        {
            result.HideToolbar = reader.GetBool(preferences, "HideToolbar") == true;
            result.HideMenubar = reader.GetBool(preferences, "HideMenubar") == true;
            result.FitWindow = reader.GetBool(preferences, "FitWindow") == true;
            result.CenterWindow = reader.GetBool(preferences, "CenterWindow") == true;
            result.DisplayDocTitle = reader.GetBool(preferences, "DisplayDocTitle") == true;
            result.Direction = reader.GetName(preferences, "Direction") switch
            {
                "L2R" => PdfReadingDirection.LeftToRight,
                "R2L" => PdfReadingDirection.RightToLeft,
                _ => null,
            };
        }

        return result.PageLayout is null
            && result.PageMode is null
            && !result.HideToolbar
            && !result.HideMenubar
            && !result.FitWindow
            && !result.CenterWindow
            && !result.DisplayDocTitle
            && result.Direction is null
                ? null
                : result;
    }
}
