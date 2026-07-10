using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using System;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// A physical PDF document: an ordered collection of pages plus document
/// metadata. Serialized through the object model as a classic PDF file.
/// </summary>
public sealed class Document
{
    private readonly Dictionary<Page, DictionaryObject> sourcePages = [];
    private readonly Dictionary<Page, DictionaryObject> sourceResources = [];
    private DocumentReader? source;
    private DictionaryObject? sourceAcroForm;

    /// <summary>Gets the document metadata.</summary>
    public DocumentInfo Info { get; } = new();

    /// <summary>Gets the ordered collection of pages.</summary>
    public PageCollection Pages { get; } = [];

    /// <summary>
    /// Gets the interactive form of a loaded document, or <c>null</c> when the
    /// document has no AcroForm.
    /// </summary>
    public AcroForm? AcroForm { get; private set; }

    /// <summary>
    /// Loads a physical document from a stream. The stream is read in full and
    /// parsed through the internal reader; each page's raw content-stream bytes
    /// are retained verbatim so untouched pages re-serialize unchanged.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="options">Load options such as the decryption password.</param>
    /// <returns>The loaded document.</returns>
    public static Document LoadFromStream(Stream stream, LoadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var reader = DocumentReader.Parse(buffer.ToArray(), options?.Password);

        var document = new Document { source = reader };
        ReadInfo(reader, document.Info);

        var catalog = reader.Trailer.TryGetValue("Root", out var root) && reader.Resolve(root!) is DictionaryObject c
            ? c
            : null;
        if (catalog is not null && catalog.TryGetValue("Pages", out var pagesRef)
            && reader.Resolve(pagesRef!) is DictionaryObject pagesNode)
        {
            CollectPages(reader, pagesNode, null, null, document);
        }

        if (catalog is not null && catalog.TryGetValue("AcroForm", out var formObject)
            && reader.Resolve(formObject!) is DictionaryObject form)
        {
            document.sourceAcroForm = form;
            document.AcroForm = new AcroForm(reader, form);
        }

        return document;
    }

