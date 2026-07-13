using System;
using Radzen.Documents.Crypto;

namespace Radzen.Documents.Pdf.Objects.Encryption;

/// <summary>
/// Supplies the unpredictable bytes standard PDF encryption needs - the document
/// <c>/ID</c>, the AES-256 file key, per-stream AES initialisation vectors, the
/// revision 6 validation and key salts and the <c>/Perms</c> noise. The library
/// never invokes a random number generator itself (it must stay reproducible and
/// safe for Blazor WebAssembly); the caller owns the randomness, exactly as
/// <c>Radzen.Documents.Pdf.Signing.ISigner</c> owns the private-key operation.
/// </summary>
/// <remarks>
/// During a single write the library requests bytes with a strictly increasing
/// <c>index</c> (0, 1, 2, ...) in a fixed order. An implementation that is a pure
/// function of <c>index</c> and <c>count</c> therefore yields byte-identical
/// documents across repeated writes, even when the same instance is reused. Supply
/// a securely seeded implementation in production;
/// <see cref="SeededEncryptionMaterial"/> derives its bytes deterministically from a
/// caller-provided seed.
/// </remarks>
public interface IEncryptionMaterial
{
    /// <summary>
    /// Returns <paramref name="count"/> bytes for the request numbered
    /// <paramref name="index"/>. The same (index, count) pair must always return the
    /// same bytes so encrypted output is reproducible.
    /// </summary>
    /// <param name="index">Zero-based ordinal of the request within one write.</param>
    /// <param name="count">Number of bytes to return.</param>
    byte[] GetBytes(int index, int count);
}

/// <summary>
/// Deterministic <see cref="IEncryptionMaterial"/> that expands a fixed seed with the
/// pure-managed SHA-256 (<see cref="Sha2"/>). The same seed reproduces the same
/// encrypted document, while distinct request indices yield distinct bytes so
/// per-stream AES initialisation vectors never repeat.
/// </summary>
public sealed class SeededEncryptionMaterial : IEncryptionMaterial
{
    private readonly byte[] seed;

    /// <summary>Creates material seeded with a copy of <paramref name="seed"/>.</summary>
    /// <param name="seed">The seed bytes. Must be non-empty.</param>
    public SeededEncryptionMaterial(byte[] seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        if (seed.Length == 0)
        {
            throw new ArgumentException("Seed must be non-empty.", nameof(seed));
        }

        this.seed = (byte[])seed.Clone();
    }

    /// <inheritdoc />
    public byte[] GetBytes(int index, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var result = new byte[count];
        var written = 0;
        var block = 0;
        while (written < count)
        {
            var prefix = new byte[seed.Length + 8];
            Array.Copy(seed, prefix, seed.Length);
            WriteInt(prefix, seed.Length, index);
            WriteInt(prefix, seed.Length + 4, block);
            var hash = Sha2.ComputeHash256(prefix);
            var take = Math.Min(hash.Length, count - written);
            Array.Copy(hash, 0, result, written, take);
            written += take;
            block++;
        }

        return result;
    }

    private static void WriteInt(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }
}

// Threads a caller's IEncryptionMaterial through one write, handing out the fixed
// order of byte requests the standard security handler makes. Created fresh per
// write so the request index always restarts at zero and output stays reproducible.
internal sealed class MaterialSequence(IEncryptionMaterial material)
{
    private int index;

    public byte[] Next(int count) => material.GetBytes(index++, count);
}
