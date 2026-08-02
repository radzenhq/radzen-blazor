#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class AttachmentSpecConformanceTests
{
    private static readonly byte[] InvoiceXml = Encoding.UTF8.GetBytes(
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<rsm:CrossIndustryInvoice xmlns:rsm=\"urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100\">\n" +
        "  <rsm:ExchangedDocument><ram:ID>INV-2026-0042</ram:ID></rsm:ExchangedDocument>\n" +
        "</rsm:CrossIndustryInvoice>\n");

    private static readonly byte[] BinaryPayload = [0x00, 0x01, 0x02, 0xFF, 0xFE, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];

    private static readonly DateTimeOffset XmlModified = new(2026, 3, 15, 10, 30, 45, TimeSpan.FromHours(2));

    private static (Document Document, DocumentRenderer Renderer) Author(PdfAConformance? conformance = null)
    {
        var document = new Document();
        var builderRenderer = new DocumentRenderer();
        BuildTestSupport.RegisterLatin(document);

        document.Info.Title = "Invoice 42";
        document.Info.Author = "Radzen Ltd";

        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Invoice body", BuildTestSupport.Latin);

        if (conformance is not null)
        {
            builderRenderer.Conformance = conformance.Value;
        }

        return (document, builderRenderer);
    }

    private static PortableDocument AuthorWithBothAttachments(PdfAConformance? conformance = null)
    {
        var (document, builderRenderer) = Author(conformance);
        var rendered = builderRenderer.Render(document);

        var xml = rendered.Attachments.Add("factur-x.xml", InvoiceXml, AttachmentRelationship.Alternative, "text/xml");
        xml.Description = "Factur-X invoice data";
        xml.ModificationDate = XmlModified;

        rendered.Attachments.Add("scan.bin", BinaryPayload, AttachmentRelationship.Supplement, "application/octet-stream");

        return rendered;
    }

    private static DocumentReader ReadAuthored(PortableDocument rendered)
        => DocumentReader.Parse(rendered.ToArray());

    private static byte[] RenderAuthored(PortableDocument rendered) => rendered.ToArray();

    private static DictionaryObject Catalog(DocumentReader reader)
    {
        Assert.True(reader.Trailer.TryGetValue("Root", out var rootObject), "trailer has /Root");
        return Assert.IsType<DictionaryObject>(reader.Resolve(rootObject!));
    }

    private static Dictionary<string, DictionaryObject> EmbeddedFiles(DocumentReader reader)
    {
        var catalog = Catalog(reader);
        var names = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Names"]));
        var tree = Assert.IsType<DictionaryObject>(reader.Resolve(names["EmbeddedFiles"]));
        var pairs = Assert.IsType<ArrayObject>(reader.Resolve(tree["Names"]));
        Assert.True(pairs.Count % 2 == 0, "/Names array holds key-value pairs");

        var result = new Dictionary<string, DictionaryObject>(StringComparer.Ordinal);
        for (var i = 0; i < pairs.Count; i += 2)
        {
            var key = Assert.IsType<StringObject>(reader.Resolve(pairs[i])).Value;
            result[key] = Assert.IsType<DictionaryObject>(reader.Resolve(pairs[i + 1]));
        }

        return result;
    }

    private static StreamObject EmbeddedStream(DocumentReader reader, DictionaryObject filespec, string key = "F")
    {
        var ef = Assert.IsType<DictionaryObject>(reader.Resolve(filespec["EF"]));
        Assert.True(ef.TryGetValue(key, out var streamObject), $"/EF has /{key}");
        return Assert.IsType<StreamObject>(reader.Resolve(streamObject!));
    }

    private static byte[] Payload(DocumentReader reader, StreamObject stream)
    {
        if (!stream.Dictionary.TryGetValue("Filter", out var filter) || filter is null)
        {
            return stream.Data.ToArray();
        }

        Assert.Equal("FlateDecode", Assert.IsType<NameObject>(reader.Resolve(filter)).Value);
        return FlateFilter.Decode(stream.Data.ToArray());
    }

    private static string ParamsModDate(DocumentReader reader, StreamObject stream)
    {
        var parameters = Assert.IsType<DictionaryObject>(reader.Resolve(stream.Dictionary["Params"]));
        return Assert.IsType<StringObject>(reader.Resolve(parameters["ModDate"])).Value;
    }

    [Fact]
    public void Attach_TwoFiles_NameTreeContainsBoth()
    {
        var reader = ReadAuthored(AuthorWithBothAttachments());
        var files = EmbeddedFiles(reader);

        Assert.Equal(2, files.Count);
        Assert.Contains("factur-x.xml", files.Keys);
        Assert.Contains("scan.bin", files.Keys);
    }

    [Fact]
    public void Attach_Filespec_HasAllSpecKeys()
    {
        var reader = ReadAuthored(AuthorWithBothAttachments());
        var files = EmbeddedFiles(reader);

        foreach (var (name, relationship) in new[] { ("factur-x.xml", "Alternative"), ("scan.bin", "Supplement") })
        {
            var filespec = files[name];
            Assert.Equal("Filespec", BuildTestSupport.Name(reader, filespec, "Type"));
            Assert.Equal(name, Assert.IsType<StringObject>(reader.Resolve(filespec["F"])).Value);
            Assert.Equal(name, Assert.IsType<StringObject>(reader.Resolve(filespec["UF"])).Value);
            Assert.Equal(relationship, BuildTestSupport.Name(reader, filespec, "AFRelationship"));

            var stream = EmbeddedStream(reader, filespec);
            var parameters = Assert.IsType<DictionaryObject>(reader.Resolve(stream.Dictionary["Params"]));
            Assert.True(parameters.ContainsKey("Size"), "/Params has /Size");
            Assert.True(parameters.ContainsKey("ModDate"), "/Params has /ModDate");
        }
    }

    [Fact]
    public void Attach_EmbeddedFileDict_HasFAndUfStreamReferences()
    {
        var reader = ReadAuthored(AuthorWithBothAttachments());
        var filespec = EmbeddedFiles(reader)["factur-x.xml"];

        var f = EmbeddedStream(reader, filespec, "F");
        var uf = EmbeddedStream(reader, filespec, "UF");

        Assert.Same(f, uf);
        Assert.Equal(InvoiceXml, Payload(reader, f));
    }

    [Fact]
    public void Attach_EmbeddedStream_ParamsSizeAndModDate()
    {
        var reader = ReadAuthored(AuthorWithBothAttachments());
        var files = EmbeddedFiles(reader);

        var xml = EmbeddedStream(reader, files["factur-x.xml"]);
        var xmlParams = Assert.IsType<DictionaryObject>(reader.Resolve(xml.Dictionary["Params"]));
        Assert.Equal(InvoiceXml.Length, BuildTestSupport.Int(xmlParams, "Size"));
        Assert.Equal("D:20260315083045+00'00'", ParamsModDate(reader, xml));

        var binary = EmbeddedStream(reader, files["scan.bin"]);
        var binaryParams = Assert.IsType<DictionaryObject>(reader.Resolve(binary.Dictionary["Params"]));
        Assert.Equal(BinaryPayload.Length, BuildTestSupport.Int(binaryParams, "Size"));
        Assert.Equal("D:20000101000000+00'00'", ParamsModDate(reader, binary));
    }

    [Fact]
    public void Attach_MimeType_WrittenAsSubtypeName()
    {
        var reader = ReadAuthored(AuthorWithBothAttachments());
        var files = EmbeddedFiles(reader);

        Assert.Equal("text/xml", BuildTestSupport.Name(reader, EmbeddedStream(reader, files["factur-x.xml"]).Dictionary, "Subtype"));
        Assert.Equal("application/octet-stream", BuildTestSupport.Name(reader, EmbeddedStream(reader, files["scan.bin"]).Dictionary, "Subtype"));
    }

    [Fact]
    public void Attach_Description_RoundTrips()
    {
        var reader = ReadAuthored(AuthorWithBothAttachments());
        var files = EmbeddedFiles(reader);

        Assert.Equal("Factur-X invoice data",
            Assert.IsType<StringObject>(reader.Resolve(files["factur-x.xml"]["Desc"])).Value);
        Assert.False(files["scan.bin"].ContainsKey("Desc"), "no /Desc when the description is not set");
    }

    [Fact]
    public void Attach_CatalogAf_ListsBothFilespecs()
    {
        var reader = ReadAuthored(AuthorWithBothAttachments());
        var catalog = Catalog(reader);
        var af = Assert.IsType<ArrayObject>(reader.Resolve(catalog["AF"]));

        Assert.Equal(2, af.Count);

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in af)
        {
            var filespec = Assert.IsType<DictionaryObject>(reader.Resolve(entry));
            Assert.Equal("Filespec", BuildTestSupport.Name(reader, filespec, "Type"));
            names.Add(Assert.IsType<StringObject>(reader.Resolve(filespec["F"])).Value);
        }

        Assert.Equal(new HashSet<string>(["factur-x.xml", "scan.bin"]), names);
    }

    [Fact]
    public void Attach_BinaryPayload_RoundTripsByteIdentical()
    {
        var reader = ReadAuthored(AuthorWithBothAttachments());
        var files = EmbeddedFiles(reader);

        Assert.Equal(BinaryPayload, Payload(reader, EmbeddedStream(reader, files["scan.bin"])));
        Assert.Equal(InvoiceXml, Payload(reader, EmbeddedStream(reader, files["factur-x.xml"])));
    }

    [Fact]
    public void Build_SameInputs_ByteIdentical()
    {
        var first = RenderAuthored(AuthorWithBothAttachments());
        var second = RenderAuthored(AuthorWithBothAttachments());

        Assert.Equal(first, second);
    }

    [Fact]
    public void Build_SameInputs_PdfA3B_ByteIdenticalIgnoringTrailerId()
    {
        var first = RenderAuthored(AuthorWithBothAttachments(PdfAConformance.PdfA3B));
        var second = RenderAuthored(AuthorWithBothAttachments(PdfAConformance.PdfA3B));

        Assert.Equal(MaskDocumentId(first), MaskDocumentId(second));
    }

    private static string MaskDocumentId(byte[] pdf)
        => Regex.Replace(Encoding.Latin1.GetString(pdf), @"\(([0-9A-F]{32})\)", "(ID)");
}
