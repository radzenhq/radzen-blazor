using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf.Objects;

// ISO 32000-1 7.3.7.
internal sealed class DictionaryObject : DocumentObject, IEnumerable<KeyValuePair<string, DocumentObject>>
{
    private readonly List<string> keys = [];
    private readonly Dictionary<string, DocumentObject> values = [];

    public int Count => keys.Count;

    public IReadOnlyList<string> Keys => keys;

    public DocumentObject this[string key]
    {
        get => values[key];
        set
        {
            if (!values.ContainsKey(key))
            {
                keys.Add(key);
            }

            values[key] = value;
        }
    }

    public bool ContainsKey(string key) => values.ContainsKey(key);

    public bool TryGetValue(string key, out DocumentObject? value) => values.TryGetValue(key, out value);

    internal override void Write(Stream stream, WriteContext context)
    {
        PdfBytes.WriteAscii(stream, "<<");
        foreach (var key in keys)
        {
            stream.WriteByte((byte)' ');
            NameObject.WriteEscaped(stream, key);
            stream.WriteByte((byte)' ');
            values[key].Write(stream, context);
        }

        PdfBytes.WriteAscii(stream, " >>");
    }

    public IEnumerator<KeyValuePair<string, DocumentObject>> GetEnumerator()
    {
        foreach (var key in keys)
        {
            yield return new KeyValuePair<string, DocumentObject>(key, values[key]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
