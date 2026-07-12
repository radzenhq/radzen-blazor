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

    internal byte[] Data { get; }
}

/// <summary>
/// The files to embed into the produced PDF. Each attachment is written as an
/// /EmbeddedFiles name-tree entry and an associated-files (/AF) file specification.
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

    /// <summary>Returns an enumerator over the attachments in insertion order.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<Attachment> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
