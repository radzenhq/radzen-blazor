using System;
using System.IO;

namespace Radzen.Documents.Core;

/// <summary>
/// Resource limits that bound the work done on image and font payloads, whether they are authored
/// into a document or read out of one. They turn an attacker-controlled or accidental size into a
/// recoverable exception instead of an out-of-memory failure. Pass them to the authoring entry
/// points that buffer a stream, and to the renderer's image decoders to govern measurement and
/// decoding during rendering.
/// </summary>
public class ResourceLimits
{
    internal const long DefaultMaxFileBytes = int.MaxValue;

    internal const long DefaultMaxImagePixels = 64L * 1024 * 1024;

    private protected ResourceLimits()
    {
    }

    /// <summary>Maximum decoded image size in pixels (width * height). Default 64M (e.g. 8000 x 8000).</summary>
    public long MaxImagePixels { get; init; } = DefaultMaxImagePixels;

    /// <summary>
    /// Maximum size in bytes of a source file buffered while loading. Default 2 GiB minus one byte,
    /// the largest buffer that can be addressed as a single array.
    /// </summary>
    public long MaxFileBytes { get; init; } = DefaultMaxFileBytes;

    /// <summary>The default limits used when a caller does not supply their own.</summary>
    public static ResourceLimits Default => new();

    internal virtual ResourceLimits Snapshot()
    {
        Validate();

        return new ResourceLimits
        {
            MaxImagePixels = MaxImagePixels,
            MaxFileBytes = MaxFileBytes,
        };
    }

    internal void ValidateImageDimensions(long width, long height, string format)
    {
        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException($"{format} image has invalid dimensions.");
        }

        if (width * height > MaxImagePixels)
        {
            throw new InvalidDataException($"{format} image dimensions exceed the maximum decodable size.");
        }
    }

    private protected void Validate()
    {
        RequirePositive(MaxImagePixels, nameof(MaxImagePixels));
        RequirePositive(MaxFileBytes, nameof(MaxFileBytes));
    }

    private protected static void RequirePositive(long value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, "Resource limits must be positive.");
        }
    }
}
