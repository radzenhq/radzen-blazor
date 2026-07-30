#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class DocumentMetadataRegressionTests
{
    private const string CustomNamespace = "urn:radzen:test:metadata";

    [Theory]
    [InlineData("/Dest [3 0 R /FitB]", "FitB")]
    [InlineData("/A << /S /URI /URI (https://radzen.com) >>", "URI")]
    public void UnsupportedOutlineTarget_LoadsAndIsPreserved(string targetEntry, string targetKind)
    {
        var document = Load(BuildPdf("/Outlines 6 0 R", targetEntry));

        Assert.Contains(document.Pages[0].Content.OfType<TextContent>(), text => text.Text == "page-body");
        Assert.Null(Assert.Single(document.Outline).Target);

        var reader = DocumentReader.Parse(document.ToArray());
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var outlines = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Outlines"]));
        var item = Assert.IsType<DictionaryObject>(reader.Resolve(outlines["First"]));
        if (targetKind == "FitB")
        {
            var destination = Assert.IsType<ArrayObject>(reader.Resolve(item["Dest"]));
            Assert.Equal("FitB", Assert.IsType<NameObject>(reader.Resolve(destination[1])).Value);
        }
        else
        {
            var action = Assert.IsType<DictionaryObject>(reader.Resolve(item["A"]));
            Assert.Equal("URI", Assert.IsType<NameObject>(reader.Resolve(action["S"])).Value);
            Assert.Equal("https://radzen.com", Assert.IsType<StringObject>(reader.Resolve(action["URI"])).Value);
        }
    }

    [Theory]
    [InlineData("/Dest [3 0 R /XYZ null null 0]")]
    [InlineData("/Dest [3 0 R /XYZ null 700 null]")]
    [InlineData("/Dest [3 0 R /FitH null]")]
    public void OutlineTargetWithNullCoordinates_LoadsWithoutThrowing(string targetEntry)
    {
        var document = Load(BuildPdf("/Outlines 6 0 R", targetEntry));

        Assert.Contains(document.Pages[0].Content.OfType<TextContent>(), text => text.Text == "page-body");
        Assert.NotNull(Assert.Single(document.Outline).Target);
    }

    [Fact]
    public void NonAttachmentNames_SurviveLoadedSave()
    {
        const string names = "/Names << /JavaScript << /Names [(startup) 8 0 R] >> "
            + "/Dests << /Names [(chapter) [3 0 R /Fit]] >> >>";

        var saved = Load(BuildPdf(names, null)).ToArray();

        var reader = DocumentReader.Parse(saved);
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var tree = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Names"]));

        var dests = Assert.IsType<ArrayObject>(
            reader.Resolve(Assert.IsType<DictionaryObject>(reader.Resolve(tree["Dests"]))["Names"]));
        Assert.Equal("chapter", Assert.IsType<StringObject>(reader.Resolve(dests[0])).Value);
        var destination = Assert.IsType<ArrayObject>(reader.Resolve(dests[1]));
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        Assert.Equal(
            Assert.IsType<ReferenceObject>(kids[0]).ObjectNumber,
            Assert.IsType<ReferenceObject>(destination[0]).ObjectNumber);

        var javaScript = Assert.IsType<ArrayObject>(
            reader.Resolve(Assert.IsType<DictionaryObject>(reader.Resolve(tree["JavaScript"]))["Names"]));
        Assert.Equal("startup", Assert.IsType<StringObject>(reader.Resolve(javaScript[0])).Value);
    }

    [Fact]
    public void NewlyCreatedXmpPacket_HasStandardFramingAndPadding()
    {
        var document = new Document();

        document.Xmp.SetProperty(CustomNamespace, "status", "draft");

        var packet = Assert.IsType<byte[]>(document.Xmp.GetPacket());
        var text = Encoding.UTF8.GetString(packet);
        Assert.True(text.StartsWith("<?xpacket begin=\"\uFEFF\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>", StringComparison.Ordinal));
        Assert.True(text.EndsWith("<?xpacket end=\"w\"?>", StringComparison.Ordinal));
        Assert.Contains(string.Concat(Enumerable.Repeat("                                                                                \n", 24)), text, StringComparison.Ordinal);
        Assert.NotNull(XDocument.Parse(text, System.Xml.Linq.LoadOptions.PreserveWhitespace).Root);
    }

    [Fact]
    public void ExistingXmpPacket_KeepsItsFramingWhenEdited()
    {
        const string packet = "<?xpacket begin=\"x\" id=\"custom\"?>\n"
            + "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"><rdf:Description rdf:about=\"\" /></rdf:RDF></x:xmpmeta>\n"
            + "   \n<?xpacket end=\"r\"?>";
        var document = new Document();
        document.Xmp.SetPacket(Encoding.UTF8.GetBytes(packet));

        document.Xmp.SetProperty(CustomNamespace, "status", "edited");

        var edited = Encoding.UTF8.GetString(Assert.IsType<byte[]>(document.Xmp.GetPacket()));
        Assert.True(edited.StartsWith("<?xpacket begin=\"x\" id=\"custom\"?>", StringComparison.Ordinal));
        Assert.True(edited.EndsWith("<?xpacket end=\"r\"?>", StringComparison.Ordinal));
        Assert.Contains("\n   \n<?xpacket", edited, StringComparison.Ordinal);
    }

    private static Document Load(byte[] bytes)
        => Document.LoadFromStream(new MemoryStream(bytes));

    private static byte[] BuildPdf(string catalogExtra, string? outlineTarget)
    {
        var content = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (page-body) Tj ET");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R " + catalogExtra + " >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
            + "/Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + content.Length + " >>\nstream\n").Append(content).Append("\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");
        if (outlineTarget is null)
        {
            pdf.Object(6, "6 0 obj\nnull\nendobj\n");
            pdf.Object(7, "7 0 obj\nnull\nendobj\n");
        }
        else
        {
            pdf.Object(6, "6 0 obj\n<< /Type /Outlines /First 7 0 R /Last 7 0 R /Count 1 >>\nendobj\n");
            pdf.Object(7, "7 0 obj\n<< /Title (Unsupported target) /Parent 6 0 R " + outlineTarget + " >>\nendobj\n");
        }

        pdf.Object(8, "8 0 obj\n<< /S /JavaScript /JS (app.alert\\(1\\)) >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 9\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 9; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 9 /Root 1 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }
}
