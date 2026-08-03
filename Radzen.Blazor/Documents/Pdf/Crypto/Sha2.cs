using System;
using System.Security.Cryptography;

namespace Radzen.Documents.Pdf.Crypto;

internal static class Sha2
{
    public static byte[] ComputeHash256(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return ComputeHash256((ReadOnlySpan<byte>)data);
    }

    public static byte[] ComputeHash256(ReadOnlySpan<byte> data)
    {
#pragma warning disable RS0030
        return SHA256.HashData(data);
#pragma warning restore RS0030
    }

    public static byte[] ComputeHash384(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
#pragma warning disable RS0030
        return SHA384.HashData(data);
#pragma warning restore RS0030
    }

    public static byte[] ComputeHash512(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
#pragma warning disable RS0030
        return SHA512.HashData(data);
#pragma warning restore RS0030
    }
}

#pragma warning disable RS0030
internal sealed class Sha256Hasher
{
    private IncrementalHash? hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

    public void Append(ReadOnlySpan<byte> data) => Unfinished().AppendData(data);

    public void Append(byte value)
    {
        Span<byte> single = [value];
        Unfinished().AppendData(single);
    }

    public byte[] Finish()
    {
        var current = Unfinished();
        var digest = current.GetCurrentHash();
        current.Dispose();
        hash = null;
        return digest;
    }

    private IncrementalHash Unfinished()
        => hash
            ?? throw new InvalidOperationException(
                "This Sha256Hasher has already been finalized; create a new instance for another digest.");
}
#pragma warning restore RS0030
