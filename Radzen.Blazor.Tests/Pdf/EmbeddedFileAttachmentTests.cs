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
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class EmbeddedFileAttachmentTests
{
    private static readonly byte[] InvoiceXml = Encoding.UTF8.GetBytes(
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<rsm:CrossIndustryInvoice xmlns:rsm=\"urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100\">\n" +
        "  <rsm:ExchangedDocument><ram:ID>INV-2026-0042</ram:ID></rsm:ExchangedDocument>\n" +
        "  <ram:Name>Facture 42 - café</ram:Name>\n" +
        "</rsm:CrossIndustryInvoice>\n");

    private static (Document Document, DocumentRenderer Renderer) Author(PdfAConformance? conformance = null)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        document.Info.Title = "Invoice 42";
        document.Info.Author = "Radzen Ltd";

        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Invoice body", BuildTestSupport.Latin);

        var builderRenderer = new DocumentRenderer();
        if (conformance is not null)
        {
            builderRenderer.Conformance = conformance.Value;
        }

        return (document, builderRenderer);
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
    public void Attach_EmbeddedFileBytes_RoundTripByteIdentical()
    {
        var (document, builderRenderer) = Author();
        builderRenderer.Attachments.Add("invoice-data.xml", InvoiceXml, AttachmentRelationship.Data, "text/xml");

        var reader = BuildTestSupport.Read(document, builderRenderer);
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
        var (document, builderRenderer) = Author();
        builderRenderer.Attachments.Add("invoice-data.xml", InvoiceXml, AttachmentRelationship.Data, "text/xml");

        var reader = BuildTestSupport.Read(document, builderRenderer);
        var filespec = EmbeddedFiles(reader)["invoice-data.xml"];

        Assert.Equal("Filespec", BuildTestSupport.Name(reader, filespec, "Type"));
        Assert.Equal("invoice-data.xml", Assert.IsType<StringObject>(reader.Resolve(filespec["F"])).Value);
        Assert.Equal("invoice-data.xml", Assert.IsType<StringObject>(reader.Resolve(filespec["UF"])).Value);
        Assert.Equal("Data", BuildTestSupport.Name(reader, filespec, "AFRelationship"));
    }

    [Fact]
    public void Attach_CatalogAFArray_ReferencesFilespec()
    {
        var (document, builderRenderer) = Author();
        builderRenderer.Attachments.Add("invoice-data.xml", InvoiceXml, AttachmentRelationship.Alternative, "text/xml");

        var reader = BuildTestSupport.Read(document, builderRenderer);
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
        var (document, builderRenderer) = Author();
        var readme = Encoding.UTF8.GetBytes("see invoice-data.xml");
        builderRenderer.Attachments.Add("zz-notes.txt", readme, AttachmentRelationship.Supplement, "text/plain");
        builderRenderer.Attachments.Add("invoice-data.xml", InvoiceXml, AttachmentRelationship.Data, "text/xml");

        var reader = BuildTestSupport.Read(document, builderRenderer);
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
        var (document, builderRenderer) = Author(PdfAConformance.PdfA3B);
        builderRenderer.Attachments.Add("factur-x.xml", InvoiceXml, AttachmentRelationship.Data, "text/xml");

        var reader = BuildTestSupport.Read(document, builderRenderer);
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
        var (document, builderRenderer) = Author(PdfAConformance.PdfA3B);
        builderRenderer.Attachments.Add("factur-x.xml", InvoiceXml, AttachmentRelationship.Data, "text/xml");

        var reader = BuildTestSupport.Read(document, builderRenderer);

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
        var (document, builderRenderer) = Author(PdfAConformance.PdfA3B);
        builderRenderer.Attachments.Add("invoice-data.xml", InvoiceXml, AttachmentRelationship.Data, "text/xml");

        var packet = MetadataPacket(BuildTestSupport.Read(document, builderRenderer));
        Assert.DoesNotContain("<fx:DocumentFileName>", packet, StringComparison.Ordinal);
    }
}
