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
        builder.Append(" </rdf:RDF>\n");
        builder.Append("</x:xmpmeta>\n");

        for (var i = 0; i < 24; i++)
        {
            builder.Append("                                                                                \n");
        }

        builder.Append("<?xpacket end=\"w\"?>");

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    public StreamObject BuildStream()
    {
        var stream = new StreamObject(BuildPacket());
        stream.Dictionary["Type"] = new NameObject("Metadata");
        stream.Dictionary["Subtype"] = new NameObject("XML");
        return stream;
    }

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
