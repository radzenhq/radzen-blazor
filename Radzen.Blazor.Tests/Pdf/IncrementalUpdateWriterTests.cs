#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 7.5.6: the original bytes stay a byte-for-byte prefix; appended objects chain a new xref section via /Prev.
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

    private static byte[] BuildGenerationOneDocument()
    {
        var bodies = new (int Number, int Generation, string Body)[]
        {
            (1, 0, "<< /Type /Catalog /Pages 2 0 R >>"),
            (2, 0, "<< /Type /Pages /Count 1 /Kids [4 0 R] >>"),
            (3, 1, "<< /Marker (original generation one) >>"),
            (4, 0, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Extra 3 1 R >>"),
        };

        var text = new StringBuilder("%PDF-1.7\n");
        var offsets = new int[bodies.Length + 1];
        foreach (var (number, generation, body) in bodies)
        {
            offsets[number] = text.Length;
            text.Append($"{number} {generation} obj\n{body}\nendobj\n");
        }

        var xref = text.Length;
        text.Append("xref\n0 5\n0000000000 65535 f \n");
        foreach (var (number, generation, _) in bodies)
        {
            text.Append($"{offsets[number]:D10} {generation:D5} n \n");
        }

        text.Append($"trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(text.ToString());
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

    // ISO 32000-1 7.3.10: an indirect reference matches on number AND generation, so an override keeps the object's generation.
    [Fact]
    public void Override_PreservesNonZeroGenerationOfTheOverriddenObject()
    {
        var original = BuildGenerationOneDocument();

        var writer = new IncrementalUpdateWriter(original);
        var reference = writer.Override(3, new DictionaryObject { ["Marker"] = new StringObject("patched") });
        var updated = writer.ToArray();

        AssertPrefix(original, updated);
        Assert.Equal(1, reference.Generation);

        var appended = Encoding.Latin1.GetString(updated, original.Length, updated.Length - original.Length);
        Assert.Contains("3 1 obj", appended, StringComparison.Ordinal);
        Assert.Contains(" 00001 n \n", appended, StringComparison.Ordinal);

        var reader = DocumentReader.Parse(updated);
        var page = (DictionaryObject)reader.Resolve(
            ((ArrayObject)reader.Resolve(((DictionaryObject)reader.Resolve(
                Catalog(reader)["Pages"]))["Kids"]))[0]);
        var extra = Assert.IsType<DictionaryObject>(reader.Resolve(page["Extra"]));
        Assert.Equal("patched", ((StringObject)extra["Marker"]).Value);
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
        second.Trailer["RadzenNew"] = second.Override(reference.ObjectNumber, new StringObject("second"));
        var afterSecond = second.ToArray();

        AssertPrefix(afterFirst, afterSecond);

        var reader = DocumentReader.Parse(afterSecond);
        Assert.Equal("second", ((StringObject)reader.Resolve(reader.Trailer["RadzenNew"])).Value);
        Assert.Equal(FindStartXref(afterFirst), ((NumberObject)reader.Trailer["Prev"]).IntValue);
    }
}
