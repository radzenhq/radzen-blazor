#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

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
        var sequence = new MaterialSequence(new SeededEncryptionMaterial([42]));
        var options = new EncryptionOptions
        {
            Algorithm = EncryptionAlgorithm.Aes256,
            Material = new SeededEncryptionMaterial([42]),
        };
        var writer = EncryptionWriter.Build(options, new byte[16], sequence, out _);

        var plaintext = Encoding.Latin1.GetBytes(new string('A', 64));
        var first = writer.EncryptStream(plaintext, objectNumber: 3, generation: 0);
        var second = writer.EncryptStream(plaintext, objectNumber: 3, generation: 0);

        Assert.False(first.AsSpan().SequenceEqual(second));
        Assert.False(first.AsSpan(0, 16).SequenceEqual(second.AsSpan(0, 16)));
    }
}
