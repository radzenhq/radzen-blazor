using System;

namespace Radzen.Documents.Pdf.Fonts;

// What a font may resolve to at one emission site: the registry an embeddable face could come
// from, the conformance label forbidding an unembedded base-14 face (null when none does), and
// whether the site can embed a font file at all. Capability is a parameter so the generated and
// non-embedding worlds share one decision point without sharing their timing or their remedy.
internal readonly record struct FontScope(FontCollection? Fonts, string? Base14ForbiddenBy, bool CanEmbed);

// The one policy mapping a Font to a base-14 face, and the one message rejecting an
// unembedded base-14 face under PDF/A or PDF/UA.
internal static class FontResolution
{
    // Only the standard-14 set is guaranteed present in a viewer without an embedded file, so a
    // family with neither base-14 metrics nor a registered file has no honest rendering, and
    // substituting Helvetica ships the wrong glyphs and metrics under the caller's own name. An
    // empty Font.Name is not a family: it is the documented default-font path and stays Helvetica.
    public static string ResolveBase14Name(Font font, FontScope scope)
    {
        var metrics = Base14Metrics.Resolve(font);

        if (!string.IsNullOrEmpty(font.Name))
        {
            if (!scope.CanEmbed && scope.Fonts is { } collection && collection.TryResolvePrimary(font, out _))
            {
                throw new NotSupportedException(
                    $"Font family '{font.Name}' is registered as an embeddable font file, but text added to a loaded or already-built page cannot embed one; use a base-14 family (Helvetica, Courier, Times, Symbol, ZapfDingbats) here.");
            }

            if (metrics is null)
            {
                throw new NotSupportedException(
                    $"Font family '{font.Name}' is not one of the base-14 families (Helvetica, Courier, Times, Symbol, ZapfDingbats)"
                    + (scope.CanEmbed
                        ? "; register an embeddable font file for it with DocumentBuilder.Fonts, or use a base-14 family."
                        : ", and text added to a loaded or already-built page cannot embed a font file; use a base-14 family here."));
            }
        }

        var psName = metrics?.PostScriptName ?? "Helvetica";

        if (scope.Base14ForbiddenBy is { } label)
        {
            throw Base14Forbidden(label, psName, font.Name);
        }

        return psName;
    }

    // family is null or empty where the caller only knows the resolved face, not the requested family.
    public static InvalidOperationException Base14Forbidden(string label, string psName, string? family)
        => new($"{label} forbids the standard-14 font '{psName}' referenced by name; register an embeddable font file{(string.IsNullOrEmpty(family) ? "" : $" for '{family}'")} with DocumentBuilder.Fonts instead.");
}
