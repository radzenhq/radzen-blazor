#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class XrefChainEntryBudgetTests
{
    private const int FillerObjectBase = 100;

    [Fact]
    public void ChainedXrefStreams_TotalOverCap_LoaderRefuses()
    {
        var data = ChainedXrefStreamFile(fillerSections: 5, perSection: 4);
        var limits = new ReaderLimits { MaxXrefEntries = 10 };

        var exception = Assert.Throws<DocumentParseException>(() => Load(data, limits));
        Assert.Contains("maximum number of entries", exception.Message);
    }

    [Fact]
    public void ChainedXrefStreams_TotalUnderCap_LoaderBuildsEveryEntry()
    {
        var data = ChainedXrefStreamFile(fillerSections: 5, perSection: 4);

        var loader = Load(data, new ReaderLimits { MaxXrefEntries = 30 });

        Assert.Equal(23, loader.Entries.Count);
        Assert.True(loader.Entries.ContainsKey(1));
        Assert.True(loader.Entries.ContainsKey(FillerObjectBase));
        Assert.True(loader.Entries.ContainsKey(FillerObjectBase + 403));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(12)]
    [InlineData(40)]
    public void ChainedXrefStreams_EntryTableNeverExceedsConfiguredCap(int cap)
    {
        var data = ChainedXrefStreamFile(fillerSections: 20, perSection: 4);
        var limits = new ReaderLimits { MaxXrefEntries = cap };

        XrefLoader? loader = null;
        try
        {
            loader = Load(data, limits);
        }
        catch (DocumentParseException)
        {
        }

        Assert.Null(loader);
    }

    [Fact]
    public void ChainedXrefStreams_OverCap_LoadFromStreamThrows()
    {
        var data = ChainedXrefStreamFile(fillerSections: 5, perSection: 4);

        Assert.Throws<DocumentParseException>(
            () => Document.LoadFromStream(new MemoryStream(data), new ReaderLimits { MaxXrefEntries = 4 }));
    }

    [Fact]
    public void ChainedXrefStreams_UnderCap_LoadFromStreamResolvesEveryPrevSection()
    {
        var data = ChainedXrefStreamFile(fillerSections: 5, perSection: 4);

        var reader = DocumentReader.Parse(data, null, new ReaderLimits { MaxXrefEntries = 30 });

        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
        Assert.Equal("Page", Assert.IsType<NameObject>(
            Assert.IsType<DictionaryObject>(reader.GetObject(3))["Type"]).Value);
    }

    private static XrefLoader Load(byte[] data, ReaderLimits limits)
    {
        var decoder = new StreamDecoder(limits, o => o);
        var repairer = new DocumentRepairer(data, limits);
        var loader = new XrefLoader(data, limits, decoder);
        var store = new IndirectObjectStore(data, limits, loader.Entries, decoder, repairer);
        loader.Load(store);
        return loader;
    }

    private static byte[] ChainedXrefStreamFile(int fillerSections, int perSection)
    {
        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>\nendobj\n");

        var root = pdf.Position;
        var rootPayload = new byte[12];
        Copy(rootPayload, 0, FixturePdf.XrefStreamEntry(1, (int)pdf.OffsetOf(1), 0));
        Copy(rootPayload, 4, FixturePdf.XrefStreamEntry(1, (int)pdf.OffsetOf(2), 0));
        Copy(rootPayload, 8, FixturePdf.XrefStreamEntry(1, (int)pdf.OffsetOf(3), 0));
        pdf.Append("10 0 obj\n<< /Type /XRef /Size 1000 /Index [1 3] /W [1 2 1] /Root 1 0 R /Length 12 >>\nstream\n")
            .Append(rootPayload)
            .Append("\nendstream\nendobj\n");

        var previous = root;
        for (var section = 0; section < fillerSections; section++)
        {
            var offset = pdf.Position;
            var payload = new byte[4 * perSection];
            var start = FillerObjectBase + (section * 100);
            pdf.Append((11 + section) + " 0 obj\n<< /Type /XRef /Size 1000 /Index [" + start + " " + perSection
                    + "] /W [1 2 1] /Root 1 0 R /Prev " + previous + " /Length " + payload.Length + " >>\nstream\n")
                .Append(payload)
                .Append("\nendstream\nendobj\n");
            previous = offset;
        }

        pdf.Append("startxref\n" + previous + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static void Copy(byte[] target, int at, byte[] source)
    {
        for (var i = 0; i < source.Length; i++)
        {
            target[at + i] = source[i];
        }
    }
}
