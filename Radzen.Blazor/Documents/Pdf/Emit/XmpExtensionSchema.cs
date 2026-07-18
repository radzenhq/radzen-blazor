using System.Collections.Generic;
using System.Text;

namespace Radzen.Documents.Pdf.Emit;

internal static class XmpExtensionSchema
{
    // PDF/A 6.6.2.3.1: properties outside the XMP predefined schemas need a pdfaExtension schema.
    public static string Build(
        string schema,
        string namespaceUri,
        string prefix,
        IReadOnlyList<(string Name, string ValueType, string Category, string Description)> properties)
    {
        var builder = new StringBuilder();
        builder.Append("  <rdf:Description rdf:about=\"\"\n");
        builder.Append("   xmlns:pdfaExtension=\"http://www.aiim.org/pdfa/ns/extension/\"\n");
        builder.Append("   xmlns:pdfaSchema=\"http://www.aiim.org/pdfa/ns/schema#\"\n");
        builder.Append("   xmlns:pdfaProperty=\"http://www.aiim.org/pdfa/ns/property#\">\n");
        builder.Append("   <pdfaExtension:schemas>\n");
        builder.Append("    <rdf:Bag>\n");
        builder.Append("     <rdf:li rdf:parseType=\"Resource\">\n");
        builder.Append("      <pdfaSchema:schema>").Append(schema).Append("</pdfaSchema:schema>\n");
        builder.Append("      <pdfaSchema:namespaceURI>").Append(namespaceUri).Append("</pdfaSchema:namespaceURI>\n");
        builder.Append("      <pdfaSchema:prefix>").Append(prefix).Append("</pdfaSchema:prefix>\n");
        builder.Append("      <pdfaSchema:property>\n");
        builder.Append("       <rdf:Seq>\n");

        foreach (var (name, valueType, category, description) in properties)
        {
            builder.Append("        <rdf:li rdf:parseType=\"Resource\">\n");
            builder.Append("         <pdfaProperty:name>").Append(name).Append("</pdfaProperty:name>\n");
            builder.Append("         <pdfaProperty:valueType>").Append(valueType).Append("</pdfaProperty:valueType>\n");
            builder.Append("         <pdfaProperty:category>").Append(category).Append("</pdfaProperty:category>\n");
            builder.Append("         <pdfaProperty:description>").Append(description).Append("</pdfaProperty:description>\n");
            builder.Append("        </rdf:li>\n");
        }

        builder.Append("       </rdf:Seq>\n");
        builder.Append("      </pdfaSchema:property>\n");
        builder.Append("     </rdf:li>\n");
        builder.Append("    </rdf:Bag>\n");
        builder.Append("   </pdfaExtension:schemas>\n");
        builder.Append("  </rdf:Description>\n");
        return builder.ToString();
    }
}
