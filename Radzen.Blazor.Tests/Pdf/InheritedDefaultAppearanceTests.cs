#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class InheritedDefaultAppearanceTests
{
    private static byte[] ParentDefaultAppearanceForm()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [6 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] /DA (/Helv 0 Tf 0 g) /DR << /Font << /Helv 7 0 R >> >> >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /T (group) /DA (/Helv 9 Tf 0 g) /Kids [6 0 R] >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (amount) /V (100) /P 3 0 R /Parent 5 0 R /Rect [100 700 350 720] >>\nendobj\n")
            .Object(7, "7 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Name /Helv >>\nendobj\n");
        return FixturePdf.Wrap(pdf, 8);
    }

    private static PortableDocument Load() => PortableDocument.LoadFromStream(new MemoryStream(ParentDefaultAppearanceForm()));

    [Fact]
    public void FillFieldBakesFontSizeFromNonTerminalParentDefaultAppearance()
    {
        var document = Load();
        document.AcroForm!.FillField("group.amount", "100");

        var reader = FormTestSupport.Reload(document);
        var field = FormTestSupport.Field(reader, "group");
        var kids = (Radzen.Documents.Pdf.Objects.ArrayObject)reader.Resolve(field["Kids"]);
        var widget = (Radzen.Documents.Pdf.Objects.DictionaryObject)reader.Resolve(kids[0]);
        var appearance = FormTestSupport.NormalAppearanceText(reader, widget);

        Assert.Contains("9 Tf", appearance);
        Assert.DoesNotContain("12 Tf", appearance);
    }

    [Fact]
    public void FlattenPaintsFontSizeFromNonTerminalParentDefaultAppearance()
    {
        var document = Load();
        document.Flatten();

        var reader = FormTestSupport.Reload(document);
        var content = FormTestSupport.PageContentText(reader);

        Assert.Contains("9 Tf", content);
        Assert.DoesNotContain("12 Tf", content);
    }
}
