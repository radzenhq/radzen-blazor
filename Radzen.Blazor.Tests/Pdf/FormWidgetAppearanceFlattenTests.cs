#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class FormWidgetAppearanceFlattenTests
{
    private const string YesAppearance = "1 0 0 RG 2 2 16 16 re S";
    private const string OffAppearance = "0 0 1 RG 4 4 12 12 re S";

    private static byte[] CheckBoxWithCustomAppearance(string state)
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 /MediaBox [0 0 612 792] >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /Contents 7 0 R /Resources 8 0 R /Annots [6 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /FT /Btn /T (agree) /Kids [6 0 R] >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /Annot /Subtype /Widget /Parent 5 0 R /P 3 0 R /Rect [100 700 120 720] "
                + "/AP << /N << /Yes 9 0 R /Off 10 0 R >> >> /AS /" + state + " >>\nendobj\n")
            .Object(7, "7 0 obj\n<< /Length 3 >>\nstream\nq Q\nendstream\nendobj\n")
            .Object(8, "8 0 obj\n<< >>\nendobj\n")
            .Object(9, "9 0 obj\n<< /Type /XObject /Subtype /Form /BBox [0 0 20 20] /Length " + YesAppearance.Length
                + " >>\nstream\n" + YesAppearance + "\nendstream\nendobj\n")
            .Object(10, "10 0 obj\n<< /Type /XObject /Subtype /Form /BBox [0 0 20 20] /Length " + OffAppearance.Length
                + " >>\nstream\n" + OffAppearance + "\nendstream\nendobj\n");
        return FixturePdf.Wrap(pdf, 11);
    }

    private static byte[] AllPageContentBytes(DocumentReader reader)
    {
        var page = FormTestSupport.FirstPage(reader);
        var contents = reader.Resolve(page["Contents"]);
        if (contents is StreamObject single)
        {
            return FormTestSupport.DecodeBytes(single);
        }

        var bytes = new List<byte>();
        foreach (var entry in Assert.IsType<ArrayObject>(contents))
        {
            bytes.AddRange(FormTestSupport.DecodeBytes(Assert.IsType<StreamObject>(reader.Resolve(entry))));
            bytes.Add((byte)'\n');
        }

        return [.. bytes];
    }

    private static string AllPageContent(DocumentReader reader)
    {
        var page = FormTestSupport.FirstPage(reader);
        var contents = reader.Resolve(page["Contents"]);
        if (contents is StreamObject single)
        {
            return FormTestSupport.Decode(single);
        }

        var document = new System.Text.StringBuilder();
        foreach (var entry in Assert.IsType<ArrayObject>(contents))
        {
            document.Append(FormTestSupport.Decode(Assert.IsType<StreamObject>(reader.Resolve(entry))));
            document.Append('\n');
        }

        return document.ToString();
    }

    private static string[] PaintedAppearances(DocumentReader reader)
    {
        var page = FormTestSupport.FirstPage(reader);
        var resources = Assert.IsType<DictionaryObject>(reader.Resolve(page["Resources"]));
        var xobjects = reader.GetDictionary(resources, "XObject");
        if (xobjects is null)
        {
            return [];
        }

        return xobjects.Keys
            .Select(k => reader.Resolve(xobjects[k]))
            .OfType<StreamObject>()
            .Select(FormTestSupport.Decode)
            .ToArray();
    }

    [Fact]
    public void CheckedWidgetPaintsItsOwnNormalAppearance()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(CheckBoxWithCustomAppearance("Yes")));

        document.Flatten();
        var reader = FormTestSupport.Reload(document);

        Assert.Contains("Do", ContentOperationTestHelpers.Operators(AllPageContentBytes(reader)));
        Assert.Contains(PaintedAppearances(reader), a => a.Contains(YesAppearance));
    }

    [Fact]
    public void OffWidgetPaintsItsOffAppearanceInsteadOfVanishing()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(CheckBoxWithCustomAppearance("Off")));

        document.Flatten();
        var reader = FormTestSupport.Reload(document);

        Assert.Contains("Do", ContentOperationTestHelpers.Operators(AllPageContentBytes(reader)));
        Assert.Contains(PaintedAppearances(reader), a => a.Contains(OffAppearance));
    }
}
