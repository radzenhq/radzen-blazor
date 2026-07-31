using System.IO;
#nullable enable
using System;
using Radzen.Documents.Pdf.Crypto;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Pdf;

namespace Radzen.Blazor.Pdf.Tests;

public class AesCbcHardeningTests
{
    private static readonly byte[] Key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
    private static readonly byte[] Iv = Convert.FromHexString("101112131415161718191a1b1c1d1e1f");

    [Fact]
    public void Decrypt_EmptyCiphertext_Throws()
    {
        Assert.Throws<InvalidDataException>(() => AesCbc.Decrypt(Key, Iv, []));
    }

    [Fact]
    public void Decrypt_CiphertextNotBlockAligned_Throws()
    {
        Assert.Throws<InvalidDataException>(() => AesCbc.Decrypt(Key, Iv, new byte[20]));
    }

    [Fact]
    public void DecryptCbcNoPadding_PartialBlock_Throws()
    {
        Assert.Throws<InvalidDataException>(() => AesCbc.DecryptCbcNoPadding(Key, Iv, new byte[20]));
    }

    [Fact]
    public void Decrypt_PadByteOutOfRange_Throws()
    {
        var plain = new byte[16];
        var cipher = AesCbc.EncryptCbcNoPadding(Key, Iv, plain);
        Assert.Throws<InvalidDataException>(() => AesCbc.Decrypt(Key, Iv, cipher));
    }

    [Fact]
    public void Decrypt_InconsistentPadBytes_Throws()
    {
        var plain = new byte[16];
        plain[15] = 3;
        plain[14] = 1;
        plain[13] = 2;
        var cipher = AesCbc.EncryptCbcNoPadding(Key, Iv, plain);
        Assert.Throws<InvalidDataException>(() => AesCbc.Decrypt(Key, Iv, cipher));
    }

    [Fact]
    public void Decrypt_ValidPadding_StillStrips()
    {
        var plain = new byte[16];
        for (var i = 0; i < 13; i++)
        {
            plain[i] = (byte)(i + 1);
        }

        plain[13] = plain[14] = plain[15] = 3;
        var cipher = AesCbc.EncryptCbcNoPadding(Key, Iv, plain);
        Assert.Equal(plain[..13], AesCbc.Decrypt(Key, Iv, cipher));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(17)]
    public void DecryptCbcNoPadding_IvLengthOtherThanSixteen_Throws(int length)
    {
        Assert.Throws<ArgumentException>(() => AesCbc.DecryptCbcNoPadding(Key, new byte[length], new byte[16]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(17)]
    public void EncryptCbcNoPadding_IvLengthOtherThanSixteen_Throws(int length)
    {
        Assert.Throws<ArgumentException>(() => AesCbc.EncryptCbcNoPadding(Key, new byte[length], new byte[16]));
    }

    [Fact]
    public void EncryptCbcNoPadding_PartialBlock_Throws()
    {
        Assert.Throws<ArgumentException>(() => AesCbc.EncryptCbcNoPadding(Key, Iv, new byte[20]));
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        var result = new byte[a.Length + b.Length];
        Array.Copy(a, result, a.Length);
        Array.Copy(b, 0, result, a.Length, b.Length);
        return result;
    }
}
