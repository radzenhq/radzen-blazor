using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using System;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf;


/// <summary>
/// A physical PDF document: an ordered collection of pages plus document
/// metadata. Serialized through the object model as a classic PDF file.
/// </summary>
public sealed class Document
{
    private readonly Dictionary<Page, DictionaryObject> sourcePages = [];
    private readonly Dictionary<Page, DictionaryObject> sourceResources = [];
    private readonly Dictionary<Page, ArrayObject> sourceBoxes = [];
    private readonly Dictionary<Page, ArrayObject> sourceCropBoxes = [];
    private readonly Dictionary<Page, int> sourceRotations = [];
    private readonly Dictionary<Page, (DocumentReader Reader, DictionaryObject Resources)> appendedResources = [];
    private readonly Dictionary<Page, (DocumentReader Reader, DictionaryObject Node)> appendedPages = [];
    private readonly Dictionary<DocumentReader, DictionaryObject> appendedAcroForms = [];
    private DocumentReader? source;
    private DictionaryObject? sourceCatalog;
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

    // Logical structure tree of a generated document (Tagged PDF). Set by the
    // generator; null for loaded or hand-assembled documents.
    internal StructureElement? Structure { get; set; }

    // PDF/A conformance level requested at build time; drives XMP metadata,
    // the sRGB output intent, the trailer /ID and full-embedding enforcement.
    internal PdfAConformance Conformance { get; set; }

    // Files embedded on save (EmbeddedFiles name tree + /AF associated files).
    internal List<Attachment> Attachments { get; } = [];

    // Outline (bookmark) tree copied from DocumentBuilder.Outline; emitted on
    // save as the catalog /Outlines tree.
    internal List<OutlineItem> Outline { get; } = [];

    // Named destinations recorded at emit time (Run.Anchor); emitted on save as
    // the catalog /Names /Dests name tree.
    internal Dictionary<string, GeneratedAnchor> Anchors { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Loads a physical document from a stream. The stream is read in full and
    /// parsed through the internal reader; each page's raw content-stream bytes
    /// are retained verbatim so untouched pages re-serialize unchanged.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="options">Load options such as the decryption password.</param>
    /// <returns>The loaded document.</returns>
    public static Document LoadFromStream(Stream stream, LoadOptions? options = null)
        => LoadFromStream(stream, ReaderLimits.Default, options);

    /// <summary>
    /// Loads a physical document from a stream, applying the supplied resource
    /// limits while parsing untrusted input. See <see cref="ReaderLimits"/>.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="limits">The resource limits to enforce while reading.</param>
    /// <param name="options">Load options such as the decryption password.</param>
    /// <returns>The loaded document.</returns>
    public static Document LoadFromStream(Stream stream, ReaderLimits limits, LoadOptions? options = null)
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

            // Carry the source page's resource closure so appended fonts/images still
            // resolve: a built page keeps its GeneratedPage; a loaded page keeps a
            // handle to its reader and effective /Resources, imported lazily on save.
            if (source.Generated is { } generated)
            {
                page.Generated = generated;
            }
            else if (other.source is not null && other.sourceResources.TryGetValue(source, out var loadedResources))
            {
                appendedResources[page] = (other.source, loadedResources);
                page.SetTextFonts(BuildTextFonts(other.source, loadedResources));
            }

            // A loaded appended page keeps a handle to its source node so its
            // /Annots (and any widget-annotation form fields) survive the copy.
            if (other.source is not null && other.sourcePages.TryGetValue(source, out var sourceNode))
            {
                appendedPages[page] = (other.source, sourceNode);
                if (other.sourceAcroForm is not null)
                {
                    appendedAcroForms[other.source] = other.sourceAcroForm;
                }
            }

            if (other.sourceBoxes.TryGetValue(source, out var box))
            {
                sourceBoxes[page] = box;
            }

            Pages.Insert(Pages.Count, page);
        }
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

