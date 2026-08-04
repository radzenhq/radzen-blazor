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
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

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

    private static string NameTreeNode(string emission)
    {
        var tree = Shaped("catalog", @"/Names << /EmbeddedFiles (\d+) 0 R >>", Line(emission, "/Type /Catalog"));
        return IndirectObject(emission, tree.Groups[1].Value);
    }

    private static string Filespec(string emission, string name)
    {
        var entry = Shaped(
            $"embedded files name tree entry for {name}",
            $@"\({Regex.Escape(name)}\) (\d+) 0 R",
            NameTreeNode(emission));
        return IndirectObject(emission, entry.Groups[1].Value);
    }

    private static string EmbeddedFileDictionary(string emission, string filespec)
    {
        var ef = Shaped("filespec /EF", @"/EF << /F (\d+) 0 R /UF (\d+) 0 R >>", filespec);
        return Line(IndirectObject(emission, ef.Groups[1].Value), "/Type /EmbeddedFile");
    }

    [Fact]
    public void Attach_TwoFiles_NameTreeContainsBoth()
    {
        var tree = NameTreeNode(Emit(AuthorWithBothAttachments()));

        Carries("embedded files name tree", "(factur-x.xml) ", tree);
        Carries("embedded files name tree", "(scan.bin) ", tree);

        var entries = Regex.Matches(tree, @"\([^)]*\) \d+ 0 R").Count;
        Assert.True(
            entries == 2,
            $"Expected 2 embedded file entries, found {entries}.\nname tree:\n{Excerpt(tree)}");
    }

    [Fact]
    public void Attach_Filespec_HasAllSpecKeys()
    {
        var emission = Emit(AuthorWithBothAttachments());

        foreach (var (name, relationship) in new[] { ("factur-x.xml", "Alternative"), ("scan.bin", "Supplement") })
        {
            var filespec = Filespec(emission, name);
            Carries($"{name} filespec", "/Type /Filespec", filespec);
            Carries($"{name} filespec", $"/F ({name})", filespec);
            Carries($"{name} filespec", $"/UF ({name})", filespec);
            Carries($"{name} filespec", $"/AFRelationship /{relationship}", filespec);

            var embedded = EmbeddedFileDictionary(emission, filespec);
            Carries($"{name} embedded file", "/Params << /Size ", embedded);
            Carries($"{name} embedded file", "/ModDate (", embedded);
        }
    }

    [Fact]
    public void Attach_EmbeddedFileDict_HasFAndUfStreamReferences()
    {
        var rendered = AuthorWithBothAttachments();
        var ef = Shaped(
            "factur-x.xml filespec /EF",
            @"/EF << /F (\d+) 0 R /UF (\d+) 0 R >>",
            Filespec(Emit(rendered), "factur-x.xml"));

        Assert.Equal(ef.Groups[1].Value, ef.Groups[2].Value);

        var reader = ReadAuthored(rendered);
        Assert.Equal(InvoiceXml, Payload(reader, EmbeddedStream(reader, EmbeddedFiles(reader)["factur-x.xml"])));
    }

    [Fact]
    public void Attach_EmbeddedStream_ParamsSizeAndModDate()
    {
        var emission = Emit(AuthorWithBothAttachments());

        var xml = EmbeddedFileDictionary(emission, Filespec(emission, "factur-x.xml"));
        Assert.Equal(InvoiceXml.Length, NumberIn(xml, "Size"));
        Carries("factur-x.xml embedded file", "/ModDate (D:20260315083045+00'00')", xml);

        var binary = EmbeddedFileDictionary(emission, Filespec(emission, "scan.bin"));
        Assert.Equal(BinaryPayload.Length, NumberIn(binary, "Size"));
        Carries("scan.bin embedded file", "/ModDate (D:20000101000000+00'00')", binary);
    }

    [Fact]
    public void Attach_MimeType_WrittenAsSubtypeName()
    {
        var emission = Emit(AuthorWithBothAttachments());

        Carries(
            "factur-x.xml embedded file",
            "/Subtype /text#2Fxml",
            EmbeddedFileDictionary(emission, Filespec(emission, "factur-x.xml")));
        Carries(
            "scan.bin embedded file",
            "/Subtype /application#2Foctet-stream",
            EmbeddedFileDictionary(emission, Filespec(emission, "scan.bin")));
    }

    [Fact]
    public void Attach_Description_RoundTrips()
    {
        var emission = Emit(AuthorWithBothAttachments());

        Carries("factur-x.xml filespec", "/Desc (Factur-X invoice data)", Filespec(emission, "factur-x.xml"));
        Lacks("scan.bin filespec", "/Desc", Filespec(emission, "scan.bin"));
    }

    [Fact]
    public void Attach_CatalogAf_ListsBothFilespecs()
    {
        var emission = Emit(AuthorWithBothAttachments());

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var number in References("catalog", "AF", 2, Line(emission, "/Type /Catalog")))
        {
            var filespec = IndirectObject(emission, number);
            Carries($"filespec {number} 0 R", "/Type /Filespec", filespec);
            names.Add(Shaped($"filespec {number} 0 R", @"/F \(([^)]*)\)", filespec).Groups[1].Value);
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
