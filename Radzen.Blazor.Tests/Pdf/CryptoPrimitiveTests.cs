#nullable enable
using System;
using System.Linq;
using System.Text;
using Radzen.Documents.Crypto;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Known-answer tests for the hand-rolled crypto primitives used by the PDF
// standard security handler. Pins:
//   public static byte[] Md5.ComputeHash(byte[] data)
//   internal static byte[] Rc4.Transform(byte[] key, byte[] data)
//   internal static byte[] AesCbc.Decrypt(byte[] key, byte[] data)  // data = IV(16) || ciphertext, PKCS7 stripped
public class CryptoPrimitiveTests
{
    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    // MD5 vectors verified with python3 hashlib (RFC 1321 suite plus block boundaries).
    [Theory]
    [InlineData("", "d41d8cd98f00b204e9800998ecf8427e")]
    [InlineData("a", "0cc175b9c0f1b6a831c399e269772661")]
    [InlineData("abc", "900150983cd24fb0d6963f7d28e17f72")]
    [InlineData("message digest", "f96b697d7cb7938d525a2f31aaf161d0")]
    public void Md5_KnownStrings(string input, string expected)
    {
        Assert.Equal(expected, Hex(Md5.ComputeHash(Ascii(input))));
    }

    // Block-boundary inputs (55/56 force one vs two MD5 blocks; 64 exercises exact block fill).
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

    [Fact]
    public void HashHexApis_PreserveTheirEstablishedCasing()
    {
        // "abc" spans all 16 nibble values; "a" starts with 0x0c, pinning the leading zero.
        Assert.Equal("900150983cd24fb0d6963f7d28e17f72", Md5.ComputeHashHex(Ascii("abc")));
        Assert.Equal("0cc175b9c0f1b6a831c399e269772661", Md5.ComputeHashHex(Ascii("a")));
        Assert.Equal(32, Md5.ComputeHashHex(Ascii("")).Length);
        Assert.Equal("A9993E364706816ABA3E25717850C26C9CD0D89D", Sha1.ComputeHashHex(Ascii("abc")));
        Assert.Equal(Convert.FromHexString("A9993E364706816ABA3E25717850C26C9CD0D89D"), Sha1.ComputeHash(Ascii("abc")));
    }

    // RC4 is symmetric; Transform decrypts and encrypts. Classic vectors verified
    // with a hand-rolled python RC4.
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

    // AES-128-CBC. key/iv/plaintext fixed; ciphertext produced by openssl enc -aes-128-cbc (PKCS7).
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

    // AES-256-CBC, same plaintext, openssl enc -aes-256-cbc.
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

    // NIST-anchored: FIPS-197 AES-128 key with a zero IV, so the first cipher block
    // equals the published KAT 3ad77bb40d7a3660a89ecaf32466ef97. openssl PKCS7-padded
    // a single 16-byte plaintext block; Decrypt recovers it after stripping the pad block.
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
}
