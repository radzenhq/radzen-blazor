using System;
using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


/// <summary>
/// The relationship between an embedded file and the document, written as the
/// /AFRelationship key of the file specification (PDF 2.0 / PDF/A-3 associated files).
/// </summary>
public enum AttachmentRelationship
{
    /// <summary>The embedded file is the source material of the document.</summary>
    Source,

    /// <summary>The embedded file holds machine-readable data represented by the document (e.g. the Factur-X invoice XML).</summary>
    Data,

    /// <summary>The embedded file is an alternative representation of the document.</summary>
    Alternative,

    /// <summary>The embedded file supplements the document.</summary>
    Supplement,

    /// <summary>The relationship is not specified.</summary>
    Unspecified,
}

/// <summary>
/// The Factur-X / ZUGFeRD profile of an embedded <c>factur-x.xml</c> invoice,
/// written into the document XMP metadata (<c>fx:DocumentType</c>,
/// <c>fx:Version</c>, <c>fx:ConformanceLevel</c>). Set it on the attachment so the
/// XMP declares the invoice's real profile instead of the BASIC 1.0 defaults.
/// </summary>
public sealed class FacturXProfile
{
    /// <summary>Gets or sets the document type (<c>fx:DocumentType</c>), e.g. <c>INVOICE</c> or <c>ORDER</c>.</summary>
    public string DocumentType { get; set; } = "INVOICE";

    /// <summary>Gets or sets the standard version (<c>fx:Version</c>), e.g. <c>1.0</c>.</summary>
    public string Version { get; set; } = "1.0";

    /// <summary>Gets or sets the conformance level (<c>fx:ConformanceLevel</c>), e.g. <c>BASIC</c>, <c>EN 16931</c> or <c>EXTENDED</c>.</summary>
    public string ConformanceLevel { get; set; } = "BASIC";
}

/// <summary>
/// A file embedded into the produced PDF: name, payload, relationship and MIME type.
/// </summary>
public sealed class Attachment
{
    /// <summary>
    /// The modification date written when <see cref="ModificationDate"/> is not set.
    /// A fixed sentinel keeps the produced bytes reproducible.
    /// </summary>
    public static readonly DateTimeOffset DefaultModificationDate = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

    internal Attachment(string name, byte[] data, AttachmentRelationship relationship, string mimeType)
    {
        Name = name;
        Data = data;
        Relationship = relationship;
        MimeType = mimeType;
    }

    /// <summary>Gets the file name of the attachment as it appears in the PDF.</summary>
    public string Name { get; }

    /// <summary>Gets the relationship of the attachment to the document.</summary>
    public AttachmentRelationship Relationship { get; }

    /// <summary>Gets the MIME type of the attachment, e.g. <c>text/xml</c>.</summary>
    public string MimeType { get; }

    /// <summary>Returns a copy of the embedded file bytes.</summary>
    /// <returns>The embedded file bytes.</returns>
    public byte[] GetBytes() => (byte[])Data.Clone();

    /// <summary>
    /// Gets or sets the human-readable description written as the /Desc key of the
    /// file specification. Omitted when null or empty.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the modification date of the embedded file, written as the
    /// /Params /ModDate of the embedded file stream. Defaults to
    /// <see cref="DefaultModificationDate"/> so output stays deterministic; set it
    /// explicitly to record the real file timestamp.
    /// </summary>
    public DateTimeOffset ModificationDate { get; set; } = DefaultModificationDate;

    /// <summary>
    /// Gets or sets the Factur-X / ZUGFeRD profile declared in the document XMP for a
    /// <c>factur-x.xml</c> attachment of a PDF/A document. When null the XMP declares
    /// the BASIC 1.0 INVOICE defaults; set it to embed an EN 16931 or EXTENDED invoice.
    /// Ignored for attachments not named <c>factur-x.xml</c>.
    /// </summary>
    public FacturXProfile? FacturX { get; set; }

    internal byte[] Data { get; }
}

/// <summary>
/// The embedded files in a PDF. Each attachment is written as an /EmbeddedFiles
/// name-tree entry and an associated-files (/AF) file specification.
/// </summary>
public sealed class AttachmentCollection : IReadOnlyList<Attachment>
{
    private readonly List<Attachment> items = [];

    /// <summary>Gets the number of attachments.</summary>
    public int Count => items.Count;

    /// <summary>Gets the attachment at the given index.</summary>
    /// <param name="index">The zero-based index.</param>
    public Attachment this[int index] => items[index];

    /// <summary>
    /// Adds a file to embed into the produced PDF. Embedding arbitrary files in a
    /// PDF/A document requires <see cref="PdfAConformance.PdfA3B"/> or
    /// <see cref="PdfAConformance.PdfA3A"/>. An attachment named
    /// <c>factur-x.xml</c> additionally emits the Factur-X extension schema in the
    /// XMP metadata of a PDF/A-3 document.
    /// </summary>
    /// <param name="name">The file name of the attachment, e.g. <c>factur-x.xml</c>.</param>
    /// <param name="data">The file bytes.</param>
    /// <param name="relationship">The relationship of the file to the document.</param>
    /// <param name="mimeType">The MIME type of the file, e.g. <c>text/xml</c>.</param>
    /// <returns>The added attachment.</returns>
    public Attachment Add(string name, byte[] data, AttachmentRelationship relationship, string mimeType)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrEmpty(mimeType);

        var attachment = new Attachment(name, data, relationship, mimeType);
        items.Add(attachment);
        return attachment;
    }

    /// <summary>Removes an attachment.</summary>
    /// <param name="attachment">The attachment to remove.</param>
    /// <returns><c>true</c> when the attachment was removed.</returns>
    public bool Remove(Attachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        return items.Remove(attachment);
    }

    /// <summary>Removes the attachment at the given index.</summary>
    /// <param name="index">The zero-based index.</param>
    public void RemoveAt(int index) => items.RemoveAt(index);

    /// <summary>Removes all attachments.</summary>
    public void Clear() => items.Clear();

    internal void Add(Attachment attachment) => items.Add(attachment);

    /// <summary>Returns an enumerator over the attachments in insertion order.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<Attachment> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
