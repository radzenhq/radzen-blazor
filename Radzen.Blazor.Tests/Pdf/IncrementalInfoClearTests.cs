#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

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

    [Fact]
    public void IncrementalAndFullSaveAgreeOnEveryModeledField()
    {
        static void Edit(Document document)
        {
            document.Info.Title = "T";
            document.Info.Author = "A";
            document.Info.Subject = "S";
            document.Info.Keywords = "K";
            document.Info.Creator = "C";
            document.Info.Producer = "P";
            document.Info.CreationDate = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));
            document.Info.ModificationDate = new DateTimeOffset(2021, 6, 7, 8, 9, 10, TimeSpan.Zero);
        }

        var incremental = Load(UnmodeledInfoFixture());
        Edit(incremental);

        var full = Load(UnmodeledInfoFixture());
        Edit(full);
        using var stream = new MemoryStream();
        full.SaveToStream(stream);

        var incrementalInfo = Info(SaveIncremental(incremental));
        var fullInfo = Info(stream.ToArray());

        Assert.Equal(fullInfo.Keys, incrementalInfo.Keys);
        foreach (var key in fullInfo.Keys)
        {
            Assert.Equal(((StringObject)fullInfo[key]).Value, ((StringObject)incrementalInfo[key]).Value);
        }

        Assert.Equal("kept", ((StringObject)fullInfo["Custom"]).Value);
        Assert.Equal("D:20200102030405+02'00'", ((StringObject)fullInfo["CreationDate"]).Value);
    }

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
