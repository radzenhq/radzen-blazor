using System;

namespace Radzen.Documents.Pdf.Fonts;

internal readonly record struct FontScope(FontCollection? Fonts, string? Base14ForbiddenBy, bool CanEmbed);

internal static class FontResolution
{
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

    public static InvalidOperationException Base14Forbidden(string label, string psName, string? family)
        => new($"{label} forbids the standard-14 font '{psName}' referenced by name; register an embeddable font file{(string.IsNullOrEmpty(family) ? "" : $" for '{family}'")} with DocumentBuilder.Fonts instead.");
}
