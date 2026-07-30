#nullable enable
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000 7.9.2.2: /V and /T may be UTF-16BE text strings carrying the FE FF byte order mark, which must be decoded rather than surfaced as bytes.
public class FormFieldTextDecodeTests
{
    private const string Faktura = "Фактура";
    private const string Sofia = "София";
    private const string A = "А";
    private const string Be = "Б";
    private const string Imya = "Имя";

    private static string Utf16BeHex(string text)
    {
        var document = new StringBuilder("<FEFF");
        foreach (var b in Encoding.BigEndianUnicode.GetBytes(text))
        {
            document.Append(b.ToString("X2"));
        }

        return document.Append('>').ToString();
    }

    private static string Utf8Hex(string text)
    {
        var document = new StringBuilder("<EFBBBF");
        foreach (var b in Encoding.UTF8.GetBytes(text))
        {
            document.Append(b.ToString("X2"));
        }

        return document.Append('>').ToString();
    }

    private static byte[] Wrap(FixturePdf pdf, int count)
    {
        var xref = pdf.Position;
        pdf.Append("xref\n0 " + count + "\n");
        pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number < count; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size " + count + " /Root 1 0 R >>\n");
        pdf.Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static byte[] Form()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [5 0 R 6 0 R 8 0 R 9 0 R 10 0 R 11 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R 6 0 R 7 0 R 9 0 R 10 0 R 11 0 R] >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (utf) /V " + Utf16BeHex(Faktura) + " /P 3 0 R /Rect [100 700 350 720] >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (ascii) /V (Hello) /P 3 0 R /Rect [100 660 350 680] >>\nendobj\n")
            .Object(7, "7 0 obj\n<< /T (grp) /FT /Tx /V " + Utf16BeHex(Sofia) + " /Kids [8 0 R] >>\nendobj\n")
            .Object(8, "8 0 obj\n<< /Type /Annot /Subtype /Widget /T (leaf) /Parent 7 0 R /P 3 0 R /Rect [100 620 350 640] >>\nendobj\n")
            .Object(9, "9 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Ch /T (multi) /V [" + Utf16BeHex(A) + " " + Utf16BeHex(Be) + "] /P 3 0 R /Rect [100 580 350 600] >>\nendobj\n")
            .Object(10, "10 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (utf8) /V " + Utf8Hex(Faktura) + " /P 3 0 R /Rect [100 540 350 560] >>\nendobj\n")
            .Object(11, "11 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T " + Utf8Hex(Imya) + " /V (x) /P 3 0 R /Rect [100 500 350 520] >>\nendobj\n");
        return Wrap(pdf, 12);
    }

    private static Document Load()
        => Document.LoadFromStream(new MemoryStream(Form()));

    private static string? ValueOf(Document document, string name)
        => document.AcroForm!.Fields.First(f => f.Name == name).Value;

    [Fact]
    public void Utf16BeValueDecodes()
    {
        Assert.Equal(Faktura, ValueOf(Load(), "utf"));
    }

    [Fact]
    public void AsciiValueDecodes()
    {
        Assert.Equal("Hello", ValueOf(Load(), "ascii"));
    }

    [Fact]
    public void InheritedValueResolvesFromParent()
    {
        Assert.Equal(Sofia, ValueOf(Load(), "grp.leaf"));
    }

    [Fact]
    public void MultiSelectArrayValueDecodes()
    {
        Assert.Equal(A + ", " + Be, ValueOf(Load(), "multi"));
    }

    [Fact]
    public void Utf16BeNameDecodes()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [5 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T " + Utf16BeHex(Imya) + " /V (x) /P 3 0 R /Rect [100 700 350 720] >>\nendobj\n");
        var document = Document.LoadFromStream(new MemoryStream(Wrap(pdf, 6)));

        Assert.Equal(Imya, document.AcroForm!.Fields.Single().Name);
    }

    [Fact]
    // ISO 32000-2 7.9.2.2: a text string may be UTF-8 prefixed with EF BB BF.
    public void Utf8ValueDecodes()
    {
        Assert.Equal(Faktura, ValueOf(Load(), "utf8"));
    }

    [Fact]
    public void Utf8NameDecodes()
    {
        Assert.Contains(Imya, Load().AcroForm!.Fields.Select(f => f.Name));
    }

    [Fact]
    public void Utf8InfoTitleDecodes()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-2.0\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Title " + Utf8Hex(Sofia) + " >>\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 5\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number < 5; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 5 /Root 1 0 R /Info 4 0 R >>\n");
        pdf.Append("startxref\n" + xref + "\n%%EOF\n");

        var document = Document.LoadFromStream(new MemoryStream(pdf.ToArray()));

        Assert.Equal(Sofia, document.Info.Title);
    }

    [Fact]
    public void Utf8PrefixWithInvalidRemainderStaysVerbatim()
    {
        Assert.Equal("ï»¿ÿþ", FormField.DecodeTextString("ï»¿ÿþ"));
    }

    [Fact]
    public void Utf8PrefixAloneDecodesToEmpty()
    {
        Assert.Equal("", FormField.DecodeTextString("ï»¿"));
    }

    [Fact]
    public void ShortStringWithoutBomIsUnchanged()
    {
        Assert.Equal("ï»", FormField.DecodeTextString("ï»"));
    }

    [Fact]
    public void TextStringWithoutBomUsesPdfDocEncoding()
    {
        Assert.Equal("•—€", FormField.DecodeTextString("\u0080\u0084\u00a0"));
    }

    [Fact]
    public void FilledNonAsciiValueReadsBack()
    {
        var document = Load();
        document.AcroForm!.FillField("ascii", Faktura);

        var reloaded = Document.LoadFromStream(new MemoryStream(FormTestSupport.Save(document)));

        Assert.Equal(Faktura, ValueOf(reloaded, "ascii"));
    }
}
