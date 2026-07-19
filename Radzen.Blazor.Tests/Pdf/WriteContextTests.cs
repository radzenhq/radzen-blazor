#nullable enable
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class WriteContextTests
{
    private const string StreamMarker = "BT /F1 12 Tf 72 720 Td (Context threaded!) Tj ET";
    private const string StringMarker = "Context string value";

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
        page["Contents"] = contentRef;
        page["Marker"] = new StringObject(StringMarker);

        writer.Trailer["Root"] = catalogRef;
        writer.Close();
        return buffer.ToArray();
    }

    private static EncryptionOptions Options()
        => new()
        {
            Algorithm = EncryptionAlgorithm.Aes256,
            UserPassword = "pw",
            Material = new SeededEncryptionMaterial([7]),
        };

    [Fact]
    public void ConcurrentEncryptedWrites_DoNotInterfere()
    {
        var baseline = WriteGraph(Options());

        var results = new byte[64][];
        Parallel.For(0, results.Length, i => results[i] = WriteGraph(Options()));

        Assert.All(results, r => Assert.Equal(baseline, r));
    }

    [Fact]
    public void PlainWrite_IsByteIdentical()
    {
        var first = WriteGraph(null);
        var second = WriteGraph(null);

        Assert.Equal(first, second);
        Assert.DoesNotContain("/Encrypt", Encoding.Latin1.GetString(first), StringComparison.Ordinal);
    }

    [Fact]
    public void EncryptedRoundTrip_RecoversStringAndStream()
    {
        var pdf = WriteGraph(Options());
        var reader = DocumentReader.Parse(pdf, "pw");
        Assert.True(reader.IsEncrypted);

        var root = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(root["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        var page = Assert.IsType<DictionaryObject>(reader.Resolve(kids[0]));

        var content = Assert.IsType<StreamObject>(reader.Resolve(page["Contents"]));
        Assert.Equal(StreamMarker, Encoding.Latin1.GetString(content.Data.ToArray()));

        var marker = Assert.IsType<StringObject>(reader.Resolve(page["Marker"]));
        Assert.Equal(StringMarker, marker.Value);
    }
}
