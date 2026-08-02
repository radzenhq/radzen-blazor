using System;

namespace Radzen.Documents.LaidOut;

internal readonly record struct SourceId(int Value);

internal readonly record struct PaintId(int Value);

internal sealed class SceneImageData
{
    private readonly byte[] bytes;

    internal SceneImageData(byte[] source, string? mediaType = null)
    {
        bytes = [.. source];
        MediaType = mediaType;
    }

    internal ReadOnlyMemory<byte> Memory => bytes;

    internal string? MediaType { get; }
}
