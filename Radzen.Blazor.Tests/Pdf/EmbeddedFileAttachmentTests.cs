#nullable enable
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class EmbeddedFileAttachmentTests
{
    private static readonly byte[] InvoiceXml = Encoding.UTF8.GetBytes(
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<rsm:CrossIndustryInvoice xmlns:rsm=\"urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100\">\n" +
        "  <rsm:ExchangedDocument><ram:ID>INV-2026-0042</ram:ID></rsm:ExchangedDocument>\n" +
        "  <ram:Name>Facture 42 - café</ram:Name>\n" +
        "</rsm:CrossIndustryInvoice>\n");

    private static PortableDocument Authored(PdfAConformance? conformance = null)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        document.Info.Title = "Invoice 42";
        document.Info.Author = "Radzen Ltd";

        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Invoice body", BuildTestSupport.Latin);

        var renderer = new DocumentRenderer();
        if (conformance is not null)
        {
            renderer.Conformance = conformance.Value;
        }

        return renderer.Render(document);
    }

    private static string Catalog(string emission) => Line(emission, "/Type /Catalog");

    private static string NameTree(string emission)
        => IndirectObject(emission, Shaped("catalog", @"/EmbeddedFiles (\d+) 0 R", Catalog(emission)).Groups[1].Value);

    private static string Filespec(string emission, string name)
        => IndirectObject(emission, FilespecNumber(emission, name));

    private static string FilespecNumber(string emission, string name)
        => Shaped(
            $"/EmbeddedFiles name tree entry ({name})",
            $@"\({Regex.Escape(name)}\) (\d+) 0 R",
            NameTree(emission)).Groups[1].Value;

    private static string EmbeddedFileNumber(string filespec)
        => Shaped("filespec /EF", @"/EF << /F (\d+) 0 R /UF (\d+) 0 R >>", filespec).Groups[1].Value;

    private static string MetadataPacket(string emission)
        => IndirectObject(emission, Shaped("catalog", @"/Metadata (\d+) 0 R", Catalog(emission)).Groups[1].Value);

    private static byte[] EmbeddedPayload(byte[] pdf, string number)
    {
        var reader = DocumentReader.Parse(pdf);
        var stream = Assert.IsType<StreamObject>(reader.Resolve(new ReferenceObject(int.Parse(number), 0)));
        Assert.Equal("FlateDecode", Assert.IsType<NameObject>(reader.Resolve(stream.Dictionary["Filter"])).Value);
        return FlateFilter.Decode(stream.Data.ToArray());
    }

    [Fact]
    public void Attach_EmbeddedFileBytes_RoundTripByteIdentical()
    {
        var rendered = Authored();
        rendered.Attachments.Add("invoice-data.xml", InvoiceXml, AttachmentRelationship.Data, "text/xml");

        var pdf = rendered.ToArray();
        var emission = Encoding.Latin1.GetString(pdf);
        var number = EmbeddedFileNumber(Filespec(emission, "invoice-data.xml"));
        var stream = IndirectObject(emission, number);

        Carries("embedded file stream", "/Type /EmbeddedFile", stream);
        Carries("embedded file stream", "/Subtype /text#2Fxml", stream);
        Assert.Equal(InvoiceXml.Length, NumberIn(stream, "Size"));
        Assert.Equal(InvoiceXml, EmbeddedPayload(pdf, number));
    }

    [Fact]
    public void Attach_Filespec_HasNamesAndEmbeddedStream()
    {
        var rendered = Authored();
        rendered.Attachments.Add("invoice-data.xml", InvoiceXml, AttachmentRelationship.Data, "text/xml");

        var filespec = Filespec(Emit(rendered), "invoice-data.xml");

        Carries("filespec", "/Type /Filespec", filespec);
        Carries("filespec", "/F (invoice-data.xml)", filespec);
        Carries("filespec", "/UF (invoice-data.xml)", filespec);
        Carries("filespec", "/AFRelationship /Data", filespec);
        EmbeddedFileNumber(filespec);
    }

    [Fact]
    public void Attach_CatalogAFArray_ReferencesFilespec()
    {
        var rendered = Authored();
        rendered.Attachments.Add("invoice-data.xml", InvoiceXml, AttachmentRelationship.Alternative, "text/xml");

        var pdf = rendered.ToArray();
        var emission = Encoding.Latin1.GetString(pdf);
        var entry = IndirectObject(emission, References("catalog", "AF", 1, Catalog(emission))[0]);

        Carries("catalog /AF entry", "/Type /Filespec", entry);
        Carries("catalog /AF entry", "/F (invoice-data.xml)", entry);
        Carries("catalog /AF entry", "/AFRelationship /Alternative", entry);
        Assert.Equal(InvoiceXml, EmbeddedPayload(pdf, EmbeddedFileNumber(entry)));
    }

    [Fact]
    public void Attach_MultipleFiles_AllPresentAndTreeSorted()
    {
        var rendered = Authored();
        var readme = Encoding.UTF8.GetBytes("see invoice-data.xml");
        rendered.Attachments.Add("zz-notes.txt", readme, AttachmentRelationship.Supplement, "text/plain");
        rendered.Attachments.Add("invoice-data.xml", InvoiceXml, AttachmentRelationship.Data, "text/xml");

        var pdf = rendered.ToArray();
        var emission = Encoding.Latin1.GetString(pdf);

        Shaped(
            "/EmbeddedFiles name tree",
            @"/Names \[\(invoice-data\.xml\) \d+ 0 R \(zz-notes\.txt\) \d+ 0 R\]",
            NameTree(emission));

        Assert.Equal(
            readme,
            EmbeddedPayload(pdf, EmbeddedFileNumber(Filespec(emission, "zz-notes.txt"))));
        Assert.Equal(
            InvoiceXml,
            EmbeddedPayload(pdf, EmbeddedFileNumber(Filespec(emission, "invoice-data.xml"))));

        References("catalog", "AF", 2, Catalog(emission));
    }

    [Fact]
    public void FacturX_Attachment_UnderPdfA3B_EmitsFxXmpBlock()
    {
        var rendered = Authored(PdfAConformance.PdfA3B);
        rendered.Attachments.Add("factur-x.xml", InvoiceXml, AttachmentRelationship.Data, "text/xml");

        var packet = MetadataPacket(Emit(rendered));

        Carries("catalog /Metadata stream", "urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#", packet);
        Carries("catalog /Metadata stream", "<fx:DocumentType>INVOICE</fx:DocumentType>", packet);
        Carries("catalog /Metadata stream", "<fx:DocumentFileName>factur-x.xml</fx:DocumentFileName>", packet);
        Carries("catalog /Metadata stream", "<fx:Version>", packet);
        Carries("catalog /Metadata stream", "<fx:ConformanceLevel>", packet);
        Lacks("catalog /Metadata stream", "<fx:Version></fx:Version>", packet);
        Lacks("catalog /Metadata stream", "<fx:ConformanceLevel></fx:ConformanceLevel>", packet);
    }

    [Fact]
    public void FacturX_Attachment_UnderPdfA3B_KeepsConformanceAndEmbedsFile()
    {
        var rendered = Authored(PdfAConformance.PdfA3B);
        rendered.Attachments.Add("factur-x.xml", InvoiceXml, AttachmentRelationship.Data, "text/xml");

        var pdf = rendered.ToArray();
        var emission = Encoding.Latin1.GetString(pdf);

        var packet = MetadataPacket(emission);
        Carries("catalog /Metadata stream", "<pdfaid:part>3</pdfaid:part>", packet);
        Carries("catalog /Metadata stream", "<pdfaid:conformance>B</pdfaid:conformance>", packet);

        var filespec = Filespec(emission, "factur-x.xml");
        Carries("filespec", "/AFRelationship /Data", filespec);
        Assert.Equal(InvoiceXml, EmbeddedPayload(pdf, EmbeddedFileNumber(filespec)));

        Carries("catalog", "/AF [", Catalog(emission));
    }

    [Fact]
    public void NonFacturXAttachment_UnderPdfA3B_HasNoFxBlock()
    {
        var rendered = Authored(PdfAConformance.PdfA3B);
        rendered.Attachments.Add("invoice-data.xml", InvoiceXml, AttachmentRelationship.Data, "text/xml");

        Lacks("catalog /Metadata stream", "<fx:DocumentFileName>", MetadataPacket(Emit(rendered)));
    }
}
