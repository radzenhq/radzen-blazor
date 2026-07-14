using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

// The cohesive, Document-owned holder for everything a document carries because
// some of its pages came from a parsed source: the original reader and its
// catalog/AcroForm, the exact original bytes (for an incremental re-save), the
// load-time metadata snapshot, per-page source attributes, and the handles that
// let an appended loaded page re-serialize faithfully. A freshly built document
// with no loaded or appended source pages has no LoadedState (Document.Loaded is
// null); DocumentLoader constructs a fully loaded one, and Document.Append lazily
// creates an append-only one (Source/SourceBytes null) to carry merged pages.
internal sealed class LoadedState
{
    // Per-page source attributes captured at load and carried through Append, read
    // on save so an untouched loaded page re-serializes with its original node,
    // effective /Resources, media/crop boxes and rotation.
    public Dictionary<Page, DictionaryObject> SourcePages { get; } = [];

    public List<Page> LoadedPages { get; } = [];

    public Dictionary<Page, byte[]?> SourceContents { get; } = [];

    public Dictionary<Page, DictionaryObject> SourceResources { get; } = [];

    public Dictionary<Page, ArrayObject> SourceBoxes { get; } = [];

    public Dictionary<Page, ArrayObject> SourceCropBoxes { get; } = [];

    public Dictionary<Page, int> SourceRotations { get; } = [];

    public Dictionary<Page, (Rect? Bleed, Rect? Trim, Rect? Art, int Rotate)> LoadedPageSettings { get; } = [];

    // Handles for pages appended from another loaded document: each appended page's
    // reader plus effective /Resources and source node, and each donor reader's
    // AcroForm, imported lazily on save.
    public Dictionary<Page, (DocumentReader Reader, DictionaryObject Resources)> AppendedResources { get; } = [];

    public Dictionary<Page, (DocumentReader Reader, DictionaryObject Node)> AppendedPages { get; } = [];

    public Dictionary<DocumentReader, DictionaryObject> AppendedAcroForms { get; } = [];

    // The parsed source reader and its catalog/AcroForm, set only when this document
    // was itself loaded (null for an append-only carry state).
    public DocumentReader? Source { get; }

    public DictionaryObject? SourceCatalog { get; set; }

    public DictionaryObject? SourceAcroForm { get; set; }

    // The exact original file bytes retained by DocumentLoader, so SaveIncremental can
    // preserve them verbatim as the prefix of the incremental output. Null unless loaded.
    public byte[]? SourceBytes { get; }

    // The document metadata as read at load time, so SaveIncremental emits an /Info
    // override only when the caller actually changed a modeled metadata field.
    public string?[]? LoadedInfoSnapshot { get; set; }

    public IReadOnlyList<OutlineSnapshot>? LoadedOutlineSnapshot { get; set; }

    public bool OutlineRequiresRewrite { get; set; }

    public IReadOnlyList<PageLabelSnapshot>? LoadedPageLabelsSnapshot { get; set; }

    public IReadOnlyList<AttachmentSnapshot>? LoadedAttachmentSnapshot { get; set; }

    // Append-only carry state: created lazily when a fresh (or already-loaded)
    // document receives pages appended from a loaded source.
    public LoadedState()
    {
    }

    // Loaded state: created by DocumentLoader for a document parsed from a source.
    public LoadedState(DocumentReader source, byte[] sourceBytes)
    {
        Source = source;
        SourceBytes = sourceBytes;
    }

    // Records an appended loaded page's reader and effective /Resources so its
    // fonts/images still resolve on save.
    public void RecordAppendedResources(Page page, DocumentReader reader, DictionaryObject resources)
        => AppendedResources[page] = (reader, resources);

    // Carries an appended page's source-derived attributes from the donor document's
    // loaded state: its source node (for /Annots and widget form fields) and donor
    // AcroForm, plus its media/crop boxes and rotation. Also carries through a chained
    // append, since a document produced by Append records the same entries under its
    // own keys even when it has no Source of its own.
    public void CarryAppended(Page page, Page source, LoadedState origin)
    {
        if (origin.Source is { } reader && origin.SourcePages.TryGetValue(source, out var sourceNode))
        {
            AppendedPages[page] = (reader, sourceNode);
            if (origin.SourceAcroForm is { } form)
            {
                AppendedAcroForms[reader] = form;
            }
        }

        if (origin.SourceBoxes.TryGetValue(source, out var box))
        {
            SourceBoxes[page] = box;
        }

        if (origin.SourceCropBoxes.TryGetValue(source, out var cropBox))
        {
            SourceCropBoxes[page] = cropBox;
        }

        if (origin.SourceRotations.TryGetValue(source, out var rotation))
        {
            SourceRotations[page] = rotation;
        }
    }

    // Drops the loaded AcroForm handle after FormWriter flattens it, so the next
    // save emits no /AcroForm.
    public void ClearAcroForm() => SourceAcroForm = null;
}

internal sealed record AttachmentSnapshot(
    Attachment Attachment,
    string? Description,
    DateTimeOffset ModificationDate,
    FacturXProfile? FacturX,
    string? DocumentType,
    string? Version,
    string? ConformanceLevel)
{
    public static IReadOnlyList<AttachmentSnapshot> Capture(AttachmentCollection attachments)
    {
        var result = new List<AttachmentSnapshot>(attachments.Count);
        foreach (var attachment in attachments)
        {
            result.Add(new AttachmentSnapshot(
                attachment,
                attachment.Description,
                attachment.ModificationDate,
                attachment.FacturX,
                attachment.FacturX?.DocumentType,
                attachment.FacturX?.Version,
                attachment.FacturX?.ConformanceLevel));
        }

        return result;
    }

    public static bool Matches(IReadOnlyList<AttachmentSnapshot> snapshot, AttachmentCollection attachments)
    {
        if (snapshot.Count != attachments.Count)
        {
            return false;
        }

        for (var i = 0; i < snapshot.Count; i++)
        {
            var expected = snapshot[i];
            var actual = attachments[i];
            if (!ReferenceEquals(expected.Attachment, actual)
                || expected.Description != actual.Description
                || expected.ModificationDate != actual.ModificationDate
                || !ReferenceEquals(expected.FacturX, actual.FacturX)
                || expected.DocumentType != actual.FacturX?.DocumentType
                || expected.Version != actual.FacturX?.Version
                || expected.ConformanceLevel != actual.FacturX?.ConformanceLevel)
            {
                return false;
            }
        }

        return true;
    }
}
