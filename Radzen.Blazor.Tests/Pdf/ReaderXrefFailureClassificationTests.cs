#nullable enable
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ReaderXrefFailureClassificationTests
{
    [Fact]
    public void TruncatedClassicXrefEntryThrowsDocumentParseException()
    {
        var data = Encoding.ASCII.GetBytes("startxref\n19\n%%EOF\nxref\n0 1\n0000000000 65535 ");
        var limits = ReaderLimits.Default;
        var decoder = new StreamDecoder(limits, value => value);
        var loader = new XrefLoader(data, limits, decoder);
        var store = new IndirectObjectStore(
            data, limits, loader.Entries, decoder, new DocumentRepairer(data, limits));

        Assert.Throws<DocumentParseException>(() => loader.Load(store));
    }
}
