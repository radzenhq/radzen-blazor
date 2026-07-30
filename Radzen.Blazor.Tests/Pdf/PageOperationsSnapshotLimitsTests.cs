#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class PageOperationsSnapshotLimitsTests
{
    private static byte[] OnePageFile()
    {
        var document = new PortableDocument();
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("BT ET"));
        return document.ToArray();
    }

    [Fact]
    public void Snapshot_ReparsesUnderTheSourceDocumentsLimits()
    {
        var limits = new ReaderLimits { MaxFileBytes = 1_000_000, MaxObjectNestingDepth = 7 };
        using var stream = new MemoryStream(OnePageFile());
        var source = PortableDocument.LoadFromStream(stream, limits);

        var snapshot = PageOperations.Snapshot(source);

        Assert.Equal(1_000_000, snapshot.Loaded!.Source!.Limits.MaxFileBytes);
        Assert.Equal(7, snapshot.Loaded.Source.Limits.MaxObjectNestingDepth);
    }

    [Fact]
    public void Snapshot_OfABuiltDocument_UsesTheDefaultLimits()
    {
        var document = new PortableDocument();
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("BT ET"));

        var snapshot = PageOperations.Snapshot(document);

        Assert.Equal(ReaderLimits.Default.MaxFileBytes, snapshot.Loaded!.Source!.Limits.MaxFileBytes);
    }
}
