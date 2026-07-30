using System.Collections.Generic;
using Radzen.Documents.Pdf.Emission;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Content;

internal sealed class ContentResourceManifest
{
    public ContentResourceManifest(
        IReadOnlyList<KeyValuePair<string, string>> fonts,
        IReadOnlyList<KeyValuePair<string, ImageXObject>> images,
        IReadOnlyList<KeyValuePair<string, DictionaryObject>> patterns,
        IReadOnlyList<KeyValuePair<string, double>> extGStates)
    {
        Fonts = fonts;
        Images = images;
        Patterns = patterns;
        ExtGStates = extGStates;

        var imageResources = new List<EmissionImage>(images.Count);
        foreach (var image in images)
        {
            imageResources.Add(new EmissionImage(
                image.Value,
                image.Key,
                EmissionStreamPayload.Capture(image.Value.Image),
                image.Value.SoftMask is { } mask ? EmissionStreamPayload.Capture(mask) : null));
        }

        ImagesForWriting = imageResources;
    }

    public static ContentResourceManifest Empty { get; } = new([], [], [], []);

    public IReadOnlyList<KeyValuePair<string, string>> Fonts { get; }

    public IReadOnlyList<KeyValuePair<string, ImageXObject>> Images { get; }

    internal IReadOnlyList<EmissionImage> ImagesForWriting { get; }

    public IReadOnlyList<KeyValuePair<string, DictionaryObject>> Patterns { get; }

    public IReadOnlyList<KeyValuePair<string, double>> ExtGStates { get; }

    public bool IsEmpty => Fonts.Count == 0 && Images.Count == 0 && Patterns.Count == 0 && ExtGStates.Count == 0;

    public static ContentResourceManifest Combine(ContentResourceManifest first, ContentResourceManifest second)
        => first.IsEmpty ? second
            : second.IsEmpty ? first
            : new ContentResourceManifest(
                [.. first.Fonts, .. second.Fonts],
                [.. first.Images, .. second.Images],
                [.. first.Patterns, .. second.Patterns],
                [.. first.ExtGStates, .. second.ExtGStates]);
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
