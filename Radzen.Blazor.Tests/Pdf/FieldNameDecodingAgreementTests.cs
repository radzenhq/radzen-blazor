#nullable enable
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class FieldNameDecodingAgreementTests
{
    private static byte[] Wrap(FixturePdf pdf, int count)
    {
        var xref = pdf.Position;
        pdf.Append("xref\n0 " + count + "\n");
        pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number < count; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size " + count + " /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static byte[] Utf16NamedForm()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [5 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] /DA (/Helv 0 Tf 0 g) >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (\\376\\377\\000N\\000a\\000m\\000e) "
                + "/P 3 0 R /Rect [100 640 280 700] /V (x) >>\nendobj\n");
        return Wrap(pdf, 6);
    }

    private static string[] RootFieldNames(DocumentReader reader)
    {
        var root = (DictionaryObject)reader.Resolve(reader.Trailer["Root"]!)!;
        var form = (DictionaryObject)reader.Resolve(root["AcroForm"]!)!;
        var fields = (ArrayObject)reader.Resolve(form["Fields"]!)!;
        return fields
            .Select(reader.Resolve)
            .OfType<DictionaryObject>()
            .Where(d => d.ContainsKey("T"))
            .Select(d => FormField.DecodeTextString(((StringObject)reader.Resolve(d["T"]!)!).Value))
            .ToArray();
    }

    [Fact]
    public void MergedUtf16FieldName_CollidesWithAsciiNameAfterDecoding()
    {
        var document = new Document();
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("base"));
        document.FormFields.Add(new TextFieldDefinition("Name")
        {
            X = 50,
            Y = 500,
            Width = 120,
            Height = 20,
            Value = "value",
        });

        document.Append(Document.LoadFromStream(new MemoryStream(Utf16NamedForm())));

        var names = RootFieldNames(DocumentReader.Parse(document.ToArray()));
        Assert.Contains("Name", names);
        Assert.Contains("Name_2", names);
    }
}
