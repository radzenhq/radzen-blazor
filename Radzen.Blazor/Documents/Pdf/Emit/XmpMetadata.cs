using System;
using System.Globalization;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Emit;

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

    public DateTimeOffset? CreationDate { get; set; }

    public DateTimeOffset? ModificationDate { get; set; }

    public int? PdfAPart { get; set; }

    public int? PdfARevision { get; set; }

    public string PdfAConformance { get; set; } = "";

    public int? PdfUaPart { get; set; }

    public bool IncludePdfUaExtensionSchema { get; set; }

    public FacturXMetadata? FacturX { get; set; }

    public byte[] BuildPacket()
    {
        var builder = new StringBuilder();
        builder.Append("<?xpacket ").Append(XmpPacketFraming.BeginInstruction).Append("?>\n");
        builder.Append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">\n");
        builder.Append(" <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n");
        builder.Append("  <rdf:Description rdf:about=\"\"\n");
        builder.Append("   xmlns:dc=\"http://purl.org/dc/elements/1.1/\"\n");
        builder.Append("   xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\"\n");
        builder.Append("   xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\"\n");
        builder.Append("   xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\"\n");
        if (PdfUaPart is not null)
        {
            builder.Append("   xmlns:pdfuaid=\"http://www.aiim.org/pdfua/ns/id/\"\n");
        }

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

        if (PdfARevision is { } revision)
        {
            builder.Append("   <pdfaid:rev>").Append(revision).Append("</pdfaid:rev>\n");
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

        if (PdfUaPart is { } uaPart)
        {
            builder.Append("   <pdfuaid:part>").Append(uaPart).Append("</pdfuaid:part>\n");
        }

        builder.Append("  </rdf:Description>\n");

        if (FacturX is not null)
        {
            AppendFacturXExtensionSchema(builder);
        }

        if (IncludePdfUaExtensionSchema)
        {
            builder.Append(PdfUaExtensionSchema);
        }

        builder.Append(" </rdf:RDF>\n");
        builder.Append("</x:xmpmeta>\n");

        XmpPacketFraming.AppendPadding(builder);

        builder.Append("<?xpacket ").Append(XmpPacketFraming.EndInstruction).Append("?>");

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static readonly (string Name, string ValueType, string Category, string Description)[] FacturXProperties =
    [
        ("DocumentFileName", "Text", "external", "The name of the embedded XML document"),
        ("DocumentType", "Text", "external", "The type of the hybrid document in capital letters, e.g. INVOICE or ORDER"),
        ("Version", "Text", "external", "The actual version of the standard applying to the embedded XML document"),
        ("ConformanceLevel", "Text", "external", "The conformance level of the embedded XML document"),
    ];

    private static readonly string PdfUaExtensionSchema = XmpExtensionSchema.Build(
        "PDF/UA identification schema",
        "http://www.aiim.org/pdfua/ns/id/",
        "pdfuaid",
        [("part", "Integer", "internal", "PDF/UA version identifier")]);

    private static void AppendFacturXExtensionSchema(StringBuilder builder)
        => builder.Append(XmpExtensionSchema.Build(
            "Factur-X PDFA Extension Schema", FacturXNamespace, "fx", FacturXProperties));

    public StreamObject BuildStream() => WrapPacket(BuildPacket());

    public static StreamObject WrapPacket(byte[] packet)
    {
        var stream = new StreamObject(packet);
        stream.Dictionary["Type"] = new NameObject("Metadata");
        stream.Dictionary["Subtype"] = new NameObject("XML");
        return stream;
    }

    private static string FormatDate(DateTimeOffset value)
        => value.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

    private static string Escape(string value)
    {
        var builder = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (char.IsHighSurrogate(ch))
            {
                if (i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    builder.Append(ch).Append(value[i + 1]);
                    i++;
                    continue;
                }

                throw InvalidCharacter(ch);
            }

            if (char.IsLowSurrogate(ch) || IsInvalidXmlChar(ch))
            {
                throw InvalidCharacter(ch);
            }

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

    private static InvalidDataException InvalidCharacter(char ch)
        => new(
            $"XMP metadata value contains U+{(int)ch:X4}, which is not a legal XML 1.0 character; remove control characters and unpaired surrogates from the document metadata.");

    // XML 1.0 (2.2) Char production: tab, LF, CR, U+0020..U+D7FF, U+E000..U+FFFD, U+10000..U+10FFFF.
    private static bool IsInvalidXmlChar(char ch)
        => ch < 0x20 ? ch is not ('\t' or '\n' or '\r') : ch is '\uFFFE' or '\uFFFF';
}