    /// <summary>
    /// Extracts the visible text of every page in reading order, concatenated in
    /// page order with a newline between pages.
    /// </summary>
    /// <returns>The document text, or an empty string when there is no text.</returns>
    public string ExtractText()
    {
        var builder = new System.Text.StringBuilder();
        foreach (var page in Pages)
        {
            var text = page.ExtractText();
            if (text.Length == 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(text);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Appends a deep copy of every page in <paramref name="other"/> to this
    /// document. Each appended page keeps its own content stream (no resource
    /// deduplication) and <paramref name="other"/> is left unchanged.
    /// </summary>
    /// <param name="other">The document whose pages are copied.</param>
    public void Append(Document other)
    {
        ArgumentNullException.ThrowIfNull(other);

        foreach (var source in other.Pages)
        {
            var page = new Page(source.Width, source.Height);
            var content = source.GetContent();
            if (content is not null)
            {
                page.SetContent([.. content]);
            }

            Pages.Insert(Pages.Count, page);
        }
    }

    private static void CollectPages(DocumentReader reader, DictionaryObject node, ArrayObject? inheritedBox, DictionaryObject? inheritedResources, Document document)
    {
        var box = node.TryGetValue("MediaBox", out var mediaBox) && reader.Resolve(mediaBox!) is ArrayObject own
            ? own
            : inheritedBox;

        var resources = node.TryGetValue("Resources", out var resourcesObject) && reader.Resolve(resourcesObject!) is DictionaryObject ownResources
            ? ownResources
            : inheritedResources;

        if (node.TryGetValue("Kids", out var kidsObject) && reader.Resolve(kidsObject!) is ArrayObject kids)
        {
            foreach (var kid in kids)
            {
                if (reader.Resolve(kid) is DictionaryObject child)
                {
                    CollectPages(reader, child, box, resources, document);
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
    }

    private static System.Collections.Generic.Dictionary<string, Fonts.ReverseFont> BuildTextFonts(DocumentReader reader, DictionaryObject? resources)
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

    private static double Number(DocumentObject value) => value is NumberObject number ? number.DoubleValue : 0.0;

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
        => dictionary.TryGetValue(key, out var value) && reader.Resolve(value!) is StringObject text ? text.Value : null;

    /// <summary>
    /// Serializes the document to a byte array.
    /// </summary>
    /// <returns>The complete PDF file bytes.</returns>
    public byte[] ToArray()
    {
        using var stream = new MemoryStream();
        SaveToStream(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Serializes the document to the given stream.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    public void SaveToStream(Stream stream)
    {
        System.ArgumentNullException.ThrowIfNull(stream);

        var writer = new DocumentWriter(stream);

        var catalog = new DictionaryObject();
        var catalogRef = writer.Add(catalog);

        var pagesNode = new DictionaryObject();
        var pagesRef = writer.Add(pagesNode);

        var importer = source is not null ? new GraphImporter(source, writer) : null;
        var pageNodes = new List<(Page Page, DictionaryObject Node, ReferenceObject Reference)>();

        var fontRefs = new Dictionary<GeneratedFont, DocumentObject>();
        var imageRefs = new Dictionary<GeneratedImage, ReferenceObject>();

        var kids = new ArrayObject();
        foreach (var page in Pages)
        {
            var pageNode = new DictionaryObject
            {
                ["Type"] = new NameObject("Page"),
                ["Parent"] = pagesRef,
                ["MediaBox"] = MediaBox(page),
            };

            var pageRef = writer.Add(pageNode);
            if (importer is not null && sourcePages.TryGetValue(page, out var sourceNode))
            {
                importer.Seed(sourceNode, pageRef);
            }

            kids.Add(pageRef);
            pageNodes.Add((page, pageNode, pageRef));
        }

        foreach (var (page, pageNode, _) in pageNodes)
        {
            if (page.Generated is { } generated)
            {
                pageNode["Contents"] = writer.Add(FlateFilter.EncodeStream(generated.Content));
                var resources = BuildGeneratedResources(writer, generated, fontRefs, imageRefs);
                if (resources is not null)
                {
                    pageNode["Resources"] = resources;
                }

                continue;
            }

            var contentBytes = page.BuildContent(out var emitter);
            if (contentBytes is not null)
            {
                pageNode["Contents"] = writer.Add(new StreamObject(contentBytes));
            }

            var emitted = emitter is not null ? BuildResources(emitter) : null;
            var merged = importer is not null && source is not null
                && sourceResources.TryGetValue(page, out var loadedResources)
                ? MergeResources(importer, source, loadedResources, emitted)
                : emitted;
            if (merged is not null)
            {
                pageNode["Resources"] = merged;
            }
        }

        pagesNode["Type"] = new NameObject("Pages");
        pagesNode["Kids"] = kids;
        pagesNode["Count"] = new NumberObject(kids.Count);

        catalog["Type"] = new NameObject("Catalog");
        catalog["Pages"] = pagesRef;

        if (importer is not null)
        {
            PreserveForm(importer, catalog, pageNodes);
        }

        writer.Trailer["Root"] = catalogRef;

        var info = BuildInfo();
        if (info is not null)
        {
            writer.Trailer["Info"] = writer.Add(info);
        }

        writer.Close();
    }

    // Carries the loaded interactive form across a save: widget /Annots stay on
    // their pages and the catalog keeps its /AcroForm, both pointing at the same
    // (possibly mutated) field objects.
    private void PreserveForm(GraphImporter importer, DictionaryObject catalog, List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes)
    {
        foreach (var (page, node, _) in pageNodes)
        {
            if (source is null || !sourcePages.TryGetValue(page, out var sourceNode)
                || !sourceNode.TryGetValue("Annots", out var annotsObject)
                || source.Resolve(annotsObject!) is not ArrayObject annots)
            {
                continue;
            }

            var imported = new ArrayObject();
            foreach (var annot in annots)
            {
                imported.Add(importer.ImportValue(annot));
            }

            node["Annots"] = imported;
        }

        if (sourceAcroForm is not null)
        {
            catalog["AcroForm"] = importer.ImportInstance(sourceAcroForm);
        }
    }

    // Imports the loaded page's effective /Resources into the writer and overlays
    // any newly emitted entries (emitter keys win on collision) so a re-save keeps
    // the source fonts, XObjects and graphics states.
    private static DictionaryObject MergeResources(
        GraphImporter importer,
        DocumentReader reader,
        DictionaryObject loaded,
        DictionaryObject? emitted)
    {
        var result = new DictionaryObject();
        foreach (var key in loaded.Keys)
        {
            result[key] = importer.ImportValue(loaded[key]);
        }

        if (emitted is null)
        {
            return result;
        }

        foreach (var key in emitted.Keys)
        {
            if (result.ContainsKey(key) && emitted[key] is DictionaryObject added)
            {
                var combined = new DictionaryObject();
                if (reader.Resolve(loaded[key]) is DictionaryObject sub)
                {
                    foreach (var name in sub.Keys)
                    {
                        combined[name] = importer.ImportValue(sub[name]);
                    }
                }

                foreach (var name in added.Keys)
                {
                    combined[name] = added[name];
                }

                result[key] = combined;
            }
            else
            {
                result[key] = emitted[key];
            }
        }

        return result;
    }

    private static DictionaryObject? BuildGeneratedResources(
        DocumentWriter writer,
        GeneratedPage page,
        Dictionary<GeneratedFont, DocumentObject> fontRefs,
        Dictionary<GeneratedImage, ReferenceObject> imageRefs)
    {
        DictionaryObject? fonts = null;
        foreach (var font in page.Fonts)
        {
            fonts ??= new DictionaryObject();
            fonts[font.Key] = ResolveFont(writer, font, fontRefs);
        }

        DictionaryObject? xobjects = null;
        foreach (var image in page.Images)
        {
            xobjects ??= new DictionaryObject();
            xobjects[image.Key] = ResolveImage(writer, image, imageRefs);
        }

        if (fonts is null && xobjects is null)
        {
            return null;
        }

        var resources = new DictionaryObject();
        if (fonts is not null)
        {
            resources["Font"] = fonts;
        }

        if (xobjects is not null)
        {
            resources["XObject"] = xobjects;
        }

        return resources;
    }

    private static DocumentObject ResolveFont(DocumentWriter writer, GeneratedFont font, Dictionary<GeneratedFont, DocumentObject> cache)
    {
        if (cache.TryGetValue(font, out var existing))
        {
            return existing;
        }

        DocumentObject reference;
        if (font.Sfnt is { } sfnt)
        {
            reference = Fonts.Type0FontEmbedder.Embed(writer, sfnt, font.GidToUnicode);
        }
        else
        {
            reference = new DictionaryObject
            {
                ["Type"] = new NameObject("Font"),
                ["Subtype"] = new NameObject("Type1"),
                ["BaseFont"] = new NameObject(font.Base14 ?? "Helvetica"),
                ["Encoding"] = new NameObject("WinAnsiEncoding"),
            };
        }

        cache[font] = reference;
        return reference;
    }

    private static ReferenceObject ResolveImage(DocumentWriter writer, GeneratedImage image, Dictionary<GeneratedImage, ReferenceObject> cache)
    {
        if (cache.TryGetValue(image, out var existing))
        {
            return existing;
        }

        var xobject = image.Image;
        if (xobject.SoftMask is { } mask)
        {
            xobject.Image.Dictionary["SMask"] = writer.Add(mask);
        }

        var reference = writer.Add(xobject.Image);
        cache[image] = reference;
        return reference;
    }

    private static DictionaryObject? BuildResources(ContentWriter emitter)
    {
        DictionaryObject? fonts = null;
        foreach (var (baseFont, key) in emitter.Fonts)
        {
            fonts ??= new DictionaryObject();
            fonts[key] = new DictionaryObject
            {
                ["Type"] = new NameObject("Font"),
                ["Subtype"] = new NameObject("Type1"),
                ["BaseFont"] = new NameObject(baseFont),
                ["Encoding"] = new NameObject("WinAnsiEncoding"),
            };
        }

        if (fonts is null)
        {
            return null;
        }

        return new DictionaryObject { ["Font"] = fonts };
    }

    private static ArrayObject MediaBox(Page page) =>
    [
        new NumberObject(0.0),
        new NumberObject(0.0),
        new NumberObject(page.Width.Point),
        new NumberObject(page.Height.Point),
    ];

    private DictionaryObject? BuildInfo()
    {
        DictionaryObject? info = null;

        void Set(string key, string? value)
        {
            if (value is null)
            {
                return;
            }

            info ??= new DictionaryObject();
            info[key] = new StringObject(value);
        }

        Set("Title", Info.Title);
        Set("Author", Info.Author);
        Set("Subject", Info.Subject);
        Set("Keywords", Info.Keywords);
        Set("Creator", Info.Creator);

        return info;
    }
}