    /// <summary>
    /// Serializes the document to a byte array.
    /// </summary>
    /// <returns>The complete PDF file bytes.</returns>
    public byte[] ToArray()
    {
        using var stream = new PooledBufferStream(64 * 1024);
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

        if (Conformance != PdfAConformance.None)
        {
            ValidateConformance();
        }

        var writer = new DocumentWriter(stream);

        var catalog = new DictionaryObject();
        var catalogRef = writer.Add(catalog);

        var pagesNode = new DictionaryObject();
        var pagesRef = writer.Add(pagesNode);

        var importer = source is not null ? new GraphImporter(source, writer) : null;
        var appendImporters = new Dictionary<DocumentReader, GraphImporter>();
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

            if (sourceCropBoxes.TryGetValue(page, out var cropBox))
            {
                pageNode["CropBox"] = NumberBox(cropBox);
            }

            if (page.Rotate != 0)
            {
                pageNode["Rotate"] = new NumberObject(page.Rotate);
            }
            else if (sourceRotations.TryGetValue(page, out var rotation))
            {
                pageNode["Rotate"] = new NumberObject(rotation);
            }

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
                var generatedRef = writer.Add(FlateFilter.EncodeStream(generated.Content));
                var overlay = page.BuildOverlay(out var overlayEmitter);
                if (overlay is null)
                {
                    pageNode["Contents"] = generatedRef;
                }
                else
                {
                    pageNode["Contents"] = new ArrayObject { generatedRef, writer.Add(new StreamObject(overlay)) };
                }

                var resources = BuildGeneratedResources(writer, generated, fontRefs, imageRefs);
                if (overlayEmitter is not null)
                {
                    resources = OverlayResources(writer, resources, overlayEmitter);
                }

                if (resources is not null)
                {
                    pageNode["Resources"] = resources;
                }

                if (generated.Links.Count > 0)
                {
                    pageNode["Annots"] = BuildLinkAnnotations(writer, generated.Links);
                }

                continue;
            }

            HashSet<string>? reservedNames = null;
            if (source is not null && sourceResources.TryGetValue(page, out var reservedFrom))
            {
                reservedNames = ResourceNames(source, reservedFrom);
            }
            else if (appendedResources.TryGetValue(page, out var reservedAppend))
            {
                reservedNames = ResourceNames(reservedAppend.Reader, reservedAppend.Resources);
            }

            var contentBytes = page.BuildContent(out var emitter, out var overlayBytes, out var pageOverlayEmitter, reservedNames);
            if (contentBytes is not null)
            {
                var contentRef = writer.Add(new StreamObject(contentBytes));
                pageNode["Contents"] = overlayBytes is null
                    ? contentRef
                    : new ArrayObject { contentRef, writer.Add(new StreamObject(overlayBytes)) };
            }

            var activeEmitter = emitter ?? pageOverlayEmitter;
            var emitted = activeEmitter is not null ? BuildResources(writer, activeEmitter) : null;
            DictionaryObject? merged;
            if (importer is not null && source is not null
                && sourceResources.TryGetValue(page, out var loadedResources))
            {
                merged = MergeResources(importer, source, loadedResources, emitted);
            }
            else if (appendedResources.TryGetValue(page, out var appended))
            {
                if (!appendImporters.TryGetValue(appended.Reader, out var appendImporter))
                {
                    appendImporter = new GraphImporter(appended.Reader, writer);
                    appendImporters[appended.Reader] = appendImporter;
                }

                merged = MergeResources(appendImporter, appended.Reader, appended.Resources, emitted);
            }
            else
            {
                merged = emitted;
            }

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

        if (Structure is { } structure)
        {
            catalog["MarkInfo"] = new DictionaryObject { ["Marked"] = new BooleanObject(true) };
            catalog["StructTreeRoot"] = WriteStructureTree(writer, structure, pageNodes);
        }

        var appendedFields = AppendForms(pageNodes, appendImporters, writer);

        if (importer is not null)
        {
            var removed = PruneRemovedPages(importer);
            PreserveCatalog(importer, catalog, pagesRef);
            PreserveForm(importer, catalog, pageNodes, removed, appendedFields, writer);
        }
        else if (appendedFields.Count > 0)
        {
            var fields = new ArrayObject();
            foreach (var field in appendedFields)
            {
                fields.Add(field);
            }

            catalog["AcroForm"] = writer.Add(new DictionaryObject { ["Fields"] = fields });
        }

