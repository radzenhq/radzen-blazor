#nullable enable
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class NamedDestinationPreservationTests
{
    private static Document Load(byte[] bytes) => Document.LoadFromStream(new MemoryStream(bytes));

    private static DictionaryObject Catalog(DocumentReader reader)
        => Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]!));

    private static ArrayObject Kids(DocumentReader reader)
    {
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(Catalog(reader)["Pages"]));
        return Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
    }

    private static int Number(DocumentObject reference)
        => Assert.IsType<ReferenceObject>(reference).ObjectNumber;

    private static ArrayObject? Destination(DocumentReader reader, string name)
    {
        var names = Assert.IsType<DictionaryObject>(reader.Resolve(Catalog(reader)["Names"]));
        var node = Assert.IsType<DictionaryObject>(reader.Resolve(names["Dests"]));
        return Find(reader, node, name);
    }

    private static ArrayObject? Find(DocumentReader reader, DictionaryObject node, string name)
    {
        if (node.TryGetValue("Kids", out var kids))
        {
            foreach (var kid in Assert.IsType<ArrayObject>(reader.Resolve(kids)))
            {
                if (Find(reader, Assert.IsType<DictionaryObject>(reader.Resolve(kid)), name) is { } found)
                {
                    return found;
                }
            }
        }

        if (!node.TryGetValue("Names", out var pairs))
        {
            return null;
        }

        var array = Assert.IsType<ArrayObject>(reader.Resolve(pairs));
        for (var i = 0; i < array.Count; i += 2)
        {
            if (Assert.IsType<StringObject>(reader.Resolve(array[i])).Value == name)
            {
                return reader.Resolve(array[i + 1]) as ArrayObject;
            }
        }

        return null;
    }

    private static byte[] Source(string linkDestination = "(chapter2)")
    {
        var one = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (page-one-body) Tj ET");
        var two = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (page-two-body) Tj ET");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /Names << /Dests 9 0 R "
            + "/JavaScript << /Names [(startup) 12 0 R] >> >> >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 2 /Kids [3 0 R 5 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
            + "/Resources << /Font << /F1 7 0 R >> >> /Contents 4 0 R /Annots [8 0 R] >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + one.Length + " >>\nstream\n").Append(one).Append("\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
            + "/Resources << /Font << /F1 7 0 R >> >> /Contents 6 0 R >>\nendobj\n");
        pdf.Mark(6);
        pdf.Append("6 0 obj\n<< /Length " + two.Length + " >>\nstream\n").Append(two).Append("\nendstream\nendobj\n");
        pdf.Object(7, "7 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");
        pdf.Object(8, "8 0 obj\n<< /Type /Annot /Subtype /Link /Rect [10 10 100 30] "
            + "/A << /S /GoTo /D " + linkDestination + " >> >>\nendobj\n");
        pdf.Object(9, "9 0 obj\n<< /Kids [10 0 R 11 0 R] >>\nendobj\n");
        pdf.Object(10, "10 0 obj\n<< /Limits [(chapter1) (chapter1)] "
            + "/Names [(chapter1) [3 0 R /XYZ 0 700 0]] >>\nendobj\n");
        pdf.Object(11, "11 0 obj\n<< /Limits [(chapter2) (chapter2)] "
            + "/Names [(chapter2) [5 0 R /XYZ 0 500 0]] >>\nendobj\n");
        pdf.Object(12, "12 0 obj\n<< /S /JavaScript /JS (app.alert\\(1\\)) >>\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 13\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 13; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 13 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void LoadedNamedDestinations_SurviveSaveAndPointAtTheirRenumberedPage()
    {
        var output = Load(Source()).ToArray();

        var reader = DocumentReader.Parse(output);
        var kids = Kids(reader);
        foreach (var (name, index) in new[] { ("chapter1", 0), ("chapter2", 1) })
        {
            var destination = Destination(reader, name);
            Assert.NotNull(destination);
            Assert.Equal(Number(kids[index]), Number(destination![0]));
        }
    }

    [Fact]
    public void LinkAnnotationNamedTarget_ResolvesThroughThePreservedTree()
    {
        var output = Load(Source()).ToArray();

        var reader = DocumentReader.Parse(output);
        var page = Assert.IsType<DictionaryObject>(reader.Resolve(Kids(reader)[0]));
        var annots = Assert.IsType<ArrayObject>(reader.Resolve(page["Annots"]));
        var action = Assert.IsType<DictionaryObject>(
            reader.Resolve(Assert.IsType<DictionaryObject>(reader.Resolve(annots[0]))["A"]));
        var name = Assert.IsType<StringObject>(reader.Resolve(action["D"])).Value;

        var destination = Destination(reader, name);
        Assert.NotNull(destination);
        Assert.Equal(Number(Kids(reader)[1]), Number(destination![0]));
    }

    [Fact]
    public void LinkAnnotationNamedTargetLoadsResolvedPageAndFit()
    {
        var document = Load(Source());

        var link = Assert.IsType<LinkAnnotation>(Assert.Single(document.Pages[0].Annotations));

        Assert.Equal("chapter2", link.Destination);
        Assert.Equal(1, link.TargetPageIndex);
        Assert.NotNull(link.ResolvedTarget);
        Assert.Equal(OutlineFit.Coordinates, link.ResolvedTarget!.Fit);
        Assert.Equal([0, 500, 0], link.ResolvedTarget.FitArguments);
    }

    [Fact]
    public void EditedExplicitLinkRetainsItsFitTypeAndArgument()
    {
        var document = Load(Source("[5 0 R /FitBV 123]"));
        var link = Assert.IsType<LinkAnnotation>(Assert.Single(document.Pages[0].Annotations));
        link.Contents = "edited";

        var reader = DocumentReader.Parse(document.ToArray());
        var page = Assert.IsType<DictionaryObject>(reader.Resolve(Kids(reader)[0]));
        var annotation = Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(reader.GetArray(page, "Annots")!)));
        var destination = Assert.IsType<ArrayObject>(reader.Resolve(annotation["Dest"]));

        Assert.Equal("FitBV", Assert.IsType<NameObject>(reader.Resolve(destination[1])).Value);
        Assert.Equal(123, Assert.IsType<NumberObject>(reader.Resolve(destination[2])).DoubleValue);
    }

    [Fact]
    public void NonDestinationNamesBranches_SurviveSave()
    {
        var output = Load(Source()).ToArray();

        var reader = DocumentReader.Parse(output);
        var names = Assert.IsType<DictionaryObject>(reader.Resolve(Catalog(reader)["Names"]));
        var javaScript = Assert.IsType<DictionaryObject>(reader.Resolve(names["JavaScript"]));
        var pairs = Assert.IsType<ArrayObject>(reader.Resolve(javaScript["Names"]));
        Assert.Equal("startup", Assert.IsType<StringObject>(reader.Resolve(pairs[0])).Value);
        var script = Assert.IsType<DictionaryObject>(reader.Resolve(pairs[1]));
        Assert.Equal("app.alert(1)", Assert.IsType<StringObject>(reader.Resolve(script["JS"])).Value);
    }

    [Fact]
    public void NamedDestination_ForRemovedPage_CollapsesToNullRatherThanAWrongPage()
    {
        var document = Load(Source());
        document.Pages.RemoveAt(1);

        var reader = DocumentReader.Parse(document.ToArray());
        Assert.Single(Kids(reader));
        var removed = Destination(reader, "chapter2");
        Assert.NotNull(removed);
        Assert.IsType<NullObject>(reader.Resolve(removed![0]));

        var kept = Destination(reader, "chapter1");
        Assert.NotNull(kept);
        Assert.Equal(Number(Kids(reader)[0]), Number(kept![0]));
    }
}
