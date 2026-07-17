using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

public class RoundTripTests
{
    private static (byte[] Bytes, Dictionary<int, DocumentObject> Originals) WriteEveryType()
    {
        using var ms = new MemoryStream();
        var writer = new DocumentWriter(ms);

        var catalog = new DictionaryObject();
        var pages = new DictionaryObject();
        var stream = new StreamObject(new byte[] { 1, 2, 3, 10, 13, 65 });

        var catalogRef = writer.Add(catalog);
        var pagesRef = writer.Add(pages);
        var streamRef = writer.Add(stream);

        catalog["Type"] = new NameObject("Catalog");
        catalog["Pages"] = pagesRef;
        catalog["Data"] = streamRef;

        pages["Type"] = new NameObject("Pages");
        pages["Flag"] = new BooleanObject(true);
        pages["Nothing"] = new NullObject();
        pages["Title"] = new StringObject("Hello (world)\\end");
        pages["Scale"] = new NumberObject(1.5);
        pages["Neg"] = new NumberObject(-7);

        var box = new ArrayObject();
        box.Add(new NumberObject(0));
        box.Add(new NumberObject(612));
        box.Add(new NumberObject(792));
        box.Add(catalogRef);
        var nested = new DictionaryObject();
        nested["K"] = new NameObject("V");
        box.Add(nested);
        pages["Box"] = box;

        stream.Dictionary["Type"] = new NameObject("Custom");

        writer.Trailer["Root"] = catalogRef;
        writer.Close();

        var originals = new Dictionary<int, DocumentObject>
        {
            [1] = catalog,
            [2] = pages,
            [3] = stream,
        };
        return (ms.ToArray(), originals);
    }

    private static void AssertEqual(DocumentObject expected, DocumentObject actual)
    {
        switch (expected)
        {
            case BooleanObject e:
                Assert.Equal(e.Value, Assert.IsType<BooleanObject>(actual).Value);
                break;
            case NullObject:
                Assert.IsType<NullObject>(actual);
                break;
            case NumberObject e:
                var a = Assert.IsType<NumberObject>(actual);
                Assert.Equal(e.IsInteger, a.IsInteger);
                if (e.IsInteger)
                {
                    Assert.Equal(e.IntValue, a.IntValue);
                }
                else
                {
                    Assert.Equal(e.DoubleValue, a.DoubleValue, 10);
                }

                break;
            case StringObject e:
                Assert.Equal(e.Value, Assert.IsType<StringObject>(actual).Value);
                break;
            case NameObject e:
                Assert.Equal(e.Value, Assert.IsType<NameObject>(actual).Value);
                break;
            case ReferenceObject e:
                var reference = Assert.IsType<ReferenceObject>(actual);
                Assert.Equal(e.ObjectNumber, reference.ObjectNumber);
                Assert.Equal(e.Generation, reference.Generation);
                break;
            case ArrayObject e:
                var array = Assert.IsType<ArrayObject>(actual);
                Assert.Equal(e.Count, array.Count);
                for (var i = 0; i < e.Count; i++)
                {
                    AssertEqual(e[i], array[i]);
                }

                break;
            case StreamObject e:
                var streamActual = Assert.IsType<StreamObject>(actual);
                Assert.Equal(e.Data, streamActual.Data);
                foreach (var key in e.Dictionary.Keys)
                {
                    Assert.True(streamActual.Dictionary.ContainsKey(key));
                    AssertEqual(e.Dictionary[key], streamActual.Dictionary[key]);
                }

                break;
            case DictionaryObject e:
                var dict = Assert.IsType<DictionaryObject>(actual);
                Assert.Equal(e.Keys, dict.Keys);
                foreach (var key in e.Keys)
                {
                    AssertEqual(e[key], dict[key]);
                }

                break;
            default:
                Assert.True(false, "Unexpected object type " + expected.GetType().Name);
                break;
        }
    }

    [Fact]
    public void EveryObjectType_RoundTrips()
    {
        var (bytes, originals) = WriteEveryType();
        var reader = DocumentReader.Parse(bytes);

        Assert.Equal(3, reader.ObjectCount);
        foreach (var pair in originals)
        {
            AssertEqual(pair.Value, reader.GetObject(pair.Key));
        }
    }

    [Fact]
    public void RoundTrip_TrailerRootResolvesToCatalog()
    {
        var (bytes, _) = WriteEveryType();
        var reader = DocumentReader.Parse(bytes);

        var root = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(root["Type"]).Value);
    }

    [Fact]
    public void RoundTrip_StreamDataPreservedBytewise()
    {
        var (bytes, _) = WriteEveryType();
        var reader = DocumentReader.Parse(bytes);

        var stream = Assert.IsType<StreamObject>(reader.GetObject(3));
        Assert.Equal(new byte[] { 1, 2, 3, 10, 13, 65 }, stream.Data);
    }
}
