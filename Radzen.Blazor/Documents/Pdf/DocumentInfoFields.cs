using System;
using Radzen.Documents.Pdf.Write;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf;

internal sealed record DocumentInfoField(
    string Key,
    Func<DocumentInfo, string?> Value,
    Action<DocumentReader, DictionaryObject, DocumentInfo> Read);

internal static class DocumentInfoFields
{
    internal static readonly DocumentInfoField[] All =
    [
        new("Title", info => info.Title, (reader, source, info) => info.Title = DocumentLoader.Text(reader, source, "Title")),
        new("Author", info => info.Author, (reader, source, info) => info.Author = DocumentLoader.Text(reader, source, "Author")),
        new("Subject", info => info.Subject, (reader, source, info) => info.Subject = DocumentLoader.Text(reader, source, "Subject")),
        new("Keywords", info => info.Keywords, (reader, source, info) => info.Keywords = DocumentLoader.Text(reader, source, "Keywords")),
        new("Creator", info => info.Creator, (reader, source, info) => info.Creator = DocumentLoader.Text(reader, source, "Creator")),
        new("Producer", info => info.Producer, (reader, source, info) => info.Producer = DocumentLoader.Text(reader, source, "Producer")),
        new("CreationDate", info => info.CreationDate is { } value ? DocumentGraphBuilder.PdfDate(value) : null,
            (reader, source, info) => info.CreationDate = DocumentLoader.Date(reader, source, "CreationDate")),
        new("ModDate", info => info.ModificationDate is { } value ? DocumentGraphBuilder.PdfDate(value) : null,
            (reader, source, info) => info.ModificationDate = DocumentLoader.Date(reader, source, "ModDate")),
    ];

    internal static bool Contains(string key)
    {
        foreach (var field in All)
        {
            if (field.Key == key)
            {
                return true;
            }
        }

        return false;
    }
}
