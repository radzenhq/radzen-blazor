#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// ImportPage/ImportPages/Merge/Split re-parse a serialized snapshot of the source. That
// re-parse must run under the limits the caller loaded the source with, or a host that
// configured tight limits against hostile input has them silently replaced by the
// defaults for every imported page (finding #160).
public class PageOperationsSnapshotLimitsTests
{
    private static byte[] OnePageFile()
    {
        var document = new Document();
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("BT ET"));
        return document.ToArray();
    }

    [Fact]
    public void Snapshot_ReparsesUnderTheSourceDocumentsLimits()
    {
        var limits = new ReaderLimits { MaxFileBytes = 1_000_000, MaxObjectNestingDepth = 7 };
        using var stream = new MemoryStream(OnePageFile());
        var source = Document.LoadFromStream(stream, limits);

        var snapshot = PageOperations.Snapshot(source);

        Assert.Equal(1_000_000, snapshot.Loaded!.Source!.Limits.MaxFileBytes);
        Assert.Equal(7, snapshot.Loaded.Source.Limits.MaxObjectNestingDepth);
    }

    [Fact]
    public void Snapshot_OfABuiltDocument_UsesTheDefaultLimits()
    {
        var document = new Document();
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("BT ET"));

        var snapshot = PageOperations.Snapshot(document);

        Assert.Equal(ReaderLimits.Default.MaxFileBytes, snapshot.Loaded!.Source!.Limits.MaxFileBytes);
    }
}
