using System.Collections.Generic;
using Radzen.Documents.Pdf.Emit;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Content;

internal sealed class ContentResourceManifest(
    IReadOnlyList<KeyValuePair<string, string>> fonts,
    IReadOnlyList<KeyValuePair<string, ImageXObject>> images,
    IReadOnlyList<KeyValuePair<string, DictionaryObject>> patterns)
{
    public static ContentResourceManifest Empty { get; } = new([], [], []);

    public IReadOnlyList<KeyValuePair<string, string>> Fonts { get; } = fonts;

    public IReadOnlyList<KeyValuePair<string, ImageXObject>> Images { get; } = images;

    public IReadOnlyList<KeyValuePair<string, DictionaryObject>> Patterns { get; } = patterns;

    public bool IsEmpty => Fonts.Count == 0 && Images.Count == 0 && Patterns.Count == 0;
}

internal sealed class ContentEmissionResult(
    byte[]? bytes,
    ContentResourceManifest resources,
    ContentEmissionResult? overlay = null,
    bool isEmitted = false)
{
    public byte[]? Bytes { get; } = bytes;

    public ContentResourceManifest Resources { get; } = resources;

    public ContentEmissionResult? Overlay { get; } = overlay;

    public bool IsEmitted { get; } = isEmitted;
}
