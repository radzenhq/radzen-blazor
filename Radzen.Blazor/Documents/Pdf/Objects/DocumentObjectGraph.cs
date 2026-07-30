using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Objects.Encryption;

namespace Radzen.Documents.Pdf.Objects;

internal sealed class DocumentObjectGraph : IObjectWriter
{
    private readonly List<DocumentObject> objects = [];

    public DictionaryObject Trailer { get; } = new();

    public string Version { get; set; } = "1.7";

    public EncryptionOptions? Encryption { get; set; }

    public bool UseCompressedStreams { get; set; }

    public IReadOnlyList<DocumentObject> Objects => objects;

    public DictionaryObject? Catalog { get; set; }

    public IReadOnlyList<DictionaryObject> Pages { get; set; } = [];

    public ReferenceObject Add(DocumentObject value)
        => IndirectObjectRegistration.Add(value, Append);

    public DocumentObject? Resolve(ReferenceObject reference)
        => reference.ObjectNumber >= 1 && reference.ObjectNumber <= objects.Count
            ? objects[reference.ObjectNumber - 1]
            : null;

    public IReadOnlyDictionary<DocumentObject, int> BuildObjectNumberIndex()
    {
        var result = new Dictionary<DocumentObject, int>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < objects.Count; i++)
        {
            result[objects[i]] = i + 1;
        }

        return result;
    }

    public DocumentObjectGraph CopyForSerialization()
    {
        var copy = new DocumentObjectGraph
        {
            Version = Version,
            Encryption = Encryption,
            UseCompressedStreams = UseCompressedStreams,
            Catalog = Catalog,
            Pages = Pages,
        };
        foreach (var item in objects)
        {
            copy.Add(item);
        }

        foreach (var item in Trailer)
        {
            copy.Trailer[item.Key] = item.Value;
        }

        return copy;
    }

    private int Append(DocumentObject value)
    {
        ArgumentNullException.ThrowIfNull(value);
        objects.Add(value);
        return objects.Count;
    }
}
