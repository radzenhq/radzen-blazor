using System.Text;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf;

internal sealed class FacturXMetadata
{
    public string DocumentType { get; set; } = "INVOICE";

    public string DocumentFileName { get; set; } = "factur-x.xml";

    public string Version { get; set; } = "1.0";

    public string ConformanceLevel { get; set; } = "BASIC";
}

internal sealed class XmpMetadata
{
    private const string FacturXNamespace = "urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#";

    public DocumentInfo Info { get; set; } = new();

    public string Producer { get; set; } = "";

    public System.DateTimeOffset? CreationDate { get; set; }

    public System.DateTimeOffset? ModificationDate { get; set; }

    public int? PdfAPart { get; set; }

    public string PdfAConformance { get; set; } = "";

    public FacturXMetadata? FacturX { get; set; }

    public byte[] BuildPacket()
    {
        var builder = new StringBuilder();
        builder.Append("<?xpacket begin=\"﻿\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n");
        builder.Append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">\n");
        builder.Append(" <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n");
        builder.Append("  <rdf:Description rdf:about=\"\"\n");
        builder.Append("   xmlns:dc=\"http://purl.org/dc/elements/1.1/\"\n");
        builder.Append("   xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\"\n");
        builder.Append("   xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\"\n");
        builder.Append("   xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\"\n");
        builder.Append("   xmlns:fx=\"").Append(FacturXNamespace).Append("\">\n");

