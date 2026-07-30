#nullable enable

using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class InfoUnmodeledEntryTests
{
    private static PortableDocument Load(byte[] bytes) => PortableDocument.LoadFromStream(new MemoryStream(bytes));

    private static DictionaryObject SavedInfo(byte[] saved)
    {
        var reader = DocumentReader.Parse(saved);
        return Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Info"]));
    }

    [Fact]
    public void FullSave_PreservesTrappedAndCustomInfoEntries()
    {
        var loaded = Load(BuildPdf());

        var info = SavedInfo(loaded.ToArray());

        Assert.Equal("True", Assert.IsType<NameObject>(info["Trapped"]).Value);
        Assert.Equal("cost-centre-7", Assert.IsType<StringObject>(info["RadzenCustom"]).Value);
    }

    [Fact]
    public void FullSave_PreservesUnmodeledInfoEntriesWhenModeledFieldEdited()
    {
        var loaded = Load(BuildPdf());
        loaded.Info.Title = "Edited";

        var info = SavedInfo(loaded.ToArray());

        Assert.Equal("Edited", Assert.IsType<StringObject>(info["Title"]).Value);
        Assert.Equal("True", Assert.IsType<NameObject>(info["Trapped"]).Value);
        Assert.Equal("cost-centre-7", Assert.IsType<StringObject>(info["RadzenCustom"]).Value);
    }

    [Fact]
    public void FullSave_ClearedModeledFieldIsDroppedButUnmodeledEntriesSurvive()
    {
        var loaded = Load(BuildPdf());
        loaded.Info.Title = null;

        var info = SavedInfo(loaded.ToArray());

        Assert.False(info.ContainsKey("Title"));
        Assert.Equal("True", Assert.IsType<NameObject>(info["Trapped"]).Value);
    }

    [Fact]
    public void FullSave_UnmodeledInfoEntriesSurviveAReload()
    {
        var loaded = Load(Load(BuildPdf()).ToArray());

        var info = SavedInfo(loaded.ToArray());

        Assert.Equal("True", Assert.IsType<NameObject>(info["Trapped"]).Value);
        Assert.Equal("cost-centre-7", Assert.IsType<StringObject>(info["RadzenCustom"]).Value);
    }

    [Fact]
    public void AuthoredDocument_WritesNoUnmodeledInfoEntries()
    {
        var document = new PortableDocument();
        document.Pages.Add();
        document.Info.Title = "Fresh";

        var info = SavedInfo(document.ToArray());

        Assert.Equal("Fresh", Assert.IsType<StringObject>(info["Title"]).Value);
        Assert.False(info.ContainsKey("Trapped"));
    }

    private static byte[] BuildPdf()
    {
        var content = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (page-body) Tj ET");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
            + "/Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + content.Length + " >>\nstream\n").Append(content).Append("\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");
        pdf.Object(6, "6 0 obj\n<< /Title (Original) /Author (Radzen) /Trapped /True "
            + "/RadzenCustom (cost-centre-7) >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 7\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 7; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 7 /Root 1 0 R /Info 6 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }
}
