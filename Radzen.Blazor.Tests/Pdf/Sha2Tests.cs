#nullable enable
using System;
using System.Text;
using Radzen.Documents.Crypto;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Known-answer tests for the hand-rolled SHA-2 primitives that back the PDF/A
// document /ID and the ISO 32000-2 revision 6 (AESV3) key derivation. Vectors are
// the FIPS 180-4 / NIST CAVP published digests, verified with python3 hashlib.
//   internal static byte[] Sha2.Sha256(byte[] data)
//   internal static byte[] Sha2.Sha384(byte[] data)
//   internal static byte[] Sha2.Sha512(byte[] data)
public class Sha2Tests
{
    private static string Hex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();

    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    [Theory]
    [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [InlineData("abc", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    [InlineData(
        "abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq",
        "248d6a61d20638b8e5c026930c3e6039a33ce45964ff2167f6ecedd419db06c1")]
    public void Sha256_KnownStrings(string input, string expected)
    {
        Assert.Equal(expected, Hex(Sha2.Sha256(Ascii(input))));
    }

    // 55 fits with the length word in one block; 56 forces a second block; 64 is an
    // exact block fill (all three exercise the 512-bit padding boundary).
    [Theory]
    [InlineData(55, "9f4390f8d30c2dd92ec9f095b65e2b9ae9b0a925a5258e241c9f1e910f734318")]
    [InlineData(56, "b35439a4ac6f0948b6d6f9e3c6af0f5f590ce20f1bde7090ef7970686ec6738a")]
    [InlineData(64, "ffe054fe7ae0cb6dc65c3af9b61d5209f439851db43d0ba5997337df154668eb")]
    public void Sha256_BlockBoundaries(int count, string expected)
    {
        Assert.Equal(expected, Hex(Sha2.Sha256(Ascii(new string('a', count)))));
    }

    [Fact]
    public void Sha256_LongMultiBlock()
    {
        var input = Ascii(new string('a', 1000000));
        Assert.Equal("cdc76e5c9914fb9281a1c7e284d73e67f1809a48a497200e046d39ccc7112cd0", Hex(Sha2.Sha256(input)));
    }

    [Theory]
    [InlineData(
        "",
        "38b060a751ac96384cd9327eb1b1e36a21fdb71114be07434c0cc7bf63f6e1da274edebfe76f65fbd51ad2f14898b95b")]
    [InlineData(
        "abc",
        "cb00753f45a35e8bb5a03d699ac65007272c32ab0eded1631a8b605a43ff5bed8086072ba1e7cc2358baeca134c825a7")]
    [InlineData(
        "abcdefghbcdefghicdefghijdefghijkefghijklfghijklmghijklmnhijklmnoijklmnopjklmnopqklmnopqrlmnopqrsmnopqrstnopqrstu",
        "09330c33f71147e83d192fc782cd1b4753111b173b3b05d22fa08086e3b0f712fcc7c71a557e2db966c3e9fa91746039")]
    public void Sha384_KnownStrings(string input, string expected)
    {
        Assert.Equal(expected, Hex(Sha2.Sha384(Ascii(input))));
    }

    [Theory]
    [InlineData(
        "",
        "cf83e1357eefb8bdf1542850d66d8007d620e4050b5715dc83f4a921d36ce9ce47d0d13c5d85f2b0ff8318d2877eec2f63b931bd47417a81a538327af927da3e")]
    [InlineData(
        "abc",
        "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f")]
    [InlineData(
        "abcdefghbcdefghicdefghijdefghijkefghijklfghijklmghijklmnhijklmnoijklmnopjklmnopqklmnopqrlmnopqrsmnopqrstnopqrstu",
        "8e959b75dae313da8cf4f72814fc143f8f7779c6eb9f7fa17299aeadb6889018501d289e4900f7e4331b99dec4b5433ac7d329eeb6dd26545e96e55b874be909")]
    public void Sha512_KnownStrings(string input, string expected)
    {
        Assert.Equal(expected, Hex(Sha2.Sha512(Ascii(input))));
    }

    // SHA-384/512 pad to a 1024-bit block with a 128-bit length: 111 keeps a single
    // block, 112 forces a second, 128 is an exact block fill.
    [Theory]
    [InlineData(111, "3c37955051cb5c3026f94d551d5b5e2ac38d572ae4e07172085fed81f8466b8f90dc23a8ffcdea0b8d8e58e8fdacc80a")]
    [InlineData(112, "187d4e07cb306103c69967bf544d0dfbe9042577599c73c330abc0cb64c61236d5ed565ee19119d8c31779a38f791fcd")]
    [InlineData(128, "edb12730a366098b3b2beac75a3bef1b0969b15c48e2163c23d96994f8d1bef760c7e27f3c464d3829f56c0d53808b0b")]
    public void Sha384_BlockBoundaries(int count, string expected)
    {
        Assert.Equal(expected, Hex(Sha2.Sha384(Ascii(new string('a', count)))));
    }

    [Theory]
    [InlineData(
        111,
        "fa9121c7b32b9e01733d034cfc78cbf67f926c7ed83e82200ef86818196921760b4beff48404df811b953828274461673c68d04e297b0eb7b2b4d60fc6b566a2")]
    [InlineData(
        112,
        "c01d080efd492776a1c43bd23dd99d0a2e626d481e16782e75d54c2503b5dc32bd05f0f1ba33e568b88fd2d970929b719ecbb152f58f130a407c8830604b70ca")]
    [InlineData(
        128,
        "b73d1929aa615934e61a871596b3f3b33359f42b8175602e89f7e06e5f658a243667807ed300314b95cacdd579f3e33abdfbe351909519a846d465c59582f321")]
    public void Sha512_BlockBoundaries(int count, string expected)
    {
        Assert.Equal(expected, Hex(Sha2.Sha512(Ascii(new string('a', count)))));
    }
}
