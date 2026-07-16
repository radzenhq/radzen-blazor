#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Clearing a modeled /Info field and saving incrementally removes the key, matching what
// a full SaveToStream of the same edit emits: the two save paths must not disagree.
public class IncrementalInfoClearTests
{
    private static byte[] BaseDocument()
    {
        var document = new Document();
        document.Info.Title = "Original title";
        document.Info.Author = "Original author";
        document.Pages.Add(PageSizes.A4).SetContent(Encoding.ASCII.GetBytes("BT ET"));
        return document.ToArray();
    }

    // An /Info carrying a key the library does not model alongside a modeled one.
    private static byte[] UnmodeledInfoFixture()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Title (Original title) /Custom (kept) >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 5\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 4; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        return pdf.Append("trailer\n<< /Size 5 /Root 1 0 R /Info 4 0 R >>\nstartxref\n" + xref + "\n%%EOF\n").ToArray();
    }

    private static Document Load(byte[] pdf) => Document.LoadFromStream(new MemoryStream(pdf));

    private static byte[] SaveIncremental(Document document)
    {
        using var stream = new MemoryStream();
        document.SaveIncremental(stream);
        return stream.ToArray();
    }

    private static DictionaryObject Info(byte[] pdf)
    {
        var reader = DocumentReader.Parse(pdf);
        return Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Info"]));
    }

    [Fact]
    public void ClearedTitleIsRemoved_AndUntouchedFieldsSurvive()
    {
        var document = Load(BaseDocument());
        document.Info.Title = null;

        var info = Info(SaveIncremental(document));

        Assert.False(info.ContainsKey("Title"), "/Title must be gone");
        Assert.Equal("Original author", ((StringObject)info["Author"]).Value);
    }

    [Fact]
    public void ClearedTitleReloadsAsNull()
    {
        var document = Load(BaseDocument());
        document.Info.Title = null;

        Assert.Null(Load(SaveIncremental(document)).Info.Title);
    }

    [Fact]
    public void IncrementalAndFullSaveAgreeOnAClearedField()
    {
        var incremental = Load(BaseDocument());
        incremental.Info.Title = null;

        var full = Load(BaseDocument());
        full.Info.Title = null;
        using var stream = new MemoryStream();
        full.SaveToStream(stream);

        Assert.False(Info(SaveIncremental(incremental)).ContainsKey("Title"));
        Assert.False(Info(stream.ToArray()).ContainsKey("Title"));
    }

    // An unmodeled key the library does not model is still carried over untouched.
    [Fact]
    public void ClearingAModeledFieldPreservesUnmodeledKeys()
    {
        var document = Load(UnmodeledInfoFixture());
        document.Info.Title = null;

        var info = Info(SaveIncremental(document));
        Assert.False(info.ContainsKey("Title"), "/Title must be gone");
        Assert.Equal("kept", ((StringObject)info["Custom"]).Value);
    }
}