        if (Info.Title is { } title)
        {
            builder.Append("   <dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">")
                .Append(Escape(title))
                .Append("</rdf:li></rdf:Alt></dc:title>\n");
        }

        if (Info.Author is { } author)
        {
            builder.Append("   <dc:creator><rdf:Seq><rdf:li>")
                .Append(Escape(author))
                .Append("</rdf:li></rdf:Seq></dc:creator>\n");
        }

        if (Info.Subject is { } subject)
        {
            builder.Append("   <dc:description><rdf:Alt><rdf:li xml:lang=\"x-default\">")
                .Append(Escape(subject))
                .Append("</rdf:li></rdf:Alt></dc:description>\n");
        }

        if (Info.Creator is { } creator)
        {
            builder.Append("   <xmp:CreatorTool>").Append(Escape(creator)).Append("</xmp:CreatorTool>\n");
        }

        if (CreationDate is { } created)
        {
            builder.Append("   <xmp:CreateDate>").Append(FormatDate(created)).Append("</xmp:CreateDate>\n");
        }

        if (ModificationDate is { } modified)
        {
            builder.Append("   <xmp:ModifyDate>").Append(FormatDate(modified)).Append("</xmp:ModifyDate>\n");
        }

        if (Info.Keywords is { } keywords)
        {
            builder.Append("   <pdf:Keywords>").Append(Escape(keywords)).Append("</pdf:Keywords>\n");
        }

        builder.Append("   <pdf:Producer>").Append(Escape(Producer)).Append("</pdf:Producer>\n");

        if (PdfAPart is { } part)
        {
            builder.Append("   <pdfaid:part>").Append(part).Append("</pdfaid:part>\n");
        }

        if (!string.IsNullOrEmpty(PdfAConformance))
        {
            builder.Append("   <pdfaid:conformance>")
                .Append(Escape(PdfAConformance))
                .Append("</pdfaid:conformance>\n");
        }

        if (FacturX is { } fx)
        {
            builder.Append("   <fx:DocumentType>").Append(Escape(fx.DocumentType)).Append("</fx:DocumentType>\n");
            builder.Append("   <fx:DocumentFileName>")
                .Append(Escape(fx.DocumentFileName))
                .Append("</fx:DocumentFileName>\n");
            builder.Append("   <fx:Version>").Append(Escape(fx.Version)).Append("</fx:Version>\n");
            builder.Append("   <fx:ConformanceLevel>")
                .Append(Escape(fx.ConformanceLevel))
                .Append("</fx:ConformanceLevel>\n");
        }

        builder.Append("  </rdf:Description>\n");

        if (FacturX is not null)
        {
            AppendFacturXExtensionSchema(builder);
        }

        builder.Append(" </rdf:RDF>\n");
        builder.Append("</x:xmpmeta>\n");

        for (var i = 0; i < 24; i++)
        {
            builder.Append("                                                                                \n");
        }

        builder.Append("<?xpacket end=\"w\"?>");

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static readonly (string Name, string Description)[] FacturXProperties =
    [
        ("DocumentFileName", "The name of the embedded XML document"),
        ("DocumentType", "The type of the hybrid document in capital letters, e.g. INVOICE or ORDER"),
        ("Version", "The actual version of the standard applying to the embedded XML document"),
        ("ConformanceLevel", "The conformance level of the embedded XML document"),
    ];

    // PDF/A 6.6.2.3.1: properties outside the XMP predefined schemas must be
    // declared through a pdfaExtension extension schema.
    private static void AppendFacturXExtensionSchema(StringBuilder builder)
    {
        builder.Append("  <rdf:Description rdf:about=\"\"\n");
        builder.Append("   xmlns:pdfaExtension=\"http://www.aiim.org/pdfa/ns/extension/\"\n");
        builder.Append("   xmlns:pdfaSchema=\"http://www.aiim.org/pdfa/ns/schema#\"\n");
        builder.Append("   xmlns:pdfaProperty=\"http://www.aiim.org/pdfa/ns/property#\">\n");
        builder.Append("   <pdfaExtension:schemas>\n");
        builder.Append("    <rdf:Bag>\n");
        builder.Append("     <rdf:li rdf:parseType=\"Resource\">\n");
        builder.Append("      <pdfaSchema:schema>Factur-X PDFA Extension Schema</pdfaSchema:schema>\n");
        builder.Append("      <pdfaSchema:namespaceURI>").Append(FacturXNamespace).Append("</pdfaSchema:namespaceURI>\n");
        builder.Append("      <pdfaSchema:prefix>fx</pdfaSchema:prefix>\n");
        builder.Append("      <pdfaSchema:property>\n");
        builder.Append("       <rdf:Seq>\n");

        foreach (var (name, description) in FacturXProperties)
        {
            builder.Append("        <rdf:li rdf:parseType=\"Resource\">\n");
            builder.Append("         <pdfaProperty:name>").Append(name).Append("</pdfaProperty:name>\n");
            builder.Append("         <pdfaProperty:valueType>Text</pdfaProperty:valueType>\n");
            builder.Append("         <pdfaProperty:category>external</pdfaProperty:category>\n");
            builder.Append("         <pdfaProperty:description>").Append(description).Append("</pdfaProperty:description>\n");
            builder.Append("        </rdf:li>\n");
        }

        builder.Append("       </rdf:Seq>\n");
        builder.Append("      </pdfaSchema:property>\n");
        builder.Append("     </rdf:li>\n");
        builder.Append("    </rdf:Bag>\n");
        builder.Append("   </pdfaExtension:schemas>\n");
        builder.Append("  </rdf:Description>\n");
    }

    public StreamObject BuildStream()
    {
        var stream = new StreamObject(BuildPacket());
        stream.Dictionary["Type"] = new NameObject("Metadata");
        stream.Dictionary["Subtype"] = new NameObject("XML");
        return stream;
    }

    // XMP dates are ISO 8601 (yyyy-MM-ddThh:mm:ss+hh:mm); the caller-supplied offset
    // is preserved verbatim so the value round-trips without reading any clock.
    private static string FormatDate(System.DateTimeOffset value)
        => value.ToString("yyyy-MM-dd'T'HH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture);

    private static string Escape(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '&':
                    builder.Append("&amp;");
                    break;
                case '<':
                    builder.Append("&lt;");
                    break;
                case '>':
                    builder.Append("&gt;");
                    break;
                case '"':
                    builder.Append("&quot;");
                    break;
                case '\'':
                    builder.Append("&apos;");
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }

        return builder.ToString();
    }
}
