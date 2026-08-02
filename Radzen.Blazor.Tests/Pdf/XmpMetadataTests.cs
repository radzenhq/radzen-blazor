#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using Radzen.Documents.Pdf.Write;
using Radzen.Documents;
namespace Radzen.Blazor.Pdf.Tests;

public class XmpMetadataTests
{
    private static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";
    private static readonly XNamespace Xmp = "http://ns.adobe.com/xap/1.0/";
    private static readonly XNamespace Pdf = "http://ns.adobe.com/pdf/1.3/";
    private static readonly XNamespace PdfaId = "http://www.aiim.org/pdfa/ns/id/";

    private static XmpMetadata Sample()
    {
        return new XmpMetadata
        {
            Info = new DocumentInfo
            {
                Title = "Q3 Invoice & Receipt",
                Author = "Radzen Ltd",
                Subject = "Invoice for services rendered",
                Keywords = "invoice, facturx, pdfa",
                Creator = "Radzen Blazor Studio",
            },
            Producer = "Radzen PDF 1.0",
            PdfAPart = 3,
            PdfAConformance = "B",
            FacturX = new FacturXMetadata
            {
                DocumentType = "INVOICE",
                DocumentFileName = "factur-x.xml",
                Version = "1.0",
            },
        };
    }

    private static string PacketText() => Encoding.UTF8.GetString(Sample().BuildPacket());

    private static XDocument PacketXml()
    {
        var text = PacketText();
        var start = text.IndexOf('<', text.IndexOf("?>", StringComparison.Ordinal) + 2);
        var end = text.LastIndexOf("</x:xmpmeta>", StringComparison.Ordinal) + "</x:xmpmeta>".Length;
        return XDocument.Parse(text[start..end]);
    }

    private static string? ElementOrAttr(XDocument doc, XNamespace ns, string local)
    {
        var el = doc.Descendants(ns + local).FirstOrDefault();
        if (el != null)
        {
            return el.Value.Trim();
        }

        var attr = doc.Descendants().Attributes(ns + local).FirstOrDefault();
        return attr?.Value.Trim();
    }

    private static XElement? FacturX(XDocument doc, string local)
    {
        return doc.Descendants().FirstOrDefault(e =>
            e.Name.LocalName == local &&
            e.Name.Namespace.NamespaceName.Contains("factur-x", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Packet_IsValidXml()
    {
        var doc = PacketXml();
        Assert.Equal("xmpmeta", doc.Root!.Name.LocalName);
    }

    [Fact]
    public void Packet_HasRdfRootDescription()
    {
        XNamespace rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        Assert.NotNull(PacketXml().Descendants(rdf + "RDF").FirstOrDefault());
        Assert.NotNull(PacketXml().Descendants(rdf + "Description").FirstOrDefault());
    }

    [Theory]
    [InlineData("dc", "title", "Q3 Invoice & Receipt")]
    [InlineData("dc", "creator", "Radzen Ltd")]
    [InlineData("dc", "description", "Invoice for services rendered")]
    [InlineData("xmp", "CreatorTool", "Radzen Blazor Studio")]
    [InlineData("pdf", "Keywords", "invoice, facturx, pdfa")]
    [InlineData("pdf", "Producer", "Radzen PDF 1.0")]
    [InlineData("pdfaid", "part", "3")]
    [InlineData("pdfaid", "conformance", "B")]
    [InlineData("facturx", "DocumentType", "INVOICE")]
    [InlineData("facturx", "DocumentFileName", "factur-x.xml")]
    [InlineData("facturx", "Version", "1.0")]
    public void Packet_CarriesTheAuthoredMetadataField(string schema, string local, string expected)
    {
        var xml = PacketXml();
        var actual = schema switch
        {
            "dc" => xml.Descendants(Dc + local).FirstOrDefault()?.Value.Trim(),
            "xmp" => ElementOrAttr(xml, Xmp, local),
            "pdf" => ElementOrAttr(xml, Pdf, local),
            "pdfaid" => ElementOrAttr(xml, PdfaId, local),
            _ => FacturX(xml, local)?.Value.Trim(),
        };

        Assert.NotNull(actual);
        Assert.Contains(expected, actual, StringComparison.Ordinal);
    }

    [Fact]
    public void Packet_IsWrappedInAPaddedXpacketEnvelope()
    {
        var text = PacketText();

        Assert.StartsWith("<?xpacket begin", text.TrimStart(), StringComparison.Ordinal);
        Assert.Contains("<?xpacket end", text, StringComparison.Ordinal);

        var afterMeta = text.IndexOf("</x:xmpmeta>", StringComparison.Ordinal) + "</x:xmpmeta>".Length;
        var beforeEnd = text.IndexOf("<?xpacket end", StringComparison.Ordinal);
        Assert.True(beforeEnd > afterMeta);

        var padding = text[afterMeta..beforeEnd];
        Assert.True(string.IsNullOrWhiteSpace(padding));
        Assert.True(padding.Length >= 100, $"expected padding >= 100 whitespace chars, got {padding.Length}");
    }

    [Fact]
    public void Stream_IsAnUncompressedXmlMetadataStreamHoldingThePacket()
    {
        var sample = Sample();
        var stream = sample.BuildStream();

        Assert.Equal("Metadata", ((NameObject)stream.Dictionary["Type"]).Value);
        Assert.Equal("XML", ((NameObject)stream.Dictionary["Subtype"]).Value);
        Assert.False(stream.Dictionary.ContainsKey("Filter"));
        Assert.Equal(sample.BuildPacket(), stream.Data);
    }

    [Theory]
    [InlineData("null\u0000char")]
    [InlineData("bell\u0007char")]
    [InlineData("esc\u001bchar")]
    [InlineData("noncharacter\uFFFF")]
    public void BuildPacket_ThrowsOnXmlInvalidControlCharacters(string title)
    {
        var xmp = Sample();
        xmp.Info.Title = title;
        Assert.Throws<InvalidDataException>(() => xmp.BuildPacket());
    }

    [Theory]
    [InlineData("tab\there")]
    [InlineData("line\nbreak")]
    public void BuildPacket_AllowsXmlLegalWhitespaceControls(string title)
    {
        var xmp = Sample();
        xmp.Info.Title = title;
        Assert.NotNull(xmp.BuildPacket());
    }

    [Theory]
    [InlineData("lone", 0xD800, "high")]
    [InlineData("lone", 0xDC00, "low")]
    [InlineData("trailing", 0xD800, "")]
    [InlineData("", 0xDC00, "leadingLow")]
    public void BuildPacket_ThrowsOnMalformedSurrogates(string prefix, int surrogate, string suffix)
    {
        var xmp = Sample();
        xmp.Info.Title = prefix + (char)surrogate + suffix;

        Assert.Throws<InvalidDataException>(() => xmp.BuildPacket());
    }

    [Fact]
    public void BuildPacket_PreservesValidSurrogatePair()
    {
        var xmp = Sample();
        xmp.Info.Title = "emoji\U0001F600done";
        var text = Encoding.UTF8.GetString(xmp.BuildPacket());
        Assert.Contains("emoji\U0001F600done", text);
    }
}
