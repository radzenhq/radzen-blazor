using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// A PDF dictionary object (ISO 32000-1 section 7.3.7). Entries preserve
/// insertion order and are serialized between <c>&lt;&lt;</c> and <c>&gt;&gt;</c>,
/// each key written as a name followed by its value.
/// </summary>
public sealed class DictionaryObject : DocumentObject, IEnumerable<KeyValuePair<string, DocumentObject>>
{
    private readonly List<string> keys = [];
    private readonly Dictionary<string, DocumentObject> values = [];

    /// <summary>
    /// Gets the number of entries in the dictionary.
    /// </summary>
    public int Count => keys.Count;

    /// <summary>
    /// Gets the keys in insertion order.
    /// </summary>
    public IReadOnlyList<string> Keys => keys;

    /// <summary>
    /// Gets or sets the value for the specified key (without a leading slash).
    /// Setting a new key appends it; setting an existing key preserves its position.
    /// </summary>
    /// <param name="key">The entry key.</param>
    /// <returns>The value for <paramref name="key"/>.</returns>
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

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><c>true</c> if the key is present; otherwise <c>false</c>.</returns>
    public bool ContainsKey(string key) => values.ContainsKey(key);

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">When this method returns, the value if found; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if the key is present; otherwise <c>false</c>.</returns>
    public bool TryGetValue(string key, out DocumentObject? value) => values.TryGetValue(key, out value);

    /// <inheritdoc />
    public override void Write(Stream stream)
    {
        PdfBytes.WriteAscii(stream, "<<");
        foreach (var key in keys)
        {
            stream.WriteByte((byte)' ');
            NameObject.WriteEscaped(stream, key);
            stream.WriteByte((byte)' ');
            values[key].Write(stream);
        }

        PdfBytes.WriteAscii(stream, " >>");
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, DocumentObject>> GetEnumerator()
    {
        foreach (var key in keys)
        {
            yield return new KeyValuePair<string, DocumentObject>(key, values[key]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
