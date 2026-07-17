using System;

namespace Radzen.Documents.Pdf.Fonts;

// The one policy mapping a Font to a base-14 face, and the one message rejecting an
// unembedded base-14 face under PDF/A or PDF/UA.
internal static class FontResolution
{
    // A family with no base-14 metrics falls back to Helvetica rather than failing: the
    // standard-14 set is what a viewer is guaranteed to have without an embedded file.
    public static string ResolveBase14Name(Font font) => Base14Metrics.Resolve(font)?.PostScriptName ?? "Helvetica";

    // family is null where the caller only knows the resolved face, not the requested family.
    public static InvalidOperationException Base14Forbidden(string label, string psName, string? family)
        => new($"{label} forbids the standard-14 font '{psName}' referenced by name; register an embeddable font file{(family is null ? "" : $" for '{family}'")} with DocumentBuilder.Fonts instead.");
}
