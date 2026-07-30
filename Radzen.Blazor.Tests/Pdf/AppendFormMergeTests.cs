#nullable enable
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class AppendFormMergeTests
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

        pdf.Append("trailer\n<< /Size " + count + " /Root 1 0 R >>\n");
        pdf.Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

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
        return Wrap(pdf, 12);
    }

    private static DictionaryObject AcroForm(DocumentReader reader)
    {
        var root = (DictionaryObject)reader.Resolve(reader.Trailer["Root"]!)!;
        return (DictionaryObject)reader.Resolve(root["AcroForm"]!)!;
    }

    private static string[] RootFieldNames(DocumentReader reader)
    {
        var fields = (ArrayObject)reader.Resolve(AcroForm(reader)["Fields"]!)!;
        return fields
            .Select(f => reader.Resolve(f) as DictionaryObject)
            .Where(d => d is not null && d!.TryGetValue("T", out _))
            .Select(d => ((StringObject)reader.Resolve(d!["T"]!)!).Value)
            .ToArray();
    }

    private static DictionaryObject RootByName(DocumentReader reader, string name)
    {
        var fields = (ArrayObject)reader.Resolve(AcroForm(reader)["Fields"]!)!;
        foreach (var f in fields)
        {
            if (reader.Resolve(f) is DictionaryObject d && d.TryGetValue("T", out var t)
                && reader.Resolve(t!) is StringObject s && s.Value == name)
            {
                return d;
            }
        }

        throw new Xunit.Sdk.XunitException($"root field '{name}' not found; got [{string.Join(", ", RootFieldNames(reader))}]");
    }

    [Fact]
    public void Append_NestedSourceForm_KeepsTreeIntact()
    {
        var a = new PortableDocument();
        a.Pages.Add().SetContent(Encoding.ASCII.GetBytes("base"));
        a.Append(PortableDocument.LoadFromStream(new MemoryStream(NestedForm())));

        var reader = DocumentReader.Parse(a.ToArray());
        var names = RootFieldNames(reader);

        Assert.Contains("address", names);
        Assert.Contains("Name", names);

        var address = RootByName(reader, "address");
        var kids = (ArrayObject)reader.Resolve(address["Kids"]!)!;
        Assert.Equal(2, kids.Count);

        var zip = kids
            .Select(k => (DictionaryObject)reader.Resolve(k)!)
            .Single(d => ((StringObject)reader.Resolve(d["T"]!)!).Value == "zip");
        Assert.True(zip.ContainsKey("Kids"));
    }

    [Fact]
    public void Append_NestedSourceForm_UnionsDefaultResourceFonts()
    {
        var a = new PortableDocument();
        a.Pages.Add().SetContent(Encoding.ASCII.GetBytes("base"));
        a.Append(PortableDocument.LoadFromStream(new MemoryStream(NestedForm())));

        var reader = DocumentReader.Parse(a.ToArray());
        var dr = (DictionaryObject)reader.Resolve(AcroForm(reader)["DR"]!)!;
        var fonts = (DictionaryObject)reader.Resolve(dr["Font"]!)!;

        Assert.True(fonts.ContainsKey("Cour"), "source /DR font must union into merged form");
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

        var reader = DocumentReader.Parse(a.ToArray());
        var names = RootFieldNames(reader);

        Assert.Contains("Name", names);
        Assert.Contains("Name_2", names);
        Assert.Contains("address", names);
        Assert.Equal(names.Length, names.Distinct().Count());
    }
}
