#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class PdfAConformanceTests
{
    private static (Document Document, DocumentRenderer Renderer) Author(PdfAConformance conformance)
    {
        var document = new Document();
        var renderer = new DocumentRenderer();
        BuildTestSupport.RegisterLatin(document);

        document.Info.Title = "PDF/A Invoice";
        document.Info.Author = "Radzen Ltd";
        document.Info.Subject = "Conformance fixture";
        document.Info.Creator = "Radzen.Documents.Pdf tests";
        document.Info.Keywords = "pdfa, invoice";

        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Hello PDF/A", BuildTestSupport.Latin);

        renderer.Conformance = conformance;
        return (document, renderer);
    }

    private static string Emitted((Document Document, DocumentRenderer Renderer) authored)
        => Encoding.Latin1.GetString(authored.Renderer.ToArray(authored.Document));

    private static string MetadataStream(string emission)
    {
        var reference = Shaped("catalog", @"/Metadata (\d+) 0 R", Line(emission, "/Type /Catalog"));
        var metadata = IndirectObject(emission, reference.Groups[1].Value);
        Lacks("metadata stream", "/Filter", metadata);
        return metadata;
    }

    [Fact]
    public void PdfA3B_MetadataStream_HasPdfaidPart3ConformanceB()
    {
        var metadata = MetadataStream(Emitted(Author(PdfAConformance.PdfA3B)));

        Carries("metadata stream", "/Type /Metadata", metadata);
        Carries("metadata stream", "/Subtype /XML", metadata);
        Carries("metadata stream", "<?xpacket begin=", metadata);
        Carries("metadata stream", "<?xpacket end=\"w\"?>", metadata);
        Carries("metadata stream", "<pdfaid:part>3</pdfaid:part>", metadata);
        Carries("metadata stream", "<pdfaid:conformance>B</pdfaid:conformance>", metadata);
    }

    [Fact]
    public void PdfA3A_MetadataStream_HasConformanceA()
    {
        var metadata = MetadataStream(Emitted(Author(PdfAConformance.PdfA3A)));

        Carries("metadata stream", "<pdfaid:part>3</pdfaid:part>", metadata);
        Carries("metadata stream", "<pdfaid:conformance>A</pdfaid:conformance>", metadata);
    }

    [Fact]
    public void PdfA3B_Xmp_MatchesDocumentInfo()
    {
        var metadata = MetadataStream(Emitted(Author(PdfAConformance.PdfA3B)));

        Carries("metadata stream", "<dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">PDF/A Invoice</rdf:li>", metadata);
        Carries("metadata stream", "<dc:creator><rdf:Seq><rdf:li>Radzen Ltd</rdf:li>", metadata);
        Carries("metadata stream", "Conformance fixture</rdf:li></rdf:Alt></dc:description>", metadata);
        Carries("metadata stream", "<xmp:CreatorTool>Radzen.Documents.Pdf tests</xmp:CreatorTool>", metadata);
        Carries("metadata stream", "<pdf:Keywords>pdfa, invoice</pdf:Keywords>", metadata);
        Carries("metadata stream", "<pdf:Producer>", metadata);
    }

    [Fact]
    public void PdfA3B_OutputIntents_SrgbGtsPdfa1WithIccProfile()
    {
        var emission = Emitted(Author(PdfAConformance.PdfA3B));

        var intents = References("catalog", "OutputIntents", 1, Line(emission, "/Type /Catalog"));
        var intent = IndirectObject(emission, intents[0]);

        Carries("output intent", "/Type /OutputIntent", intent);
        Carries("output intent", "/S /GTS_PDFA1", intent);
        Carries("output intent", "/OutputConditionIdentifier ", intent);

        var reference = Shaped("output intent", @"/DestOutputProfile (\d+) 0 R", intent);
        var profile = IndirectObject(emission, reference.Groups[1].Value);

        Assert.Equal(3, NumberIn(profile, "N"));

        var length = NumberIn(profile, "Length");
        Assert.True(length > 128, $"DestOutputProfile carries only {length} bytes of ICC data.");
        Shaped("DestOutputProfile", @"stream\n[\s\S]{36}acsp", profile);
    }

    [Fact]
    public void PdfA3B_Trailer_HasDocumentId()
    {
        var emission = Emitted(Author(PdfAConformance.PdfA3B));

        Shaped("trailer", @"/ID \[\([^)]+\) \([^)]+\)\]", Line(emission, "/Root "));
    }

    [Fact]
    public void PdfA3B_HasNoEncryption()
    {
        var emission = Emitted(Author(PdfAConformance.PdfA3B));

        Lacks("trailer", "/Encrypt", Line(emission, "/Root "));
    }

    [Fact]
    public void PdfA3B_AllFonts_EmbeddedSubsetsWithCidSet()
    {
        var emission = Emitted(Author(PdfAConformance.PdfA3B));

        var resource = Shaped("page /Resources", @"/Font << /\w+ (\d+) 0 R >>", Line(emission, "/Type /Page "));
        var font = IndirectObject(emission, resource.Groups[1].Value);

        Carries("font", "/Subtype /Type0", font);

        var descendants = References("font", "DescendantFonts", 1, font);
        var descendant = IndirectObject(emission, descendants[0]);

        var descriptorReference = Shaped("descendant font", @"/FontDescriptor (\d+) 0 R", descendant);
        var descriptor = IndirectObject(emission, descriptorReference.Groups[1].Value);

        Shaped("font descriptor", @"/FontFile[23] \d+ 0 R", descriptor);

        var cidSetReference = Shaped("font descriptor", @"/CIDSet (\d+) 0 R", descriptor);
        var cidSet = IndirectObject(emission, cidSetReference.Groups[1].Value);

        var length = NumberIn(cidSet, "Length");
        Assert.True(length > 0, $"/CIDSet carries {length} bytes.");
    }

    [Fact]
    public void PdfA3B_Base14FontByName_ThrowsActionable()
    {
        var document = new Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Hello", "Helvetica");
        var renderer = new DocumentRenderer();
        renderer.Conformance = PdfAConformance.PdfA3B;

        var exception = Assert.Throws<InvalidOperationException>(() => renderer.ToArray(document));

        Assert.Contains("PDF/A", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Helvetica", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void None_Base14FontByName_Succeeds()
    {
        var document = new Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Hello", "Helvetica");

        Assert.NotEmpty(new DocumentRenderer().ToArray(document));
    }

    [Fact]
    public void PdfA3A_OutputIsTagged()
    {
        var emission = Emitted(Author(PdfAConformance.PdfA3A));
        var catalog = Line(emission, "/Type /Catalog");

        Carries("catalog", "/MarkInfo << /Marked true >>", catalog);

        var reference = Shaped("catalog", @"/StructTreeRoot (\d+) 0 R", catalog);
        Carries("structure tree root", "/Type /StructTreeRoot", IndirectObject(emission, reference.Groups[1].Value));
    }

    [Fact]
    public void None_ProducesNoPdfaMachinery()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Plain", BuildTestSupport.Latin);

        var emission = Encoding.Latin1.GetString(new DocumentRenderer().ToArray(document));

        Lacks("catalog", "/OutputIntents", Line(emission, "/Type /Catalog"));
        Lacks("emission", "pdfaid:part", emission);
    }

    [Fact]
    public void PdfA3B_ContentSurvives_ExtractText()
    {
        var authored = Author(PdfAConformance.PdfA3B);
        var document = BuildTestSupport.Reload(authored.Document, authored.Renderer);
        Assert.Contains("Hello PDF/A", document.Pages[0].ExtractText(), StringComparison.Ordinal);
    }
}
