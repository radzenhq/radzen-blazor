using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Documents.Pdf;

internal sealed class AppliedImageCache<T>
{
    private readonly Dictionary<Image, Entry> entries = [];

    public T Get(Image image, Func<T> create)
    {
        if (entries.TryGetValue(image, out var entry) && entry.Matches(image))
        {
            return entry.Value;
        }

        var value = create();
        entries[image] = new Entry(
            image.Interpolate, image.Stencil, image.ColorKeyMask?.ToArray(), value);
        return value;
    }

    private sealed record Entry(bool Interpolate, bool Stencil, int[]? ColorKeyMask, T Value)
    {
        public bool Matches(Image image)
            => Interpolate == image.Interpolate
                && Stencil == image.Stencil
                && (ColorKeyMask is null
                    ? image.ColorKeyMask is null
                    : image.ColorKeyMask is not null && ColorKeyMask.AsSpan().SequenceEqual(image.ColorKeyMask));
    }
}
