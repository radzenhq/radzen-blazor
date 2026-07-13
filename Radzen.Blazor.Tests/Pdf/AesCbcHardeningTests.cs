#nullable enable
using System;
using Radzen.Documents.Crypto;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// AesCbc must fail loud on corrupt ciphertext rather than silently degrading:
// too-short input, a partial trailing block, and invalid PKCS#7 padding all throw
// DocumentParseException instead of returning truncated or padding-polluted bytes.
public class AesCbcHardeningTests
{
    private static readonly byte[] Key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
    private static readonly byte[] Iv = Convert.FromHexString("101112131415161718191a1b1c1d1e1f");

    [Fact]
    public void Decrypt_ShorterThanIv_Throws()
    {
        Assert.Throws<DocumentParseException>(() => AesCbc.Decrypt(Key, new byte[15]));
    }

    [Fact]
    public void Decrypt_IvOnlyNoCiphertext_Throws()
    {
        Assert.Throws<DocumentParseException>(() => AesCbc.Decrypt(Key, new byte[16]));
    }

    [Fact]
    public void Decrypt_CiphertextNotBlockAligned_Throws()
    {
        // IV(16) + 20 ciphertext bytes: the trailing 4 bytes are not a whole block.
        Assert.Throws<DocumentParseException>(() => AesCbc.Decrypt(Key, new byte[36]));
    }

    [Fact]
    public void DecryptCbcNoPadding_PartialBlock_Throws()
    {
        Assert.Throws<DocumentParseException>(() => AesCbc.DecryptCbcNoPadding(Key, Iv, new byte[20]));
    }

    [Fact]
    public void Decrypt_PadByteOutOfRange_Throws()
    {
        // Plaintext block whose last byte is 0x00 is not valid PKCS#7 (pad must be 1..16).
        var plain = new byte[16];
        var ivCipher = Concat(Iv, AesCbc.EncryptCbcNoPadding(Key, Iv, plain));
        Assert.Throws<DocumentParseException>(() => AesCbc.Decrypt(Key, ivCipher));
    }

    [Fact]
    public void Decrypt_InconsistentPadBytes_Throws()
    {
        // Last byte claims pad length 3, but the preceding padding bytes are not all 3.
        var plain = new byte[16];
        plain[15] = 3;
        plain[14] = 1;
        plain[13] = 2;
        var ivCipher = Concat(Iv, AesCbc.EncryptCbcNoPadding(Key, Iv, plain));
        Assert.Throws<DocumentParseException>(() => AesCbc.Decrypt(Key, ivCipher));
    }

    [Fact]
    public void Decrypt_ValidPadding_StillStrips()
    {
        // A well-formed PKCS#7 block (13 data bytes + three 0x03 pad bytes) round-trips.
        var plain = new byte[16];
        for (var i = 0; i < 13; i++)
        {
            plain[i] = (byte)(i + 1);
        }

        plain[13] = plain[14] = plain[15] = 3;
        var ivCipher = Concat(Iv, AesCbc.EncryptCbcNoPadding(Key, Iv, plain));
        Assert.Equal(plain[..13], AesCbc.Decrypt(Key, ivCipher));
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        var result = new byte[a.Length + b.Length];
        Array.Copy(a, result, a.Length);
        Array.Copy(b, 0, result, a.Length, b.Length);
        return result;
    }
}
