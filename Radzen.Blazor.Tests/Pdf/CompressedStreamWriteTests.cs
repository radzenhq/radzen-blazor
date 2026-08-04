#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 7.5.7 and 7.5.8: object streams and cross-reference streams
public class CompressedStreamWriteTests
{
    private const string StreamMarker = "BT /F1 12 Tf (compressed marker) Tj ET";
    private const string StringMarker = "plain string marker";
    private const int PageCount = 30;

    private static byte[] WriteGraph(bool compressed, EncryptionOptions? encryption = null)
    {
        using var buffer = new MemoryStream();
        var writer = new DocumentWriter(buffer)
        {
            UseCompressedStreams = compressed,
            Encryption = encryption,
        };

        var catalog = new DictionaryObject { ["Type"] = new NameObject("Catalog") };
        var pages = new DictionaryObject { ["Type"] = new NameObject("Pages"), ["Count"] = new NumberObject(PageCount) };
        var content = new StreamObject(Encoding.Latin1.GetBytes(StreamMarker));

        var catalogRef = writer.Add(catalog);
        var pagesRef = writer.Add(pages);
        var contentRef = writer.Add(content);
        catalog["Pages"] = pagesRef;

        var kids = new ArrayObject();
        for (var i = 0; i < PageCount; i++)
        {
            var page = new DictionaryObject
            {
                ["Type"] = new NameObject("Page"),
                ["Parent"] = pagesRef,
                ["MediaBox"] = new ArrayObject
                {
                    new NumberObject(0), new NumberObject(0), new NumberObject(612), new NumberObject(792),
                },
                ["Contents"] = contentRef,
                ["Marker"] = new StringObject(StringMarker),
            };
            kids.Add(writer.Add(page));
        }

        pages["Kids"] = kids;
        writer.Trailer["Root"] = catalogRef;
        writer.Close();
        return buffer.ToArray();
    }

    private static string Latin1(byte[] bytes) => Encoding.Latin1.GetString(bytes);

    [Fact]
    public void Default_WritesClassicTableAndTrailer()
    {
        var text = Latin1(WriteGraph(compressed: false));

        Carries("classic emission", "\nxref\n0 ", text);
        Carries("classic emission", "trailer\n", text);
        Lacks("classic emission", "/ObjStm", text);
        Lacks("classic emission", "/XRef", text);
    }

    [Fact]
    public void Compressed_WritesXrefStreamInsteadOfTable()
    {
        var text = Latin1(WriteGraph(compressed: true));

        Carries("compressed emission", "/Type /XRef", text);
        Carries("compressed emission", "/Type /ObjStm", text);
        Carries("compressed emission", "startxref\n", text);
        Lacks("compressed emission", "\nxref\n", text);
        Lacks("compressed emission", "trailer\n", text);
    }

    [Fact]
    public void Compressed_PacksDictionariesButKeepsStreamsTopLevel()
    {
        var text = Latin1(WriteGraph(compressed: true));

        Lacks("compressed emission", "\n1 0 obj\n", text);
        Lacks("compressed emission", "\n2 0 obj\n", text);
        Carries("compressed emission", "\n3 0 obj\n", text);
    }

    [Fact]
    public void Compressed_IsSmallerThanClassic()
    {
        var classic = WriteGraph(compressed: false);
        var compressed = WriteGraph(compressed: true);

        Assert.True(compressed.Length < classic.Length,
            $"compressed {compressed.Length} bytes vs classic {classic.Length} bytes");
    }

    [Fact]
    public void Compressed_RoundTripsThroughDocumentReader()
    {
        var reader = DocumentReader.Parse(WriteGraph(compressed: true));

        Assert.Equal("XRef", Assert.IsType<NameObject>(reader.Trailer["Type"]).Value);

        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);

        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        Assert.Equal(PageCount, Assert.IsType<NumberObject>(pages["Count"]).IntValue);

        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        foreach (var kid in kids)
        {
            var page = Assert.IsType<DictionaryObject>(reader.Resolve(kid));
            Assert.Equal("Page", Assert.IsType<NameObject>(page["Type"]).Value);
            Assert.Equal(StringMarker, Assert.IsType<StringObject>(page["Marker"]).Value);

            var content = Assert.IsType<StreamObject>(reader.Resolve(page["Contents"]));
            Assert.Equal(StreamMarker, Latin1(content.Data.ToArray()));
        }
    }

    [Fact]
    public void Compressed_Encrypted_KeepsEncryptDictionaryTopLevelAndRoundTrips()
    {
        var pdf = WriteGraph(compressed: true, new EncryptionOptions { Material = new SeededEncryptionMaterial([7]), Algorithm = EncryptionAlgorithm.Aes128 });
        var text = Latin1(pdf);

        var reader = DocumentReader.Parse(pdf, "");
        Assert.True(reader.IsEncrypted);

        var encrypt = Assert.IsType<ReferenceObject>(reader.Trailer["Encrypt"]);
        Assert.Contains($"\n{encrypt.ObjectNumber} 0 obj\n", text);

        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        var page = Assert.IsType<DictionaryObject>(reader.Resolve(kids[0]));

        Assert.Equal(StringMarker, Assert.IsType<StringObject>(page["Marker"]).Value);
        var content = Assert.IsType<StreamObject>(reader.Resolve(page["Contents"]));
        Assert.Equal(StreamMarker, Latin1(content.Data.ToArray()));
    }
}
