using System;

namespace Radzen.Documents.Pdf;

internal static class Fnv1a64
{
    public const ulong OffsetBasis = 14695981039346656037;

    private const ulong Prime = 1099511628211;

    public static ulong Hash(ReadOnlySpan<byte> data, ulong hash = OffsetBasis)
    {
        foreach (var b in data)
        {
            hash = (hash ^ b) * Prime;
        }

        return hash;
    }
}

internal static class Fnv1a32
{
    public const uint OffsetBasis = 2166136261;

    private const uint Prime = 16777619;

    public static uint Combine(uint hash, uint value) => (hash ^ value) * Prime;
}
