#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class FacturXXmpExtensionSchemaTests
{
    private const string FxNamespace = "urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#";

    private static readonly XNamespace Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    private static readonly XNamespace PdfaExtension = "http://www.aiim.org/pdfa/ns/extension/";
    private static readonly XNamespace PdfaSchema = "http://www.aiim.org/pdfa/ns/schema#";
    private static readonly XNamespace PdfaProperty = "http://www.aiim.org/pdfa/ns/property#";
    private static readonly XNamespace PdfaId = "http://www.aiim.org/pdfa/ns/id/";

    private static readonly string[] FxProperties =
        ["DocumentType", "DocumentFileName", "Version", "ConformanceLevel"];

    private static readonly byte[] InvoiceXml = Encoding.UTF8.GetBytes(
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<rsm:CrossIndustryInvoice xmlns:rsm=\"urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100\"/>\n");

    private static XDocument BuildMetadata()
    {
        var document = new Document();
        var renderer = new DocumentRenderer { Conformance = PdfAConformance.PdfA3B };
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "Invoice 42";

        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Invoice body", BuildTestSupport.Latin);
        var rendered = renderer.Render(document);
        rendered.Attachments.Add("factur-x.xml", InvoiceXml, AttachmentRelationship.Data, "text/xml");

        return XmpPacket(rendered);
    }

    private static XDocument XmpPacket(PortableDocument document)
    {
        var emission = Emit(document);
        var reference = Shaped("catalog", @"/Metadata (\d+) 0 R", Line(emission, "/Type /Catalog"));
        var metadata = IndirectObject(emission, reference.Groups[1].Value);

        const string opening = ">>\nstream\n";
        var start = metadata.IndexOf(opening, StringComparison.Ordinal);
        var end = metadata.LastIndexOf("\nendstream", StringComparison.Ordinal);
        Assert.True(
            start >= 0 && end > start,
            $"The /Metadata object carries no stream payload.\n/Metadata object:\n{Excerpt(metadata)}");

        var payload = metadata[(start + opening.Length)..end];
        return XDocument.Parse(Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(payload)));
    }

    private static string? Value(XElement scope, XName name)
        => scope.Element(name)?.Value ?? scope.Attribute(name)?.Value;

    private static XElement FxSchemaEntry(XDocument xmp)
    {
        var schemasElements = xmp.Descendants(PdfaExtension + "schemas").ToList();
        Assert.True(schemasElements.Count > 0,
            "XMP must contain a pdfaExtension:schemas block declaring the fx: Factur-X extension schema");

        var entries = schemasElements
            .SelectMany(s => s.Descendants(Rdf + "li"))
            .Where(li => Value(li, PdfaSchema + "namespaceURI") == FxNamespace)
            .ToList();

        Assert.True(entries.Count == 1,
            $"pdfaExtension:schemas must declare exactly one schema entry for {FxNamespace}, found {entries.Count}");
        return entries[0];
    }

    [Fact]
    public void FacturX_Xmp_DeclaresExtensionSchemaForFxNamespace()
    {
        var entry = FxSchemaEntry(BuildMetadata());

        Assert.Equal("fx", Value(entry, PdfaSchema + "prefix"));
        Assert.Equal("Factur-X PDFA Extension Schema", Value(entry, PdfaSchema + "schema"));
    }

    [Fact]
    public void FacturX_Xmp_ExtensionSchemaDescribesAllFourFxProperties()
    {
        var entry = FxSchemaEntry(BuildMetadata());

        var propertyContainer = entry.Element(PdfaSchema + "property");
        Assert.True(propertyContainer is not null, "fx schema entry must have pdfaSchema:property");

        var byName = new Dictionary<string, XElement>(StringComparer.Ordinal);
        foreach (var li in propertyContainer!.Descendants(Rdf + "li"))
        {
            var name = Value(li, PdfaProperty + "name");
            if (name is not null)
            {
                byName[name] = li;
            }
        }

        foreach (var name in FxProperties)
        {
            Assert.True(byName.TryGetValue(name, out var li),
                $"extension schema must describe fx:{name}");
            Assert.Equal("Text", Value(li!, PdfaProperty + "valueType"));
            Assert.Equal("external", Value(li!, PdfaProperty + "category"));
            Assert.False(string.IsNullOrEmpty(Value(li!, PdfaProperty + "description")),
                $"fx:{name} must have a pdfaProperty:description");
        }
    }

    [Fact]
    public void FacturX_Xmp_DeclaresProfileSetOnAttachment()
    {
        var document = new Document();
        var renderer = new DocumentRenderer { Conformance = PdfAConformance.PdfA3B };
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "Invoice 42";
        BuildTestSupport.AddText(document.Sections.Add(), "Invoice body", BuildTestSupport.Latin);

        var rendered = renderer.Render(document);
        var attachment = rendered.Attachments.Add("factur-x.xml", InvoiceXml, AttachmentRelationship.Data, "text/xml");
        attachment.FacturX = new FacturXProfile
        {
            DocumentType = "INVOICE",
            Version = "1.0",
            ConformanceLevel = "EN 16931",
        };

        var xmp = XmpPacket(rendered);

        XNamespace fx = FxNamespace;
        var descriptions = xmp.Descendants(Rdf + "Description").ToList();
        Assert.Equal("EN 16931",
            descriptions.Select(d => Value(d, fx + "ConformanceLevel")).Single(v => v is not null));
    }

    [Fact]
    public void FacturX_Xmp_ThrowsWhenAttachmentProfileFieldIsEmpty()
    {
        var document = new Document();
        var renderer = new DocumentRenderer { Conformance = PdfAConformance.PdfA3B };
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "Invoice 42";
        BuildTestSupport.AddText(document.Sections.Add(), "Invoice body", BuildTestSupport.Latin);

        var rendered = renderer.Render(document);
        var attachment = rendered.Attachments.Add("factur-x.xml", InvoiceXml, AttachmentRelationship.Data, "text/xml");
        attachment.FacturX = new FacturXProfile { ConformanceLevel = "" };

        Assert.Throws<InvalidOperationException>(rendered.ToArray);
    }

    [Fact]
    public void FacturX_Xmp_KeepsPdfaidAndFxValuesIntact()
    {
        var xmp = BuildMetadata();
        var descriptions = xmp.Descendants(Rdf + "Description").ToList();

        Assert.Equal("3", descriptions.Select(d => Value(d, PdfaId + "part")).Single(v => v is not null));
        Assert.Equal("B", descriptions.Select(d => Value(d, PdfaId + "conformance")).Single(v => v is not null));

        XNamespace fx = FxNamespace;
        Assert.Equal("factur-x.xml",
            descriptions.Select(d => Value(d, fx + "DocumentFileName")).Single(v => v is not null));
        Assert.Equal("INVOICE",
            descriptions.Select(d => Value(d, fx + "DocumentType")).Single(v => v is not null));
    }
}
