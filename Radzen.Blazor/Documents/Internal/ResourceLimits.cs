namespace Radzen.Documents;

internal sealed class ResourceLimits
{
    internal const long DefaultMaxFileBytes = int.MaxValue;

    internal const long DefaultMaxImagePixels = 64L * 1024 * 1024;

    public long MaxFileBytes { get; init; } = DefaultMaxFileBytes;

    public long MaxImagePixels { get; init; } = DefaultMaxImagePixels;

    public static ResourceLimits Default { get; } = new();
}
