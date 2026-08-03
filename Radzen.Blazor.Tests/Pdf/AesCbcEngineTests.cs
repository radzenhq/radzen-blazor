#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf.Crypto;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class AesCbcEngineTests
{
    private static readonly byte[] Key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
    private static readonly byte[] Iv = Convert.FromHexString("101112131415161718191a1b1c1d1e1f");

    private static AesCbcEngine Engine => new(null);

    [Fact]
    public void DecryptPadded_EmptyCiphertext_Throws()
    {
        Assert.Throws<InvalidDataException>(() => Engine.DecryptPadded(Key, Iv, []));
    }

    [Fact]
    public void DecryptPadded_CiphertextNotBlockAligned_Throws()
    {
        Assert.Throws<InvalidDataException>(() => Engine.DecryptPadded(Key, Iv, new byte[20]));
    }

    [Fact]
    public void DecryptNoPadding_PartialBlock_Throws()
    {
        Assert.Throws<InvalidDataException>(() => Engine.DecryptNoPadding(Key, Iv, new byte[20]));
    }

    [Fact]
    public void DecryptPadded_PadByteOutOfRange_Throws()
    {
        var cipher = Engine.EncryptNoPadding(Key, Iv, new byte[16]);
        Assert.Throws<InvalidDataException>(() => Engine.DecryptPadded(Key, Iv, cipher));
    }

    [Fact]
    public void DecryptPadded_InconsistentPadBytes_Throws()
    {
        var plain = new byte[16];
        plain[15] = 3;
        plain[14] = 1;
        plain[13] = 2;
        var cipher = Engine.EncryptNoPadding(Key, Iv, plain);
        Assert.Throws<InvalidDataException>(() => Engine.DecryptPadded(Key, Iv, cipher));
    }

    [Fact]
    public void DecryptPadded_ValidPadding_StripsIt()
    {
        var plain = new byte[16];
        for (var i = 0; i < 13; i++)
        {
            plain[i] = (byte)(i + 1);
        }

        plain[13] = plain[14] = plain[15] = 3;
        var cipher = Engine.EncryptNoPadding(Key, Iv, plain);
        Assert.Equal(plain[..13], Engine.DecryptPadded(Key, Iv, cipher));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(17)]
    public void DecryptNoPadding_IvLengthOtherThanSixteen_Throws(int length)
    {
        Assert.Throws<ArgumentException>(() => Engine.DecryptNoPadding(Key, new byte[length], new byte[16]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(17)]
    public void EncryptNoPadding_IvLengthOtherThanSixteen_Throws(int length)
    {
        Assert.Throws<ArgumentException>(() => Engine.EncryptNoPadding(Key, new byte[length], new byte[16]));
    }

    [Fact]
    public void EncryptNoPadding_PartialBlock_Throws()
    {
        Assert.Throws<ArgumentException>(() => Engine.EncryptNoPadding(Key, Iv, new byte[20]));
    }

    [Theory]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(32)]
    public void EncryptNoPadding_AcceptsEveryAesKeyLength(int keyLength)
    {
        Assert.Equal(16, Engine.EncryptNoPadding(new byte[keyLength], Iv, new byte[16]).Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(20)]
    [InlineData(64)]
    public void EncryptNoPadding_RejectsOtherKeyLengths(int keyLength)
    {
        Assert.Throws<InvalidDataException>(() => Engine.EncryptNoPadding(new byte[keyLength], Iv, new byte[16]));
    }

    [Fact]
    public void EncryptNoPadding_RoundTrips()
    {
        var plain = Convert.FromHexString("00112233445566778899aabbccddeeff0f0e0d0c0b0a09080706050403020100");
        var cipher = Engine.EncryptNoPadding(Key, Iv, plain);
        Assert.Equal(plain, Engine.DecryptNoPadding(Key, Iv, cipher));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(31)]
    public void Pkcs7_PadThenStrip_RoundTrips(int length)
    {
        var data = new byte[length];
        for (var i = 0; i < length; i++)
        {
            data[i] = (byte)(i * 7);
        }

        var padded = Pkcs7.Pad(data);
        Assert.Equal(0, padded.Length % 16);
        Assert.True(padded.Length > data.Length);
        Assert.Equal(data, Pkcs7.Strip(padded));
    }
}
