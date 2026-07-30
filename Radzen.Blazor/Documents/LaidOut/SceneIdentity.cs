using System;

namespace Radzen.Documents.LaidOut;

internal readonly record struct SourceId(int Value);

internal readonly record struct LaidOutNodeId(int Value);

internal sealed class SceneImageData
{
    private readonly byte[] bytes;

    internal SceneImageData(byte[] source)
    {
        bytes = [.. source];
    }

    internal ReadOnlyMemory<byte> Memory => bytes;
}
