#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Contract for the XMP metadata packet + /Metadata stream (PDF/A-3 + Factur-X layer).
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

    [Fact]
    public void DcTitle_FromDocumentInfoTitle_XmlEscaped()
    {
        var title = PacketXml().Descendants(Dc + "title").FirstOrDefault();
        Assert.NotNull(title);
        Assert.Contains("Q3 Invoice & Receipt", title!.Value);
    }

    [Fact]
    public void DcCreator_FromDocumentInfoAuthor()
    {
        var creator = PacketXml().Descendants(Dc + "creator").FirstOrDefault();
        Assert.NotNull(creator);
        Assert.Contains("Radzen Ltd", creator!.Value);
    }

    [Fact]
    public void DcDescription_FromDocumentInfoSubject()
    {
        var description = PacketXml().Descendants(Dc + "description").FirstOrDefault();
        Assert.NotNull(description);
        Assert.Contains("Invoice for services rendered", description!.Value);
    }

    [Fact]
    public void XmpCreatorTool_FromDocumentInfoCreator()
    {
        Assert.Equal("Radzen Blazor Studio", ElementOrAttr(PacketXml(), Xmp, "CreatorTool"));
    }

    [Fact]
    public void PdfKeywords_FromDocumentInfoKeywords()
    {
        Assert.Equal("invoice, facturx, pdfa", ElementOrAttr(PacketXml(), Pdf, "Keywords"));
    }

    [Fact]
    public void PdfProducer_FromProducer()
    {
        Assert.Equal("Radzen PDF 1.0", ElementOrAttr(PacketXml(), Pdf, "Producer"));
    }

    [Fact]
    public void PdfaId_Part_IsThree()
    {
        Assert.Equal("3", ElementOrAttr(PacketXml(), PdfaId, "part"));
    }

    [Fact]
    public void PdfaId_Conformance_IsB()
    {
        Assert.Equal("B", ElementOrAttr(PacketXml(), PdfaId, "conformance"));
    }

    [Fact]
    public void FacturX_Namespace_IsFacturXUrn()
    {
        var element = FacturX(PacketXml(), "DocumentType");
        Assert.NotNull(element);
        Assert.Contains("factur-x", element!.Name.Namespace.NamespaceName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FacturX_DocumentType()
    {
        Assert.Equal("INVOICE", FacturX(PacketXml(), "DocumentType")?.Value.Trim());
    }

    [Fact]
    public void FacturX_DocumentFileName()
    {
        Assert.Equal("factur-x.xml", FacturX(PacketXml(), "DocumentFileName")?.Value.Trim());
    }

    [Fact]
    public void FacturX_Version()
    {
        Assert.Equal("1.0", FacturX(PacketXml(), "Version")?.Value.Trim());
    }

    [Fact]
    public void Envelope_BeginProcessingInstruction()
    {
        Assert.StartsWith("<?xpacket begin", PacketText().TrimStart());
    }

    [Fact]
    public void Envelope_EndProcessingInstruction()
    {
        Assert.Contains("<?xpacket end", PacketText());
    }

    [Fact]
    public void Envelope_PaddingWhitespacePresentBeforeEnd()
    {
        var text = PacketText();
        var afterMeta = text.IndexOf("</x:xmpmeta>", StringComparison.Ordinal) + "</x:xmpmeta>".Length;
        var beforeEnd = text.IndexOf("<?xpacket end", StringComparison.Ordinal);
        Assert.True(beforeEnd > afterMeta);
        var padding = text[afterMeta..beforeEnd];
        Assert.True(string.IsNullOrWhiteSpace(padding));
        Assert.True(padding.Length >= 100, $"expected padding >= 100 whitespace chars, got {padding.Length}");
    }

    [Fact]
    public void Stream_TypeIsMetadata()
    {
        var stream = Sample().BuildStream();
        Assert.Equal("Metadata", ((NameObject)stream.Dictionary["Type"]).Value);
    }

    [Fact]
    public void Stream_SubtypeIsXml()
    {
        var stream = Sample().BuildStream();
        Assert.Equal("XML", ((NameObject)stream.Dictionary["Subtype"]).Value);
    }

    [Fact]
    public void Stream_HasNoFilter()
    {
        var stream = Sample().BuildStream();
        Assert.False(stream.Dictionary.ContainsKey("Filter"));
    }

    [Fact]
    public void Stream_DataEqualsPacket()
    {
        var sample = Sample();
        Assert.Equal(sample.BuildPacket(), sample.BuildStream().Data);
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
}
