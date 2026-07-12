#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Incremental-update contract (ISO 32000-1 7.5.6): the original bytes stay a
// byte-for-byte prefix of the output, added/overridden objects are appended
// after the original end-of-file, and a new cross-reference section (matching
// the original file's style) chains to the previous one via /Prev. This is the
// foundation digital signatures build on: a signature covers a byte range of
// the file, so nothing before the appended section may move.
public class IncrementalUpdateWriterTests
{
    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    private static byte[] BuildClassicDocument()
    {
        var document = new Document();
        document.Info.Title = "Incremental";
        document.Pages.Add(PageSizes.A4).SetContent(Ascii("BT (page zero) Tj ET"));
        document.Pages.Add(PageSizes.Letter).SetContent(Ascii("BT (page one) Tj ET"));
        return document.ToArray();
    }

    private static byte[] BuildXrefStreamDocument()
    {
        using var buffer = new MemoryStream();
        var writer = new DocumentWriter(buffer) { UseCompressedStreams = true };

        var catalog = new DictionaryObject { ["Type"] = new NameObject("Catalog") };
        var pages = new DictionaryObject { ["Type"] = new NameObject("Pages"), ["Count"] = new NumberObject(1) };
        var content = new StreamObject(Ascii("BT (compressed page) Tj ET"));

        var catalogRef = writer.Add(catalog);
        var pagesRef = writer.Add(pages);
        var contentRef = writer.Add(content);
        catalog["Pages"] = pagesRef;

        var page = new DictionaryObject
        {
            ["Type"] = new NameObject("Page"),
            ["Parent"] = pagesRef,
            ["MediaBox"] = new ArrayObject
            {
                new NumberObject(0), new NumberObject(0), new NumberObject(612), new NumberObject(792),
            },
            ["Contents"] = contentRef,
        };
        pages["Kids"] = new ArrayObject { writer.Add(page) };

        writer.Trailer["Root"] = catalogRef;
        var id = new StringObject("0123456789abcdef");
        writer.Trailer["ID"] = new ArrayObject { id, id };
        writer.Close();
        return buffer.ToArray();
    }

    private static long FindStartXref(byte[] data)
    {
        var text = Encoding.Latin1.GetString(data);
        var index = text.LastIndexOf("startxref", StringComparison.Ordinal);
        Assert.True(index >= 0);
        var rest = text[(index + "startxref".Length)..].TrimStart('\r', '\n', ' ');
        var end = 0;
        while (end < rest.Length && char.IsAsciiDigit(rest[end]))
        {
            end++;
        }

        return long.Parse(rest[..end]);
    }

    private static void AssertPrefix(byte[] original, byte[] updated)
    {
        Assert.True(updated.Length > original.Length);
        Assert.True(updated.AsSpan(0, original.Length).SequenceEqual(original));
    }

    private static int RootNumber(DocumentReader reader)
        => ((ReferenceObject)reader.Trailer["Root"]).ObjectNumber;

    private static DictionaryObject Catalog(DocumentReader reader)
        => (DictionaryObject)reader.Resolve(reader.Trailer["Root"]);

