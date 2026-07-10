using System.IO;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// A physical PDF document: an ordered collection of pages plus document
/// metadata. Serialized through the object model as a classic PDF file.
/// </summary>
public sealed class Document
{
    /// <summary>Gets the document metadata.</summary>
    public DocumentInfo Info { get; } = new();

    /// <summary>Gets the ordered collection of pages.</summary>
    public PageCollection Pages { get; } = [];

    /// <summary>
    /// Serializes the document to a byte array.
    /// </summary>
    /// <returns>The complete PDF file bytes.</returns>
    public byte[] ToArray()
    {
        using var stream = new MemoryStream();
        Save(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Serializes the document to the given stream.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    public void Save(Stream stream)
    {
        System.ArgumentNullException.ThrowIfNull(stream);

        var writer = new DocumentWriter(stream);

        var catalog = new DictionaryObject();
        var catalogRef = writer.Add(catalog);

        var pagesNode = new DictionaryObject();
        var pagesRef = writer.Add(pagesNode);

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

            var content = page.GetContent();
            if (content is not null)
            {
                var contentStream = new StreamObject(content);
                pageNode["Contents"] = writer.Add(contentStream);
            }

            kids.Add(pageRef);
        }

        pagesNode["Type"] = new NameObject("Pages");
        pagesNode["Kids"] = kids;
        pagesNode["Count"] = new NumberObject(kids.Count);

        catalog["Type"] = new NameObject("Catalog");
        catalog["Pages"] = pagesRef;

        writer.Trailer["Root"] = catalogRef;

        var info = BuildInfo();
        if (info is not null)
        {
            writer.Trailer["Info"] = writer.Add(info);
        }

        writer.Close();
    }

    /// <summary>
    /// Serializes the document to the file at the given path.
    /// </summary>
    /// <param name="path">The destination file path.</param>
    public void Save(string path)
    {
        System.ArgumentNullException.ThrowIfNull(path);
        using var stream = File.Create(path);
        Save(stream);
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
