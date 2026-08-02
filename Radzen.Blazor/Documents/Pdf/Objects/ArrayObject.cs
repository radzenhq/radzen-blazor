using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf.Objects;

// ISO 32000-1 7.3.6.
internal sealed class ArrayObject : DocumentObject, IEnumerable<DocumentObject>
{
    private readonly List<DocumentObject> items = [];

    public int Count => items.Count;

    public DocumentObject this[int index] => items[index];

    public void Add(DocumentObject item)
    {
        items.Add(item);
    }

    internal override void Write(Stream stream, WriteContext context)
    {
        stream.WriteByte((byte)'[');
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                stream.WriteByte((byte)' ');
            }

            items[i].Write(stream, context);
        }

        stream.WriteByte((byte)']');
    }

    public IEnumerator<DocumentObject> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
