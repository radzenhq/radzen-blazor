using System;
using Radzen.Documents.Fonts;

namespace Radzen.Documents.Pdf.Fonts;

internal readonly record struct FontScope(
    FontCollection? Fonts,
    FontCollectionSnapshot? Snapshot,
    string? Base14ForbiddenBy,
    bool CanEmbed);

internal static class FontResolution
{
    public static string ResolveBase14Name(Font font, FontScope scope)
    {
        var metrics = BuiltInFontMetrics.Resolve(font);
        var family = font.EffectiveFamily;

        if (!string.IsNullOrEmpty(family))
        {
            if (!scope.CanEmbed
                && (scope.Fonts is { } collection && collection.TryResolvePrimary(font, out _)
                    || scope.Snapshot is { } snapshot && snapshot.HasFamily(family)))
            {
                throw new NotSupportedException(
                    $"Font family '{family}' is registered as an embeddable font file, but text added to a loaded or already-built page cannot embed one; use a base-14 family (Helvetica, Courier, Times, Symbol, ZapfDingbats) here.");
            }

            if (metrics is null)
            {
                throw new NotSupportedException(
                    $"Font family '{family}' is not one of the base-14 families (Helvetica, Courier, Times, Symbol, ZapfDingbats)"
                    + (scope.CanEmbed
                        ? "; register an embeddable font file for it with Document.Fonts, or use a base-14 family."
                        : ", and text added to a loaded or already-built page cannot embed a font file; use a base-14 family here."));
            }
        }

        var psName = metrics is { } resolved ? StandardFonts.PostScriptName(resolved.Face()) : "Helvetica";

        if (scope.Base14ForbiddenBy is { } label)
        {
            throw Base14Forbidden(label, psName, family);
        }

        return psName;
    }

    public static InvalidOperationException Base14Forbidden(string label, string psName, string? family)
        => new($"{label} forbids the standard-14 font '{psName}' referenced by name; register an embeddable font file{(string.IsNullOrEmpty(family) ? "" : $" for '{family}'")} with Document.Fonts instead.");
}
