#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ReaderRepairRobustnessTests
{
    private static PortableDocument Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return PortableDocument.LoadFromStream(stream);
    }

    private static string Offset(FixturePdf pdf, int number)
        => pdf.OffsetOf(number).ToString(CultureInfo.InvariantCulture);

    [Fact]
    public void CorruptFlateXrefStream_TriggersRepairAndLoadsPages()
    {
        var pdf = new FixturePdf();
        pdf.Append("%PDF-1.5\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Length 3 >>\nstream\n0 g\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /XRef /W [1 2 1] /Size 6 /Root 1 0 R /Filter /FlateDecode /Length 8 >>\nstream\nGARBAGE!\nendstream\nendobj\n");
        pdf.Append("startxref\n" + Offset(pdf, 5) + "\n%%EOF\n");

        var document = Load(pdf.ToArray());

        Assert.Equal(1, document.Pages.Count);
        Assert.Equal(Encoding.ASCII.GetBytes("0 g"), document.Pages[0].GetContent());
    }

    [Fact]
    public void CorruptFlateXrefStream_ExtractsTextAfterRepair()
    {
        var pdf = new FixturePdf();
        pdf.Append("%PDF-1.5\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R"
            + " /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n");
        var content = "BT /F1 12 Tf 72 700 Td (Recovered) Tj ET";
        pdf.Object(4, "4 0 obj\n<< /Length " + content.Length.ToString(CultureInfo.InvariantCulture)
            + " >>\nstream\n" + content + "\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");
        pdf.Object(6, "6 0 obj\n<< /Type /XRef /W [1 2 1] /Size 7 /Root 1 0 R /Filter /FlateDecode /Length 4 >>\nstream\nBAD!\nendstream\nendobj\n");
        pdf.Append("startxref\n" + Offset(pdf, 6) + "\n%%EOF\n");

        var document = Load(pdf.ToArray());

        Assert.Contains("Recovered", document.ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void Repair_TwoCatalogs_PicksNewestDeterministically()
    {
        var pdf = new FixturePdf();
        pdf.Append("%PDF-1.5\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n");
        pdf.Object(7, "7 0 obj\n<< /Type /Page /Parent 6 0 R /MediaBox [0 0 300 400] >>\nendobj\n");
        pdf.Object(6, "6 0 obj\n<< /Type /Pages /Kids [3 0 R 7 0 R] /Count 2 >>\nendobj\n");
        pdf.Object(8, "8 0 obj\n<< /Type /Catalog /Pages 6 0 R >>\nendobj\n");
        pdf.Append("startxref\n" + Offset(pdf, 1) + "\n%%EOF\n");

        var document = Load(pdf.ToArray());

        Assert.Equal(2, document.Pages.Count);
        Assert.Equal(300.0, document.Pages[1].Width.Point, 0.01);
        Assert.Equal(400.0, document.Pages[1].Height.Point, 0.01);
    }

    [Fact]
    public void CyclicStreamLength_RecoversTheContentInsteadOfRecursing()
    {
        var pdf = new FixturePdf();
        pdf.Append("%PDF-1.4\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Length 4 0 R >>\nstream\n(hello)\nendstream\nendobj\n");
        var xrefOffset = pdf.Position;
        pdf.Append("xref\n0 5\n");
        pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 4; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n"
            + xrefOffset.ToString(CultureInfo.InvariantCulture) + "\n%%EOF\n");

        var document = Load(pdf.ToArray());

        Assert.Equal(Encoding.ASCII.GetBytes("(hello)"), document.Pages[0].GetContent());
    }

    private static byte[] ClassicFile(int root, params (int Number, string Body)[] objects)
    {
        var pdf = new FixturePdf().Append("%PDF-1.4\n");
        foreach (var (number, body) in objects)
        {
            pdf.Object(number, $"{number} 0 obj\n{body}\nendobj\n");
        }

        var xref = pdf.Position;
        var max = objects[^1].Number;
        pdf.Append($"xref\n0 {max + 1}\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= max; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        return pdf.Append($"trailer\n<< /Size {max + 1} /Root {root} 0 R >>\nstartxref\n{xref}\n%%EOF\n").ToArray();
    }

    [Fact]
    public void MissingUnrecoverableRoot_Throws()
    {
        var bytes = ClassicFile(1,
            (1, "<< /Type /NotCatalog >>"),
            (2, "<< /Type /Pages /Kids [] /Count 0 >>"));

        Assert.Throws<DocumentParseException>(() => PortableDocument.LoadFromStream(new MemoryStream(bytes)));
    }

    [Fact]
    public void MistypedRoot_ReconstructsCatalogAndLoadsAllPages()
    {
        var bytes = ClassicFile(99,
            (1, "<< /Type /Catalog /Pages 2 0 R >>"),
            (2, "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>"),
            (3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>"),
            (4, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>"));

        Assert.Equal(2, PortableDocument.LoadFromStream(new MemoryStream(bytes)).Pages.Count);
    }

    [Fact]
    public void TruncatedXrefStream_RepairsInsteadOfDroppingObjects()
    {
        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n");
        var off3 = pdf.Position;
        pdf.Append("3 0 obj\n<< /Type /XRef /Size 6 /Index [0 6] /W [1 2 1] /Root 1 0 R /Length 12 >>\nstream\n")
            .Append(new byte[12])
            .Append("\nendstream\nendobj\n")
            .Append("startxref\n" + off3 + "\n%%EOF\n");

        var reader = DocumentReader.Parse(pdf.ToArray());
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }

    [Fact]
    public void WrongLength_RecoversToEndstream()
    {
        const string payload = "HELLO-STREAM-DATA";
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n");
        pdf.Mark(3).Append("3 0 obj\n<< /Length 999999 >>\nstream\n" + payload + "\nendstream\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 4\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(3)))
            .Append("trailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");

        var reader = DocumentReader.Parse(pdf.ToArray());
        var stream = Assert.IsType<StreamObject>(reader.GetObject(3));
        Assert.Equal(payload, Encoding.Latin1.GetString(reader.DecodeStream(stream)));
    }
}