        if (Attachments.Count > 0)
        {
            WriteAttachments(writer, catalog);
        }

        if (Anchors.Count > 0)
        {
            WriteDestinations(writer, catalog, pageNodes);
        }

        if (Outline.Count > 0)
        {
            catalog["Outlines"] = WriteOutline(writer, pageNodes);
        }

        if (Conformance != PdfAConformance.None)
        {
            WriteConformance(writer, catalog);
        }

        writer.Trailer["Root"] = catalogRef;

        var info = BuildInfo();
        if (info is not null)
        {
            writer.Trailer["Info"] = writer.Add(info);
        }

        writer.Close();
    }

    private void ValidateConformance()
    {
        if (source is not null && source.IsEncrypted)
        {
            throw new InvalidOperationException("PDF/A forbids encryption; the source document is encrypted.");
        }

        if (Conformance == PdfAConformance.PdfA3A && Structure is null)
        {
            throw new InvalidOperationException(
                "PDF/A-3 Level A requires Tagged PDF logical structure; the document has no structure tree. Build the document with DocumentBuilder or use PdfAConformance.PdfA3B.");
        }

        foreach (var page in Pages)
        {
            if (page.Generated is not { } generated)
            {
                continue;
            }

            foreach (var font in generated.Fonts)
            {
                if (font.Sfnt is null)
                {
                    throw new InvalidOperationException(
                        $"PDF/A forbids the standard-14 font '{font.Base14 ?? "Helvetica"}' referenced by name; register an embeddable font file with DocumentBuilder.Fonts instead.");
                }
            }
        }

        // Overlay text added through Page.Content emits a non-embedded base-14 Type1
        // font, which the generated.Fonts scan above cannot see; reject it with the
        // same error the generator raises. Loaded original text keeps its own font
        // reference (FontResourceName) and is not re-emitted as a base-14 face.
        foreach (var page in Pages)
        {
            foreach (var element in page.Content)
            {
                if (element is not TextContent { FontResourceName: null } text)
                {
                    continue;
                }

                var name = Fonts.Base14Metrics.Resolve(text.Font)?.PostScriptName ?? "Helvetica";
                throw new InvalidOperationException(
                    $"PDF/A forbids the standard-14 font '{name}' referenced by name; register an embeddable font file for '{text.Font.Name}' with DocumentBuilder.Fonts instead.");
            }
        }
    }

    private void WriteAttachments(DocumentWriter writer, DictionaryObject catalog)
    {
        var filespecs = new SortedDictionary<string, ReferenceObject>(StringComparer.Ordinal);
        var af = new ArrayObject();

        foreach (var attachment in Attachments)
        {
            var file = FlateFilter.EncodeStream(attachment.Data);
            file.Dictionary["Type"] = new NameObject("EmbeddedFile");
            file.Dictionary["Subtype"] = new NameObject(attachment.MimeType);
            file.Dictionary["Params"] = new DictionaryObject { ["Size"] = new NumberObject(attachment.Data.Length) };

            var filespec = new DictionaryObject
            {
                ["Type"] = new NameObject("Filespec"),
                ["F"] = new StringObject(attachment.Name),
                ["UF"] = new StringObject(attachment.Name),
                ["AFRelationship"] = new NameObject(attachment.Relationship.ToString()),
                ["EF"] = new DictionaryObject { ["F"] = writer.Add(file) },
            };

            var reference = writer.Add(filespec);
            filespecs[attachment.Name] = reference;
            af.Add(reference);
        }

        var names = new ArrayObject();
        foreach (var (name, reference) in filespecs)
        {
            names.Add(new StringObject(name));
            names.Add(reference);
        }

        catalog["Names"] = new DictionaryObject
        {
            ["EmbeddedFiles"] = writer.Add(new DictionaryObject { ["Names"] = names }),
        };
        catalog["AF"] = af;
    }

    private void WriteConformance(DocumentWriter writer, DictionaryObject catalog)
    {
        var xmp = new XmpMetadata
        {
            Info = Info,
            Producer = "Radzen.Documents.Pdf",
            PdfAPart = 3,
            PdfAConformance = Conformance == PdfAConformance.PdfA3A ? "A" : "B",
        };

        foreach (var attachment in Attachments)
        {
            if (attachment.Name == "factur-x.xml")
            {
                xmp.FacturX = new FacturXMetadata();
                break;
            }
        }

        catalog["Metadata"] = writer.Add(xmp.BuildStream());

        var intent = OutputIntentBuilder.BuildSrgb("sRGB IEC61966-2.1");
        if (intent["DestOutputProfile"] is StreamObject profile)
        {
            intent["DestOutputProfile"] = writer.Add(profile);
        }

        writer.Trailer["ID"] = BuildDocumentId();
        catalog["OutputIntents"] = new ArrayObject { writer.Add(intent) };
    }

    private ArrayObject BuildDocumentId()
    {
        var seed = $"{Info.Title}\n{Info.Author}\n{Pages.Count}\n{DateTime.UtcNow.Ticks}\n{Guid.NewGuid():N}";
        var hash = Radzen.Documents.Crypto.Sha2.Sha256(System.Text.Encoding.UTF8.GetBytes(seed));
        var id = Convert.ToHexString(hash, 0, 16);
        return [new StringObject(id), new StringObject(id)];
    }

    // Serializes the logical structure tree: one indirect StructElem per element
    // (marked-content kids before child elements), /Pg on elements with marks, a
    // /StructParents key per marked page and a flat Nums ParentTree whose per-page
    // arrays are indexed by MCID.
    private static ReferenceObject WriteStructureTree(
        DocumentWriter writer,
        StructureElement structure,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes)
    {
        var root = new DictionaryObject { ["Type"] = new NameObject("StructTreeRoot") };
        var rootRef = writer.Add(root);

        var parents = new Dictionary<int, List<DocumentObject>>();
        root["K"] = WriteStructureElement(writer, structure, rootRef, pageNodes, parents);

        var keys = new List<int>(parents.Keys);
        keys.Sort();

        var nums = new ArrayObject();
        foreach (var pageIndex in keys)
        {
            var entries = new ArrayObject();
            foreach (var entry in parents[pageIndex])
            {
                entries.Add(entry);
            }

            nums.Add(new NumberObject(pageIndex));
            nums.Add(writer.Add(entries));
            pageNodes[pageIndex].Node["StructParents"] = new NumberObject(pageIndex);
        }

        root["ParentTree"] = writer.Add(new DictionaryObject { ["Nums"] = nums });
        root["ParentTreeNextKey"] = new NumberObject(keys.Count == 0 ? 0 : keys[^1] + 1);
        return rootRef;
    }

    private static ReferenceObject WriteStructureElement(
        DocumentWriter writer,
        StructureElement element,
        ReferenceObject parentRef,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes,
        Dictionary<int, List<DocumentObject>> parents)
    {
        var dictionary = new DictionaryObject
        {
            ["Type"] = new NameObject("StructElem"),
            ["S"] = new NameObject(element.Type),
            ["P"] = parentRef,
        };
        var reference = writer.Add(dictionary);

        var kids = new ArrayObject();
        var firstPage = element.Marks.Count > 0 ? element.Marks[0].PageIndex : -1;
        if (firstPage >= 0)
        {
            dictionary["Pg"] = pageNodes[firstPage].Reference;
        }

        foreach (var (pageIndex, mcid) in element.Marks)
        {
            if (pageIndex == firstPage)
            {
                kids.Add(new NumberObject(mcid));
            }
            else
            {
                kids.Add(new DictionaryObject
                {
                    ["Type"] = new NameObject("MCR"),
                    ["Pg"] = pageNodes[pageIndex].Reference,
                    ["MCID"] = new NumberObject(mcid),
                });
            }

            if (!parents.TryGetValue(pageIndex, out var entries))
            {
                entries = [];
                parents[pageIndex] = entries;
            }

            while (entries.Count <= mcid)
            {
                entries.Add(new NullObject());
            }

            entries[mcid] = reference;
        }

        foreach (var child in element.Children)
        {
            kids.Add(WriteStructureElement(writer, child, reference, pageNodes, parents));
        }

        if (kids.Count > 0)
        {
            dictionary["K"] = kids;
        }

        return reference;
    }

    // Catalog keys this writer builds itself; a preserved source catalog must not
    // overwrite them (or drag in a duplicate sub-graph for them).
    private static readonly HashSet<string> ManagedCatalogKeys = new(StringComparer.Ordinal)
    {
        "Type", "Pages", "AcroForm", "Names", "AF", "OutputIntents", "MarkInfo", "StructTreeRoot",
    };

    // Marks every source page node that was loaded but removed from Pages before
    // saving so preserved destinations and annotation /P links that still point at
    // them collapse to null instead of resurrecting the page (and its content).
    private HashSet<DictionaryObject> PruneRemovedPages(GraphImporter importer)
    {
        var removed = new HashSet<DictionaryObject>();
        var kept = new HashSet<Page>(Pages);
        foreach (var pair in sourcePages)
        {
            if (!kept.Contains(pair.Key))
            {
                importer.Prune(pair.Value);
                removed.Add(pair.Value);
            }
        }

        return removed;
    }

    // Carries catalog-level features a loaded document declared - /Outlines,
    // /PageLabels, /OpenAction, /ViewerPreferences, /Metadata, /Lang - that this
    // writer does not build itself. Page-referencing entries repoint onto the new
    // page tree; entries pointing at a removed page collapse to null.
    private void PreserveCatalog(GraphImporter importer, DictionaryObject catalog, ReferenceObject pagesRef)
    {
        if (sourceCatalog is null || source is null)
        {
            return;
        }

        if (sourceCatalog.TryGetValue("Pages", out var sourcePagesObject)
            && source.Resolve(sourcePagesObject!) is DictionaryObject sourcePagesNode)
        {
            importer.Seed(sourcePagesNode, pagesRef);
        }

        foreach (var key in sourceCatalog.Keys)
        {
            if (ManagedCatalogKeys.Contains(key) || catalog.ContainsKey(key))
            {
                continue;
            }

            // A conforming save writes its own XMP; keep the source's otherwise.
            if (string.Equals(key, "Metadata", StringComparison.Ordinal) && Conformance != PdfAConformance.None)
            {
                continue;
            }

            catalog[key] = importer.ImportValue(sourceCatalog[key]);
        }
    }

    // Carries the loaded interactive form across a save: widget /Annots stay on
    // their pages and the catalog keeps its /AcroForm, both pointing at the same
    // (possibly mutated) field objects. Fields whose widget lived on a removed
    // page are dropped so the deleted page never re-enters through a /P link.
    private void PreserveForm(GraphImporter importer, DictionaryObject catalog, List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes, HashSet<DictionaryObject> removed, List<DocumentObject> appendedFields, DocumentWriter writer)
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

        if (sourceAcroForm is not null && source is not null)
        {
            catalog["AcroForm"] = ImportAcroForm(importer, source, sourceAcroForm, removed, appendedFields, writer);
        }
        else if (appendedFields.Count > 0)
        {
            var fields = new ArrayObject();
            foreach (var field in appendedFields)
            {
                fields.Add(field);
            }

            catalog["AcroForm"] = writer.Add(new DictionaryObject { ["Fields"] = fields });
        }
    }

    // Rebuilds the AcroForm field-by-field: source fields whose widget lived on a
    // removed page are dropped, and already-imported fields from appended pages are
    // added, so the merged form lists exactly the fields that still have a widget.
    private static ReferenceObject ImportAcroForm(GraphImporter importer, DocumentReader reader, DictionaryObject acroForm, HashSet<DictionaryObject> removed, List<DocumentObject> appendedFields, DocumentWriter writer)
    {
        var result = new DictionaryObject();
        ArrayObject? fieldsArray = null;
        foreach (var key in acroForm.Keys)
        {
            if (string.Equals(key, "Fields", StringComparison.Ordinal))
            {
                fieldsArray = [];
                if (reader.Resolve(acroForm[key]) is ArrayObject fields)
                {
                    foreach (var field in fields)
                    {
                        if (!FieldOnRemovedPage(reader, field, removed))
                        {
                            fieldsArray.Add(importer.ImportValue(field));
                        }
                    }
                }
            }
            else
            {
                result[key] = importer.ImportValue(acroForm[key]);
            }
        }

        fieldsArray ??= [];
        foreach (var field in appendedFields)
        {
            fieldsArray.Add(field);
        }

        result["Fields"] = fieldsArray;
        return writer.Add(result);
    }

    // Imports the /Annots of every appended loaded page (seeding its new page ref
    // so annotation /P links repoint) and returns the merged widget/field objects
    // to add to the combined AcroForm.
    private List<DocumentObject> AppendForms(List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes, Dictionary<DocumentReader, GraphImporter> appendImporters, DocumentWriter writer)
    {
        var fields = new List<DocumentObject>();
        foreach (var (page, node, reference) in pageNodes)
        {
            if (!appendedPages.TryGetValue(page, out var appended))
            {
                continue;
            }

            var reader = appended.Reader;
            if (!appendImporters.TryGetValue(reader, out var importer))
            {
                importer = new GraphImporter(reader, writer);
                appendImporters[reader] = importer;
            }

            importer.Seed(appended.Node, reference);
            if (!appended.Node.TryGetValue("Annots", out var annotsObject)
                || reader.Resolve(annotsObject!) is not ArrayObject annots)
            {
                continue;
            }

            var imported = new ArrayObject();
            foreach (var annot in annots)
            {
                imported.Add(importer.ImportValue(annot));
            }

            node["Annots"] = imported;

            if (!appendedAcroForms.ContainsKey(reader))
            {
                continue;
            }

            for (var i = 0; i < annots.Count; i++)
            {
                if (reader.Resolve(annots[i]) is DictionaryObject annot && IsMergedFormField(annot))
                {
                    fields.Add(imported[i]);
                }
            }
        }

        return fields;
    }

    // A merged widget/field (ISO 32000-1 12.7.3.1): a /Widget annotation that also
    // carries the field's /FT and is not a child of another field.
    private static bool IsMergedFormField(DictionaryObject annot)
        => annot.TryGetValue("Subtype", out var subtype) && subtype is NameObject name
            && string.Equals(name.Value, "Widget", StringComparison.Ordinal)
            && annot.ContainsKey("FT") && !annot.ContainsKey("Parent");

    // A form field is bound to a page through the /P of its own widget (a merged
    // field/widget) or of any widget in its /Kids.
    private static bool FieldOnRemovedPage(DocumentReader reader, DocumentObject field, HashSet<DictionaryObject> removed)
    {
        if (reader.Resolve(field) is not DictionaryObject dict)
        {
            return false;
        }

        if (dict.TryGetValue("P", out var p) && reader.Resolve(p!) is DictionaryObject page && removed.Contains(page))
        {
            return true;
        }

        if (dict.TryGetValue("Kids", out var kidsObject) && reader.Resolve(kidsObject!) is ArrayObject kids)
        {
            foreach (var kid in kids)
            {
                if (reader.Resolve(kid) is DictionaryObject kidDict
                    && kidDict.TryGetValue("P", out var kidP)
                    && reader.Resolve(kidP!) is DictionaryObject kidPage && removed.Contains(kidPage))
                {
                    return true;
                }
            }
        }

        return false;
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

    // The /Font and /XObject names a loaded page already binds; a full re-emit must not
    // reuse any of them for a freshly registered base-14 face or image XObject.
    private static HashSet<string> ResourceNames(DocumentReader reader, DictionaryObject resources)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        CollectResourceKeys(reader, resources, "Font", names);
        CollectResourceKeys(reader, resources, "XObject", names);
        return names;
    }

    private static void CollectResourceKeys(DocumentReader reader, DictionaryObject resources, string category, HashSet<string> names)
    {
        if (resources.TryGetValue(category, out var value) && value is not null && reader.Resolve(value) is DictionaryObject dict)
        {
            foreach (var key in dict.Keys)
            {
                names.Add(key);
            }
        }
    }

    // Emits the /Names /Dests name tree mapping each anchor to [pageRef /XYZ 0 top 0],
    // merging into a /Names dictionary the attachments block may already have set.
    private void WriteDestinations(
        DocumentWriter writer,
        DictionaryObject catalog,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes)
    {
        var sorted = new SortedDictionary<string, GeneratedAnchor>(Anchors, StringComparer.Ordinal);
        var names = new ArrayObject();
        foreach (var (name, anchor) in sorted)
        {
            names.Add(new StringObject(name));
            names.Add(DestinationArray(pageNodes[anchor.PageIndex].Reference, anchor.Top));
        }

        var dests = writer.Add(new DictionaryObject { ["Names"] = names });
        if (catalog.TryGetValue("Names", out var existing) && existing is DictionaryObject tree)
        {
            tree["Dests"] = dests;
        }
        else
        {
            catalog["Names"] = new DictionaryObject { ["Dests"] = dests };
        }
    }

    private ReferenceObject WriteOutline(
        DocumentWriter writer,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes)
    {
        var root = new DictionaryObject { ["Type"] = new NameObject("Outlines") };
        var rootRef = writer.Add(root);
        root["Count"] = new NumberObject(WriteOutlineLevel(writer, Outline, root, rootRef, pageNodes));
        return rootRef;
    }

    // Writes one sibling level and recurses into children; returns the number of
    // items in the subtree so every /Count is positive (all levels open).
    private int WriteOutlineLevel(
        DocumentWriter writer,
        IReadOnlyList<OutlineItem> items,
        DictionaryObject parent,
        ReferenceObject parentRef,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes)
    {
        var nodes = new List<(DictionaryObject Node, ReferenceObject Reference)>(items.Count);
        foreach (var item in items)
        {
            var node = new DictionaryObject
            {
                ["Title"] = new StringObject(item.Title),
                ["Parent"] = parentRef,
                ["Dest"] = OutlineDestination(item.Target, pageNodes),
            };

            nodes.Add((node, writer.Add(node)));
        }

        var total = items.Count;
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                nodes[i].Node["Prev"] = nodes[i - 1].Reference;
            }

            if (i < items.Count - 1)
            {
                nodes[i].Node["Next"] = nodes[i + 1].Reference;
            }

            if (items[i].Children.Count > 0)
            {
                var descendants = WriteOutlineLevel(writer, [.. items[i].Children], nodes[i].Node, nodes[i].Reference, pageNodes);
                nodes[i].Node["Count"] = new NumberObject(descendants);
                total += descendants;
            }
        }

        parent["First"] = nodes[0].Reference;
        parent["Last"] = nodes[^1].Reference;
        return total;
    }

    // An anchor target navigates through the named-destination tree; a page target
    // gets an explicit destination to the top of the page.
    private DocumentObject OutlineDestination(
        OutlineTarget target,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes)
    {
        if (target.Anchor is { } anchor)
        {
            if (!Anchors.ContainsKey(anchor))
            {
                throw new InvalidOperationException($"Outline target anchor '{anchor}' does not exist; set Run.Anchor on the destination run.");
            }

            return new StringObject(anchor);
        }

        var pageIndex = target.PageIndex ?? 0;
        if (pageIndex >= pageNodes.Count)
        {
            throw new InvalidOperationException($"Outline target page index {pageIndex} is out of range; the document has {pageNodes.Count} pages.");
        }

        var (page, _, reference) = pageNodes[pageIndex];
        return DestinationArray(reference, page.Height.Point);
    }

    private static ArrayObject DestinationArray(ReferenceObject pageRef, double top) =>
    [
        pageRef,
        new NameObject("XYZ"),
        new NumberObject(0.0),
        new NumberObject(top),
        new NumberObject(0.0),
    ];

    private static ArrayObject BuildLinkAnnotations(DocumentWriter writer, IReadOnlyList<GeneratedLink> links)
    {
        var annots = new ArrayObject();
        foreach (var link in links)
        {
            ArrayObject rect =
            [
                new NumberObject(link.X1),
                new NumberObject(link.Y1),
                new NumberObject(link.X2),
                new NumberObject(link.Y2),
            ];

            ArrayObject border = [new NumberObject(0.0), new NumberObject(0.0), new NumberObject(0.0)];

            annots.Add(writer.Add(new DictionaryObject
            {
                ["Type"] = new NameObject("Annot"),
                ["Subtype"] = new NameObject("Link"),
                ["Rect"] = rect,
                ["Border"] = border,
                // PDF/A (ISO 19005-3 6.3.2) requires the Print flag (bit 3 = 4) set
                // and Hidden/NoView clear on every annotation.
                ["F"] = new NumberObject(4),
                ["A"] = link.Destination is { } destination
                    ? new DictionaryObject
                    {
                        ["S"] = new NameObject("GoTo"),
                        ["D"] = new StringObject(destination),
                    }
                    : new DictionaryObject
                    {
                        ["S"] = new NameObject("URI"),
                        ["URI"] = new StringObject(link.Uri!),
                    },
            }));
        }

        return annots;
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
            reference = Fonts.Type0FontEmbedder.Embed(writer, sfnt, font.GidToUnicode, font.CompactGidMap);
        }
        else
        {
            reference = Base14FontDictionary(font.Base14 ?? "Helvetica");
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

    // Adds the fonts and image XObjects referenced by an overlay stream to a built
    // page's resources. Overlay keys use a distinct prefix so generated entries are
    // never clobbered.
    private static DictionaryObject? OverlayResources(DocumentWriter writer, DictionaryObject? resources, ContentWriter emitter)
    {
        var emitted = BuildResources(writer, emitter);
        if (emitted is null)
        {
            return resources;
        }

        resources ??= new DictionaryObject();
        foreach (var key in emitted.Keys)
        {
            if (resources.TryGetValue(key, out var existing) && existing is DictionaryObject target
                && emitted[key] is DictionaryObject added)
            {
                foreach (var name in added.Keys)
                {
                    target[name] = added[name];
                }
            }
            else
            {
                resources[key] = emitted[key];
            }
        }

        return resources;
    }

    // ISO 32000-1 9.6.6.4: Symbol and ZapfDingbats are symbolic and carry a built-in
    // encoding; declaring /Encoding /WinAnsiEncoding would remap their glyphs, so it is
    // omitted for them and kept for the non-symbolic base-14 faces.
    private static DictionaryObject Base14FontDictionary(string baseFont)
    {
        var dictionary = new DictionaryObject
        {
            ["Type"] = new NameObject("Font"),
            ["Subtype"] = new NameObject("Type1"),
            ["BaseFont"] = new NameObject(baseFont),
        };

        if (baseFont is not ("Symbol" or "ZapfDingbats"))
        {
            dictionary["Encoding"] = new NameObject("WinAnsiEncoding");
        }

        return dictionary;
    }

    private static DictionaryObject? BuildResources(DocumentWriter writer, ContentWriter emitter)
    {
        DictionaryObject? fonts = null;
        foreach (var (baseFont, key) in emitter.Fonts)
        {
            fonts ??= new DictionaryObject();
            fonts[key] = Base14FontDictionary(baseFont);
        }

        DictionaryObject? xobjects = null;
        foreach (var (key, image) in emitter.Images)
        {
            xobjects ??= new DictionaryObject();
            if (image.SoftMask is { } mask)
            {
                image.Image.Dictionary["SMask"] = writer.Add(mask);
            }

            xobjects[key] = writer.Add(image.Image);
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

    private ArrayObject MediaBox(Page page)
    {
        // Re-emit a loaded page's original box so a non-zero origin round-trips;
        // content coordinates are preserved verbatim and would otherwise shift.
        if (sourceBoxes.TryGetValue(page, out var box))
        {
            return NumberBox(box);
        }

        return
        [
            new NumberObject(0.0),
            new NumberObject(0.0),
            new NumberObject(page.Width.Point),
            new NumberObject(page.Height.Point),
        ];
    }

    private static ArrayObject NumberBox(ArrayObject box) =>
    [
        new NumberObject(Number(box[0])),
        new NumberObject(Number(box[1])),
        new NumberObject(Number(box[2])),
        new NumberObject(Number(box[3])),
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
