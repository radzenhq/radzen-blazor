#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class RoundTripFidelityLaneTests
{
    private static PortableDocument Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return PortableDocument.LoadFromStream(stream);
    }

    private static DocumentReader SaveAndParse(PortableDocument document)
        => DocumentReader.Parse(document.ToArray());

    private static DictionaryObject Catalog(DocumentReader reader)
        => Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]!));

    private static string Name(DocumentReader reader, DictionaryObject dictionary, string key)
        => Assert.IsType<NameObject>(reader.Resolve(dictionary[key])).Value;


    private static byte[] BuildWithInfo()
    {
        var content = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (body) Tj ET");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + content.Length + " >>\nstream\n").Append(content).Append("\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Producer (Acme Producer) /CreationDate (D:20200102030405Z) /ModDate (D:20210304050607+02'00') >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 6; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 6 /Root 1 0 R /Info 5 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void InfoProducerAndDates_SurviveLoadSave()
    {
        var reader = SaveAndParse(Load(BuildWithInfo()));

        Assert.True(reader.Trailer.TryGetValue("Info", out var infoObject));
        var info = Assert.IsType<DictionaryObject>(reader.Resolve(infoObject!));
        Assert.Equal("Acme Producer", Assert.IsType<StringObject>(reader.Resolve(info["Producer"])).Value);

        var created = Assert.IsType<StringObject>(reader.Resolve(info["CreationDate"])).Value;
        Assert.StartsWith("D:20200102030405", created, StringComparison.Ordinal);
        var modified = Assert.IsType<StringObject>(reader.Resolve(info["ModDate"])).Value;
        Assert.StartsWith("D:20210304050607", modified, StringComparison.Ordinal);
    }

    [Fact]
    public void InfoProducer_CallerOverrideWins()
    {
        var document = Load(BuildWithInfo());
        document.Info.Producer = "Overridden";

        var reader = SaveAndParse(document);
        var info = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Info"]!));
        Assert.Equal("Overridden", Assert.IsType<StringObject>(reader.Resolve(info["Producer"])).Value);
    }


    private static byte[] BuildWithEmbeddedFile()
    {
        var xml = Encoding.UTF8.GetBytes("<invoice/>");
        var content = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (body) Tj ET");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /Names 6 0 R /AF [7 0 R] >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + content.Length + " >>\nstream\n").Append(content).Append("\nendstream\nendobj\n");
        pdf.Mark(5);
        pdf.Append("5 0 obj\n<< /Type /EmbeddedFile /Subtype /text#2Fxml /Length " + xml.Length
            + " /Params << /Size " + xml.Length + " /ModDate (D:20200101000000Z) >> >>\nstream\n")
            .Append(xml).Append("\nendstream\nendobj\n");
        pdf.Object(6, "6 0 obj\n<< /EmbeddedFiles << /Names [(factur-x.xml) 7 0 R] >> >>\nendobj\n");
        pdf.Object(7, "7 0 obj\n<< /Type /Filespec /F (factur-x.xml) /UF (factur-x.xml) /AFRelationship /Data /Desc (invoice) /EF << /F 5 0 R /UF 5 0 R >> >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 8\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 8; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 8 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void EmbeddedFileAndAF_SurviveLoadSave()
    {
        var reader = SaveAndParse(Load(BuildWithEmbeddedFile()));
        var catalog = Catalog(reader);

        Assert.True(catalog.TryGetValue("AF", out var afObject), "catalog has /AF");
        var af = Assert.IsType<ArrayObject>(reader.Resolve(afObject!));
        var filespec = Assert.IsType<DictionaryObject>(reader.Resolve(af[0]));
        Assert.Equal("factur-x.xml", Assert.IsType<StringObject>(reader.Resolve(filespec["F"])).Value);
        Assert.Equal("Data", Name(reader, filespec, "AFRelationship"));

        var names = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Names"]));
        var tree = Assert.IsType<DictionaryObject>(reader.Resolve(names["EmbeddedFiles"]));
        var pairs = Assert.IsType<ArrayObject>(reader.Resolve(tree["Names"]));
        Assert.Equal("factur-x.xml", Assert.IsType<StringObject>(reader.Resolve(pairs[0])).Value);

        var ef = Assert.IsType<DictionaryObject>(reader.Resolve(filespec["EF"]));
        var stream = Assert.IsType<StreamObject>(reader.Resolve(ef["F"]));
        Assert.Equal(Encoding.UTF8.GetBytes("<invoice/>"), reader.DecodeStream(stream));
    }


    private static DictionaryObject HelveticaFont() => new()
    {
        ["Type"] = new NameObject("Font"),
        ["Subtype"] = new NameObject("Type1"),
        ["BaseFont"] = new NameObject("Helvetica"),
        ["Encoding"] = new NameObject("WinAnsiEncoding"),
    };

    [Fact]
    public void TjDisplacement_SurvivesInterpretReEmit()
    {
        var original = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td [(He) -120 (llo)] TJ ET\n");
        var document = ExtractionSupport.BuildSinglePage(_ => HelveticaFont(), original);

        TextContent? run = null;
        foreach (var element in document.Pages[0].Content)
        {
            if (element is TextContent text)
            {
                run = text;
                break;
            }
        }

        Assert.NotNull(run);
        run!.Color = Color.Red;

        var bytes = document.ToArray();
        var content = Encoding.Latin1.GetString(
            ContentTestHelpers.PageContent(DocumentReader.Parse(bytes), 0));

        Assert.Contains("] TJ", content, StringComparison.Ordinal);
        Assert.Contains("-120", content, StringComparison.Ordinal);
        Assert.Contains("Hello", Load(bytes).ExtractText(), StringComparison.Ordinal);
    }
}
