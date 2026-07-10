using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// A PDF array object (ISO 32000-1 section 7.3.6). Serialized as its elements
/// separated by single spaces and enclosed in square brackets.
/// </summary>
public sealed class ArrayObject : DocumentObject, IEnumerable<DocumentObject>
{
    private readonly List<DocumentObject> items = [];

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    public int Count => items.Count;

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based element index.</param>
    /// <returns>The element at <paramref name="index"/>.</returns>
    public DocumentObject this[int index] => items[index];

    /// <summary>
    /// Appends an element to the array.
    /// </summary>
    /// <param name="item">The element to append.</param>
    public void Add(DocumentObject item)
    {
        items.Add(item);
    }

    /// <inheritdoc />
    public override void Write(Stream stream)
    {
        stream.WriteByte((byte)'[');
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                stream.WriteByte((byte)' ');
            }

            items[i].Write(stream);
        }

        stream.WriteByte((byte)']');
    }

    /// <inheritdoc />
    public IEnumerator<DocumentObject> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
