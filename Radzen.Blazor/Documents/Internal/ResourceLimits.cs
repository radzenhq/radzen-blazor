namespace Radzen.Documents;

internal sealed class ResourceLimits
{
    public long MaxFileBytes { get; init; } = 2L * 1024 * 1024 * 1024;

    public long MaxImagePixels { get; init; } = 64L * 1024 * 1024;

    public static ResourceLimits Default => new();
}
