using System.Collections.Generic;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Render;

// ISO 32000-1 11.6.6: transparency-group form XObject whose /Group declares /S /Transparency.
internal sealed class GeneratedTransparencyGroup
{
    public required byte[] Content { get; init; }

    public required double[] BBox { get; init; }

    public string? ColorSpace { get; init; }

    public bool? Isolated { get; init; }

    public bool? Knockout { get; init; }

    public IReadOnlyList<KeyValuePair<string, StreamObject>> XObjects { get; init; } = [];
}
