#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class AppendFormMergeTests
{
    private static byte[] NestedForm()
    {
        const string ap = "/Tx BMC q (x) Tj Q EMC";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [6 0 R 8 0 R 9 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R 9 0 R] /NeedAppearances true /DA (/Helv 0 Tf 0 g) /DR << /Font << /Cour 7 0 R >> >> >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /T (address) /Kids [6 0 R 10 0 R] >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (city) /V () /Parent 5 0 R /P 3 0 R /Rect [100 700 350 720] /DA (/Cour 12 Tf 0 g) >>\nendobj\n")
            .Object(7, "7 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Name /Cour >>\nendobj\n")
            .Object(8, "8 0 obj\n<< /Type /Annot /Subtype /Widget /P 3 0 R /Parent 10 0 R /Rect [100 660 350 680] /AP << /N 11 0 R >> >>\nendobj\n")
            .Object(9, "9 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (Name) /V () /P 3 0 R /Rect [100 600 350 620] /DA (/Cour 12 Tf 0 g) >>\nendobj\n")
            .Object(10, "10 0 obj\n<< /FT /Tx /T (zip) /V () /Parent 5 0 R /Kids [8 0 R] >>\nendobj\n")
            .Object(11, "11 0 obj\n<< /Type /XObject /Subtype /Form /BBox [0 0 250 20] /Length " + ap.Length + " >>\nstream\n" + ap + "\nendstream\nendobj\n");
        return FixturePdf.Wrap(pdf, 12);
    }

    private static string[] RootFields(string emission, int count)
        => [.. References("AcroForm", "Fields", count, Line(emission, "/Fields ["))
            .Select(number => IndirectObject(emission, number))];

    private static string Field(string[] fields, string name)
    {
        var marker = $"/T ({name})";
        var matches = fields.Where(field => field.Contains(marker, StringComparison.Ordinal)).ToArray();
        Assert.True(
            matches.Length == 1,
            $"Exactly one field must carry '{marker}', found {matches.Length}."
            + $"\nFields:\n{string.Join("\n", fields.Select(Excerpt))}");
        return matches[0];
    }

    [Fact]
    public void Append_NestedSourceForm_KeepsTreeIntact()
    {
        var a = new PortableDocument();
        a.Pages.Add().SetContent(Encoding.ASCII.GetBytes("base"));
        a.Append(PortableDocument.LoadFromStream(new MemoryStream(NestedForm())));

        var emission = Emit(a);
        var roots = RootFields(emission, 2);

        var address = Field(roots, "address");
        Field(roots, "Name");

        string[] kids = [.. References("address field", "Kids", 2, address)
            .Select(number => IndirectObject(emission, number))];

        Carries("zip field", "/Kids [", Field(kids, "zip"));
    }

    [Fact]
    public void Append_NestedSourceForm_UnionsDefaultResourceFonts()
    {
        var a = new PortableDocument();
        a.Pages.Add().SetContent(Encoding.ASCII.GetBytes("base"));
        a.Append(PortableDocument.LoadFromStream(new MemoryStream(NestedForm())));

        Shaped("AcroForm /DR", @"/DR << /Font << /Cour \d+ 0 R", Line(Emit(a), "/Fields ["));
    }

    [Fact]
    public void Append_CollidingRootName_DisambiguatesWithoutDropping()
    {
        var a = new PortableDocument();
        var page = a.Pages.Add();
        page.SetContent(Encoding.ASCII.GetBytes("base"));
        a.FormFields.Add(new TextFieldDefinition("Name")
        {
            PageIndex = 0,
            X = 100,
            Y = 500,
            Width = 200,
            Height = 20,
        });
        a.Append(PortableDocument.LoadFromStream(new MemoryStream(NestedForm())));

        var emission = Emit(a);
        var roots = RootFields(emission, 3);

        Field(roots, "Name");
        Field(roots, "Name_2");
        Field(roots, "address");
    }
}