    [Fact]
    public void Add_NewObjectResolvesAndOriginalStaysIntact()
    {
        var original = BuildClassicDocument();
        var originalReader = DocumentReader.Parse(original);
        var originalSize = ((NumberObject)originalReader.Trailer["Size"]).IntValue;

        var writer = new IncrementalUpdateWriter(original);
        var added = writer.Add(new DictionaryObject
        {
            ["Type"] = new NameObject("RadzenTest"),
            ["Marker"] = new StringObject("incremental marker"),
        });
        writer.Trailer["RadzenNew"] = added;
        var updated = writer.ToArray();

        AssertPrefix(original, updated);

        var reader = DocumentReader.Parse(updated);

        var newObject = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["RadzenNew"]));
        Assert.Equal("incremental marker", ((StringObject)newObject["Marker"]).Value);

        var catalog = Catalog(reader);
        var pagesDict = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pagesDict["Kids"]));
        Assert.Equal(2, kids.Count);

        Assert.Equal(added.ObjectNumber, originalSize);
        Assert.Equal(originalSize + 1, ((NumberObject)reader.Trailer["Size"]).IntValue);
        Assert.Equal(FindStartXref(original), ((NumberObject)reader.Trailer["Prev"]).IntValue);
    }

    [Fact]
    public void Add_PreservesOriginalTrailerId()
    {
        var original = BuildXrefStreamDocument();
        var originalReader = DocumentReader.Parse(original);
        var originalId = (ArrayObject)originalReader.Trailer["ID"];

        var writer = new IncrementalUpdateWriter(original);
        writer.Trailer["RadzenNew"] = writer.Add(new NumberObject(42));
        var reader = DocumentReader.Parse(writer.ToArray());

        var id = Assert.IsType<ArrayObject>(reader.Trailer["ID"]);
        Assert.Equal(((StringObject)originalId[0]).Value, ((StringObject)id[0]).Value);
        Assert.Equal(((StringObject)originalId[1]).Value, ((StringObject)id[1]).Value);
    }

    [Fact]
    public void Override_ReaderResolvesNewVersionOfCatalog()
    {
        var original = BuildClassicDocument();
        var originalReader = DocumentReader.Parse(original);
        var rootNumber = RootNumber(originalReader);

        var patched = new DictionaryObject();
        foreach (var pair in Catalog(originalReader))
        {
            patched[pair.Key] = pair.Value;
        }

        patched["Radzen"] = new StringObject("patched");

        var writer = new IncrementalUpdateWriter(original, originalReader);
        var reference = writer.Override(rootNumber, patched);
        var updated = writer.ToArray();

        AssertPrefix(original, updated);
        Assert.Equal(rootNumber, reference.ObjectNumber);
        Assert.Equal(0, reference.Generation);

        var reader = DocumentReader.Parse(updated);
        Assert.Equal(rootNumber, RootNumber(reader));

        var catalog = Catalog(reader);
        Assert.Equal("patched", ((StringObject)catalog["Radzen"]).Value);
        Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));

        Assert.Equal(((NumberObject)originalReader.Trailer["Size"]).IntValue,
            ((NumberObject)reader.Trailer["Size"]).IntValue);
    }

    [Fact]
    public void Override_RejectsNumbersOutsideTheOriginalChain()
    {
        var writer = new IncrementalUpdateWriter(BuildClassicDocument());

        Assert.Throws<ArgumentOutOfRangeException>(() => writer.Override(0, new NumberObject(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.Override(100000, new NumberObject(1)));
    }

    [Fact]
    public void Write_WithoutObjectsThrows()
    {
        var writer = new IncrementalUpdateWriter(BuildClassicDocument());

        Assert.Throws<InvalidOperationException>(() => writer.ToArray());
    }

    [Fact]
    public void Write_ClassicSource_EmitsClassicXrefTable()
    {
        var original = BuildClassicDocument();
        var writer = new IncrementalUpdateWriter(original);
        writer.Trailer["RadzenNew"] = writer.Add(new NumberObject(7));
        var updated = writer.ToArray();

        var appended = Encoding.Latin1.GetString(updated, original.Length, updated.Length - original.Length);
        Assert.Contains("xref\n", appended);
        Assert.Contains("trailer\n", appended);
        Assert.DoesNotContain("/Type /XRef", appended);
    }

    [Fact]
    public void Write_XrefStreamSource_EmitsXrefStream()
    {
        var original = BuildXrefStreamDocument();
        var originalReader = DocumentReader.Parse(original);
        var originalSize = ((NumberObject)originalReader.Trailer["Size"]).IntValue;

        var writer = new IncrementalUpdateWriter(original);
        var added = writer.Add(new DictionaryObject { ["Marker"] = new StringObject("stream update") });
        writer.Trailer["RadzenNew"] = added;
        var updated = writer.ToArray();

        AssertPrefix(original, updated);

        var appended = Encoding.Latin1.GetString(updated, original.Length, updated.Length - original.Length);
        Assert.Contains("/Type /XRef", appended);
        Assert.DoesNotContain("trailer", appended);

        var reader = DocumentReader.Parse(updated);
        var newObject = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["RadzenNew"]));
        Assert.Equal("stream update", ((StringObject)newObject["Marker"]).Value);
        Assert.IsType<DictionaryObject>(Catalog(reader));

        // The appended xref stream itself occupies one more object number.
        Assert.Equal(originalSize + 2, ((NumberObject)reader.Trailer["Size"]).IntValue);
        Assert.Equal(FindStartXref(original), ((NumberObject)reader.Trailer["Prev"]).IntValue);
    }

    [Fact]
    public void Write_XrefStreamSource_OverrideResolvesNewVersion()
    {
        var original = BuildXrefStreamDocument();
        var originalReader = DocumentReader.Parse(original);
        var rootNumber = RootNumber(originalReader);

        var patched = new DictionaryObject();
        foreach (var pair in Catalog(originalReader))
        {
            patched[pair.Key] = pair.Value;
        }

        patched["Radzen"] = new StringObject("patched stream");

        var writer = new IncrementalUpdateWriter(original, originalReader);
        writer.Override(rootNumber, patched);
        var updated = writer.ToArray();

        AssertPrefix(original, updated);
        var reader = DocumentReader.Parse(updated);
        Assert.Equal("patched stream", ((StringObject)Catalog(reader)["Radzen"]).Value);
    }

    [Fact]
    public void Write_SameInputsProduceIdenticalBytes()
    {
        var original = BuildClassicDocument();

        byte[] Build()
        {
            var writer = new IncrementalUpdateWriter(original);
            writer.Trailer["RadzenNew"] = writer.Add(new DictionaryObject
            {
                ["Marker"] = new StringObject("deterministic"),
            });
            writer.Override(RootNumber(DocumentReader.Parse(original)), new DictionaryObject
            {
                ["Type"] = new NameObject("Catalog"),
            });
            return writer.ToArray();
        }

        Assert.Equal(Build(), Build());
    }

    [Fact]
    public void Write_ChainedUpdatesResolveNewestVersion()
    {
        var original = BuildClassicDocument();

        var first = new IncrementalUpdateWriter(original);
        first.Trailer["RadzenNew"] = first.Add(new StringObject("first"));
        var afterFirst = first.ToArray();

        var second = new IncrementalUpdateWriter(afterFirst);
        var reference = (ReferenceObject)DocumentReader.Parse(afterFirst).Trailer["RadzenNew"];
        // Custom trailer keys are not inherited across updates; each trailer restates them.
        second.Trailer["RadzenNew"] = second.Override(reference.ObjectNumber, new StringObject("second"));
        var afterSecond = second.ToArray();

        AssertPrefix(afterFirst, afterSecond);

        var reader = DocumentReader.Parse(afterSecond);
        Assert.Equal("second", ((StringObject)reader.Resolve(reader.Trailer["RadzenNew"])).Value);
        Assert.Equal(FindStartXref(afterFirst), ((NumberObject)reader.Trailer["Prev"]).IntValue);
    }
}
