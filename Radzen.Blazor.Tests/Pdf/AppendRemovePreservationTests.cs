#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class AppendRemovePreservationTests
{
    private static PortableDocument Load(byte[] bytes)
    {
        using var input = new MemoryStream(bytes);
        return PortableDocument.LoadFromStream(input);
    }

    private static string Latin1(byte[] bytes) => Encoding.Latin1.GetString(bytes);

    private static DictionaryObject Catalog(DocumentReader reader)
        => Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]!));

    private static ArrayObject Kids(DocumentReader reader)
    {
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(Catalog(reader)["Pages"]));
        return Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
    }

    private static byte[] LinkAnnotSource()
    {
        var content = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (linked-page) Tj ET");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Annots [5 0 R] >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + content.Length + " >>\nstream\n").Append(content).Append("\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Link /Rect [10 10 100 30] "
            + "/A << /S /URI /URI (https://radzen.com/appended) >> >>\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 6; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 6 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void Append_PageWithUriLink_KeepsUrl()
    {
        var target = new PortableDocument();
        target.Pages.Add().SetContent(Encoding.ASCII.GetBytes("cover"));
        target.Append(Load(LinkAnnotSource()));

        var output = target.ToArray();

        Assert.Contains("https://radzen.com/appended", Latin1(output));

        var reader = DocumentReader.Parse(output);
        var kids = Kids(reader);
        Assert.Equal(2, kids.Count);
        var appended = Assert.IsType<DictionaryObject>(reader.Resolve(kids[1]));
        var annots = Assert.IsType<ArrayObject>(reader.Resolve(appended["Annots"]));
        var annot = Assert.IsType<DictionaryObject>(reader.Resolve(annots[0]));
        var action = Assert.IsType<DictionaryObject>(reader.Resolve(annot["A"]));
        Assert.Equal("https://radzen.com/appended", Assert.IsType<StringObject>(reader.Resolve(action["URI"])).Value);
    }

    private static byte[] TwoPageFormSource()
    {
        var one = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (REDACT-ME-UNIQUE) Tj ET");
        var two = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (page-two-body) Tj ET");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 7 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 2 /Kids [3 0 R 5 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Annots [8 0 R] >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + one.Length + " >>\nstream\n").Append(one).Append("\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 6 0 R >>\nendobj\n");
        pdf.Mark(6);
        pdf.Append("6 0 obj\n<< /Length " + two.Length + " >>\nstream\n").Append(two).Append("\nendstream\nendobj\n");
        pdf.Object(7, "7 0 obj\n<< /Fields [8 0 R] /DR << >> >>\nendobj\n");
        pdf.Object(8, "8 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (RedactField) /P 3 0 R /Rect [0 0 100 20] >>\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 9\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 9; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 9 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void RemoveAt_RedactsPageContentAndField()
    {
        var document = Load(TwoPageFormSource());
        document.Pages.RemoveAt(0);

        var output = document.ToArray();
        var text = Latin1(output);

        Assert.DoesNotContain("REDACT-ME-UNIQUE", text);
        Assert.DoesNotContain("RedactField", text);

        var reader = DocumentReader.Parse(output);
        Assert.Single(Kids(reader));

        var acroForm = Assert.IsType<DictionaryObject>(reader.Resolve(Catalog(reader)["AcroForm"]));
        var fields = Assert.IsType<ArrayObject>(reader.Resolve(acroForm["Fields"]));
        Assert.Empty(fields);
    }

    [Fact]
    public void RemoveAt_KeepsRemainingPageContent()
    {
        var document = Load(TwoPageFormSource());
        document.Pages.RemoveAt(0);

        var text = Latin1(document.ToArray());
        Assert.Contains("page-two-body", text);
    }

    [Fact]
    public void RemoveAt_OtherPage_KeepsSurvivingField()
    {
        var document = Load(TwoPageFormSource());
        document.Pages.RemoveAt(1);

        var output = document.ToArray();
        Assert.Contains("RedactField", Latin1(output));

        var reader = DocumentReader.Parse(output);
        var acroForm = Assert.IsType<DictionaryObject>(reader.Resolve(Catalog(reader)["AcroForm"]));
        var fields = Assert.IsType<ArrayObject>(reader.Resolve(acroForm["Fields"]));
        Assert.Single(fields);
    }
}
