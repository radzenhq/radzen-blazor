using System.Diagnostics.CodeAnalysis;

namespace Radzen.Documents.Pdf.Emit;

// A pluggable image-format decoder: sniffs its own magic bytes and, on a match,
// decodes the payload into an image XObject. Registered in ImageDecoder's registry
// so a new format is a new implementation plus a registration, not a central switch edit.
internal interface IImageDecoder
{
    bool TryDecode(byte[] data, ReaderLimits limits, [NotNullWhen(true)] out ImageXObject? xobject);
}
