#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// P3: EmbeddedFiles / AF attachments (Factur-X). Pins the public
// DocumentBuilder.Attachments collection with
// Add(string name, byte[] data, AttachmentRelationship relationship, string mimeType)
// and the conforming SaveToStream output: an /EmbeddedFiles name tree under
// Catalog /Names, a Catalog /AF array of /Filespec dictionaries carrying
// /AFRelationship and /EF /F -> an /EmbeddedFile stream (/Subtype mime,
// /Params /Size), byte-identical round-trip of the payload, and - for a
// factur-x.xml attachment - the fx: Factur-X block in the XMP packet.
public class EmbeddedFileAttachmentTests
{
    private const string MissingApi =
        "DocumentBuilder.Attachments with Add(string name, byte[] data, AttachmentRelationship relationship, string mimeType) is missing - P3 attachments are not implemented";

    private static readonly byte[] InvoiceXml = Encoding.UTF8.GetBytes(
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<rsm:CrossIndustryInvoice xmlns:rsm=\"urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100\">\n" +
        "  <rsm:ExchangedDocument><ram:ID>INV-2026-0042</ram:ID></rsm:ExchangedDocument>\n" +
        "  <ram:Name>Facture 42 - café</ram:Name>\n" +
        "</rsm:CrossIndustryInvoice>\n");

    private static PropertyInfo? AttachmentsProperty()
        => typeof(DocumentBuilder).GetProperty("Attachments");

    private static void Attach(DocumentBuilder builder, string name, byte[] data, string relationship, string mimeType)
    {
        var property = AttachmentsProperty();
        Assert.True(property is not null && property.CanRead, MissingApi);

        var attachments = property!.GetValue(builder);
        Assert.True(attachments is not null, MissingApi);

        var add = attachments!.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m =>
                m.Name == "Add"
                && m.GetParameters() is { Length: 4 } p
                && p[0].ParameterType == typeof(string)
                && p[1].ParameterType == typeof(byte[])
                && p[2].ParameterType.IsEnum
                && p[3].ParameterType == typeof(string));
        Assert.True(add is not null, MissingApi);

        var relationshipType = add!.GetParameters()[2].ParameterType;
        Assert.True(Enum.GetNames(relationshipType).Contains(relationship),
            $"AttachmentRelationship is missing the '{relationship}' value");

