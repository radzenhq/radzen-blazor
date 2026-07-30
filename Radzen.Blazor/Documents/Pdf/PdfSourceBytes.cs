using System.IO;
using Radzen.Documents.Internal;

namespace Radzen.Documents.Pdf;

internal static class PdfSourceBytes
{
    internal static byte[] ReadFully(Stream stream, long maxFileBytes)
    {
        try
        {
            return StreamBytes.ReadFully(stream, maxFileBytes);
        }
        catch (InvalidDataException exception)
        {
            throw new DocumentParseException(exception.Message, -1);
        }
    }
}
