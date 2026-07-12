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
    internal readonly Dictionary<Page, DictionaryObject> sourcePages = [];
    internal readonly Dictionary<Page, DictionaryObject> sourceResources = [];
    internal readonly Dictionary<Page, ArrayObject> sourceBoxes = [];
    internal readonly Dictionary<Page, ArrayObject> sourceCropBoxes = [];
    internal readonly Dictionary<Page, int> sourceRotations = [];
    internal readonly Dictionary<Page, (DocumentReader Reader, DictionaryObject Resources)> appendedResources = [];
    internal readonly Dictionary<Page, (DocumentReader Reader, DictionaryObject Node)> appendedPages = [];
    internal readonly Dictionary<DocumentReader, DictionaryObject> appendedAcroForms = [];
    internal DocumentReader? source;
    internal DictionaryObject? sourceCatalog;
    internal DictionaryObject? sourceAcroForm;

    /// <summary>Gets the document metadata.</summary>
    public DocumentInfo Info { get; } = new();

    /// <summary>Gets the ordered collection of pages.</summary>
    public PageCollection Pages { get; } = [];

    /// <summary>
    /// Gets the interactive form of a loaded document, or <c>null</c> when the
    /// document has no AcroForm.
    /// </summary>
    public AcroForm? AcroForm { get; internal set; }

    /// <summary>
    /// Gets the form fields to create on this document. Each definition is
    /// saved as a widget annotation on its page and listed in the catalog
    /// <c>/AcroForm /Fields</c> with a generated appearance stream.
    /// </summary>
    public IList<FormFieldDefinition> FormFields { get; } = [];

    /// <summary>
    /// Gets or sets the encryption to apply when saving. When <c>null</c> the
    /// document is written unencrypted.
    /// </summary>
    public Objects.Encryption.EncryptionOptions? Encryption { get; set; }

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
        => DocumentLoader.Load(stream, limits, options);

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
                page.SetTextFonts(DocumentLoader.BuildTextFonts(other.source, loadedResources));
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

    /// <summary>
    /// Flattens the interactive form into static page content: each field's
    /// current value renders onto its page, and the fields, their widget
    /// annotations and the catalog <c>/AcroForm</c> are removed. Applies both
    /// to a loaded form (<see cref="AcroForm"/>) and to pending
    /// <see cref="FormFields"/> definitions.
    /// </summary>
    public void Flatten()
    {
        foreach (var definition in FormFields)
        {
            if (definition.PageIndex < 0 || definition.PageIndex >= Pages.Count)
            {
                throw new InvalidOperationException(
                    $"Form field '{definition.Name}' targets page {definition.PageIndex}; the document has {Pages.Count} pages.");
            }

            DrawDefinition(Pages[definition.PageIndex], definition);
        }

        FormFields.Clear();
        FlattenLoadedForm();
    }

    private static void DrawDefinition(Page page, FormFieldDefinition definition)
    {
        if (definition is TextFieldDefinition text)
        {
            if (text.Value.Length == 0)
            {
                return;
            }

            var baseline = FieldAppearances.Baseline(definition.Height.Point, text.Font.Size);
            page.Content.Add(new TextContent(
                text.Value,
                definition.X + Unit.FromPoint(2.0),
                definition.Y + Unit.FromPoint(baseline))
            {
                Font = text.Font,
            });
        }
        else if (definition is CheckBoxFieldDefinition { Checked: true })
        {
            page.Content.Add(FieldAppearances.CheckMark(
                definition.X.Point, definition.Y.Point, definition.Width.Point, definition.Height.Point));
        }
    }

    // Renders every loaded widget's current value into its page content, strips
    // the widgets from the page /Annots and drops the form so the next save
    // emits no /AcroForm.
    private void FlattenLoadedForm()
    {
        if (source is null || sourceAcroForm is null)
        {
            return;
        }

        var formDa = sourceAcroForm.TryGetValue("DA", out var da) && source.Resolve(da!) is StringObject text
            ? text.Value
            : null;

        foreach (var page in Pages)
        {
            if (!sourcePages.TryGetValue(page, out var node)
                || !node.TryGetValue("Annots", out var annotsObject)
                || source.Resolve(annotsObject!) is not ArrayObject annots)
            {
                continue;
            }

            var remaining = new ArrayObject();
            var widgets = 0;
            foreach (var entry in annots)
            {
                if (source.Resolve(entry) is DictionaryObject annot && IsWidget(annot))
                {
                    widgets++;
                    DrawWidget(page, annot, formDa);
                }
                else
                {
                    remaining.Add(entry);
                }
            }

            if (widgets > 0)
            {
                node["Annots"] = remaining.Count > 0 ? remaining : (DocumentObject)new NullObject();
            }
        }

        sourceAcroForm = null;
        AcroForm = null;
    }

    private bool IsWidget(DictionaryObject annot)
        => annot.TryGetValue("Subtype", out var subtype) && source!.Resolve(subtype!) is NameObject name
            && string.Equals(name.Value, "Widget", StringComparison.Ordinal);

    // Draws a widget's current value as static content: a text or choice value
    // in its /DA font, a non-Off button state as the check-mark glyph. A hidden
    // widget (/F bit 2) contributes nothing but is still removed.
    private void DrawWidget(Page page, DictionaryObject widget, string? formDa)
    {
        if (widget.TryGetValue("F", out var f) && source!.Resolve(f!) is NumberObject flags
            && (flags.IntValue & 2) != 0)
        {
            return;
        }

        if (Inherited(widget, "FT") is not NameObject type)
        {
            return;
        }

        var (x, y, width, height) = WidgetRect(widget);
        if (string.Equals(type.Value, "Btn", StringComparison.Ordinal))
        {
            var state = widget.TryGetValue("AS", out var asObject) && source!.Resolve(asObject!) is NameObject asName
                ? asName.Value
                : (Inherited(widget, "V") as NameObject)?.Value;
            if (state is not null && !string.Equals(state, "Off", StringComparison.Ordinal))
            {
                page.Content.Add(FieldAppearances.CheckMark(x, y, width, height));
            }

            return;
        }

        if (type.Value is not ("Tx" or "Ch"))
        {
            return;
        }

        var value = Inherited(widget, "V") is StringObject stored
            ? FormField.DecodeTextString(stored.Value)
            : null;
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        var da = (Inherited(widget, "DA") as StringObject)?.Value ?? formDa;
        var (daFont, daSize) = FieldAppearances.ParseDefaultAppearance(da);
        var font = FieldAppearances.AppearanceFont(daFont, daSize > 0.0 ? daSize : FieldAppearances.DefaultFontSize);
        page.Content.Add(new TextContent(
            value,
            Unit.FromPoint(x + 2.0),
            Unit.FromPoint(y + FieldAppearances.Baseline(height, font.Size)))
        {
            Font = font,
        });
    }

    // Walks the widget's /Parent chain for an inheritable field attribute
    // (ISO 32000-1 12.7.3.1) and returns it resolved.
    private DocumentObject? Inherited(DictionaryObject widget, string key)
    {
        var current = widget;
        for (var depth = 0; current is not null && depth < 32; depth++)
        {
            if (current.TryGetValue(key, out var value))
            {
                return source!.Resolve(value!);
            }

            current = current.TryGetValue("Parent", out var parent)
                && source!.Resolve(parent!) is DictionaryObject next
                ? next
                : null;
        }

        return null;
    }

    private (double X, double Y, double Width, double Height) WidgetRect(DictionaryObject widget)
    {
        if (widget.TryGetValue("Rect", out var rectObject) && source!.Resolve(rectObject!) is ArrayObject rect
            && rect.Count >= 4)
        {
            var x0 = DocumentLoader.Number(source.Resolve(rect[0]));
            var y0 = DocumentLoader.Number(source.Resolve(rect[1]));
            var x1 = DocumentLoader.Number(source.Resolve(rect[2]));
            var y1 = DocumentLoader.Number(source.Resolve(rect[3]));
            return (Math.Min(x0, x1), Math.Min(y0, y1), Math.Abs(x1 - x0), Math.Abs(y1 - y0));
        }

        return (0.0, 0.0, 0.0, 0.0);
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
            new ConformanceWriter(this).ValidateConformance();
        }

        var writer = new DocumentWriter(stream) { Encryption = Encryption };

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
                ["MediaBox"] = PageResourceBuilder.MediaBox(this, page),
            };

            if (sourceCropBoxes.TryGetValue(page, out var cropBox))
            {
                pageNode["CropBox"] = PageResourceBuilder.NumberBox(cropBox);
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

                var resources = PageResourceBuilder.BuildGeneratedResources(writer, generated, fontRefs, imageRefs);
                if (overlayEmitter is not null)
                {
                    resources = PageResourceBuilder.OverlayResources(writer, resources, overlayEmitter);
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
                reservedNames = PageResourceBuilder.ResourceNames(source, reservedFrom);
            }
            else if (appendedResources.TryGetValue(page, out var reservedAppend))
            {
                reservedNames = PageResourceBuilder.ResourceNames(reservedAppend.Reader, reservedAppend.Resources);
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
            var emitted = activeEmitter is not null ? PageResourceBuilder.BuildResources(writer, activeEmitter) : null;
            DictionaryObject? merged;
            if (importer is not null && source is not null
                && sourceResources.TryGetValue(page, out var loadedResources))
            {
                merged = PageResourceBuilder.MergeResources(importer, source, loadedResources, emitted);
            }
            else if (appendedResources.TryGetValue(page, out var appended))
            {
                if (!appendImporters.TryGetValue(appended.Reader, out var appendImporter))
                {
                    appendImporter = new GraphImporter(appended.Reader, writer);
                    appendImporters[appended.Reader] = appendImporter;
                }

                merged = PageResourceBuilder.MergeResources(appendImporter, appended.Reader, appended.Resources, emitted);
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
            catalog["StructTreeRoot"] = StructureWriter.WriteStructureTree(writer, structure, pageNodes);
        }

        var appendedFields = AppendForms(pageNodes, appendImporters, writer);
        List<(int PageIndex, ReferenceObject Reference)> createdWidgets =
            FormFields.Count > 0 ? WriteCreatedFields(writer, pageNodes, appendedFields) : [];

        if (importer is not null)
        {
            var removed = PruneRemovedPages(importer);
            PreserveCatalog(importer, catalog, pagesRef);
            PreserveForm(importer, catalog, pageNodes, removed, appendedFields, writer);
        }
        else if (appendedFields.Count > 0)
        {
            catalog["AcroForm"] = writer.Add(FieldsForm(appendedFields));
        }

        foreach (var (pageIndex, reference) in createdWidgets)
        {
            var node = pageNodes[pageIndex].Node;
            if (node.TryGetValue("Annots", out var annots) && annots is ArrayObject array)
            {
                array.Add(reference);
            }
            else
            {
                node["Annots"] = new ArrayObject { reference };
            }
        }

        if (Attachments.Count > 0)
        {
            new AttachmentWriter(this).WriteAttachments(writer, catalog);
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
            new ConformanceWriter(this).WriteConformance(writer, catalog);
        }

        writer.Trailer["Root"] = catalogRef;

        var info = BuildInfo();
        if (info is not null)
        {
            writer.Trailer["Info"] = writer.Add(info);
        }

        writer.Close();
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
            catalog["AcroForm"] = writer.Add(FieldsForm(appendedFields));
        }
    }

    // Rebuilds the AcroForm field-by-field: source fields whose widget lived on a
    // removed page are dropped, and already-imported fields from appended pages are
    // added, so the merged form lists exactly the fields that still have a widget.
    private ReferenceObject ImportAcroForm(GraphImporter importer, DocumentReader reader, DictionaryObject acroForm, HashSet<DictionaryObject> removed, List<DocumentObject> appendedFields, DocumentWriter writer)
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
        ApplyCreatedFormDefaults(result);
        return writer.Add(result);
    }

    private DictionaryObject FieldsForm(List<DocumentObject> fieldRefs)
    {
        var fields = new ArrayObject();
        foreach (var field in fieldRefs)
        {
            fields.Add(field);
        }

        var form = new DictionaryObject { ["Fields"] = fields };
        ApplyCreatedFormDefaults(form);
        return form;
    }

    // Gives a form holding created fields the defaults an editor needs: a form
    // /DA, the /DR fonts the created field /DA strings name, and /NeedAppearances
    // when a value falls outside WinAnsi so viewers regenerate it with a capable
    // font. No-op when no fields were created, keeping untouched saves identical.
    private void ApplyCreatedFormDefaults(DictionaryObject form)
    {
        if (FormFields.Count == 0)
        {
            return;
        }

        if (!form.ContainsKey("DA"))
        {
            form["DA"] = new StringObject("/Helv 0 Tf 0 g");
        }

        if (!form.ContainsKey("DR"))
        {
            var fonts = new DictionaryObject { ["Helv"] = PageResourceBuilder.Base14FontDictionary("Helvetica") };
            foreach (var definition in FormFields)
            {
                if (definition is TextFieldDefinition text)
                {
                    var baseFont = BaseFontOf(text.Font);
                    if (!fonts.ContainsKey(baseFont))
                    {
                        fonts[baseFont] = PageResourceBuilder.Base14FontDictionary(baseFont);
                    }
                }
            }

            form["DR"] = new DictionaryObject { ["Font"] = fonts };
        }

        foreach (var definition in FormFields)
        {
            if (definition is TextFieldDefinition text && !FieldAppearances.CanEncode(text.Value))
            {
                form["NeedAppearances"] = new BooleanObject(true);
                break;
            }
        }
    }

    private static string BaseFontOf(Font font)
        => Fonts.Base14Metrics.Resolve(font)?.PostScriptName ?? "Helvetica";

    // Emits one merged field/widget annotation per FormFields definition, each
    // with a generated normal appearance, and lists it in the form fields. The
    // returned page bindings attach to the page /Annots after any preserved
    // form rebuilds them.
    private List<(int PageIndex, ReferenceObject Reference)> WriteCreatedFields(
        DocumentWriter writer,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes,
        List<DocumentObject> fields)
    {
        var created = new List<(int, ReferenceObject)>();
        foreach (var definition in FormFields)
        {
            if (definition.PageIndex < 0 || definition.PageIndex >= pageNodes.Count)
            {
                throw new InvalidOperationException(
                    $"Form field '{definition.Name}' targets page {definition.PageIndex}; the document has {pageNodes.Count} pages.");
            }

            var x = definition.X.Point;
            var y = definition.Y.Point;
            var width = definition.Width.Point;
            var height = definition.Height.Point;
            var widget = new DictionaryObject
            {
                ["Type"] = new NameObject("Annot"),
                ["Subtype"] = new NameObject("Widget"),
                ["T"] = new StringObject(definition.Name),
                ["Rect"] = new ArrayObject
                {
                    new NumberObject(x),
                    new NumberObject(y),
                    new NumberObject(x + width),
                    new NumberObject(y + height),
                },
                ["F"] = new NumberObject(4),
                ["P"] = pageNodes[definition.PageIndex].Reference,
            };

            if (definition is TextFieldDefinition text)
            {
                widget["FT"] = new NameObject("Tx");
                widget["V"] = new StringObject(text.Value);
                widget["DA"] = new StringObject(
                    "/" + BaseFontOf(text.Font)
                    + " " + text.Font.Size.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
                    + " Tf 0 g");
                if (FieldAppearances.CanEncode(text.Value))
                {
                    widget["AP"] = new DictionaryObject
                    {
                        ["N"] = writer.Add(FieldAppearances.BuildText(text.Value, width, height, text.Font)),
                    };
                }
            }
            else if (definition is CheckBoxFieldDefinition checkBox)
            {
                var state = checkBox.Checked ? "Yes" : "Off";
                widget["FT"] = new NameObject("Btn");
                widget["V"] = new NameObject(state);
                widget["AS"] = new NameObject(state);
                widget["AP"] = new DictionaryObject
                {
                    ["N"] = new DictionaryObject
                    {
                        ["Yes"] = writer.Add(FieldAppearances.BuildCheck(width, height)),
                        ["Off"] = writer.Add(FieldAppearances.BuildOff(width, height)),
                    },
                };
            }

            var reference = writer.Add(widget);
            fields.Add(reference);
            created.Add((definition.PageIndex, reference));
        }

        return created;
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