        add.Invoke(attachments, [name, data, Enum.Parse(relationshipType, relationship), mimeType]);
    }

    private static DocumentBuilder Author(string? conformance = null)
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);

        builder.Info.Title = "Invoice 42";
        builder.Info.Author = "Radzen Ltd";

        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Invoice body", BuildTestSupport.Latin);

        if (conformance is not null)
        {
            builder.Conformance = (PdfAConformance)Enum.Parse(typeof(PdfAConformance), conformance);
        }

        return builder;
    }

    private static DictionaryObject Catalog(DocumentReader reader)
    {
        Assert.True(reader.Trailer.TryGetValue("Root", out var rootObject), "trailer has /Root");
        return Assert.IsType<DictionaryObject>(reader.Resolve(rootObject!));
    }

    private static Dictionary<string, DictionaryObject> EmbeddedFiles(DocumentReader reader)
    {
        var catalog = Catalog(reader);
        Assert.True(catalog.TryGetValue("Names", out var namesObject), "catalog has /Names");
        var names = Assert.IsType<DictionaryObject>(reader.Resolve(namesObject!));
        Assert.True(names.TryGetValue("EmbeddedFiles", out var treeObject), "/Names has /EmbeddedFiles");
        var tree = Assert.IsType<DictionaryObject>(reader.Resolve(treeObject!));

        var result = new Dictionary<string, DictionaryObject>(StringComparer.Ordinal);
        CollectNameTree(reader, tree, result);
        return result;
    }

    private static void CollectNameTree(DocumentReader reader, DictionaryObject node, Dictionary<string, DictionaryObject> acc)
    {
        if (node.TryGetValue("Kids", out var kidsObject) && reader.Resolve(kidsObject!) is ArrayObject kids)
        {
            foreach (var kid in kids)
            {
                if (reader.Resolve(kid) is DictionaryObject child)
                {
                    CollectNameTree(reader, child, acc);
                }
            }

            return;
        }

        Assert.True(node.TryGetValue("Names", out var pairsObject), "name tree leaf has /Names");
        var pairs = Assert.IsType<ArrayObject>(reader.Resolve(pairsObject!));
        Assert.True(pairs.Count % 2 == 0, "/Names array holds key-value pairs");

        var previous = default(string);
        for (var i = 0; i < pairs.Count; i += 2)
        {
            var key = Assert.IsType<StringObject>(reader.Resolve(pairs[i])).Value;
            if (previous is not null)
            {
                Assert.True(string.CompareOrdinal(previous, key) < 0,
                    $"name tree keys must be sorted: '{previous}' before '{key}'");
            }

            previous = key;
            acc[key] = Assert.IsType<DictionaryObject>(reader.Resolve(pairs[i + 1]));
        }
    }

    private static StreamObject EmbeddedStream(DocumentReader reader, DictionaryObject filespec)
    {
        Assert.True(filespec.TryGetValue("EF", out var efObject), "filespec has /EF");
        var ef = Assert.IsType<DictionaryObject>(reader.Resolve(efObject!));
        Assert.True(ef.TryGetValue("F", out var fObject), "/EF has /F");
        return Assert.IsType<StreamObject>(reader.Resolve(fObject!));
    }

    private static byte[] Payload(DocumentReader reader, StreamObject stream)
    {
        if (!stream.Dictionary.TryGetValue("Filter", out var filter) || filter is null)
        {
            return stream.Data.ToArray();
        }

        var name = Assert.IsType<NameObject>(reader.Resolve(filter));
        Assert.Equal("FlateDecode", name.Value);
        return FlateFilter.Decode(stream.Data.ToArray());
    }

    private static string MetadataPacket(DocumentReader reader)
    {
        var catalog = Catalog(reader);
        Assert.True(catalog.TryGetValue("Metadata", out var metadataObject), "catalog has /Metadata");
        var metadata = Assert.IsType<StreamObject>(reader.Resolve(metadataObject!));
        return Encoding.UTF8.GetString(metadata.Data.ToArray());
    }

    [Fact]
    public void Attachments_PublicApiShape()
    {
        var property = AttachmentsProperty();
        Assert.True(property is not null, MissingApi);
        Assert.True(property!.CanRead, "Attachments must be readable");

        var collectionType = property.PropertyType;
        Assert.Equal("AttachmentCollection", collectionType.Name);
        Assert.Equal("Radzen.Documents.Pdf", collectionType.Namespace);
        Assert.True(collectionType.IsPublic, "AttachmentCollection must be public");

        var add = collectionType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m =>
                m.Name == "Add"
                && m.GetParameters() is { Length: 4 } p
                && p[0].ParameterType == typeof(string)
                && p[1].ParameterType == typeof(byte[])
                && p[2].ParameterType.IsEnum
                && p[3].ParameterType == typeof(string));
        Assert.True(add is not null, MissingApi);

        var relationshipType = add!.GetParameters()[2].ParameterType;
        Assert.Equal("AttachmentRelationship", relationshipType.Name);
        Assert.Equal("Radzen.Documents.Pdf", relationshipType.Namespace);
        Assert.True(relationshipType.IsPublic, "AttachmentRelationship must be public");
        Assert.Equal(
            new HashSet<string>(["Source", "Data", "Alternative", "Supplement", "Unspecified"]),
            new HashSet<string>(Enum.GetNames(relationshipType)));
    }

    [Fact]
    public void Attach_EmbeddedFileBytes_RoundTripByteIdentical()
    {
        var builder = Author();
        Attach(builder, "invoice-data.xml", InvoiceXml, "Data", "text/xml");

        var reader = BuildTestSupport.Read(builder);
        var files = EmbeddedFiles(reader);
        Assert.True(files.ContainsKey("invoice-data.xml"), "/EmbeddedFiles name tree contains the attachment name");

        var stream = EmbeddedStream(reader, files["invoice-data.xml"]);
        Assert.Equal("EmbeddedFile", BuildTestSupport.Name(reader, stream.Dictionary, "Type"));
        Assert.Equal("text/xml", BuildTestSupport.Name(reader, stream.Dictionary, "Subtype"));

        Assert.True(stream.Dictionary.TryGetValue("Params", out var paramsObject), "embedded file stream has /Params");
        var parameters = Assert.IsType<DictionaryObject>(reader.Resolve(paramsObject!));
        Assert.Equal(InvoiceXml.Length, BuildTestSupport.Int(parameters, "Size"));

        Assert.Equal(InvoiceXml, Payload(reader, stream));
    }

    [Fact]
    public void Attach_Filespec_HasNamesAndEmbeddedStream()
    {
        var builder = Author();
        Attach(builder, "invoice-data.xml", InvoiceXml, "Data", "text/xml");

        var reader = BuildTestSupport.Read(builder);
        var filespec = EmbeddedFiles(reader)["invoice-data.xml"];

        Assert.Equal("Filespec", BuildTestSupport.Name(reader, filespec, "Type"));
        Assert.Equal("invoice-data.xml", Assert.IsType<StringObject>(reader.Resolve(filespec["F"])).Value);
        Assert.Equal("invoice-data.xml", Assert.IsType<StringObject>(reader.Resolve(filespec["UF"])).Value);
        Assert.Equal("Data", BuildTestSupport.Name(reader, filespec, "AFRelationship"));
    }

    [Fact]
    public void Attach_CatalogAFArray_ReferencesFilespec()
    {
        var builder = Author();
        Attach(builder, "invoice-data.xml", InvoiceXml, "Alternative", "text/xml");

        var reader = BuildTestSupport.Read(builder);
        var catalog = Catalog(reader);
        Assert.True(catalog.TryGetValue("AF", out var afObject), "catalog has /AF");
        var af = Assert.IsType<ArrayObject>(reader.Resolve(afObject!));
        var entry = Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(af)));

        Assert.Equal("Filespec", BuildTestSupport.Name(reader, entry, "Type"));
        Assert.Equal("invoice-data.xml", Assert.IsType<StringObject>(reader.Resolve(entry["F"])).Value);
        Assert.Equal("Alternative", BuildTestSupport.Name(reader, entry, "AFRelationship"));
        Assert.Equal(InvoiceXml, Payload(reader, EmbeddedStream(reader, entry)));
    }

    [Fact]
    public void Attach_MultipleFiles_AllPresentAndTreeSorted()
    {
        var builder = Author();
        var readme = Encoding.UTF8.GetBytes("see invoice-data.xml");
        Attach(builder, "zz-notes.txt", readme, "Supplement", "text/plain");
        Attach(builder, "invoice-data.xml", InvoiceXml, "Data", "text/xml");

        var reader = BuildTestSupport.Read(builder);
        var files = EmbeddedFiles(reader);

        Assert.Equal(2, files.Count);
        Assert.Equal(readme, Payload(reader, EmbeddedStream(reader, files["zz-notes.txt"])));
        Assert.Equal(InvoiceXml, Payload(reader, EmbeddedStream(reader, files["invoice-data.xml"])));

        var catalog = Catalog(reader);
        var af = Assert.IsType<ArrayObject>(reader.Resolve(catalog["AF"]));
        Assert.Equal(2, af.Count);
    }

    [Fact]
    public void FacturX_Attachment_UnderPdfA3B_EmitsFxXmpBlock()
    {
        var builder = Author("PdfA3B");
        Attach(builder, "factur-x.xml", InvoiceXml, "Data", "text/xml");

        var reader = BuildTestSupport.Read(builder);
        var packet = MetadataPacket(reader);

        Assert.Contains("urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#", packet, StringComparison.Ordinal);
        Assert.Contains("<fx:DocumentType>INVOICE</fx:DocumentType>", packet, StringComparison.Ordinal);
        Assert.Contains("<fx:DocumentFileName>factur-x.xml</fx:DocumentFileName>", packet, StringComparison.Ordinal);
        Assert.Contains("<fx:Version>", packet, StringComparison.Ordinal);
        Assert.Contains("<fx:ConformanceLevel>", packet, StringComparison.Ordinal);
        Assert.DoesNotContain("<fx:Version></fx:Version>", packet, StringComparison.Ordinal);
        Assert.DoesNotContain("<fx:ConformanceLevel></fx:ConformanceLevel>", packet, StringComparison.Ordinal);
    }

    [Fact]
    public void FacturX_Attachment_UnderPdfA3B_KeepsConformanceAndEmbedsFile()
    {
        var builder = Author("PdfA3B");
        Attach(builder, "factur-x.xml", InvoiceXml, "Data", "text/xml");

        var reader = BuildTestSupport.Read(builder);

        var packet = MetadataPacket(reader);
        Assert.Contains("<pdfaid:part>3</pdfaid:part>", packet, StringComparison.Ordinal);
        Assert.Contains("<pdfaid:conformance>B</pdfaid:conformance>", packet, StringComparison.Ordinal);

        var filespec = EmbeddedFiles(reader)["factur-x.xml"];
        Assert.Equal("Data", BuildTestSupport.Name(reader, filespec, "AFRelationship"));
        Assert.Equal(InvoiceXml, Payload(reader, EmbeddedStream(reader, filespec)));

        var catalog = Catalog(reader);
        Assert.True(catalog.ContainsKey("AF"), "catalog has /AF");
    }

    [Fact]
    public void NonFacturXAttachment_UnderPdfA3B_HasNoFxBlock()
    {
        var builder = Author("PdfA3B");
        Attach(builder, "invoice-data.xml", InvoiceXml, "Data", "text/xml");

        var packet = MetadataPacket(BuildTestSupport.Read(builder));
        Assert.DoesNotContain("<fx:DocumentFileName>", packet, StringComparison.Ordinal);
    }
}
