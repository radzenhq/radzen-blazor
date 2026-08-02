using System.IO;

namespace Radzen.Documents.Pdf.Objects;

// ISO 32000-1 7.3: the COS object grammar.
internal abstract class DocumentObject
{
    private protected DocumentObject()
    {
    }

    public void Write(Stream stream) => Write(stream, WriteContext.None);

    internal abstract void Write(Stream stream, WriteContext context);
}
