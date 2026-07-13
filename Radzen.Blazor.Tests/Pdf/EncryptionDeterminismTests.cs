#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// The encryption seam must be fully deterministic: the same options and the same
// caller-supplied material must yield byte-identical encrypted output, and the
// library must generate no randomness of its own (no System.Security.Cryptography).
public class EncryptionDeterminismTests
{
    private const string StreamMarker = "BT /F1 12 Tf 72 720 Td (Deterministic!) Tj ET";
    private const string StringMarker = "Deterministic string value";

    private static byte[] WriteGraph(EncryptionOptions? options)
    {
        using var buffer = new MemoryStream();
        var writer = new DocumentWriter(buffer) { Encryption = options };

        var catalog = new DictionaryObject { ["Type"] = new NameObject("Catalog") };
        var pages = new DictionaryObject { ["Type"] = new NameObject("Pages"), ["Count"] = new NumberObject(1) };
        var page = new DictionaryObject { ["Type"] = new NameObject("Page") };
        var content = new StreamObject(Encoding.Latin1.GetBytes(StreamMarker));

        var catalogRef = writer.Add(catalog);
        var pagesRef = writer.Add(pages);
        var pageRef = writer.Add(page);
        var contentRef = writer.Add(content);

        catalog["Pages"] = pagesRef;
        pages["Kids"] = new ArrayObject { pageRef };
        page["Parent"] = pagesRef;
        page["MediaBox"] = new ArrayObject
        {
            new NumberObject(0), new NumberObject(0), new NumberObject(612), new NumberObject(792),
        };
        page["Contents"] = contentRef;
        page["Marker"] = new StringObject(StringMarker);

        writer.Trailer["Root"] = catalogRef;
        writer.Close();
        return buffer.ToArray();
    }

    private static EncryptionOptions Options(EncryptionAlgorithm algorithm)
        => new()
        {
            Algorithm = algorithm,
            UserPassword = "pw",
            Material = new SeededEncryptionMaterial([1, 2, 3, 4, 5, 6, 7, 8]),
        };

    [Theory]
    [InlineData(EncryptionAlgorithm.Rc4)]
    [InlineData(EncryptionAlgorithm.Aes128)]
    [InlineData(EncryptionAlgorithm.Aes256)]
    public void SameOptionsAndMaterial_ProduceByteIdenticalOutput(EncryptionAlgorithm algorithm)
    {
        var first = WriteGraph(Options(algorithm));
        var second = WriteGraph(Options(algorithm));

        Assert.Equal(first, second);
        Assert.Contains("/Encrypt", Encoding.Latin1.GetString(first), StringComparison.Ordinal);
    }

    [Fact]
    public void SharedMaterialInstance_AcrossTwoWrites_IsByteIdentical()
    {
        var material = new SeededEncryptionMaterial([9, 8, 7, 6]);
        var first = WriteGraph(new EncryptionOptions { Algorithm = EncryptionAlgorithm.Aes256, Material = material });
        var second = WriteGraph(new EncryptionOptions { Algorithm = EncryptionAlgorithm.Aes256, Material = material });

        Assert.Equal(first, second);
    }

    [Fact]
    public void DifferentMaterial_ProducesDifferentOutput()
    {
        var a = WriteGraph(new EncryptionOptions
        {
            Algorithm = EncryptionAlgorithm.Aes256,
            Material = new SeededEncryptionMaterial([1]),
        });
        var b = WriteGraph(new EncryptionOptions
        {
            Algorithm = EncryptionAlgorithm.Aes256,
            Material = new SeededEncryptionMaterial([2]),
        });

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void MissingMaterial_FailsLoud()
    {
        Assert.Throws<InvalidOperationException>(
            () => WriteGraph(new EncryptionOptions { Algorithm = EncryptionAlgorithm.Aes128 }));
    }

    [Fact]
    public void PerStreamIvs_AreUnique()
    {
        // Two AESV3 streams under one file key must get distinct IVs: identical
        // plaintext encrypted with a reused IV yields identical ciphertext and
        // leaks. Drive the real writer path and compare the two encrypted blobs
        // (AESV3 layout is IV(16) || ciphertext) rather than round-tripping,
        // because the reader strips the IV on decrypt.
        var sequence = new MaterialSequence(new SeededEncryptionMaterial([42]));
        var options = new EncryptionOptions
        {
            Algorithm = EncryptionAlgorithm.Aes256,
            Material = new SeededEncryptionMaterial([42]),
        };
        var writer = EncryptionWriter.Build(options, new byte[16], sequence, out _);

        // AESV3 encrypts every object under the file key directly, so the object
        // number does not vary the IV - the uniqueness comes solely from the
        // material sequence advancing one fresh IV per stream. Use one object
        // number so the sequence advance is unmistakably the only difference.
        var plaintext = Encoding.Latin1.GetBytes(new string('A', 64));
        var first = writer.EncryptStream(plaintext, objectNumber: 3, generation: 0);
        var second = writer.EncryptStream(plaintext, objectNumber: 3, generation: 0);

        // Identical plaintext must not produce identical output, and the 16-byte
        // IV prefixes must differ.
        Assert.False(first.AsSpan().SequenceEqual(second));
        Assert.False(first.AsSpan(0, 16).SequenceEqual(second.AsSpan(0, 16)));
    }
}
