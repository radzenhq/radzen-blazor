#nullable enable
using System;
using System.Linq;
using System.Text;
using Radzen.Documents.Crypto;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class CryptoPrimitiveTests
{
    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    // RFC 1321 MD5 test vectors
    [Theory]
    [InlineData("", "d41d8cd98f00b204e9800998ecf8427e")]
    [InlineData("a", "0cc175b9c0f1b6a831c399e269772661")]
    [InlineData("abc", "900150983cd24fb0d6963f7d28e17f72")]
    [InlineData("message digest", "f96b697d7cb7938d525a2f31aaf161d0")]
    public void Md5_KnownStrings(string input, string expected)
    {
        Assert.Equal(expected, Hex(Md5.ComputeHash(Ascii(input))));
    }

    [Theory]
    [InlineData(55, "ef1772b6dff9a122358552954ad0df65")]
    [InlineData(56, "3b0c8ac703f828b04c6c197006d17218")]
    [InlineData(64, "014842d480b571495a4a0363793f7367")]
    public void Md5_BlockBoundaries(int count, string expected)
    {
        Assert.Equal(expected, Hex(Md5.ComputeHash(Ascii(new string('a', count)))));
    }

    [Fact]
    public void Md5_LongInput()
    {
        var input = Ascii(string.Concat(Enumerable.Repeat("1234567890", 8)));
        Assert.Equal(80, input.Length);
        Assert.Equal("57edf4a22be3c955ac49da2e2107b67a", Hex(Md5.ComputeHash(input)));
    }

    [Fact]
    public void Md5_EmptyArray()
    {
        Assert.Equal("d41d8cd98f00b204e9800998ecf8427e", Hex(Md5.ComputeHash([])));
    }

    // RFC 3174 section 7.3 and NIST SHA-1 short message vectors
    [Theory]
    [InlineData("", "da39a3ee5e6b4b0d3255bfef95601890afd80709")]
    [InlineData("a", "86f7e437faa5a7fce15d1ddcb9eaeaea377667b8")]
    [InlineData("abc", "a9993e364706816aba3e25717850c26c9cd0d89d")]
    [InlineData("message digest", "c12252ceda8be8994d5fa0290a47231c1d16aae3")]
    [InlineData(
        "abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq",
        "84983e441c3bd26ebaae4aa1f95129e5e54670f1")]
    public void Sha1_KnownStrings(string input, string expected)
    {
        Assert.Equal(expected, Hex(Sha1.ComputeHash(Ascii(input))));
    }

    [Theory]
    [InlineData(55, "c1c8bbdc22796e28c0e15163d20899b65621d65a")]
    [InlineData(56, "c2db330f6083854c99d4b5bfb6e8f29f201be699")]
    [InlineData(64, "0098ba824b5c16427bd7a1122a5a442a25ec644d")]
    public void Sha1_BlockBoundaries(int count, string expected)
    {
        Assert.Equal(expected, Hex(Sha1.ComputeHash(Ascii(new string('a', count)))));
    }

    [Fact]
    public void Sha1_LongInput()
    {
        var input = Ascii(string.Concat(Enumerable.Repeat("1234567890", 8)));
        Assert.Equal(80, input.Length);
        Assert.Equal("50abf5706a150990a08b2c5ea40fa0e585554732", Hex(Sha1.ComputeHash(input)));
    }

    // RFC 3174 section 7.3 TEST4
    [Fact]
    public void Sha1_MultiBlockRepetition()
    {
        var input = Ascii(string.Concat(
            Enumerable.Repeat("0123456701234567012345670123456701234567012345670123456701234567", 10)));

        Assert.Equal(640, input.Length);
        Assert.Equal("dea356a2cddd90c7a7ecedc5ebb563934f460452", Hex(Sha1.ComputeHash(input)));
    }

    [Fact]
    public void Sha1_EmptyArray()
    {
        Assert.Equal("da39a3ee5e6b4b0d3255bfef95601890afd80709", Hex(Sha1.ComputeHash([])));
    }

    [Theory]
    [InlineData("Key", "Plaintext", "bbf316e8d940af0ad3")]
    [InlineData("Wiki", "pedia", "1021bf0420")]
    [InlineData("Secret", "Attack at dawn", "45a01f645fc35b383552544b9bf5")]
    public void Rc4_EncryptVectors(string key, string plaintext, string expected)
    {
        Assert.Equal(expected, Hex(Rc4.Transform(Ascii(key), Ascii(plaintext))));
    }

    [Fact]
    public void Rc4_RoundTrip()
    {
        var key = Ascii("Secret");
        var cipher = Rc4.Transform(key, Ascii("Attack at dawn"));
        Assert.Equal("Attack at dawn", Encoding.ASCII.GetString(Rc4.Transform(key, cipher)));
    }

    [Fact]
    public void AesCbc_128_DecryptRemovesPadding()
    {
        var key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
        var ivCipher = Convert.FromHexString(
            "101112131415161718191a1b1c1d1e1f" +
            "93e11dc27929233d2a1f758a15282c29869bf5aee56a855c9819b1805cbf4aa225f08de943505b327bee843729283261");
        var plain = AesCbc.Decrypt(key, ivCipher);
        Assert.Equal("Hello AES-CBC PDF decrypt test!!", Encoding.ASCII.GetString(plain));
    }

    [Fact]
    public void AesCbc_256_DecryptRemovesPadding()
    {
        var key = Convert.FromHexString("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f");
        var ivCipher = Convert.FromHexString(
            "101112131415161718191a1b1c1d1e1f" +
            "262a223698169dbd4bc569d22c5a01164e82ba0f046e7cc428217a15d40f9f7e3c73b2112e0357819a89edf1d91c0936");
        var plain = AesCbc.Decrypt(key, ivCipher);
        Assert.Equal("Hello AES-CBC PDF decrypt test!!", Encoding.ASCII.GetString(plain));
    }

    // FIPS-197 AES-128 KAT: zero IV, first cipher block = 3ad77bb40d7a3660a89ecaf32466ef97
    [Fact]
    public void AesCbc_128_NistAnchoredSingleBlock()
    {
        var key = Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");
        var ivCipher = Convert.FromHexString(
            "00000000000000000000000000000000" +
            "3ad77bb40d7a3660a89ecaf32466ef974b0673d23da20679744afa8e3d589236");
        var plain = AesCbc.Decrypt(key, ivCipher);
        Assert.Equal("6bc1bee22e409f96e93d7e117393172a", Hex(plain));
    }

    [Fact]
    public void Rc4_EmptyKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => Rc4.Transform([], Ascii("Attack at dawn")));
    }
}
